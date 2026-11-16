using POS.Shared.DTOs;

namespace POS.Client.Services;

/// <summary>Cachea los catálogos (roles, almacenes, productos, etc.) durante la sesión.</summary>
public class CatalogosService
{
    private readonly ApiClient _api;
    private CatalogosDto? _cache;

    public CatalogosService(ApiClient api)
    {
        _api = api;
    }

    public async Task<CatalogosDto> GetAsync(bool recargar = false)
    {
        if (_cache != null && !recargar)
        {
            return _cache;
        }

        _cache = await _api.GetAsync<CatalogosDto>("api/catalogos") ?? new CatalogosDto();
        return _cache;
    }

    public void Invalidar() => _cache = null;
}
