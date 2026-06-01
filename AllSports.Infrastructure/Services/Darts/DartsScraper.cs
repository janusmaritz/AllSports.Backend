using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Domain.Entities.Darts;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AllSports.Infrastructure.Services.Darts;

public class DartsScraper : IDartsScraper
{
    public async Task<PlayerProfile?> ScrapePlayerAsync(string url)
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

    public async Task<List<DartsRanking>> ScrapeRankingsAsync(string url)
    {
        var web = new HtmlWeb();
        var doc = await web.LoadFromWebAsync(url);

        var rankingNodes = doc.DocumentNode.SelectNodes(
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' flex ') " +
            "and contains(concat(' ', normalize-space(@class), ' '), ' items-center ') " +
            "and .//a[contains(@href, '/players/')] " +
            "and .//span[contains(@class, 'ml-auto')]]");

        var rankings = new List<DartsRanking>();
        if (rankingNodes == null)
        {
            return rankings;
        }

        var scrapedAtUtc = DateTime.UtcNow;

        foreach (var node in rankingNodes)
        {
            var rankText = HtmlEntity.DeEntitize(node.SelectSingleNode("./span[1]")?.InnerText ?? string.Empty).Trim();
            var playerText = HtmlEntity.DeEntitize(
                node.SelectSingleNode(".//a[contains(@href, '/players/')]/span")?.InnerText
                ?? node.SelectSingleNode(".//a[contains(@href, '/players/')]")?.InnerText
                ?? string.Empty).Trim();
            var moneyText = HtmlEntity.DeEntitize(node.SelectSingleNode("./span[contains(@class, 'ml-auto')]")?.InnerText ?? string.Empty).Trim();

            if (!TryParseRank(rankText, out var rank)
                || string.IsNullOrWhiteSpace(playerText)
                || !TryParseMoneyAmount(moneyText, out var moneyAmount))
            {
                continue;
            }

            rankings.Add(new DartsRanking
            {
                Rank = rank,
                PlayerName = playerText,
                MoneyAmount = moneyAmount,
                SourceUrl = url,
                ScrapedAtUtc = scrapedAtUtc
            });
        }

        return rankings;
    }

    private string GetValueByLabel(HtmlNode parentContainer, string labelText)
    {
        var node = parentContainer.SelectSingleNode($".//div[contains(text(), '{labelText}')]/following-sibling::div[1]");

        return node != null ? HtmlEntity.DeEntitize(node.InnerText.Trim()) : "Unknown";
    }

    private static bool TryParseRank(string value, out int rank)
    {
        var match = Regex.Match(value, @"\d+");

        return int.TryParse(match.Value, out rank);
    }

    private static bool TryParseMoneyAmount(string value, out decimal moneyAmount)
    {
        var numericValue = Regex.Replace(value, @"[^\d.]", string.Empty);

        return decimal.TryParse(numericValue, NumberStyles.Number, CultureInfo.InvariantCulture, out moneyAmount);
    }
}
