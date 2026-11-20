using System.ComponentModel.DataAnnotations;

namespace POS.Shared.DTOs;

public class CheckpointDto
{
    public int IdCheckpoint { get; set; }
    public int IdGuia { get; set; }
    public int IdHito { get; set; }
    public string Hito { get; set; } = string.Empty;
    public byte OrdenHito { get; set; }
    public int? IdAlmacen { get; set; }
    public string? Almacen { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string RolUsuario { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}

public class RegistrarCheckpointRequest
{
    [Range(1, int.MaxValue)]
    public int IdGuia { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el hito.")]
    public int IdHito { get; set; }

    public int? IdAlmacen { get; set; }
}

public class IncidenciaDto
{
    public int IdIncidencia { get; set; }
    public int IdGuia { get; set; }
    public string CodigoGuia { get; set; } = string.Empty;
    public int IdTipoIncidencia { get; set; }
    public string TipoIncidencia { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string RutaFoto { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
}

public class CrearIncidenciaRequest
{
    [Range(1, int.MaxValue)]
    public int IdGuia { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de incidencia.")]
    public int IdTipoIncidencia { get; set; }

    [Required, MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fotografía de evidencia es obligatoria (RN08).")]
    [MaxLength(300)]
    public string RutaFoto { get; set; } = string.Empty;
}

public class EntregaDto
{
    public int IdEntrega { get; set; }
    public int IdGuia { get; set; }
    public string CodigoGuia { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? NombreReceptor { get; set; }
    public string? RutaFirma { get; set; }
    public string? RutaFoto { get; set; }
    public DateTime FechaHora { get; set; }
}

public class CrearEntregaRequest
{
    [Range(1, int.MaxValue)]
    public int IdGuia { get; set; }

    [MaxLength(120)]
    public string? NombreReceptor { get; set; }

    [MaxLength(300)]
    public string? RutaFirma { get; set; }

    [MaxLength(300)]
    public string? RutaFoto { get; set; }
}

public class HojaRutaDto
{
    public int IdHojaRuta { get; set; }
    public int IdVehiculo { get; set; }
    public string VehiculoPlaca { get; set; } = string.Empty;
    public string VehiculoTipo { get; set; } = string.Empty;
    public int IdConductor { get; set; }
    public string Conductor { get; set; } = string.Empty;
    public int IdUsuarioCreador { get; set; }
    public string UsuarioCreador { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int TotalGuias { get; set; }
    public int GuiasEntregadas { get; set; }
    public decimal PesoTotal { get; set; }
    public decimal VolumenTotal { get; set; }
    public List<GuiaDto> Guias { get; set; } = new();
}

public class CrearHojaRutaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un vehículo.")]
    public int IdVehiculo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un conductor.")]
    public int IdConductor { get; set; }
}

public class RecursoConductorDto
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class RecursoVehiculoDto
{
    public int IdVehiculo { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal CapacidadKg { get; set; }
    public decimal CapacidadM3 { get; set; }
}

public class RecursosHojaRutaDto
{
    public List<RecursoConductorDto> Conductores { get; set; } = new();
    public List<RecursoVehiculoDto> Vehiculos { get; set; } = new();
}

public class MovimientoInventarioDto
{
    public int IdMovimiento { get; set; }
    public int IdAlmacen { get; set; }
    public string Almacen { get; set; } = string.Empty;
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public int? IdGuia { get; set; }
    public string? CodigoGuia { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string TipoMovimiento { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public DateTime FechaHora { get; set; }
}

public class CrearMovimientoRequest
{
    [Range(1, int.MaxValue)]
    public int IdAlmacen { get; set; }

    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    public int? IdGuia { get; set; }

    [Required]
    public string TipoMovimiento { get; set; } = "ENTRADA";

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }
}

public class InventarioDto
{
    public int IdAlmacen { get; set; }
    public string Almacen { get; set; } = string.Empty;
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public int StockActual { get; set; }
}

public class NotificacionDto
{
    public int IdNotificacion { get; set; }
    public int IdUsuario { get; set; }
    public int? IdGuia { get; set; }
    public string? CodigoGuia { get; set; }
    public int? IdHojaRuta { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaHora { get; set; }
}
