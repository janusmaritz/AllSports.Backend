using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Domain.Entities.Darts;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace AllSports.Infrastructure.Services.Darts;

public class DartsScraper : IDartsScraper
{
    public async Task<PlayerProfile> ScrapePlayerAsync(string url)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(url);

        var profile = new PlayerProfile();

        var containerPath = "/html/body/div[2]/div/div/div[2]/div[2]/div[1]/div[1]/div/div";
        var container = doc.DocumentNode.SelectSingleNode(containerPath);

        if (container == null) return null;

        var nameNode = container.SelectSingleNode(".//h1");
        if (nameNode != null)
        {
            profile.FullName = nameNode.InnerText.Trim();
        }

        // Nickname
        profile.Nickname = GetValueByLabel(container, "Nickname");

        // Age
        var ageText = GetValueByLabel(container, "Age");
        if (int.TryParse(ageText, out int age))
        {
            profile.Age = age;
        }

        // Darts Used
        profile.DartsUsed = GetValueByLabel(container, "Used Darts");

        // Walk-on Song (Searching for text "Walk-on" covers "Walk-on Music (Song)")
        profile.WalkOnSong = GetValueByLabel(container, "Walk-on");

        return profile;
    }

    private string GetValueByLabel(HtmlNode parentContainer, string labelText)
    {
        var node = parentContainer.SelectSingleNode($".//div[contains(text(), '{labelText}')]/following-sibling::div[1]");

        return node != null ? HtmlEntity.DeEntitize(node.InnerText.Trim()) : "Unknown";
    }
}