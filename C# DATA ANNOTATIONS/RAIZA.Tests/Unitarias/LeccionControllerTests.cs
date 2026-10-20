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
    /// Pruebas unitarias para LeccionController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Consulta y catálogo de lecciones por módulo educativo.
    /// - Creación, actualización y eliminación de lecciones.
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest y 404 NotFound.
    /// </summary>
    public class LeccionControllerTests
    {
        private readonly Mock<ILeccionI> _leccionRepoMock;
        private readonly LeccionController _sut;

        public LeccionControllerTests()
        {
            _leccionRepoMock = new Mock<ILeccionI>();
            _sut = new LeccionController(_leccionRepoMock.Object);
        }

        [Fact]
        public async Task GetLecciones_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var listaMock = new List<Leccion>
            {
                new Leccion { idleccion = 1, Titulo = "Introducción a Sensores", Tipo = "Video", Orden = 1, idmodulo = 1 },
                new Leccion { idleccion = 2, Titulo = "Armado de Chasis", Tipo = "Práctica", Orden = 2, idmodulo = 1 }
            };
            _leccionRepoMock.Setup(r => r.GetLecciones()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetLecciones();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var lecciones = okResult.Value.Should().BeAssignableTo<IEnumerable<Leccion>>().Subject;
            lecciones.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetLeccionById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var leccion = new Leccion { idleccion = 1, Titulo = "Introducción", Tipo = "Video", Orden = 1, idmodulo = 1 };
            _leccionRepoMock.Setup(r => r.GetLeccionById(1)).ReturnsAsync(leccion);

            // Act
            var resultado = await _sut.GetLeccionById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(leccion);
        }

        [Fact]
        public async Task GetLeccionById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            _leccionRepoMock.Setup(r => r.GetLeccionById(99)).ReturnsAsync((Leccion?)null);

            // Act
            var resultado = await _sut.GetLeccionById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateLeccion_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");

            // Act
            var resultado = await _sut.CreateLeccion(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateLeccion_Valida_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var leccion = new Leccion { idleccion = 1, Titulo = "Programación C++ Arduino", Tipo = "Código", Orden = 3, idmodulo = 1 };
            _leccionRepoMock.Setup(r => r.CreateLeccion(leccion)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateLeccion(leccion);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(LeccionController.GetLeccionById));
            created.Value.Should().BeEquivalentTo(leccion);
        }

        [Fact]
        public async Task UpdateLeccion_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var leccion = new Leccion { idleccion = 1, Titulo = "Arduino" };

            // Act
            var resultado = await _sut.UpdateLeccion(5, leccion);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateLeccion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var leccion = new Leccion { idleccion = 1, Titulo = "Arduino" };
            _leccionRepoMock.Setup(r => r.GetLeccionById(1)).ReturnsAsync((Leccion?)null);

            // Act
            var resultado = await _sut.UpdateLeccion(1, leccion);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateLeccion_Valida_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var leccion = new Leccion { idleccion = 1, Titulo = "Arduino Actualizado" };
            _leccionRepoMock.Setup(r => r.GetLeccionById(1)).ReturnsAsync(leccion);
            _leccionRepoMock.Setup(r => r.UpdateLeccion(leccion)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateLeccion(1, leccion);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteLeccion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _leccionRepoMock.Setup(r => r.GetLeccionById(99)).ReturnsAsync((Leccion?)null);

            // Act
            var resultado = await _sut.DeleteLeccion(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteLeccion_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var leccion = new Leccion { idleccion = 1 };
            _leccionRepoMock.Setup(r => r.GetLeccionById(1)).ReturnsAsync(leccion);
            _leccionRepoMock.Setup(r => r.DeleteLeccion(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteLeccion(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
