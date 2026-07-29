using COM_STT_API.Models;
using COM_STT_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace COM_STT_API.Controllers;

[ApiController]
[Route("api/inspection-batch")]
public class InspectionBatchController : ControllerBase
{
    private readonly InspectionBatchService _batchService;

    public InspectionBatchController(InspectionBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? date, [FromQuery] string? status)
    {
        try
        {
            var batches = await _batchService.GetAllBatchesAsync(date, status);
            return Ok(new { success = true, total = batches.Count, data = batches });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("registered")]
    public async Task<IActionResult> GetRegistered([FromQuery] string? date, [FromQuery] string? status)
    {
        try
        {
            var batches = await _batchService.GetAllBatchesAsync(date, status);
            return Ok(new { success = true, total = batches.Count, data = batches });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("completed")]
    public async Task<IActionResult> GetCompleted([FromQuery] string? date)
    {
        try
        {
            var batches = await _batchService.GetCompletedBatchesAsync(date);
            return Ok(new { success = true, total = batches.Count, data = batches });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet("{seq:int}")]
    public async Task<IActionResult> GetBySeq(int seq)
    {
        try
        {
            var batch = await _batchService.GetBatchAsync(seq);
            if (batch == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đợt này" });
            }
            return Ok(new { success = true, data = batch });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InspectionBatch batch)
    {
        try
        {
            var seq = await _batchService.CreateBatchAsync(batch);
            return StatusCode(201, new { success = true, SEQ = seq });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{seq:int}")]
    public async Task<IActionResult> Update(int seq, [FromBody] InspectionBatch batch)
    {
        try
        {
            var success = await _batchService.UpdateBatchAsync(seq, batch.LineCode, batch.MachineCode, batch.CShift, batch.Plant);
            if (!success)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đợt này" });
            }
            return Ok(new { success = true, message = "Cập nhật đợt thành công" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPut("{seq:int}/finish")]
    public async Task<IActionResult> Finish(int seq)
    {
        try
        {
            var success = await _batchService.FinishBatchAsync(seq);
            if (!success)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đợt này" });
            }
            return Ok(new { success = true, message = "Đã đánh dấu đợt hoàn tất kiểm lỗi" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
