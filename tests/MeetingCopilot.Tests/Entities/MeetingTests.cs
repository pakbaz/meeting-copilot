using FluentAssertions;
using MeetingCopilot.Contracts.Entities;

namespace MeetingCopilot.Tests.Entities;

public class MeetingTests
{
    [Fact]
    public void Meeting_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var meeting = new Meeting();

        // Assert
        meeting.Id.Should().NotBeNullOrEmpty();
        meeting.Title.Should().BeNullOrEmpty();
        meeting.Status.Should().Be(MeetingStatus.Setup);
        meeting.Participants.Should().NotBeNull().And.BeEmpty();
        meeting.AgendaItems.Should().NotBeNull().And.BeEmpty();
        meeting.Attachments.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Meeting_ShouldCreateWithCustomValues()
    {
        // Arrange & Act
        var meeting = new Meeting
        {
            Id = "test-meeting-123",
            Title = "Test Meeting",
            Context = "This is a test meeting context",
            Status = MeetingStatus.Active
        };

        // Assert
        meeting.Id.Should().Be("test-meeting-123");
        meeting.Title.Should().Be("Test Meeting");
        meeting.Context.Should().Be("This is a test meeting context");
        meeting.Status.Should().Be(MeetingStatus.Active);
    }

    [Fact]
    public void Meeting_ShouldSupportAgendaItems()
    {
        // Arrange
        var agendaItem1 = new AgendaItem { Title = "Introduction", Order = 1 };
        var agendaItem2 = new AgendaItem { Title = "Discussion", Order = 2 };

        // Act
        var meeting = new Meeting
        {
            Title = "Meeting with Agenda",
            AgendaItems = new List<AgendaItem> { agendaItem1, agendaItem2 }
        };

        // Assert
        meeting.AgendaItems.Should().HaveCount(2);
        meeting.AgendaItems[0].Title.Should().Be("Introduction");
        meeting.AgendaItems[1].Title.Should().Be("Discussion");
    }

    [Fact]
    public void Meeting_ShouldSupportParticipants()
    {
        // Arrange
        var participant1 = new Participant { DisplayName = "Alice", Role = ParticipantRole.Organizer };
        var participant2 = new Participant { DisplayName = "Bob", Role = ParticipantRole.Attendee };

        // Act
        var meeting = new Meeting
        {
            Title = "Meeting with Participants",
            Participants = new List<Participant> { participant1, participant2 }
        };

        // Assert
        meeting.Participants.Should().HaveCount(2);
        meeting.Participants[0].DisplayName.Should().Be("Alice");
        meeting.Participants[0].Role.Should().Be(ParticipantRole.Organizer);
        meeting.Participants[1].DisplayName.Should().Be("Bob");
        meeting.Participants[1].Role.Should().Be(ParticipantRole.Attendee);
    }

    [Fact]
    public void Meeting_ShouldTrackStartTime()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow;

        // Act
        var meeting = new Meeting
        {
            Title = "Started Meeting",
            StartedAt = startTime
        };

        // Assert
        meeting.StartedAt.Should().Be(startTime);
    }
}

public class AgendaItemTests
{
    [Fact]
    public void AgendaItem_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var item = new AgendaItem { Title = "Test Item" };

        // Assert
        item.Id.Should().NotBeNullOrEmpty();
        item.Title.Should().Be("Test Item");
        item.Status.Should().Be(AgendaItemStatus.Pending);
        item.Order.Should().Be(0);
    }

    [Fact]
    public void AgendaItem_ShouldSupportAllStatuses()
    {
        // Arrange & Act
        var pendingItem = new AgendaItem { Title = "Pending", Status = AgendaItemStatus.Pending };
        var inProgressItem = new AgendaItem { Title = "In Progress", Status = AgendaItemStatus.InProgress };
        var completedItem = new AgendaItem { Title = "Completed", Status = AgendaItemStatus.Completed };

        // Assert
        pendingItem.Status.Should().Be(AgendaItemStatus.Pending);
        inProgressItem.Status.Should().Be(AgendaItemStatus.InProgress);
        completedItem.Status.Should().Be(AgendaItemStatus.Completed);
    }

    [Fact]
    public void AgendaItem_WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var original = new AgendaItem 
        { 
            Title = "Original", 
            Status = AgendaItemStatus.Pending,
            Order = 1 
        };

        // Act
        var updated = original with { Status = AgendaItemStatus.Completed };

        // Assert
        original.Status.Should().Be(AgendaItemStatus.Pending);
        updated.Status.Should().Be(AgendaItemStatus.Completed);
        updated.Title.Should().Be("Original");
        updated.Order.Should().Be(1);
        updated.Id.Should().Be(original.Id);
    }
}

public class ParticipantTests
{
    [Fact]
    public void Participant_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var participant = new Participant { DisplayName = "Test User" };

        // Assert
        participant.Id.Should().NotBeNullOrEmpty();
        participant.DisplayName.Should().Be("Test User");
        participant.Role.Should().Be(ParticipantRole.Attendee);
    }

    [Fact]
    public void Participant_ShouldSupportAllRoles()
    {
        // Arrange & Act
        var organizer = new Participant { DisplayName = "Organizer", Role = ParticipantRole.Organizer };
        var presenter = new Participant { DisplayName = "Presenter", Role = ParticipantRole.Presenter };
        var attendee = new Participant { DisplayName = "Attendee", Role = ParticipantRole.Attendee };

        // Assert
        organizer.Role.Should().Be(ParticipantRole.Organizer);
        presenter.Role.Should().Be(ParticipantRole.Presenter);
        attendee.Role.Should().Be(ParticipantRole.Attendee);
    }
}
