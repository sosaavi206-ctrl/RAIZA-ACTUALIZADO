using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RAIZA.Controllers;
using RAIZA.Interfaces;
using RAIZA.Models;
using RAIZA.Tests.Helpers;
using Xunit;

namespace RAIZA.Tests.Unitarias
{
    /// <summary>
    /// Pruebas unitarias para UsuarioController usando Moq y FluentAssertions.
    /// Valida:
    /// - Códigos de estado HTTP (200, 201, 400, 403, 404, 409).
    /// - Seguridad: ocultamiento de contraseña hash y validación de permisos de acceso.
    /// - Reglas de negocio: validación de correo, longitud de contraseña y roles.
    /// </summary>
    public class UsuarioControllerTests
    {
        private readonly Mock<IUsuarioI> _usuarioRepoMock;
        private readonly UsuarioController _sut; // System Under Test

        public UsuarioControllerTests()
        {
            _usuarioRepoMock = new Mock<IUsuarioI>();
            _sut = new UsuarioController(_usuarioRepoMock.Object);
        }

        [Fact]
        public async Task GetUsuarios_ComoAdmin_RetornaOkConContrasenasOcultas()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Usuario>
            {
                new Usuario { Id = 1, Nombre = "Admin", Correo = "admin@raiza.com", ContrasenaHash = "$2a$12$hash1" },
                new Usuario { Id = 2, Nombre = "Estudiante 1", Correo = "est1@raiza.com", ContrasenaHash = "$2a$12$hash2" }
            };
            _usuarioRepoMock.Setup(r => r.GetUsuarios()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetUsuarios();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var usuarios = okResult.Value.Should().BeAssignableTo<IEnumerable<Usuario>>().Subject;
            usuarios.Should().HaveCount(2);
            usuarios.Should().OnlyContain(u => string.IsNullOrEmpty(u.ContrasenaHash));
        }

        [Fact]
        public async Task GetUsuarioById_UsuarioPropio_RetornaOk()
        {
            // Arrange: El usuario con ID 5 consulta su propio perfil
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var usuario = new Usuario
            {
                Id = 5,
                Nombre = "Carlos Lopez",
                Correo = "carlos@raiza.com",
                ContrasenaHash = "$2a$12$secreto"
            };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(5)).ReturnsAsync(usuario);

            // Act
            var resultado = await _sut.GetUsuarioById(5);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var usuarioDevuelto = okResult.Value.Should().BeOfType<Usuario>().Subject;
            usuarioDevuelto.Id.Should().Be(5);
            usuarioDevuelto.ContrasenaHash.Should().BeEmpty(); // Seguridad: contraseña oculta
        }

        [Fact]
        public async Task GetUsuarioById_UsuarioAjenoNoAdmin_Retorna403Forbidden()
        {
            // Arrange: El estudiante con ID 5 intenta consultar el perfil del usuario con ID 10
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetUsuarioById(10);

            // Assert
            var statusCodeResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(403);
            _usuarioRepoMock.Verify(r => r.GetUsuarioById(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetUsuarioById_Inexistente_Retorna404NotFound()
        {
            // Arrange: Administrador consulta un usuario que no existe en el sistema
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(999)).ReturnsAsync((Usuario?)null);

            // Act
            var resultado = await _sut.GetUsuarioById(999);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Theory]
        [InlineData("", "password123", "Nombre Valido", "Estudiante")] // Correo vacío
        [InlineData("correoInvalido", "password123", "Nombre Valido", "Estudiante")] // Correo sin formato
        [InlineData("valido@raiza.com", "1234567", "Nombre Valido", "Estudiante")] // Contraseña < 8 caracteres
        [InlineData("valido@raiza.com", "password123", "", "Estudiante")] // Nombre vacío
        [InlineData("valido@raiza.com", "password123", "Nombre", "SuperUser")] // Rol no permitido
        public async Task CreateUsuario_DatosInvalidos_Retorna400BadRequest(
            string correo, string contrasena, string nombre, string rol)
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var usuarioInvalido = new Usuario
            {
                Correo = correo,
                ContrasenaHash = contrasena,
                Nombre = nombre,
                Rol = rol,
                Estado = "Activo"
            };

            // Act
            var resultado = await _sut.CreateUsuario(usuarioInvalido);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
            _usuarioRepoMock.Verify(r => r.CreateUsuario(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task CreateUsuario_CorreoDuplicado_Retorna409Conflict()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var usuarioNuevo = new Usuario
            {
                Correo = "duplicado@raiza.com",
                ContrasenaHash = "Segura12345",
                Nombre = "Usuario Prueba",
                Rol = "Estudiante",
                Estado = "Activo"
            };

            // Simula que ya existe un usuario con ese correo
            _usuarioRepoMock.Setup(r => r.GetUsuarioByCorreo("duplicado@raiza.com"))
                            .ReturnsAsync(new Usuario { Id = 3, Correo = "duplicado@raiza.com" });

            // Act
            var resultado = await _sut.CreateUsuario(usuarioNuevo);

            // Assert
            resultado.Should().BeOfType<ConflictObjectResult>();
            _usuarioRepoMock.Verify(r => r.CreateUsuario(It.IsAny<Usuario>()), Times.Never);
        }

        [Fact]
        public async Task CreateUsuario_DatosValidos_Retorna201CreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var nuevoUsuario = new Usuario
            {
                Id = 15,
                Correo = "nuevo@raiza.com",
                ContrasenaHash = "Segura12345",
                Nombre = "Nuevo Estudiante",
                Rol = "Estudiante",
                Estado = "Activo"
            };

            _usuarioRepoMock.Setup(r => r.GetUsuarioByCorreo("nuevo@raiza.com"))
                            .ReturnsAsync((Usuario?)null);
            _usuarioRepoMock.Setup(r => r.CreateUsuario(It.IsAny<Usuario>()))
                            .ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateUsuario(nuevoUsuario);

            // Assert
            var createdResult = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            var usuarioCreado = createdResult.Value.Should().BeOfType<Usuario>().Subject;
            usuarioCreado.ContrasenaHash.Should().BeEmpty(); // No se debe exponer el hash
            _usuarioRepoMock.Verify(r => r.CreateUsuario(It.IsAny<Usuario>()), Times.Once);
        }
    }
}
