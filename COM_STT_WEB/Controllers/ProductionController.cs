using COM_STT_WEB.API.Service;
using COM_STT_WEB.Models.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_WEB.Controllers;

[Authorize]
public class ProductionController : Controller
{
    private readonly ProductionApiService _apiService;

    public ProductionController(ProductionApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? date, [FromQuery] string? status)
    {
        var targetDate = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyyMMdd") : date.Replace("-", "");
        var targetStatus = string.IsNullOrEmpty(status) ? "N" : status;

        var batches = await _apiService.GetBatchesAsync(targetDate, targetStatus);

        var model = new ProductionIndexViewModel
        {
            Batches = batches.Select(b => new InspectionBatchViewModel
            {
                Seq = b.Seq,
                LineCode = b.LineCode,
                MachineCode = b.MachineCode,
                IpUpload = b.IpUpload,
                ProdType = b.ProdType,
                CShift = b.CShift,
                Plant = b.Plant,
                DGather = b.DGather,
                Status = b.Status,
                Orders = b.Orders,
                Styles = b.Styles,
                Sizes = b.Sizes,
                PcardNames = b.PcardNames,
                TotalQty = b.TotalQty,
                OrderQty = b.OrderQty
            }).ToList(),
            DateFilter = string.IsNullOrEmpty(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date,
            StatusFilter = targetStatus
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BatchRegistrationViewModel
        {
            Plant = "B",
            CShift = "SHIFT1",
            IsEdit = false
        });
    }

    // POST kept for backward compat but new UI submits via JS directly to API
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BatchRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (clientIp == "::1") clientIp = "127.0.0.1";

        var seq = await _apiService.CreateBatchAsync(model, clientIp);
        if (seq <= 0)
        {
            ModelState.AddModelError(string.Empty, "Đăng ký đợt sản xuất thất bại. Vui lòng thử lại.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đăng ký thành công đợt sản xuất #{seq}";
        return RedirectToAction(nameof(Scan), new { seq });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int seq)
    {
        var batch = await _apiService.GetBatchAsync(seq);
        if (batch == null)
        {
            return NotFound();
        }

        return View(new BatchRegistrationViewModel
        {
            Seq = batch.Seq,
            Plant = batch.Plant ?? "B",
            LineCode = batch.LineCode ?? string.Empty,
            MachineCode = batch.MachineCode ?? string.Empty,
            CShift = batch.CShift ?? "SHIFT1",
            IsEdit = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int seq, BatchRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _apiService.UpdateBatchAsync(seq, model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Cập nhật đợt sản xuất thất bại.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Đã cập nhật thông tin đợt #{seq}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Finish(int seq)
    {
        var success = await _apiService.FinishBatchAsync(seq);
        if (!success)
        {
            TempData["ErrorMessage"] = $"Không thể hoàn tất đợt sản xuất #{seq}";
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã hoàn tất kiểm lỗi đợt #{seq}";
        }
        return RedirectToAction(nameof(Index));
    }

    // ============================================================
    // JSON PROXY ENDPOINTS — JS phía trình duyệt gọi vào đây (cùng origin,
    // tự động có cookie đăng nhập + [Authorize]), KHÔNG gọi thẳng COM_STT_API.
    // Lý do: ApiSettings:BaseUrl (http://localhost:5052) chỉ đúng trên máy dev;
    // JS chạy trên điện thoại thật gọi thẳng vào đó sẽ luôn lỗi kết nối.
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> BatchesJson([FromQuery] string? date, [FromQuery] string? status)
    {
        var batches = await _apiService.GetBatchesAsync(date, status ?? "ALL");
        return Ok(new { success = true, data = batches });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAjax([FromBody] BatchRegistrationViewModel model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.LineCode) || string.IsNullOrWhiteSpace(model.MachineCode))
        {
            return BadRequest(new { success = false, message = "Vui lòng chọn Chuyền và nhập ít nhất 1 mã máy." });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (clientIp == "::1") clientIp = "127.0.0.1";

        var seq = await _apiService.CreateBatchAsync(model, clientIp);
        if (seq <= 0)
        {
            return StatusCode(500, new { success = false, message = "Đăng ký đợt sản xuất thất bại. Vui lòng thử lại." });
        }

        return Ok(new { success = true, seq });
    }

    [HttpGet]
    public async Task<IActionResult> CheckPcard([FromQuery] string pcardNo)
    {
        var exists = await _apiService.ExistsPcardAsync(pcardNo);
        if (exists)
        {
            return Ok(new { success = true });
        }
        return NotFound(new { success = false });
    }

    [HttpGet]
    public async Task<IActionResult> ProdPlan([FromQuery] string cardNo)
    {
        var plan = await _apiService.GetProdPlanInfoAsync(cardNo);
        if (plan == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy thông tin kế hoạch cho thẻ PCard " + cardNo });
        }
        return Ok(new { success = true, data = plan });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePcard([FromBody] PcardScanItemViewModel item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.PcardNo))
        {
            return BadRequest(new { success = false, message = "PCARD_NO là bắt buộc" });
        }

        var ok = await _apiService.CreateInspectionHeadAsync(item);
        if (!ok)
        {
            return StatusCode(409, new { success = false, message = "Không thể lưu PCard vào đợt (có thể đã được quét trước đó)." });
        }

        return Ok(new { success = true });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePcard([FromQuery] string pcardNo)
    {
        var ok = await _apiService.DeleteInspectionHeadAsync(pcardNo);
        if (!ok)
        {
            return NotFound(new { success = false });
        }
        return Ok(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> Scan(int seq)
    {
        var batch = await _apiService.GetBatchAsync(seq);
        if (batch == null)
        {
            return NotFound();
        }

        var pcards = await _apiService.GetPcardsBySeqAsync(seq);

        var model = new PcardScanViewModel
        {
            Batch = new InspectionBatchViewModel
            {
                Seq = batch.Seq,
                LineCode = batch.LineCode,
                MachineCode = batch.MachineCode,
                CShift = batch.CShift,
                Plant = batch.Plant,
                DGather = batch.DGather,
                Status = batch.Status
            },
            Pcards = pcards
        };

        return View(model);
    }
}
