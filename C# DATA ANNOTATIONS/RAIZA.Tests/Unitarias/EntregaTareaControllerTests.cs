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
    /// Pruebas unitarias para EntregaTareaController.
    /// Valida que:
    /// - Un estudiante no pueda borrar ni modificar entregas ajenas (403 Forbidden).
    /// - Un estudiante pueda borrar su propia entrega para reenviarla (200 OK).
    /// - Al entregar una tarea como estudiante, el backend asigne forzosamente el ID del token
    ///   e ignore notas o instructores manipulados en el cuerpo de la petición.
    /// </summary>
    public class EntregaTareaControllerTests
    {
        private readonly Mock<IEntregaTareaI> _entregaRepoMock;
        private readonly EntregaTareaController _sut;

        public EntregaTareaControllerTests()
        {
            _entregaRepoMock = new Mock<IEntregaTareaI>();
            _sut = new EntregaTareaController(_entregaRepoMock.Object);
        }

        [Fact]
        public async Task DeleteEntregaTarea_EstudianteEliminaEntregaAjena_Retorna403Forbidden()
        {
            // Arrange: El estudiante con Id 10 intenta eliminar la entrega perteneciente al estudiante con Id 20
            TestUserHelper.SetUserContext(_sut, idUsuario: 10, rol: "Estudiante");
            var entregaAjena = new EntregaTarea
            {
                Identregatarea = 1,
                idestudiante = 20,
                UrlArchivo = "https://raiza.com/entregas/abc.pdf"
            };
            _entregaRepoMock.Setup(r => r.GetEntregaTareaById(1)).ReturnsAsync(entregaAjena);

            // Act
            var resultado = await _sut.DeleteEntregaTarea(1);

            // Assert
            var statusResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(403);
            _entregaRepoMock.Verify(r => r.DeleteEntregaTarea(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEntregaTarea_EstudianteEliminaEntregaPropia_RetornaOk()
        {
            // Arrange: El estudiante con Id 10 elimina su propia entrega
            TestUserHelper.SetUserContext(_sut, idUsuario: 10, rol: "Estudiante");
            var entregaPropia = new EntregaTarea
            {
                Identregatarea = 2,
                idestudiante = 10,
                UrlArchivo = "https://raiza.com/entregas/tarea10.pdf"
            };
            _entregaRepoMock.Setup(r => r.GetEntregaTareaById(2)).ReturnsAsync(entregaPropia);
            _entregaRepoMock.Setup(r => r.DeleteEntregaTarea(2)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteEntregaTarea(2);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
            _entregaRepoMock.Verify(r => r.DeleteEntregaTarea(2), Times.Once);
        }

        [Fact]
        public async Task CreateEntregaTarea_ComoEstudiante_FuerzaIdPropioYAnulaCalificacion()
        {
            // Arrange: El estudiante intenta inyectar idestudiante=99 y auto-calificarse con 5.0
            TestUserHelper.SetUserContext(_sut, idUsuario: 7, rol: "Estudiante");
            var intentoManipulacion = new EntregaTarea
            {
                idestudiante = 99,
                idtarea = 1,
                Calificacion = 5.0m,
                idinstructorcalifica = 3,
                UrlArchivo = "https://raiza.com/entregas/evidencia.pdf"
            };

            _entregaRepoMock.Setup(r => r.CreateEntregaTarea(It.IsAny<EntregaTarea>())).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateEntregaTarea(intentoManipulacion);

            // Assert: Se debe haber forzado su ID de sesión y limpiado la calificación
            intentoManipulacion.idestudiante.Should().Be(7);
            intentoManipulacion.Calificacion.Should().BeNull();
            intentoManipulacion.idinstructorcalifica.Should().BeNull();
            resultado.Should().BeOfType<CreatedAtActionResult>();
        }
    }
}
