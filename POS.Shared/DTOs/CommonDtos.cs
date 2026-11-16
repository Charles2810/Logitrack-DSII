namespace POS.Shared.DTOs;

/// <summary>Respuesta uniforme de error de la API.</summary>
public class ApiError
{
    public int Codigo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Traza { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}

public class ArchivoSubidoDto
{
    public string Ruta { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public long Tamano { get; set; }
}
