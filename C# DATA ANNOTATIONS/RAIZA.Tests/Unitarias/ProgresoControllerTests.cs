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
    /// Pruebas unitarias para ProgresoController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Control de acceso por estudiante (progreso de cursos y módulos).
    /// - Protección contra manipulación de progreso ajeno (HTTP 403 Forbidden).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 401 Unauthorized, 403 Forbidden y 404 NotFound.
    /// </summary>
    public class ProgresoControllerTests
    {
        private readonly Mock<IProgresoI> _progresoRepoMock;
        private readonly ProgresoController _sut;

        public ProgresoControllerTests()
        {
            _progresoRepoMock = new Mock<IProgresoI>();
            _sut = new ProgresoController(_progresoRepoMock.Object);
        }

        [Fact]
        public async Task GetProgresos_ComoGestor_RetornaTodos()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Progreso>
            {
                new Progreso { Idprogreso = 1, idestudiante = 5, Idmodulo = 1, Porcentaje = 100, Completado = true },
                new Progreso { Idprogreso = 2, idestudiante = 8, Idmodulo = 1, Porcentaje = 50, Completado = false }
            };
            _progresoRepoMock.Setup(r => r.GetProgresos()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetProgresos();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var progresos = okResult.Value.Should().BeAssignableTo<IEnumerable<Progreso>>().Subject;
            progresos.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetProgresos_ComoEstudiante_FiltraPorEstudiante()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var listaMock = new List<Progreso>
            {
                new Progreso { Idprogreso = 1, idestudiante = 5, Idmodulo = 1, Porcentaje = 100, Completado = true },
                new Progreso { Idprogreso = 2, idestudiante = 8, Idmodulo = 1, Porcentaje = 50, Completado = false }
            };
            _progresoRepoMock.Setup(r => r.GetProgresos()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetProgresos();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var progresos = okResult.Value.Should().BeAssignableTo<IEnumerable<Progreso>>().Subject;
            progresos.Should().HaveCount(1);
            progresos.First().idestudiante.Should().Be(5);
        }

        [Fact]
        public async Task GetProgresos_SinSesion_RetornaUnauthorized()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetProgresos();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetProgresoById_ComoPropietario_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5, Porcentaje = 75 };
            _progresoRepoMock.Setup(r => r.GetProgresoById(1)).ReturnsAsync(progreso);

            // Act
            var resultado = await _sut.GetProgresoById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(progreso);
        }

        [Fact]
        public async Task GetProgresoById_EstudianteAjeno_RetornaForbidden403()
        {
            // Arrange (estudiante 99 consulta progreso de estudiante 5)
            TestUserHelper.SetUserContext(_sut, idUsuario: 99, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5, Porcentaje = 75 };
            _progresoRepoMock.Setup(r => r.GetProgresoById(1)).ReturnsAsync(progreso);

            // Act
            var resultado = await _sut.GetProgresoById(1);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetProgresoById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _progresoRepoMock.Setup(r => r.GetProgresoById(99)).ReturnsAsync((Progreso?)null);

            // Act
            var resultado = await _sut.GetProgresoById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateProgreso_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");

            // Act
            var resultado = await _sut.CreateProgreso(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateProgreso_EstudianteIntentaRegistrarParaOtro_RetornaForbidden403()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 99, Idmodulo = 1, Porcentaje = 20 };

            // Act
            var resultado = await _sut.CreateProgreso(progreso);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateProgreso_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5, Idmodulo = 1, Porcentaje = 100, Completado = true };
            _progresoRepoMock.Setup(r => r.CreateProgreso(progreso)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateProgreso(progreso);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ProgresoController.GetProgresoById));
            created.Value.Should().BeEquivalentTo(progreso);
        }

        [Fact]
        public async Task UpdateProgreso_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5 };

            // Act
            var resultado = await _sut.UpdateProgreso(10, progreso);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateProgreso_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5 };
            _progresoRepoMock.Setup(r => r.GetProgresoById(1)).ReturnsAsync((Progreso?)null);

            // Act
            var resultado = await _sut.UpdateProgreso(1, progreso);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteProgreso_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _progresoRepoMock.Setup(r => r.GetProgresoById(99)).ReturnsAsync((Progreso?)null);

            // Act
            var resultado = await _sut.DeleteProgreso(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteProgreso_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var progreso = new Progreso { Idprogreso = 1, idestudiante = 5 };
            _progresoRepoMock.Setup(r => r.GetProgresoById(1)).ReturnsAsync(progreso);
            _progresoRepoMock.Setup(r => r.DeleteProgreso(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteProgreso(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
