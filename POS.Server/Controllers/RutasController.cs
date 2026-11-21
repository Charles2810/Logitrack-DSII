using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

/// <summary>
/// Expone las guías con coordenadas para el módulo de mapas y simulación de entregas.
/// La ruta geométrica se calcula en el navegador con OSRM; aquí solo se entrega
/// la información de origen/destino y las estimaciones guardadas.
/// </summary>
[ApiController]
[Route("api/rutas")]
[Authorize]
public class RutasController : ControllerBase
{
    private readonly AppDbContext _context;

    public RutasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GuiaDto>>> Get([FromQuery] int? idHojaRuta)
    {
        var query = RutasVisibles();

        if (idHojaRuta.HasValue)
        {
            query = query.Where(g => g.IdHojaRuta == idHojaRuta.Value);
        }

        var items = await query
            .OrderByDescending(g => g.FechaRegistro)
            .ProyectarResumen()
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuiaDto>> GetById(int id)
    {
        var guia = await RutasVisibles()
            .Where(g => g.IdGuia == id)
            .ProyectarDetalle()
            .FirstOrDefaultAsync();

        return guia == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "Guía no encontrada o sin coordenadas para trazar la ruta." })
            : Ok(guia);
    }

    private IQueryable<POS.Shared.Entities.GuiaEnvio> RutasVisibles()
    {
        var query = _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.LatitudDestino != null && g.LongitudDestino != null);

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(g => g.HojaRuta != null && g.HojaRuta.IdConductor == idUsuario);
        }

        return query;
    }
}
