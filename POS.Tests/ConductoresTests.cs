using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using POS.Shared.DTOs;

namespace POS.Tests;

public class ConductoresTests : IntegrationTestBase
{
    private const string Correo = "conductor.tests@logitrack.test";
    private const string Nombre = "Conductor Pruebas Integradas";

    public ConductoresTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Crud_de_conductores_con_baja_logica_y_autorizacion()
    {
        var admin = await LoginAsync("luis.mendez@logitrack.test");
        var adminClient = CreateClient(admin.Token);

        var listado = await adminClient.GetFromJsonAsync<List<ConductorDto>>("/api/conductores", Json);
        Assert.NotNull(listado);

        var existentes = await adminClient.GetFromJsonAsync<List<ConductorDto>>(
            $"/api/conductores?buscar={Uri.EscapeDataString(Correo)}", Json);
        var conductor = existentes?.FirstOrDefault();

        if (conductor == null)
        {
            var crear = await adminClient.PostAsJsonAsync("/api/conductores", new CrearConductorRequest
            {
                NombreCompleto = Nombre,
                Correo = Correo,
                Password = "LogiTrack2026!",
                Activo = true
            });
            Assert.Equal(HttpStatusCode.Created, crear.StatusCode);
            conductor = await crear.Content.ReadFromJsonAsync<ConductorDto>(Json);
        }
        Assert.NotNull(conductor);

        var editar = await adminClient.PutAsJsonAsync($"/api/conductores/{conductor.IdConductor}",
            new ActualizarConductorRequest { NombreCompleto = Nombre, Correo = Correo, Activo = true });
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);

        var reset = await adminClient.PostAsJsonAsync($"/api/conductores/{conductor.IdConductor}/reset-password",
            new ResetPasswordRequest { Password = "LogiTrack2026!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var conductorAuth = await LoginAsync(Correo);
        var clientConductor = CreateClient(conductorAuth.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await clientConductor.GetAsync("/api/conductores")).StatusCode);

        var baja = await adminClient.PatchAsJsonAsync($"/api/conductores/{conductor.IdConductor}/estado",
            new CambiarEstadoConductorRequest { Activo = false });
        Assert.Equal(HttpStatusCode.OK, baja.StatusCode);

        var estado = await baja.Content.ReadFromJsonAsync<EstadoResponse>(Json);
        Assert.NotNull(estado);
        Assert.False(estado.Conductor.Activo);
    }

    private sealed class EstadoResponse
    {
        public string Mensaje { get; set; } = string.Empty;
        public ConductorDto Conductor { get; set; } = new();
    }
}
