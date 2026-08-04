using System.Text.Json.Serialization;

namespace COM_STT_API.Models;

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
    public string? CKeyinloc { get; set; } = "CUT_2";

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

    [JsonPropertyName("Q_PLAN")]
    public int QPlan { get; set; }

    [JsonPropertyName("I_IP_NO")]
    public string? IIpNo { get; set; }

    // Mã PCard vừa quét — dùng để gọi MES.PROC_CREATE_COMPSTT_SET (không phải cột của TRTB_M_KEYIN_YIELD).
    [JsonPropertyName("PCARD_NO")]
    public string? PcardNo { get; set; }

    // Line nhận hàng khi Set Out (VD "B-01".."B-30", "E-01".."E-30") — lưu vào cột LINEOUT của
    // TRTB_M_COMPSTT_SET_HEADER, chỉ dùng khi C_ACTION='OUTPUT'.
    [JsonPropertyName("LINE_OUT")]
    public string? LineOut { get; set; }
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
