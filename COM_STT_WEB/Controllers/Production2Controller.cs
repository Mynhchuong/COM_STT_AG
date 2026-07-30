using COM_STT_WEB.API.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class Production2Controller : Controller
{
    private readonly ProductionApiService _apiService;
    private readonly string _apiBaseUrl;

    public Production2Controller(ProductionApiService apiService, IConfiguration configuration)
    {
        _apiService = apiService;
        _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5052/";
    }

    private void SetApiBaseUrl() => ViewData["ApiBaseUrl"] = _apiBaseUrl;

    [HttpGet]
    public IActionResult Index()
    {
        SetApiBaseUrl();
        return View();
    }

    // ============================================================
    // 1. SET IN — ĐỌC HÀNG LOẠT PCARD (VÀO CHUYỀN)
    // ============================================================
    [HttpGet]
    public IActionResult SetIn()
    {
        SetApiBaseUrl();
        return View();
    }

    // ============================================================
    // 2. SET OUT — RA CHUYỀN
    // ============================================================
    [HttpGet]
    public IActionResult SetOut()
    {
        SetApiBaseUrl();
        return View();
    }

    // ============================================================
    // PROXY API LOOKUP CHO PCARD PLAN INFO
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetPcardInfo([FromQuery] string cardNo)
    {
        if (string.IsNullOrWhiteSpace(cardNo))
        {
            return BadRequest(new { success = false, message = "Mã PCard không được để trống." });
        }

        var plan = await _apiService.GetProdPlanInfoAsync(cardNo.Trim().ToUpper());
        if (plan == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy thông tin kế hoạch cho PCard #{cardNo}" });
        }

        return Ok(new { success = true, data = plan });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveYieldBatch([FromBody] List<KeyinYieldItemModel> items)
    {
        if (items == null || !items.Any())
        {
            return BadRequest(new { success = false, message = "Danh sách thẻ quét rỗng." });
        }

        // Nhân viên chắc chắn quét cùng 1 PO trong 1 lượt Set In — chặn sớm nếu lẫn PO khác nhau.
        var distinctOrders = items.Select(i => i.COrdNo ?? string.Empty).Distinct().ToList();
        if (distinctOrders.Count > 1)
        {
            return BadRequest(new { success = false, message = "Tất cả thẻ trong 1 lượt lưu phải cùng 1 Order (PO)." });
        }

        var user = COM_STT_WEB.Helpers.AuthHelper.GetCurrentUser(User);
        var empCd = user?.EmpCd ?? "N/A";
        var lineCd = user?.LineCd;

        // C_WORKER VARCHAR2(10) / C_WORK_LINE VARCHAR2(6) — cột rất ngắn, không đủ chứa họ tên
        // đầy đủ (FullName) hay tên phòng ban (DeptNm) như trước đây (gây ORA-12899). Dùng
        // EmpCd/LineCd cho khớp độ dài cột. I_IP_NO để null theo yêu cầu.
        foreach (var item in items)
        {
            item.CWorker = Truncate(empCd, 10);
            item.IIpNo = null;
            item.CWorkLine = Truncate(lineCd, 6);
        }

        var result = await _apiService.SaveYieldBatchAsync(items);
        if (!result.Success)
        {
            return StatusCode(500, new { success = false, message = result.Message ?? "Lỗi khi lưu danh sách vào TRTB_M_KEYIN_YIELD" });
        }

        return Ok(new { success = true, count = result.Count, ordNo = distinctOrders.FirstOrDefault() });
    }

    // ============================================================
    // 3. XEM LOG — DANH SÁCH ĐÃ GHI TRONG NGÀY HÔM NAY + XOÁ (DÙNG KHI TEST)
    // ============================================================
    [HttpGet]
    public IActionResult Log()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetTodayLog()
    {
        var user = COM_STT_WEB.Helpers.AuthHelper.GetCurrentUser(User);
        var empCd = Truncate(user?.EmpCd, 10);
        var items = await _apiService.GetTodayYieldAsync(empCd);
        return Ok(new { success = true, data = items });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLog([FromQuery] string dGather)
    {
        var ok = await _apiService.DeleteYieldAsync(dGather);
        if (!ok)
        {
            return NotFound(new { success = false, message = "Không tìm thấy dòng này để xoá" });
        }
        return Ok(new { success = true });
    }

    // ============================================================
    // 4. QUẢN LÝ SET IN/OUT — bảng pivot số lượng theo size, tra theo C_ORD_NO (chạy trên desktop)
    // ============================================================
    [HttpGet]
    public IActionResult Manage()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetSizePivot([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số Order." });
        }

        var rows = await _apiService.GetSizePivotByOrderAsync(ordNo.Trim());
        return Ok(new { success = true, data = rows });
    }

    // ============================================================
    // 5. CHỜ SET IN — trạng thái theo Order: TOTAL kế hoạch / đã quét / còn lại theo từng part+size
    // ============================================================
    [HttpGet]
    public IActionResult Pending(string? ordNo)
    {
        ViewData["OrdNo"] = ordNo;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPartYieldStatus([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số Order." });
        }

        var rows = await _apiService.GetPartYieldStatusByOrderAsync(ordNo.Trim());
        return Ok(new { success = true, data = rows });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPartDone([FromQuery] string ordNo, [FromQuery] string size, [FromQuery] string partsNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo) || string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(partsNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số." });
        }

        var result = await _apiService.MarkPartYieldDoneAsync(ordNo.Trim(), size.Trim(), partsNo.Trim());
        return Ok(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePartQty([FromQuery] string ordNo, [FromQuery] string size, [FromQuery] string partsNo, [FromQuery] int qty)
    {
        if (string.IsNullOrWhiteSpace(ordNo) || string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(partsNo))
        {
            return BadRequest(new { success = false, message = "Thiếu tham số." });
        }

        var result = await _apiService.UpdatePartYieldQtyAsync(ordNo.Trim(), size.Trim(), partsNo.Trim(), qty);
        return Ok(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteOrder([FromQuery] string ordNo)
    {
        if (string.IsNullOrWhiteSpace(ordNo))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập số Order." });
        }

        var ok = await _apiService.CompleteOrderAsync(ordNo.Trim());
        if (!ok)
        {
            return StatusCode(500, new { success = false, message = "Không thể đánh dấu hoàn tất." });
        }

        return Ok(new { success = true });
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
