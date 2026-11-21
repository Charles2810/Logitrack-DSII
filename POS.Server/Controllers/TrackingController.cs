using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.DTOs;
using POS.Shared.Utils;

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

        // Coordenadas de la guía para el mapa de seguimiento (sin datos personales).
        var ruta = await _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.CodigoGuia == busqueda)
            .Select(g => new
            {
                LatitudOrigen = (double?)g.AlmacenOrigen!.Latitud,
                LongitudOrigen = (double?)g.AlmacenOrigen!.Longitud,
                LatitudDestino = (double?)g.LatitudDestino,
                LongitudDestino = (double?)g.LongitudDestino,
                DistanciaGuardada = (double?)g.DistanciaEstimadaKm,
                DuracionGuardada = g.DuracionEstimadaMin
            })
            .FirstOrDefaultAsync();

        var distancia = ruta?.DistanciaGuardada ?? 0;
        if (distancia <= 0 && ruta != null)
        {
            distancia = GeoCalculos.DistanciaKm(ruta.LatitudOrigen, ruta.LongitudOrigen,
                ruta.LatitudDestino, ruta.LongitudDestino);
        }

        return Ok(new TrazabilidadDto
        {
            CodigoGuia = busqueda.ToUpperInvariant(),
            EstadoActual = estado,
            Eventos = eventos,
            LatitudOrigen = ruta?.LatitudOrigen,
            LongitudOrigen = ruta?.LongitudOrigen,
            LatitudDestino = ruta?.LatitudDestino,
            LongitudDestino = ruta?.LongitudDestino,
            DistanciaKm = distancia,
            DuracionEstimadaMin = ruta?.DuracionGuardada ?? GeoCalculos.DuracionEstimadaMin(distancia)
        });
    }
}
