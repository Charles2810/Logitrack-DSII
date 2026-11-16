using POS.Shared.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace POS.Client.Services;

/// <summary>
/// Cliente HTTP único de la aplicación. Adjunta el token JWT, traduce los errores
/// de la API a <see cref="ApiException"/> y expone helpers tipados.
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly JwtAuthenticationStateProvider _auth;

    public ApiClient(HttpClient http, JwtAuthenticationStateProvider auth)
    {
        _http = http;
        _auth = auth;
    }

    /// <summary>Se dispara cuando la API responde 401 para que el layout redirija al login.</summary>
    public event Action? SesionExpirada;

    public Task<T?> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);

    public Task<T?> PostAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Post, url, body);

    public Task<T?> PutAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Put, url, body);

    public Task<T?> PatchAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Patch, url, body);

    public Task<T?> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);

    public Task PostAsync(string url, object? body) => SendAsync<object>(HttpMethod.Post, url, body);

    public Task PatchAsync(string url, object? body = null) => SendAsync<object>(HttpMethod.Patch, url, body);

    public async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);

        var token = await GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body != null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            SesionExpirada?.Invoke();
            throw new ApiException(response.StatusCode, 401,
                "Su sesión expiró o no tiene permisos. Inicie sesión nuevamente.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildExceptionAsync(response);
        }

        if (response.StatusCode == HttpStatusCode.NoContent ||
            response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task<ArchivoSubidoDto?> SubirArchivoAsync(Stream contenido, string nombreArchivo, string carpeta)
    {
        using var content = new MultipartFormDataContent();
        var file = new StreamContent(contenido);
        file.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(nombreArchivo));

        content.Add(file, "archivo", nombreArchivo);
        content.Add(new StringContent(carpeta), "carpeta");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/files/upload") { Content = content };

        var token = await GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            SesionExpirada?.Invoke();
            throw new ApiException(response.StatusCode, 401, "Su sesión expiró. Inicie sesión nuevamente.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildExceptionAsync(response);
        }

        return await response.Content.ReadFromJsonAsync<ArchivoSubidoDto>(Json);
    }

    public async Task<ArchivoSubidoDto?> SubirArchivoAsync(byte[] contenido, string nombreArchivo, string carpeta)
    {
        using var stream = new MemoryStream(contenido);
        return await SubirArchivoAsync(stream, nombreArchivo, carpeta);
    }

    private async Task<string?> GetTokenAsync()
    {
        var state = await _auth.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true
            ? await _auth.GetTokenAsync()
            : null;
    }

    private static async Task<ApiException> BuildExceptionAsync(HttpResponseMessage response)
    {
        var texto = await response.Content.ReadAsStringAsync();

        try
        {
            using var document = JsonDocument.Parse(texto);
            var root = document.RootElement;

            if (root.TryGetProperty("mensaje", out var mensaje))
            {
                var codigo = root.TryGetProperty("codigo", out var c) && c.TryGetInt32(out var n) ? n : (int)response.StatusCode;
                var detalle = root.TryGetProperty("detalle", out var d) ? d.GetString() : null;
                return new ApiException(response.StatusCode, codigo, mensaje.GetString() ?? "Error de la API.", detalle);
            }

            // ValidationProblemDetails
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var mensajes = new List<string>();
                foreach (var error in errors.EnumerateObject())
                {
                    foreach (var item in error.Value.EnumerateArray())
                    {
                        mensajes.Add(item.GetString() ?? string.Empty);
                    }
                }

                if (mensajes.Count > 0)
                {
                    return new ApiException(response.StatusCode, (int)response.StatusCode,
                        string.Join(" ", mensajes.Where(m => !string.IsNullOrWhiteSpace(m))));
                }
            }

            if (root.TryGetProperty("title", out var title))
            {
                return new ApiException(response.StatusCode, (int)response.StatusCode,
                    title.GetString() ?? "Error de la API.");
            }
        }
        catch (JsonException)
        {
            // respuesta no JSON
        }

        return new ApiException(response.StatusCode, (int)response.StatusCode,
            string.IsNullOrWhiteSpace(texto)
                ? $"Error {(int)response.StatusCode} al comunicarse con el servidor."
                : texto);
    }

    private static string GetContentType(string nombreArchivo) =>
        Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
}
