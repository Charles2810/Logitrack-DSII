using System.ComponentModel.DataAnnotations;

namespace POS.Shared.DTOs;

public class RolDto
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CrearUsuarioRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    public int IdRol { get; set; }

    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string Password { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class ActualizarUsuarioRequest
{
    [Range(1, int.MaxValue)]
    public int IdRol { get; set; }

    [Required, MaxLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(120)]
    public string Correo { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class CambiarEstadoUsuarioRequest
{
    public bool Activo { get; set; }
}

public class ClienteDto
{
    public int IdCliente { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}

public class CrearClienteRequest
{
    [Required, MaxLength(150)]
    public string RazonSocial { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Nit { get; set; } = string.Empty;

    [MaxLength(25)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? Direccion { get; set; }

    [Range(-90, 90)]
    public double? Latitud { get; set; }

    [Range(-180, 180)]
    public double? Longitud { get; set; }
}

public class AlmacenDto
{
    public int IdAlmacen { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}

public class TipoCargaDto
{
    public int IdTipoCarga { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class HitoDto
{
    public int IdHito { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Orden { get; set; }
}

public class TipoIncidenciaDto
{
    public int IdTipoIncidencia { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class VehiculoDto
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal CapacidadKg { get; set; }
    public decimal CapacidadM3 { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class CrearVehiculoRequest
{
    [Required, MaxLength(15)]
    public string Placa { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Tipo { get; set; } = string.Empty;

    [Range(0.01, 9999999, ErrorMessage = "La capacidad en kg debe ser mayor a cero.")]
    public decimal CapacidadKg { get; set; }

    [Range(0.01, 9999999, ErrorMessage = "La capacidad en m3 debe ser mayor a cero.")]
    public decimal CapacidadM3 { get; set; }

    [Required]
    public string Estado { get; set; } = "Disponible";
}

public class ActualizarVehiculoRequest
{
    [Required, MaxLength(15)]
    public string Placa { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Tipo { get; set; } = string.Empty;

    [Range(0.01, 9999999)]
    public decimal CapacidadKg { get; set; }

    [Range(0.01, 9999999)]
    public decimal CapacidadM3 { get; set; }

    [Required]
    public string Estado { get; set; } = "Disponible";
}

public class CambiarEstadoVehiculoRequest
{
    [Required]
    public string Estado { get; set; } = "Disponible";
}

public class ProductoDto
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PesoUnitarioKg { get; set; }
    public decimal VolumenUnitarioM3 { get; set; }
}

public class CrearProductoRequest
{
    [Required, MaxLength(40)]
    public string Sku { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Range(0.001, 9999999)]
    public decimal PesoUnitarioKg { get; set; }

    [Range(0.0001, 9999999)]
    public decimal VolumenUnitarioM3 { get; set; }
}

/// <summary>Catálogos que el cliente carga una sola vez por pantalla.</summary>
public class CatalogosDto
{
    public List<RolDto> Roles { get; set; } = new();
    public List<AlmacenDto> Almacenes { get; set; } = new();
    public List<TipoCargaDto> TiposCarga { get; set; } = new();
    public List<HitoDto> Hitos { get; set; } = new();
    public List<TipoIncidenciaDto> TiposIncidencia { get; set; } = new();
    public List<ClienteDto> Clientes { get; set; } = new();
    public List<ProductoDto> Productos { get; set; } = new();
    public List<VehiculoDto> Vehiculos { get; set; } = new();
    public List<UsuarioDto> Conductores { get; set; } = new();
    public List<UsuarioDto> PersonalAlmacen { get; set; } = new();
    public List<UsuarioDto> Supervisores { get; set; } = new();
}
