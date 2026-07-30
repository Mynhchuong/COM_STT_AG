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

        var success = await _apiService.SaveYieldBatchAsync(items);
        if (!success)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu danh sách vào TRTB_M_KEYIN_YIELD" });
        }

        return Ok(new { success = true, count = items.Count });
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

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
