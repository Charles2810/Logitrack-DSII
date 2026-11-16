namespace POS.Server.Infrastructure;

/// <summary>Error de negocio controlado que la API devuelve como 400.</summary>
public class BusinessException : Exception
{
    public int Codigo { get; }

    public BusinessException(string mensaje, int codigo = 400) : base(mensaje)
    {
        Codigo = codigo;
    }
}
