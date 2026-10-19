using FluentAssertions;
using RAIZA.Controllers;
using Xunit;

namespace RAIZA.Tests.Unitarias
{
    /// <summary>
    /// Pruebas unitarias para la normalización de métodos de pago y estados.
    /// Valida que el sistema rechace entradas inválidas antes de enviar a la BD
    /// y acepte variantes comunes del frontend sin disparar errores 500.
    /// Aplica particiones de equivalencia y análisis de valores límite (Caja Negra).
    /// </summary>
    public class UtilidadesPagoTests
    {
        [Theory]
        [InlineData("Tarjeta", "Tarjeta")]
        [InlineData("tarjeta", "Tarjeta")]
        [InlineData("Tarjeta de Crédito", "Tarjeta")]
        [InlineData("Tarjeta de Débito", "Tarjeta")]
        [InlineData("tarjeta_credito", "Tarjeta")]
        [InlineData("TARJETA", "Tarjeta")]
        [InlineData("Nequi", "Nequi")]
        [InlineData("nequi", "Nequi")]
        [InlineData("NEQUI", "Nequi")]
        [InlineData("PSE", "PSE")]
        [InlineData("pse", "PSE")]
        [InlineData("Otro", "Otro")]
        [InlineData("otro", "Otro")]
        public void NormalizarMetodoPago_ValoresValidos_RetornaCanónico(string entrada, string esperado)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarMetodoPago(entrada);

            // Assert
            resultado.Should().Be(esperado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Efectivo")]
        [InlineData("Bitcoin")]
        [InlineData("PayPal")]
        [InlineData("12345")]
        [InlineData("Desconocido")]
        public void NormalizarMetodoPago_ValoresInvalidos_RetornaNull(string? entrada)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarMetodoPago(entrada);

            // Assert
            resultado.Should().BeNull();
        }

        [Theory]
        [InlineData("Pendiente", "Pendiente")]
        [InlineData("pendiente", "Pendiente")]
        [InlineData("Aprobado", "Aprobado")]
        [InlineData("aprobado", "Aprobado")]
        [InlineData("Rechazado", "Rechazado")]
        [InlineData("rechazado", "Rechazado")]
        public void NormalizarEstado_EstadosCompraValidos_RetornaCanónico(string entrada, string esperado)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarEstado(entrada, UtilidadesPago.EstadosCompra);

            // Assert
            resultado.Should().Be(esperado);
        }

        [Theory]
        [InlineData("Enviado")]
        [InlineData("Entregado")]
        [InlineData("Cancelado")]
        [InlineData("Pagado")]
        [InlineData("En camino")]
        [InlineData(null)]
        [InlineData("")]
        public void NormalizarEstado_EstadosCompraInvalidos_RetornaNull(string? entrada)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarEstado(entrada, UtilidadesPago.EstadosCompra);

            // Assert
            resultado.Should().BeNull();
        }

        [Theory]
        [InlineData("Pendiente", "Pendiente")]
        [InlineData("Enviado", "Enviado")]
        [InlineData("enviado", "Enviado")]
        [InlineData("Entregado", "Entregado")]
        [InlineData("Cancelado", "Cancelado")]
        public void NormalizarEstado_EstadosPedidoKitValidos_RetornaCanónico(string entrada, string esperado)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarEstado(entrada, UtilidadesPago.EstadosPedidoKit);

            // Assert
            resultado.Should().Be(esperado);
        }

        [Theory]
        [InlineData("Pagado")]
        [InlineData("En camino")]
        [InlineData("Rechazado")]
        [InlineData("Aprobado")]
        [InlineData(null)]
        [InlineData("")]
        public void NormalizarEstado_EstadosPedidoKitInvalidos_RetornaNull(string? entrada)
        {
            // Act
            var resultado = UtilidadesPago.NormalizarEstado(entrada, UtilidadesPago.EstadosPedidoKit);

            // Assert
            resultado.Should().BeNull();
        }
    }
}
