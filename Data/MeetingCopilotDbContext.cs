using meeting_copilot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace meeting_copilot.Data;

public class MeetingCopilotDbContext : DbContext
{
    public MeetingCopilotDbContext(DbContextOptions<MeetingCopilotDbContext> options) : base(options)
    {
    }

    public DbSet<Keypoint> Keypoints => Set<Keypoint>();
    public DbSet<GuestInfo> Guests => Set<GuestInfo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Keypoint>(entity =>
        {
            entity.ToTable("Keypoints");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.GuestId).HasMaxLength(64);
            entity.Property(k => k.Point).HasMaxLength(2048);
            entity.Property(k => k.SuggestedBy).HasMaxLength(128);
        });

        modelBuilder.Entity<GuestInfo>(entity =>
        {
            entity.ToTable("GuestInfo");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.GuestId).HasMaxLength(64);
            entity.Property(g => g.GuestName).HasMaxLength(256);
            entity.Property(g => g.JobTitle).HasMaxLength(256);
            entity.HasIndex(g => g.GuestId).IsUnique();
        });
    }
}
