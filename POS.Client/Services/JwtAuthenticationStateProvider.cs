using Microsoft.AspNetCore.Components.Authorization;
using POS.Shared.DTOs;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace POS.Client.Services;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    public const string TokenKey = "logitrack_token";
    public const string AuthKey = "logitrack_auth";

    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly LocalStorageService _storage;
    private AuthenticationState? _current;

    public JwtAuthenticationStateProvider(LocalStorageService storage)
    {
        _storage = storage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_current != null)
        {
            return _current;
        }

        var token = await _storage.GetItemAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            _current = Anonymous;
            return _current;
        }

        var principal = BuildPrincipal(token);
        if (principal == null)
        {
            await _storage.RemoveItemAsync(TokenKey);
            await _storage.RemoveItemAsync(AuthKey);
            _current = Anonymous;
            return _current;
        }

        _current = new AuthenticationState(principal);
        return _current;
    }

    public async Task<AuthResponse?> GetStoredAuthAsync()
    {
        var json = await _storage.GetItemAsync(AuthKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AuthResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _storage.GetItemAsync(TokenKey);
    }

    public async Task<AuthResponse> SignInAsync(AuthResponse auth)
    {
        await _storage.SetItemAsync(TokenKey, auth.Token);
        await _storage.SetItemAsync(AuthKey, JsonSerializer.Serialize(auth,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        _current = new AuthenticationState(BuildPrincipal(auth.Token) ??
            new ClaimsPrincipal(new ClaimsIdentity()));
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
        return auth;
    }

    public async Task SignOutAsync()
    {
        await _storage.RemoveItemAsync(TokenKey);
        await _storage.RemoveItemAsync(AuthKey);
        _current = Anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(_current));
    }

    private static ClaimsPrincipal? BuildPrincipal(string token)
    {
        var payload = ReadPayload(token);
        if (payload == null)
        {
            return null;
        }

        var id = Get(payload, "nameid", "nameidentifier", "sub");
        var name = Get(payload, "unique_name", "name");
        var email = Get(payload, "email");
        var role = Get(payload, "role");
        var idRol = Get(payload, "id_rol");
        var exp = Get(payload, "exp");

        if (long.TryParse(exp, out var expSeconds))
        {
            var expira = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            if (expira < DateTimeOffset.UtcNow)
            {
                return null;
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, string.IsNullOrWhiteSpace(id) ? "0" : id),
            new(ClaimTypes.Name, name ?? email ?? "Usuario"),
            new(ClaimTypes.Email, email ?? string.Empty),
            new(ClaimTypes.Role, role ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(idRol))
        {
            claims.Add(new Claim("id_rol", idRol));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static Dictionary<string, string>? ReadPayload(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
            {
                return null;
            }

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var document = JsonDocument.Parse(json);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                values[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                    ? string.Join(",", property.Value.EnumerateArray().Select(x => x.GetString()))
                    : property.Value.ToString();
            }

            return values;
        }
        catch
        {
            return null;
        }
    }

    private static string? Get(Dictionary<string, string> payload, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var pair in payload)
            {
                if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    pair.Key.EndsWith("/" + name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }
        }

        return null;
    }
}
