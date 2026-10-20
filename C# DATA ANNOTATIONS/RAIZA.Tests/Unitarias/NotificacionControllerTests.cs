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
    /// Pruebas unitarias para NotificacionController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Control de acceso por usuario (estudiantes solo ven sus notificaciones, gestores ven todas).
    /// - Privacidad y restricción de notificaciones ajenas (HTTP 403).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 401 Unauthorized, 403 Forbidden y 404 NotFound.
    /// </summary>
    public class NotificacionControllerTests
    {
        private readonly Mock<INotificacionI> _notifRepoMock;
        private readonly NotificacionController _sut;

        public NotificacionControllerTests()
        {
            _notifRepoMock = new Mock<INotificacionI>();
            _sut = new NotificacionController(_notifRepoMock.Object);
        }

        [Fact]
        public async Task GetNotificaciones_ComoGestor_RetornaTodas()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Notificacion>
            {
                new Notificacion { Idnotificacion = 1, idusuario = 5, mensaje = "Entrega calificada", estadoleido = false },
                new Notificacion { Idnotificacion = 2, idusuario = 8, mensaje = "Nuevo kit asignado", estadoleido = true }
            };
            _notifRepoMock.Setup(r => r.GetNotificaciones()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetNotificaciones();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var notifs = okResult.Value.Should().BeAssignableTo<IEnumerable<Notificacion>>().Subject;
            notifs.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetNotificaciones_ComoEstudiante_FiltraPorUsuario()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var listaMock = new List<Notificacion>
            {
                new Notificacion { Idnotificacion = 1, idusuario = 5, mensaje = "Entrega calificada", estadoleido = false },
                new Notificacion { Idnotificacion = 2, idusuario = 8, mensaje = "Nuevo kit asignado", estadoleido = true }
            };
            _notifRepoMock.Setup(r => r.GetNotificaciones()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetNotificaciones();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var notifs = okResult.Value.Should().BeAssignableTo<IEnumerable<Notificacion>>().Subject;
            notifs.Should().HaveCount(1);
            notifs.First().idusuario.Should().Be(5);
        }

        [Fact]
        public async Task GetNotificaciones_SinSesionIdentificable_RetornaUnauthorized()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetNotificaciones();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetNotificacionById_ComoDestinatario_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var notif = new Notificacion { Idnotificacion = 1, idusuario = 5, mensaje = "Mensaje propio" };
            _notifRepoMock.Setup(r => r.GetNotificacionById(1)).ReturnsAsync(notif);

            // Act
            var resultado = await _sut.GetNotificacionById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(notif);
        }

        [Fact]
        public async Task GetNotificacionById_UsuarioAjeno_RetornaForbidden403()
        {
            // Arrange (el usuario 99 intenta ver notificación del usuario 5)
            TestUserHelper.SetUserContext(_sut, idUsuario: 99, rol: "Estudiante");
            var notif = new Notificacion { Idnotificacion = 1, idusuario = 5, mensaje = "Mensaje ajeno" };
            _notifRepoMock.Setup(r => r.GetNotificacionById(1)).ReturnsAsync(notif);

            // Act
            var resultado = await _sut.GetNotificacionById(1);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetNotificacionById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _notifRepoMock.Setup(r => r.GetNotificacionById(99)).ReturnsAsync((Notificacion?)null);

            // Act
            var resultado = await _sut.GetNotificacionById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateNotificacion_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");

            // Act
            var resultado = await _sut.CreateNotificacion(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateNotificacion_Valida_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var notif = new Notificacion
            {
                Idnotificacion = 1,
                idusuario = 5,
                mensaje = "Nueva tarea publicada",
                tiponotificacion = "Sistema",
                estadoleido = false,
                fechaenvivo = DateTime.UtcNow
            };
            _notifRepoMock.Setup(r => r.CreateNotificacion(notif)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateNotificacion(notif);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(NotificacionController.GetNotificacionById));
            created.Value.Should().BeEquivalentTo(notif);
        }

        [Fact]
        public async Task UpdateNotificacion_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var notif = new Notificacion { Idnotificacion = 1, mensaje = "Test" };

            // Act
            var resultado = await _sut.UpdateNotificacion(5, notif);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateNotificacion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var notif = new Notificacion { Idnotificacion = 1, mensaje = "Test" };
            _notifRepoMock.Setup(r => r.GetNotificacionById(1)).ReturnsAsync((Notificacion?)null);

            // Act
            var resultado = await _sut.UpdateNotificacion(1, notif);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateNotificacion_Valida_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var notif = new Notificacion { Idnotificacion = 1, mensaje = "Test", estadoleido = true };
            _notifRepoMock.Setup(r => r.GetNotificacionById(1)).ReturnsAsync(notif);
            _notifRepoMock.Setup(r => r.UpdateNotificacion(notif)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateNotificacion(1, notif);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteNotificacion_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _notifRepoMock.Setup(r => r.GetNotificacionById(99)).ReturnsAsync((Notificacion?)null);

            // Act
            var resultado = await _sut.DeleteNotificacion(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteNotificacion_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var notif = new Notificacion { Idnotificacion = 1 };
            _notifRepoMock.Setup(r => r.GetNotificacionById(1)).ReturnsAsync(notif);
            _notifRepoMock.Setup(r => r.DeleteNotificacion(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteNotificacion(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
