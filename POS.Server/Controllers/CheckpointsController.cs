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
[Route("api/checkpoints")]
[Authorize]
public class CheckpointsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CheckpointsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("guia/{idGuia:int}")]
    public async Task<ActionResult<List<CheckpointDto>>> GetByGuia(int idGuia)
    {
        if (!await PuedeVerGuiaAsync(idGuia))
        {
            return NotFound(new ApiError { Codigo = 404, Mensaje = "Guía no encontrada o sin acceso para su rol." });
        }

        return Ok(await Proyectar(_context.Checkpoints.AsNoTracking().Where(c => c.IdGuia == idGuia)).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Conductor},{Roles.PersonalAlmacen}")]
    public async Task<ActionResult<CheckpointDto>> Create([FromBody] RegistrarCheckpointRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .Include(g => g.HojaRuta)
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.IdGuia == request.IdGuia)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        if (guia.HitoActual?.Nombre == Hitos.Entregado)
        {
            throw new BusinessException("La guía ya fue entregada; no admite más checkpoints.");
        }

        var hito = await _context.Hitos.FindAsync(request.IdHito)
            ?? throw new BusinessException("El hito seleccionado no existe.", 404);

        if (hito.Nombre == Hitos.Entregado)
        {
            throw new BusinessException("El hito Entregado solo se registra mediante la confirmación de entrega.");
        }

        if (hito.Orden != guia.HitoActual!.Orden + 1)
        {
            throw new BusinessException("El checkpoint debe corresponder al siguiente hito de la guía (RN05).");
        }

        if (guia.IdHojaRuta == null || guia.HojaRuta == null)
        {
            throw new BusinessException("La guía no tiene una hoja de ruta asignada.");
        }

        if (guia.HojaRuta.Estado == Estados.HojaFinalizada)
        {
            throw new BusinessException("La hoja de ruta ya está finalizada.");
        }

        var idUsuario = User.GetUserId();
        if (User.IsInRole(Roles.Conductor) && guia.HojaRuta.IdConductor != idUsuario)
        {
            throw new BusinessException("Solo el conductor asignado a la hoja de ruta puede registrar este checkpoint (RN13).");
        }

        var esSalidaAlmacen = hito.Nombre == Hitos.SalidaAlmacen;
        if (esSalidaAlmacen)
        {
            foreach (var detalle in guia.Detalles)
            {
                var stock = await _context.Inventarios
                    .Where(i => i.IdAlmacen == guia.IdAlmacenOrigen && i.IdProducto == detalle.IdProducto)
                    .Select(i => i.StockActual)
                    .FirstOrDefaultAsync();

                if (stock < detalle.Cantidad)
                {
                    throw new BusinessException(
                        $"Stock insuficiente en el almacén de origen para el producto {detalle.IdProducto}: " +
                        $"disponible {stock}, requerido {detalle.Cantidad}.");
                }
            }
        }

        var checkpoint = new Checkpoint
        {
            IdGuia = request.IdGuia,
            IdHito = request.IdHito,
            IdAlmacen = request.IdAlmacen,
            IdUsuario = idUsuario
        };

        await using var tx = await _context.Database.BeginTransactionAsync();

        _context.Checkpoints.Add(checkpoint);

        if (esSalidaAlmacen)
        {
            foreach (var detalle in guia.Detalles)
            {
                _context.MovimientosInventario.Add(new MovimientoInventario
                {
                    IdAlmacen = guia.IdAlmacenOrigen,
                    IdProducto = detalle.IdProducto,
                    IdGuia = guia.IdGuia,
                    IdUsuario = idUsuario,
                    TipoMovimiento = Estados.TipoSalida,
                    Cantidad = detalle.Cantidad
                });
            }
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        var creado = await Proyectar(_context.Checkpoints.AsNoTracking()
                .Where(c => c.IdCheckpoint == checkpoint.IdCheckpoint))
            .FirstAsync();

        return CreatedAtAction(nameof(GetByGuia), new { idGuia = request.IdGuia }, creado);
    }

    private static IQueryable<CheckpointDto> Proyectar(IQueryable<Checkpoint> query) =>
        query.Select(c => new CheckpointDto
        {
            IdCheckpoint = c.IdCheckpoint,
            IdGuia = c.IdGuia,
            IdHito = c.IdHito,
            Hito = c.Hito!.Nombre,
            OrdenHito = c.Hito!.Orden,
            IdAlmacen = c.IdAlmacen,
            Almacen = c.Almacen != null ? c.Almacen.Nombre : null,
            IdUsuario = c.IdUsuario,
            Usuario = c.Usuario!.NombreCompleto,
            RolUsuario = c.Usuario!.Rol!.Nombre,
            FechaHora = c.FechaHora
        });

    private async Task<bool> PuedeVerGuiaAsync(int idGuia)
    {
        var query = _context.GuiasEnvio.AsNoTracking().Where(g => g.IdGuia == idGuia);

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(g => g.HojaRuta != null && g.HojaRuta.IdConductor == idUsuario);
        }

        return await query.AnyAsync();
    }
}
