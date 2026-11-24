using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/conductores")]
[Authorize(Roles = Roles.AdministradorSistema)]
public class ConductoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConductoresController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ConductorDto>>> Get([FromQuery] bool? activo, [FromQuery] string? buscar)
    {
        var idRol = await ObtenerIdRolConductorAsync();
        var query = _context.Usuarios.AsNoTracking().Where(u => u.IdRol == idRol);

        if (activo.HasValue)
        {
            query = query.Where(u => u.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(u => u.NombreCompleto.Contains(term) || u.Correo.Contains(term));
        }

        var items = await query
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new ConductorDto
            {
                IdConductor = u.IdUsuario,
                NombreCompleto = u.NombreCompleto,
                Correo = u.Correo,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConductorDto>> GetById(int id)
    {
        var idRol = await ObtenerIdRolConductorAsync();
        var item = await ProyectarAsync(id, idRol);

        return item == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "Conductor no encontrado." })
            : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ConductorDto>> Create([FromBody] CrearConductorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var idRol = await ObtenerIdRolConductorAsync();
        var correo = request.Correo.Trim().ToLowerInvariant();

        if (await _context.Usuarios.AnyAsync(u => u.Correo.ToLower() == correo))
        {
            throw new BusinessException("Ya existe un usuario registrado con ese correo.", 409);
        }

        var conductor = new POS.Shared.Entities.Usuario
        {
            IdRol = idRol,
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = request.Activo
        };

        _context.Usuarios.Add(conductor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = conductor.IdUsuario },
            await ProyectarAsync(conductor.IdUsuario, idRol));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ConductorDto>> Update(int id, [FromBody] ActualizarConductorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var idRol = await ObtenerIdRolConductorAsync();
        var conductor = await ObtenerConductorAsync(id, idRol);

        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Correo.ToLower() == correo))
        {
            throw new BusinessException("Ya existe otro usuario registrado con ese correo.", 409);
        }

        conductor.NombreCompleto = request.NombreCompleto.Trim();
        conductor.Correo = correo;
        conductor.Activo = request.Activo;

        await _context.SaveChangesAsync();
        return Ok(await ProyectarAsync(id, idRol));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<object>> CambiarEstado(int id, [FromBody] CambiarEstadoConductorRequest request)
    {
        var idRol = await ObtenerIdRolConductorAsync();
        var conductor = await ObtenerConductorAsync(id, idRol);

        if (!request.Activo && id == User.GetUserId())
        {
            throw new BusinessException("No puede inhabilitar su propia cuenta.");
        }

        conductor.Activo = request.Activo;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = request.Activo
                ? "Conductor habilitado correctamente."
                : "Conductor inhabilitado correctamente.",
            conductor = await ProyectarAsync(id, idRol)
        });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var idRol = await ObtenerIdRolConductorAsync();
        var conductor = await ObtenerConductorAsync(id, idRol);

        conductor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contraseña restablecida correctamente." });
    }

    private async Task<int> ObtenerIdRolConductorAsync()
    {
        var rol = await _context.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Nombre == Roles.Conductor);

        if (rol == null)
        {
            throw new BusinessException("No existe el rol Conductor/Operador en el catálogo de roles.", 404);
        }

        return rol.IdRol;
    }

    private async Task<POS.Shared.Entities.Usuario> ObtenerConductorAsync(int id, int idRol) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id && u.IdRol == idRol)
        ?? throw new BusinessException("Conductor no encontrado.", 404);

    private Task<ConductorDto?> ProyectarAsync(int id, int idRol) =>
        _context.Usuarios.AsNoTracking()
            .Where(u => u.IdUsuario == id && u.IdRol == idRol)
            .Select(u => new ConductorDto
            {
                IdConductor = u.IdUsuario,
                NombreCompleto = u.NombreCompleto,
                Correo = u.Correo,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            })
            .FirstOrDefaultAsync();
}
