using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Server.Infrastructure;
using POS.Shared.Constants;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.AdministradorSistema)]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioDto>>> Get([FromQuery] bool? activo, [FromQuery] string? buscar)
    {
        var query = _context.Usuarios.AsNoTracking().Include(u => u.Rol).AsQueryable();

        if (activo.HasValue)
        {
            query = query.Where(u => u.Activo == activo.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var term = buscar.Trim();
            query = query.Where(u =>
                u.NombreCompleto.Contains(term) ||
                u.Correo.Contains(term) ||
                u.Rol!.Nombre.Contains(term));
        }

        var items = await query
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
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> GetById(int id)
    {
        var item = await ProyectarAsync(id);

        return item == null
            ? NotFound(new ApiError { Codigo = 404, Mensaje = "Usuario no encontrado." })
            : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Create([FromBody] CrearUsuarioRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await ValidarRolAsync(request.IdRol);

        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Correo.ToLower() == correo))
        {
            throw new BusinessException("Ya existe un usuario registrado con ese correo.", 409);
        }

        var usuario = new POS.Shared.Entities.Usuario
        {
            IdRol = request.IdRol,
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correo,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = request.Activo
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = usuario.IdUsuario }, await ProyectarAsync(usuario.IdUsuario));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Update(int id, [FromBody] ActualizarUsuarioRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await ValidarRolAsync(request.IdRol);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id)
            ?? throw new BusinessException("Usuario no encontrado.", 404);

        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.IdUsuario != id && u.Correo.ToLower() == correo))
        {
            throw new BusinessException("Ya existe otro usuario registrado con ese correo.", 409);
        }

        usuario.IdRol = request.IdRol;
        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.Correo = correo;
        usuario.Activo = request.Activo;

        await _context.SaveChangesAsync();
        return Ok(await ProyectarAsync(id));
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<UsuarioDto>> CambiarEstado(int id, [FromBody] CambiarEstadoUsuarioRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id)
            ?? throw new BusinessException("Usuario no encontrado.", 404);

        if (!request.Activo && id == User.GetUserId())
        {
            throw new BusinessException("No puede inhabilitar su propia cuenta.");
        }

        usuario.Activo = request.Activo;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = request.Activo ? "Usuario habilitado correctamente." : "Usuario inhabilitado correctamente.",
            usuario = await ProyectarAsync(id)
        });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == id)
            ?? throw new BusinessException("Usuario no encontrado.", 404);

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Contraseña restablecida correctamente." });
    }

    private async Task ValidarRolAsync(int idRol)
    {
        if (!await _context.Roles.AnyAsync(r => r.IdRol == idRol))
        {
            throw new BusinessException("El rol seleccionado no existe.");
        }
    }

    private Task<UsuarioDto?> ProyectarAsync(int id) =>
        _context.Usuarios.AsNoTracking()
            .Where(u => u.IdUsuario == id)
            .Select(u => new UsuarioDto
            {
                IdUsuario = u.IdUsuario,
                IdRol = u.IdRol,
                Rol = u.Rol!.Nombre,
                NombreCompleto = u.NombreCompleto,
                Correo = u.Correo,
                Activo = u.Activo,
                FechaCreacion = u.FechaCreacion
            })
            .FirstOrDefaultAsync();
}
