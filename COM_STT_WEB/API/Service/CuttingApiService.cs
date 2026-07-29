using System.Text.Json.Serialization;

namespace COM_STT_WEB.API.Service;

public class CuttingApiService
{
    private readonly HttpClient _httpClient;

    public CuttingApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CuttingCardInfoResponse?> GetCardInfoAsync(string cardNo)
    {
        var response = await _httpClient.GetAsync($"api/cutting/card/{cardNo}");
        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadFromJsonAsync<ApiResponse<CuttingCardInfoResponse>>();
            return res?.Data;
        }
        return null;
    }
}

public class CuttingCardInfoResponse
{
    [JsonPropertyName("I_CARD_NO")]
    public string ICardNo { get; set; } = string.Empty;

    [JsonPropertyName("STATUS")]
    public string? Status { get; set; }

    [JsonPropertyName("Q_QTY")]
    public int QQty { get; set; }

    [JsonPropertyName("C_ORD_OP")]
    public string? COrdOp { get; set; }

    [JsonPropertyName("C_LINE")]
    public string? CLine { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("F_CLOSE")]
    public string? FClose { get; set; }

    [JsonPropertyName("Q_PLAN")]
    public int QPlan { get; set; }

    [JsonPropertyName("Q_GATHER")]
    public int QGather { get; set; }
}
