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

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            // Tăng nhẹ 1 giây giữa các dòng nếu cần để đảm bảo tính duy nhất của D_GATHER nếu trùng toàn bộ các cột khác
            var itemTime = now.AddSeconds(i).ToString("yyyyMMddHHmmss");

            var parameters = new OracleParameter[]
            {
                new("dGather",   itemTime),
                new("cAction",   item.CAction ?? "INPUT"),
                new("cKeyinloc", item.CKeyinloc ?? "SCREEN"),
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
            if (affected > 0) count++;
        }

        return count;
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
