namespace CuniApi.Models.Requests;

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public double? DailyNorm { get; set; }
    public double? Weight { get; set; }
    public double? TimeActive { get; set; }
    public string? Email { get; set; }
    public string? AvatarURL { get; set; }
}