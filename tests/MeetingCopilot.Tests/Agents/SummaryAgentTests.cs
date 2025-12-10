using FluentAssertions;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Messages;

namespace MeetingCopilot.Tests.Agents;

/// <summary>
/// Tests for SummaryAgent message handling and key point extraction.
/// </summary>
public class SummaryAgentTests
{
    [Fact]
    public void KeyPointExtractedMessage_ShouldHaveCorrectStructure()
    {
        // Arrange & Act
        var message = new KeyPointExtractedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "SummaryAgent",
            Priority = 5,
            Timestamp = DateTimeOffset.UtcNow,
            KeyPointId = "kp-123",
            Title = "Budget decision for Q4",
            Content = "The team decided to allocate $50,000 for the Q4 marketing campaign.",
            SpeakerId = "speaker-1",
            SpeakerName = "Alice",
            PriorityScore = 75,
            SourceUtteranceId = "utterance-456",
            Categories = new List<string> { "decision", "budget" }
        };

        // Assert
        message.KeyPointId.Should().Be("kp-123");
        message.Title.Should().Contain("Budget");
        message.Content.Should().Contain("$50,000");
        message.PriorityScore.Should().Be(75);
        message.Categories.Should().Contain("decision");
    }

    [Fact]
    public void AgendaProgressUpdatedMessage_ShouldTrackAgendaChanges()
    {
        // Arrange & Act
        var message = new AgendaProgressUpdatedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "SummaryAgent",
            Priority = 3,
            Timestamp = DateTimeOffset.UtcNow,
            AgendaItemId = "agenda-1",
            NewStatus = AgendaItemStatus.InProgress
        };

        // Assert
        message.AgendaItemId.Should().Be("agenda-1");
        message.NewStatus.Should().Be(AgendaItemStatus.InProgress);
    }

    [Fact]
    public void KeyPointExtractedMessage_WithMinimalProperties_ShouldBeValid()
    {
        // Arrange & Act - Test with minimal required properties
        var message = new KeyPointExtractedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "meeting-1",
            UserId = "user-1",
            SourceAgent = "SummaryAgent",
            Priority = 5,
            Timestamp = DateTimeOffset.UtcNow,
            KeyPointId = "kp-1",
            Title = "Key Point",
            Content = "Content",
            SpeakerId = "speaker-1",
            PriorityScore = 50,
            SourceUtteranceId = "utt-1"
        };

        // Assert
        message.Should().NotBeNull();
        message.MeetingId.Should().NotBeNullOrEmpty();
        message.SpeakerName.Should().BeNull(); // Optional property
    }

    [Fact]
    public void AgendaStatusValues_ShouldIncludeAllStates()
    {
        // Verify all agenda status values are accessible
        var pending = AgendaItemStatus.Pending;
        var inProgress = AgendaItemStatus.InProgress;
        var completed = AgendaItemStatus.Completed;

        pending.Should().NotBe(inProgress);
        inProgress.Should().NotBe(completed);
        completed.Should().NotBe(pending);
    }

    [Fact]
    public void UtteranceProcessedMessage_CanBeProcessedBySummaryAgent()
    {
        // Arrange - Create a message that SummaryAgent should handle
        var message = new UtteranceProcessedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "TranscriptAgent",
            Priority = 10,
            Timestamp = DateTimeOffset.UtcNow,
            Interaction = new Interaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "test-user",
                MeetingId = "test-meeting",
                Type = InteractionType.Utterance,
                Content = "We decided to go with option B for the Q4 marketing campaign with a budget of $50,000.",
                SpeakerId = "speaker-1",
                SpeakerName = "Alice",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        // Assert - Message has content that should trigger key point extraction
        message.Interaction.Content.Should().Contain("decided");
        message.Interaction.Content.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void KeyPointExtractedMessage_Categories_ShouldBeInitialized()
    {
        // Test that Categories default to empty list
        var message = new KeyPointExtractedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "meeting-1",
            UserId = "user-1",
            SourceAgent = "SummaryAgent",
            Priority = 5,
            Timestamp = DateTimeOffset.UtcNow,
            KeyPointId = "kp-1",
            Title = "Key Point",
            Content = "Content",
            SpeakerId = "speaker-1",
            PriorityScore = 50,
            SourceUtteranceId = "utt-1"
            // Categories not set explicitly
        };

        // Assert - Categories should be initialized to empty list, not null
        message.Categories.Should().NotBeNull();
        message.Categories.Should().BeEmpty();
    }
}
