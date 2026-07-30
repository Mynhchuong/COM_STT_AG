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
        // C_ORD_OP = 'UCT' cố định: chỉ lấy PCard thuộc công đoạn cắt.
        // C_WIDTH/C_PO_NUM lấy từ oe_order_headers_all@inf_m_e (attribute8/attribute15) — theo xác nhận nghiệp vụ.
        const string sql = @"
            SELECT CARD.I_CARD_NO, CARD.C_CARD_STATUS AS STATUS, CARD.Q_QTY,
                   PLAN.C_ORD_OP, PLAN.C_LINE, PLAN.I_PARTS_NO, PLAN.C_STYLE, PLAN.C_SIZE,
                   PLAN.C_PARTS, PLAN.F_CLOSE, PLAN.I_PO_NO, PLAN.Q_PLAN, PLAN.Q_GATHER,
                   ORD.attribute8  AS C_WIDTH,
                   ORD.attribute15 AS C_PO_NUM,
                   MDL.MES_GROUP_SUM
            FROM MES.TRTB_M_CARD CARD
            JOIN MES.TRTB_M_PROD_PLAN PLAN ON CARD.C_JOBORDER_NO = PLAN.C_JOBORDER_NO
            LEFT JOIN oe_order_headers_all@inf_m_e ORD ON PLAN.I_PO_NO = TO_CHAR(ORD.order_number)
            LEFT JOIN MES.MES_MODEL@DL_AGERP MDL ON PLAN.C_STYLE = MDL.MES_STYLE_NO
            WHERE CARD.I_CARD_NO = :cardNo
              AND PLAN.C_ORD_OP = 'UCT'";

        var results = await _db.ExecuteQueryAsync(sql, MapRow, new OracleParameter("cardNo", cardNo));
        return results.FirstOrDefault();
    }

    private static ProdPlanInfo MapRow(OracleDataReader r) => new()
    {
        ICardNo = r["I_CARD_NO"] as string ?? string.Empty,
        Status = r["STATUS"] as string,
        CParts = r["C_PARTS"] as string,
        CStyle = r["C_STYLE"] as string,
        CSize = r["C_SIZE"] as string,
        IPoNo = r["I_PO_NO"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        QQty = r["Q_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_QTY"]),
        MesGroupSum = r["MES_GROUP_SUM"] as string,
        CWidth = r["C_WIDTH"] as string,
        CPoNum = r["C_PO_NUM"] as string,
        COrdOp = r["C_ORD_OP"] as string,
        CLine = r["C_LINE"] as string,
        FClose = r["F_CLOSE"] as string,
        QPlan = r["Q_PLAN"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_PLAN"]),
        QGather = r["Q_GATHER"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_GATHER"])
    };
}
