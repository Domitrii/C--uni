public class WaterRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;
    public string Time { get; set; } = null!;
    public int Amount { get; set; }
    public string OwnerId { get; set; } = null!;
}
