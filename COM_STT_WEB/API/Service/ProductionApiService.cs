using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using COM_STT_WEB.Models.Production;

namespace COM_STT_WEB.API.Service;

public class ProductionApiService
{
    private readonly HttpClient _httpClient;

    public ProductionApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InspectionBatchViewModel>> GetBatchesAsync(string? date, string? status)
    {
        var url = $"api/inspection-batch?status={status ?? "N"}";
        if (!string.IsNullOrEmpty(date))
        {
            url += $"&date={date}";
        }

        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<InspectionBatchViewModel>>>(url);
        return response?.Data ?? new List<InspectionBatchViewModel>();
    }

    public async Task<InspectionBatchViewModel?> GetBatchAsync(int seq)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<InspectionBatchViewModel>>($"api/inspection-batch/{seq}");
        return response?.Data;
    }

    public async Task<int> CreateBatchAsync(BatchRegistrationViewModel model, string ipAddress)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inspection-batch", new
        {
            LINE_CODE = model.LineCode,
            MACHINE_CODE = model.MachineCode,
            IP_UPLOAD = ipAddress,
            C_SHIFT = model.CShift,
            PLANT = model.Plant
        });

        if (response.IsSuccessStatusCode)
        {
            var res = await response.Content.ReadFromJsonAsync<CreateBatchResponse>();
            return res?.Seq ?? 0;
        }

        return 0;
    }

    public async Task<bool> UpdateBatchAsync(int seq, BatchRegistrationViewModel model)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/inspection-batch/{seq}", new
        {
            LINE_CODE = model.LineCode,
            MACHINE_CODE = model.MachineCode,
            C_SHIFT = model.CShift,
            PLANT = model.Plant
        });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> FinishBatchAsync(int seq)
    {
        var response = await _httpClient.PutAsync($"api/inspection-batch/{seq}/finish", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<PcardScanItemViewModel>> GetPcardsBySeqAsync(int seq)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<PcardScanItemViewModel>>>($"api/inspection-head?seq={seq}");
        return response?.Data ?? new List<PcardScanItemViewModel>();
    }

    public async Task<bool> ExistsPcardAsync(string pcardNo)
    {
        var response = await _httpClient.GetAsync($"api/inspection-head/{pcardNo}");
        return response.StatusCode == HttpStatusCode.OK;
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

    public async Task<bool> CreateInspectionHeadAsync(PcardScanItemViewModel item)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inspection-head", new
        {
            PCARD_NO = item.PcardNo,
            C_GROUP_SUM = item.CGroupSum,
            C_STYLE = item.CStyle,
            C_ORDER = item.COrder,
            I_PARTS_NO = item.IPartsNo,
            C_SIZE = item.CSize,
            SEQ = item.Seq,
            Q_QTY = item.QQty
        });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteInspectionHeadAsync(string pcardNo)
    {
        var response = await _httpClient.DeleteAsync($"api/inspection-head/{pcardNo}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SaveYieldBatchAsync(List<KeyinYieldItemModel> items)
    {
        var response = await _httpClient.PostAsJsonAsync("api/yield/save-batch", items);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<KeyinYieldLogItemModel>> GetTodayYieldAsync(string? worker)
    {
        var url = "api/yield/today";
        if (!string.IsNullOrWhiteSpace(worker))
        {
            url += $"?worker={Uri.EscapeDataString(worker)}";
        }
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<KeyinYieldLogItemModel>>>(url);
        return response?.Data ?? new List<KeyinYieldLogItemModel>();
    }

    public async Task<bool> DeleteYieldAsync(string dGather)
    {
        var response = await _httpClient.DeleteAsync($"api/yield/{Uri.EscapeDataString(dGather)}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<KeyinYieldSizePivotRowModel>> GetSizePivotByOrderAsync(string ordNo)
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<KeyinYieldSizePivotRowModel>>>(
            $"api/yield/size-pivot?ordNo={Uri.EscapeDataString(ordNo)}");
        return response?.Data ?? new List<KeyinYieldSizePivotRowModel>();
    }
}

public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class CreateBatchResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    // API serializes SEQ as lowercase "seq" (ASP.NET camelCase default)
    [JsonPropertyName("seq")]
    public int Seq { get; set; }
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
    public string? CKeyinloc { get; set; } = "SCREEN";

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

public class KeyinYieldSizePivotRowModel
{
    [JsonPropertyName("C_PO_NUM")]
    public string? CPoNum { get; set; }

    [JsonPropertyName("C_ORD_NO")]
    public string? COrdNo { get; set; }

    [JsonPropertyName("C_STYLE")]
    public string? CStyle { get; set; }

    [JsonPropertyName("C_WIDTH")]
    public string? CWidth { get; set; }

    [JsonPropertyName("C_ACTION")]
    public string? CAction { get; set; }

    [JsonPropertyName("C_KEYINLOC")]
    public string? CKeyinLoc { get; set; }

    [JsonPropertyName("SIZES")]
    public Dictionary<string, int> Sizes { get; set; } = new();
}
