using COM_STT_API.Models;
using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/inspection-head")]
public class InspectionHeadController : ControllerBase
{
    private readonly InspectionHeadService _headService;

    public InspectionHeadController(InspectionHeadService headService)
    {
        _headService = headService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBySeq([FromQuery] int? seq)
    {
        try
        {
            if (seq == null)
            {
                return BadRequest(new { success = false, message = "Thiếu tham số seq" });
            }
            var pcards = await _headService.GetBySeqAsync(seq.Value);
            return Ok(new { success = true, total = pcards.Count, data = pcards });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{pcardNo}")]
    public async Task<IActionResult> GetByPcardNo(string pcardNo)
    {
        try
        {
            var pcard = await _headService.GetByPcardNoAsync(pcardNo);
            if (pcard == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy PCARD_NO" });
            }
            return Ok(new { success = true, data = pcard });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InspectionHead head)
    {
        try
        {
            if (string.IsNullOrEmpty(head.PcardNo))
            {
                return BadRequest(new { success = false, message = "PCARD_NO là bắt buộc" });
            }
            if (head.Seq <= 0)
            {
                return BadRequest(new { success = false, message = "SEQ là bắt buộc" });
            }

            // Check if exists
            var exists = await _headService.ExistsAsync(head.PcardNo);
            if (exists)
            {
                return Conflict(new { success = false, message = "PCARD_NO đã tồn tại" });
            }

            var success = await _headService.CreateHeadAsync(head);
            if (!success)
            {
                return StatusCode(500, new { success = false, message = "Không thể lưu PCARD vào cơ sở dữ liệu" });
            }

            return StatusCode(201, new { success = true, message = "Tạo HEAD thành công", data = new { PCARD_NO = head.PcardNo } });
        }
        catch (OracleException ex) when (ex.Number == 1)
        {
            // ORA-00001: 2 lượt quét PCARD_NO giống nhau lọt qua bước kiểm tra ExistsAsync
            // gần như cùng lúc (race condition) — ràng buộc UNIQUE ở DB chặn lại ở đây.
            return Conflict(new { success = false, message = "PCARD_NO đã tồn tại (trùng do quét đồng thời)" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpDelete("{pcardNo}")]
    public async Task<IActionResult> Delete(string pcardNo)
    {
        try
        {
            var success = await _headService.DeleteHeadAsync(pcardNo);
            if (!success)
            {
                return NotFound(new { success = false, message = "Không tìm thấy PCARD_NO để xoá" });
            }
            return Ok(new { success = true, message = "Xoá PCARD thành công", data = new { PCARD_NO = pcardNo } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
