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
    /// Pruebas unitarias para ClaseParticipanteController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Registro y consulta de participantes en clases en vivo.
    /// - Prevención de doble inscripción del mismo estudiante en la misma clase (clave compuesta idclase, idestudiante).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest y 404 NotFound.
    /// </summary>
    public class ClaseParticipanteControllerTests
    {
        private readonly Mock<ClaseParticipanteI> _participanteRepoMock;
        private readonly ClaseParticipanteController _sut;

        public ClaseParticipanteControllerTests()
        {
            _participanteRepoMock = new Mock<ClaseParticipanteI>();
            _sut = new ClaseParticipanteController(_participanteRepoMock.Object);
        }

        [Fact]
        public async Task GetClaseParticipantes_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var listaMock = new List<ClaseParticipante>
            {
                new ClaseParticipante { idclase = 1, idestudiante = 10, FechaIngreso = DateTime.UtcNow },
                new ClaseParticipante { idclase = 1, idestudiante = 11, FechaIngreso = DateTime.UtcNow }
            };
            _participanteRepoMock.Setup(r => r.GetClaseParticipantes()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetClaseParticipantes();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var participantes = okResult.Value.Should().BeAssignableTo<IEnumerable<ClaseParticipante>>().Subject;
            participantes.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetClaseParticipanteById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync(participante);

            // Act
            var resultado = await _sut.GetClaseParticipanteById(1, 10);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(participante);
        }

        [Fact]
        public async Task GetClaseParticipanteById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 99)).ReturnsAsync((ClaseParticipante?)null);

            // Act
            var resultado = await _sut.GetClaseParticipanteById(1, 99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateClaseParticipante_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");

            // Act
            var resultado = await _sut.CreateClaseParticipante(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateClaseParticipante_YaInscrito_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync(participante);

            // Act
            var resultado = await _sut.CreateClaseParticipante(participante);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateClaseParticipante_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10, FechaIngreso = DateTime.UtcNow };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync((ClaseParticipante?)null);
            _participanteRepoMock.Setup(r => r.CreateClaseParticipante(participante)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateClaseParticipante(participante);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ClaseParticipanteController.GetClaseParticipanteById));
            created.Value.Should().BeEquivalentTo(participante);
        }

        [Fact]
        public async Task UpdateClaseParticipante_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };

            // Act
            var resultado = await _sut.UpdateClaseParticipante(2, 10, participante);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateClaseParticipante_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync((ClaseParticipante?)null);

            // Act
            var resultado = await _sut.UpdateClaseParticipante(1, 10, participante);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateClaseParticipante_Valido_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync(participante);
            _participanteRepoMock.Setup(r => r.UpdateClaseParticipante(participante)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateClaseParticipante(1, 10, participante);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteClaseParticipante_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 99)).ReturnsAsync((ClaseParticipante?)null);

            // Act
            var resultado = await _sut.DeleteClaseParticipante(1, 99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteClaseParticipante_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Instructor");
            var participante = new ClaseParticipante { idclase = 1, idestudiante = 10 };
            _participanteRepoMock.Setup(r => r.GetClaseParticipanteById(1, 10)).ReturnsAsync(participante);
            _participanteRepoMock.Setup(r => r.DeleteClaseParticipante(1, 10)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteClaseParticipante(1, 10);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
