using System.ComponentModel.DataAnnotations;

namespace POS.Shared.DTOs;

public class ConductorDto
{
    public int IdConductor { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CrearConductorRequest
{
    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class ActualizarConductorRequest
{
    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class CambiarEstadoConductorRequest
{
    public bool Activo { get; set; }
}
