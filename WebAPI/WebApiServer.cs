using System.Text;
using Application.Services;
using Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace WebAPI
{
    public static class WebApiServer
    {
        public static WebApplication CreateApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configuración de DbContext (SQL Server 2022 con ConnectionString de appsettings.json)
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? "Server=localhost;Database=ClinicaOdontologicaDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

            builder.Services.AddDbContext<TurnoMolarDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            });

            // Configuración de CORS para permitir consumo desde cualquier cliente WinForms / Web
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Configuración de Autenticación con JWT Bearer
            var key = Encoding.ASCII.GetBytes(AuthService.SecretKey);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = AuthService.Issuer,
                    ValidateAudience = true,
                    ValidAudience = AuthService.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            // Configuración de Swagger con soporte para Authorization Bearer
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TurnoMolar API",
                    Version = "v1",
                    Description = "API REST de Gestión Odontológica y Turnos - Sistema TurnoMolar"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Ejemplo: \"Bearer {token}\"",
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

            // Registro de Repositorios (Data Layer)
            builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
            builder.Services.AddScoped<IOdontologoRepository, OdontologoRepository>();
            builder.Services.AddScoped<IEspecialidadRepository, EspecialidadRepository>();
            builder.Services.AddScoped<IInsumoRepository, InsumoRepository>();
            builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
            builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();
            builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
            builder.Services.AddScoped<IMultaRepository, MultaRepository>();
            builder.Services.AddScoped<IObraSocialRepository, ObraSocialRepository>();
            builder.Services.AddScoped<IHistoriaClinicaRepository, HistoriaClinicaRepository>();

            // Registro de Servicios (Application Layer)
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IPacienteService, PacienteService>();
            builder.Services.AddScoped<IOdontologoService, OdontologoService>();
            builder.Services.AddScoped<IEspecialidadService, EspecialidadService>();
            builder.Services.AddScoped<IInsumoService, InsumoService>();
            builder.Services.AddScoped<ITurnoOdontologicoService, TurnoOdontologicoService>();
            builder.Services.AddScoped<IConsultaService, ConsultaService>();
            builder.Services.AddScoped<IFacturaService, FacturaService>();
            builder.Services.AddScoped<IMultaService, MultaService>();
            builder.Services.AddScoped<IReportesService, ReportesService>();

            var app = builder.Build();

            // Inicializar la base de datos y cargar Seed Data en SQL Server Express
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<TurnoMolarDbContext>();
                    var created = context.Database.EnsureCreated();
                    if (created)
                    {
                        Console.WriteLine("✅ Base de datos 'ClinicaOdontologicaDb' creada y configurada con éxito en SQL Server.");
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ Conexión establecida con la base de datos 'ClinicaOdontologicaDb' existente en SQL Server.");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ [AVISO CONEXIÓN BD]: No se pudo conectar a la instancia de SQL Server local ({ex.Message}).");
                    Console.WriteLine("ℹ️ Verifique que el servicio 'SQL Server (SQLEXPRESS)' esté iniciado en Windows Services.");
                    Console.ResetColor();
                }
            }

            app.UseCors("AllowAll");

            if (app.Environment.IsDevelopment() || true)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TurnoMolar API v1");
                    c.RoutePrefix = "swagger";
                });
                app.MapGet("/", () => Results.Redirect("/swagger"));
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // Mapeo de Endpoints
            app.MapAuthEndpoints();
            app.MapPacienteEndpoints();
            app.MapOdontologoEndpoints();
            app.MapEspecialidadEndpoints();
            app.MapInsumoEndpoints();
            app.MapTurnoOdontologicoEndpoints();
            app.MapConsultaEndpoints();
            app.MapFacturaEndpoints();
            app.MapMultaEndpoints();
            app.MapReportesEndpoints();

            return app;
        }

        public static Task StartAsync(string[] args)
        {
            var app = CreateApp(args);
            return app.RunAsync();
        }
    }
}
