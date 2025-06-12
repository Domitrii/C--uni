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
        var all = await _water.Find(w => w.OwnerId == ownerId).ToListAsync();
        return all.Where(w => w.Time.Contains(day)).ToList();
    }

    public async Task<List<WaterRecord>> GetMonthlyAsync(string ownerId, string? month)
    {
        month ??= DateTime.Now.ToString("yyyy-MM");
        var all = await _water.Find(w => w.OwnerId == ownerId && w.Date == month).ToListAsync();
        return all;
    }

    public async Task<WaterRecord> CreateAsync(WaterRecord record)
    {
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
        var result = await _water.ReplaceOneAsync(w => w.Id == id, updated);
        return result.ModifiedCount > 0;
    }
}
