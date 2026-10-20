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
    /// Pruebas unitarias para CertificadoController siguiendo la Guía de Pruebas ADSO.
    /// Valida:
    /// - Control de acceso por roles (un estudiante solo ve sus certificados, gestores ven todos).
    /// - Protección de certificados ajenos (HTTP 403 Forbidden).
    /// - Operaciones CRUD y códigos HTTP: 200 OK, 201 CreatedAtAction, 400 BadRequest, 401 Unauthorized, 403 Forbidden y 404 NotFound.
    /// </summary>
    public class CertificadoControllerTests
    {
        private readonly Mock<CertificadoI> _certificadoRepoMock;
        private readonly CertificadoController _sut;

        public CertificadoControllerTests()
        {
            _certificadoRepoMock = new Mock<CertificadoI>();
            _sut = new CertificadoController(_certificadoRepoMock.Object);
        }

        [Fact]
        public async Task GetCertificados_ComoGestor_RetornaTodosLosCertificados()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var listaMock = new List<Certificado>
            {
                new Certificado { idcertificado = 1, idestudiante = 5, CodigoVerificacion = "CERT-001", UrlPdf = "https://raiza.edu/c1.pdf" },
                new Certificado { idcertificado = 2, idestudiante = 10, CodigoVerificacion = "CERT-002", UrlPdf = "https://raiza.edu/c2.pdf" }
            };
            _certificadoRepoMock.Setup(r => r.GetCertificados()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetCertificados();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var certs = okResult.Value.Should().BeAssignableTo<IEnumerable<Certificado>>().Subject;
            certs.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetCertificados_ComoEstudiante_FiltraSoloLosPropios()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var listaMock = new List<Certificado>
            {
                new Certificado { idcertificado = 1, idestudiante = 5, CodigoVerificacion = "CERT-001", UrlPdf = "https://raiza.edu/c1.pdf" },
                new Certificado { idcertificado = 2, idestudiante = 10, CodigoVerificacion = "CERT-002", UrlPdf = "https://raiza.edu/c2.pdf" }
            };
            _certificadoRepoMock.Setup(r => r.GetCertificados()).ReturnsAsync(listaMock);

            // Act
            var resultado = await _sut.GetCertificados();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var certs = okResult.Value.Should().BeAssignableTo<IEnumerable<Certificado>>().Subject;
            certs.Should().HaveCount(1);
            certs.First().idestudiante.Should().Be(5);
        }

        [Fact]
        public async Task GetCertificados_SinSesionIdentificable_RetornaUnauthorized()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetCertificados();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetCertificadoById_ComoEstudiantePropietario_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            var cert = new Certificado { idcertificado = 1, idestudiante = 5, CodigoVerificacion = "CERT-001", UrlPdf = "https://raiza.edu/c1.pdf" };
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(1)).ReturnsAsync(cert);

            // Act
            var resultado = await _sut.GetCertificadoById(1);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(cert);
        }

        [Fact]
        public async Task GetCertificadoById_ComoEstudianteAjeno_RetornaForbidden()
        {
            // Arrange (el estudiante 99 intenta ver el certificado del estudiante 5)
            TestUserHelper.SetUserContext(_sut, idUsuario: 99, rol: "Estudiante");
            var cert = new Certificado { idcertificado = 1, idestudiante = 5, CodigoVerificacion = "CERT-001", UrlPdf = "https://raiza.edu/c1.pdf" };
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(1)).ReturnsAsync(cert);

            // Act
            var resultado = await _sut.GetCertificadoById(1);

            // Assert
            var objResult = resultado.Should().BeOfType<ObjectResult>().Subject;
            objResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetCertificadoById_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(99)).ReturnsAsync((Certificado?)null);

            // Act
            var resultado = await _sut.GetCertificadoById(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateCertificado_Null_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");

            // Act
            var resultado = await _sut.CreateCertificado(null!);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateCertificado_Valido_RetornaCreatedAtAction()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var cert = new Certificado
            {
                idcertificado = 1,
                idestudiante = 5,
                idmodulo = 2,
                CodigoVerificacion = "CERT-ABC",
                UrlPdf = "https://raiza.edu/c.pdf",
                FechaEmision = DateTime.UtcNow
            };
            _certificadoRepoMock.Setup(r => r.CreateCertificado(cert)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.CreateCertificado(cert);

            // Assert
            var created = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(CertificadoController.GetCertificadoById));
            created.Value.Should().BeEquivalentTo(cert);
        }

        [Fact]
        public async Task UpdateCertificado_IdMismatch_RetornaBadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var cert = new Certificado { idcertificado = 5, idestudiante = 1 };

            // Act
            var resultado = await _sut.UpdateCertificado(10, cert);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateCertificado_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var cert = new Certificado { idcertificado = 5, idestudiante = 1 };
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(5)).ReturnsAsync((Certificado?)null);

            // Act
            var resultado = await _sut.UpdateCertificado(5, cert);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateCertificado_Valido_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var cert = new Certificado { idcertificado = 5, idestudiante = 1 };
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(5)).ReturnsAsync(cert);
            _certificadoRepoMock.Setup(r => r.UpdateCertificado(cert)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.UpdateCertificado(5, cert);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task DeleteCertificado_Inexistente_RetornaNotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(99)).ReturnsAsync((Certificado?)null);

            // Act
            var resultado = await _sut.DeleteCertificado(99);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteCertificado_Existente_RetornaOk()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var cert = new Certificado { idcertificado = 1 };
            _certificadoRepoMock.Setup(r => r.GetCertificadoById(1)).ReturnsAsync(cert);
            _certificadoRepoMock.Setup(r => r.DeleteCertificado(1)).ReturnsAsync(true);

            // Act
            var resultado = await _sut.DeleteCertificado(1);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
        }
    }
}
