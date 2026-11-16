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
[Route("api/inventario")]
[Authorize]
public class InventarioController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventarioController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventarioDto>>> Get([FromQuery] int? idAlmacen, [FromQuery] string? buscar)
    {
        var query = _context.Inventarios.AsNoTracking().AsQueryable();

        if (idAlmacen.HasValue)
        {
            query = query.Where(i => i.IdAlmacen == idAlmacen.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(i => i.Producto!.Sku.Contains(term) || i.Producto!.Nombre.Contains(term));
        }

        var items = await query
            .OrderBy(i => i.Almacen!.Nombre)
            .ThenBy(i => i.Producto!.Nombre)
            .Select(i => new InventarioDto
            {
                IdAlmacen = i.IdAlmacen,
                Almacen = i.Almacen!.Nombre,
                IdProducto = i.IdProducto,
                Sku = i.Producto!.Sku,
                Producto = i.Producto!.Nombre,
                StockActual = i.StockActual
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("alertas")]
    public async Task<ActionResult<List<InventarioDto>>> Alertas([FromQuery] int umbral = 20)
    {
        var items = await _context.Inventarios.AsNoTracking()
            .Where(i => i.StockActual <= umbral)
            .OrderBy(i => i.StockActual)
            .Select(i => new InventarioDto
            {
                IdAlmacen = i.IdAlmacen,
                Almacen = i.Almacen!.Nombre,
                IdProducto = i.IdProducto,
                Sku = i.Producto!.Sku,
                Producto = i.Producto!.Nombre,
                StockActual = i.StockActual
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("movimientos")]
    public async Task<ActionResult<PagedResult<MovimientoInventarioDto>>> Movimientos(
        [FromQuery] int? idAlmacen,
        [FromQuery] int? idProducto,
        [FromQuery] string? tipo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.MovimientosInventario.AsNoTracking().AsQueryable();

        if (idAlmacen.HasValue)
        {
            query = query.Where(m => m.IdAlmacen == idAlmacen.Value);
        }

        if (idProducto.HasValue)
        {
            query = query.Where(m => m.IdProducto == idProducto.Value);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            query = query.Where(m => m.TipoMovimiento == tipo);
        }

        if (desde.HasValue)
        {
            query = query.Where(m => m.FechaHora >= desde.Value.Date);
        }

        if (hasta.HasValue)
        {
            query = query.Where(m => m.FechaHora < hasta.Value.Date.AddDays(1));
        }

        var total = await query.CountAsync();
        var items = await Proyectar(query)
            .OrderByDescending(m => m.FechaHora)
            .ThenByDescending(m => m.IdMovimiento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new PagedResult<MovimientoInventarioDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpPost("movimientos")]
    [Authorize(Roles = $"{Roles.PersonalAlmacen},{Roles.AdministradorLogistico},{Roles.AdministradorSistema}")]
    public async Task<ActionResult<MovimientoInventarioDto>> RegistrarMovimiento([FromBody] CrearMovimientoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.TipoMovimiento is not (Estados.TipoEntrada or Estados.TipoSalida))
        {
            throw new BusinessException("El tipo de movimiento debe ser ENTRADA o SALIDA.");
        }

        if (!await _context.Almacenes.AnyAsync(a => a.IdAlmacen == request.IdAlmacen))
        {
            throw new BusinessException("El almacén seleccionado no existe.", 404);
        }

        if (!await _context.Productos.AnyAsync(p => p.IdProducto == request.IdProducto))
        {
            throw new BusinessException("El producto seleccionado no existe.", 404);
        }

        if (request.IdGuia.HasValue && !await _context.GuiasEnvio.AnyAsync(g => g.IdGuia == request.IdGuia.Value))
        {
            throw new BusinessException("La guía indicada no existe.", 404);
        }

        var movimiento = new MovimientoInventario
        {
            IdAlmacen = request.IdAlmacen,
            IdProducto = request.IdProducto,
            IdGuia = request.IdGuia,
            IdUsuario = User.GetUserId(),
            TipoMovimiento = request.TipoMovimiento,
            Cantidad = request.Cantidad
        };

        _context.MovimientosInventario.Add(movimiento);
        await _context.SaveChangesAsync();

        var creado = await Proyectar(_context.MovimientosInventario.AsNoTracking()
                .Where(m => m.IdMovimiento == movimiento.IdMovimiento))
            .FirstAsync();

        return CreatedAtAction(nameof(Movimientos), new { }, creado);
    }

    private static IQueryable<MovimientoInventarioDto> Proyectar(IQueryable<MovimientoInventario> query) =>
        query.Select(m => new MovimientoInventarioDto
        {
            IdMovimiento = m.IdMovimiento,
            IdAlmacen = m.IdAlmacen,
            Almacen = m.Almacen!.Nombre,
            IdProducto = m.IdProducto,
            Sku = m.Producto!.Sku,
            Producto = m.Producto!.Nombre,
            IdGuia = m.IdGuia,
            CodigoGuia = m.Guia != null ? m.Guia.CodigoGuia : null,
            IdUsuario = m.IdUsuario,
            Usuario = m.Usuario!.NombreCompleto,
            TipoMovimiento = m.TipoMovimiento,
            Cantidad = m.Cantidad,
            FechaHora = m.FechaHora
        });
}
