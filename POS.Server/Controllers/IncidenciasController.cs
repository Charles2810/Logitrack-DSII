using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/incidencias")]
[Authorize]
public class IncidenciasController : ControllerBase
{
    private readonly AppDbContext _context;

    public IncidenciasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidenciaDto>>> Get(
        [FromQuery] int? idGuia,
        [FromQuery] int? idTipoIncidencia,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = IncidenciasVisibles();

        if (idGuia.HasValue)
        {
            query = query.Where(i => i.IdGuia == idGuia.Value);
        }

        if (idTipoIncidencia.HasValue)
        {
            query = query.Where(i => i.IdTipoIncidencia == idTipoIncidencia.Value);
        }

        var total = await query.CountAsync();
        var items = await Proyectar(query)
            .OrderByDescending(i => i.FechaHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<IncidenciaDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("guia/{idGuia:int}")]
    public async Task<ActionResult<List<IncidenciaDto>>> GetByGuia(int idGuia)
    {
        var query = IncidenciasVisibles().Where(i => i.IdGuia == idGuia);
        return Ok(await Proyectar(query).OrderBy(i => i.FechaHora).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.CentroControl}")]
    public async Task<ActionResult<IncidenciaDto>> Create([FromBody] CrearIncidenciaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.RutaFoto))
        {
            throw new BusinessException("La fotografía de evidencia es obligatoria para registrar una incidencia (RN08).");
        }

        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .Include(g => g.HojaRuta)
            .FirstOrDefaultAsync(g => g.IdGuia == request.IdGuia)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        if (guia.HitoActual?.Nombre == Hitos.Entregado)
        {
            throw new BusinessException("No se puede reportar una incidencia sobre una guía entregada.");
        }

        if (guia.IdHojaRuta == null || guia.HojaRuta == null)
        {
            throw new BusinessException("La guía no tiene una hoja de ruta activa.");
        }

        if (guia.HojaRuta.Estado == Estados.HojaFinalizada)
        {
            throw new BusinessException("La hoja de ruta ya está finalizada.");
        }

        var idUsuario = User.GetUserId();
        if (User.IsInRole(Roles.Conductor) && guia.HojaRuta.IdConductor != idUsuario)
        {
            throw new BusinessException("Solo el conductor asignado a la ruta puede reportar la incidencia (RN13).");
        }

        if (!await _context.TiposIncidencia.AnyAsync(t => t.IdTipoIncidencia == request.IdTipoIncidencia))
        {
            throw new BusinessException("El tipo de incidencia seleccionado no existe.");
        }

        var incidencia = new Incidencia
        {
            IdGuia = request.IdGuia,
            IdTipoIncidencia = request.IdTipoIncidencia,
            IdUsuario = idUsuario,
            Descripcion = request.Descripcion.Trim(),
            RutaFoto = request.RutaFoto.Trim()
        };

        _context.Incidencias.Add(incidencia);
        await _context.SaveChangesAsync();

        var creada = await Proyectar(_context.Incidencias.AsNoTracking()
                .Where(i => i.IdIncidencia == incidencia.IdIncidencia))
            .FirstAsync();

        return CreatedAtAction(nameof(GetByGuia), new { idGuia = request.IdGuia }, creada);
    }

    private IQueryable<Incidencia> IncidenciasVisibles()
    {
        IQueryable<Incidencia> query = _context.Incidencias.AsNoTracking();

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(i => i.Guia!.HojaRuta != null && i.Guia.HojaRuta.IdConductor == idUsuario);
        }

        return query;
    }

    private static IQueryable<IncidenciaDto> Proyectar(IQueryable<Incidencia> query) =>
        query.Select(i => new IncidenciaDto
        {
            IdIncidencia = i.IdIncidencia,
            IdGuia = i.IdGuia,
            CodigoGuia = i.Guia!.CodigoGuia,
            IdTipoIncidencia = i.IdTipoIncidencia,
            TipoIncidencia = i.TipoIncidencia!.Nombre,
            IdUsuario = i.IdUsuario,
            Usuario = i.Usuario!.NombreCompleto,
            Descripcion = i.Descripcion,
            RutaFoto = i.RutaFoto,
            FechaHora = i.FechaHora
        });
}
