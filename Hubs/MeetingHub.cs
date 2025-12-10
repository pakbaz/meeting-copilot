using MeetingCopilot.Contracts.Events;
using Microsoft.AspNetCore.SignalR;

namespace meeting_copilot.Hubs;

/// <summary>
/// SignalR hub for real-time meeting communication.
/// Broadcasts transcription, answers, key points, and research events to clients.
/// </summary>
public class MeetingHub : Hub
{
    private readonly ILogger<MeetingHub> _logger;

    public MeetingHub(ILogger<MeetingHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinMeeting(string meetingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, meetingId);
        _logger.LogInformation("Client {ConnectionId} joined meeting {MeetingId}", Context.ConnectionId, meetingId);
    }

    public async Task LeaveMeeting(string meetingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, meetingId);
        _logger.LogInformation("Client {ConnectionId} left meeting {MeetingId}", Context.ConnectionId, meetingId);
    }

    public async Task StartTranscription(string meetingId)
    {
        _logger.LogInformation("Starting transcription for meeting {MeetingId}", meetingId);
        // Transcription logic will be implemented in SpeechRecognitionService
        await Task.CompletedTask;
    }

    public async Task StopTranscription(string meetingId)
    {
        _logger.LogInformation("Stopping transcription for meeting {MeetingId}", meetingId);
        await Task.CompletedTask;
    }

    public async Task SendCommand(string meetingId, SlashCommand command)
    {
        _logger.LogInformation("Processing command {Type} for meeting {MeetingId}", command.Type, meetingId);
        // Command processing will be handled by SlashCommandService
        await Task.CompletedTask;
    }

    public async Task SubmitQuestion(string meetingId, string question)
    {
        _logger.LogInformation("Question submitted for meeting {MeetingId}: {Question}", meetingId, question);
        // Question will be routed to AnswerAgent
        await Task.CompletedTask;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

public record SlashCommand(string Type, string Argument);

/// <summary>
/// Static extension methods for broadcasting events from agents to meeting groups.
/// </summary>
public static class MeetingHubExtensions
{
    /// <summary>
    /// Broadcasts a transcription event to all clients in a meeting.
    /// </summary>
    public static async Task BroadcastUtteranceProcessedAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        TranscriptionEvent transcriptionEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("UtteranceProcessed", transcriptionEvent);
    }

    /// <summary>
    /// Broadcasts when an answer is ready for display.
    /// </summary>
    public static async Task BroadcastAnswerReadyAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        AnswerEvent answerEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("AnswerReady", answerEvent);
    }

    /// <summary>
    /// Broadcasts when key points are updated.
    /// </summary>
    public static async Task BroadcastKeyPointsUpdatedAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        KeyPointsEvent keyPointsEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("KeyPointsUpdated", keyPointsEvent);
    }

    /// <summary>
    /// Broadcasts when research results are ready.
    /// </summary>
    public static async Task BroadcastResearchReadyAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        ResearchEvent researchEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("ResearchReady", researchEvent);
    }

    /// <summary>
    /// Broadcasts connection status changes for degraded mode indication.
    /// </summary>
    public static async Task BroadcastConnectionStatusChangedAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        ConnectionStatusEvent statusEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("ConnectionStatusChanged", statusEvent);
    }

    /// <summary>
    /// Broadcasts when a question is detected in the transcript.
    /// </summary>
    public static async Task BroadcastQuestionDetectedAsync(
        this IHubContext<MeetingHub> hubContext,
        string meetingId,
        QuestionDetectedEvent questionEvent)
    {
        await hubContext.Clients.Group(meetingId)
            .SendAsync("QuestionDetected", questionEvent);
    }
}
