using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class ProdPlanService
{
    private readonly AgmesOracleService _db;

    public ProdPlanService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<ProdPlanInfo?> GetProdPlanInfoAsync(string cardNo)
    {
        const string sql = @"
            SELECT B.I_CARD_NO, A.C_PARTS, A.C_STYLE, A.C_SIZE, A.I_PO_NO, A.I_PARTS_NO, B.Q_QTY, C.MES_GROUP_SUM
            FROM MES.TRTB_M_PROD_PLAN A, MES.TRTB_M_CARD B, MES.MES_MODEL@DL_AGERP C
            WHERE A.C_JOBORDER_NO = B.C_JOBORDER_NO
              AND A.C_STYLE = C.MES_STYLE_NO
              AND B.I_CARD_NO LIKE :cardNo
            ORDER BY B.I_CARD_NO";

        // Nếu 1 I_CARD_NO khớp nhiều dòng kế hoạch (nhiều C_PARTS cùng joborder),
        // FirstOrDefault() sau ORDER BY sẽ luôn trả về cùng 1 dòng ổn định giữa các lần gọi.
        var results = await _db.ExecuteQueryAsync(sql, MapRow, new OracleParameter("cardNo", cardNo));
        return results.FirstOrDefault();
    }

    private static ProdPlanInfo MapRow(OracleDataReader r) => new()
    {
        ICardNo = r["I_CARD_NO"] as string ?? string.Empty,
        CParts = r["C_PARTS"] as string,
        CStyle = r["C_STYLE"] as string,
        CSize = r["C_SIZE"] as string,
        IPoNo = r["I_PO_NO"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        QQty = r["Q_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_QTY"]),
        MesGroupSum = r["MES_GROUP_SUM"] as string
    };
}
