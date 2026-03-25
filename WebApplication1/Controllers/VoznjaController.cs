using Microsoft.AspNetCore.Mvc;
using CarPooling.Services;
using System.Diagnostics;

namespace CarPooling.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VoznjeController : ControllerBase
{
    private readonly VoznjaService _service;

    public VoznjeController(VoznjaService service)
    {
        _service = service;
    }

    [HttpGet("gradovi")]
    public async Task<IActionResult> GetGradovi()
    {
        var gradovi = await _service.GetSviGradovi();
        return Ok(gradovi);
    }
    [HttpGet("pretraga")]
    public async Task<IActionResult> Pretraga(
        [FromQuery] string odGrada,
        [FromQuery] string doGrada,
        [FromQuery] string datumIVreme,
        [FromQuery] int page,
        [FromQuery] int pageSize)
    {
        if (string.IsNullOrWhiteSpace(odGrada) ||
            string.IsNullOrWhiteSpace(doGrada) ||
            string.IsNullOrWhiteSpace(datumIVreme))
        {
            return BadRequest("Parametri odGrada, doGrada i vreme su obavezni.");
        }

        try
        {
            var (items,total) = await _service.PronadjiNajoptimalnijuVoznju(odGrada, doGrada, datumIVreme, page, pageSize);
            return Ok(new
                {
                items,
                total,
                page,
                pageSize,
                totalPages=(int)Math.Ceiling(total/(double)pageSize)
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}