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
    public async Task<IActionResult> GetDaily([FromQuery] string? day = null)
    {
        var userId = User.FindFirst("id")?.Value!;
        var data = await _service.GetDailyAsync(userId, day);
        var total = data.Sum(w => w.Amount);
        return Ok(new { data, waterAmount = total });
    }

    [HttpGet("month")]
    public async Task<IActionResult> GetMonth([FromQuery] string? month = null)
    {
        var userId = User.FindFirst("id")?.Value!;
        var data = await _service.GetMonthlyAsync(userId, month);
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WaterRecord record)
    {
        var userId = User.FindFirst("id")?.Value!;
        record.OwnerId = userId;
        var created = await _service.CreateAsync(record);
        return CreatedAtAction(nameof(GetDaily), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] WaterRecord updated)
    {
        var result = await _service.UpdateAsync(id, updated);
        return result ? Ok(updated) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirst("id")?.Value!;
        var result = await _service.DeleteAsync(id, userId);
        return result ? Ok("Deleted") : NotFound();
    }
}
