using FluentAssertions;
using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Tests.Entities;

public class InsightTests
{
    [Fact]
    public void Insight_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var insight = new Insight { Title = "Test Insight", Content = "Test content" };

        // Assert
        insight.Id.Should().NotBeNullOrEmpty();
        insight.Title.Should().Be("Test Insight");
        insight.Type.Should().Be(InsightType.KeyPoint);
        insight.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Insight_ShouldSupportAllTypes()
    {
        // Arrange & Act
        var keypoint = new Insight { Title = "Keypoint", Content = "c", Type = InsightType.KeyPoint };
        var action = new Insight { Title = "Action", Content = "c", Type = InsightType.ActionItem };
        var parkingLot = new Insight { Title = "Parking", Content = "c", Type = InsightType.ParkingLot };
        var research = new Insight { Title = "Research", Content = "c", Type = InsightType.ResearchResult };

        // Assert
        keypoint.Type.Should().Be(InsightType.KeyPoint);
        action.Type.Should().Be(InsightType.ActionItem);
        parkingLot.Type.Should().Be(InsightType.ParkingLot);
        research.Type.Should().Be(InsightType.ResearchResult);
    }

    [Fact]
    public void Insight_ShouldSupportPriorityScore()
    {
        // Arrange & Act
        var insight = new Insight
        {
            Title = "High Priority Insight",
            Content = "Detailed content",
            PriorityScore = 95.5,
            SourceUtteranceId = "utterance-123"
        };

        // Assert
        insight.PriorityScore.Should().Be(95.5);
        insight.SourceUtteranceId.Should().Be("utterance-123");
    }

    [Fact]
    public void Insight_ShouldSupportActionItemDetails()
    {
        // Arrange & Act
        var insight = new Insight
        {
            Title = "Action Item",
            Content = "Complete the task",
            Type = InsightType.ActionItem,
            OwnerSpeakerId = "speaker-123",
            OwnerName = "John Doe",
            Status = ActionItemStatus.Pending
        };

        // Assert
        insight.OwnerSpeakerId.Should().Be("speaker-123");
        insight.OwnerName.Should().Be("John Doe");
        insight.Status.Should().Be(ActionItemStatus.Pending);
    }

    [Fact]
    public void Insight_ShouldSupportResearchDetails()
    {
        // Arrange & Act
        var insight = new Insight
        {
            Title = "Research Result",
            Content = "Findings about topic X",
            Type = InsightType.ResearchResult,
            ResearchQuery = "What is topic X?",
            WebSources = new List<WebSource>
            {
                new WebSource { Title = "Source 1", Url = "https://example.com/1" }
            }
        };

        // Assert
        insight.ResearchQuery.Should().Be("What is topic X?");
        insight.WebSources.Should().HaveCount(1);
    }
}

public class InteractionTests
{
    [Fact]
    public void Interaction_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var interaction = new Interaction
        {
            MeetingId = "meeting-123",
            Content = "Hello world"
        };

        // Assert
        interaction.Id.Should().NotBeNullOrEmpty();
        interaction.MeetingId.Should().Be("meeting-123");
        interaction.Content.Should().Be("Hello world");
        interaction.Type.Should().Be(InteractionType.Utterance);
        interaction.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Interaction_ShouldSupportAllTypes()
    {
        // Arrange & Act
        var utterance = new Interaction { MeetingId = "m", Content = "t", Type = InteractionType.Utterance };
        var question = new Interaction { MeetingId = "m", Content = "q", Type = InteractionType.Question };
        var answer = new Interaction { MeetingId = "m", Content = "a", Type = InteractionType.Answer };
        var agentMessage = new Interaction { MeetingId = "m", Content = "m", Type = InteractionType.AgentMessage };

        // Assert
        utterance.Type.Should().Be(InteractionType.Utterance);
        question.Type.Should().Be(InteractionType.Question);
        answer.Type.Should().Be(InteractionType.Answer);
        agentMessage.Type.Should().Be(InteractionType.AgentMessage);
    }

    [Fact]
    public void Interaction_ShouldSupportSpeakerInfo()
    {
        // Arrange & Act
        var interaction = new Interaction
        {
            MeetingId = "meeting-123",
            Content = "This is what I said",
            SpeakerId = "speaker-1",
            SpeakerName = "John Doe"
        };

        // Assert
        interaction.SpeakerId.Should().Be("speaker-1");
        interaction.SpeakerName.Should().Be("John Doe");
    }

    [Fact]
    public void Interaction_ShouldSupportConfidence()
    {
        // Arrange & Act
        var interaction = new Interaction
        {
            MeetingId = "meeting-123",
            Content = "Recognized speech",
            Confidence = 0.92
        };

        // Assert
        interaction.Confidence.Should().Be(0.92);
    }

    [Fact]
    public void Interaction_ShouldSupportAgentMetadata()
    {
        // Arrange & Act
        var interaction = new Interaction
        {
            MeetingId = "meeting-123",
            Content = "Agent response",
            Type = InteractionType.Answer,
            AgentName = "AnswerAgent",
            ProcessingTimeMs = 250
        };

        // Assert
        interaction.AgentName.Should().Be("AnswerAgent");
        interaction.ProcessingTimeMs.Should().Be(250);
    }
}
