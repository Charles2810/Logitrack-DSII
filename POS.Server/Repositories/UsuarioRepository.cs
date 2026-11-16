using Microsoft.EntityFrameworkCore;
using POS.Server.Data;
using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Repositories;

/// <summary>
/// Implementación especializada del repositorio de usuarios.
/// </summary>
public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByCorreoAsync(string correo)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        return await _dbSet
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Correo.ToLower() == correoNormalizado);
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        return await _dbSet.AnyAsync(u => u.Correo.ToLower() == correoNormalizado);
    }

    public async Task<UsuarioSesionDto?> GetSesionDtoAsync(int idUsuario)
    {
        return await _dbSet
            .Include(u => u.Rol)
            .AsNoTracking()
            .Where(u => u.IdUsuario == idUsuario && u.Activo)
            .Select(u => new UsuarioSesionDto
            {
                IdUsuario = u.IdUsuario,
                NombreCompleto = u.NombreCompleto,
                Correo = u.Correo,
                IdRol = u.IdRol,
                Rol = u.Rol!.Nombre
            })
            .FirstOrDefaultAsync();
    }
}
