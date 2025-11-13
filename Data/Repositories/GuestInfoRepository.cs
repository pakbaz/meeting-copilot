using meeting_copilot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace meeting_copilot.Data.Repositories;

public class GuestInfoRepository
{
    private readonly MeetingCopilotDbContext _dbContext;

    public GuestInfoRepository(MeetingCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(string guestId, string guestName, string jobTitle, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Guests.FirstOrDefaultAsync(g => g.GuestId == guestId, cancellationToken);
        if (existing is null)
        {
            existing = new GuestInfo
            {
                GuestId = guestId,
                GuestName = guestName,
                JobTitle = jobTitle,
                LastUpdatedUtc = DateTime.UtcNow
            };
            _dbContext.Guests.Add(existing);
        }
        else
        {
            existing.GuestName = guestName;
            existing.JobTitle = jobTitle;
            existing.LastUpdatedUtc = DateTime.UtcNow;
            _dbContext.Guests.Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<GuestInfo?> GetByGuestIdAsync(string guestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Guests.FirstOrDefaultAsync(g => g.GuestId == guestId, cancellationToken);
    }
}
