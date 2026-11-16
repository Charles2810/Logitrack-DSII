using System.ComponentModel.DataAnnotations;

namespace POS.Shared.DTOs;

public class DetalleGuiaDto
{
    public int IdProducto { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public int Cantidad { get; set; }
}

public class DetalleGuiaRequest
{
    [Range(1, int.MaxValue)]
    public int IdProducto { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; }
}

public class CrearGuiaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un cliente.")]
    public int IdCliente { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el almacén de origen.")]
    public int IdAlmacenOrigen { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el tipo de carga.")]
    public int IdTipoCarga { get; set; }

    public int? IdHojaRuta { get; set; }

    [Required, MaxLength(120)]
    public string Destinatario { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DireccionDestino { get; set; } = string.Empty;

    [Range(0.01, 9_999_999, ErrorMessage = "El peso debe ser mayor a cero.")]
    public decimal PesoKg { get; set; }

    [Range(0.001, 9_999_999, ErrorMessage = "El volumen debe ser mayor a cero.")]
    public decimal VolumenM3 { get; set; }

    public List<DetalleGuiaRequest> Detalles { get; set; } = new();
}

public class ActualizarGuiaRequest
{
    [Range(1, int.MaxValue)]
    public int IdCliente { get; set; }

    [Range(1, int.MaxValue)]
    public int IdAlmacenOrigen { get; set; }

    [Range(1, int.MaxValue)]
    public int IdTipoCarga { get; set; }

    [Required, MaxLength(120)]
    public string Destinatario { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DireccionDestino { get; set; } = string.Empty;

    [Range(0.01, 9_999_999)]
    public decimal PesoKg { get; set; }

    [Range(0.001, 9_999_999)]
    public decimal VolumenM3 { get; set; }

    public List<DetalleGuiaRequest>? Detalles { get; set; }
}

public class AsignarGuiaRequest
{
    public int? IdHojaRuta { get; set; }
}

public class GuiaDto
{
    public int IdGuia { get; set; }
    public string CodigoGuia { get; set; } = string.Empty;
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public int IdAlmacenOrigen { get; set; }
    public string AlmacenOrigen { get; set; } = string.Empty;
    public int IdTipoCarga { get; set; }
    public string TipoCarga { get; set; } = string.Empty;
    public int IdHitoActual { get; set; }
    public string HitoActual { get; set; } = string.Empty;
    public byte OrdenHito { get; set; }
    public int? IdHojaRuta { get; set; }
    public string? EstadoHojaRuta { get; set; }
    public int IdUsuarioRegistro { get; set; }
    public string UsuarioRegistro { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string DireccionDestino { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal VolumenM3 { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int TotalDetalles { get; set; }
    public int TotalIncidencias { get; set; }
    public bool TieneEntrega { get; set; }
    public List<DetalleGuiaDto>? Detalles { get; set; }
}

public class GuiaFiltro
{
    public string? Buscar { get; set; }
    public int? IdHito { get; set; }
    public int? IdCliente { get; set; }
    public int? IdHojaRuta { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
