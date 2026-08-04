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

        return Ok(new { success = true, count = result.Count, ordNo = distinctOrders.FirstOrDefault(), partYieldMessage = result.PartYieldMessage, basketId = result.BasketId, outputErrors = result.OutputErrors });
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
    public async Task<IActionResult> GetTodayLog([FromQuery] string? date)
    {
        var user = COM_STT_WEB.Helpers.AuthHelper.GetCurrentUser(User);
        var empCd = Truncate(user?.EmpCd, 10);
        var items = await _apiService.GetTodayYieldAsync(empCd, date);
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
    // 6. CHỜ SET IN (v2) — dựa trên TRTB_M_COMPSTT_SET_HEADER/DETAIL (basket 1-2 PCard/lượt)
    // ============================================================
    [HttpGet]
    public IActionResult Pending2(int? basketId)
    {
        ViewData["BasketId"] = basketId;
        return View();
    }

    // ============================================================
    // 7. KIỂM TRA PENDING — quét mã PCard bằng máy quét cầm tay để tra nhanh
    // basket tương ứng còn chưa nhận đủ hay không (nhảy sang Pending2?basketId=...)
    // ============================================================
    [HttpGet]
    public IActionResult CheckPending()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetBasketIdByCard([FromQuery] string cardNo)
    {
        if (string.IsNullOrWhiteSpace(cardNo))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập/quét mã PCard." });
        }

        var basketId = await _apiService.GetBasketIdByCardNoAsync(cardNo.Trim());
        if (basketId == null)
        {
            return NotFound(new { success = false, message = $"Không tìm thấy basket cho PCard {cardNo}." });
        }
        return Ok(new { success = true, basketId });
    }

    [HttpGet]
    public async Task<IActionResult> GetBasketHeader([FromQuery] int basketId)
    {
        var rows = await _apiService.GetBasketHeaderAsync(basketId);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet]
    public async Task<IActionResult> GetBasketDetail([FromQuery] int basketId)
    {
        var rows = await _apiService.GetBasketDetailAsync(basketId);
        return Ok(new { success = true, data = rows });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkBasketDetailDone([FromQuery] int basketId, [FromQuery] string partsNo)
    {
        var user = COM_STT_WEB.Helpers.AuthHelper.GetCurrentUser(User);
        var empCd = user?.EmpCd ?? "N/A";
        var result = await _apiService.MarkBasketDetailDoneAsync(basketId, partsNo, empCd);
        return Ok(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBasketDetailQty([FromQuery] int basketId, [FromQuery] string partsNo, [FromQuery] int qty)
    {
        var user = COM_STT_WEB.Helpers.AuthHelper.GetCurrentUser(User);
        var empCd = user?.EmpCd ?? "N/A";
        var result = await _apiService.UpdateBasketDetailQtyAsync(basketId, partsNo, qty, empCd);
        return Ok(new { success = result.Success, message = result.Message });
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
