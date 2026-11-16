using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard()
    {
        var idUsuario = User.GetUserId();

        var totalGuias = await _context.GuiasEnvio.CountAsync();
        var registradas = await _context.GuiasEnvio.CountAsync(g => g.HitoActual!.Nombre == Hitos.Registrada);
        var entregadas = await _context.GuiasEnvio.CountAsync(g => g.HitoActual!.Nombre == Hitos.Entregado);

        var dto = new DashboardDto
        {
            TotalGuias = totalGuias,
            GuiasRegistradas = registradas,
            GuiasEntregadas = entregadas,
            GuiasEnTransito = totalGuias - registradas - entregadas,
            HojasActivas = await _context.HojasRuta.CountAsync(h =>
                h.Estado == Estados.HojaPendiente || h.Estado == Estados.HojaEnCurso),
            VehiculosDisponibles = await _context.Vehiculos.CountAsync(v => v.Estado == Estados.VehiculoDisponible),
            TotalIncidencias = await _context.Incidencias.CountAsync(),
            NotificacionesNoLeidas = await _context.Notificaciones.CountAsync(n => n.IdUsuario == idUsuario && !n.Leida),
            PesoTotalEntregado = await _context.GuiasEnvio
                .Where(g => g.HitoActual!.Nombre == Hitos.Entregado)
                .SumAsync(g => (decimal?)g.PesoKg) ?? 0,
            UltimasGuias = await _context.GuiasEnvio.AsNoTracking()
                .OrderByDescending(g => g.FechaRegistro)
                .ThenByDescending(g => g.IdGuia)
                .Take(6)
                .ProyectarResumen()
                .ToListAsync(),
            UltimasNotificaciones = await _context.Notificaciones.AsNoTracking()
                .Where(n => n.IdUsuario == idUsuario)
                .OrderByDescending(n => n.FechaHora)
                .Take(6)
                .Select(n => new NotificacionDto
                {
                    IdNotificacion = n.IdNotificacion,
                    IdUsuario = n.IdUsuario,
                    IdGuia = n.IdGuia,
                    CodigoGuia = n.Guia != null ? n.Guia.CodigoGuia : null,
                    Tipo = n.Tipo,
                    Mensaje = n.Mensaje,
                    Leida = n.Leida,
                    FechaHora = n.FechaHora
                })
                .ToListAsync(),
            GuiasPorHito = await _context.GuiasEnvio.AsNoTracking()
                .GroupBy(g => g.HitoActual!.Nombre)
                .Select(g => new ConteoEstadoDto { Etiqueta = g.Key, Total = g.Count() })
                .ToListAsync()
        };

        var hayTiempos = await _context.ReporteEnvios.AnyAsync(r => r.HorasTransito != null);
        if (hayTiempos)
        {
            dto.PromedioHorasEntrega = Math.Round(
                await _context.ReporteEnvios
                    .Where(r => r.HorasTransito != null)
                    .AverageAsync(r => (double)r.HorasTransito!.Value), 2);
        }

        return Ok(dto);
    }

    [HttpGet("envios")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico},{Roles.CentroControl}")]
    public async Task<ActionResult<List<ReporteEnvioDto>>> Envios(
        [FromQuery] string? buscar,
        [FromQuery] string? estado,
        [FromQuery] string? cliente,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] bool? conIncidencias,
        [FromQuery] int limite = 500)
    {
        limite = Math.Clamp(limite, 1, 2000);

        var query = _context.ReporteEnvios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(r =>
                r.CodigoGuia.Contains(term) ||
                r.Destinatario.Contains(term) ||
                r.Cliente.Contains(term) ||
                r.DireccionDestino.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(r => r.EstadoActual == estado);
        }

        if (!string.IsNullOrWhiteSpace(cliente))
        {
            query = query.Where(r => r.Cliente.Contains(cliente.Trim()));
        }

        if (desde.HasValue)
        {
            query = query.Where(r => r.FechaRegistro >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(r => r.FechaRegistro < hasta.Value.Date.AddDays(1));
        }

        if (conIncidencias == true)
        {
            query = query.Where(r => r.TotalIncidencias > 0);
        }

        var items = await query
            .OrderByDescending(r => r.FechaRegistro)
            .Take(limite)
            .Select(r => new ReporteEnvioDto
            {
                IdGuia = r.IdGuia,
                CodigoGuia = r.CodigoGuia,
                FechaRegistro = r.FechaRegistro,
                Cliente = r.Cliente,
                AlmacenOrigen = r.AlmacenOrigen,
                TipoCarga = r.TipoCarga,
                Destinatario = r.Destinatario,
                DireccionDestino = r.DireccionDestino,
                PesoKg = r.PesoKg,
                VolumenM3 = r.VolumenM3,
                EstadoActual = r.EstadoActual,
                EstadoHojaRuta = r.EstadoHojaRuta,
                Vehiculo = r.Vehiculo,
                Conductor = r.Conductor,
                FechaEntrega = r.FechaEntrega,
                HorasTransito = r.HorasTransito,
                TotalIncidencias = r.TotalIncidencias
            })
            .ToListAsync();

        return Ok(items);
    }
}
