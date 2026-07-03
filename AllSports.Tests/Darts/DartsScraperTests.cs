using System.Net;
using AllSports.Domain.Entities.Darts;
using AllSports.Infrastructure.Services.Darts;
using Xunit;

namespace AllSports.Tests.Darts;

public class DartsScraperTests : IAsyncLifetime
{
    private List<DartsTournament> _tournaments = [];

    public async Task InitializeAsync()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Darts", "Fixtures", "mastercaller-calendar.html");
        _tournaments = await ScrapeServedFixtureAsync(fixturePath);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public void ParsesExpectedNumberOfTournaments()
    {
        // 5 rows after the first month heading, one of which is a duplicate.
        Assert.Equal(4, _tournaments.Count);
    }

    [Fact]
    public void SkipsRowsBeforeFirstMonthHeading()
    {
        Assert.DoesNotContain(_tournaments, t => t.Name == "Featured Slider Tournament");
    }

    [Fact]
    public void ParsesSingleDayTournament()
    {
        var t = Assert.Single(_tournaments, t => t.Name == "Q-School 2026 QF-01 EU");
        Assert.Equal(new DateOnly(2026, 1, 5), t.StartDate);
        Assert.Equal(new DateOnly(2026, 1, 5), t.EndDate);
        Assert.Equal("Kalkar, DEU", t.Location);
        Assert.Equal("PDC", t.Organisation);
        Assert.Equal("https://mastercaller.com/tournaments/q-school/2026-qf-01-eu", t.DetailUrl);
    }

    [Fact]
    public void ParsesMultiDayTournamentAndDeduplicatesRepeatedMonths()
    {
        var t = Assert.Single(_tournaments, t => t.Name == "Q-School Ranking Tour Card 2026-UK TourCard");
        Assert.Equal(new DateOnly(2026, 1, 5), t.StartDate);
        Assert.Equal(new DateOnly(2026, 1, 11), t.EndDate);
        Assert.Equal(string.Empty, t.Organisation);
    }

    [Fact]
    public void ParsesTournamentStartingInPreviousYear()
    {
        var t = Assert.Single(_tournaments, t => t.Name == "WDF World Championship");
        Assert.Equal(new DateOnly(2025, 12, 28), t.StartDate);
        Assert.Equal(new DateOnly(2026, 1, 3), t.EndDate);
        Assert.Equal("WDF", t.Organisation);
    }

    [Fact]
    public void ParsesTournamentEndingInNextYear()
    {
        var t = Assert.Single(_tournaments, t => t.Name == "PDC World Darts Championship");
        Assert.Equal(new DateOnly(2026, 12, 10), t.StartDate);
        Assert.Equal(new DateOnly(2027, 1, 3), t.EndDate);
        Assert.Equal("PDC", t.Organisation);
    }

    [Fact]
    public void SetsSourceUrlAndScrapedAt()
    {
        Assert.All(_tournaments, t =>
        {
            Assert.StartsWith("http://localhost:", t.SourceUrl);
            Assert.NotEqual(default, t.ScrapedAtUtc);
        });
    }

    /// <summary>
    /// HtmlWeb only loads over HTTP, so serve the fixture from a local listener.
    /// </summary>
    private static async Task<List<DartsTournament>> ScrapeServedFixtureAsync(string fixturePath)
    {
        var bytes = await File.ReadAllBytesAsync(fixturePath);

        using var listener = StartListener(out var port);
        var serveLoop = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
                catch (Exception) when (!listener.IsListening)
                {
                    break;
                }
            }
        });

        try
        {
            var scraper = new DartsScraper();
            return await scraper.ScrapeTournamentsAsync($"http://localhost:{port}/calendar");
        }
        finally
        {
            listener.Stop();
            await Task.WhenAny(serveLoop, Task.Delay(1000));
        }
    }

    private static HttpListener StartListener(out int port)
    {
        for (var attempt = 0; ; attempt++)
        {
            port = Random.Shared.Next(20000, 60000);
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                listener.Start();
                return listener;
            }
            catch (HttpListenerException) when (attempt < 10)
            {
                // Port in use — try another.
            }
        }
    }
}
