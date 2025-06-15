using MongoDB.Driver;
using Microsoft.Extensions.Options;
using CuniApi.Models;

namespace CuniApi.Services;

public class WaterService
{
    private readonly IMongoCollection<WaterRecord> _water;

    public WaterService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _water = db.GetCollection<WaterRecord>("WaterRecords");
    }

    public async Task<List<WaterRecord>> GetDailyAsync(string ownerId, string? day)
    {
        day ??= DateTime.Now.ToString("yyyy-MM-dd");
        
        var filter = Builders<WaterRecord>.Filter.And(
            Builders<WaterRecord>.Filter.Eq(w => w.OwnerId, ownerId),
            Builders<WaterRecord>.Filter.Regex(w => w.Time, new MongoDB.Bson.BsonRegularExpression(day))
        );
        
        return await _water.Find(filter).ToListAsync();
    }

    public async Task<List<WaterRecord>> GetMonthlyAsync(string ownerId, string? month)
    {
        month ??= DateTime.Now.ToString("yyyy-MM");
        
        var filter = Builders<WaterRecord>.Filter.And(
            Builders<WaterRecord>.Filter.Eq(w => w.OwnerId, ownerId),
            Builders<WaterRecord>.Filter.Regex(w => w.Time, new MongoDB.Bson.BsonRegularExpression($"^{month}"))
        );
        
        return await _water.Find(filter).ToListAsync();
    }

    public async Task<WaterRecord?> GetByIdAsync(string id, string ownerId)
    {
        return await _water.Find(w => w.Id == id && w.OwnerId == ownerId).FirstOrDefaultAsync();
    }

    public async Task<WaterRecord> CreateAsync(WaterRecord record)
    {
        if (string.IsNullOrEmpty(record.Time))
        {
            record.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        await _water.InsertOneAsync(record);
        return record;
    }

    public async Task<bool> DeleteAsync(string id, string ownerId)
    {
        var result = await _water.DeleteOneAsync(w => w.Id == id && w.OwnerId == ownerId);
        return result.DeletedCount > 0;
    }

    public async Task<bool> UpdateAsync(string id, WaterRecord updated)
    {
        updated.Id = id;
        
        var result = await _water.ReplaceOneAsync(w => w.Id == id, updated);
        return result.ModifiedCount > 0;
    }

    public async Task<object> GetMonthlyStatsAsync(string ownerId, string? month)
    {
        var records = await GetMonthlyAsync(ownerId, month);
        
        var dailyStats = records
            .GroupBy(r => r.Time.Substring(0, 10))
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(r => r.Amount),
                RecordsCount = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToList();
        
        return new
        {
            DailyStats = dailyStats,
            TotalAmount = records.Sum(r => r.Amount),
            TotalRecords = records.Count,
            DaysTracked = dailyStats.Count
        };
    }
}