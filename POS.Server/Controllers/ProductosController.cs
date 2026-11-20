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
[Route("api/productos")]
[Authorize]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductoDto>>> Get([FromQuery] string? buscar)
    {
        var query = _context.Productos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(p => p.Sku.Contains(term) || p.Nombre.Contains(term));
        }

        return Ok(await query.OrderBy(p => p.Nombre).Select(p => new ProductoDto
        {
            IdProducto = p.IdProducto,
            Sku = p.Sku,
            Nombre = p.Nombre,
            PesoUnitarioKg = p.PesoUnitarioKg,
            VolumenUnitarioM3 = p.VolumenUnitarioM3
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<ProductoDto>> Create([FromBody] CrearProductoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var producto = new Producto
        {
            Sku = request.Sku.Trim().ToUpperInvariant(),
            Nombre = request.Nombre.Trim(),
            PesoUnitarioKg = request.PesoUnitarioKg,
            VolumenUnitarioM3 = request.VolumenUnitarioM3
        };

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { }, ToDto(producto));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<ProductoDto>> Update(int id, [FromBody] CrearProductoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var producto = await _context.Productos.FirstOrDefaultAsync(p => p.IdProducto == id)
            ?? throw new BusinessException("Producto no encontrado.", 404);

        producto.Sku = request.Sku.Trim().ToUpperInvariant();
        producto.Nombre = request.Nombre.Trim();
        producto.PesoUnitarioKg = request.PesoUnitarioKg;
        producto.VolumenUnitarioM3 = request.VolumenUnitarioM3;

        await _context.SaveChangesAsync();
        return Ok(ToDto(producto));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult> Delete(int id)
    {
        var producto = await _context.Productos.FindAsync(id)
            ?? throw new BusinessException("Producto no encontrado.", 404);

        if (await _context.Inventarios.AnyAsync(i => i.IdProducto == id) ||
            await _context.MovimientosInventario.AnyAsync(m => m.IdProducto == id) ||
            await _context.DetallesGuia.AnyAsync(d => d.IdProducto == id))
        {
            throw new BusinessException("No se puede eliminar el producto: tiene stock, movimientos o detalles de guía asociados.", 409);
        }

        _context.Productos.Remove(producto);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ProductoDto ToDto(Producto p) => new()
    {
        IdProducto = p.IdProducto,
        Sku = p.Sku,
        Nombre = p.Nombre,
        PesoUnitarioKg = p.PesoUnitarioKg,
        VolumenUnitarioM3 = p.VolumenUnitarioM3
    };
}
