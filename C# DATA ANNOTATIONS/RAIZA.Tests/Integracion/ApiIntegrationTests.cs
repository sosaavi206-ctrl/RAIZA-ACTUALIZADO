using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RAIZA.Controllers;
using RAIZA.Data;
using Xunit;

namespace RAIZA.Tests.Integracion
{
    /// <summary>
    /// Pruebas de integración con WebApplicationFactory siguiendo la Sección 3.6 de la Guía ADSO.
    /// Levanta la aplicación ASP.NET Core en memoria y prueba las peticiones HTTP reales a través del pipeline.
    /// </summary>
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureServices(services =>
                {
                    // Reemplaza el DbContext real por InMemory para que no dependa de SQL Server en vivo
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DatabaseService>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<DatabaseService>(options =>
                    {
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                    });
                });
            }).CreateClient();
        }

        [Fact]
        public async Task Diagnostico_EnDesarrollo_Retorna200Ok()
        {
            // Act: llamada HTTP real al endpoint público de diagnóstico
            var response = await _client.GetAsync("/api/Auth/Diagnostico");

            // Assert: debe responder HTTP 200 OK
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task EndpointProtegido_SinTokenJwt_Retorna401Unauthorized()
        {
            // Act: llamada HTTP real a un controlador protegido (Administrador) sin cabecera Authorization
            var response = await _client.GetAsync("/api/Administrador");

            // Assert: el pipeline de autenticación JWT de ASP.NET Core debe responder 401
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_CredencialesInvalidas_Retorna401Unauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "usuario.inexistente@raiza.com",
                Password = "PasswordInvalida123!",
                RecaptchaToken = "dummy"
            };

            // Act: llamada HTTP POST real a /api/Auth/Login
            var response = await _client.PostAsJsonAsync("/api/Auth/Login", loginDto);

            // Assert: credenciales inválidas deben retornar 401
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Registro_CamposVacios_Retorna400BadRequest()
        {
            // Arrange
            var registroVacio = new RegistroDto();

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/Registro", registroVacio);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
