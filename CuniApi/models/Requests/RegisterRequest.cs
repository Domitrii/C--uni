namespace CuniApi.Models.Requests;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string RepeatPassword { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public double DailyNorm { get; set; }
    public double Weight { get; set; }
    public double TimeActive { get; set; }
}