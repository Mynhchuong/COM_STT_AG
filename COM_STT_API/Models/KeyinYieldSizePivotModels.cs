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
