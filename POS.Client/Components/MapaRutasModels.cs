namespace POS.Client.Components;

/// <summary>Parámetros del mapa que puede enlazar cualquier página o diálogo.</summary>
public class MapaRutaDto
{
    public PuntoMapaDto? Origen { get; set; }
    public PuntoMapaDto? Destino { get; set; }
    public List<PuntoMapaDto> Waypoints { get; set; } = new();

    public bool PuedeTrazar => PuntoMapaDto.EsValido(Origen) && PuntoMapaDto.EsValido(Destino);
}

public class PuntoMapaDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }

    public static bool EsValido(PuntoMapaDto? punto) =>
        punto != null && punto.Lat >= -90 && punto.Lat <= 90 && punto.Lng >= -180 && punto.Lng <= 180;
}

public record ProgresoRuta(double Progreso, double Latitud, double Longitud, double RestanteKm, int RestanteMin);
