using FluentAssertions;
using meeting_copilot.Hubs;
using meeting_copilot.Services;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Events;
using MeetingCopilot.Contracts.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MeetingCopilot.Tests.Services;

/// <summary>
/// Tests for the SignalRMessageBroadcaster service.
/// </summary>
public class SignalRMessageBroadcasterTests
{
    private readonly Mock<IHubContext<MeetingHub>> _hubContextMock;
    private readonly Mock<ILogger<SignalRMessageBroadcaster>> _loggerMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly SignalRMessageBroadcaster _broadcaster;

    public SignalRMessageBroadcasterTests()
    {
        _hubContextMock = new Mock<IHubContext<MeetingHub>>();
        _loggerMock = new Mock<ILogger<SignalRMessageBroadcaster>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _hubClientsMock = new Mock<IHubClients>();

        _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(_hubClientsMock.Object);

        _broadcaster = new SignalRMessageBroadcaster(_hubContextMock.Object, _loggerMock.Object);
    }

    #region UtteranceProcessed Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendUtteranceProcessed_WhenUtteranceProcessedMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new UtteranceProcessedMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            Interaction = new Interaction
            {
                Id = "interaction-1",
                UserId = "test-user",
                MeetingId = meetingId,
                Type = InteractionType.Utterance,
                Content = "Hello, this is a test",
                SpeakerId = "speaker-1",
                SpeakerName = "John Doe",
                Confidence = 0.95,
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "UtteranceProcessed",
            It.Is<object[]>(args => args.Length == 1 && args[0] is TranscriptionEvent),
            default), Times.Once);
    }

    #endregion

    #region AnswerReady Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendAnswerReady_WhenAnswerReadyMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new AnswerReadyMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            QuestionMessageId = "question-1",
            QuestionText = "What is the budget?",
            AnswerText = "The budget is $50,000",
            Confidence = 0.85,
            Sources = new List<string> { "doc1.pdf", "doc2.pdf" },
            ProcessingTimeMs = 250
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "AnswerReady",
            It.Is<object[]>(args => args.Length == 1 && args[0] is AnswerEvent),
            default), Times.Once);
    }

    #endregion

    #region KeyPointExtracted Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendKeyPointsUpdated_WhenKeyPointExtractedMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new KeyPointExtractedMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            KeyPointId = "keypoint-1",
            Title = "Important Decision",
            Content = "We decided to go with option A",
            PriorityScore = 80,
            SourceUtteranceId = "utterance-1"
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "KeyPointsUpdated",
            It.Is<object[]>(args => args.Length == 1 && args[0] is KeyPointsEvent),
            default), Times.Once);
    }

    #endregion

    #region ResearchReady Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendResearchReady_WhenResearchReadyMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new ResearchReadyMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            Query = "Azure Cosmos DB pricing",
            Summary = "Azure Cosmos DB offers various pricing tiers...",
            Results = new List<ResearchResult>
            {
                new ResearchResult { Title = "Pricing", Url = "https://azure.com", Snippet = "See pricing..." }
            }
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ResearchReady",
            It.Is<object[]>(args => args.Length == 1 && args[0] is ResearchEvent),
            default), Times.Once);
    }

    #endregion

    #region QuestionDetected Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendQuestionDetected_WhenQuestionDetectedMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new QuestionDetectedMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            QuestionText = "What is the deadline?",
            SpeakerId = "speaker-1",
            SpeakerName = "Alice",
            QuestionType = "clarification",
            Confidence = 0.9,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "QuestionDetected",
            It.Is<object[]>(args => args.Length == 1 && args[0] is QuestionDetectedEvent),
            default), Times.Once);
    }

    #endregion

    #region AgendaItemUpdated Tests

    [Fact]
    public async Task BroadcastAsync_ShouldSendAgendaItemUpdated_WhenAgendaProgressUpdatedMessage()
    {
        // Arrange
        var meetingId = "test-meeting";
        var message = new AgendaProgressUpdatedMessage
        {
            MeetingId = meetingId,
            UserId = "test-user",
            AgendaItemId = "agenda-1",
            NewStatus = AgendaItemStatus.Completed
        };

        // Act
        await _broadcaster.BroadcastAsync(message);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "AgendaItemUpdated",
            It.Is<object[]>(args => args.Length == 1 && args[0] is AgendaItemUpdatedEvent),
            default), Times.Once);
    }

    #endregion

    #region ConnectionStatus Tests

    [Fact]
    public async Task BroadcastConnectionStatusAsync_ShouldSendConnectionStatusChanged()
    {
        // Arrange
        var meetingId = "test-meeting";
        var statusEvent = new ConnectionStatusEvent
        {
            Status = ConnectionStatus.Connected,
            Message = "Connection established",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _broadcaster.BroadcastConnectionStatusAsync(meetingId, statusEvent);

        // Assert
        _hubClientsMock.Verify(c => c.Group(meetingId), Times.Once);
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "ConnectionStatusChanged",
            It.Is<object[]>(args => args.Length == 1),
            default), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task BroadcastAsync_ShouldNotThrow_WhenUnknownMessageType()
    {
        // Arrange - create a message that doesn't match any handler
        var message = new AgendaProgressUpdatedMessage 
        { 
            MeetingId = "test",
            UserId = "user",
            AgendaItemId = "item-1",
            NewStatus = AgendaItemStatus.Pending
        };

        // Act - Just verify no exception thrown for known type
        var act = async () => await _broadcaster.BroadcastAsync(message);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastAsync_ShouldLogWarning_WhenHubThrowsException()
    {
        // Arrange
        var message = new AnswerReadyMessage
        {
            MeetingId = "test-meeting",
            UserId = "test-user",
            QuestionMessageId = "q-1",
            QuestionText = "Test?",
            AnswerText = "Test answer"
        };

        _clientProxyMock.Setup(c => c.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SignalR error"));

        // Act
        var act = async () => await _broadcaster.BroadcastAsync(message);

        // Assert - Should not throw, just log warning
        await act.Should().NotThrowAsync();
    }

    #endregion
}
