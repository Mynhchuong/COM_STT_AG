using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

// Báo cáo Kế hoạch / Đã tạo SET / Còn lại theo Size, dựa trên MES.V_COMPSTT_SET_REPORT_BASE
// (view JOIN đơn giản TRTB_M_PROD_PLAN + TRTB_M_PART_INFO + TRTB_M_COMPSTT_SET_HEADER, không lọc
// sẵn ngày/PO/Part — filter được truyền động ở đây vì Oracle VIEW không nhận tham số runtime).
public class ReportService
{
    private readonly AgmesOracleService _db;

    public ReportService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<List<CompSttSetReportRow>> GetCompSttSetReportAsync(DateTime fromDate, DateTime toDate, string? poFilter, string? partFilter)
    {
        var sizeCols = YieldService.SizeCodes;
        var qplanCols = string.Join(",\n            ", sizeCols.Select(s => $"MAX(CASE WHEN C_SIZE='{s}' THEN Q_PLAN END) AS S{s}"));
        var setqtyColsSum = string.Join(",\n            ", sizeCols.Select(s => $"SUM(CASE WHEN C_SIZE='{s}' THEN SET_QTY ELSE 0 END) AS S{s}"));
        var selectQplanCols = string.Join(", ", sizeCols.Select(s => $"P.S{s} AS S{s}"));
        var selectSetqtyCols = string.Join(", ", sizeCols.Select(s => $"S.S{s} AS S{s}"));
        var selectBalanceCols = string.Join(", ", sizeCols.Select(s => $"(NVL(P.S{s},0) - NVL(S.S{s},0)) AS S{s}"));

        // CHỈ thêm điều kiện LIKE khi người dùng thực sự nhập PO/Part — để trống mà vẫn giữ
        // "LIKE :poFilter" với bind value '%' khiến Oracle không tối ưu được (bind variable, không
        // biết trước là match-all), gây full scan treo >120s dù chạy SQL trực tiếp (literal) rất
        // nhanh. Bỏ hẳn điều kiện khi trống thay vì LIKE '%' mới khớp đúng plan nhanh như test tay.
        var extraConditions = new List<string>();
        var parameters = new List<OracleParameter>
        {
            new("fromDate", OracleDbType.Date) { Value = fromDate },
            new("toDate", OracleDbType.Date) { Value = toDate }
        };
        if (!string.IsNullOrWhiteSpace(poFilter))
        {
            extraConditions.Add("AND I_PO_NO LIKE :poFilter");
            parameters.Add(new OracleParameter("poFilter", poFilter.Trim() + "%"));
        }
        if (!string.IsNullOrWhiteSpace(partFilter))
        {
            extraConditions.Add("AND I_PARTS_NO LIKE :partFilter");
            parameters.Add(new OracleParameter("partFilter", partFilter.Trim() + "%"));
        }
        var extraConditionsSql = extraConditions.Count > 0 ? string.Join("\n                  ", extraConditions) : "";

        var sql = $@"
            WITH BASE AS (
                SELECT I_PO_NO, C_STYLE, C_SIZE, I_PARTS_NO, N_PARTS_NO, Q_PLAN, SET_QTY
                FROM MES.V_COMPSTT_SET_REPORT_BASE
                WHERE IN_DATE BETWEEN :fromDate AND :toDate
                  {extraConditionsSql}
            ),
            PIVOT_QPLAN AS (
                SELECT I_PO_NO, C_STYLE, I_PARTS_NO, N_PARTS_NO,
                    {qplanCols}
                FROM BASE
                GROUP BY I_PO_NO, C_STYLE, I_PARTS_NO, N_PARTS_NO
            ),
            PIVOT_SETQTY AS (
                SELECT I_PO_NO, I_PARTS_NO,
                    {setqtyColsSum}
                FROM BASE
                GROUP BY I_PO_NO, I_PARTS_NO
            )
            SELECT P.I_PO_NO, P.C_STYLE, P.I_PARTS_NO, P.N_PARTS_NO, 1 AS LINE_NO, 'Q_PLAN' AS LINE_TYPE,
                {selectQplanCols}
            FROM PIVOT_QPLAN P
            UNION ALL
            SELECT P.I_PO_NO, P.C_STYLE, P.I_PARTS_NO, P.N_PARTS_NO, 2 AS LINE_NO, 'SET_QTY' AS LINE_TYPE,
                {selectSetqtyCols}
            FROM PIVOT_QPLAN P
            LEFT JOIN PIVOT_SETQTY S ON P.I_PO_NO = S.I_PO_NO AND P.I_PARTS_NO = S.I_PARTS_NO
            UNION ALL
            SELECT P.I_PO_NO, P.C_STYLE, P.I_PARTS_NO, P.N_PARTS_NO, 3 AS LINE_NO, 'BALANCE_QTY' AS LINE_TYPE,
                {selectBalanceCols}
            FROM PIVOT_QPLAN P
            LEFT JOIN PIVOT_SETQTY S ON P.I_PO_NO = S.I_PO_NO AND P.I_PARTS_NO = S.I_PARTS_NO
            ORDER BY 1, 3, 5";

        return await _db.ExecuteQueryAsync(sql, MapRow, parameters.ToArray());
    }

    // Báo cáo theo PO — dựa trên MES.V_COMPSTT_PO_REPORT (Part No cố định '190' đã bake sẵn
    // trong view). Chỉ lọc theo I_PO_NO = bind exact-match (không LIKE), đã test 158ms cho 1 PO.
    public async Task<List<CompSttPoReportRow>> GetCompSttPoReportAsync(string po)
    {
        const string sql = @"
            SELECT * FROM MES.V_COMPSTT_PO_REPORT
            WHERE I_PO_NO = :po
            ORDER BY RW, I_PARTS_NO, LINE_TYPE";

        return await _db.ExecuteQueryAsync(sql, MapPoReportRow, new OracleParameter("po", po));
    }

    private static CompSttPoReportRow MapPoReportRow(OracleDataReader r)
    {
        var row = new CompSttPoReportRow
        {
            IPoNo = r["I_PO_NO"] as string,
            Rw = Convert.ToInt32(r["RW"]),
            LineType = (r["LINE_TYPE"] as string)?.Trim(),
            IPartsNo = r["I_PARTS_NO"] as string,
            NPartsNo = r["N_PARTS_NO"] as string,
            LineNo = Convert.ToInt32(r["LINE_NO"])
        };

        foreach (var size in YieldService.SizeCodes)
        {
            var col = "SIZE_" + size;
            var val = r[col];
            row.Sizes[size] = val == DBNull.Value ? 0 : Convert.ToInt32(val);
        }

        return row;
    }

    private static CompSttSetReportRow MapRow(OracleDataReader r)
    {
        var row = new CompSttSetReportRow
        {
            IPoNo = r["I_PO_NO"] as string,
            CStyle = r["C_STYLE"] as string,
            IPartsNo = r["I_PARTS_NO"] as string,
            NPartsNo = r["N_PARTS_NO"] as string,
            LineNo = Convert.ToInt32(r["LINE_NO"]),
            LineType = r["LINE_TYPE"] as string
        };

        foreach (var size in YieldService.SizeCodes)
        {
            var col = "S" + size;
            var val = r[col];
            row.Sizes[size] = val == DBNull.Value ? 0 : Convert.ToInt32(val);
        }

        return row;
    }
}
