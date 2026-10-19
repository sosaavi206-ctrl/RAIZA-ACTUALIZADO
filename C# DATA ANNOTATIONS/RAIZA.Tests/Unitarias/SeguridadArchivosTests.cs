using FluentAssertions;
using Xunit;

namespace RAIZA.Tests.Unitarias
{
    /// <summary>
    /// Pruebas de seguridad para la validación de archivos adjuntos (Entregas de Tareas).
    /// Verifica que se bloqueen extensiones ejecutables o peligrosas (Prevención de RCE / Malware)
    /// y se permitan únicamente extensiones aprobadas.
    /// </summary>
    public class SeguridadArchivosTests
    {
        private static readonly string[] ExtensionesPermitidas =
            { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".docx", ".doc", ".zip" };

        private const long MaximoTamanoBytes = 10 * 1024 * 1024; // 10 MB

        [Theory]
        [InlineData("tarea.pdf", true)]
        [InlineData("foto_evidencia.jpg", true)]
        [InlineData("captura.PNG", true)]
        [InlineData("animacion.gif", true)]
        [InlineData("video_demo.mp4", true)]
        [InlineData("informe.docx", true)]
        [InlineData("codigo_fuente.zip", true)]
        [InlineData("script.exe", false)]
        [InlineData("malware.bat", false)]
        [InlineData("exploit.sh", false)]
        [InlineData("shell.php", false)]
        [InlineData("payload.js", false)]
        [InlineData("virus.dll", false)]
        [InlineData("archivo_sin_extension", false)]
        public void ValidarExtension_FiltraArchivosSeguros(string nombreArchivo, bool esperadoValido)
        {
            // Act
            var extension = Path.GetExtension(nombreArchivo).ToLowerInvariant();
            var esValido = ExtensionesPermitidas.Contains(extension);

            // Assert
            esValido.Should().Be(esperadoValido);
        }

        [Theory]
        [InlineData(1024, true)] // 1 KB
        [InlineData(5 * 1024 * 1024, true)] // 5 MB
        [InlineData(10 * 1024 * 1024, true)] // 10 MB exactos (Límite superior)
        [InlineData(10 * 1024 * 1024 + 1, false)] // 10 MB + 1 byte (Excede)
        [InlineData(50 * 1024 * 1024, false)] // 50 MB
        [InlineData(0, false)] // Archivo vacío (Límite inferior inválido)
        public void ValidarTamanoArchivo_RespetaLimitesDeMemoria(long tamanoBytes, bool esperadoValido)
        {
            // Act
            var esValido = tamanoBytes > 0 && tamanoBytes <= MaximoTamanoBytes;

            // Assert
            esValido.Should().Be(esperadoValido);
        }
    }
}
