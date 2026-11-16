namespace POS.Shared.Constants;

/// <summary>
/// Nombres de rol exactamente como están registrados en la tabla dbo.rol.
/// Los triggers de la base de datos comparan contra estos textos.
/// </summary>
public static class Roles
{
    public const string AdministradorSistema = "Administrador del Sistema";
    public const string AdministradorLogistico = "Administrador Logístico";
    public const string Conductor = "Conductor/Operador";
    public const string PersonalAlmacen = "Personal de Almacén";
    public const string CentroControl = "Centro de Control/Supervisor";

    public static readonly string[] Todos =
    {
        AdministradorSistema,
        AdministradorLogistico,
        Conductor,
        PersonalAlmacen,
        CentroControl
    };

    public static readonly string[] Administracion =
    {
        AdministradorSistema,
        AdministradorLogistico
    };

    public static readonly string[] Operacion =
    {
        AdministradorLogistico,
        Conductor,
        PersonalAlmacen,
        CentroControl
    };
}

public static class Hitos
{
    public const string Registrada = "Registrada";
    public const string SalidaAlmacen = "Salida de Almacén";
    public const string EnTransito = "En Tránsito";
    public const string EnCentroDistribucion = "En Centro de Distribución Local";
    public const string EnRepartoFinal = "En Reparto Final";
    public const string Entregado = "Entregado";
}

public static class Estados
{
    public const string HojaPendiente = "Pendiente";
    public const string HojaEnCurso = "En curso";
    public const string HojaFinalizada = "Finalizada";

    public const string VehiculoDisponible = "Disponible";
    public const string VehiculoEnRuta = "En ruta";
    public const string VehiculoMantenimiento = "Mantenimiento";
    public const string VehiculoInhabilitado = "Inhabilitado";

    public const string TipoEntrada = "ENTRADA";
    public const string TipoSalida = "SALIDA";
}
