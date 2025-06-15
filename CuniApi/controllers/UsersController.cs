using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CuniApi.Models.Requests;
using CuniApi.Services;

namespace CuniApi.Controllers;

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
        try
        {
            var user = await _service.RegisterAsync(request);
            return Ok(new { user.Id, user.Email });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var (accessToken, refreshToken, user) = await _service.LoginAsync(request);
            
            await _service.SaveRefreshTokenAsync(user.Id, refreshToken);
            
            return Ok(new {
                accessToken,
                refreshToken,
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var user = await _service.GetUserAsync(userId);
            
            if (user == null)
                return NotFound(new { message = "User not found" });
            
            return Ok(new {
                user.Id,
                user.Email,
                user.Name,
                user.Gender,
                user.DailyNorm,
                user.Weight,
                user.TimeActive,
                user.AvatarURL
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> Update(UpdateUserRequest request)
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            var updated = await _service.UpdateUserAsync(userId, request);
            return Ok(new {
                updated.Id,
                updated.Email,
                updated.Name,
                updated.Gender,
                updated.DailyNorm,
                updated.Weight,
                updated.TimeActive,
                updated.AvatarURL
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var (accessToken, refreshToken) = await _service.RefreshTokenAsync(request.RefreshToken);
            return Ok(new { accessToken, refreshToken });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userId = User.FindFirst("id")?.Value!;
            await _service.RevokeRefreshTokenAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}