using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class InspectionHeadService
{
    private readonly AgmesOracleService _db;

    public InspectionHeadService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<List<InspectionHead>> GetBySeqAsync(int seq)
    {
        const string sql = "SELECT * FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW WHERE SEQ = :SEQ ORDER BY PCARD_NO";
        return await _db.ExecuteQueryAsync(sql, MapRow, new OracleParameter("SEQ", seq));
    }

    public async Task<InspectionHead?> GetByPcardNoAsync(string pcardNo)
    {
        const string sql = "SELECT * FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW WHERE PCARD_NO = :PCARD_NO";
        var results = await _db.ExecuteQueryAsync(sql, MapRow, new OracleParameter("PCARD_NO", pcardNo));
        return results.FirstOrDefault();
    }

    public async Task<bool> ExistsAsync(string pcardNo)
    {
        const string sql = "SELECT COUNT(*) AS CNT FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW WHERE PCARD_NO = :PCARD_NO";
        var results = await _db.ExecuteQueryAsync(sql, r => Convert.ToInt32(r["CNT"]), new OracleParameter("PCARD_NO", pcardNo));
        return results.FirstOrDefault() > 0;
    }

    public async Task<bool> CreateHeadAsync(InspectionHead head)
    {
        const string sql = @"
            INSERT INTO MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                (PCARD_NO, C_GROUP_SUM, C_STYLE, C_ORDER, I_PARTS_NO, C_SIZE, SEQ, Q_QTY)
            VALUES
                (:PCARD_NO, :C_GROUP_SUM, :C_STYLE, :C_ORDER, :I_PARTS_NO, :C_SIZE, :SEQ, :Q_QTY)";

        var rowsAffected = await _db.ExecuteNonQueryAsync(sql,
            new OracleParameter("PCARD_NO", head.PcardNo),
            new OracleParameter("C_GROUP_SUM", head.CGroupSum ?? (object)DBNull.Value),
            new OracleParameter("C_STYLE", head.CStyle ?? (object)DBNull.Value),
            new OracleParameter("C_ORDER", head.COrder ?? (object)DBNull.Value),
            new OracleParameter("I_PARTS_NO", head.IPartsNo ?? (object)DBNull.Value),
            new OracleParameter("C_SIZE", head.CSize ?? (object)DBNull.Value),
            new OracleParameter("SEQ", head.Seq),
            new OracleParameter("Q_QTY", head.QQty));

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteHeadAsync(string pcardNo)
    {
        const string sql = "DELETE FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW WHERE PCARD_NO = :PCARD_NO";
        var rowsAffected = await _db.ExecuteNonQueryAsync(sql, new OracleParameter("PCARD_NO", pcardNo));
        return rowsAffected > 0;
    }

    private static InspectionHead MapRow(OracleDataReader r) => new()
    {
        PcardNo = r["PCARD_NO"] as string ?? string.Empty,
        CGroupSum = r["C_GROUP_SUM"] as string,
        CStyle = r["C_STYLE"] as string,
        COrder = r["C_ORDER"] as string,
        IPartsNo = r["I_PARTS_NO"] as string,
        CSize = r["C_SIZE"] as string,
        Seq = Convert.ToInt32(r["SEQ"]),
        QQty = r["Q_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["Q_QTY"])
    };
}
