using POS.Shared.DTOs;

namespace POS.Server.Infrastructure;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var error = SqlErrorTranslator.Translate(ex);
            var status = SqlErrorTranslator.StatusCodeFor(error);

            if (error.Codigo >= 500 && error.Codigo < 50000)
            {
                _logger.LogError(ex, "Error no controlado en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("Regla de negocio {Codigo} en {Metodo} {Ruta}: {Mensaje}",
                    error.Codigo, context.Request.Method, context.Request.Path, error.Mensaje);
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
