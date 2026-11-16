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
[Route("api/hojas-ruta")]
[Authorize]
public class HojasRutaController : ControllerBase
{
    private readonly AppDbContext _context;

    public HojasRutaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HojaRutaDto>>> Get(
        [FromQuery] string? estado,
        [FromQuery] int? idConductor,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.HojasRuta.AsNoTracking().AsQueryable();

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(h => h.IdConductor == idUsuario);
        }
        else if (idConductor.HasValue)
        {
            query = query.Where(h => h.IdConductor == idConductor.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(h => h.Estado == estado);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(h => h.FechaCreacion)
            .ThenByDescending(h => h.IdHojaRuta)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HojaRutaDto
            {
                IdHojaRuta = h.IdHojaRuta,
                IdVehiculo = h.IdVehiculo,
                VehiculoPlaca = h.Vehiculo!.Placa,
                VehiculoTipo = h.Vehiculo!.Tipo,
                IdConductor = h.IdConductor,
                Conductor = h.Conductor!.NombreCompleto,
                IdUsuarioCreador = h.IdUsuarioCreador,
                UsuarioCreador = h.UsuarioCreador!.NombreCompleto,
                Estado = h.Estado,
                FechaCreacion = h.FechaCreacion,
                TotalGuias = h.Guias.Count,
                GuiasEntregadas = h.Guias.Count(g => g.HitoActual!.Nombre == Hitos.Entregado),
                PesoTotal = h.Guias.Sum(g => (decimal?)g.PesoKg) ?? 0,
                VolumenTotal = h.Guias.Sum(g => (decimal?)g.VolumenM3) ?? 0
            })
            .ToListAsync();

        return Ok(new PagedResult<HojaRutaDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<HojaRutaDto>> GetById(int id)
    {
        var hoja = await ObtenerAsync(id);

        return hoja == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "Hoja de ruta no encontrada o sin acceso para su rol." })
            : Ok(hoja);
    }

    private async Task<HojaRutaDto?> ObtenerAsync(int id)
    {
        var query = _context.HojasRuta.AsNoTracking().AsQueryable();

        if (User.IsInRole(Roles.Conductor))
        {
            var idUsuario = User.GetUserId();
            query = query.Where(h => h.IdConductor == idUsuario);
        }

        var hoja = await query
            .Where(h => h.IdHojaRuta == id)
            .Select(h => new HojaRutaDto
            {
                IdHojaRuta = h.IdHojaRuta,
                IdVehiculo = h.IdVehiculo,
                VehiculoPlaca = h.Vehiculo!.Placa,
                VehiculoTipo = h.Vehiculo!.Tipo,
                IdConductor = h.IdConductor,
                Conductor = h.Conductor!.NombreCompleto,
                IdUsuarioCreador = h.IdUsuarioCreador,
                UsuarioCreador = h.UsuarioCreador!.NombreCompleto,
                Estado = h.Estado,
                FechaCreacion = h.FechaCreacion,
                TotalGuias = h.Guias.Count,
                GuiasEntregadas = h.Guias.Count(g => g.HitoActual!.Nombre == Hitos.Entregado),
                PesoTotal = h.Guias.Sum(g => (decimal?)g.PesoKg) ?? 0,
                VolumenTotal = h.Guias.Sum(g => (decimal?)g.VolumenM3) ?? 0
            })
            .FirstOrDefaultAsync();

        if (hoja == null)
        {
            return null;
        }

        hoja.Guias = await _context.GuiasEnvio.AsNoTracking()
            .Where(g => g.IdHojaRuta == id)
            .OrderBy(g => g.FechaRegistro)
            .ProyectarResumen()
            .ToListAsync();

        return hoja;
    }

    [HttpGet("mias")]
    [Authorize(Roles = Roles.Conductor)]
    public async Task<ActionResult<List<HojaRutaDto>>> Mias([FromQuery] bool soloActivas = true)
    {
        var idUsuario = User.GetUserId();

        var query = _context.HojasRuta.AsNoTracking()
            .Where(h => h.IdConductor == idUsuario);

        if (soloActivas)
        {
            query = query.Where(h => h.Estado != Estados.HojaFinalizada);
        }

        var items = await query
            .OrderByDescending(h => h.FechaCreacion)
            .Select(h => new HojaRutaDto
            {
                IdHojaRuta = h.IdHojaRuta,
                IdVehiculo = h.IdVehiculo,
                VehiculoPlaca = h.Vehiculo!.Placa,
                VehiculoTipo = h.Vehiculo!.Tipo,
                IdConductor = h.IdConductor,
                Conductor = h.Conductor!.NombreCompleto,
                IdUsuarioCreador = h.IdUsuarioCreador,
                UsuarioCreador = h.UsuarioCreador!.NombreCompleto,
                Estado = h.Estado,
                FechaCreacion = h.FechaCreacion,
                TotalGuias = h.Guias.Count,
                GuiasEntregadas = h.Guias.Count(g => g.HitoActual!.Nombre == Hitos.Entregado),
                PesoTotal = h.Guias.Sum(g => (decimal?)g.PesoKg) ?? 0,
                VolumenTotal = h.Guias.Sum(g => (decimal?)g.VolumenM3) ?? 0
            })
            .ToListAsync();

        foreach (var hoja in items)
        {
            hoja.Guias = await _context.GuiasEnvio.AsNoTracking()
                .Where(g => g.IdHojaRuta == hoja.IdHojaRuta)
                .OrderBy(g => g.FechaRegistro)
                .ProyectarResumen()
                .ToListAsync();
        }

        return Ok(items);
    }

    [HttpGet("recursos")]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult<RecursosHojaRutaDto>> Recursos()
    {
        var estadosActivos = new[] { Estados.HojaPendiente, Estados.HojaEnCurso };

        var conductoresOcupados = await _context.HojasRuta.AsNoTracking()
            .Where(h => estadosActivos.Contains(h.Estado))
            .Select(h => h.IdConductor)
            .Distinct()
            .ToListAsync();

        var vehiculosOcupados = await _context.HojasRuta.AsNoTracking()
            .Where(h => estadosActivos.Contains(h.Estado))
            .Select(h => h.IdVehiculo)
            .Distinct()
            .ToListAsync();

        var conductores = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Activo && u.Rol!.Nombre == Roles.Conductor && !conductoresOcupados.Contains(u.IdUsuario))
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new RecursoConductorDto
            {
                IdUsuario = u.IdUsuario,
                NombreCompleto = u.NombreCompleto
            })
            .ToListAsync();

        var vehiculos = await _context.Vehiculos.AsNoTracking()
            .Where(v => v.Estado == Estados.VehiculoDisponible && !vehiculosOcupados.Contains(v.IdVehiculo))
            .OrderBy(v => v.Placa)
            .Select(v => new RecursoVehiculoDto
            {
                IdVehiculo = v.IdVehiculo,
                Placa = v.Placa,
                Tipo = v.Tipo,
                CapacidadKg = v.CapacidadKg,
                CapacidadM3 = v.CapacidadM3
            })
            .ToListAsync();

        return Ok(new RecursosHojaRutaDto
        {
            Conductores = conductores,
            Vehiculos = vehiculos
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.AdministradorLogistico)]
    public async Task<ActionResult<HojaRutaDto>> Create([FromBody] CrearHojaRutaRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var vehiculo = await _context.Vehiculos.FirstOrDefaultAsync(v => v.IdVehiculo == request.IdVehiculo)
            ?? throw new BusinessException("El vehículo seleccionado no existe.", 404);

        var conductor = await _context.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.IdUsuario == request.IdConductor)
            ?? throw new BusinessException("El conductor seleccionado no existe.", 404);

        if (!conductor.Activo || conductor.Rol?.Nombre != Roles.Conductor)
        {
            throw new BusinessException("El usuario seleccionado no es un Conductor/Operador activo.");
        }

        if (vehiculo.Estado is Estados.VehiculoMantenimiento or Estados.VehiculoInhabilitado)
        {
            throw new BusinessException("El vehículo está en mantenimiento o inhabilitado y no puede asignarse.");
        }

        if (await _context.HojasRuta.AnyAsync(h => h.IdVehiculo == vehiculo.IdVehiculo
                && (h.Estado == Estados.HojaPendiente || h.Estado == Estados.HojaEnCurso)))
        {
            throw new BusinessException("El vehículo ya tiene una hoja de ruta activa (RN04).");
        }

        if (await _context.HojasRuta.AnyAsync(h => h.IdConductor == conductor.IdUsuario
                && (h.Estado == Estados.HojaPendiente || h.Estado == Estados.HojaEnCurso)))
        {
            throw new BusinessException("El conductor ya tiene una hoja de ruta activa (RN04).");
        }

        var hoja = new HojaRuta
        {
            IdVehiculo = vehiculo.IdVehiculo,
            IdConductor = conductor.IdUsuario,
            IdUsuarioCreador = User.GetUserId(),
            Estado = Estados.HojaPendiente
        };

        _context.HojasRuta.Add(hoja);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = hoja.IdHojaRuta }, await ObtenerAsync(hoja.IdHojaRuta));
    }
}
