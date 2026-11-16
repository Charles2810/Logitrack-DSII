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
[Route("api/entregas")]
[Authorize]
public class EntregasController : ControllerBase
{
    private readonly AppDbContext _context;

    public EntregasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EntregaDto>>> Get(
        [FromQuery] int? idGuia,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = EntregasVisibles();

        if (idGuia.HasValue)
        {
            query = query.Where(e => e.IdGuia == idGuia.Value);
        }

        var total = await query.CountAsync();
        var items = await Proyectar(query)
            .OrderByDescending(e => e.FechaHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<EntregaDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("guia/{idGuia:int}")]
    public async Task<ActionResult<EntregaDto>> GetByGuia(int idGuia)
    {
        var entrega = await Proyectar(EntregasVisibles().Where(e => e.IdGuia == idGuia)).FirstOrDefaultAsync();

        return entrega == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "La guía aún no tiene una entrega registrada." })
            : Ok(entrega);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Conductor)]
    public async Task<ActionResult<EntregaDto>> Create([FromBody] CrearEntregaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.RutaFirma) && string.IsNullOrWhiteSpace(request.RutaFoto))
        {
            throw new BusinessException("La entrega requiere la firma del receptor o una fotografía como comprobante (RN07).");
        }

        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .Include(g => g.HojaRuta)
            .FirstOrDefaultAsync(g => g.IdGuia == request.IdGuia)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        var idUsuario = User.GetUserId();

        if (guia.IdHojaRuta == null || guia.HojaRuta == null)
        {
            throw new BusinessException("La guía no tiene una hoja de ruta asignada.");
        }

        if (guia.HojaRuta.IdConductor != idUsuario)
        {
            throw new BusinessException("Solo el conductor asignado a la ruta puede confirmar la entrega (RN13).");
        }

        if (guia.HitoActual?.Nombre != Hitos.EnRepartoFinal)
        {
            throw new BusinessException("La guía debe estar en 'En Reparto Final' para confirmar la entrega (RN07).");
        }

        if (await _context.Entregas.AnyAsync(e => e.IdGuia == request.IdGuia))
        {
            throw new BusinessException("La guía ya tiene una entrega registrada.");
        }

        var entrega = new Entrega
        {
            IdGuia = request.IdGuia,
            IdUsuario = idUsuario,
            NombreReceptor = string.IsNullOrWhiteSpace(request.NombreReceptor) ? null : request.NombreReceptor.Trim(),
            RutaFirma = string.IsNullOrWhiteSpace(request.RutaFirma) ? null : request.RutaFirma.Trim(),
            RutaFoto = string.IsNullOrWhiteSpace(request.RutaFoto) ? null : request.RutaFoto.Trim()
        };

        _context.Entregas.Add(entrega);
        await _context.SaveChangesAsync();

        var creada = await Proyectar(_context.Entregas.AsNoTracking()
                .Where(e => e.IdEntrega == entrega.IdEntrega))
            .FirstAsync();

        return CreatedAtAction(nameof(GetByGuia), new { idGuia = request.IdGuia }, creada);
    }

    private IQueryable<Entrega> EntregasVisibles()
    {
        IQueryable<Entrega> query = _context.Entregas.AsNoTracking();

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(e => e.Guia!.HojaRuta != null && e.Guia.HojaRuta.IdConductor == idUsuario);
        }

        return query;
    }

    private static IQueryable<EntregaDto> Proyectar(IQueryable<Entrega> query) =>
        query.Select(e => new EntregaDto
        {
            IdEntrega = e.IdEntrega,
            IdGuia = e.IdGuia,
            CodigoGuia = e.Guia!.CodigoGuia,
            Destinatario = e.Guia!.Destinatario,
            IdUsuario = e.IdUsuario,
            Usuario = e.Usuario!.NombreCompleto,
            NombreReceptor = e.NombreReceptor,
            RutaFirma = e.RutaFirma,
            RutaFoto = e.RutaFoto,
            FechaHora = e.FechaHora
        });
}
