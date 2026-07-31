using System.Text.Json.Serialization;

namespace COM_STT_API.Models;

// 1 dòng / (PO, Part, LineType) trong báo cáo Kế hoạch/Đã SET/Còn lại theo size.
public class CompSttSetReportRow
{
    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    // "1"=Q_PLAN (kế hoạch), "2"=SET_QTY (đã tạo SET), "3"=BALANCE_QTY (còn lại)
    [JsonPropertyName("LINE_NO")]
    public int LineNo { get; set; }

    [JsonPropertyName("LINE_TYPE")]
    public string? LineType { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}
