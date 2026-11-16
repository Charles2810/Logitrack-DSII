using POS.Shared.DTOs;

namespace POS.Server.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegistroAsync(RegistroRequest request);
    Task<UsuarioSesionDto?> GetSesionAsync(int idUsuario);
    Task<bool> CambiarPasswordAsync(int idUsuario, CambiarPasswordRequest request);
}
