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
    /// Pruebas unitarias para PedidoKitController.
    /// Valida:
    /// - Que un estudiante solo pueda ver sus propios pedidos (aislamiento de datos).
    /// - Que no se puedan crear pedidos sin sesión activa (401).
    /// - Que se rechacen kits inexistentes, perfiles faltantes o métodos de pago no permitidos (400).
    /// - Que un pedido válido cree la compra y el pedido de forma consistente (201).
    /// </summary>
    public class PedidoKitControllerTests
    {
        private readonly Mock<IPedidoKit_I> _pedidoKitRepoMock;
        private readonly Mock<IClassKitI> _classKitRepoMock;
        private readonly Mock<IEstudiante_I> _estudianteRepoMock;
        private readonly Mock<CompraI> _compraRepoMock;
        private readonly PedidoKitController _sut;

        public PedidoKitControllerTests()
        {
            _pedidoKitRepoMock = new Mock<IPedidoKit_I>();
            _classKitRepoMock = new Mock<IClassKitI>();
            _estudianteRepoMock = new Mock<IEstudiante_I>();
            _compraRepoMock = new Mock<CompraI>();

            _sut = new PedidoKitController(
                _pedidoKitRepoMock.Object,
                _classKitRepoMock.Object,
                _estudianteRepoMock.Object,
                _compraRepoMock.Object
            );
        }

        [Fact]
        public async Task GetMisPedidos_SinTokenValido_Retorna401Unauthorized()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: null, rol: "Estudiante");

            // Act
            var resultado = await _sut.GetMisPedidos();

            // Assert
            resultado.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMisPedidos_ConTokenEstudiante_FiltraUnicamentePedidosPropios()
        {
            // Arrange: El estudiante de la sesión tiene Id 10
            TestUserHelper.SetUserContext(_sut, idUsuario: 10, rol: "Estudiante");
            var listaTotal = new List<PedidoKit>
            {
                new PedidoKit { idPedidoKit = 1, Idestudiante = 10, Estado = "Pendiente" },
                new PedidoKit { idPedidoKit = 2, Idestudiante = 20, Estado = "Enviado" }, // De otro estudiante
                new PedidoKit { idPedidoKit = 3, Idestudiante = 10, Estado = "Entregado" }
            };
            _pedidoKitRepoMock.Setup(r => r.GetPedidoKits()).ReturnsAsync(listaTotal);

            // Act
            var resultado = await _sut.GetMisPedidos();

            // Assert: Solo deben devolverse los pedidos con Idestudiante == 10
            var okResult = resultado.Should().BeOfType<OkObjectResult>().Subject;
            var misPedidos = okResult.Value.Should().BeAssignableTo<IEnumerable<PedidoKit>>().Subject;
            misPedidos.Should().HaveCount(2);
            misPedidos.Should().OnlyContain(p => p.Idestudiante == 10);
        }

        [Fact]
        public async Task CreatePedidoKit_KitInexistente_Retorna400BadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 5, rol: "Estudiante");
            _classKitRepoMock.Setup(r => r.GetClassKitById(99)).ReturnsAsync((Class_Kit?)null);

            var request = new RegistrarPedidoKitRequest
            {
                idClassKit = 99,
                MetodoPago = "PSE"
            };

            // Act
            var resultado = await _sut.CreatePedidoKit(request);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreatePedidoKit_EstudianteSinPerfil_Retorna400BadRequest()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 8, rol: "Estudiante");
            _classKitRepoMock.Setup(r => r.GetClassKitById(1))
                            .ReturnsAsync(new Class_Kit { idclass_kit = 1, name = "Kit Básico", precio = 120000 });
            _estudianteRepoMock.Setup(r => r.GetEstudianteById(8)).ReturnsAsync((Estudiante?)null);

            var request = new RegistrarPedidoKitRequest
            {
                idClassKit = 1,
                MetodoPago = "Tarjeta"
            };

            // Act
            var resultado = await _sut.CreatePedidoKit(request);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreatePedidoKit_MetodoPagoInvalido_Retorna400BadRequest()
        {
            // Arrange: Se intenta pagar con método no admitido por la base de datos
            TestUserHelper.SetUserContext(_sut, idUsuario: 8, rol: "Estudiante");
            _classKitRepoMock.Setup(r => r.GetClassKitById(1))
                            .ReturnsAsync(new Class_Kit { idclass_kit = 1, name = "Kit Básico", precio = 120000 });
            _estudianteRepoMock.Setup(r => r.GetEstudianteById(8))
                               .ReturnsAsync(new Estudiante { idestudiante = 8 });

            var request = new RegistrarPedidoKitRequest
            {
                idClassKit = 1,
                MetodoPago = "Criptomoneda" // Inválido
            };

            // Act
            var resultado = await _sut.CreatePedidoKit(request);

            // Assert
            resultado.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreatePedidoKit_DatosValidos_CreaCompraYPedidoRetornando201()
        {
            // Arrange
            TestUserHelper.SetUserContext(_sut, idUsuario: 8, rol: "Estudiante");
            var kit = new Class_Kit { idclass_kit = 2, name = "Kit Avanzado", precio = 250000, idmodulo = 3 };
            _classKitRepoMock.Setup(r => r.GetClassKitById(2)).ReturnsAsync(kit);
            _estudianteRepoMock.Setup(r => r.GetEstudianteById(8)).ReturnsAsync(new Estudiante { idestudiante = 8 });

            _compraRepoMock.Setup(r => r.CreateCompra(It.IsAny<Compra>())).ReturnsAsync(true);
            _pedidoKitRepoMock.Setup(r => r.CreatePedidoKit(It.IsAny<PedidoKit>())).ReturnsAsync(true);

            var request = new RegistrarPedidoKitRequest
            {
                idClassKit = 2,
                Cantidad = 1,
                MetodoPago = "Nequi",
                Direccionenvio = "Calle 10 # 20-30"
            };

            // Act
            var resultado = await _sut.CreatePedidoKit(request);

            // Assert
            var createdResult = resultado.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            _compraRepoMock.Verify(r => r.CreateCompra(It.Is<Compra>(c =>
                c.MetodoPago == "Nequi" && c.Monto == 250000 && c.Estado == "Pendiente" && c.idestudiante == 8)), Times.Once);
            _pedidoKitRepoMock.Verify(r => r.CreatePedidoKit(It.Is<PedidoKit>(p =>
                p.idclasskit == 2 && p.Idestudiante == 8 && p.Estado == "Pendiente")), Times.Once);
        }
    }
}
