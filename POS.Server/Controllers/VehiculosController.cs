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
[Route("api/vehiculos")]
[Authorize]
public class VehiculosController : ControllerBase
{
    private static readonly string[] EstadosValidos =
    {
        Estados.VehiculoDisponible,
        Estados.VehiculoEnRuta,
        Estados.VehiculoMantenimiento,
        Estados.VehiculoInhabilitado
    };

    private readonly AppDbContext _context;

    public VehiculosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehiculoDto>>> Get([FromQuery] string? estado, [FromQuery] string? buscar)
    {
        var query = _context.Vehiculos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(v => v.Estado == estado);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(v => v.Placa.Contains(term) || v.Tipo.Contains(term));
        }

        return Ok(await query.OrderBy(v => v.Placa).Select(v => new VehiculoDto
        {
            IdVehiculo = v.IdVehiculo,
            Placa = v.Placa,
            Tipo = v.Tipo,
            CapacidadKg = v.CapacidadKg,
            CapacidadM3 = v.CapacidadM3,
            Estado = v.Estado
        }).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehiculoDto>> GetById(int id)
    {
        var vehiculo = await _context.Vehiculos.AsNoTracking().FirstOrDefaultAsync(v => v.IdVehiculo == id);
        return vehiculo == null ? NotFound(NoEncontrado()) : Ok(ToDto(vehiculo));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<VehiculoDto>> Create([FromBody] CrearVehiculoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        ValidarEstado(request.Estado);

        var vehiculo = new Vehiculo
        {
            Placa = request.Placa.Trim().ToUpperInvariant(),
            Tipo = request.Tipo.Trim(),
            CapacidadKg = request.CapacidadKg,
            CapacidadM3 = request.CapacidadM3,
            Estado = request.Estado
        };

        _context.Vehiculos.Add(vehiculo);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vehiculo.IdVehiculo }, ToDto(vehiculo));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<VehiculoDto>> Update(int id, [FromBody] ActualizarVehiculoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        ValidarEstado(request.Estado);

        var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id)
            ?? throw new BusinessException("Vehículo no encontrado.", 404);

        vehiculo.Placa = request.Placa.Trim().ToUpperInvariant();
        vehiculo.Tipo = request.Tipo.Trim();
        vehiculo.CapacidadKg = request.CapacidadKg;
        vehiculo.CapacidadM3 = request.CapacidadM3;
        vehiculo.Estado = request.Estado;

        await _context.SaveChangesAsync();
        return Ok(ToDto(vehiculo));
    }

    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = $"{Roles.AdministradorSistema},{Roles.AdministradorLogistico}")]
    public async Task<ActionResult<VehiculoDto>> CambiarEstado(int id, [FromBody] CambiarEstadoVehiculoRequest request)
    {
        ValidarEstado(request.Estado);

        var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == id)
            ?? throw new BusinessException("Vehículo no encontrado.", 404);

        vehiculo.Estado = request.Estado;
        await _context.SaveChangesAsync();

        return Ok(ToDto(vehiculo));
    }

    private static void ValidarEstado(string estado)
    {
        if (!EstadosValidos.Contains(estado))
        {
            throw new BusinessException("El estado del vehículo debe ser Disponible, En ruta, Mantenimiento o Inhabilitado.");
        }
    }

    private static ApiError NoEncontrado() => new() { Codigo = 404, Mensaje = "Vehículo no encontrado." };

    private static VehiculoDto ToDto(Vehiculo v) => new()
    {
        IdVehiculo = v.IdVehiculo,
        Placa = v.Placa,
        Tipo = v.Tipo,
        CapacidadKg = v.CapacidadKg,
        CapacidadM3 = v.CapacidadM3,
        Estado = v.Estado
    };
}
