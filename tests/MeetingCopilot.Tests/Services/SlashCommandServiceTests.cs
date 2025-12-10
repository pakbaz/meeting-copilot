using FluentAssertions;
using meeting_copilot.Services;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MeetingCopilot.Tests.Services;

/// <summary>
/// Tests for the SlashCommandService.
/// </summary>
public class SlashCommandServiceTests
{
    private readonly Mock<ILogger<SlashCommandService>> _loggerMock;
    private readonly Mock<ISpeakerRepository> _speakerRepoMock;
    private readonly SlashCommandService _service;

    public SlashCommandServiceTests()
    {
        _loggerMock = new Mock<ILogger<SlashCommandService>>();
        _speakerRepoMock = new Mock<ISpeakerRepository>();
        _service = new SlashCommandService(_speakerRepoMock.Object, _loggerMock.Object);
    }

    #region IsSlashCommand Tests

    [Theory]
    [InlineData("/speaker John Doe", true)]
    [InlineData("/research AI meeting assistants", true)]
    [InlineData("/suggest next steps", true)]
    [InlineData("/help", true)]
    [InlineData("Hello world", false)]
    [InlineData("What is the /research topic?", false)]
    [InlineData("", false)]
    public void IsSlashCommand_ShouldCorrectlyIdentifySlashCommands(string input, bool expected)
    {
        // Act
        var result = _service.IsSlashCommand(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("/SPEAKER John", true)]
    [InlineData("/Research quantum computing", true)]
    [InlineData("/HELP", true)]
    public void IsSlashCommand_ShouldBeCaseInsensitive(string input, bool expected)
    {
        // Act
        var result = _service.IsSlashCommand(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsSlashCommand_ShouldHandleWhitespacePrefix()
    {
        // Arrange
        var input = "  /help";

        // Act
        var result = _service.IsSlashCommand(input);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ShouldReturnHelpMessage_WhenHelpCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/help";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeTrue();
        result.Type.Should().Be(SlashCommandService.SlashCommandType.Help);
        result.Message.Should().Contain("Available Commands:");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResearchData_WhenResearchCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/research machine learning best practices";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeTrue();
        result.Type.Should().Be(SlashCommandService.SlashCommandType.Research);
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenResearchCommandWithNoArgs()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/research";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Usage:");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUnknownCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/invalid command";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Unknown command");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNotSlashCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "regular text";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Not a slash command");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuggestData_WhenSuggestCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/suggest";

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeTrue();
        result.Type.Should().Be(SlashCommandService.SlashCommandType.Suggest);
    }

    #endregion

    #region Speaker Command Tests

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateSpeaker_WhenValidSpeakerCommand()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/speaker Guest1 John Smith";

        _speakerRepoMock.Setup(r => r.GetByMeetingIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Speaker>());

        _speakerRepoMock.Setup(r => r.CreateAsync(It.IsAny<Speaker>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Speaker s, CancellationToken _) => s);

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeTrue();
        result.Type.Should().Be(SlashCommandService.SlashCommandType.Speaker);
        result.Message.Should().Contain("John Smith");
        _speakerRepoMock.Verify(r => r.CreateAsync(It.Is<Speaker>(s => s.DisplayName == "John Smith"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateExistingSpeaker_WhenSpeakerExists()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/speaker Guest1 Jane Doe [Manager]";

        var existingSpeaker = new Speaker
        {
            Id = "speaker-1",
            UserId = userId,
            MeetingId = meetingId,
            SpeakerLabel = "Guest1",
            DisplayName = "Unknown",
            FirstSeenAt = DateTimeOffset.UtcNow
        };

        _speakerRepoMock.Setup(r => r.GetByMeetingIdAsync(meetingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Speaker> { existingSpeaker });

        _speakerRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Speaker>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Speaker s, CancellationToken _) => s);

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeTrue();
        result.Type.Should().Be(SlashCommandService.SlashCommandType.Speaker);
        result.Message.Should().Contain("Jane Doe");
        result.Message.Should().Contain("Manager");
        _speakerRepoMock.Verify(r => r.UpdateAsync(It.Is<Speaker>(s => s.DisplayName == "Jane Doe" && s.Role == "Manager"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSpeakerCommandWithInvalidFormat()
    {
        // Arrange
        var meetingId = "test-meeting";
        var userId = "test-user";
        var input = "/speaker"; // Missing arguments

        // Act
        var result = await _service.ExecuteAsync(meetingId, userId, input);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Usage:");
    }

    #endregion
}
