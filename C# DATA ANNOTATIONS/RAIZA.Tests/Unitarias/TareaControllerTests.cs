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
    /// Pruebas unitarias para TareaController.
    /// Valida:
    /// - Códigos HTTP (200, 400, 401, 404).
    /// - Verificación de identidad mediante token JWT.
    /// - Lógica de entrega de tareas y upsert.
    /// </summary>
    public class TareaControllerTests
    {
        private readonly Mock<ITarea_I> _tareaRepoMock;
        private readonly Mock<IEntregaTareaI> _entregaRepoMock;
        private readonly TareaController _sut;

        public TareaControllerTests()
        {
            _tareaRepoMock = new Mock<ITarea_I>();
            _entregaRepoMock = new Mock<IEntregaTareaI>();
            _sut = new TareaController(_tareaRepoMock.Object, _entregaRepoMock.Object);
        }

        [Fact]
        public async Task GetTareas_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var tareasMock = new List<Tarea>
            {
                new Tarea { idtarea = 1, Titulo = "Construir Circuito", idmodulo = 1 },
                new Tarea { idtarea = 2, Titulo = "Programar Sensor", idmodulo = 1 }
            };
            _tareaRepoMock.Setup(r => r.GetTareas()).ReturnsAsync(tareasMock);

            // Act
            var resultado = await _sut.GetTareas();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var tareas = okResult.Value.Should().BeAssignableTo<IEnumerable<Tarea>>().Subject;
            tareas.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTareaById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            var tarea = new Tarea { idtarea = 1, Titulo = "Práctica LED" };
            _tareaRepoMock.Setup(r => r.GetTareaById(1)).ReturnsAsync(tarea);

            // Act
            var resultado = await _sut.GetTareaById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var tareaRetornada = okResult.Value.Should().BeOfType<Tarea>().Subject;
            tareaRetornada.idtarea.Should().Be(1);
        }

        [Fact]
        public async Task GetTareaById_Inexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Estudiante");
            _tareaRepoMock.Setup(r => r.GetTareaById(999)).ReturnsAsync((Tarea?)null);

            // Act
            var resultado = await _sut.GetTareaById(999);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMisTareas_SinTokenOIdInvalido_Retorna401Unauthorized()
        {
            // Arrange: Contexto sin claim IdUsuario
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetMisTareas();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMisTareas_ConTokenEstudiante_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 15, rol: "Estudiante");
            _tareaRepoMock.Setup(r => r.GetTareas()).ReturnsAsync(new List<Tarea>());

            // Act
            var resultado = await _sut.GetMisTareas();

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task EntregarTarea_DatosInvalidos_Retorna400BadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");

            // Act: modelo nulo o con IdTarea <= 0
            var resultadoNull = await _sut.EntregarTarea(null!);
            var resultadoIdCero = await _sut.EntregarTarea(new EntregaDto { IdTarea = 0 });

            // Assert
            resultadoNull.Should().BeOfType<BadRequestObjectResult>();
            resultadoIdCero.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EntregarTarea_TareaInexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            _tareaRepoMock.Setup(r => r.GetTareaById(50)).ReturnsAsync((Tarea?)null);

            // Act
            var resultado = await _sut.EntregarTarea(new EntregaDto { IdTarea = 50, Comentario = "Mi entrega" });

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task EntregarTarea_EstudianteValido_RegistraEntregaYRetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            _tareaRepoMock.Setup(r => r.GetTareaById(1))
                          .ReturnsAsync(new Tarea { idtarea = 1, Titulo = "Robótica" });
            _entregaRepoMock.Setup(r => r.GetEntregaTareas())
                            .ReturnsAsync(new List<EntregaTarea>()); // No tiene entrega previa
            _entregaRepoMock.Setup(r => r.CreateEntregaTarea(It.IsAny<EntregaTarea>()))
                            .ReturnsAsync(true);

            // Act
            var resultado = await _sut.EntregarTarea(new EntregaDto { IdTarea = 1, Comentario = "Listo el circuito" });

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            _entregaRepoMock.Verify(r => r.CreateEntregaTarea(It.Is<EntregaTarea>(e =>
                e.idtarea == 1 && e.idestudiante == 5 && e.Comentario == "Listo el circuito")), Times.Once);
        }
    }
}
