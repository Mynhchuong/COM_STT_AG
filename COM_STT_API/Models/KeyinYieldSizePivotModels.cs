using System.Text.Json.Serialization;

namespace COM_STT_API.Models;

public class KeyinYieldSizePivotRow
{
    [JsonPropertyName("C_PO_NUM")]
    public string? CPoNum { get; set; }

    [JsonPropertyName("C_ORD_NO")]
    public string? COrdNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_WIDTH")]
    public string? CWidth { get; set; }

    // INPUT / OUTPUT / BALANCE (BALANCE = INPUT - OUTPUT theo từng size)
    [JsonPropertyName("C_ACTION")]
    public string? CAction { get; set; }

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinLoc { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}

public class KeyinPartYieldStatusRow
{
    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    // "1"=TOTAL (kế hoạch), "2"=DONE (đã quét), "3"=REMAIN (còn lại = kế hoạch - đã quét)
    [JsonPropertyName("ROW_TYPE")]
    public string? RowType { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}
