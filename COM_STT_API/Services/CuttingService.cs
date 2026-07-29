using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class CuttingService
{
    private readonly AgmesOracleService _db;

    public CuttingService(AgmesOracleService db)
    {
        _db = db;
    }

    // Bước 1 của Check-in Chuyền cắt: quét PCARD, tra thông tin đợt cắt (C_ORD_OP = 'UCT' cố định).
    public async Task<CuttingCardInfo?> GetCardInfoAsync(string cardNo)
    {
        const string sql = @"
            SELECT I_CARD_NO, STATUS, Q_QTY, C_ORD_OP, C_LINE, I_PARTS_NO, C_STYLE, F_CLOSE, Q_PLAN, Q_GATHER
            FROM MES.V_CUTTING_CARD_INFO
            WHERE I_CARD_NO = :cardNo
              AND C_ORD_OP = 'UCT'";

        var results = await _db.ExecuteQueryAsync(sql, MapRow, new OracleParameter("cardNo", cardNo));
        return results.FirstOrDefault();
    }

    private static CuttingCardInfo MapRow(OracleDataReader r) => new()
    {
        ICardNo = r["I_CARD_NO"] as string ?? string.Empty,
        Status = r["STATUS"] as string,
        QQty = r["Q_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_QTY"]),
        COrdOp = r["C_ORD_OP"] as string,
        CLine = r["C_LINE"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        CStyle = r["C_STYLE"] as string,
        FClose = r["F_CLOSE"] as string,
        QPlan = r["Q_PLAN"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_PLAN"]),
        QGather = r["Q_GATHER"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_GATHER"])
    };
}
