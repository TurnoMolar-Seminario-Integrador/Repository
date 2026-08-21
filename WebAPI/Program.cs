using Application.Services;
using Data;
using WebAPI;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddScoped<IPacienteRepository, PacienteRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();


builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<ITurnoOdontologicoService, TurnoOdontologicoService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


app.MapPacienteEndpoints();
app.MapTurnoOdontologicoEndpoints();
app.Run();