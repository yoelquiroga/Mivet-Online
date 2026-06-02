using Microsoft.Data.SqlClient;

namespace VeterinariaAPI.Services
{
    public class AutoCancelCitasService : BackgroundService
    {
        private readonly string _connectionString;
        private readonly ILogger<AutoCancelCitasService> _logger;

        public AutoCancelCitasService(IConfiguration configuration, ILogger<AutoCancelCitasService> logger)
        {
            _connectionString = configuration.GetConnectionString("cn") ?? throw new InvalidOperationException("Cadena de conexi�n no encontrada.");
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCancelCitasService iniciado.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(10));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    int rowsAffected;
                    using (var cn = new SqlConnection(_connectionString))
                    {
                        await cn.OpenAsync(stoppingToken);
                        using var cmd = new SqlCommand(
                            "UPDATE cita SET est_cit = 'C' WHERE est_cit = 'P' AND cal_cit < DATEADD(MINUTE, -30, GETDATE())",
                            cn
                        );
                        rowsAffected = await cmd.ExecuteNonQueryAsync(stoppingToken);
                    }

                    if (rowsAffected > 0)
                    {
                        _logger.LogInformation("Auto-cancel: {Count} cita(s) marcada(s) como cancelada(s).", rowsAffected);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en AutoCancelCitasService al ejecutar el UPDATE.");
                }
            }

            _logger.LogInformation("AutoCancelCitasService detenido.");
        }
    }
}
