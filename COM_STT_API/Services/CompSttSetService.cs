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

    // PARTS_TOTAL/PARTS_OUT (subquery đếm trên DETAIL): cho biết basket đang Out DỞ bao nhiêu
    // phần — dùng chung cho cả Pending2/SetOut (không hiển thị) lẫn Báo cáo theo PO (hiển thị).
    private const string HeaderSelectColumns = @"
        H.BASKET_ID, H.I_CARD_NO, H.I_PO_NO, H.C_SIZE, H.C_STYLE, H.C_KEYINLOC,
        H.I_PARTS_NO, H.N_PARTS_NO, H.Q_PLAN, H.C_QTY, H.SET_QTY, H.IS_IN, H.IN_DATE,
        H.IS_COMPLETE_SET, H.IS_OUT, H.DATE_OUT, H.PROCESS_OUT,
        (SELECT COUNT(*) FROM MES.TRTB_M_COMPSTT_SET_DETAIL D WHERE D.BASKET_ID = H.BASKET_ID) AS PARTS_TOTAL,
        (SELECT COUNT(*) FROM MES.TRTB_M_COMPSTT_SET_DETAIL D WHERE D.BASKET_ID = H.BASKET_ID AND D.IS_OUT = 'Y') AS PARTS_OUT";

    public async Task<List<CompSttSetHeaderRow>> GetBasketHeaderAsync(int basketId)
    {
        var sql = $@"
            SELECT {HeaderSelectColumns}
            FROM MES.TRTB_M_COMPSTT_SET_HEADER H
            WHERE H.BASKET_ID = :basketId
            ORDER BY H.I_CARD_NO";

        return await _db.ExecuteQueryAsync(sql, MapHeaderRow, new OracleParameter("basketId", basketId));
    }

    // Danh sách thẻ PCard theo PO — dùng cho Báo cáo theo PO, phân loại: đã Out xong / đang Out
    // dở (1 phần part đã Out) / đang chờ nhận (đã Set In, có nhận 1 phần) / chưa nhận gì.
    public async Task<List<CompSttSetHeaderRow>> GetHeaderRowsByPoAsync(string po)
    {
        var sql = $@"
            SELECT {HeaderSelectColumns}
            FROM MES.TRTB_M_COMPSTT_SET_HEADER H
            WHERE H.I_PO_NO = :po
            ORDER BY H.I_CARD_NO";

        return await _db.ExecuteQueryAsync(sql, MapHeaderRow, new OracleParameter("po", po));
    }

    public async Task<List<CompSttSetDetailRow>> GetBasketDetailAsync(int basketId)
    {
        const string sql = @"
            SELECT BASKET_ID, I_PO_NO, C_SIZE, C_STYLE, C_KEYINLOC,
                   I_PARTS_NO, N_PARTS_NO, C_QTY, QTY_RECEIVE, IS_DONE, DATE_DONE, WORKER_ID,
                   OUT_TO, IS_OUT
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
        if (await IsPartLockedAsync(basketId, partsNo))
        {
            return (false, "Part này đã Out rồi — không thể sửa số lượng nhận nữa.");
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
        if (await IsPartLockedAsync(basketId, partsNo))
        {
            return (false, "Part này đã Out rồi — không thể sửa số lượng nhận nữa.");
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

    // Khoá sửa IN theo TỪNG PART: 1 part chỉ bị khoá khi CHÍNH part đó đã Out (IS_OUT='Y'), hoặc
    // khi cả basket đã Out xong hết (HEADER.IS_OUT='Y'). KHÔNG khoá cả basket chỉ vì basket đang
    // "Out dở" (PROCESS_OUT='Y') — vì quy trình thật cho phép 1 phần part đã Out cho nhà A trong
    // khi các part còn lại vẫn đang được nhận hàng để chuẩn bị Out cho nhà B sau.
    private async Task<bool> IsPartLockedAsync(int basketId, string partsNo)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM MES.TRTB_M_COMPSTT_SET_HEADER
                 WHERE BASKET_ID = :basketId AND IS_OUT = 'Y') AS HEADER_OUT_CNT,
                (SELECT COUNT(*) FROM MES.TRTB_M_COMPSTT_SET_DETAIL
                 WHERE BASKET_ID = :basketId2 AND I_PARTS_NO = :partsNo AND IS_OUT = 'Y') AS PART_OUT_CNT
            FROM DUAL";

        var results = await _db.ExecuteQueryAsync(sql,
            r => Convert.ToInt32(r["HEADER_OUT_CNT"]) + Convert.ToInt32(r["PART_OUT_CNT"]),
            new OracleParameter("basketId", basketId),
            new OracleParameter("basketId2", basketId),
            new OracleParameter("partsNo", partsNo));
        return results.FirstOrDefault() > 0;
    }

    // Out theo từng chi tiết (part) — thay cho Out nguyên cả thẻ trước đây. 1 part chỉ Out được
    // khi đã nhận đủ (IS_DONE='Y') và chưa Out lần nào. Sau khi Out xong nhóm part được chọn:
    // luôn đánh dấu PROCESS_OUT='Y' cho cả basket (khoá sửa IN từ đây), và nếu KHÔNG còn part nào
    // trong basket chưa Out thì đánh dấu IS_OUT='Y'/DATE_OUT=SYSDATE cho cả basket (Out xong hết).
    public async Task<(bool Success, string? Message)> MarkPartsOutAsync(int basketId, List<string> partsNoList, string outTo, string workerId)
    {
        if (partsNoList == null || partsNoList.Count == 0)
        {
            return (false, "Chưa chọn part nào để Out.");
        }
        if (string.IsNullOrWhiteSpace(outTo))
        {
            return (false, "Chưa chọn nhà/line để Out.");
        }

        const string checkSql = @"
            SELECT I_PARTS_NO, IS_DONE, IS_OUT
            FROM MES.TRTB_M_COMPSTT_SET_DETAIL
            WHERE BASKET_ID = :basketId";

        var rows = await _db.ExecuteQueryAsync(checkSql,
            r => (PartsNo: r["I_PARTS_NO"] as string ?? string.Empty,
                  IsDone: r["IS_DONE"] as string,
                  IsOut: r["IS_OUT"] as string),
            new OracleParameter("basketId", basketId));

        if (rows.Count == 0)
        {
            return (false, "Không tìm thấy basket này.");
        }

        var byPartsNo = rows.ToDictionary(r => r.PartsNo, StringComparer.OrdinalIgnoreCase);
        foreach (var partsNo in partsNoList)
        {
            if (!byPartsNo.TryGetValue(partsNo, out var row))
            {
                return (false, $"Không tìm thấy part {partsNo} trong basket này.");
            }
            if (!string.Equals(row.IsDone, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Part {partsNo} chưa nhận đủ — chưa thể Out.");
            }
            if (string.Equals(row.IsOut, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Part {partsNo} đã Out rồi — không thể Out thêm lần nữa.");
            }
        }

        // WHERE thêm NVL(IS_OUT,'N')<>'Y' làm lưới an toàn cho race condition, giống cách đã làm
        // với Out theo cả thẻ trước đây — chỉ update đúng những part thật sự chưa Out.
        var inClauseParams = partsNoList.Select((p, i) => $":p{i}").ToList();
        var updateSql = $@"
            UPDATE MES.TRTB_M_COMPSTT_SET_DETAIL
            SET IS_OUT = 'Y', OUT_TO = :outTo
            WHERE BASKET_ID = :basketId AND NVL(IS_OUT,'N') <> 'Y'
              AND I_PARTS_NO IN ({string.Join(",", inClauseParams)})";

        var updateParams = new List<OracleParameter>
        {
            new("outTo", outTo),
            new("basketId", basketId)
        };
        for (int i = 0; i < partsNoList.Count; i++)
        {
            updateParams.Add(new OracleParameter($"p{i}", partsNoList[i]));
        }

        var affected = await _db.ExecuteNonQueryAsync(updateSql, updateParams.ToArray());
        if (affected != partsNoList.Count)
        {
            return (false, "Có part đã bị Out bởi thao tác khác cùng lúc — vui lòng tải lại và thử lại.");
        }

        // Luôn khoá sửa IN cho cả basket ngay khi bắt đầu Out (dù mới 1 part).
        await _db.ExecuteNonQueryAsync(
            "UPDATE MES.TRTB_M_COMPSTT_SET_HEADER SET PROCESS_OUT = 'Y' WHERE BASKET_ID = :basketId",
            new OracleParameter("basketId", basketId));

        // Nếu không còn part nào chưa Out trong basket — đánh dấu cả basket đã Out xong hết.
        const string remainingSql = @"
            SELECT COUNT(*) AS CNT
            FROM MES.TRTB_M_COMPSTT_SET_DETAIL
            WHERE BASKET_ID = :basketId AND NVL(IS_OUT,'N') <> 'Y'";
        var remaining = await _db.ExecuteQueryAsync(remainingSql, r => Convert.ToInt32(r["CNT"]),
            new OracleParameter("basketId", basketId));

        if (remaining.FirstOrDefault() == 0)
        {
            await _db.ExecuteNonQueryAsync(
                "UPDATE MES.TRTB_M_COMPSTT_SET_HEADER SET IS_OUT = 'Y', DATE_OUT = SYSDATE WHERE BASKET_ID = :basketId",
                new OracleParameter("basketId", basketId));
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
        SetQty = r["SET_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["SET_QTY"]),
        IsIn = r["IS_IN"] as string,
        InDate = r["IN_DATE"] == DBNull.Value ? null : Convert.ToDateTime(r["IN_DATE"]),
        IsCompleteSet = r["IS_COMPLETE_SET"] as string,
        IsOut = r["IS_OUT"] as string,
        DateOut = r["DATE_OUT"] == DBNull.Value ? null : Convert.ToDateTime(r["DATE_OUT"]),
        ProcessOut = r["PROCESS_OUT"] as string,
        PartsOut = r["PARTS_OUT"] == DBNull.Value ? 0 : Convert.ToInt32(r["PARTS_OUT"]),
        PartsTotal = r["PARTS_TOTAL"] == DBNull.Value ? 0 : Convert.ToInt32(r["PARTS_TOTAL"])
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
        WorkerId = r["WORKER_ID"] as string,
        OutTo = r["OUT_TO"] as string,
        IsOut = r["IS_OUT"] as string
    };
}
