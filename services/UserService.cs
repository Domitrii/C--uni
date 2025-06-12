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

    public async Task<(string token, User user)> LoginAsync(LoginRequest request)
    {
        var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            throw new Exception("Invalid credentials");

        var token = GenerateJwtToken(user.Id);
        return (token, user);
    }

    public async Task<User?> GetUserAsync(string id) =>
        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User> UpdateUserAsync(string id, UpdateUserRequest data)
    {
        var update = Builders<User>.Update
            .Set(u => u.Name, data.Name)
            .Set(u => u.Gender, data.Gender)
            .Set(u => u.DailyNorm, data.DailyNorm)
            .Set(u => u.Weight, data.Weight)
            .Set(u => u.TimeActive, data.TimeActive)
            .Set(u => u.Email, data.Email);

        var result = await _users.FindOneAndUpdateAsync(u => u.Id == id, update, new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After });
        return result;
    }

    public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshToken)
{
    var principal = GetPrincipalFromExpiredToken(refreshToken, _config["Jwt:RefreshKey"]!);
    var userId = principal.FindFirst("id")?.Value;

    var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
    if (user == null || user.RefreshToken != refreshToken)
        throw new Exception("Unauthorized");

    var newAccessToken = GenerateJwtToken(user.Id);
    var newRefreshToken = GenerateRefreshToken(user.Id);

    await _users.UpdateOneAsync(u => u.Id == user.Id, Builders<User>.Update.Set(u => u.RefreshToken, newRefreshToken));

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
        ValidateLifetime = false // ключовий момент
    };

    var tokenHandler = new JwtSecurityTokenHandler();
    return tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken _);
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
            expires: DateTime.Now.AddHours(23),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
