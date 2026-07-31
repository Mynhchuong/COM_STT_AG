using System.Text.Json.Serialization;

namespace COM_STT_API.Models;

// 1 dòng / 1 PCard trong basket (basket có 1 hoặc 2 PCard)
public class CompSttSetHeaderRow
{
    [JsonPropertyName("BASKET_ID")]
    public int BasketId { get; set; }

    [JsonPropertyName("I_CARD_NO")]
    public string? ICardNo { get; set; }

    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinloc { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    [JsonPropertyName("Q_PLAN")]
    public int QPlan { get; set; }

    [JsonPropertyName("C_QTY")]
    public int CQty { get; set; }

    [JsonPropertyName("IS_IN")]
    public string? IsIn { get; set; }

    [JsonPropertyName("IN_DATE")]
    public DateTime? InDate { get; set; }

    [JsonPropertyName("IS_COMPLETE_SET")]
    public string? IsCompleteSet { get; set; }

    [JsonPropertyName("IS_OUT")]
    public string? IsOut { get; set; }
}

// 1 dòng / 1 part cần chuẩn bị trong basket (routing explosion)
public class CompSttSetDetailRow
{
    [JsonPropertyName("BASKET_ID")]
    public int BasketId { get; set; }

    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("C_SIZE")]
    public string? CSize { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinloc { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    [JsonPropertyName("C_QTY")]
    public int CQty { get; set; }

    [JsonPropertyName("QTY_RECEIVE")]
    public int QtyReceive { get; set; }

    [JsonPropertyName("IS_DONE")]
    public string? IsDone { get; set; }

    [JsonPropertyName("DATE_DONE")]
    public DateTime? DateDone { get; set; }

    [JsonPropertyName("WORKER_ID")]
    public string? WorkerId { get; set; }
}
