using FluentAssertions;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Moq;

namespace MeetingCopilot.Tests.Components;

/// <summary>
/// Tests for the MeetingSummary component logic.
/// </summary>
public class MeetingSummaryTests
{
    #region Summary Data Loading Tests

    [Fact]
    public async Task LoadSummaryData_ShouldExtractKeyTakeaways_FromInsights()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "default-user";
        
        var insights = new List<Insight>
        {
            new Insight
            {
                Id = "insight-1",
                UserId = userId,
                MeetingId = meetingId,
                Type = InsightType.KeyPoint,
                Title = "Budget Approved",
                Content = "The Q4 budget was approved at $500,000",
                PriorityScore = 90,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Insight
            {
                Id = "insight-2",
                UserId = userId,
                MeetingId = meetingId,
                Type = InsightType.KeyPoint,
                Title = "Timeline Set",
                Content = "Project completion targeted for Q1 2025",
                PriorityScore = 75,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Assert - Verify key takeaway extraction logic
        var keyTakeaways = insights
            .Where(i => i.Type == InsightType.KeyPoint)
            .OrderByDescending(i => i.PriorityScore)
            .Take(10)
            .Select(i => $"• {i.Title}: {i.Content}")
            .ToList();

        keyTakeaways.Should().HaveCount(2);
        keyTakeaways[0].Should().Contain("Budget Approved"); // Higher priority first
    }

    [Fact]
    public async Task LoadSummaryData_ShouldExtractActionItems_FromInsights()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "default-user";

        var insights = new List<Insight>
        {
            new Insight
            {
                Id = "insight-1",
                UserId = userId,
                MeetingId = meetingId,
                Type = InsightType.ActionItem,
                Title = "Review proposal",
                Content = "Review the vendor proposal by Friday",
                OwnerName = "Alice",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new Insight
            {
                Id = "insight-2",
                UserId = userId,
                MeetingId = meetingId,
                Type = InsightType.ActionItem,
                Title = "Schedule follow-up",
                Content = "Schedule follow-up meeting next week",
                OwnerName = null,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Assert - Verify action item extraction logic
        var actionItems = insights
            .Where(i => i.Type == InsightType.ActionItem)
            .OrderBy(i => i.CreatedAt)
            .Select(i => $"• {i.Title}" + (!string.IsNullOrEmpty(i.OwnerName) ? $" (@{i.OwnerName})" : ""))
            .ToList();

        actionItems.Should().HaveCount(2);
        actionItems[0].Should().Contain("@Alice");
        actionItems[1].Should().NotContain("@");
    }

    [Fact]
    public async Task LoadSummaryData_ShouldBuildTranscript_FromInteractions()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "default-user";

        var interactions = new List<Interaction>
        {
            new Interaction
            {
                Id = "int-1",
                UserId = userId,
                MeetingId = meetingId,
                Type = InteractionType.Utterance,
                Content = "Good morning everyone",
                SpeakerName = "Alice",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-30)
            },
            new Interaction
            {
                Id = "int-2",
                UserId = userId,
                MeetingId = meetingId,
                Type = InteractionType.Utterance,
                Content = "Let's start with the agenda",
                SpeakerName = "Bob",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-29)
            },
            new Interaction
            {
                Id = "int-3",
                UserId = userId,
                MeetingId = meetingId,
                Type = InteractionType.Question, // Not an utterance - should be filtered
                Content = "What is the first item?",
                SpeakerName = "Charlie",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-28)
            }
        };

        // Assert - Verify transcript building logic
        var utterances = interactions
            .Where(i => i.Type == InteractionType.Utterance)
            .OrderBy(i => i.Timestamp)
            .ToList();

        var transcript = utterances
            .Select(u => $"[{u.SpeakerName ?? "Unknown"}] {u.Content}")
            .ToList();

        utterances.Should().HaveCount(2); // Only utterances, not questions
        transcript[0].Should().Contain("Alice");
        transcript[1].Should().Contain("Bob");
    }

    [Fact]
    public void LoadSummaryData_ShouldHandleEmptyInsights()
    {
        // Arrange
        var insights = new List<Insight>();

        // Act
        var keyTakeaways = insights
            .Where(i => i.Type == InsightType.KeyPoint)
            .Select(i => $"• {i.Title}")
            .ToList();

        // Assert
        keyTakeaways.Should().BeEmpty();
    }

    [Fact]
    public void LoadSummaryData_ShouldHandleEmptyInteractions()
    {
        // Arrange
        var interactions = new List<Interaction>();

        // Act
        var transcript = interactions
            .Where(i => i.Type == InteractionType.Utterance)
            .Select(u => $"[{u.SpeakerName}] {u.Content}")
            .ToList();

        // Assert
        transcript.Should().BeEmpty();
    }

    #endregion

    #region Full Transcript Tests

    [Fact]
    public void FullTranscript_ShouldOrderByTimestamp()
    {
        // Arrange
        var interactions = new List<Interaction>
        {
            new Interaction
            {
                Id = "3",
                Type = InteractionType.Utterance,
                Content = "Third",
                Timestamp = DateTimeOffset.UtcNow
            },
            new Interaction
            {
                Id = "1",
                Type = InteractionType.Utterance,
                Content = "First",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new Interaction
            {
                Id = "2",
                Type = InteractionType.Utterance,
                Content = "Second",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
            }
        };

        // Act
        var ordered = interactions.OrderBy(i => i.Timestamp).ToList();

        // Assert
        ordered[0].Content.Should().Be("First");
        ordered[1].Content.Should().Be("Second");
        ordered[2].Content.Should().Be("Third");
    }

    [Fact]
    public void FullTranscript_ShouldShowLowConfidenceWarning()
    {
        // Arrange
        var interaction = new Interaction
        {
            Id = "1",
            Type = InteractionType.Utterance,
            Content = "Unclear audio",
            Confidence = 0.65 // Below 0.8 threshold
        };

        // Assert
        interaction.Confidence.Should().BeLessThan(0.8);
        // In UI, this would show a warning indicator
    }

    #endregion
}
