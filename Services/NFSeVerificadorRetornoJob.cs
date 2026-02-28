using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Servicos.NFSe;

namespace CorteCor.Services
{
    // Será inicializado pelo ASP.NET Core de tempos em tempos. Exemplo usando BackgroundService.
    public class NFSeVerificadorRetornoJob : BackgroundService
    {
        private readonly ILogger<NFSeVerificadorRetornoJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public NFSeVerificadorRetornoJob(ILogger<NFSeVerificadorRetornoJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarLotesPendentesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no job de verificação de NFS-e.");
                }

                // Aguarda 2 minutos antes de varrer novamente (pode ser configurado)
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        private async Task VerificarLotesPendentesAsync()
        {
            // O BackgroundService é Singleton. Precisamos de um Scope para usar Services transientes/scoped.
            using var scope = _serviceProvider.CreateScope();
            var notaHandler = scope.ServiceProvider.GetRequiredService<NotaFiscalHandler>();
            var configHandler = scope.ServiceProvider.GetRequiredService<SalaoConfigFiscalHandler>();
            var certFactory = scope.ServiceProvider.GetRequiredService<CertificadoFiscalFactory>();
            var logHandler = scope.ServiceProvider.GetRequiredService<NotaFiscalLogHandler>();

            // Buscar notas NFS-e em Processamento
            var notasProcessando = await notaHandler.ObterPorStatusAsync("Processando", "NFS-e");

            if (!notasProcessando.Any()) return;

            foreach (var nota in notasProcessando)
            {
                // Obter a configuração a partir do Salão
                var configSalao = await configHandler.ObterPorSalaoAsync(nota.IdSalao);
                
                if (configSalao == null || configSalao.CertificadoPfx == null) continue;

                var certificado = certFactory.InstanciarCertificado(configSalao);

                // Configuração base da prefeitura
                var cfgParams = new Configuracao
                {
                    TipoDFe = TipoDFe.NFSe,
                    CertificadoDigital = certificado
                };

                try 
                {
                   await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Consulta RPS", $"Iniciou verificação do Lote {nota.NumeroRecibo}.");
                   
                   var consultaLoteRps = new ConsultarLoteRps(nota.NumeroRecibo, cfgParams);
                   consultaLoteRps.Executar();

                   var resposta = consultaLoteRps.Result;
                   
                   // A estrutura do 'Result' varia muito de acordo com o XSD da prefeitura.
                   // Algumas retornam SituacaoLote, outras CStat. Portanto, precisa do Wrapper Municipal Específico.
                   throw new NotImplementedException("O Parsing do retorno depende do objeto deserializado da Prefeitura.");
                }
                catch (Exception ex)
                {
                   await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Falha Consulta", $"Erro ao baixar status RPS: {ex.Message}");
                   _logger.LogError(ex, $"Erro ao verificar lote RPS {nota.NumeroRecibo}");
                }
            }
        }
    }
}
