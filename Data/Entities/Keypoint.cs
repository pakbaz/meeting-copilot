namespace meeting_copilot.Data.Entities;

public class Keypoint
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string GuestId { get; set; } = string.Empty;
    public bool Todo { get; set; }
    public string Point { get; set; } = string.Empty;
    public string SuggestedBy { get; set; } = string.Empty;
    public bool NeedsFollowUp { get; set; }
}
