using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

// Đọc/ghi dữ liệu basket (SET) từ MES.TRTB_M_COMPSTT_SET_HEADER/DETAIL — tạo ra bởi
// MES.PROC_CREATE_COMPSTT_SET khi Set In (xem YieldService.SaveYieldBatchAsync).
public class CompSttSetService
{
    private readonly AgmesOracleService _db;

    public CompSttSetService(AgmesOracleService db)
    {
        _db = db;
    }

    // I_CARD_NO có UNIQUE INDEX riêng — 1 PCard chỉ thuộc đúng 1 basket, tra thẳng ra BASKET_ID.
    public async Task<int?> GetBasketIdByCardNoAsync(string cardNo)
    {
        const string sql = @"
            SELECT BASKET_ID FROM MES.TRTB_M_COMPSTT_SET_HEADER
            WHERE I_CARD_NO = :cardNo";

        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["BASKET_ID"]),
            new OracleParameter("cardNo", cardNo));
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<List<CompSttSetHeaderRow>> GetBasketHeaderAsync(int basketId)
    {
        const string sql = @"
            SELECT BASKET_ID, I_CARD_NO, I_PO_NO, C_SIZE, C_STYLE, C_KEYINLOC,
                   I_PARTS_NO, N_PARTS_NO, Q_PLAN, C_QTY, IS_IN, IN_DATE,
                   IS_COMPLETE_SET, IS_OUT
            FROM MES.TRTB_M_COMPSTT_SET_HEADER
            WHERE BASKET_ID = :basketId
            ORDER BY I_CARD_NO";

        return await _db.ExecuteQueryAsync(sql, MapHeaderRow, new OracleParameter("basketId", basketId));
    }

    public async Task<List<CompSttSetDetailRow>> GetBasketDetailAsync(int basketId)
    {
        const string sql = @"
            SELECT BASKET_ID, I_PO_NO, C_SIZE, C_STYLE, C_KEYINLOC,
                   I_PARTS_NO, N_PARTS_NO, C_QTY, QTY_RECEIVE, IS_DONE, DATE_DONE, WORKER_ID
            FROM MES.TRTB_M_COMPSTT_SET_DETAIL
            WHERE BASKET_ID = :basketId
            ORDER BY I_PARTS_NO";

        return await _db.ExecuteQueryAsync(sql, MapDetailRow, new OracleParameter("basketId", basketId));
    }

    // Lấy dòng detail hiện tại (I_PO_NO/C_QTY/QTY_RECEIVE) để tính INPUT_QTY (số cộng thêm) trước
    // khi gọi PROC_SCAN_UPDATE_SET — thủ tục CỘNG DỒN vào QTY_RECEIVE chứ không set thẳng.
    private async Task<(string? PoNo, int CQty, int QtyReceive)?> GetDetailRowAsync(int basketId, string partsNo)
    {
        const string sql = @"
            SELECT I_PO_NO, C_QTY, QTY_RECEIVE
            FROM MES.TRTB_M_COMPSTT_SET_DETAIL
            WHERE BASKET_ID = :basketId AND I_PARTS_NO = :partsNo";

        var results = await _db.ExecuteQueryAsync(sql,
            r => (r["I_PO_NO"] as string, r["C_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["C_QTY"]), r["QTY_RECEIVE"] == DBNull.Value ? 0 : Convert.ToInt32(r["QTY_RECEIVE"])),
            new OracleParameter("basketId", basketId),
            new OracleParameter("partsNo", partsNo));

        return results.Count > 0 ? results[0] : null;
    }

    // Bấm 1 lần (chưa có QTY_RECEIVE) — nhận đủ theo C_QTY: cộng thêm đúng phần còn thiếu.
    public async Task<(bool Success, string? Message)> MarkBasketDetailDoneAsync(int basketId, string partsNo, string workerId)
    {
        if (await IsBasketOutAsync(basketId))
        {
            return (false, "Basket này đã Set Out rồi — không thể sửa số lượng nhận nữa.");
        }

        var row = await GetDetailRowAsync(basketId, partsNo);
        if (row == null)
        {
            return (false, "Không tìm thấy dòng part này trong basket.");
        }

        var inputQty = row.Value.CQty - row.Value.QtyReceive;
        return await ScanUpdateSetAsync(row.Value.PoNo, basketId, partsNo, inputQty, workerId);
    }

    // Bấm lần 2 trở đi — nhập tay TỔNG số lượng thực nhận mong muốn (giữ đúng UX cũ), tự quy đổi
    // ra INPUT_QTY (chênh lệch so với QTY_RECEIVE hiện tại) để cộng dồn đúng theo thủ tục.
    public async Task<(bool Success, string? Message)> UpdateBasketDetailQtyAsync(int basketId, string partsNo, int newTotalQty, string workerId)
    {
        if (await IsBasketOutAsync(basketId))
        {
            return (false, "Basket này đã Set Out rồi — không thể sửa số lượng nhận nữa.");
        }

        var row = await GetDetailRowAsync(basketId, partsNo);
        if (row == null)
        {
            return (false, "Không tìm thấy dòng part này trong basket.");
        }
        if (newTotalQty < 0 || newTotalQty > row.Value.CQty)
        {
            return (false, $"Số lượng ({newTotalQty}) phải từ 0 đến {row.Value.CQty}.");
        }

        var inputQty = newTotalQty - row.Value.QtyReceive;
        return await ScanUpdateSetAsync(row.Value.PoNo, basketId, partsNo, inputQty, workerId);
    }

    // Basket có 1-2 thẻ (header row/thẻ) — chỉ cần 1 thẻ trong basket đã Set Out là coi như basket
    // đã "chốt", không cho sửa số lượng nhận nữa (tránh sửa ngược sau khi hàng đã ra chuyền).
    private async Task<bool> IsBasketOutAsync(int basketId)
    {
        const string sql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_COMPSTT_SET_HEADER
            WHERE BASKET_ID = :basketId AND IS_OUT = 'Y'";

        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("basketId", basketId));
        return results.FirstOrDefault() > 0;
    }

    // Set Out: chỉ cho phép khi C_QTY (số lượng của thẻ) = SET_QTY (đã tạo đủ SET) VÀ thẻ CHƯA
    // Out lần nào (IS_OUT khác 'Y') — đạt điều kiện thì đánh dấu IS_OUT='Y', DATE_OUT=SYSDATE cho
    // đúng dòng header của thẻ này (I_CARD_NO unique). Thiếu check IS_OUT trước đây khiến 1 thẻ
    // Set Out được nhiều lần (mỗi lần đều ghi thêm dòng vào TRTB_M_KEYIN_YIELD).
    public async Task<(bool Success, string? Message)> TryMarkCardOutAsync(string cardNo, string? lineOut)
    {
        const string selectSql = @"
            SELECT C_QTY, SET_QTY, IS_OUT
            FROM MES.TRTB_M_COMPSTT_SET_HEADER
            WHERE I_CARD_NO = :cardNo";

        var rows = await _db.ExecuteQueryAsync(selectSql,
            r => (CQty: r["C_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["C_QTY"]),
                  SetQty: r["SET_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["SET_QTY"]),
                  IsOut: r["IS_OUT"] as string),
            new OracleParameter("cardNo", cardNo));

        if (rows.Count == 0)
        {
            return (false, $"Không tìm thấy basket cho PCard {cardNo} — chưa Set In.");
        }

        var (cQty, setQty, isOut) = rows[0];
        if (string.Equals(isOut, "Y", StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"PCard {cardNo} đã Out rồi — không thể Out thêm lần nữa.");
        }
        if (cQty != setQty)
        {
            return (false, $"PCard {cardNo}: chưa đủ SET ({setQty}/{cQty}) — không thể Set Out.");
        }

        // WHERE thêm IS_OUT <> 'Y' làm lưới an toàn cho race condition (2 request Set Out cùng
        // thẻ gần như đồng thời) — chỉ request nào thật sự update được (affected=1) mới coi là hợp lệ.
        const string updateSql = @"
            UPDATE MES.TRTB_M_COMPSTT_SET_HEADER
            SET IS_OUT = 'Y', DATE_OUT = SYSDATE, LINEOUT = :lineOut
            WHERE I_CARD_NO = :cardNo AND NVL(IS_OUT, 'N') <> 'Y'";

        var affected = await _db.ExecuteNonQueryAsync(updateSql,
            new OracleParameter("lineOut", string.IsNullOrWhiteSpace(lineOut) ? (object)DBNull.Value : lineOut),
            new OracleParameter("cardNo", cardNo));
        if (affected == 0)
        {
            return (false, $"PCard {cardNo} đã Out rồi — không thể Out thêm lần nữa.");
        }
        return (true, null);
    }

    private async Task<(bool Success, string? Message)> ScanUpdateSetAsync(string? poNo, int basketId, string partsNo, int inputQty, string workerId)
    {
        try
        {
            await _db.ExecuteProcedureAsync("MES.PROC_SCAN_UPDATE_SET",
                new OracleParameter("P_PO_NO", poNo ?? string.Empty),
                new OracleParameter("P_BASKET_ID", basketId),
                new OracleParameter("P_PARTS_NO", partsNo),
                new OracleParameter("INPUT_QTY", inputQty),
                new OracleParameter("P_WORKER_ID", workerId ?? string.Empty));
            return (true, null);
        }
        catch (OracleException ex)
        {
            return (false, ex.Message);
        }
    }

    private static CompSttSetHeaderRow MapHeaderRow(OracleDataReader r) => new()
    {
        BasketId = Convert.ToInt32(r["BASKET_ID"]),
        ICardNo = r["I_CARD_NO"] as string,
        IPoNo = r["I_PO_NO"] as string,
        CSize = r["C_SIZE"] as string,
        CStyle = r["C_STYLE"] as string,
        CKeyinloc = r["C_KEYINLOC"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        NPartsNo = r["N_PARTS_NO"] as string,
        QPlan = r["Q_PLAN"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_PLAN"]),
        CQty = r["C_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["C_QTY"]),
        IsIn = r["IS_IN"] as string,
        InDate = r["IN_DATE"] == DBNull.Value ? null : Convert.ToDateTime(r["IN_DATE"]),
        IsCompleteSet = r["IS_COMPLETE_SET"] as string,
        IsOut = r["IS_OUT"] as string
    };

    private static CompSttSetDetailRow MapDetailRow(OracleDataReader r) => new()
    {
        BasketId = Convert.ToInt32(r["BASKET_ID"]),
        IPoNo = r["I_PO_NO"] as string,
        CSize = r["C_SIZE"] as string,
        CStyle = r["C_STYLE"] as string,
        CKeyinloc = r["C_KEYINLOC"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        NPartsNo = r["N_PARTS_NO"] as string,
        CQty = r["C_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["C_QTY"]),
        QtyReceive = r["QTY_RECEIVE"] == DBNull.Value ? 0 : Convert.ToInt32(r["QTY_RECEIVE"]),
        IsDone = r["IS_DONE"] as string,
        DateDone = r["DATE_DONE"] == DBNull.Value ? null : Convert.ToDateTime(r["DATE_DONE"]),
        WorkerId = r["WORKER_ID"] as string
    };
}
