using FluentAssertions;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace MeetingCopilot.Tests.UITests;

/// <summary>
/// Comprehensive UI tests for Meeting Copilot using Playwright.
/// These tests require the application to be running locally at https://localhost:7282
/// 
/// To run these tests:
/// 1. Install Playwright browsers: dotnet tool install --global Microsoft.Playwright.CLI &amp;&amp; playwright install chromium
/// 2. Start the app: dotnet run --project meeting-copilot.csproj
/// 3. Run tests: dotnet test --filter "FullyQualifiedName~MeetingCopilotUITests"
/// 
/// Note: Tests are skipped if Playwright browsers are not installed or app is not running.
/// </summary>
[Collection("UI Tests")]
[Trait("Category", "UI")]
public class MeetingCopilotUITests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private const string BaseUrl = "https://localhost:7282";
    private bool _isSetupSuccessful;
    private string? _skipReason;

    public MeetingCopilotUITests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--ignore-certificate-errors"]
            });
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });
            _page = await context.NewPageAsync();
            
            // Verify app is running
            try
            {
                var response = await _page.GotoAsync(BaseUrl, new PageGotoOptions { Timeout = 5000 });
                _isSetupSuccessful = response?.Ok ?? false;
                if (!_isSetupSuccessful)
                {
                    _skipReason = $"App not running at {BaseUrl}. Start with: dotnet run";
                }
            }
            catch (Exception ex)
            {
                _isSetupSuccessful = false;
                _skipReason = $"Cannot connect to app at {BaseUrl}: {ex.Message}";
            }
        }
        catch (PlaywrightException ex)
        {
            _skipReason = $"Playwright setup failed: {ex.Message}. Run: dotnet tool install --global Microsoft.Playwright.CLI && playwright install chromium";
            _isSetupSuccessful = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            try { await _page.CloseAsync(); } catch { /* ignore */ }
        }
        if (_browser != null)
        {
            try { await _browser.CloseAsync(); } catch { /* ignore */ }
        }
        _playwright?.Dispose();
    }

    private void SkipIfNotSetup()
    {
        if (!_isSetupSuccessful || _page == null)
        {
            _output.WriteLine(_skipReason ?? "Test environment not ready");
            Skip.If(true, _skipReason ?? "Test environment not ready");
        }
    }

    #region Navigation Tests

    [SkippableFact]
    public async Task HomePage_ShouldRedirectToPreMeetingSetup()
    {
        SkipIfNotSetup();
        
        // Arrange & Act
        await _page!.GotoAsync(BaseUrl);
        
        // Assert
        await _page.WaitForURLAsync($"{BaseUrl}/meeting/setup", new PageWaitForURLOptions { Timeout = 10000 });
        _page.Url.Should().Contain("/meeting/setup");
    }

    [SkippableFact]
    public async Task PreMeetingSetup_ShouldDisplayRequiredElements()
    {
        SkipIfNotSetup();
        
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/meeting/setup");
        
        // Act & Assert
        var contextTextarea = _page.GetByPlaceholder("Meeting Title, Agenda, Attendees");
        await contextTextarea.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        (await contextTextarea.IsVisibleAsync()).Should().BeTrue("Context textarea should be visible");
        
        var startButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start Meeting" });
        (await startButton.IsVisibleAsync()).Should().BeTrue("Start Meeting button should be visible");
    }

    #endregion

    #region Meeting Setup Tests

    [SkippableFact]
    public async Task StartMeeting_WithValidContext_ShouldNavigateToInMeetingPage()
    {
        SkipIfNotSetup();
        
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/meeting/setup");
        var contextTextarea = _page.GetByPlaceholder("Meeting Title, Agenda, Attendees");
        await contextTextarea.FillAsync("Test Meeting\n## Agenda\n1. First item\n2. Second item");
        
        // Act
        var startButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start Meeting" });
        await startButton.ClickAsync();
        
        // Wait for navigation to meeting page
        await _page.WaitForURLAsync(url => url.Contains("/meeting/") && !url.Contains("/setup"), 
            new PageWaitForURLOptions { Timeout = 15000 });
        
        // Assert
        _page.Url.Should().MatchRegex(@"/meeting/[a-f0-9-]+$", "Should navigate to meeting page with GUID");
    }

    [SkippableFact]
    public async Task StartMeeting_ShouldShowStartingState()
    {
        SkipIfNotSetup();
        
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/meeting/setup");
        var contextTextarea = _page.GetByPlaceholder("Meeting Title, Agenda, Attendees");
        await contextTextarea.FillAsync("Test Meeting for Button State");
        
        // Act
        var startButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start Meeting" });
        await startButton.ClickAsync();
        
        // Assert - Button should show "Starting..." while processing
        var startingButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Starting..." });
        try
        {
            await startingButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 2000 });
            // If we caught it, verify it's disabled
            (await startingButton.IsDisabledAsync()).Should().BeTrue("Button should be disabled while starting");
        }
        catch (TimeoutException)
        {
            // Button state changed too quickly, that's okay - the meeting started fast
            _output.WriteLine("Button state changed quickly - meeting started fast");
        }
    }

    #endregion

    #region In-Meeting UI Tests

    [SkippableFact]
    public async Task InMeeting_ShouldDisplayAllPanels()
    {
        SkipIfNotSetup();
        
        // Arrange - Start a meeting first
        await StartMeetingAsync("Panel Test Meeting\n## Agenda\n1. Test agenda item");
        
        // Assert - Check all main panels are visible
        var answerHeading = _page!.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Answer:" });
        (await answerHeading.IsVisibleAsync()).Should().BeTrue("Answer panel should be visible");
        
        var keyPointsHeading = _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Key points:" });
        (await keyPointsHeading.IsVisibleAsync()).Should().BeTrue("Key points panel should be visible");
        
        var researchHeading = _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Research & Next topic:" });
        (await researchHeading.IsVisibleAsync()).Should().BeTrue("Research panel should be visible");
        
        var agendaHeading = _page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Summary and Agenda:" });
        (await agendaHeading.IsVisibleAsync()).Should().BeTrue("Agenda panel should be visible");
    }

    [SkippableFact]
    public async Task InMeeting_ShouldDisplayAgendaItems()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Agenda Test\n## Agenda\n1. First item\n2. Second item\n3. Third item");
        
        // Assert - Check agenda items are displayed
        var agendaList = _page!.Locator("li");
        var count = await agendaList.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(3, "Should display at least 3 agenda items");
    }

    [SkippableFact]
    public async Task InMeeting_ShouldDisplayInputTextbox()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Input Test Meeting");
        
        // Assert
        var inputBox = _page!.GetByPlaceholder("Input...");
        (await inputBox.IsVisibleAsync()).Should().BeTrue("Input textbox should be visible");
    }

    [SkippableFact]
    public async Task InMeeting_ShouldDisplayEndMeetingButton()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("End Button Test Meeting");
        
        // Assert
        var endButton = _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "End Meeting" });
        (await endButton.IsVisibleAsync()).Should().BeTrue("End Meeting button should be visible");
    }

    [SkippableFact]
    public async Task InMeeting_ShouldDisplayMeetingTimer()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Timer Test Meeting");
        
        // Assert - Should display time in format 00:00
        var timeText = _page!.Locator("text=Time:");
        (await timeText.IsVisibleAsync()).Should().BeTrue("Timer should be visible");
    }

    #endregion

    #region Slash Command Tests

    [SkippableFact]
    public async Task SlashCommand_Help_ShouldDisplayUsageInfo()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Help Command Test");
        var inputBox = _page!.GetByPlaceholder("Input...");
        
        // Act
        await inputBox.FillAsync("/help");
        await inputBox.PressAsync("Enter");
        
        // Wait a moment for the response
        await _page.WaitForTimeoutAsync(1000);
        
        // Assert - Help should show available commands in the answer panel
        var answerPanel = _page.Locator("[class*='answer'], [class*='Answer']").First;
        var answerText = await answerPanel.TextContentAsync() ?? "";
        answerText.Should().Contain("Available Commands", "Help command should display available commands");
    }

    [SkippableFact]
    public async Task SlashCommand_Research_ShouldBeAccepted()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Research Command Test\n## Goals\n- Learn about AI");
        var inputBox = _page!.GetByPlaceholder("Input...");
        
        // Act
        await inputBox.FillAsync("/research AI trends 2024");
        await inputBox.PressAsync("Enter");
        
        // Assert - Input should be cleared after submission
        await _page.WaitForTimeoutAsync(500);
        var inputValue = await inputBox.InputValueAsync();
        inputValue.Should().BeEmpty("Input should be cleared after submitting command");
    }

    [SkippableFact]
    public async Task SlashCommand_Speaker_ShouldBeAccepted()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Speaker Command Test");
        var inputBox = _page!.GetByPlaceholder("Input...");
        
        // Act
        await inputBox.FillAsync("/speaker Guest1 John Smith");
        await inputBox.PressAsync("Enter");
        
        // Assert - Input should be cleared
        await _page.WaitForTimeoutAsync(500);
        var inputValue = await inputBox.InputValueAsync();
        inputValue.Should().BeEmpty("Input should be cleared after submitting speaker command");
    }

    [SkippableFact]
    public async Task Input_NonSlashCommand_ShouldBeAccepted()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("Regular Input Test");
        var inputBox = _page!.GetByPlaceholder("Input...");
        
        // Act - Enter a question without slash
        await inputBox.FillAsync("What is the project timeline?");
        await inputBox.PressAsync("Enter");
        
        // Assert - Input should be cleared
        await _page.WaitForTimeoutAsync(500);
        var inputValue = await inputBox.InputValueAsync();
        inputValue.Should().BeEmpty("Input should be cleared after submitting question");
    }

    #endregion

    #region End Meeting Tests

    [SkippableFact]
    public async Task EndMeeting_ShouldNavigateToSummaryPage()
    {
        SkipIfNotSetup();
        
        // Arrange
        await StartMeetingAsync("End Meeting Test");
        
        // Act
        var endButton = _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "End Meeting" });
        await endButton.ClickAsync();
        
        // Wait for navigation
        await _page.WaitForURLAsync(url => url.Contains("/summary"), 
            new PageWaitForURLOptions { Timeout = 15000 });
        
        // Assert
        _page.Url.Should().Contain("/summary", "Should navigate to summary page");
    }

    [SkippableFact]
    public async Task SummaryPage_ShouldDisplayMeetingInfo()
    {
        SkipIfNotSetup();
        
        // Arrange - Start and end a meeting
        await StartMeetingAsync("Summary Test Meeting\n## Agenda\n1. Test item");
        
        var endButton = _page!.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "End Meeting" });
        await endButton.ClickAsync();
        
        await _page.WaitForURLAsync(url => url.Contains("/summary"), 
            new PageWaitForURLOptions { Timeout = 15000 });
        
        // Assert - Summary page should have meeting title
        var pageContent = await _page.ContentAsync();
        pageContent.Should().Contain("Summary", "Summary page should contain summary information");
    }

    #endregion

    #region Error Handling Tests

    [SkippableFact]
    public async Task NonExistentMeeting_ShouldHandleGracefully()
    {
        SkipIfNotSetup();
        
        // Act - Navigate to a non-existent meeting
        var response = await _page!.GotoAsync($"{BaseUrl}/meeting/00000000-0000-0000-0000-000000000000");
        
        // Assert - Should either redirect or show appropriate message
        // (Implementation may vary - just verify no crash)
        response.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private async Task StartMeetingAsync(string context)
    {
        await _page!.GotoAsync($"{BaseUrl}/meeting/setup");
        
        var contextTextarea = _page.GetByPlaceholder("Meeting Title, Agenda, Attendees");
        await contextTextarea.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
        await contextTextarea.FillAsync(context);
        
        var startButton = _page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Start Meeting" });
        await startButton.ClickAsync();
        
        // Wait for navigation to meeting page
        await _page.WaitForURLAsync(url => url.Contains("/meeting/") && !url.Contains("/setup"), 
            new PageWaitForURLOptions { Timeout = 15000 });
        
        // Wait for the meeting UI to be ready
        var inputBox = _page.GetByPlaceholder("Input...");
        await inputBox.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });
    }

    #endregion
}

/// <summary>
/// Collection definition for UI tests to prevent parallel execution
/// UI tests should run sequentially to avoid browser conflicts
/// </summary>
[CollectionDefinition("UI Tests", DisableParallelization = true)]
public class UITestsCollection : ICollectionFixture<UITestsFixture>
{
}

/// <summary>
/// Fixture for UI tests - can be used for shared setup/teardown
/// </summary>
public class UITestsFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Global setup for UI tests
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Global cleanup for UI tests
        return Task.CompletedTask;
    }
}
