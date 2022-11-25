namespace Scrape_Impact_Fest_2022;

static class Program
{
    static async Task Main()
    {
        var scraper = new ImpactFest
        {
            Debug = true
        };
        await scraper.Scrape();
    }
}