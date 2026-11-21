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
[Route("api/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClienteDto>>> Get([FromQuery] string? buscar)
    {
        var query = _context.Clientes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(c =>
                c.RazonSocial.Contains(term) ||
                c.Nit.Contains(term) ||
                (c.Telefono != null && c.Telefono.Contains(term)));
        }

        return Ok(await query.OrderBy(c => c.RazonSocial).Select(c => new ClienteDto
        {
            IdCliente = c.IdCliente,
            RazonSocial = c.RazonSocial,
            Nit = c.Nit,
            Telefono = c.Telefono,
            Direccion = c.Direccion,
            Latitud = (double?)c.Latitud,
            Longitud = (double?)c.Longitud
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<ClienteDto>> Create([FromBody] CrearClienteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var cliente = new Cliente
        {
            RazonSocial = request.RazonSocial.Trim(),
            Nit = request.Nit.Trim(),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim(),
            Latitud = request.Latitud.HasValue ? (decimal)request.Latitud.Value : null,
            Longitud = request.Longitud.HasValue ? (decimal)request.Longitud.Value : null
        };

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { }, ToDto(cliente));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<ClienteDto>> Update(int id, [FromBody] CrearClienteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == id)
            ?? throw new BusinessException("Cliente no encontrado.", 404);

        cliente.RazonSocial = request.RazonSocial.Trim();
        cliente.Nit = request.Nit.Trim();
        cliente.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        cliente.Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();
        cliente.Latitud = request.Latitud.HasValue ? (decimal)request.Latitud.Value : null;
        cliente.Longitud = request.Longitud.HasValue ? (decimal)request.Longitud.Value : null;

        await _context.SaveChangesAsync();
        return Ok(ToDto(cliente));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id)
            ?? throw new BusinessException("Cliente no encontrado.", 404);

        if (await _context.GuiasEnvio.AnyAsync(g => g.IdCliente == id))
        {
            throw new BusinessException("No se puede eliminar el cliente: tiene guías de envío asociadas.", 409);
        }

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ClienteDto ToDto(Cliente c) => new()
    {
        IdCliente = c.IdCliente,
        RazonSocial = c.RazonSocial,
        Nit = c.Nit,
        Telefono = c.Telefono,
        Direccion = c.Direccion,
        Latitud = (double?)c.Latitud,
        Longitud = (double?)c.Longitud
    };
}
