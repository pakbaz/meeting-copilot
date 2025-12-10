using FluentAssertions;
using MeetingCopilot.Contracts.Entities;
using MeetingCopilot.Contracts.Services;

namespace MeetingCopilot.Tests.Services;

public class MeetingContextParserTests
{
    #region ParseTitle Tests

    [Fact]
    public void ParseTitle_ShouldReturnFirstLine_WhenSimpleContext()
    {
        // Arrange
        var context = "Q4 Planning Meeting\nSome additional context";

        // Act
        var title = MeetingContextParser.ParseTitle(context);

        // Assert
        title.Should().Be("Q4 Planning Meeting");
    }

    [Fact]
    public void ParseTitle_ShouldRemoveTitlePrefix()
    {
        // Arrange
        var context = "Title: Product Review\nMore details here";

        // Act
        var title = MeetingContextParser.ParseTitle(context);

        // Assert
        title.Should().Be("Product Review");
    }

    [Fact]
    public void ParseTitle_ShouldRemoveSubjectPrefix()
    {
        // Arrange
        var context = "Subject: Weekly Standup\nAttendees: Team";

        // Act
        var title = MeetingContextParser.ParseTitle(context);

        // Assert
        title.Should().Be("Weekly Standup");
    }

    [Fact]
    public void ParseTitle_ShouldRemoveMeetingPrefix()
    {
        // Arrange
        var context = "Meeting: Sprint Review\nDate: Monday";

        // Act
        var title = MeetingContextParser.ParseTitle(context);

        // Assert
        title.Should().Be("Sprint Review");
    }

    [Fact]
    public void ParseTitle_ShouldReturnEmpty_WhenNullContext()
    {
        // Act
        var title = MeetingContextParser.ParseTitle(null);

        // Assert
        title.Should().BeEmpty();
    }

    [Fact]
    public void ParseTitle_ShouldReturnEmpty_WhenEmptyContext()
    {
        // Act
        var title = MeetingContextParser.ParseTitle("   ");

        // Assert
        title.Should().BeEmpty();
    }

    #endregion

    #region ParseAgendaItems Tests

    [Fact]
    public void ParseAgendaItems_ShouldParseNumberedList()
    {
        // Arrange
        var context = @"Project Kickoff
1. Introduction
2. Project Overview
3. Timeline Discussion";

        // Act
        var items = MeetingContextParser.ParseAgendaItems(context);

        // Assert
        items.Should().HaveCount(3);
        items[0].Title.Should().Be("Introduction");
        items[0].Order.Should().Be(1);
        items[1].Title.Should().Be("Project Overview");
        items[1].Order.Should().Be(2);
        items[2].Title.Should().Be("Timeline Discussion");
        items[2].Order.Should().Be(3);
    }

    [Fact]
    public void ParseAgendaItems_ShouldParseBulletPointsAfterAgendaHeader()
    {
        // Arrange
        var context = @"Weekly Sync
Agenda:
- Status updates
- Blockers review
- Next steps";

        // Act
        var items = MeetingContextParser.ParseAgendaItems(context);

        // Assert
        items.Should().HaveCount(3);
        items[0].Title.Should().Be("Status updates");
        items[1].Title.Should().Be("Blockers review");
        items[2].Title.Should().Be("Next steps");
    }

    [Fact]
    public void ParseAgendaItems_ShouldSetPendingStatus()
    {
        // Arrange
        var context = "1. First item\n2. Second item";

        // Act
        var items = MeetingContextParser.ParseAgendaItems(context);

        // Assert
        items.Should().OnlyContain(i => i.Status == AgendaItemStatus.Pending);
    }

    [Fact]
    public void ParseAgendaItems_ShouldReturnEmpty_WhenNoAgendaItems()
    {
        // Arrange
        var context = "Just some meeting notes without any agenda";

        // Act
        var items = MeetingContextParser.ParseAgendaItems(context);

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public void ParseAgendaItems_ShouldReturnEmpty_WhenNullContext()
    {
        // Act
        var items = MeetingContextParser.ParseAgendaItems(null);

        // Assert
        items.Should().BeEmpty();
    }

    [Fact]
    public void ParseAgendaItems_ShouldParseTopicsHeader()
    {
        // Arrange
        var context = @"Topics:
- Architecture review
- Code quality";

        // Act
        var items = MeetingContextParser.ParseAgendaItems(context);

        // Assert
        items.Should().HaveCount(2);
    }

    #endregion

    #region ParseParticipants Tests

    [Fact]
    public void ParseParticipants_ShouldParseCommasSeparatedList()
    {
        // Arrange
        var context = @"Team Meeting
Attendees: Alice, Bob, Charlie";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants.Should().HaveCount(3);
        participants[0].DisplayName.Should().Be("Alice");
        participants[1].DisplayName.Should().Be("Bob");
        participants[2].DisplayName.Should().Be("Charlie");
    }

    [Fact]
    public void ParseParticipants_ShouldParseBulletList()
    {
        // Arrange
        var context = @"Meeting
Participants:
- John Smith
- Jane Doe";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants.Should().HaveCount(2);
        participants[0].DisplayName.Should().Be("John Smith");
        participants[1].DisplayName.Should().Be("Jane Doe");
    }

    [Fact]
    public void ParseParticipants_ShouldSetFirstAsOrganizer()
    {
        // Arrange
        var context = "Attendees: Leader, Member1, Member2";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants[0].Role.Should().Be(ParticipantRole.Organizer);
        participants[1].Role.Should().Be(ParticipantRole.Attendee);
        participants[2].Role.Should().Be(ParticipantRole.Attendee);
    }

    [Fact]
    public void ParseParticipants_ShouldHandlePeopleKeyword()
    {
        // Arrange
        var context = "People: Mike, Sarah";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants.Should().HaveCount(2);
    }

    [Fact]
    public void ParseParticipants_ShouldHandleInviteesKeyword()
    {
        // Arrange
        var context = "Invitees: Participant1, Participant2";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants.Should().HaveCount(2);
    }

    [Fact]
    public void ParseParticipants_ShouldReturnEmpty_WhenNoParticipants()
    {
        // Arrange
        var context = "A meeting with no participant list";

        // Act
        var participants = MeetingContextParser.ParseParticipants(context);

        // Assert
        participants.Should().BeEmpty();
    }

    [Fact]
    public void ParseParticipants_ShouldReturnEmpty_WhenNullContext()
    {
        // Act
        var participants = MeetingContextParser.ParseParticipants(null);

        // Assert
        participants.Should().BeEmpty();
    }

    #endregion

    #region ParseMeetingDetails Integration Tests

    [Fact]
    public void ParseMeetingDetails_ShouldParseCompleteContext()
    {
        // Arrange
        var context = @"Q4 Planning Session

Attendees: Product Manager, Engineering Lead, Designer

Agenda:
1. Review Q3 outcomes
2. Set Q4 goals
3. Resource allocation
4. Timeline discussion";

        // Act
        var (title, agendaItems, participants) = MeetingContextParser.ParseMeetingDetails(context);

        // Assert
        title.Should().Be("Q4 Planning Session");
        agendaItems.Should().HaveCount(4);
        participants.Should().HaveCount(3);
    }

    [Fact]
    public void ParseMeetingDetails_ShouldHandlePartialContext()
    {
        // Arrange
        var context = @"Weekly Standup
1. Status updates
2. Blockers";

        // Act
        var (title, agendaItems, participants) = MeetingContextParser.ParseMeetingDetails(context);

        // Assert
        title.Should().Be("Weekly Standup");
        agendaItems.Should().HaveCount(2);
        participants.Should().BeEmpty();
    }

    [Fact]
    public void ParseMeetingDetails_ShouldHandleTitleOnlyContext()
    {
        // Arrange
        var context = "Quick Sync";

        // Act
        var (title, agendaItems, participants) = MeetingContextParser.ParseMeetingDetails(context);

        // Assert
        title.Should().Be("Quick Sync");
        agendaItems.Should().BeEmpty();
        participants.Should().BeEmpty();
    }

    #endregion
}
