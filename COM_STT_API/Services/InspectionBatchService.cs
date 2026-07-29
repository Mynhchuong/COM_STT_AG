using System.Data;
using COM_STT_API.Data;
using COM_STT_API.Models;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Services;

public class InspectionBatchService
{
    private readonly AgmesOracleService _db;

    public InspectionBatchService(AgmesOracleService db)
    {
        _db = db;
    }

    public async Task<List<InspectionBatch>> GetAllBatchesAsync(string? date, string? status)
    {
        var conditions = new List<string>();
        var parameters = new List<OracleParameter>();

        if (!string.IsNullOrEmpty(date))
        {
            conditions.Add("SUBSTR(B.D_GATHER, 1, 8) = :targetDate");
            parameters.Add(new OracleParameter("targetDate", date));
        }

        var statusFilter = (status ?? "N").ToUpper();
        if (statusFilter != "ALL")
        {
            conditions.Add("B.STATUS = :statusFilter");
            parameters.Add(new OracleParameter("statusFilter", statusFilter));
        }

        var whereClause = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

        var sql = $@"
            SELECT
                B.SEQ, B.LINE_CODE, B.MACHINE_CODE, B.IP_UPLOAD, B.PROD_TYPE, B.C_SHIFT, B.PLANT, B.D_GATHER, B.STATUS,
                O.ORDERS,
                S.STYLES,
                SZ.SIZES,
                P.PCARD_NAMES,
                Q.TOTAL_QTY
            FROM MES.TRTB_PRSTT_INSPECTION_SEQ B
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_ORDER || ', ') ORDER BY C_ORDER).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS ORDERS
                FROM (
                    SELECT DISTINCT SEQ, C_ORDER
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_ORDER IS NOT NULL
                )
                GROUP BY SEQ
            ) O ON O.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_STYLE || ', ') ORDER BY C_STYLE).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS STYLES
                FROM (
                    SELECT DISTINCT SEQ, C_STYLE
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_STYLE IS NOT NULL
                )
                GROUP BY SEQ
            ) S ON S.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_SIZE || ', ') ORDER BY C_SIZE).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS SIZES
                FROM (
                    SELECT DISTINCT SEQ, C_SIZE
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_SIZE IS NOT NULL
                )
                GROUP BY SEQ
            ) SZ ON SZ.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, PCARD_NO || ', ') ORDER BY PCARD_NO).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS PCARD_NAMES
                FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                GROUP BY SEQ
            ) P ON P.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ, SUM(Q_QTY) AS TOTAL_QTY
                FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                GROUP BY SEQ
            ) Q ON Q.SEQ = B.SEQ
            {whereClause}
            ORDER BY B.D_GATHER DESC";

        var batches = await _db.ExecuteQueryAsync(sql, MapRow, parameters.ToArray());

        // Calculate OrderQty sum for each batch using DL_AGERP db link function
        var uniqueOrders = new HashSet<string>();
        foreach (var b in batches)
        {
            if (string.IsNullOrEmpty(b.Orders)) continue;
            var parts = b.Orders.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));
            foreach (var o in parts)
            {
                uniqueOrders.Add(o);
            }
        }

        // Gọi song song thay vì tuần tự — mỗi lần gọi tự mở connection riêng (xem
        // OracleServiceBase.ExecuteQueryAsync) nên an toàn để chạy đồng thời.
        var orderQtyTasks = uniqueOrders.Select(async order =>
        {
            try
            {
                var val = await _db.ExecuteQueryAsync(
                    "SELECT F_ORDER_NO_QTY@DL_AGERP(:orderCode) AS QTY FROM DUAL",
                    r => OracleServiceBase.ConvertToInt(r["QTY"]),
                    new OracleParameter("orderCode", order)
                );
                return (order, qty: val.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching order qty for {order}: {ex.Message}");
                return (order, qty: 0);
            }
        });

        var orderQtyResults = await Task.WhenAll(orderQtyTasks);
        var orderQtyMap = orderQtyResults.ToDictionary(x => x.order, x => x.qty);

        foreach (var b in batches)
        {
            if (string.IsNullOrEmpty(b.Orders)) continue;
            var parts = b.Orders.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));
            b.OrderQty = parts.Sum(o => orderQtyMap.TryGetValue(o, out var q) ? q : 0);
        }

        return batches;
    }

    public async Task<InspectionBatch?> GetBatchAsync(int seq)
    {
        const string sql = "SELECT * FROM MES.TRTB_PRSTT_INSPECTION_SEQ WHERE SEQ = :SEQ";
        var results = await _db.ExecuteQueryAsync(sql, MapSimpleRow, new OracleParameter("SEQ", seq));
        return results.FirstOrDefault();
    }

    public async Task<int> CreateBatchAsync(InspectionBatch batch)
    {
        var seqResult = await _db.ExecuteQueryAsync(
            "SELECT SEQ_INSPECTION_BATCH.NEXTVAL AS SEQ FROM DUAL",
            r => Convert.ToInt32(r["SEQ"])
        );
        var seq = seqResult.First();

        var dGather = string.IsNullOrEmpty(batch.DGather) 
            ? DateTime.Now.ToString("yyyyMMddHHmmss") 
            : batch.DGather;

        const string sql = @"
            INSERT INTO MES.TRTB_PRSTT_INSPECTION_SEQ
                (SEQ, LINE_CODE, MACHINE_CODE, IP_UPLOAD, PROD_TYPE, C_SHIFT, PLANT, D_GATHER, STATUS)
            VALUES
                (:SEQ, :LINE_CODE, :MACHINE_CODE, :IP_UPLOAD, :PROD_TYPE, :C_SHIFT, :PLANT, :D_GATHER, 'N')";

        await _db.ExecuteNonQueryAsync(sql,
            new OracleParameter("SEQ", seq),
            new OracleParameter("LINE_CODE", batch.LineCode ?? (object)DBNull.Value),
            new OracleParameter("MACHINE_CODE", batch.MachineCode ?? (object)DBNull.Value),
            new OracleParameter("IP_UPLOAD", batch.IpUpload ?? (object)DBNull.Value),
            new OracleParameter("PROD_TYPE", batch.ProdType),
            new OracleParameter("C_SHIFT", batch.CShift ?? (object)DBNull.Value),
            new OracleParameter("PLANT", batch.Plant ?? (object)DBNull.Value),
            new OracleParameter("D_GATHER", dGather));

        return seq;
    }

    public async Task<bool> UpdateBatchAsync(int seq, string? lineCode, string? machineCode, string? shift, string? plant)
    {
        const string sql = @"
            UPDATE MES.TRTB_PRSTT_INSPECTION_SEQ
            SET LINE_CODE = :LINE_CODE,
                MACHINE_CODE = :MACHINE_CODE,
                C_SHIFT = :C_SHIFT,
                PLANT = :PLANT
            WHERE SEQ = :SEQ";

        var rowsAffected = await _db.ExecuteNonQueryAsync(sql,
            new OracleParameter("LINE_CODE", lineCode ?? (object)DBNull.Value),
            new OracleParameter("MACHINE_CODE", machineCode ?? (object)DBNull.Value),
            new OracleParameter("C_SHIFT", shift ?? (object)DBNull.Value),
            new OracleParameter("PLANT", plant ?? (object)DBNull.Value),
            new OracleParameter("SEQ", seq));

        return rowsAffected > 0;
    }

    public async Task<bool> FinishBatchAsync(int seq)
    {
        const string sql = "UPDATE MES.TRTB_PRSTT_INSPECTION_SEQ SET STATUS = 'Y' WHERE SEQ = :SEQ";
        var rowsAffected = await _db.ExecuteNonQueryAsync(sql, new OracleParameter("SEQ", seq));
        return rowsAffected > 0;
    }

    public async Task<List<InspectionBatch>> GetCompletedBatchesAsync(string? date)
    {
        var conditions = new List<string> { "B.STATUS = 'Y'" };
        var parameters = new List<OracleParameter>();

        if (!string.IsNullOrEmpty(date))
        {
            conditions.Add("SUBSTR(B.D_GATHER, 1, 8) = :targetDate");
            parameters.Add(new OracleParameter("targetDate", date));
        }

        var sql = $@"
            SELECT
                B.SEQ, B.LINE_CODE, B.MACHINE_CODE, B.IP_UPLOAD, B.PROD_TYPE, B.C_SHIFT, B.PLANT, B.D_GATHER, B.STATUS,
                O.ORDERS,
                S.STYLES,
                SZ.SIZES,
                P.PCARD_NAMES,
                Q.TOTAL_QTY
            FROM MES.TRTB_PRSTT_INSPECTION_SEQ B
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_ORDER || ', ') ORDER BY C_ORDER).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS ORDERS
                FROM (
                    SELECT DISTINCT SEQ, C_ORDER
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_ORDER IS NOT NULL
                )
                GROUP BY SEQ
            ) O ON O.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_STYLE || ', ') ORDER BY C_STYLE).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS STYLES
                FROM (
                    SELECT DISTINCT SEQ, C_STYLE
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_STYLE IS NOT NULL
                )
                GROUP BY SEQ
            ) S ON S.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, C_SIZE || ', ') ORDER BY C_SIZE).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS SIZES
                FROM (
                    SELECT DISTINCT SEQ, C_SIZE
                    FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                    WHERE C_SIZE IS NOT NULL
                )
                GROUP BY SEQ
            ) SZ ON SZ.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ,
                    RTRIM(
                        XMLAGG(XMLELEMENT(E, PCARD_NO || ', ') ORDER BY PCARD_NO).EXTRACT('//text()').GETSTRINGVAL(),
                        ', '
                    ) AS PCARD_NAMES
                FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                GROUP BY SEQ
            ) P ON P.SEQ = B.SEQ
            LEFT JOIN (
                SELECT SEQ, SUM(Q_QTY) AS TOTAL_QTY
                FROM MES.TRTB_PRSTT_INSPECTION_HEAD_NEW
                GROUP BY SEQ
            ) Q ON Q.SEQ = B.SEQ
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY B.D_GATHER DESC";

        return await _db.ExecuteQueryAsync(sql, MapRow, parameters.ToArray());
    }

    private static InspectionBatch MapRow(OracleDataReader r) => new()
    {
        Seq = Convert.ToInt32(r["SEQ"]),
        LineCode = r["LINE_CODE"] as string,
        MachineCode = r["MACHINE_CODE"] as string,
        IpUpload = r["IP_UPLOAD"] as string,
        ProdType = Convert.ToInt32(r["PROD_TYPE"]),
        CShift = r["C_SHIFT"] as string,
        Plant = r["PLANT"] as string,
        DGather = r["D_GATHER"] as string,
        Status = r["STATUS"] as string ?? "N",
        Orders = r["ORDERS"] as string,
        Styles = r["STYLES"] as string,
        Sizes = r["SIZES"] as string,
        PcardNames = r["PCARD_NAMES"] as string,
        TotalQty = r["TOTAL_QTY"] == DBNull.Value ? 0 : Convert.ToInt32(r["TOTAL_QTY"])
    };

    private static InspectionBatch MapSimpleRow(OracleDataReader r) => new()
    {
        Seq = Convert.ToInt32(r["SEQ"]),
        LineCode = r["LINE_CODE"] as string,
        MachineCode = r["MACHINE_CODE"] as string,
        IpUpload = r["IP_UPLOAD"] as string,
        ProdType = Convert.ToInt32(r["PROD_TYPE"]),
        CShift = r["C_SHIFT"] as string,
        Plant = r["PLANT"] as string,
        DGather = r["D_GATHER"] as string,
        Status = r["STATUS"] as string ?? "N"
    };
}
