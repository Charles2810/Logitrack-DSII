using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using POS.Shared.DTOs;

namespace POS.Tests;

public class RutasMapasTests : IntegrationTestBase
{
    public RutasMapasTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Lista_y_detalle_de_rutas_incluyen_coordenadas()
    {
        var auth = await LoginAsync("ana.rojas@logitrack.test");
        var client = CreateClient(auth.Token);

        var rutas = await client.GetFromJsonAsync<List<GuiaDto>>("/api/rutas", Json);
        Assert.NotNull(rutas);
        Assert.Equal(4, rutas.Count);

        var guia = await client.GetFromJsonAsync<GuiaDto>("/api/rutas/2", Json);
        Assert.NotNull(guia);
        Assert.Equal("LT-00000002", guia.CodigoGuia);
        Assert.True(guia.TieneRuta);
        Assert.Equal(-17.7837, guia.LatitudOrigen!.Value, 6);
        Assert.Equal(-63.180426, guia.LongitudOrigen!.Value, 6);
        Assert.Equal(-17.7961, guia.LatitudDestino!.Value, 6);
        Assert.Equal(-63.1649, guia.LongitudDestino!.Value, 6);
        Assert.Equal(2.7, guia.DistanciaRutaKm, 2);
        Assert.Equal(12, guia.DuracionRutaMin);
    }

    [Fact]
    public async Task Filtro_por_hoja_de_ruta_devuelve_sus_guias()
    {
        var auth = await LoginAsync("ana.rojas@logitrack.test");
        var client = CreateClient(auth.Token);

        var rutas = await client.GetFromJsonAsync<List<GuiaDto>>("/api/rutas?idHojaRuta=1", Json);

        Assert.NotNull(rutas);
        Assert.Equal(2, rutas.Count);
        Assert.Contains(rutas, g => g.CodigoGuia == "LT-00000002");
        Assert.Contains(rutas, g => g.CodigoGuia == "LT-00000001");
    }

    [Fact]
    public async Task Tracking_publico_expone_mapa_sin_token()
    {
        var tracking = await Client.GetFromJsonAsync<TrazabilidadDto>("/api/tracking/LT-00000002", Json);

        Assert.NotNull(tracking);
        Assert.Equal("En Tránsito", tracking.EstadoActual);
        Assert.True(tracking.TieneMapa);
        Assert.Equal(2.7, tracking.DistanciaKm, 2);
        Assert.Equal(12, tracking.DuracionEstimadaMin);
        Assert.Equal(3, tracking.Eventos.Count);
    }

    [Fact]
    public async Task Rutas_exigen_token_y_el_conductor_solo_ve_las_suyas()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client.GetAsync("/api/rutas")).StatusCode);

        var conductor = await LoginAsync("carlos.soliz@logitrack.test");
        var client = CreateClient(conductor.Token);

        var rutas = await client.GetFromJsonAsync<List<GuiaDto>>("/api/rutas", Json);
        Assert.NotNull(rutas);
        Assert.Equal(2, rutas.Count);
    }

    [Fact]
    public async Task Recursos_visuales_y_rutas_de_la_ui_responden()
    {
        var urls = new[]
        {
            "/",
            "/conductores",
            "/rastreo?codigo=LT-00000002",
            "/simulacion-rutas",
            "/js/mapa.js",
            "/lib/leaflet/leaflet.js",
            "/lib/leaflet/leaflet.css"
        };

        foreach (var url in urls)
        {
            var response = await Client.GetAsync(url);
            Assert.True(response.IsSuccessStatusCode, $"{url} devolvió {(int)response.StatusCode}");
        }
    }
}
