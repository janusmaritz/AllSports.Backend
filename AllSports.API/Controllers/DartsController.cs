using AllSports.API.Requests;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Responses;
using AllSports.Domain.Entities.Darts;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DartsController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public DartsController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public ActionResult<List<DartsMatch>> GetMatches()
    {
        var matches = new List<DartsMatch>
        {
            new DartsMatch
            {
                Id = 1,
                TournamentName = "World Darts Championship",
                MatchDate = DateTime.Now.AddDays(1),
                PlayerOneName = "Michael van Gerwen",
                PlayerTwoName = "Luke Littler",
                IsFinished = false
            },
            new DartsMatch
            {
                Id = 2,
                TournamentName = "Premier League Night 1",
                MatchDate = DateTime.Now.AddDays(-1),
                PlayerOneName = "Gerwyn Price",
                PlayerTwoName = "Michael Smith",
                PlayerOneScore = 6,
                PlayerTwoScore = 4,
                IsFinished = true
            }
        };

        return Ok(matches);
    }

    [HttpPost("scrape-profile")]
    public async Task<ActionResult<PlayerProfile>> ScrapeAndSave([FromBody] ScrapeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("You must provide a URL.");
        }

        try
        {
            var result = await _playerService.ImportPlayerFromUrlAsync(request.Url);

            return CreatedAtAction(nameof(GetMatches), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("scrape-bulk")]
    public async Task<ActionResult<BulkImportResult>> ScrapeBulk([FromBody] BulkScrapeRequest request)
    {
        if (request.Urls == null || !request.Urls.Any())
        {
            return BadRequest("No URLs provided.");
        }

        var result = await _playerService.ImportPlayersAsync(request.Urls);

        return Ok(result);
    }

    [HttpGet("players")]
    public async Task<ActionResult<List<PlayerProfile>>> GetAllPlayers()
    {
        var players = await _playerService.GetAllPlayersAsync();
        return Ok(players);
    }
}