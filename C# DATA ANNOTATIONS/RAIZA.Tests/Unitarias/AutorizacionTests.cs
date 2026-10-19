using System.Security.Claims;
using FluentAssertions;
using RAIZA.Controllers;
using Xunit;

namespace RAIZA.Tests.Unitarias
{
    /// <summary>
    /// Pruebas unitarias para las utilidades de autorización y extracción de claims del JWT.
    /// Verifica la regla de control de acceso basada en roles (RBAC) y la lectura segura del usuario de sesión.
    /// </summary>
    public class AutorizacionTests
    {
        [Theory]
        [InlineData("Administrador", true)]
        [InlineData("Instructor", true)]
        [InlineData("Estudiante", false)]
        [InlineData("Invitado", false)]
        public void EsGestor_ValidaRolCorrectamente(string rol, bool esperadoEsGestor)
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.Role, rol) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var esGestor = Autorizacion.EsGestor(principal);

            // Assert
            esGestor.Should().Be(esperadoEsGestor);
        }

        [Fact]
        public void EsGestor_UsuarioSinRoles_RetornaFalse()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var esGestor = Autorizacion.EsGestor(principal);

            // Assert
            esGestor.Should().BeFalse();
        }

        [Fact]
        public void IdUsuario_ClaimPresenteYValido_RetornaIdNumerico()
        {
            // Arrange
            var claims = new[] { new Claim("IdUsuario", "42") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var id = Autorizacion.IdUsuario(principal);

            // Assert
            id.Should().Be(42);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("-")]
        public void IdUsuario_ClaimNoNumerico_RetornaNull(string valorClaim)
        {
            // Arrange
            var claims = new[] { new Claim("IdUsuario", valorClaim) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            // Act
            var id = Autorizacion.IdUsuario(principal);

            // Assert
            id.Should().BeNull();
        }

        [Fact]
        public void IdUsuario_ClaimAusente_RetornaNull()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var id = Autorizacion.IdUsuario(principal);

            // Assert
            id.Should().BeNull();
        }
    }
}
