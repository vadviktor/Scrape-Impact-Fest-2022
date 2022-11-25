using System.Collections;

namespace Scrape_Impact_Fest_2022;

public class Attendee
{
    public string Name { get; set; } = null!;
    public string Job { get; set; } = null!;
    public string Company { get; set; } = null!;
    public string? AboutMe { get; set; }
    public string? ProfileType { get; set; }
    public List<string>? ThemesOfInterest { get; set; }
    public List<string>? SocialMediaLinks { get; set; }
    public List<string>? ContactLinks { get; set; }
}
