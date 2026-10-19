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
    /// Pruebas unitarias para ModuloController.
    /// Valida:
    /// - Consulta de catálogo educativo (módulos de aprendizaje).
    /// - Manejo de respuestas HTTP 200, 201, 400 y 404.
    /// </summary>
    public class ModuloControllerTests
    {
        private readonly Mock<IModuloI> _moduloRepoMock;
        private readonly ModuloController _sut;

        public ModuloControllerTests()
        {
            _moduloRepoMock = new Mock<IModuloI>();
            _sut = new ModuloController(_moduloRepoMock.Object);
        }

        [Fact]
        public async Task GetModulos_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var modulosMock = new List<Modulo>
            {
                new Modulo { idmodulo = 1, Nivel = "Básico", Precio = 50000, idtematica = 1, idinstructor = 1 },
                new Modulo { idmodulo = 2, Nivel = "Avanzado", Precio = 80000, idtematica = 2, idinstructor = 1 }
            };
            _moduloRepoMock.Setup(r => r.GetModulos()).ReturnsAsync(modulosMock);

            // Act
            var resultado = await _sut.GetModulos();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var modulos = okResult.Value.Should().BeAssignableTo<IEnumerable<Modulo>>().Subject;
            modulos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetModuloById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var modulo = new Modulo { idmodulo = 1, Nivel = "Básico", Precio = 50000 };
            _moduloRepoMock.Setup(r => r.GetModuloById(1)).ReturnsAsync(modulo);

            // Act
            var resultado = await _sut.GetModuloById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var moduloDevuelto = okResult.Value.Should().BeOfType<Modulo>().Subject;
            moduloDevuelto.idmodulo.Should().Be(1);
        }

        [Fact]
        public async Task GetModuloById_Inexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            _moduloRepoMock.Setup(r => r.GetModuloById(99)).ReturnsAsync((Modulo?)null);

            // Act
            var resultado = await _sut.GetModuloById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateModulo_DatosNulos_Retorna400BadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");

            // Act
            var resultado = await _sut.CreateModulo(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateModulo_DatosValidos_Retorna201CreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var nuevoModulo = new Modulo
            {
                idmodulo = 5,
                Nivel = "Intermedio",
                Precio = 70000,
                idtematica = 1,
                idinstructor = 2
            };
            _moduloRepoMock.Setup(r => r.CreateModulo(It.IsAny<Modulo>())).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateModulo(nuevoModulo);

            // Assert
            var createdResult = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            _moduloRepoMock.Verify(r => r.CreateModulo(It.Is<Modulo>(m => m.idmodulo == 5)), Times.Once);
        }
    }
}
