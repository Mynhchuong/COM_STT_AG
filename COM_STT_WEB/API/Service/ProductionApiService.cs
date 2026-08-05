using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace COM_STT_WEB.API.Service;

public class ProductionApiService
{
    private readonly HttpClient _httpClient;

    public ProductionApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PcardPlanInfoResponse?> GetProdPlanInfoAsync(string cardNo)
    {
        var response = await _httpClient.GetAsync($"api/prod-plan/{cardNo}");
        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadFromJsonAsync<ApiResponse<PcardPlanInfoResponse>>();
            return res?.Data;
        }
        return null;
    }

    public async Task<SaveYieldBatchResult> SaveYieldBatchAsync(List<KeyinYieldItemModel> items)
    {
        var response = await _httpClient.PostAsJsonAsync("api/yield/save-batch", items);
        var body = await response.Content.ReadFromJsonAsync<SaveYieldBatchApiResponse>();

        return new SaveYieldBatchResult
        {
            Success = response.IsSuccessStatusCode && (body?.Success ?? false),
            Count = body?.SavedCount ?? 0,
            Message = body?.Message,
            PartYieldMessage = body?.PartYieldMessage,
            BasketId = body?.BasketId,
            OutputErrors = body?.OutputErrors
        };
    }

    public async Task<int?> GetBasketIdByCardNoAsync(string cardNo)
    {
        var response = await _httpClient.GetAsync($"api/compstt-set/basket-by-card?cardNo={Uri.EscapeDataString(cardNo)}");
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadFromJsonAsync<BasketIdApiResponse>();
        return body?.BasketId;
    }

    public async Task<List<CompSttSetHeaderRowModel>> GetBasketHeaderAsync(int basketId)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CompSttSetHeaderRowModel>>>(
            $"api/compstt-set/header?basketId={basketId}");
        return response?.Data ?? new List<CompSttSetHeaderRowModel>();
    }

    public async Task<List<CompSttSetHeaderRowModel>> GetCardsByPoAsync(string po)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CompSttSetHeaderRowModel>>>(
            $"api/compstt-set/cards-by-po?po={Uri.EscapeDataString(po)}");
        return response?.Data ?? new List<CompSttSetHeaderRowModel>();
    }

    public async Task<List<CompSttSetDetailRowModel>> GetBasketDetailAsync(int basketId)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CompSttSetDetailRowModel>>>(
            $"api/compstt-set/detail?basketId={basketId}");
        return response?.Data ?? new List<CompSttSetDetailRowModel>();
    }

    public async Task<PartYieldUpdateResult> MarkBasketDetailDoneAsync(int basketId, string partsNo, string workerId)
    {
        var url = $"api/compstt-set/mark-done?basketId={basketId}&partsNo={Uri.EscapeDataString(partsNo)}&workerId={Uri.EscapeDataString(workerId)}";
        var response = await _httpClient.PostAsync(url, null);
        var body = await response.Content.ReadFromJsonAsync<PartYieldUpdateApiResponse>();
        return new PartYieldUpdateResult { Success = body?.Success ?? false, Message = body?.Message };
    }

    public async Task<PartYieldUpdateResult> UpdateBasketDetailQtyAsync(int basketId, string partsNo, int qty, string workerId)
    {
        var url = $"api/compstt-set/update-qty?basketId={basketId}&partsNo={Uri.EscapeDataString(partsNo)}&qty={qty}&workerId={Uri.EscapeDataString(workerId)}";
        var response = await _httpClient.PostAsync(url, null);
        var body = await response.Content.ReadFromJsonAsync<PartYieldUpdateApiResponse>();
        return new PartYieldUpdateResult { Success = body?.Success ?? false, Message = body?.Message };
    }

    public async Task<List<KeyinYieldLogItemModel>> GetTodayYieldAsync(string? worker, string? date = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(worker)) query.Add($"worker={Uri.EscapeDataString(worker)}");
        if (!string.IsNullOrWhiteSpace(date)) query.Add($"date={Uri.EscapeDataString(date)}");
        var url = "api/yield/today" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<KeyinYieldLogItemModel>>>(url);
        return response?.Data ?? new List<KeyinYieldLogItemModel>();
    }

    public async Task<bool> DeleteYieldAsync(string dGather)
    {
        var response = await _httpClient.DeleteAsync($"api/yield/{Uri.EscapeDataString(dGather)}");
        return response.IsSuccessStatusCode;
    }
}

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class PcardPlanInfoResponse
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

public class KeyinYieldItemModel
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

    [JsonPropertyName("PCARD_NO")]
    public string? PcardNo { get; set; }

    [JsonPropertyName("LINE_OUT")]
    public string? LineOut { get; set; }
}

public class KeyinYieldLogItemModel
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

public class SaveYieldBatchApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("savedCount")]
    public int SavedCount { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("partYieldMessage")]
    public string? PartYieldMessage { get; set; }

    [JsonPropertyName("basketId")]
    public int? BasketId { get; set; }

    [JsonPropertyName("outputErrors")]
    public List<string>? OutputErrors { get; set; }
}

public class SaveYieldBatchResult
{
    public bool Success { get; set; }
    public int Count { get; set; }
    public string? Message { get; set; }
    public string? PartYieldMessage { get; set; }
    public int? BasketId { get; set; }
    public List<string>? OutputErrors { get; set; }
}

public class PartYieldUpdateApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class PartYieldUpdateResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class BasketIdApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("basketId")]
    public int? BasketId { get; set; }
}

public class CompSttSetHeaderRowModel
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

    [JsonPropertyName("SET_QTY")]
    public int SetQty { get; set; }

    [JsonPropertyName("IS_IN")]
    public string? IsIn { get; set; }

    [JsonPropertyName("IN_DATE")]
    public DateTime? InDate { get; set; }

    [JsonPropertyName("IS_COMPLETE_SET")]
    public string? IsCompleteSet { get; set; }

    [JsonPropertyName("IS_OUT")]
    public string? IsOut { get; set; }

    [JsonPropertyName("DATE_OUT")]
    public DateTime? DateOut { get; set; }

    [JsonPropertyName("LINEOUT")]
    public string? LineOut { get; set; }
}

public class CompSttSetDetailRowModel
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
