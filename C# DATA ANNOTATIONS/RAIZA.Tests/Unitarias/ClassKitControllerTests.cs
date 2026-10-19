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
    /// Pruebas unitarias para ClassKitController (Catálogo de Kits de Robótica).
    /// </summary>
    public class ClassKitControllerTests
    {
        private readonly Mock<IClassKitI> _kitRepoMock;
        private readonly ClassKitController _sut;

        public ClassKitControllerTests()
        {
            _kitRepoMock = new Mock<IClassKitI>();
            _sut = new ClassKitController(_kitRepoMock.Object);
        }

        [Fact]
        public async Task GetClassKits_RetornaOkConListaDeKits()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var kitsMock = new List<Class_Kit>
            {
                new Class_Kit { idclass_kit = 1, name = "Kit Robótica Inicial", precio = 95000, stockdisponible = 10 },
                new Class_Kit { idclass_kit = 2, name = "Kit IoT Avanzado", precio = 180000, stockdisponible = 5 }
            };
            _kitRepoMock.Setup(r => r.GetClassKits()).ReturnsAsync(kitsMock);

            // Act
            var resultado = await _sut.GetClassKits();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var kits = okResult.Value.Should().BeAssignableTo<IEnumerable<Class_Kit>>().Subject;
            kits.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetClassKitById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var kit = new Class_Kit { idclass_kit = 1, name = "Kit Inicial", precio = 95000 };
            _kitRepoMock.Setup(r => r.GetClassKitById(1)).ReturnsAsync(kit);

            // Act
            var resultado = await _sut.GetClassKitById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var kitDevuelto = okResult.Value.Should().BeOfType<Class_Kit>().Subject;
            kitDevuelto.idclass_kit.Should().Be(1);
        }

        [Fact]
        public async Task GetClassKitById_Inexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            _kitRepoMock.Setup(r => r.GetClassKitById(999)).ReturnsAsync((Class_Kit?)null);

            // Act
            var resultado = await _sut.GetClassKitById(999);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
