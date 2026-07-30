using System.Text.Json.Serialization;

namespace COM_STT_API.Models;

public class InspectionBatch
{
    [JsonPropertyName("SEQ")]
    public int Seq { get; set; }

    [JsonPropertyName("LINE_CODE")]
    public string? LineCode { get; set; }

    [JsonPropertyName("MACHINE_CODE")]
    public string? MachineCode { get; set; }

    [JsonPropertyName("IP_UPLOAD")]
    public string? IpUpload { get; set; }

    [JsonPropertyName("PROD_TYPE")]
    public int ProdType { get; set; } = 1;

    [JsonPropertyName("C_SHIFT")]
    public string? CShift { get; set; }

    [JsonPropertyName("PLANT")]
    public string? Plant { get; set; }

    [JsonPropertyName("D_GATHER")]
    public string? DGather { get; set; }

    [JsonPropertyName("STATUS")]
    public string Status { get; set; } = "N";

    // Extra fields populated during joins
    [JsonPropertyName("ORDERS")]
    public string? Orders { get; set; }

    [JsonPropertyName("STYLES")]
    public string? Styles { get; set; }

    [JsonPropertyName("SIZES")]
    public string? Sizes { get; set; }

    [JsonPropertyName("PCARD_NAMES")]
    public string? PcardNames { get; set; }

    [JsonPropertyName("TOTAL_QTY")]
    public int TotalQty { get; set; }

    [JsonPropertyName("ORDER_QTY")]
    public int OrderQty { get; set; }
}

public class InspectionHead
{
    [JsonPropertyName("PCARD_NO")]
    public string PcardNo { get; set; } = string.Empty;

    [JsonPropertyName("C_GROUP_SUM")]
    public string? CGroupSum { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_ORDER")]
    public string? COrder { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("SEQ")]
    public int Seq { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }
}

public class ProdPlanInfo
{
    [JsonPropertyName("I_CARD_NO")]
    public string ICardNo { get; set; } = string.Empty;

    [JsonPropertyName("C_PARTS")]
    public string? CParts { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }

    [JsonPropertyName("MES_GROUP_SUM")]
    public string? MesGroupSum { get; set; }

    [JsonPropertyName("C_WIDTH")]
    public string? CWidth { get; set; }

    [JsonPropertyName("C_PO_NUM")]
    public string? CPoNum { get; set; }

    [JsonPropertyName("STATUS")]
    public string? Status { get; set; }

    [JsonPropertyName("C_ORD_OP")]
    public string? COrdOp { get; set; }

    [JsonPropertyName("C_LINE")]
    public string? CLine { get; set; }

    [JsonPropertyName("F_CLOSE")]
    public string? FClose { get; set; }

    [JsonPropertyName("Q_PLAN")]
    public int QPlan { get; set; }

    [JsonPropertyName("Q_GATHER")]
    public int QGather { get; set; }
}

public class KeyinYieldItemDto
{
    [JsonPropertyName("D_GATHER")]
    public string? DGather { get; set; }

    [JsonPropertyName("C_ACTION")]
    public string? CAction { get; set; } = "INPUT";

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinloc { get; set; } = "SET";

    [JsonPropertyName("C_KEYINPART")]
    public string? CKeyinpart { get; set; }

    [JsonPropertyName("C_PO_NUM")]
    public string? CPoNum { get; set; }

    [JsonPropertyName("C_ORD_NO")]
    public string? COrdNo { get; set; }

    [JsonPropertyName("C_WIDTH")]
    public string? CWidth { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("C_WORK_LINE")]
    public string? CWorkLine { get; set; }

    [JsonPropertyName("C_WORKER")]
    public string? CWorker { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }

    [JsonPropertyName("I_IP_NO")]
    public string? IIpNo { get; set; }
}

public class KeyinYieldLogItem
{
    [JsonPropertyName("D_GATHER")]
    public string DGather { get; set; } = string.Empty;

    [JsonPropertyName("C_ACTION")]
    public string? CAction { get; set; }

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinloc { get; set; }

    [JsonPropertyName("C_KEYINPART")]
    public string? CKeyinpart { get; set; }

    [JsonPropertyName("C_PO_NUM")]
    public string? CPoNum { get; set; }

    [JsonPropertyName("C_ORD_NO")]
    public string? COrdNo { get; set; }

    [JsonPropertyName("C_WIDTH")]
    public string? CWidth { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("C_WORK_LINE")]
    public string? CWorkLine { get; set; }

    [JsonPropertyName("C_WORKER")]
    public string? CWorker { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }

    [JsonPropertyName("I_IP_NO")]
    public string? IIpNo { get; set; }
}
