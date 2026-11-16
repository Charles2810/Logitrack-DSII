using POS.Shared.DTOs;
using POS.Shared.Entities;

namespace POS.Server.Repositories;

/// <summary>
/// Contrato específico del repositorio para la entidad Usuario.
/// </summary>
public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByCorreoAsync(string correo);
    Task<bool> ExisteCorreoAsync(string correo);
    Task<UsuarioSesionDto?> GetSesionDtoAsync(int idUsuario);
}
