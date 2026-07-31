using System.Text.Json.Serialization;

namespace COM_STT_WEB.API.Service;

public class ReportApiService
{
    private readonly HttpClient _httpClient;

    public ReportApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<CompSttSetReportRowModel>> GetCompSttSetReportAsync(string fromDate, string toDate, string? po, string? part)
    {
        var query = new List<string> { $"fromDate={Uri.EscapeDataString(fromDate)}", $"toDate={Uri.EscapeDataString(toDate)}" };
        if (!string.IsNullOrWhiteSpace(po)) query.Add($"po={Uri.EscapeDataString(po)}");
        if (!string.IsNullOrWhiteSpace(part)) query.Add($"part={Uri.EscapeDataString(part)}");

        var response = await _httpClient.GetFromJsonAsync<ReportApiResponse<List<CompSttSetReportRowModel>>>(
            "api/report/compstt-set?" + string.Join("&", query));
        return response?.Data ?? new List<CompSttSetReportRowModel>();
    }
}

public class ReportApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class CompSttSetReportRowModel
{
    [JsonPropertyName("I_PO_NO")]
    public string? IPoNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("I_PARTS_NO")]
    public string? IPartsNo { get; set; }

    [JsonPropertyName("N_PARTS_NO")]
    public string? NPartsNo { get; set; }

    [JsonPropertyName("LINE_NO")]
    public int LineNo { get; set; }

    [JsonPropertyName("LINE_TYPE")]
    public string? LineType { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}
