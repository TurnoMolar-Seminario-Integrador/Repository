using Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TurnoMolar.Tests.Infraestructura
{
    // Base SQLite en memoria con el esquema del DbContext y el seed del DbInitializer.
    // Cada test crea la suya, así no comparten estado. SQLite (y no el proveedor InMemory)
    // hace cumplir las claves foráneas y los índices UNIQUE del MDF.
    public sealed class BaseDeDatosDePrueba : IDisposable
    {
        private readonly SqliteConnection _conexion;

        public BaseDeDatosDePrueba(bool conSeed = true)
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();
            using var contexto = NuevoContexto();
            if (conSeed)
                DbInitializer.InitializeAsync(contexto).GetAwaiter().GetResult();
            else
                contexto.Database.EnsureCreated();
        }

        // Un contexto nuevo por operación, como en la aplicación (scoped por request).
        public TurnoMolarDbContext NuevoContexto()
        {
            var opciones = new DbContextOptionsBuilder<TurnoMolarDbContext>()
                .UseSqlite(_conexion)
                .Options;
            return new TurnoMolarDbContext(opciones);
        }

        public void Dispose() => _conexion.Dispose();
    }
}
