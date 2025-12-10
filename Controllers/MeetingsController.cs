using Microsoft.AspNetCore.Mvc;
using meeting_copilot.Services;
using MeetingCopilot.Contracts.Entities;

namespace meeting_copilot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeetingsController : ControllerBase
{
    private readonly MeetingService _meetingService;
    private readonly ILogger<MeetingsController> _logger;

    public MeetingsController(
        MeetingService meetingService,
        ILogger<MeetingsController> logger)
    {
        _meetingService = meetingService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Meeting>> CreateMeeting([FromBody] CreateMeetingRequest request)
    {
        try
        {
            // For MVP, use a default user ID (in production, this would come from authentication)
            var userId = "default-user";

            var meeting = await _meetingService.CreateMeetingAsync(
                userId,
                request.Title,
                request.Description,
                request.AgendaItems,
                request.Attachments,
                request.MicrophoneDeviceId,
                request.Language ?? "en-US"
            );

            return CreatedAtAction(nameof(GetMeeting), new { id = meeting.Id }, meeting);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating meeting");
            return StatusCode(500, new { error = "Failed to create meeting" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Meeting>> GetMeeting(string id)
    {
        try
        {
            var meeting = await _meetingService.GetMeetingAsync(id);
            if (meeting == null)
            {
                return NotFound();
            }

            return meeting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meeting {MeetingId}", id);
            return StatusCode(500, new { error = "Failed to get meeting" });
        }
    }

    [HttpPost("{id}/start")]
    public async Task<ActionResult<Meeting>> StartMeeting(string id)
    {
        try
        {
            var meeting = await _meetingService.StartMeetingAsync(id);
            return meeting;
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting meeting {MeetingId}", id);
            return StatusCode(500, new { error = "Failed to start meeting" });
        }
    }

    [HttpPost("{id}/end")]
    public async Task<ActionResult<Meeting>> EndMeeting(string id)
    {
        try
        {
            var meeting = await _meetingService.EndMeetingAsync(id);
            return meeting;
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending meeting {MeetingId}", id);
            return StatusCode(500, new { error = "Failed to end meeting" });
        }
    }
}

public record CreateMeetingRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public List<AgendaItem>? AgendaItems { get; init; }
    public List<AttachmentRef>? Attachments { get; init; }
    public string? MicrophoneDeviceId { get; init; }
    public string? Language { get; init; }
}
