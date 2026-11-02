using Application.Services;

namespace Frontend.MVC.Services
{
    // Ejecuta el cierre de jornada (IVencimientoTurnosService) al iniciar la aplicación y después
    // cada hora: así un turno "Reservado" de un día anterior pasa a "Ausente" aunque el servidor
    // haya estado apagado a medianoche. El proceso es idempotente (solo toma turnos RESERVADO).
    public class CierreJornadaHostedService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CierreJornadaHostedService> _logger;

        public CierreJornadaHostedService(IServiceScopeFactory scopeFactory, ILogger<CierreJornadaHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Espera breve para que DbInitializer termine de crear la base al arrancar.
            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var servicio = scope.ServiceProvider.GetRequiredService<IVencimientoTurnosService>();
                    var procesados = await servicio.ProcesarTurnosVencidosAsync(DateTime.Now);
                    if (procesados > 0)
                        _logger.LogInformation("Cierre de jornada: {Cantidad} turno(s) vencido(s) pasaron a Ausente.", procesados);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el cierre de jornada de turnos vencidos.");
                }

                try { await Task.Delay(Intervalo, stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
