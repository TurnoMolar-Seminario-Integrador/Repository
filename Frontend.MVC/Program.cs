using Application.Services;
using Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TurnoMolarDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
});

// 2. Inyección de Repositorios y Servicios de Aplicación
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// CUU01 - Agendar Turno Odontológico: TurnoController consume esta misma lógica de
// negocio (compartida en Application.Services) en vez de manipular el DbContext
// directamente, con el mismo criterio ya aplicado al login (ver AuthService).
builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IOdontologoRepository, OdontologoRepository>();
builder.Services.AddScoped<IEspecialidadRepository, EspecialidadRepository>();
builder.Services.AddScoped<IObraSocialRepository, ObraSocialRepository>();
builder.Services.AddScoped<IComprobanteTurnoRepository, ComprobanteTurnoRepository>();
builder.Services.AddScoped<IAgendaTurnoService, AgendaTurnoService>();

// CUU02 - Gestionar Asistencia a Turno Odontológico: OdontologoController (pantalla Control
// de Asistencias) consume esta lógica de negocio, con el mismo criterio que CUU01.
builder.Services.AddScoped<IAsistenciaTurnoService, AsistenciaTurnoService>();

// CUU03 - Finalizar Atención Odontológica: IInsumoRepository e IHistoriaClinicaRepository ya
// existían en Data/ pero no estaban registrados (nada los usaba todavía). IAtencionOdontologicaRepository
// e IPagoRepository son nuevos. OdontologoController (pantalla Agenda de Hoy / TurnosDelDia)
// consume IFinalizarAtencionService con el mismo criterio que CUU01 y CUU02.
builder.Services.AddScoped<IInsumoRepository, InsumoRepository>();
builder.Services.AddScoped<IHistoriaClinicaRepository, HistoriaClinicaRepository>();
builder.Services.AddScoped<IAtencionOdontologicaRepository, AtencionOdontologicaRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IFinalizarAtencionService, FinalizarAtencionService>();

// 3. Autenticación por Cookies (EC03 - Login)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// 4. Controladores y Vistas MVC
builder.Services.AddControllersWithViews();

// 5. Cliente HTTP para llamadas a la API
builder.Services.AddHttpClient();

var app = builder.Build();

// Inicialización de la Base de Datos y Seeders (Pacientes, Odontólogos, Obras Sociales, Usuarios)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TurnoMolarDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbInitializer.InitializeAsync(dbContext, logger);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos.");
    }
}

// Configurar Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".avif"] = "image/avif";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.UseRouting();

// Middleware de Autenticación y Autorización (Crucial para EC03 - Login)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();
