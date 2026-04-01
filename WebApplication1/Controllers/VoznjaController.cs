using CarPooling.Services;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace CarPooling.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoznjeController : ControllerBase
{
    private readonly PresedanjeService1 _transferService1;
    private readonly ScoringService _scoringService;

    public VoznjeController(   PresedanjeService1 service1, ScoringService scoringService )
    {
        _transferService1 = service1;
        _scoringService = scoringService;
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await _transferService1.GetAllCities();
        return Ok(cities);
    }

    [HttpGet]
    public async Task<IActionResult> FindRoutes(
        [FromQuery] string odGrada,
        [FromQuery] string doGrada,
        [FromQuery] string vreme,
        [FromQuery] int maxPresedanja = 1)
    {
        if (string.IsNullOrWhiteSpace(odGrada) || string.IsNullOrWhiteSpace(doGrada) || string.IsNullOrWhiteSpace(vreme))
            return BadRequest("Parameters fromCity, toCity, and departureTime are required.");

        if (maxPresedanja < 0 || maxPresedanja > 3)
            return BadRequest("maxTransfers must be between 0 and 3.");


        var requestedTime = DateTime.Parse(vreme);
        var routes = await _transferService1.FindRoutes(odGrada, doGrada, vreme, maxPresedanja);
        var result = _scoringService.RankAllRoutes(routes, requestedTime);
        return Ok(result);
    }
}