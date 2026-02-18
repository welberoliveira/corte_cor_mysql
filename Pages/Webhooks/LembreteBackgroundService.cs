using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace CorteCor.Pages.Webhooks
{
    public class LembreteBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LembreteBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Verifica a cada 5 minutos

        public LembreteBackgroundService(IServiceProvider serviceProvider, ILogger<LembreteBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Lembretes iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessarLembretesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar lembretes.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Serviço de Lembretes parado.");
        }

        private async Task ProcessarLembretesAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var lembreteService = scope.ServiceProvider.GetRequiredService<LembreteService>();
                await lembreteService.ProcessarLembretesAsync(stoppingToken);
            }
        }
    }
}
