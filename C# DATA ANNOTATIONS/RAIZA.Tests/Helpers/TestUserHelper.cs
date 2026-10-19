using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RAIZA.Tests.Helpers
{
    public static class TestUserHelper
    {
        public static void SetUserContext(ControllerBase controller, int? idUsuario = null, string rol = "Estudiante", string correo = "test@raiza.com")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, correo),
                new Claim(ClaimTypes.Role, rol)
            };

            if (idUsuario.HasValue)
            {
                claims.Add(new Claim("IdUsuario", idUsuario.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }
    }
}
