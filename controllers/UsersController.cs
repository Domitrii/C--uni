[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _service;

    public UsersController(UserService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = await _service.RegisterAsync(request);
        return Ok(new { user.Id, user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (token, user) = await _service.LoginAsync(request);
        return Ok(new {
            token,
            user = new {
                user.Id,
                user.Email,
                user.Name,
                user.Gender,
                user.DailyNorm,
                user.Weight,
                user.TimeActive
            }
        });
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst("id")?.Value!;
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }

    [HttpPost("update")]
    [Authorize]
    public async Task<IActionResult> Update(UpdateUserRequest request)
    {
        var userId = User.FindFirst("id")?.Value!;
        var updated = await _service.UpdateUserAsync(userId, request);
        return Ok(updated);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT → logout тільки на клієнті (фронті)
        return Ok("Logged out");
    }
}
