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

    public async Task<List<DartsTournament>> ScrapeTournamentsAsync(string url)
    {
        // The calendar page is ~40 MB, which can exceed HtmlWeb's fixed 100-second
        // HttpClient timeout — download it ourselves with a more generous limit.
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var html = await httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // The calendar page interleaves month headings (<h3>January 2026</h3>) with
        // tournament rows. Select both in document order and track the current month.
        var nodes = doc.DocumentNode.SelectNodes(
            "//h3[contains(@class, 'inline-flex')] | " +
            "//div[contains(concat(' ', normalize-space(@class), ' '), ' py-3 ')]" +
            "[.//a[contains(@href, '/tournaments/')]]");

        var tournaments = new List<DartsTournament>();
        if (nodes == null)
            return tournaments;

        var scrapedAtUtc = DateTime.UtcNow;
        DateOnly? currentMonth = null;
        var seen = new HashSet<string>();

        foreach (var node in nodes)
        {
            if (node.Name == "h3")
            {
                currentMonth = ParseMonthHeading(NormalizeText(node.InnerText));
                continue;
            }

            // Rows before the first month heading (e.g. the featured slider) are skipped.
            if (currentMonth is null) continue;

            var link = node.SelectSingleNode(".//a[contains(@href, '/tournaments/')]");
            var name = NormalizeText(link?.SelectSingleNode(".//h2")?.InnerText ?? link?.InnerText ?? string.Empty);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var locationNode = node.SelectSingleNode(".//img[contains(@src, '/images/flags/')]/following-sibling::span[1]");
            var location = locationNode is not null ? NormalizeText(locationNode.InnerText) : string.Empty;

            var dateNode = node.SelectSingleNode(".//div[contains(@class, 'text-sm')]/div[last()]");
            var dateText = dateNode is not null ? NormalizeText(dateNode.InnerText) : string.Empty;
            if (!TryParseDateRange(dateText, currentMonth.Value, out var startDate, out var endDate)) continue;

            // Multi-month tournaments can be listed under more than one month heading.
            if (!seen.Add($"{name}|{startDate:O}")) continue;

            tournaments.Add(new DartsTournament
            {
                Name         = name,
                Location     = location,
                Organisation = DetectOrganisation(node),
                StartDate    = startDate,
                EndDate      = endDate,
                DetailUrl    = link?.GetAttributeValue("href", string.Empty) ?? string.Empty,
                SourceUrl    = url,
                ScrapedAtUtc = scrapedAtUtc
            });
        }

        return tournaments;
    }

    private static DateOnly? ParseMonthHeading(string headingText)
    {
        // e.g. "January 2026"
        if (DateTime.TryParseExact(headingText, "MMMM yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var month))
        {
            return DateOnly.FromDateTime(month);
        }
        return null;
    }

    private static bool TryParseDateRange(string dateText, DateOnly month, out DateOnly startDate, out DateOnly endDate)
    {
        // e.g. "5 Jan" or "5 Jan - 11 Jan" or "28 Dec - 3 Jan" (spanning a year boundary)
        startDate = endDate = default;

        var parts = dateText.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 0 or > 2) return false;

        if (!TryParseDayMonth(parts[0], month.Year, out startDate)) return false;

        // A tournament listed under January can start in the previous December (and vice versa).
        var monthGap = startDate.Month - month.Month;
        if (monthGap > 6) startDate = startDate.AddYears(-1);
        else if (monthGap < -6) startDate = startDate.AddYears(1);

        endDate = startDate;
        if (parts.Length == 2)
        {
            if (!TryParseDayMonth(parts[1], startDate.Year, out endDate)) return false;
            if (endDate < startDate) endDate = endDate.AddYears(1);
        }

        return true;
    }

    private static bool TryParseDayMonth(string value, int year, out DateOnly date)
    {
        date = default;
        if (!DateTime.TryParseExact($"{value} {year}", "d MMM yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
        {
            return false;
        }
        date = DateOnly.FromDateTime(parsed);
        return true;
    }

    private static string DetectOrganisation(HtmlNode row)
    {
        // Organisation is only indicated by an inline SVG logo in the row's first cell:
        // the WDF logo carries id="WDF_Logo…", the PDC logo is identified by its viewBox width.
        var logoHtml = row.SelectSingleNode("./div[1]")?.InnerHtml ?? string.Empty;
        if (logoHtml.Contains("WDF_Logo", StringComparison.OrdinalIgnoreCase)) return "WDF";
        if (logoHtml.Contains("324.629", StringComparison.Ordinal)) return "PDC";
        return string.Empty;
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
