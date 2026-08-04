using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class YieldService
{
    private readonly AgmesOracleService _db;
    private readonly CompSttSetService _compSttSetService;

    public YieldService(AgmesOracleService db, CompSttSetService compSttSetService)
    {
        _db = db;
        _compSttSetService = compSttSetService;
    }

    public async Task<(int Count, string? PartYieldMessage, int? BasketId, List<string> OutputErrors)> SaveYieldBatchAsync(List<KeyinYieldItemDto> items)
    {
        if (items == null || !items.Any()) return (0, null, null, new List<string>());

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
        var outputErrors = new List<string>();

        // Chặn trùng NGAY TRONG 1 lần bấm Lưu (VD: gửi lại do lỗi mạng khiến batch bị trùng) —
        // không đụng tới các thẻ hợp lệ khác nhau đã lưu ở NHỮNG LẦN quét/lưu trước đó, vì
        // Q_QTY luôn lấy từ kế hoạch nên nhiều thẻ khác nhau cùng Style/Size vẫn hợp lệ và cần giữ đủ.
        var seenInBatch = new HashSet<string>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];

            // PHẢI có PcardNo trong key — 2 thẻ PCard KHÁC NHAU nhưng trùng Style/Size/Qty (rất
            // thường gặp, VD 2 thẻ cùng PO cùng size) vẫn là 2 dòng hợp lệ cần giữ đủ, không được
            // coi là trùng. Chỉ khi cùng PcardNo (VD gửi lại do lỗi mạng) mới thật sự là trùng.
            var dedupKey = string.Join('|',
                item.CAction, item.CKeyinloc, item.CPoNum, item.COrdNo,
                item.CStyle, item.CSize, item.CWidth, item.QQty, item.PcardNo);
            if (!seenInBatch.Add(dedupKey))
            {
                continue; // dòng này trùng y hệt 1 dòng khác đã xử lý trong cùng batch — bỏ qua
            }

            // Set Out (OUTPUT): chỉ cho lưu khi basket của thẻ này đã đủ SET (C_QTY = SET_QTY).
            // Không đạt thì bỏ qua thẻ này (không insert YIELD, không đánh dấu IS_OUT), báo lỗi rõ.
            if (string.Equals(item.CAction, "OUTPUT", StringComparison.OrdinalIgnoreCase))
            {
                var (canOut, outMessage) = await _compSttSetService.TryMarkCardOutAsync(item.PcardNo ?? string.Empty, item.LineOut);
                if (!canOut)
                {
                    outputErrors.Add(outMessage ?? $"PCard {item.PcardNo}: không thể Set Out.");
                    continue;
                }
            }

            // Tăng nhẹ 1 giây giữa các dòng nếu cần để đảm bảo tính duy nhất của D_GATHER nếu trùng toàn bộ các cột khác
            var itemTime = now.AddSeconds(i).ToString("yyyyMMddHHmmss");

            var parameters = new OracleParameter[]
            {
                new("dGather",   itemTime),
                new("cAction",   item.CAction ?? "INPUT"),
                new("cKeyinloc", item.CKeyinloc ?? "CUT_2"),
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
            }
        }

        // Set In (C_ACTION='INPUT') sau khi lưu xong TRTB_M_KEYIN_YIELD (không đổi, vẫn lưu như cũ ở
        // trên) — gọi THÊM 1 LẦN DUY NHẤT cho cả lượt (1 hoặc 2 thẻ) qua thủ tục
        // PROC_CREATE_COMPSTT_SET để tạo SET vào MES.TRTB_M_COMPSTT_SET_HEADER/DETAIL (bảng mới,
        // tách biệt hoàn toàn với TRTB_M_KEYIN_PART_YIELD/cách cũ SP_INSERT_KEYIN_PART_YIELD).
        // Set Out (OUTPUT) không gọi — chỉ lưu vào TRTB_M_KEYIN_YIELD.
        string? partYieldMessage = null;
        int? basketId = null;
        if (count > 0 && items[0].CAction?.Equals("INPUT", StringComparison.OrdinalIgnoreCase) == true)
        {
            var cardNo1 = items[0].PcardNo;
            var cardNo2 = items.Count > 1 ? items[1].PcardNo : null;
            var workerId = items[0].CWorker;
            partYieldMessage = await CreateCompSttSetAsync(cardNo1, cardNo2, workerId);

            // I_CARD_NO có UNIQUE INDEX riêng nên basket luôn tra được, kể cả khi PROC báo lỗi
            // trùng (ERROR: đã có basket từ trước) — vẫn cần basketId để chuyển sang Pending2.
            if (!string.IsNullOrWhiteSpace(cardNo1))
            {
                basketId = await _compSttSetService.GetBasketIdByCardNoAsync(cardNo1);
            }
        }

        return (count, partYieldMessage, basketId, outputErrors);
    }

    // Tạo SET cho 1 hoặc 2 PCard vừa Set In vào TRTB_M_COMPSTT_SET_HEADER/DETAIL qua thủ tục có sẵn.
    private async Task<string?> CreateCompSttSetAsync(string? cardNo1, string? cardNo2, string? workerId)
    {
        var resultParam = new OracleParameter("p_result", OracleDbType.Varchar2, 4000)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        await _db.ExecuteProcedureAsync("MES.PROC_CREATE_COMPSTT_SET",
            new OracleParameter("p_card_no1", cardNo1 ?? string.Empty),
            new OracleParameter("p_card_no2", string.IsNullOrWhiteSpace(cardNo2) ? (object)DBNull.Value : cardNo2),
            new OracleParameter("p_worker_id", workerId ?? string.Empty),
            resultParam);

        return resultParam.Value == DBNull.Value ? null : resultParam.Value?.ToString();
    }

    // Trước khi chuyển từ Set In sang trang Chờ Set In — kiểm tra Style đã có Routing chưa.
    // Chưa có Routing thì SP_INSERT_KEYIN_PART_YIELD sẽ không nổ được dòng nào vào
    // TRTB_M_KEYIN_PART_YIELD (do JOIN với bảng này ra rỗng) — vào trang Chờ Set In cũng
    // không có gì để xem, nên chặn sớm ở đây và báo cho biết luôn.
    public async Task<bool> RoutingExistsAsync(string style)
    {
        const string sql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_KEYIN_ROUTING
            WHERE C_STYLE = :styleVal";

        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("styleVal", style ?? string.Empty));
        return results.FirstOrDefault() > 0;
    }

    // date: định dạng yyyyMMdd — không truyền thì mặc định hôm nay (giữ đúng hành vi cũ).
    public async Task<List<KeyinYieldLogItem>> GetTodayAsync(string? worker, string? date = null)
    {
        var day = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyyMMdd") : date.Trim();

        var conditions = new List<string> { "SUBSTR(D_GATHER, 1, 8) = :day" };
        var parameters = new List<OracleParameter> { new("day", day) };

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

    // Nút "Hoàn tất": web chỉ cho bấm khi ĐÃ QUÉT = KẾ HOẠCH và CÒN LẠI = 0 ở mọi size/part (kiểm
    // tra phía client trước khi hiện nút) — nên ở đây chốt thẳng toàn bộ PO/CUT_2 luôn, không cần
    // NOT EXISTS kiểm tra lại. Sau khi hoàn tất, web sẽ khoá không cho bấm/sửa ô ĐÃ QUÉT nữa.
    public async Task<int> CompleteOrderAsync(string ordNo)
    {
        const string sql = @"
            UPDATE MES.TRTB_M_KEYIN_PART_YIELD
            SET Q_QTY = Q_PLAN, IS_DONE = 'Y',
                DATE_DONE = NVL(DATE_DONE, SYSDATE),
                IS_COMPLETE = 'Y',
                DATE_COMPLETE = NVL(DATE_COMPLETE, SYSDATE)
            WHERE I_PO_NO = :ordNo
              AND C_KEYINLOC = 'CUT_2'";

        return await _db.ExecuteNonQueryAsync(sql, new OracleParameter("ordNo", ordNo));
    }

    // Kiểm tra Order đã bấm Hoàn tất chưa (để khoá lại không cho sửa ô ĐÃ QUÉT khi vào lại trang).
    public async Task<bool> IsOrderCompleteAsync(string ordNo)
    {
        const string sql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_KEYIN_PART_YIELD
            WHERE I_PO_NO = :ordNo AND C_KEYINLOC = 'CUT_2' AND IS_COMPLETE = 'Y'";

        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("ordNo", ordNo));
        return results.FirstOrDefault() > 0;
    }

    // Bấm 1 lần (chưa xử lý lần nào) trên ô ĐÃ QUÉT — nhận đủ theo kế hoạch.
    public async Task<(bool Success, string? Message)> MarkPartYieldDoneAsync(string ordNo, string size, string partsNo)
    {
        const string sql = @"
            UPDATE MES.TRTB_M_KEYIN_PART_YIELD
            SET Q_QTY = Q_PLAN, IS_DONE = 'Y', DATE_DONE = SYSDATE
            WHERE I_PO_NO = :ordNo AND C_SIZE = :sizeVal AND I_PARTS_NO = :partsNo AND C_KEYINLOC = 'CUT_2'";

        var rows = await _db.ExecuteNonQueryAsync(sql,
            new OracleParameter("ordNo", ordNo),
            new OracleParameter("sizeVal", size),
            new OracleParameter("partsNo", partsNo));

        return rows > 0 ? (true, null) : (false, "Không tìm thấy dòng dữ liệu để cập nhật.");
    }

    // Bấm lần thứ 2 trở đi (đã xử lý rồi, muốn sửa) — nhập tay số lượng thực nhận, không được vượt kế hoạch.
    public async Task<(bool Success, string? Message)> UpdatePartYieldQtyAsync(string ordNo, string size, string partsNo, int qty)
    {
        if (qty < 0)
        {
            return (false, "Số lượng không hợp lệ.");
        }

        const string planSql = @"
            SELECT NVL(MAX(Q_PLAN), 0) AS Q_PLAN
            FROM MES.TRTB_M_KEYIN_PART_YIELD
            WHERE I_PO_NO = :ordNo AND C_SIZE = :sizeVal AND I_PARTS_NO = :partsNo AND C_KEYINLOC = 'CUT_2'";

        var planResults = await _db.ExecuteQueryAsync(planSql, r => Convert.ToInt32(r["Q_PLAN"]),
            new OracleParameter("ordNo", ordNo),
            new OracleParameter("sizeVal", size),
            new OracleParameter("partsNo", partsNo));
        var plan = planResults.FirstOrDefault();

        if (plan <= 0)
        {
            return (false, "Không tìm thấy kế hoạch cho part/size này.");
        }
        if (qty > plan)
        {
            return (false, $"Số lượng ({qty}) không được lớn hơn kế hoạch ({plan}).");
        }

        const string sql = @"
            UPDATE MES.TRTB_M_KEYIN_PART_YIELD
            SET Q_QTY = :qty, IS_DONE = 'N', DATE_DONE = NULL
            WHERE I_PO_NO = :ordNo AND C_SIZE = :sizeVal AND I_PARTS_NO = :partsNo AND C_KEYINLOC = 'CUT_2'";

        var rows = await _db.ExecuteNonQueryAsync(sql,
            new OracleParameter("qty", qty),
            new OracleParameter("ordNo", ordNo),
            new OracleParameter("sizeVal", size),
            new OracleParameter("partsNo", partsNo));

        return rows > 0 ? (true, null) : (false, "Không tìm thấy dòng dữ liệu để cập nhật.");
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
