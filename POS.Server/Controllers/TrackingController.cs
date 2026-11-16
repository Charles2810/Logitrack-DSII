using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/tracking")]
[AllowAnonymous]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<TrazabilidadDto>> Get(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return BadRequest(new ApiError { Codigo = 400, Mensaje = "Debe indicar el código de guía." });
        }

        var busqueda = codigo.Trim();
        var eventos = await _context.TrazabilidadPublica.AsNoTracking()
            .Where(t => t.CodigoGuia == busqueda)
            .OrderBy(t => t.OrdenHito)
            .ThenBy(t => t.FechaHora)
            .Select(t => new TrazabilidadEventoDto
            {
                TipoEvento = t.TipoEvento,
                Hito = t.Hito,
                OrdenHito = t.OrdenHito,
                FechaHora = t.FechaHora,
                Estacion = t.Estacion
            })
            .ToListAsync();

        if (eventos.Count == 0)
        {
            return NotFound(new ApiError
            {
                Codigo = 404,
                Mensaje = $"No se encontró la guía '{busqueda}'. Verifique el código e intente nuevamente."
            });
        }

        var estado = await _context.TrazabilidadPublica.AsNoTracking()
            .Where(t => t.CodigoGuia == busqueda)
            .Select(t => t.EstadoActual)
            .FirstAsync();

        return Ok(new TrazabilidadDto
        {
            CodigoGuia = eventos.Count > 0 ? busqueda.ToUpperInvariant() : busqueda,
            EstadoActual = estado,
            Eventos = eventos
        });
    }
}
