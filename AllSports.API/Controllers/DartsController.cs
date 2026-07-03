using AllSports.API.Requests.Darts;
using AllSports.Application.Common.Pagination;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Queries.Darts;
using AllSports.Application.Responses;
using AllSports.Domain.Entities.Darts;
using Microsoft.AspNetCore.Mvc;

namespace AllSports.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DartsController : ControllerBase
{
    private readonly IPlayerService _playerService;
    private readonly IDartsRankingService _rankingService;
    private readonly IDartsTournamentService _tournamentService;

    public DartsController(
        IPlayerService playerService,
        IDartsRankingService rankingService,
        IDartsTournamentService tournamentService)
    {
        _playerService = playerService;
        _rankingService = rankingService;
        _tournamentService = tournamentService;
    }

    [HttpGet]
    public ActionResult<List<DartsMatch>> GetMatches()
    {
        return Ok(Array.Empty<DartsMatch>());
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

            return CreatedAtAction(nameof(GetPlayer), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("scrape-bulk")]
    public async Task<ActionResult<BulkImportResult>> ScrapeBulk([FromBody] BulkScrapeRequest request)
    {
        if (request.Urls.Count == 0)
        {
            return BadRequest("No URLs provided.");
        }

        var result = await _playerService.ImportPlayersAsync(request.Urls);

        return Ok(result);
    }

    [HttpGet("players/{id:int}")]
    public async Task<ActionResult<PlayerProfile>> GetPlayer(int id)
    {
        var player = await _playerService.GetPlayerByIdAsync(id);
        if (player is null) return NotFound();
        return Ok(player);
    }

    [HttpGet("players")]
    public async Task<ActionResult<PagedResult<PlayerProfile>>> GetAllPlayers([FromQuery] PlayerQuery query)
    {
        return Ok(await _playerService.GetPlayersAsync(query));
    }

    [HttpPost("scrape-rankings")]
    public async Task<ActionResult<List<DartsRanking>>> ScrapeRankings([FromBody] ScrapeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("You must provide a URL.");
        }

        try
        {
            var result = await _rankingService.ImportRankingsFromUrlAsync(request.Url);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("rankings")]
    public async Task<ActionResult<PagedResult<DartsRanking>>> GetAllRankings([FromQuery] RankingQuery query)
    {
        return Ok(await _rankingService.GetRankingsAsync(query));
    }

    [HttpPost("scrape-tournaments")]
    public async Task<ActionResult<List<DartsTournament>>> ScrapeTournaments([FromBody] ScrapeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("You must provide a URL.");
        }

        try
        {
            var result = await _tournamentService.ImportTournamentsFromUrlAsync(request.Url);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("tournaments")]
    public async Task<ActionResult<PagedResult<DartsTournament>>> GetAllTournaments([FromQuery] TournamentQuery query)
    {
        return Ok(await _tournamentService.GetTournamentsAsync(query));
    }
}
