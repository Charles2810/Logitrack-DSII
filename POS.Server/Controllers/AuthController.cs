using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Infrastructure;
using POS.Server.Services;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new ApiError
            {
                Codigo = 401,
                Mensaje = "Correo o contraseña incorrectos, o el usuario está inhabilitado."
            });
        }

        return Ok(response);
    }

    [HttpPost("registro")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Registro([FromBody] RegistroRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var response = await _authService.RegistroAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UsuarioSesionDto>> Me()
    {
        var sesion = await _authService.GetSesionAsync(User.GetUserId());
        if (sesion == null)
        {
            return NotFound(new ApiError { Codigo = 404, Mensaje = "El usuario ya no está activo." });
        }
        return Ok(sesion);
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await _authService.CambiarPasswordAsync(User.GetUserId(), request);
        return Ok(new { mensaje = "Contraseña actualizada correctamente." });
    }
}
