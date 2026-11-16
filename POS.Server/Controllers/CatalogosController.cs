using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.Constants;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/catalogos")]
[Authorize]
public class CatalogosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<CatalogosDto>> Get()
    {
        var conductores = await ConductoresQuery().ToListAsync();

        var dto = new CatalogosDto
        {
            Roles = await _context.Roles.AsNoTracking()
                .OrderBy(r => r.IdRol)
                .Select(r => new RolDto { IdRol = r.IdRol, Nombre = r.Nombre })
                .ToListAsync(),

            Almacenes = await _context.Almacenes.AsNoTracking()
                .OrderBy(a => a.Nombre)
                .Select(a => new AlmacenDto
                {
                    IdAlmacen = a.IdAlmacen,
                    Nombre = a.Nombre,
                    Tipo = a.Tipo,
                    Ciudad = a.Ciudad,
                    Direccion = a.Direccion
                })
                .ToListAsync(),

            TiposCarga = await _context.TiposCarga.AsNoTracking()
                .OrderBy(t => t.Nombre)
                .Select(t => new TipoCargaDto { IdTipoCarga = t.IdTipoCarga, Nombre = t.Nombre })
                .ToListAsync(),

            Hitos = await _context.Hitos.AsNoTracking()
                .OrderBy(h => h.Orden)
                .Select(h => new HitoDto { IdHito = h.IdHito, Nombre = h.Nombre, Orden = h.Orden })
                .ToListAsync(),

            TiposIncidencia = await _context.TiposIncidencia.AsNoTracking()
                .OrderBy(t => t.Nombre)
                .Select(t => new TipoIncidenciaDto { IdTipoIncidencia = t.IdTipoIncidencia, Nombre = t.Nombre })
                .ToListAsync(),

            Clientes = await _context.Clientes.AsNoTracking()
                .OrderBy(c => c.RazonSocial)
                .Select(c => new ClienteDto
                {
                    IdCliente = c.IdCliente,
                    RazonSocial = c.RazonSocial,
                    Nit = c.Nit,
                    Telefono = c.Telefono,
                    Direccion = c.Direccion
                })
                .ToListAsync(),

            Productos = await _context.Productos.AsNoTracking()
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoDto
                {
                    IdProducto = p.IdProducto,
                    Sku = p.Sku,
                    Nombre = p.Nombre,
                    PesoUnitarioKg = p.PesoUnitarioKg,
                    VolumenUnitarioM3 = p.VolumenUnitarioM3
                })
                .ToListAsync(),

            Vehiculos = await _context.Vehiculos.AsNoTracking()
                .OrderBy(v => v.Placa)
                .Select(v => new VehiculoDto
                {
                    IdVehiculo = v.IdVehiculo,
                    Placa = v.Placa,
                    Tipo = v.Tipo,
                    CapacidadKg = v.CapacidadKg,
                    CapacidadM3 = v.CapacidadM3,
                    Estado = v.Estado
                })
                .ToListAsync(),

            Conductores = conductores,
            PersonalAlmacen = await UsuariosPorRolQuery(Roles.PersonalAlmacen).ToListAsync(),
            Supervisores = await UsuariosPorRolQuery(Roles.CentroControl).ToListAsync()
        };

        return Ok(dto);
    }

    [HttpGet("conductores")]
    public async Task<ActionResult<List<UsuarioDto>>> Conductores()
        => Ok(await ConductoresQuery().ToListAsync());

    private IQueryable<UsuarioDto> ConductoresQuery() => UsuariosPorRolQuery(Roles.Conductor);

    private IQueryable<UsuarioDto> UsuariosPorRolQuery(string rol) =>
        _context.Usuarios.AsNoTracking()
            .Where(u => u.Rol!.Nombre == rol)
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new UsuarioDto
            {
                IdUsuario = u.IdUsuario,
                IdRol = u.IdRol,
                Rol = u.Rol!.Nombre,
                NombreCompleto = u.NombreCompleto,
                Correo = u.Correo,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            });
}
