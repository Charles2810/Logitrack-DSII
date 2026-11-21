namespace POS.Shared.Utils;

/// <summary>
/// Cálculos geográficos ligeros usados por la API y por el cliente Blazor.
/// La ciudad base del sistema es Santa Cruz de la Sierra (Bolivia).
/// </summary>
public static class GeoCalculos
{
    public const double LatitudSantaCruz = -17.783700;
    public const double LongitudSantaCruz = -63.180426;
    public const int ZoomSantaCruz = 13;

    private const double RadioTierraKm = 6371.0088;

    /// <summary>Distancia aproximada en km entre dos puntos (fórmula de Haversine).</summary>
    public static double DistanciaKm(double? latitudOrigen, double? longitudOrigen,
                                     double? latitudDestino, double? longitudDestino)
    {
        if (!SonValidas(latitudOrigen, longitudOrigen) || !SonValidas(latitudDestino, longitudDestino))
        {
            return 0;
        }

        var lat1 = GradosARadianes(latitudOrigen!.Value);
        var lat2 = GradosARadianes(latitudDestino!.Value);
        var deltaLat = GradosARadianes(latitudDestino.Value - latitudOrigen.Value);
        var deltaLon = GradosARadianes(longitudDestino!.Value - longitudOrigen!.Value);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(RadioTierraKm * c, 2);
    }

    /// <summary>ETA sencillo para entregas urbanas; se usa como estimación inicial.</summary>
    public static int DuracionEstimadaMin(double distanciaKm, double velocidadKmh = 32)
    {
        if (distanciaKm <= 0)
        {
            return 0;
        }

        var velocidad = velocidadKmh <= 0 ? 32 : velocidadKmh;
        return (int)Math.Ceiling(distanciaKm / velocidad * 60);
    }

    public static bool SonValidas(double? latitud, double? longitud) =>
        latitud.HasValue && longitud.HasValue &&
        latitud.Value >= -90 && latitud.Value <= 90 &&
        longitud.Value >= -180 && longitud.Value <= 180;

    private static double GradosARadianes(double grados) => grados * Math.PI / 180.0;
}
