using Microsoft.IdentityModel.Tokens;
using POS.Server.Infrastructure;
using POS.Server.Repositories;
using POS.Shared.DTOs;
using POS.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace POS.Server.Services;

/// <summary>
/// Servicio de aplicación para autenticación y autorización.
/// Aplica el patrón Repositorio para la persistencia y el patrón Singleton para la configuración de seguridad.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IConfiguration _configuration;
    private readonly IAppSecurityConfigSingleton _securityConfig;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IConfiguration configuration,
        IAppSecurityConfigSingleton securityConfig)
    {
        _usuarioRepository = usuarioRepository;
        _configuration = configuration;
        _securityConfig = securityConfig;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var usuario = await _usuarioRepository.GetByCorreoAsync(request.Correo);

        if (usuario == null || !usuario.Activo)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            return null;
        }

        return GenerateAuthResponse(usuario);
    }

    public async Task<AuthResponse> RegistroAsync(RegistroRequest request)
    {
        var correo = request.Correo.Trim().ToLowerInvariant();

        // 1. Validar si el correo ya existe
        if (await _usuarioRepository.ExisteCorreoAsync(correo))
        {
            throw new BusinessException("El correo electrónico ya se encuentra registrado en el sistema.", 400);
        }

        // 2. Encriptar contraseña usando BCrypt con el factor de trabajo del Singleton
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, _securityConfig.BcryptWorkFactor);

        // 3. Crear entidad Usuario (Rol 4 = Cliente por defecto si no se especifica)
        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            Correo = correo,
            PasswordHash = passwordHash,
            IdRol = request.IdRol ?? 4,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        // 4. Persistir mediante el patrón Repositorio
        await _usuarioRepository.AddAsync(usuario);
        await _usuarioRepository.SaveChangesAsync();

        // 5. Cargar el usuario con su rol asociado para generar el token
        var usuarioGuardado = await _usuarioRepository.GetByCorreoAsync(correo);
        return GenerateAuthResponse(usuarioGuardado ?? usuario);
    }

    public async Task<UsuarioSesionDto?> GetSesionAsync(int idUsuario)
    {
        return await _usuarioRepository.GetSesionDtoAsync(idUsuario);
    }

    public async Task<bool> CambiarPasswordAsync(int idUsuario, CambiarPasswordRequest request)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(idUsuario);
        if (usuario == null || !usuario.Activo)
        {
            throw new BusinessException("El usuario no existe o está inhabilitado.", 404);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.PasswordHash))
        {
            throw new BusinessException("La contraseña actual no es correcta.");
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva, _securityConfig.BcryptWorkFactor);
        _usuarioRepository.Update(usuario);
        await _usuarioRepository.SaveChangesAsync();
        return true;
    }

    private AuthResponse GenerateAuthResponse(Usuario usuario)
    {
        var (token, expira) = GenerateToken(usuario);

        return new AuthResponse
        {
            Token = token,
            Expira = expira,
            IdUsuario = usuario.IdUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.Correo,
            IdRol = usuario.IdRol,
            Rol = usuario.Rol?.Nombre ?? "Cliente"
        };
    }

    private (string Token, DateTime Expira) GenerateToken(Usuario usuario)
    {
        var section = _configuration.GetSection("Jwt");
        var key = section["Key"] ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");
        var issuer = section["Issuer"] ?? "LogiTrack";
        var audience = section["Audience"] ?? "LogiTrackClient";
        var minutes = int.TryParse(section["ExpiresMinutes"], out var m) ? m : _securityConfig.JwtExpiresMinutesDefault;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol?.Nombre ?? "Cliente"),
            new Claim("id_rol", usuario.IdRol.ToString())
        };

        var expira = DateTime.UtcNow.AddMinutes(minutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expira,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return (handler.WriteToken(token), expira);
    }
}
