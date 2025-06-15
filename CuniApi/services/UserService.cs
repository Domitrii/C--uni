using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CuniApi.Models;
using CuniApi.Models.Requests;

namespace CuniApi.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    private readonly IConfiguration _config;

    public UserService(IOptions<MongoDbSettings> settings, IConfiguration config)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _users = db.GetCollection<User>("Users");
        _config = config;
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.RepeatPassword)
            throw new Exception("Passwords do not match");

        var exists = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (exists != null)
            throw new Exception("User already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            Password = hash,
            Name = request.Name,
            Gender = request.Gender,
            DailyNorm = request.DailyNorm,
            Weight = request.Weight,
            TimeActive = request.TimeActive
        };

        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task<(string accessToken, string refreshToken, User user)> LoginAsync(LoginRequest request)
    {
        var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new Exception("Invalid credentials");

        var accessToken = GenerateJwtToken(user.Id);
        var refreshToken = GenerateRefreshToken(user.Id);
        
        return (accessToken, refreshToken, user);
    }

    public async Task<User?> GetUserAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User> UpdateUserAsync(string id, UpdateUserRequest data)
{
    var updateBuilder = Builders<User>.Update;
    var updates = new List<UpdateDefinition<User>>();

    if (!string.IsNullOrEmpty(data.Name))
        updates.Add(updateBuilder.Set(u => u.Name, data.Name));
    
    if (!string.IsNullOrEmpty(data.Gender))
        updates.Add(updateBuilder.Set(u => u.Gender, data.Gender));
    
    if (data.DailyNorm.HasValue)
        updates.Add(updateBuilder.Set(u => u.DailyNorm, data.DailyNorm.Value));
    
    if (data.Weight.HasValue)
        updates.Add(updateBuilder.Set(u => u.Weight, data.Weight.Value));
    
    if (data.TimeActive.HasValue)
        updates.Add(updateBuilder.Set(u => u.TimeActive, data.TimeActive.Value));
    
    if (!string.IsNullOrEmpty(data.Email))
    {
        var existingUser = await _users.Find(u => u.Email == data.Email && u.Id != id).FirstOrDefaultAsync();
        if (existingUser != null)
            throw new Exception("Email already exists");
        
        updates.Add(updateBuilder.Set(u => u.Email, data.Email));
    }

    if (!string.IsNullOrEmpty(data.AvatarURL))
        updates.Add(updateBuilder.Set(u => u.AvatarURL, data.AvatarURL));

    if (!updates.Any())
        throw new Exception("No data to update");

    var update = updateBuilder.Combine(updates);
    
    // ИСПРАВЛЕНИЕ: Указываем оба generic параметра
    var result = await _users.FindOneAndUpdateAsync<User, User>(
    u => u.Id == id, 
    update, 
    new FindOneAndUpdateOptions<User, User> { ReturnDocument = ReturnDocument.After }
);
    
    if (result == null)
        throw new Exception("User not found");
        
    return result;
}

    public async Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        await _users.UpdateOneAsync(
            u => u.Id == userId, 
            Builders<User>.Update.Set(u => u.RefreshToken, refreshToken)
        );
    }

    public async Task RevokeRefreshTokenAsync(string userId)
    {
        await _users.UpdateOneAsync(
            u => u.Id == userId, 
            Builders<User>.Update.Unset(u => u.RefreshToken)
        );
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(refreshToken, _config["Jwt:RefreshKey"]!);
        var userId = principal.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userId))
            throw new Exception("Invalid token");

        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null || user.RefreshToken != refreshToken)
            throw new Exception("Invalid refresh token");

        var newAccessToken = GenerateJwtToken(user.Id);
        var newRefreshToken = GenerateRefreshToken(user.Id);

        await _users.UpdateOneAsync(
            u => u.Id == user.Id, 
            Builders<User>.Update.Set(u => u.RefreshToken, newRefreshToken)
        );

        return (newAccessToken, newRefreshToken);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token, string key)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken _);
        
        return principal;
    }

    private string GenerateRefreshToken(string userId)
    {
        var claims = new[] { new Claim("id", userId) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:RefreshKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateJwtToken(string userId)
    {
        var claims = new[] { new Claim("id", userId) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}