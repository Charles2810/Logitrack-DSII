using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using POS.Shared.DTOs;

namespace POS.Tests;

public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = CreateClient();
    }

    protected HttpClient CreateClient(string? token = null)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        }

        return client;
    }

    protected async Task<AuthResponse> LoginAsync(string correo)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new { correo, password = "LogiTrack2026!" });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
    }
}
