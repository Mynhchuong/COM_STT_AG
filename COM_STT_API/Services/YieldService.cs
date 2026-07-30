using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class YieldService
{
    private readonly AgmesOracleService _db;

    public YieldService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<int> SaveYieldBatchAsync(List<KeyinYieldItemDto> items)
    {
        if (items == null || !items.Any()) return 0;

        const string sql = @"
            INSERT INTO MES.TRTB_M_KEYIN_YIELD (
                D_GATHER, C_ACTION, C_KEYINLOC, C_KEYINPART, C_PO_NUM, C_ORD_NO, 
                C_WIDTH, C_STYLE, C_SIZE, C_WORK_LINE, C_WORKER, Q_QTY, I_IP_NO
            ) VALUES (
                :dGather, :cAction, :cKeyinloc, :cKeyinpart, :cPoNum, :cOrdNo, 
                :cWidth, :cStyle, :cSize, :cWorkLine, :cWorker, :qQty, :iIpNo
            )";

        int count = 0;
        var now = DateTime.Now;

        // Chặn trùng NGAY TRONG 1 lần bấm Lưu (VD: gửi lại do lỗi mạng khiến batch bị trùng) —
        // không đụng tới các thẻ hợp lệ khác nhau đã lưu ở NHỮNG LẦN quét/lưu trước đó, vì
        // Q_QTY luôn lấy từ kế hoạch nên nhiều thẻ khác nhau cùng Style/Size vẫn hợp lệ và cần giữ đủ.
        var seenInBatch = new HashSet<string>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            var dedupKey = string.Join('|',
                item.CAction, item.CKeyinloc, item.CPoNum, item.COrdNo,
                item.CStyle, item.CSize, item.CWidth, item.QQty);
            if (!seenInBatch.Add(dedupKey))
            {
                continue; // dòng này trùng y hệt 1 dòng khác đã xử lý trong cùng batch — bỏ qua
            }

            // Tăng nhẹ 1 giây giữa các dòng nếu cần để đảm bảo tính duy nhất của D_GATHER nếu trùng toàn bộ các cột khác
            var itemTime = now.AddSeconds(i).ToString("yyyyMMddHHmmss");

            var parameters = new OracleParameter[]
            {
                new("dGather",   itemTime),
                new("cAction",   item.CAction ?? "INPUT"),
                new("cKeyinloc", item.CKeyinloc ?? "SET"),
                new("cKeyinpart",item.CKeyinpart ?? "N/A"),
                new("cPoNum",    item.CPoNum ?? string.Empty),
                new("cOrdNo",    item.COrdNo ?? item.CPoNum ?? string.Empty),
                new("cWidth",    string.IsNullOrWhiteSpace(item.CWidth) ? (object)DBNull.Value : item.CWidth),
                new("cStyle",    item.CStyle ?? string.Empty),
                new("cSize",     item.CSize ?? string.Empty),
                new("cWorkLine", item.CWorkLine ?? string.Empty),
                new("cWorker",   item.CWorker ?? string.Empty),
                new("qQty",      item.QQty),
                new("iIpNo",     item.IIpNo ?? string.Empty)
            };

            var affected = await _db.ExecuteNonQueryAsync(sql, parameters);
            if (affected > 0)
            {
                count++;
                await InsertPartYieldAsync(item);
            }
        }

        return count;
    }

    // Ghi thêm vào TRTB_M_KEYIN_PART_YIELD (nổ theo routing/part cho công đoạn CUT_2) mỗi khi
    // lưu 1 dòng vào TRTB_M_KEYIN_YIELD. Bảng này chỉ có 1 dòng/part/Order/Style/Size/NGÀY nên
    // check tồn tại TRƯỚC khi insert (thay vì insert rồi bắt lỗi trùng).
    private async Task InsertPartYieldAsync(KeyinYieldItemDto item)
    {
        var exists = await PartYieldExistsTodayAsync(item.COrdNo, item.CSize, item.CStyle);
        if (exists) return;

        try
        {
            await _db.ExecuteProcedureAsync("MES.SP_INSERT_KEYIN_PART_YIELD",
                new OracleParameter("p_i_po_no", item.COrdNo ?? string.Empty),
                new OracleParameter("p_c_size", item.CSize ?? string.Empty),
                new OracleParameter("p_c_style", item.CStyle ?? string.Empty),
                new OracleParameter("p_q_qty", item.QQty),
                new OracleParameter("p_q_plan", item.QPlan));
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            // Lưới an toàn cho race condition giữa lúc check và lúc insert — đã có rồi thì bỏ qua.
        }
    }

    private async Task<bool> PartYieldExistsTodayAsync(string? ordNo, string? size, string? style)
    {
        // Lưu ý: "SIZE" là từ khoá dành riêng của Oracle — không dùng ":size" làm tên bind
        // variable (gây ORA-01745), đổi thành ":sizeVal"/":styleVal".
        const string sql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_KEYIN_PART_YIELD
            WHERE D_GATHER = TO_CHAR(SYSDATE, 'YYYYMMDD')
              AND I_PO_NO = :ordNo
              AND C_SIZE = :sizeVal
              AND C_STYLE = :styleVal";

        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("ordNo", ordNo ?? string.Empty),
            new OracleParameter("sizeVal", size ?? string.Empty),
            new OracleParameter("styleVal", style ?? string.Empty));

        return results.FirstOrDefault() > 0;
    }

    public async Task<List<KeyinYieldLogItem>> GetTodayAsync(string? worker)
    {
        var conditions = new List<string> { "SUBSTR(D_GATHER, 1, 8) = :today" };
        var parameters = new List<OracleParameter> { new("today", DateTime.Now.ToString("yyyyMMdd")) };

        if (!string.IsNullOrWhiteSpace(worker))
        {
            conditions.Add("C_WORKER = :worker");
            parameters.Add(new OracleParameter("worker", worker));
        }

        var sql = $@"
            SELECT D_GATHER, C_ACTION, C_KEYINLOC, C_KEYINPART, C_PO_NUM, C_ORD_NO,
                   C_WIDTH, C_STYLE, C_SIZE, C_WORK_LINE, C_WORKER, Q_QTY, I_IP_NO
            FROM MES.TRTB_M_KEYIN_YIELD
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY D_GATHER DESC";

        return await _db.ExecuteQueryAsync(sql, MapRow, parameters.ToArray());
    }

    public async Task<bool> DeleteAsync(string dGather)
    {
        const string sql = "DELETE FROM MES.TRTB_M_KEYIN_YIELD WHERE D_GATHER = :dGather";
        var rowsAffected = await _db.ExecuteNonQueryAsync(sql, new OracleParameter("dGather", dGather));
        return rowsAffected > 0;
    }

    // Danh sách 44 mã size cố định (01M..22T) — khớp đúng các cột SIZE_xx trong view V_KEYIN_YIELD_SIZE_PIVOT.
    public static readonly IReadOnlyList<string> SizeCodes = BuildSizeCodes();

    private static List<string> BuildSizeCodes()
    {
        var list = new List<string>();
        for (int i = 1; i <= 22; i++)
        {
            var n = i.ToString("00");
            list.Add(n + "M");
            list.Add(n + "T");
        }
        return list;
    }

    public async Task<List<KeyinYieldSizePivotRow>> GetSizePivotByOrderAsync(string ordNo)
    {
        var sizeCols = string.Join(", ", SizeCodes.Select(s => "SIZE_" + s));
        var sql = $@"
            SELECT C_PO_NUM, C_ORD_NO, C_STYLE, C_WIDTH, C_ACTION, C_KEYINLOC, {sizeCols}
            FROM MES.V_KEYIN_YIELD_SIZE_PIVOT
            WHERE C_ORD_NO = :ordNo
            ORDER BY C_STYLE, C_WIDTH, DECODE(C_ACTION, 'INPUT', 1, 'OUTPUT', 2, 'BALANCE', 3, 4)";

        return await _db.ExecuteQueryAsync(sql, MapSizePivotRow, new OracleParameter("ordNo", ordNo));
    }

    private static KeyinYieldSizePivotRow MapSizePivotRow(OracleDataReader r)
    {
        var row = new KeyinYieldSizePivotRow
        {
            CPoNum = r["C_PO_NUM"] as string,
            COrdNo = r["C_ORD_NO"] as string,
            CStyle = r["C_STYLE"] as string,
            CWidth = r["C_WIDTH"] as string,
            CAction = r["C_ACTION"] as string,
            CKeyinLoc = r["C_KEYINLOC"] as string
        };

        foreach (var size in SizeCodes)
        {
            var col = "SIZE_" + size;
            var val = r[col];
            row.Sizes[size] = val == DBNull.Value ? 0 : Convert.ToInt32(val);
        }

        return row;
    }

    // Đánh dấu hoàn tất Set In cho cả Order — không bắt buộc CÒN LẠI = 0, người dùng tự quyết định.
    public async Task<int> CompleteOrderAsync(string ordNo)
    {
        const string sql = @"
            UPDATE MES.TRTB_M_KEYIN_PART_YIELD
            SET IS_COMPLETE = 'Y', DATE_COMPLETE = SYSDATE
            WHERE I_PO_NO = :ordNo";

        return await _db.ExecuteNonQueryAsync(sql, new OracleParameter("ordNo", ordNo));
    }

    public async Task<List<KeyinPartYieldStatusRow>> GetPartYieldStatusByOrderAsync(string ordNo)
    {
        var sizeCols = string.Join(", ", SizeCodes.Select(s => "SIZE_" + s));
        var sql = $@"
            SELECT I_PO_NO, C_STYLE, ROW_TYPE, I_PARTS_NO, N_PARTS_NO, {sizeCols}
            FROM MES.V_KEYIN_PART_YIELD_STATUS
            WHERE I_PO_NO = :ordNo
            ORDER BY I_PARTS_NO, ROW_TYPE";

        return await _db.ExecuteQueryAsync(sql, MapPartYieldStatusRow, new OracleParameter("ordNo", ordNo));
    }

    private static KeyinPartYieldStatusRow MapPartYieldStatusRow(OracleDataReader r)
    {
        var row = new KeyinPartYieldStatusRow
        {
            IPoNo = r["I_PO_NO"] as string,
            CStyle = r["C_STYLE"] as string,
            RowType = r["ROW_TYPE"] as string,
            IPartsNo = r["I_PARTS_NO"] as string,
            NPartsNo = r["N_PARTS_NO"] as string
        };

        foreach (var size in SizeCodes)
        {
            var col = "SIZE_" + size;
            var val = r[col];
            row.Sizes[size] = val == DBNull.Value ? 0 : Convert.ToInt32(val);
        }

        return row;
    }

    private static KeyinYieldLogItem MapRow(OracleDataReader r) => new()
    {
        DGather = r["D_GATHER"] as string ?? string.Empty,
        CAction = r["C_ACTION"] as string,
        CKeyinloc = r["C_KEYINLOC"] as string,
        CKeyinpart = r["C_KEYINPART"] as string,
        CPoNum = r["C_PO_NUM"] as string,
        COrdNo = r["C_ORD_NO"] as string,
        CWidth = r["C_WIDTH"] as string,
        CStyle = r["C_STYLE"] as string,
        CSize = r["C_SIZE"] as string,
        CWorkLine = r["C_WORK_LINE"] as string,
        CWorker = r["C_WORKER"] as string,
        QQty = r["Q_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_QTY"]),
        IIpNo = r["I_IP_NO"] as string
    };
}
