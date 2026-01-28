using AllSports.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DartsController : ControllerBase
{
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
}