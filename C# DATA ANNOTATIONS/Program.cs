using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RAIZA.Data;
using RAIZA.Interfaces;
using RAIZA.Repositories;
using RAIZA.Services;

public partial class Program
{
    private const string PoliticaCors = "Frontend";

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = builder.Configuration;

        // Acepta ambos nombres para no romper configuraciones anteriores.
        var cadenaConexion = config.GetConnectionString("CadenaConexion")
                             ?? config.GetConnectionString("SQLConnectionStrings");

        var advertencias = ValidarConfiguracion(config, cadenaConexion);

        // ---------------- Servicios ----------------
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "RAIZA API", Version = "v1" });

            // Agrega el botón verde "Authorize" para autenticación JWT Bearer
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Autenticación JWT usando esquema Bearer. Escribe: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // Repositorios
        builder.Services.AddScoped<IUsuarioI, Usuario_R>();
        builder.Services.AddScoped<ITematicaI, Tematica_R>();
        builder.Services.AddScoped<ITarea_I, Tarea_R>();
        builder.Services.AddScoped<IProgresoLeccionI, ProgresoLeccion_R>();
        builder.Services.AddScoped<IProgresoI, Progreso_R>();
        builder.Services.AddScoped<IPedidoKit_I, PedidoKit_R>();
        builder.Services.AddScoped<INotificacionI, Notificacion_R>();
        builder.Services.AddScoped<IModuloI, Modulo_R>();
        builder.Services.AddScoped<ILeccionI, Leccion_R>();
        builder.Services.AddScoped<IInstructor_I, Instructor_R>();
        builder.Services.AddScoped<IEstudiante_I, Estudiante_R>();
        builder.Services.AddScoped<IEntregaTareaI, EntregaTarea_R>();
        builder.Services.AddScoped<CompraI, Compra_R>();
        builder.Services.AddScoped<IClassKitI, ClassKit_R>();
        builder.Services.AddScoped<IClasesEnVivoI, ClasesEnVivo_R>();
        builder.Services.AddScoped<ClaseParticipanteI, ClaseParticipante_R>();
        builder.Services.AddScoped<CertificadoI, Certificado_R>();
        builder.Services.AddScoped<AdministradorI, Administrador_R>();

        // Envío de correos (códigos de verificación y recuperación de contraseña)
        builder.Services.AddScoped<ICorreoService, CorreoService>();

        // Base de datos (reintentos automáticos ante fallos transitorios de Azure SQL)
        builder.Services.AddDbContext<DatabaseService>(options =>
            options.UseSqlServer(cadenaConexion, sql => sql.EnableRetryOnFailure()));

        // Autenticación JWT
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
                };
            });

        // Por defecto toda la API exige token; AuthController se marca con [AllowAnonymous].
        // NOTA: se aplica con MapControllers().RequireAuthorization() en lugar de FallbackPolicy
        // para que los archivos estáticos (wwwroot/entregas) NO exijan token.
        builder.Services.AddAuthorization();

        // CORS: abierto solo en desarrollo; en producción solo los orígenes de "Cors:AllowedOrigins".
        var origenesPermitidos = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(PoliticaCors, policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        // ---------------- Pipeline ----------------
        var app = builder.Build();

        foreach (var advertencia in advertencias)
        {
            app.Logger.LogWarning("Configuración: {Advertencia}", advertencia);
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors(PoliticaCors);

        app.UseAuthentication();
        app.UseAuthorization();

        // Todos los endpoints de la API requieren autenticación (equivalente a la
        // antigua FallbackPolicy), pero los archivos estáticos siguen siendo públicos.
        app.MapControllers().RequireAuthorization();

        // ==================================================================
        // MIGRACIÓN ÚNICA: convierte a BCrypt las contraseñas que aún están en texto plano.
        // 1) Haz una copia de seguridad de la BD.  2) Ejecuta la API una vez.
        // 3) Revisa en el log "Contraseñas migradas a BCrypt: N".  4) BORRA este bloque.
        // (Es seguro ejecutarlo más de una vez: solo toca filas que todavía no son hash.)
        // ==================================================================
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var pendientes = db.Usuario.AsEnumerable()
                .Where(u => !string.IsNullOrEmpty(u.ContrasenaHash)
                            && !(u.ContrasenaHash.Length == 60 && u.ContrasenaHash.StartsWith("$2")))
                .ToList();

            foreach (var u in pendientes)
            {
                u.ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(u.ContrasenaHash);
            }

            if (pendientes.Count > 0) db.SaveChanges();
            app.Logger.LogWarning("Contraseñas migradas a BCrypt: {N}", pendientes.Count);
        }

        app.Run();
    }

    /// <summary>
    /// Detiene el arranque con un mensaje claro si falta configuración crítica.
    /// Devuelve advertencias para valores no críticos (por ejemplo, correo sin configurar).
    /// </summary>
    private static List<string> ValidarConfiguracion(IConfiguration config, string? cadenaConexion)
    {
        var faltantes = new List<string>();

        if (string.IsNullOrWhiteSpace(cadenaConexion))
        {
            faltantes.Add("ConnectionStrings:CadenaConexion");
        }

        foreach (var clave in new[] { "Jwt:Key", "Jwt:Issuer", "Jwt:Audience", "Recaptcha:SecretKey" })
        {
            if (string.IsNullOrWhiteSpace(config[clave]))
            {
                faltantes.Add(clave);
            }
        }

        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException(
                "Faltan valores de configuración: " + string.Join(", ", faltantes));
        }

        if (cadenaConexion!.Contains("TU_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "La cadena de conexión todavía tiene valores de ejemplo (TU_SERVIDOR, TU_USUARIO o TU_PASSWORD). " +
                "Reemplázalos por los datos reales de Azure SQL.");
        }

        if (Encoding.UTF8.GetByteCount(config["Jwt:Key"]!) < 32)
        {
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");
        }

        var advertencias = new List<string>();

        if (string.IsNullOrWhiteSpace(config["Correo:Host"]) || string.IsNullOrWhiteSpace(config["Correo:Clave"]))
        {
            advertencias.Add("Correo:Host/Correo:Clave sin configurar: los códigos de verificación no se enviarán por correo.");
        }

        return advertencias;
    }
}