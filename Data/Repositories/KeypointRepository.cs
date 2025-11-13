using meeting_copilot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace meeting_copilot.Data.Repositories;

public class KeypointRepository
{
    private readonly MeetingCopilotDbContext _dbContext;

    public KeypointRepository(MeetingCopilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Keypoint keypoint, CancellationToken cancellationToken = default)
    {
        await _dbContext.Keypoints.AddAsync(keypoint, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Keypoint>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return _dbContext.Keypoints
            .OrderByDescending(k => k.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
