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
    /// Pruebas unitarias para ClasesEnVivoController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Programación y consulta de sesiones de clases virtuales en vivo.
    /// - Validación de identificadores y existencia en catálogo.
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest y 404 NotFound.
    /// </summary>
    public class ClasesEnVivoControllerTests
    {
        private readonly Mock<IClasesEnVivoI> _clasesEnVivoRepoMock;
        private readonly ClasesEnVivoController _sut;

        public ClasesEnVivoControllerTests()
        {
            _clasesEnVivoRepoMock = new Mock<IClasesEnVivoI>();
            _sut = new ClasesEnVivoController(_clasesEnVivoRepoMock.Object);
        }

        [Fact]
        public async Task GetClasesEnVivo_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var listaMock = new List<ClasesEnVivo>
            {
                new ClasesEnVivo { idclaasesenvivo = 1, UrlSala = "https://meet.raiza.edu/clase1", Estado = "Programada", idmodulo = 1, idinstructor = 2 },
                new ClasesEnVivo { idclaasesenvivo = 2, UrlSala = "https://meet.raiza.edu/clase2", Estado = "Finalizada", idmodulo = 2, idinstructor = 2 }
            };
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivo()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetClasesEnVivo();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var clases = okResult.Value.Should().BeAssignableTo<IEnumerable<ClasesEnVivo>>().Subject;
            clases.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetClasesEnVivoById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo { idclaasesenvivo = 1, UrlSala = "https://meet.raiza.edu/clase1", Estado = "Programada" };
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(1)).ReturnsAsync(clase);

            // Act
            var resultado = await _sut.GetClasesEnVivoById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(clase);
        }

        [Fact]
        public async Task GetClasesEnVivoById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(99)).ReturnsAsync((ClasesEnVivo?)null);

            // Act
            var resultado = await _sut.GetClasesEnVivoById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateClasesEnVivo_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");

            // Act
            var resultado = await _sut.CreateClasesEnVivo(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateClasesEnVivo_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo
            {
                idclaasesenvivo = 1,
                UrlSala = "https://meet.raiza.edu/clase1",
                Estado = "Programada",
                FechaHora = DateTime.UtcNow.AddDays(1),
                idmodulo = 1,
                idinstructor = 2
            };
            _clasesEnVivoRepoMock.Setup(r => r.CreateClasesEnVivo(clase)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateClasesEnVivo(clase);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ClasesEnVivoController.GetClasesEnVivoById));
            created.Value.Should().BeEquivalentTo(clase);
        }

        [Fact]
        public async Task UpdateClasesEnVivo_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo { idclaasesenvivo = 1, UrlSala = "https://meet.raiza.edu/clase1" };

            // Act
            var resultado = await _sut.UpdateClasesEnVivo(5, clase);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateClasesEnVivo_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo { idclaasesenvivo = 1, UrlSala = "https://meet.raiza.edu/clase1" };
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(1)).ReturnsAsync((ClasesEnVivo?)null);

            // Act
            var resultado = await _sut.UpdateClasesEnVivo(1, clase);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateClasesEnVivo_Valido_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo { idclaasesenvivo = 1, UrlSala = "https://meet.raiza.edu/clase1" };
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(1)).ReturnsAsync(clase);
            _clasesEnVivoRepoMock.Setup(r => r.UpdateClasesEnVivo(clase)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateClasesEnVivo(1, clase);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteClasesEnVivo_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(99)).ReturnsAsync((ClasesEnVivo?)null);

            // Act
            var resultado = await _sut.DeleteClasesEnVivo(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteClasesEnVivo_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var clase = new ClasesEnVivo { idclaasesenvivo = 1 };
            _clasesEnVivoRepoMock.Setup(r => r.GetClasesEnVivoById(1)).ReturnsAsync(clase);
            _clasesEnVivoRepoMock.Setup(r => r.DeleteClasesEnVivo(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteClasesEnVivo(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
