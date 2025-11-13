namespace meeting_copilot.Data.Entities;

public class GuestInfo
{
    public int Id { get; set; }
    public string GuestId { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
