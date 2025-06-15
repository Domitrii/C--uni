using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CuniApi.Services;
using CuniApi.Models;
using CuniApi.Models.Requests;

namespace CuniApi.Controllers;

[ApiController]
[Route("api/track")]
[Authorize]
public class WaterController : ControllerBase
{
    private readonly WaterService _service;

    public WaterController(WaterService service)
    {
        _service = service;
    }

    [HttpGet("day")]
    public async Task<IActionResult> GetDaily([FromQuery] string? date = null)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var data = await _service.GetDailyAsync(userId, date);
            var total = data.Sum(w => w.Amount);
            
            return Ok(new { 
                data = data.Select(w => new {
                    w.Id,
                    w.Time,
                    w.Amount
                }), 
                waterAmount = total 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("month")]
    public async Task<IActionResult> GetMonth([FromQuery] string? month = null)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var data = await _service.GetMonthlyAsync(userId, month);
            
            return Ok(data.Select(w => new {
                w.Id,
                w.Time,
                w.Amount
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("month/stats")]
    public async Task<IActionResult> GetMonthStats([FromQuery] string? month = null)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var stats = await _service.GetMonthlyStatsAsync(userId, month);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WaterRecordRequest request)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            
            var record = new WaterRecord
            {
                Time = request.Time ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Amount = request.Amount,
                OwnerId = userId
            };
            
            var created = await _service.CreateAsync(record);
            
            return CreatedAtAction(nameof(GetDaily), 
                new { date = created.Time.Substring(0, 10) }, 
                new {
                    created.Id,
                    created.Time,
                    created.Amount
                });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] WaterRecordRequest request)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            
            var existing = await _service.GetByIdAsync(id, userId);
            if (existing == null)
                return NotFound(new { message = "Water record not found" });
            
            var updated = new WaterRecord
            {
                Id = id,
                Time = request.Time ?? existing.Time,
                Amount = request.Amount,
                OwnerId = userId
            };
            
            var success = await _service.UpdateAsync(id, updated);
            
            if (success)
                return Ok(new {
                    updated.Id,
                    updated.Time,
                    updated.Amount
                });
            else
                return NotFound(new { message = "Failed to update water record" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var result = await _service.DeleteAsync(id, userId);
            
            if (result)
                return Ok(new { message = "Water record deleted successfully" });
            else
                return NotFound(new { message = "Water record not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}