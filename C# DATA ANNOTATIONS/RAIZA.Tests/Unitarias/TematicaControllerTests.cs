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
    /// Pruebas unitarias para TematicaController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Catálogo de temáticas y ramas de estudio de robótica (electrónica, programación, mecatrónica).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest y 404 NotFound.
    /// </summary>
    public class TematicaControllerTests
    {
        private readonly Mock<ITematicaI> _tematicaRepoMock;
        private readonly TematicaController _sut;

        public TematicaControllerTests()
        {
            _tematicaRepoMock = new Mock<ITematicaI>();
            _sut = new TematicaController(_tematicaRepoMock.Object);
        }

        [Fact]
        public async Task GetTematicas_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var listaMock = new List<Tematica>
            {
                new Tematica { idtematica = 1, Nombre = "Robótica Móvil", ImagenPortada = "robotica.jpg" },
                new Tematica { idtematica = 2, Nombre = "Domótica y Sensores", ImagenPortada = "domotica.jpg" }
            };
            _tematicaRepoMock.Setup(r => r.GetTematicas()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetTematicas();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var tematicas = okResult.Value.Should().BeAssignableTo<IEnumerable<Tematica>>().Subject;
            tematicas.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTematicaById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var tematica = new Tematica { idtematica = 1, Nombre = "Robótica Móvil", ImagenPortada = "robotica.jpg" };
            _tematicaRepoMock.Setup(r => r.GetTematicaById(1)).ReturnsAsync(tematica);

            // Act
            var resultado = await _sut.GetTematicaById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(tematica);
        }

        [Fact]
        public async Task GetTematicaById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            _tematicaRepoMock.Setup(r => r.GetTematicaById(99)).ReturnsAsync((Tematica?)null);

            // Act
            var resultado = await _sut.GetTematicaById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateTematica_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");

            // Act
            var resultado = await _sut.CreateTematica(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateTematica_Valida_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var tematica = new Tematica { idtematica = 1, Nombre = "Inteligencia Artificial", ImagenPortada = "ia.jpg" };
            _tematicaRepoMock.Setup(r => r.CreateTematica(tematica)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateTematica(tematica);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(TematicaController.GetTematicaById));
            created.Value.Should().BeEquivalentTo(tematica);
        }

        [Fact]
        public async Task UpdateTematica_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var tematica = new Tematica { idtematica = 1, Nombre = "IA" };

            // Act
            var resultado = await _sut.UpdateTematica(5, tematica);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTematica_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var tematica = new Tematica { idtematica = 1, Nombre = "IA" };
            _tematicaRepoMock.Setup(r => r.GetTematicaById(1)).ReturnsAsync((Tematica?)null);

            // Act
            var resultado = await _sut.UpdateTematica(1, tematica);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateTematica_Valida_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var tematica = new Tematica { idtematica = 1, Nombre = "IA Actualizada" };
            _tematicaRepoMock.Setup(r => r.GetTematicaById(1)).ReturnsAsync(tematica);
            _tematicaRepoMock.Setup(r => r.UpdateTematica(tematica)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateTematica(1, tematica);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteTematica_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _tematicaRepoMock.Setup(r => r.GetTematicaById(99)).ReturnsAsync((Tematica?)null);

            // Act
            var resultado = await _sut.DeleteTematica(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTematica_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var tematica = new Tematica { idtematica = 1 };
            _tematicaRepoMock.Setup(r => r.GetTematicaById(1)).ReturnsAsync(tematica);
            _tematicaRepoMock.Setup(r => r.DeleteTematica(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteTematica(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
