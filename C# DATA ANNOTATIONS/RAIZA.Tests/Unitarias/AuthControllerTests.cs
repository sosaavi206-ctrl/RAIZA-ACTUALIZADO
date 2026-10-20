using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using RAIZA.Controllers;
using RAIZA.Data;
using RAIZA.Models;
using RAIZA.Services;
using Xunit;

namespace RAIZA.Tests.Unitarias
{
    /// <summary>
    /// Pruebas unitarias para AuthController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Flujo de autenticación (Login, validación de credenciales, protección contra valores nulos o vacíos).
    /// - Flujo de registro de estudiantes (reglas de contraseña compleja, formato de correo, detección de duplicados).
    /// - Verificación de correo y restablecimiento de contraseña.
    /// - Endpoint de diagnóstico según entorno de desarrollo/producción.
    /// - Códigos de respuesta HTTP: 200 OK, 400 BadRequest, 401 Unauthorized, 404 NotFound y 409 Conflict.
    /// </summary>
    public class AuthControllerTests : IDisposable
    {
        private readonly DatabaseService _context;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<IHostEnvironment> _envMock;
        private readonly Mock<ICorreoService> _correoMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly IConfiguration _configuration;
        private readonly AuthController _sut;

        public AuthControllerTests()
        {
            var dbOptions = new DbContextOptionsBuilder<DatabaseService>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new DatabaseService(dbOptions);

            _loggerMock = new Mock<ILogger<AuthController>>();
            _envMock = new Mock<IHostEnvironment>();
            _correoMock = new Mock<ICorreoService>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();

            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", "ClaveSecretaDePruebasUnitariasSuperLargaParaJwt2026!" },
                { "Jwt:Issuer", "RaizaBackend" },
                { "Jwt:Audience", "RaizaFrontend" },
                { "Recaptcha:SecretKey", "" } // Vacío en pruebas unitarias para no llamar a Google
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            _sut = new AuthController(
                _configuration,
                _context,
                _loggerMock.Object,
                _envMock.Object,
                _correoMock.Object,
                _scopeFactoryMock.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Diagnostico_EnEntornoDesarrollo_RetornaOk()
        {
            // Arrange
            _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

            // Act
            var resultado = await _sut.Diagnostico();

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Diagnostico_FueraDeDesarrollo_RetornaNotFound()
        {
            // Arrange
            _envMock.Setup(e => e.EnvironmentName).Returns(Environments.Production);

            // Act
            var resultado = await _sut.Diagnostico();

            // Assert
            resultado.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Login_Null_RetornaBadRequest()
        {
            // Act
            var resultado = await _sut.Login(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData("", "Password123!")]
        [InlineData("test@raiza.com", "")]
        [InlineData("   ", "   ")]
        public async Task Login_CredencialesIncompletas_RetornaBadRequest(string email, string password)
        {
            // Arrange
            var loginDto = new LoginDto { Email = email, Password = password };

            // Act
            var resultado = await _sut.Login(loginDto);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_RecaptchaFalla_RetornaBadRequest()
        {
            // Arrange (cuando el secret o el token de recaptcha no son válidos)
            var loginDto = new LoginDto
            {
                Email = "estudiante@raiza.com",
                Password = "Password123!",
                RecaptchaToken = "" // token vacío
            };

            // Act
            var resultado = await _sut.Login(loginDto);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Registrar_Null_RetornaBadRequest()
        {
            // Act
            var resultado = await _sut.Registrar(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData("", "test@raiza.com", "Password123!")]
        [InlineData("Juan Perez", "", "Password123!")]
        [InlineData("Juan Perez", "test@raiza.com", "")]
        public async Task Registrar_CamposObligatoriosVacios_RetornaBadRequest(string nombre, string correo, string password)
        {
            // Arrange
            var registroDto = new RegistroDto { Nombre = nombre, Correo = correo, ContrasenaHash = password };

            // Act
            var resultado = await _sut.Registrar(registroDto);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Registrar_CorreoMalFormado_RetornaBadRequest()
        {
            // Arrange
            var registroDto = new RegistroDto
            {
                Nombre = "Carlos Lopez",
                Correo = "correoInvalidoSinArroba",
                ContrasenaHash = "Segura123!"
            };

            // Act
            var resultado = await _sut.Registrar(registroDto);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData("1234567")]          // Menos de 8 caracteres
        [InlineData("sololetras")]        // Sin números
        [InlineData("12345678")]          // Solo números
        public async Task Registrar_PasswordInsegura_RetornaBadRequest(string passwordInvalida)
        {
            // Arrange
            var registroDto = new RegistroDto
            {
                Nombre = "Estudiante Nuevo",
                Correo = "nuevo@raiza.com",
                ContrasenaHash = passwordInvalida
            };

            // Act
            var resultado = await _sut.Registrar(registroDto);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Registrar_UsuarioExistenteYVerificado_RetornaConflict()
        {
            // Arrange
            var usuarioExistente = new Usuario
            {
                Id = 1,
                Nombre = "Usuario Registrado",
                Correo = "registrado@raiza.com",
                ContrasenaHash = "$2a$11$eO.hX592x...",
                Rol = "Estudiante",
                Estado = "Activo",
                VerificacionPendiente = false
            };
            _context.Usuario.Add(usuarioExistente);
            await _context.SaveChangesAsync();

            var registroDto = new RegistroDto
            {
                Nombre = "Intento Duplicado",
                Correo = "registrado@raiza.com",
                ContrasenaHash = "ContrasenaValida123"
            };

            // Act
            var resultado = await _sut.Registrar(registroDto);

            // Assert
            resultado.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task VerificarCorreo_NullODatosVacios_RetornaBadRequest()
        {
            // Act
            var resultado1 = await _sut.VerificarCorreo(null!);
            var resultado2 = await _sut.VerificarCorreo(new VerificarCorreoDto { Correo = "", Codigo = "" });

            // Assert
            resultado1.Should().BeOfType<BadRequestObjectResult>();
            resultado2.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ReenviarCodigo_SinDatos_RetornaOkConMensajeGenerico()
        {
            // Act (la API responde 200 genérico para no filtrar correos existentes)
            var resultado = await _sut.ReenviarCodigo(new ReenviarCodigoDto { Correo = "noexiste@raiza.com" });

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task SolicitarRecuperacion_RespondeOkGenerico()
        {
            // Act (la API responde 200 genérico por seguridad)
            var resultado = await _sut.SolicitarRecuperacion(new SolicitarRecuperacionDto { Email = "alguien@raiza.com" });

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task RestablecerPassword_DatosIncompletos_RetornaBadRequest()
        {
            // Act
            var resultado = await _sut.RestablecerPassword(new RestablecerPasswordDto
            {
                Email = "",
                Codigo = "",
                NuevaContrasena = ""
            });

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RestablecerPassword_PasswordDebil_RetornaBadRequest()
        {
            // Act
            var resultado = await _sut.RestablecerPassword(new RestablecerPasswordDto
            {
                Email = "test@raiza.com",
                Codigo = "123456",
                NuevaContrasena = "debil"
            });

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
