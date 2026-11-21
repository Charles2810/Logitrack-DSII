using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using POS.Shared.Utils;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/guias")]
[Authorize]
public class GuiasController : ControllerBase
{
    private readonly AppDbContext _context;

    public GuiasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GuiaDto>>> Get([FromQuery] GuiaFiltro filtro)
    {
        var page = Math.Max(1, filtro.Page);
        var pageSize = Math.Clamp(filtro.PageSize, 1, 100);

        var query = GuiasVisibles();

        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var term = filtro.Buscar.Trim();
            query = query.Where(g =>
                g.CodigoGuia.Contains(term) ||
                g.Destinatario.Contains(term) ||
                g.DireccionDestino.Contains(term) ||
                g.Cliente!.RazonSocial.Contains(term));
        }

        if (filtro.IdHito.HasValue)
        {
            query = query.Where(g => g.IdHitoActual == filtro.IdHito.Value);
        }

        if (filtro.IdCliente.HasValue)
        {
            query = query.Where(g => g.IdCliente == filtro.IdCliente.Value);
        }

        if (filtro.IdHojaRuta.HasValue)
        {
            query = query.Where(g => g.IdHojaRuta == filtro.IdHojaRuta.Value);
        }

        if (filtro.Desde.HasValue)
        {
            query = query.Where(g => g.FechaRegistro >= filtro.Desde.Value.Date);
        }

        if (filtro.Hasta.HasValue)
        {
            query = query.Where(g => g.FechaRegistro < filtro.Hasta.Value.Date.AddDays(1));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(g => g.FechaRegistro)
            .ThenByDescending(g => g.IdGuia)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ProyectarResumen()
            .ToListAsync();

        return Ok(new PagedResult<GuiaDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GuiaDto>> GetById(int id)
    {
        var guia = await GuiasVisibles()
            .Where(g => g.IdGuia == id)
            .ProyectarDetalle()
            .FirstOrDefaultAsync();

        return guia == null ? NoEncontrada() : Ok(guia);
    }

    [HttpGet("{id:int}/checkpoints")]
    public async Task<ActionResult<List<CheckpointDto>>> Checkpoints(int id)
    {
        if (!await GuiasVisibles().AnyAsync(g => g.IdGuia == id))
        {
            return NoEncontrada();
        }

        var items = await _context.Checkpoints.AsNoTracking()
            .Where(c => c.IdGuia == id)
            .OrderBy(c => c.FechaHora)
            .ThenBy(c => c.IdCheckpoint)
            .Select(c => new CheckpointDto
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
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}/incidencias")]
    public async Task<ActionResult<List<IncidenciaDto>>> Incidencias(int id)
    {
        if (!await GuiasVisibles().AnyAsync(g => g.IdGuia == id))
        {
            return NoEncontrada();
        }

        var items = await _context.Incidencias.AsNoTracking()
            .Where(i => i.IdGuia == id)
            .OrderByDescending(i => i.FechaHora)
            .Select(i => new IncidenciaDto
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
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}/entrega")]
    public async Task<ActionResult<EntregaDto>> Entrega(int id)
    {
        if (!await GuiasVisibles().AnyAsync(g => g.IdGuia == id))
        {
            return NoEncontrada();
        }

        var entrega = await _context.Entregas.AsNoTracking()
            .Where(e => e.IdGuia == id)
            .Select(e => new EntregaDto
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
            })
            .FirstOrDefaultAsync();

        return entrega == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "La guía aún no tiene una entrega registrada." })
            : Ok(entrega);
    }

    [HttpGet("{id:int}/siguiente-hito")]
    public async Task<ActionResult<HitoDto?>> SiguienteHito(int id)
    {
        var guia = await GuiasVisibles()
            .Include(g => g.HitoActual)
            .FirstOrDefaultAsync(g => g.IdGuia == id);

        if (guia == null)
        {
            return NoEncontrada();
        }

        if (guia.HitoActual!.Nombre == Hitos.Entregado)
        {
            return Ok(null);
        }

        var siguiente = await _context.Hitos.AsNoTracking()
            .Where(h => h.Orden == guia.HitoActual.Orden + 1 && h.Nombre != Hitos.Entregado)
            .Select(h => new HitoDto { IdHito = h.IdHito, Nombre = h.Nombre, Orden = h.Orden })
            .FirstOrDefaultAsync();

        return Ok(siguiente);
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult<GuiaDto>> Create([FromBody] CrearGuiaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.Detalles == null || request.Detalles.Count == 0)
        {
            throw new BusinessException("Debe registrar al menos un producto en el detalle de la guía.");
        }

        var idsProductos = request.Detalles.Select(d => d.IdProducto).ToList();
        if (idsProductos.Count != idsProductos.Distinct().Count())
        {
            throw new BusinessException("No puede repetir el mismo producto en el detalle de la guía.");
        }

        var almacen = await _context.Almacenes
            .FirstOrDefaultAsync(a => a.IdAlmacen == request.IdAlmacenOrigen);
        if (almacen == null)
        {
            throw new BusinessException("El almacén de origen seleccionado no existe.", 404);
        }

        var productos = await _context.Productos
            .Where(p => idsProductos.Contains(p.IdProducto))
            .ToDictionaryAsync(p => p.IdProducto, p => p.Sku);

        if (productos.Count != idsProductos.Count)
        {
            throw new BusinessException("Uno o más productos seleccionados no existen.");
        }

        var stocks = await _context.Inventarios
            .Where(i => i.IdAlmacen == request.IdAlmacenOrigen && idsProductos.Contains(i.IdProducto))
            .ToDictionaryAsync(i => i.IdProducto, i => i.StockActual);

        foreach (var detalle in request.Detalles)
        {
            var disponible = stocks.GetValueOrDefault(detalle.IdProducto);
            if (detalle.Cantidad > disponible)
            {
                throw new BusinessException($"Stock insuficiente en el almacén de origen para el producto {productos[detalle.IdProducto]}: disponible {disponible}, requerido {detalle.Cantidad}.");
            }
        }

        var idRegistrada = await _context.Hitos
            .Where(h => h.Nombre == Hitos.Registrada)
            .Select(h => h.IdHito)
            .FirstAsync();

        if (request.IdHojaRuta.HasValue)
        {
            var hoja = await _context.HojasRuta
                .Include(h => h.Vehiculo)
                .FirstOrDefaultAsync(h => h.IdHojaRuta == request.IdHojaRuta.Value)
                ?? throw new BusinessException("La hoja de ruta seleccionada no existe.", 404);

            if (hoja.Estado == Estados.HojaFinalizada)
            {
                throw new BusinessException("No se puede asignar una guía a una hoja de ruta finalizada.");
            }
        }

        var latitudOrigen = (double?)almacen.Latitud;
        var longitudOrigen = (double?)almacen.Longitud;
        var distanciaKm = GeoCalculos.DistanciaKm(latitudOrigen, longitudOrigen,
            request.LatitudDestino, request.LongitudDestino);

        var guia = new GuiaEnvio
        {
            IdCliente = request.IdCliente,
            IdAlmacenOrigen = request.IdAlmacenOrigen,
            IdTipoCarga = request.IdTipoCarga,
            IdHitoActual = idRegistrada,
            IdHojaRuta = request.IdHojaRuta,
            IdUsuarioRegistro = User.GetUserId(),
            Destinatario = request.Destinatario.Trim(),
            DireccionDestino = request.DireccionDestino.Trim(),
            LatitudDestino = request.LatitudDestino.HasValue ? (decimal)request.LatitudDestino.Value : null,
            LongitudDestino = request.LongitudDestino.HasValue ? (decimal)request.LongitudDestino.Value : null,
            DistanciaEstimadaKm = distanciaKm > 0 ? (decimal)distanciaKm : null,
            DuracionEstimadaMin = distanciaKm > 0 ? GeoCalculos.DuracionEstimadaMin(distanciaKm) : null,
            PesoKg = request.PesoKg,
            VolumenM3 = request.VolumenM3,
            Detalles = request.Detalles.Select(d => new DetalleGuia
            {
                IdProducto = d.IdProducto,
                Cantidad = d.Cantidad
            }).ToList()
        };

        _context.GuiasEnvio.Add(guia);
        await _context.SaveChangesAsync();

        var creada = await _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.IdGuia == guia.IdGuia)
            .ProyectarDetalle()
            .FirstAsync();

        return CreatedAtAction(nameof(GetById), new { id = guia.IdGuia }, creada);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult<GuiaDto>> Update(int id, [FromBody] ActualizarGuiaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.IdGuia == id)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        if (guia.HitoActual?.Nombre == Hitos.Entregado)
        {
            throw new BusinessException("Una guía entregada no puede modificarse (RNF05).");
        }

        var almacen = await _context.Almacenes
            .FirstOrDefaultAsync(a => a.IdAlmacen == request.IdAlmacenOrigen)
            ?? throw new BusinessException("El almacén de origen seleccionado no existe.", 404);

        guia.IdCliente = request.IdCliente;
        guia.IdAlmacenOrigen = request.IdAlmacenOrigen;
        guia.IdTipoCarga = request.IdTipoCarga;
        guia.Destinatario = request.Destinatario.Trim();
        guia.DireccionDestino = request.DireccionDestino.Trim();
        guia.LatitudDestino = request.LatitudDestino.HasValue ? (decimal)request.LatitudDestino.Value : null;
        guia.LongitudDestino = request.LongitudDestino.HasValue ? (decimal)request.LongitudDestino.Value : null;

        var distanciaKm = GeoCalculos.DistanciaKm((double?)almacen.Latitud, (double?)almacen.Longitud,
            request.LatitudDestino, request.LongitudDestino);
        guia.DistanciaEstimadaKm = distanciaKm > 0 ? (decimal)distanciaKm : null;
        guia.DuracionEstimadaMin = distanciaKm > 0 ? GeoCalculos.DuracionEstimadaMin(distanciaKm) : null;

        guia.PesoKg = request.PesoKg;
        guia.VolumenM3 = request.VolumenM3;

        if (request.Detalles != null)
        {
            if (request.Detalles.Count == 0)
            {
                throw new BusinessException("La guía debe conservar al menos un producto en su detalle.");
            }

            var idsProductos = request.Detalles.Select(d => d.IdProducto).ToList();
            if (idsProductos.Count != idsProductos.Distinct().Count())
            {
                throw new BusinessException("No puede repetir el mismo producto en el detalle de la guía.");
            }

            if (await _context.Checkpoints.AnyAsync(c => c.IdGuia == id))
            {
                throw new BusinessException("El detalle de una guía despachada no puede modificarse (RNF05).");
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            _context.DetallesGuia.RemoveRange(guia.Detalles);
            await _context.SaveChangesAsync();

            foreach (var detalle in request.Detalles)
            {
                _context.DetallesGuia.Add(new DetalleGuia
                {
                    IdGuia = id,
                    IdProducto = detalle.IdProducto,
                    Cantidad = detalle.Cantidad
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        return Ok(await _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.IdGuia == id)
            .ProyectarDetalle()
            .FirstAsync());
    }

    [HttpPatch("{id:int}/hoja-ruta")]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult<GuiaDto>> AsignarHojaRuta(int id, [FromBody] AsignarGuiaRequest request)
    {
        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .FirstOrDefaultAsync(g => g.IdGuia == id)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        if (guia.HitoActual?.Nombre == Hitos.Entregado)
        {
            throw new BusinessException("Una guía entregada no puede reasignarse.");
        }

        guia.IdHojaRuta = request.IdHojaRuta;
        await _context.SaveChangesAsync();

        return Ok(await _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.IdGuia == id)
            .ProyectarDetalle()
            .FirstAsync());
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult> Delete(int id)
    {
        var guia = await _context.GuiasEnvio
            .Include(g => g.HitoActual)
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.IdGuia == id)
            ?? throw new BusinessException("Guía no encontrada.", 404);

        if (guia.HitoActual?.Nombre != Hitos.Registrada)
        {
            throw new BusinessException("Solo se pueden eliminar guías en el hito Registrada (RNF05).", 409);
        }

        if (await _context.Checkpoints.AnyAsync(c => c.IdGuia == id) ||
            await _context.Incidencias.AnyAsync(i => i.IdGuia == id) ||
            await _context.Entregas.AnyAsync(e => e.IdGuia == id) ||
            await _context.MovimientosInventario.AnyAsync(m => m.IdGuia == id) ||
            await _context.Notificaciones.AnyAsync(n => n.IdGuia == id))
        {
            throw new BusinessException("No se puede eliminar la guía: ya tiene eventos de trazabilidad asociados (RNF05).", 409);
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        _context.DetallesGuia.RemoveRange(guia.Detalles);
        await _context.SaveChangesAsync();

        _context.GuiasEnvio.Remove(guia);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return NoContent();
    }

    private IQueryable<GuiaEnvio> GuiasVisibles()
    {
        IQueryable<GuiaEnvio> query = _context.GuiasEnvio.AsNoTracking();

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(g => g.HojaRuta != null && g.HojaRuta.IdConductor == idUsuario);
        }

        return query;
    }

    private NotFoundObjectResult NoEncontrada() =>
        NotFound(new ApiError { Codigo = 404, Mensaje = "Guía no encontrada o sin acceso para su rol." });
}
