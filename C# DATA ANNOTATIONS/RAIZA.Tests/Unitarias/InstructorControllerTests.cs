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
    /// Pruebas unitarias para InstructorController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Consulta y administración de perfiles de instructores de robótica.
    /// - Validaciones de integridad referencial con Usuario (rol Instructor requerido, unicidad).
    /// - Códigos de respuesta HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 404 NotFound y 409 Conflict.
    /// </summary>
    public class InstructorControllerTests
    {
        private readonly Mock<IInstructor_I> _instructorRepoMock;
        private readonly Mock<IUsuarioI> _usuarioRepoMock;
        private readonly InstructorController _sut;

        public InstructorControllerTests()
        {
            _instructorRepoMock = new Mock<IInstructor_I>();
            _usuarioRepoMock = new Mock<IUsuarioI>();
            _sut = new InstructorController(_instructorRepoMock.Object, _usuarioRepoMock.Object);
        }

        [Fact]
        public async Task GetInstructores_RetornaOkConLista()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Instructor>
            {
                new Instructor { idinstructor = 1, Especialidad = "Robótica y Arduino", Biografia = "Ingeniero Mecatrónico" },
                new Instructor { idinstructor = 2, Especialidad = "Inteligencia Artificial", Biografia = "Magíster en Sistemas" }
            };
            _instructorRepoMock.Setup(r => r.GetInstructores()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetInstructores();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var instructores = okResult.Value.Should().BeAssignableTo<IEnumerable<Instructor>>().Subject;
            instructores.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetInstructorById_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 1, Especialidad = "Robótica", Biografia = "Bio" };
            _instructorRepoMock.Setup(r => r.GetInstructorById(1)).ReturnsAsync(instructor);

            // Act
            var resultado = await _sut.GetInstructorById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(instructor);
        }

        [Fact]
        public async Task GetInstructorById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _instructorRepoMock.Setup(r => r.GetInstructorById(99)).ReturnsAsync((Instructor?)null);

            // Act
            var resultado = await _sut.GetInstructorById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateInstructor_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");

            // Act
            var resultado = await _sut.CreateInstructor(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CreateInstructor_IdInvalido_RetornaBadRequest(int idInvalido)
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = idInvalido, Especialidad = "Electrónica", Biografia = "Docente" };

            // Act
            var resultado = await _sut.CreateInstructor(instructor);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateInstructor_UsuarioInexistente_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 10, Especialidad = "Robótica", Biografia = "Docente" };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync((Usuario?)null);

            // Act
            var resultado = await _sut.CreateInstructor(instructor);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateInstructor_UsuarioNoTieneRolInstructor_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 10, Especialidad = "Robótica", Biografia = "Docente" };
            var usuarioEstudiante = new Usuario { Id = 10, Nombre = "Pedro", Rol = "Estudiante" };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioEstudiante);

            // Act
            var resultado = await _sut.CreateInstructor(instructor);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateInstructor_YaExistePerfil_RetornaConflict()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 10, Especialidad = "Robótica", Biografia = "Docente" };
            var usuarioInstructor = new Usuario { Id = 10, Nombre = "Prof. Gómez", Rol = "Instructor" };
            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioInstructor);
            _instructorRepoMock.Setup(r => r.GetInstructorById(10)).ReturnsAsync(instructor);

            // Act
            var resultado = await _sut.CreateInstructor(instructor);

            // Assert
            resultado.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task CreateInstructor_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 10, Especialidad = "Robótica", Biografia = "Docente apasionado" };
            var usuarioInstructor = new Usuario { Id = 10, Nombre = "Prof. Gómez", Rol = "Instructor" };

            _usuarioRepoMock.Setup(r => r.GetUsuarioById(10)).ReturnsAsync(usuarioInstructor);
            _instructorRepoMock.Setup(r => r.GetInstructorById(10)).ReturnsAsync((Instructor?)null);
            _instructorRepoMock.Setup(r => r.CreateInstructor(instructor)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateInstructor(instructor);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(InstructorController.GetInstructorById));
            created.Value.Should().BeEquivalentTo(instructor);
        }

        [Fact]
        public async Task UpdateInstructor_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 5, Especialidad = "Robótica", Biografia = "Bio" };

            // Act
            var resultado = await _sut.UpdateInstructor(10, instructor);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateInstructor_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 5, Especialidad = "Robótica", Biografia = "Bio" };
            _instructorRepoMock.Setup(r => r.GetInstructorById(5)).ReturnsAsync((Instructor?)null);

            // Act
            var resultado = await _sut.UpdateInstructor(5, instructor);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateInstructor_Valido_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 5, Especialidad = "Robótica Avanzada", Biografia = "Bio actualizada" };
            _instructorRepoMock.Setup(r => r.GetInstructorById(5)).ReturnsAsync(instructor);
            _instructorRepoMock.Setup(r => r.UpdateInstructor(instructor)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateInstructor(5, instructor);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteInstructor_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _instructorRepoMock.Setup(r => r.GetInstructorById(99)).ReturnsAsync((Instructor?)null);

            // Act
            var resultado = await _sut.DeleteInstructor(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteInstructor_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var instructor = new Instructor { idinstructor = 1, Especialidad = "Robótica", Biografia = "Bio" };
            _instructorRepoMock.Setup(r => r.GetInstructorById(1)).ReturnsAsync(instructor);
            _instructorRepoMock.Setup(r => r.DeleteInstructor(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteInstructor(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
