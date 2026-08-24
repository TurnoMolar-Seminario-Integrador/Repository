using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data
{
    /// <summary>
    /// Factory utilizada por las herramientas de EF Core (dotnet-ef migrations) en tiempo de diseño.
    /// Permite generar migraciones sin necesidad de levantar el WebAPI completo.
    /// 
    /// Para generar la migración inicial, ejecutar desde la carpeta /Data:
    ///   dotnet ef migrations add InitialCreate --output-dir Migrations
    /// 
    /// Para actualizar la BD local:
    ///   dotnet ef database update
    /// 
    /// Para cambiar al host remoto (trabajo en equipo), modificar la cadena de conexión
    /// en appsettings.json del proyecto WebAPI.
    /// </summary>
    public class TurnoMolarDbContextFactory : IDesignTimeDbContextFactory<TurnoMolarDbContext>
    {
        public TurnoMolarDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TurnoMolarDbContext>();

            // Cadena de conexión de desarrollo para herramientas de migración
            // Para trabajo en equipo, cambiar a la cadena del host remoto compartido
            optionsBuilder.UseSqlServer(
                "Server=clinicakarina_db.mssql.somee.com;Database=clinicakarina_db;User Id=manufg2006_SQLLogin_1;Password=jgvph71xn9;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;",
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)
            );

            return new TurnoMolarDbContext(optionsBuilder.Options);
        }
    }
}
