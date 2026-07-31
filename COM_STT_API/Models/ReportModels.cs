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

// 1 dòng trong báo cáo theo PO (RW 1-4: TTL_PLAN / TOTAL SCAN QTY / RECEIVED_QTY / BALANCE_QTY),
// dựa trên MES.V_COMPSTT_PO_REPORT — Part No cố định '190' đã bake sẵn trong view.
public class CompSttPoReportRow
{
    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("RW")]
    public int Rw { get; set; }

    [JsonPropertyName("LINE_TYPE")]
    public string? LineType { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    [JsonPropertyName("LINE_NO")]
    public int LineNo { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}
