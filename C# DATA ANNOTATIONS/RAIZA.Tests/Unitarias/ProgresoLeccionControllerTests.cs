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
    /// Pruebas unitarias para ProgresoLeccionController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Control de acceso por estudiante sobre el avance en lecciones individuales.
    /// - Protección de avance de otros estudiantes (HTTP 403 Forbidden).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 401 Unauthorized, 403 Forbidden y 404 NotFound.
    /// </summary>
    public class ProgresoLeccionControllerTests
    {
        private readonly Mock<IProgresoLeccionI> _progresoLeccionRepoMock;
        private readonly ProgresoLeccionController _sut;

        public ProgresoLeccionControllerTests()
        {
            _progresoLeccionRepoMock = new Mock<IProgresoLeccionI>();
            _sut = new ProgresoLeccionController(_progresoLeccionRepoMock.Object);
        }

        [Fact]
        public async Task GetProgresoLecciones_ComoGestor_RetornaTodas()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<ProgresoLeccion>
            {
                new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5, Idleccion = 1, completado = true, fecha = DateTime.UtcNow },
                new ProgresoLeccion { Idprogresoleccion = 2, Idestudiante = 8, Idleccion = 1, completado = false, fecha = DateTime.UtcNow }
            };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLecciones()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetProgresoLecciones();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var progresos = okResult.Value.Should().BeAssignableTo<IEnumerable<ProgresoLeccion>>().Subject;
            progresos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetProgresoLecciones_ComoEstudiante_FiltraPorEstudiante()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var listaMock = new List<ProgresoLeccion>
            {
                new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5, Idleccion = 1, completado = true },
                new ProgresoLeccion { Idprogresoleccion = 2, Idestudiante = 8, Idleccion = 1, completado = false }
            };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLecciones()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetProgresoLecciones();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var progresos = okResult.Value.Should().BeAssignableTo<IEnumerable<ProgresoLeccion>>().Subject;
            progresos.Should().HaveCount(1);
            progresos.First().Idestudiante.Should().Be(5);
        }

        [Fact]
        public async Task GetProgresoLecciones_SinSesion_RetornaUnauthorized()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetProgresoLecciones();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetProgresoLeccionById_ComoPropietario_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5, Idleccion = 1, completado = true };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(1)).ReturnsAsync(progreso);

            // Act
            var resultado = await _sut.GetProgresoLeccionById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(progreso);
        }

        [Fact]
        public async Task GetProgresoLeccionById_EstudianteAjeno_RetornaForbidden403()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 99, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5, Idleccion = 1, completado = true };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(1)).ReturnsAsync(progreso);

            // Act
            var resultado = await _sut.GetProgresoLeccionById(1);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetProgresoLeccionById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(99)).ReturnsAsync((ProgresoLeccion?)null);

            // Act
            var resultado = await _sut.GetProgresoLeccionById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateProgresoLeccion_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");

            // Act
            var resultado = await _sut.CreateProgresoLeccion(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateProgresoLeccion_EstudianteIntentaRegistrarParaOtro_RetornaForbidden403()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 99, Idleccion = 1 };

            // Act
            var resultado = await _sut.CreateProgresoLeccion(progreso);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateProgresoLeccion_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5, Idleccion = 1, completado = true };
            _progresoLeccionRepoMock.Setup(r => r.CreateProgresoLeccion(progreso)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateProgresoLeccion(progreso);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ProgresoLeccionController.GetProgresoLeccionById));
            created.Value.Should().BeEquivalentTo(progreso);
        }

        [Fact]
        public async Task UpdateProgresoLeccion_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5 };

            // Act
            var resultado = await _sut.UpdateProgresoLeccion(10, progreso);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateProgresoLeccion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5 };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(1)).ReturnsAsync((ProgresoLeccion?)null);

            // Act
            var resultado = await _sut.UpdateProgresoLeccion(1, progreso);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteProgresoLeccion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(99)).ReturnsAsync((ProgresoLeccion?)null);

            // Act
            var resultado = await _sut.DeleteProgresoLeccion(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteProgresoLeccion_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var progreso = new ProgresoLeccion { Idprogresoleccion = 1, Idestudiante = 5 };
            _progresoLeccionRepoMock.Setup(r => r.GetProgresoLeccionById(1)).ReturnsAsync(progreso);
            _progresoLeccionRepoMock.Setup(r => r.DeleteProgresoLeccion(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteProgresoLeccion(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
