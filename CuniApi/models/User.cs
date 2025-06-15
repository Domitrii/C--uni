using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CuniApi.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public string? AvatarURL { get; set; }
    public string Name { get; set; } = "User";
    public string Gender { get; set; } = "undefined";
    public double DailyNorm { get; set; } = 2000;
    public double Weight { get; set; } = 0;
    public double TimeActive { get; set; } = 0;
}