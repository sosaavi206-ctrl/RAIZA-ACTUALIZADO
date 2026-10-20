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
    /// Pruebas unitarias para AdministradorController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Consulta de perfiles de administradores.
    /// - Validaciones de integridad (usuario existente, rol Administrador, no duplicidad).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 404 NotFound y 409 Conflict.
    /// </summary>
    public class AdministradorControllerTests
    {
        private readonly Mock<AdministradorI> _adminRepoMock;
        private readonly Mock<IUsuarioI> _usuarioRepoMock;
        private readonly AdministradorController _sut;

        public AdministradorControllerTests()
        {
            _adminRepoMock = new Mock<AdministradorI>();
            _usuarioRepoMock = new Mock<IUsuarioI>();
            _sut = new AdministradorController(_adminRepoMock.Object, _usuarioRepoMock.Object);
        }

        [Fact]
        public async Task GetAdministradores_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Administrador>
            {
                new Administrador { idadministrador = 1, NivelAcceso = 5 },
                new Administrador { idadministrador = 2, NivelAcceso = 4 }
            };
            _adminRepoMock.Setup(r => r.GetAdministradores()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetAdministradores();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var admins = okResult.Value.Should().BeAssignableTo<IEnumerable<Administrador>>().Subject;
            admins.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAdministradorById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 1, NivelAcceso = 5 };
            _adminRepoMock.Setup(r => r.GetAdministradorById(1)).ReturnsAsync(admin);

            // Act
            var resultado = await _sut.GetAdministradorById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(admin);
        }

        [Fact]
        public async Task GetAdministradorById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _adminRepoMock.Setup(r => r.GetAdministradorById(99)).ReturnsAsync((Administrador?)null);

            // Act
            var resultado = await _sut.GetAdministradorById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateAdministrador_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");

            // Act
            var resultado = await _sut.CreateAdministrador(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateAdministrador_IdInvalido_RetornaBadRequest(int idInvalido)
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = idInvalido, NivelAcceso = 3 };

            // Act
            var resultado = await _sut.CreateAdministrador(admin);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateAdministrador_UsuarioInexistente_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 10, NivelAcceso = 5 };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync((Usuario?)null);

            // Act
            var resultado = await _sut.CreateAdministrador(admin);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateAdministrador_UsuarioNoTieneRolAdministrador_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 10, NivelAcceso = 5 };
            var usuarioEstudiante = new Usuario { Id = 10, Nombre = "Juan", Rol = "Estudiante" };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioEstudiante);

            // Act
            var resultado = await _sut.CreateAdministrador(admin);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateAdministrador_YaExistePerfil_RetornaConflict()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 10, NivelAcceso = 5 };
            var usuarioAdmin = new Usuario { Id = 10, Nombre = "Admin Master", Rol = "Administrador" };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioAdmin);
            _adminRepoMock.Setup(r => r.GetAdministradorById(10)).ReturnsAsync(admin);

            // Act
            var resultado = await _sut.CreateAdministrador(admin);

            // Assert
            resultado.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task CreateAdministrador_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 10, NivelAcceso = 5 };
            var usuarioAdmin = new Usuario { Id = 10, Nombre = "Admin Nuevo", Rol = "Administrador" };

            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioAdmin);
            _adminRepoMock.Setup(r => r.GetAdministradorById(10)).ReturnsAsync((Administrador?)null);
            _adminRepoMock.Setup(r => r.CreateAdministrador(admin)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateAdministrador(admin);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(AdministradorController.GetAdministradorById));
            created.Value.Should().BeEquivalentTo(admin);
        }

        [Fact]
        public async Task UpdateAdministrador_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 5, NivelAcceso = 3 };

            // Act
            var resultado = await _sut.UpdateAdministrador(10, admin);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateAdministrador_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 5, NivelAcceso = 3 };
            _adminRepoMock.Setup(r => r.GetAdministradorById(5)).ReturnsAsync((Administrador?)null);

            // Act
            var resultado = await _sut.UpdateAdministrador(5, admin);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateAdministrador_Valido_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 5, NivelAcceso = 4 };
            _adminRepoMock.Setup(r => r.GetAdministradorById(5)).ReturnsAsync(admin);
            _adminRepoMock.Setup(r => r.UpdateAdministrador(admin)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateAdministrador(5, admin);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteAdministrador_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _adminRepoMock.Setup(r => r.GetAdministradorById(99)).ReturnsAsync((Administrador?)null);

            // Act
            var resultado = await _sut.DeleteAdministrador(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteAdministrador_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var admin = new Administrador { idadministrador = 1, NivelAcceso = 5 };
            _adminRepoMock.Setup(r => r.GetAdministradorById(1)).ReturnsAsync(admin);
            _adminRepoMock.Setup(r => r.DeleteAdministrador(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteAdministrador(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
