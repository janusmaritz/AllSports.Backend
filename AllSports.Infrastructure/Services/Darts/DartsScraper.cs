using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Domain.Entities.Darts;
using HtmlAgilityPack;

namespace AllSports.Infrastructure.Services.Darts;

public class DartsScraper : IDartsScraper
{
    private static readonly string[] KnownBrands =
    [
        "Red Dragon", "Target", "Unicorn", "Winmau", "Harrows",
        "Shot", "Loxley", "Mission", "Bull's", "Cosmo", "One80",
        "Datadart", "XQ Max", "McDart", "Dynasty"
    ];

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
            profile.FullName = NormalizeText(nameNode.InnerText);

        profile.Nickname   = GetValueByLabel(container, "Nickname") ?? string.Empty;
        profile.DartsUsed  = GetValueByLabel(container, "Used Darts") ?? string.Empty;
        profile.WalkOnSong = GetValueByLabel(container, "Walk-on") ?? string.Empty;

        var ageText = GetValueByLabel(container, "Age");
        if (ageText is not null && int.TryParse(ageText, out int age))
            profile.Age = age;

        // Try a dedicated label first; fall back to parsing DartsUsed
        profile.DartBrand  = GetValueByLabel(container, "Darts Brand") ?? ExtractBrand(profile.DartsUsed);
        profile.DartWeight = GetValueByLabel(container, "Dart Weight") ?? ExtractWeight(profile.DartsUsed);

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
            return rankings;

        var scrapedAtUtc = DateTime.UtcNow;

        foreach (var node in rankingNodes)
        {
            var rankText = HtmlEntity.DeEntitize(node.SelectSingleNode("./span[1]")?.InnerText ?? string.Empty).Trim();
            var playerText = NormalizeText(
                node.SelectSingleNode(".//a[contains(@href, '/players/')]/span")?.InnerText
                ?? node.SelectSingleNode(".//a[contains(@href, '/players/')]")?.InnerText
                ?? string.Empty);
            var moneyText = HtmlEntity.DeEntitize(node.SelectSingleNode("./span[contains(@class, 'ml-auto')]")?.InnerText ?? string.Empty).Trim();

            if (!TryParseRank(rankText, out var rank)
                || string.IsNullOrWhiteSpace(playerText)
                || !TryParseMoneyAmount(moneyText, out var moneyAmount))
            {
                continue;
            }

            rankings.Add(new DartsRanking
            {
                Rank          = rank,
                PlayerName    = playerText,
                MoneyAmount   = moneyAmount,
                SourceUrl     = url,
                ScrapedAtUtc  = scrapedAtUtc
            });
        }

        return rankings;
    }

    private static string? GetValueByLabel(HtmlNode parentContainer, string labelText)
    {
        var node = parentContainer.SelectSingleNode($".//div[contains(text(), '{labelText}')]/following-sibling::div[1]");
        return node is not null ? NormalizeText(node.InnerText) : null;
    }

    private static string NormalizeText(string value) =>
        WebUtility.HtmlDecode(value).Normalize(NormalizationForm.FormC).Trim();

    private static string? ExtractBrand(string dartsUsed)
    {
        if (string.IsNullOrWhiteSpace(dartsUsed)) return null;
        var lower = dartsUsed.ToLowerInvariant();
        foreach (var brand in KnownBrands)
        {
            if (lower.Contains(brand.ToLowerInvariant())) return brand;
        }
        return null;
    }

    private static string? ExtractWeight(string dartsUsed)
    {
        if (string.IsNullOrWhiteSpace(dartsUsed)) return null;
        // Matches "23g", "23 gram", "21.5g", "21,5 Gram" (European decimal comma)
        var match = Regex.Match(dartsUsed, @"(\d+(?:[.,]\d+)?)\s*g(?:ram)?", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        return match.Groups[1].Value.Replace(',', '.') + "g";
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
