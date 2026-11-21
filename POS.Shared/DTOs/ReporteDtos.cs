using POS.Shared.Utils;

namespace POS.Shared.DTOs;

public class ReporteEnvioDto
{
    public int IdGuia { get; set; }
    public string CodigoGuia { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string AlmacenOrigen { get; set; } = string.Empty;
    public string TipoCarga { get; set; } = string.Empty;
    public string Destinatario { get; set; } = string.Empty;
    public string DireccionDestino { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public decimal VolumenM3 { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public string? EstadoHojaRuta { get; set; }
    public string? Vehiculo { get; set; }
    public string? Conductor { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public decimal? HorasTransito { get; set; }
    public int TotalIncidencias { get; set; }
}

public class TrazabilidadEventoDto
{
    public string TipoEvento { get; set; } = string.Empty;
    public string Hito { get; set; } = string.Empty;
    public byte OrdenHito { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Estacion { get; set; }
}

public class TrazabilidadDto
{
    public string CodigoGuia { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public List<TrazabilidadEventoDto> Eventos { get; set; } = new();

    // Datos geográficos necesarios para dibujar la ruta en el rastreo público.
    public double? LatitudOrigen { get; set; }
    public double? LongitudOrigen { get; set; }
    public double? LatitudDestino { get; set; }
    public double? LongitudDestino { get; set; }
    public double DistanciaKm { get; set; }
    public int DuracionEstimadaMin { get; set; }

    public bool TieneMapa => GeoCalculos.SonValidas(LatitudOrigen, LongitudOrigen) &&
                             GeoCalculos.SonValidas(LatitudDestino, LongitudDestino);
}

public class DashboardDto
{
    public int TotalGuias { get; set; }
    public int GuiasRegistradas { get; set; }
    public int GuiasEnTransito { get; set; }
    public int GuiasEntregadas { get; set; }
    public int HojasActivas { get; set; }
    public int VehiculosDisponibles { get; set; }
    public int TotalIncidencias { get; set; }
    public int NotificacionesNoLeidas { get; set; }
    public decimal PesoTotalEntregado { get; set; }
    public double PromedioHorasEntrega { get; set; }
    public List<GuiaDto> UltimasGuias { get; set; } = new();
    public List<NotificacionDto> UltimasNotificaciones { get; set; } = new();
    public List<ConteoEstadoDto> GuiasPorHito { get; set; } = new();
}

public class ConteoEstadoDto
{
    public string Etiqueta { get; set; } = string.Empty;
    public int Total { get; set; }
}
