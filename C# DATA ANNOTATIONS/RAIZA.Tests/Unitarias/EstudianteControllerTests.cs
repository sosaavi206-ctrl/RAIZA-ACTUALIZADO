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
    /// Pruebas unitarias para EstudianteController.
    /// Valida:
    /// - Que un estudiante solo acceda a su propio perfil (aislamiento de datos).
    /// - Respuestas canónicas 200 y 404.
    /// </summary>
    public class EstudianteControllerTests
    {
        private readonly Mock<IEstudiante_I> _estudianteRepoMock;
        private readonly Mock<IUsuarioI> _usuarioRepoMock;
        private readonly EstudianteController _sut;

        public EstudianteControllerTests()
        {
            _estudianteRepoMock = new Mock<IEstudiante_I>();
            _usuarioRepoMock = new Mock<IUsuarioI>();
            _sut = new EstudianteController(_estudianteRepoMock.Object, _usuarioRepoMock.Object);
        }

        [Fact]
        public async Task GetEstudiantes_Estudiante_FiltraSoloSuRegistro()
        {
            // Arrange: Estudiante con Id 8
            TestUserHelper.SetUserContext(_sut, idUsuario: 8, rol: "Estudiante");
            var listaTotal = new List<Estudiante>
            {
                new Estudiante { idestudiante = 8, Espremium = true },
                new Estudiante { idestudiante = 15, Espremium = false } // Ajeno
            };
            _estudianteRepoMock.Setup(r => r.GetEstudiantes()).ReturnsAsync(listaTotal);

            // Act
            var resultado = await _sut.GetEstudiantes();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var estudiantes = okResult.Value.Should().BeAssignableTo<IEnumerable<Estudiante>>().Subject;
            estudiantes.Should().HaveCount(1);
            estudiantes.Should().OnlyContain(e => e.idestudiante == 8);
        }

        [Fact]
        public async Task GetEstudianteById_Existente_RetornaOk()
        {
            // Arrange: Administrador consulta el estudiante
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var estudiante = new Estudiante { idestudiante = 8, Espremium = true };
            _estudianteRepoMock.Setup(r => r.GetEstudianteById(8)).ReturnsAsync(estudiante);

            // Act
            var resultado = await _sut.GetEstudianteById(8);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var estudianteDevuelto = okResult.Value.Should().BeOfType<Estudiante>().Subject;
            estudianteDevuelto.idestudiante.Should().Be(8);
        }

        [Fact]
        public async Task GetEstudianteById_Inexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _estudianteRepoMock.Setup(r => r.GetEstudianteById(999)).ReturnsAsync((Estudiante?)null);

            // Act
            var resultado = await _sut.GetEstudianteById(999);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
