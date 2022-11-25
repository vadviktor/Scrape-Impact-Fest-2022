using Microsoft.Playwright;

namespace Scrape_Impact_Fest_2022;

class ImpactFest
{
    public bool Debug = false;
    private readonly string _baseUrl = "https://impactfest.app.swapcard.com";
    private readonly string _eventUrl = "https://impactfest.app.swapcard.com/event/impactfest-2022";
    private IBrowser _browser = null!;
    private IPage _page = null!;
    private readonly string _username = "xxx";
    private readonly string _password = "yyy";
    private Attendees _attendees = new Attendees();
    private const string Unknown = "N/A"; 

    public async Task Scrape()
    {
        await CreateBrowserAsync();
        await LoginAsync();
        await GoToAttendeesList();
        await FilterByInvestor();
        
        await _page.GetByText("Themes of interest").ClickAsync();
        
        await ToggleThemeOfInterestAsync("Energy transition"); // select
        var investorEnergyTransitionProfilePaths = await CollectProfileLinksAsync();
        await ToggleThemeOfInterestAsync("Energy transition"); // deselect
        
        // await ToggleThemeOfInterestAsync("Climate adaptation"); // select
        // var investorClimateAdaptationProfilePaths = await CollectProfileLinksAsync();
        
        await CollectInvestorDataAsync(investorEnergyTransitionProfilePaths);
        // await CollectInvestorDataAsync(investorClimateAdaptationProfilePaths);

        await _page.PauseAsync();
    }

    private async Task CreateBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();

        BrowserTypeLaunchOptions browserOptions = null!;
        if (Debug) browserOptions = new BrowserTypeLaunchOptions() { Headless = false, SlowMo = 1000 };

        _browser = await playwright.Chromium.LaunchAsync(browserOptions);
        _page = await _browser.NewPageAsync();
    }

    private async Task LoginAsync()
    {
        await _page.GotoAsync(_baseUrl);
        // Accept cookies.
        await _page.Locator("button", new() { HasText = "Accept all" }).ClickAsync();

        await _page
            .GetByRole(AriaRole.Navigation).GetByRole(AriaRole.Button, new() { NameString = "Log in" })
            .ClickAsync();
        await _page.Locator("#lookup-email-input-id").FillAsync(_username);
        await _page
            .Locator("form button:enabled", new() { HasText = "Continue" })
            .ClickAsync();
        await _page.Locator("#login-with-email-and-password-password-id").FillAsync(_password);

        // Submit and wait for the login request to finish.
        await _page.RunAndWaitForRequestFinishedAsync(async () =>
        {
            await _page
                .Locator("button:enabled", new() { HasText = "Continue" })
                .ClickAsync();
        });
    }

    private async Task ToggleThemeOfInterestAsync(string option)
    {
        var subMenu = _page.Locator("div>ul li:nth-of-type(2) span", new() { HasText = option });
        await subMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await _page.RunAndWaitForRequestFinishedAsync(async () => { await subMenu.ClickAsync(); });
    }

    private async Task GoToAttendeesList()
    {
        await _page.GotoAsync(_eventUrl);
        await _page
            .Locator("nav a", new() { HasText = "Network with all Attendees" })
            .ClickAsync();
    }

    private async Task FilterByInvestor()
    {
        await _page.GetByText("Profile Type").ClickAsync();
        await _page.RunAndWaitForRequestFinishedAsync(async () =>
        {
            await _page
                .Locator("div>ul li:nth-of-type(1) span", new() { HasText = "Investor" })
                .ClickAsync();
        });
    }

    private async Task<int> ResultsCount()
    {
        var resultCounter = _page.Locator("div>p", new() { HasText = " results" });
        await resultCounter.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var resultsText = await resultCounter.InnerTextAsync();
        var result = Convert.ToInt32(resultsText.Split(' ')[0]);
        return result;
    }

    private async Task<List<string>> CollectProfileLinksAsync()
    {
        var targetCount = await ResultsCount();
        var profileLinks = _page.Locator(".infinite-scroll-component>div>a");
        while (targetCount > await profileLinks.CountAsync())
        {
            await _page.Locator("body").PressAsync("PageDown");
        }

        var profileUrls = new List<string>();
        for (var i = 0; i < targetCount; i++)
        {
            var href = await profileLinks.Nth(i).GetAttributeAsync("href");
            if (href != null) profileUrls.Add(href);
        }

        return profileUrls;
    }

    private async Task CollectInvestorDataAsync(List<string> profilePaths)
    {
        foreach (var profilePath in profilePaths)
        {
            await _page.GotoAsync(_baseUrl + profilePath);
            var attendee = new Attendee();
            
            attendee.Name = await _page.Locator("//div[3]/div/div/div/div[2]/div/div[2]/h2").TextContentAsync() ?? Unknown;

            var jobs = _page.Locator("//div[3]/div/div/div/div[2]/div/div[2]/h2");
            attendee.Job = await jobs.First.TextContentAsync();
            
            _attendees[profilePath] = attendee;
        }
    }
}