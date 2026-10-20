using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace RAIZA.Services
{
    public interface ICorreoService
    {
        Task EnviarCodigoAsync(string destino, string nombre, string codigo, bool esVerificacion);
    }

    public class CorreoService : ICorreoService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<CorreoService> _logger;
        private readonly IHostEnvironment _env;

        public CorreoService(IConfiguration config, ILogger<CorreoService> logger, IHostEnvironment env)
        {
            _config = config;
            _logger = logger;
            _env = env;
        }

        public async Task EnviarCodigoAsync(string destino, string nombre, string codigo, bool esVerificacion)
        {
            var host = _config["Correo:Host"];

            // Sin SMTP configurado, solo en desarrollo: el código sale en la consola del servidor
            if (string.IsNullOrWhiteSpace(host))
            {
                if (_env.IsDevelopment())
                {
                    _logger.LogWarning("[DEV] SMTP sin configurar. Código para {Correo}: {Codigo}", destino, codigo);
                    return;
                }
                throw new InvalidOperationException("Falta la configuración Correo:Host");
            }

            var usuario = _config["Correo:Usuario"];
            var clave = _config["Correo:Clave"];

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
                throw new InvalidOperationException("Falta la configuración Correo:Usuario o Correo:Clave");

            var remitente = _config["Correo:Remitente"];
            if (string.IsNullOrWhiteSpace(remitente))
                remitente = usuario;

            var asunto = esVerificacion
                ? $"{codigo} es tu código para verificar tu cuenta en RAIZA"
                : $"{codigo} es tu código para recuperar tu contraseña en RAIZA";

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(
                _config["Correo:NombreRemitente"] ?? "RAIZA",
                remitente));
            mensaje.To.Add(new MailboxAddress(nombre, destino));
            mensaje.Subject = asunto;
            mensaje.Body = new BodyBuilder
            {
                HtmlBody = ConstruirHtml(nombre, codigo, esVerificacion),
                TextBody = ConstruirTexto(nombre, codigo, esVerificacion)
            }.ToMessageBody();

            using var smtp = new SmtpClient();
            var puerto = int.TryParse(_config["Correo:Puerto"], out var p) ? p : 587;
            await smtp.ConnectAsync(host, puerto, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(usuario, clave);
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);
        }

        private static string PrimerNombre(string nombre) =>
            string.IsNullOrWhiteSpace(nombre) ? "" : nombre.Trim().Split(' ')[0];

        private static string ConstruirTexto(string nombre, string codigo, bool esVerificacion)
        {
            var intro = esVerificacion
                ? "Para terminar de crear tu cuenta en RAIZA, escribe este código:"
                : "Recibimos una solicitud para cambiar tu contraseña en RAIZA. Escribe este código:";
            return $"Hola {PrimerNombre(nombre)},\n\n{intro}\n\n{codigo}\n\nVence en 15 minutos. " +
                   "Si no fuiste tú, ignora este mensaje: nadie puede entrar a tu cuenta sin este código.\n\nRAIZA – Escuela de Joyería y Orfebrería";
        }

        private static string ConstruirHtml(string nombre, string codigo, bool esVerificacion)
        {
            var intro = esVerificacion
                ? "Para terminar de crear tu cuenta en RAIZA, escribe este código en la página:"
                : "Recibimos una solicitud para cambiar tu contraseña. Escribe este código en la página:";
            var nombreSeguro = System.Net.WebUtility.HtmlEncode(PrimerNombre(nombre));

            return $@"<!DOCTYPE html>
<html lang=""es""><body style=""margin:0;padding:24px;background:#f7f4e6;font-family:Arial,Helvetica,sans-serif;color:#2e3324;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0""><tr><td align=""center"">
    <table role=""presentation"" width=""440"" cellspacing=""0"" cellpadding=""0"" style=""max-width:440px;background:#ffffff;border:1px solid #c3cd97;border-radius:12px;"">
      <tr><td style=""padding:28px 32px 8px;font-family:Georgia,'Times New Roman',serif;font-size:24px;font-weight:bold;color:#3c4229;letter-spacing:2px;"">RAIZA</td></tr>
      <tr><td style=""padding:8px 32px 0;font-size:15px;line-height:1.5;"">Hola {nombreSeguro},<br><br>{intro}</td></tr>
      <tr><td align=""center"" style=""padding:22px 32px;"">
        <div style=""display:inline-block;background:#fbf9f0;border:1px solid #c3cd97;border-radius:10px;padding:14px 26px;font-size:32px;font-weight:bold;letter-spacing:10px;color:#3c4229;"">{codigo}</div>
      </td></tr>
      <tr><td style=""padding:0 32px 26px;font-size:13px;line-height:1.5;color:#5b6350;"">
        Vence en 15 minutos. Si no fuiste tú, ignora este mensaje: nadie puede entrar a tu cuenta sin este código.
      </td></tr>
      <tr><td style=""padding:14px 32px;border-top:1px solid #e6ead0;font-size:12px;color:#848c76;"">RAIZA – Escuela de Joyería y Orfebrería</td></tr>
    </table>
  </td></tr></table>
</body></html>";
        }
    }
}
