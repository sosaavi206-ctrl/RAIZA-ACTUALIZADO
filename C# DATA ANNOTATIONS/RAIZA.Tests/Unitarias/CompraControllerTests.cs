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
    /// Pruebas unitarias para CompraController.
    /// Valida:
    /// - Privacidad y aislamiento de compras: un estudiante solo ve sus pagos.
    /// - Manejo de compras existentes (200) o no encontradas (404).
    /// </summary>
    public class CompraControllerTests
    {
        private readonly Mock<CompraI> _compraRepoMock;
        private readonly CompraController _sut;

        public CompraControllerTests()
        {
            _compraRepoMock = new Mock<CompraI>();
            _sut = new CompraController(_compraRepoMock.Object);
        }

        [Fact]
        public async Task GetCompras_Estudiante_FiltraSoloSusCompras()
        {
            // Arrange: Estudiante con Id 12
            TestUserHelper.SetUserContext(_sut, idUsuario: 12, rol: "Estudiante");
            var listaTotal = new List<Compra>
            {
                new Compra { idcompra = 1, idestudiante = 12, Monto = 95000, MetodoPago = "PSE", Estado = "Aprobado" },
                new Compra { idcompra = 2, idestudiante = 99, Monto = 120000, MetodoPago = "Nequi", Estado = "Pendiente" } // Ajena
            };
            _compraRepoMock.Setup(r => r.GetCompras()).ReturnsAsync(listaTotal);

            // Act
            var resultado = await _sut.GetCompras();

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var compras = okResult.Value.Should().BeAssignableTo<IEnumerable<Compra>>().Subject;
            compras.Should().HaveCount(1);
            compras.Should().OnlyContain(c => c.idestudiante == 12);
        }

        [Fact]
        public async Task GetCompraById_Existente_RetornaOk()
        {
            // Arrange: Administrador consulta la compra
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            var compra = new Compra { idcompra = 5, idestudiante = 12, Monto = 95000, MetodoPago = "Tarjeta" };
            _compraRepoMock.Setup(r => r.GetCompraById(5)).ReturnsAsync(compra);

            // Act
            var resultado = await _sut.GetCompraById(5);

            // Assert
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var compraDevuelta = okResult.Value.Should().BeOfType<Compra>().Subject;
            compraDevuelta.idcompra.Should().Be(5);
        }

        [Fact]
        public async Task GetCompraById_Inexistente_Retorna404NotFound()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 1, rol: "Administrador");
            _compraRepoMock.Setup(r => r.GetCompraById(999)).ReturnsAsync((Compra?)null);

            // Act
            var resultado = await _sut.GetCompraById(999);

            // Assert
            resultado.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
