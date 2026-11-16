using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Server.Infrastructure;
using POS.Shared.DTOs;

namespace POS.Server.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] ExtensionesPermitidas =
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private static readonly string[] CarpetasPermitidas =
    {
        "firmas", "fotos", "evidencias"
    };

    private readonly IWebHostEnvironment _environment;

    public FilesController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxBytes + 1024)]
    public async Task<ActionResult<ArchivoSubidoDto>> Upload([FromForm] IFormFile archivo, [FromForm] string? carpeta)
    {
        if (archivo == null || archivo.Length == 0)
        {
            throw new BusinessException("Debe seleccionar un archivo.");
        }

        if (archivo.Length > MaxBytes)
        {
            throw new BusinessException("El archivo supera el tamaño máximo permitido de 5 MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            throw new BusinessException("Solo se permiten imágenes PNG, JPG, JPEG o WEBP.");
        }

        var destino = string.IsNullOrWhiteSpace(carpeta) ? "evidencias" : carpeta.Trim().ToLowerInvariant();
        if (!CarpetasPermitidas.Contains(destino))
        {
            destino = "evidencias";
        }

        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var directorio = Path.Combine(webRoot, "uploads", destino);
        Directory.CreateDirectory(directorio);

        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var rutaFisica = Path.Combine(directorio, nombreArchivo);

        await using (var stream = System.IO.File.Create(rutaFisica))
        {
            await archivo.CopyToAsync(stream);
        }

        var rutaPublica = $"/uploads/{destino}/{nombreArchivo}";

        return Ok(new ArchivoSubidoDto
        {
            Ruta = rutaPublica,
            Nombre = archivo.FileName,
            Tamano = archivo.Length
        });
    }
}
