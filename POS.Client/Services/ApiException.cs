using System.Net;

namespace POS.Client.Services;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public int Codigo { get; }
    public string? Detalle { get; }

    public ApiException(HttpStatusCode statusCode, int codigo, string message, string? detalle = null)
        : base(message)
    {
        StatusCode = statusCode;
        Codigo = codigo;
        Detalle = detalle;
    }
}
