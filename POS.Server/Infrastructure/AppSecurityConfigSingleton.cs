namespace POS.Server.Infrastructure;

/// <summary>
/// Contrato para el patrón de diseño Singleton de configuración de seguridad.
/// </summary>
public interface IAppSecurityConfigSingleton
{
    int BcryptWorkFactor { get; }
    int JwtExpiresMinutesDefault { get; }
    string DefaultAlgorithm { get; }
    string ApplicationName { get; }
    DateTime InitializedAt { get; }
}

/// <summary>
/// Implementación del Patrón de Diseño Singleton (GoF) con inicialización diferida
/// segura para subprocesos (Thread-Safe Lazy Initialization).
/// Además de su acceso estático por Instance, se registra en el contenedor IoC
/// de ASP.NET Core con ciclo de vida Singleton (AddSingleton).
/// </summary>
public sealed class AppSecurityConfigSingleton : IAppSecurityConfigSingleton
{
    private static readonly Lazy<AppSecurityConfigSingleton> _instance =
        new Lazy<AppSecurityConfigSingleton>(() => new AppSecurityConfigSingleton());

    public static AppSecurityConfigSingleton Instance => _instance.Value;

    // Propiedades del Singleton
    public int BcryptWorkFactor { get; private set; }
    public int JwtExpiresMinutesDefault { get; private set; }
    public string DefaultAlgorithm { get; private set; }
    public string ApplicationName { get; private set; }
    public DateTime InitializedAt { get; private set; }

    /// <summary>
    /// Constructor privado para impedir instanciación directa externa (regla del patrón Singleton).
    /// </summary>
    public AppSecurityConfigSingleton()
    {
        BcryptWorkFactor = 11; // Factor de trabajo de hashing BCrypt seguro y eficiente
        JwtExpiresMinutesDefault = 480; // 8 horas por defecto
        DefaultAlgorithm = "HmacSha256";
        ApplicationName = "LogiTrack Solutions - Sistema de Trazabilidad Logística";
        InitializedAt = DateTime.UtcNow;
    }
}
