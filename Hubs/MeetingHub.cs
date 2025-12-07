using MeetingCopilot.Contracts.Events;
using Microsoft.AspNetCore.SignalR;

namespace meeting_copilot.Hubs;

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
