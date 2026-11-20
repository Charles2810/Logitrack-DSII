using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificacionesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificacionDto>>> Get(
        [FromQuery] bool? leida,
        [FromQuery] int limite = 100)
    {
        var idUsuario = User.GetUserId();
        limite = Math.Clamp(limite, 1, 500);

        var query = _context.Notificaciones.AsNoTracking().Where(n => n.IdUsuario == idUsuario);

        if (leida.HasValue)
        {
            query = query.Where(n => n.Leida == leida.Value);
        }

        return Ok(await Proyectar(query)
            .OrderByDescending(n => n.FechaHora)
            .ThenByDescending(n => n.IdNotificacion)
            .Take(limite)
            .ToListAsync());
    }

    [HttpGet("no-leidas")]
    public async Task<ActionResult<object>> NoLeidas()
    {
        var idUsuario = User.GetUserId();
        var total = await _context.Notificaciones.CountAsync(n => n.IdUsuario == idUsuario && !n.Leida);
        return Ok(new { total });
    }

    [HttpPatch("{id:int}/leida")]
    public async Task<ActionResult<NotificacionDto>> MarcarLeida(int id)
    {
        var idUsuario = User.GetUserId();
        var notificacion = await _context.Notificaciones
            .FirstOrDefaultAsync(n => n.IdNotificacion == id && n.IdUsuario == idUsuario)
            ?? throw new BusinessException("Notificación no encontrada.", 404);

        notificacion.Leida = true;
        await _context.SaveChangesAsync();

        var dto = await Proyectar(_context.Notificaciones.AsNoTracking()
                .Where(n => n.IdNotificacion == id))
            .FirstAsync();

        return Ok(dto);
    }

    [HttpPatch("leer-todas")]
    public async Task<IActionResult> MarcarTodasLeidas()
    {
        var idUsuario = User.GetUserId();
        var pendientes = await _context.Notificaciones
            .Where(n => n.IdUsuario == idUsuario && !n.Leida)
            .ToListAsync();

        foreach (var notificacion in pendientes)
        {
            notificacion.Leida = true;
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Notificaciones marcadas como leídas.", total = pendientes.Count });
    }

    private static IQueryable<NotificacionDto> Proyectar(IQueryable<POS.Shared.Entities.Notificacion> query) =>
        query.Select(n => new NotificacionDto
        {
            IdNotificacion = n.IdNotificacion,
            IdUsuario = n.IdUsuario,
            IdGuia = n.IdGuia,
            CodigoGuia = n.Guia != null ? n.Guia.CodigoGuia : null,
            IdHojaRuta = n.IdHojaRuta,
            Tipo = n.Tipo,
            Mensaje = n.Mensaje,
            Leida = n.Leida,
            FechaHora = n.FechaHora
        });
}
