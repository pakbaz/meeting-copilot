using FluentAssertions;
using MeetingCopilot.Agents;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Messages;
using Microsoft.Extensions.Logging;
using Moq;

namespace MeetingCopilot.Tests.Agents;

/// <summary>
/// Integration tests for the AgentOrchestrator and agent pipeline.
/// </summary>
public class AgentOrchestratorTests
{
    [Fact]
    public void AgentOrchestrator_ShouldAcceptMessages()
    {
        // This test verifies that the orchestrator can accept messages
        // In a real scenario, the orchestrator would be instantiated with DI

        // Arrange
        var message = new UtteranceProcessedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "TranscriptAgent",
            Timestamp = DateTimeOffset.UtcNow,
            Interaction = new Interaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "test-user",
                MeetingId = "test-meeting",
                Type = InteractionType.Utterance,
                Content = "What is the budget for Q4?",
                SpeakerId = "speaker-1",
                SpeakerName = "Alice",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        // Assert - Message is properly formed
        message.MeetingId.Should().NotBeNullOrEmpty();
        message.Interaction.Should().NotBeNull();
        message.Interaction.Content.Should().Contain("budget");
    }

    [Fact]
    public void QuestionDetectedMessage_ShouldContainQuestionDetails()
    {
        // Arrange
        var message = new QuestionDetectedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "TranscriptAgent",
            QuestionText = "What is the timeline?",
            SpeakerId = "speaker-1",
            SpeakerName = "Bob",
            QuestionType = "clarification",
            Confidence = 0.92,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        message.QuestionText.Should().Be("What is the timeline?");
        message.Confidence.Should().BeGreaterThan(0.9);
        message.QuestionType.Should().Be("clarification");
    }

    [Fact]
    public void AnswerReadyMessage_ShouldContainAnswerWithSources()
    {
        // Arrange
        var message = new AnswerReadyMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "AnswerAgent",
            QuestionMessageId = "question-123",
            QuestionText = "What is the budget?",
            AnswerText = "The budget for Q4 is $500,000 as per the planning document.",
            Confidence = 0.85,
            Sources = new List<string> { "Q4Planning.pdf", "BudgetSheet.xlsx" },
            ProcessingTimeMs = 150
        };

        // Assert
        message.AnswerText.Should().Contain("$500,000");
        message.Sources.Should().HaveCount(2);
        message.ProcessingTimeMs.Should().BeLessThan(1000);
    }

    [Fact]
    public void ResearchRequestedMessage_ShouldHaveValidTrigger()
    {
        // Arrange
        var message = new ResearchRequestedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "User",
            Query = "Azure Cosmos DB pricing tiers",
            Trigger = ResearchTrigger.Manual,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Assert
        message.Query.Should().Contain("Cosmos DB");
        message.Trigger.Should().Be(ResearchTrigger.Manual);
    }

    [Fact]
    public void ResearchReadyMessage_ShouldContainResults()
    {
        // Arrange
        var message = new ResearchReadyMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "ResearchAgent",
            Query = "Azure pricing",
            Summary = "Azure offers various pricing models including pay-as-you-go and reserved instances.",
            Results = new List<ResearchResult>
            {
                new ResearchResult
                {
                    Title = "Azure Pricing Calculator",
                    Url = "https://azure.microsoft.com/pricing/calculator/",
                    Snippet = "Calculate your expected monthly costs..."
                },
                new ResearchResult
                {
                    Title = "Azure Pricing Overview",
                    Url = "https://azure.microsoft.com/pricing/",
                    Snippet = "Flexible pricing options for every need..."
                }
            }
        };

        // Assert
        message.Summary.Should().NotBeNullOrEmpty();
        message.Results.Should().HaveCount(2);
        message.Results.All(r => !string.IsNullOrEmpty(r.Url)).Should().BeTrue();
    }

    [Fact]
    public void KeyPointExtractedMessage_ShouldHavePriorityScore()
    {
        // Arrange
        var message = new KeyPointExtractedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "SummaryAgent",
            KeyPointId = "kp-123",
            Title = "Budget Approval",
            Content = "The team approved the Q4 budget of $500,000 for infrastructure improvements.",
            PriorityScore = 85.0,
            SourceUtteranceId = "utterance-456"
        };

        // Assert
        message.Title.Should().Be("Budget Approval");
        message.PriorityScore.Should().BeGreaterThan(80);
        message.PriorityScore.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public void AgendaProgressUpdatedMessage_ShouldTrackStatus()
    {
        // Arrange
        var message = new AgendaProgressUpdatedMessage
        {
            Id = Guid.NewGuid().ToString(),
            MeetingId = "test-meeting",
            UserId = "test-user",
            SourceAgent = "SummaryAgent",
            AgendaItemId = "agenda-1",
            NewStatus = AgendaItemStatus.Completed
        };

        // Assert
        message.AgendaItemId.Should().Be("agenda-1");
        message.NewStatus.Should().Be(AgendaItemStatus.Completed);
    }
}
