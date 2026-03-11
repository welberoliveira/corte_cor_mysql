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
                   await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Consulta Nacional", $"Iniciou verificação do Lote/Processamento.");
                   
                   // Usando a Consulta Genérica do Padrão Nacional se disponível, ou Consulta via Chave/DPS
                   var pedSitNFSe = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.NFSe
                   {
                        Versao = "1.00",
                        InfNFSe = new Unimake.Business.DFe.Xml.NFSe.NACIONAL.Consulta.InfNFSe
                        {
                            Id = nota.ChaveAcessoNacional // O Id de consulta na ABRASF Nacional pode ser a chave
                        }
                   };

                   cfgParams.TipoDFe = TipoDFe.NFSe;
                   cfgParams.PadraoNFSe = PadraoNFSe.NACIONAL;
                   cfgParams.Servico = Unimake.Business.DFe.Servicos.Servico.NFSeConsultarNfse;
                   cfgParams.TipoAmbiente = configSalao.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;

                   var consultarNfse = new Unimake.Business.DFe.Servicos.NFSe.ConsultarNfse(pedSitNFSe.GerarXML(), cfgParams);
                   consultarNfse.Executar();

                   var resposta = consultarNfse.RetornoWSString;

                   if (resposta.Contains("chNFSe"))
                   {
                        nota.Status = "Autorizada";
                        nota.XmlRetorno = resposta;
                        await notaHandler.UpdateAsync(nota);
                        await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Retorno Nacional", "Nota Autorizada após processamento.", resposta);
                   }
                   else if (resposta.Contains("rejeicao") || resposta.Contains("erro"))
                   {
                        nota.Status = "Rejeitada";
                        nota.XmlRetorno = resposta;
                        await notaHandler.UpdateAsync(nota);
                        await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Rejeição Nacional", "Nota Rejeitada após processamento.", resposta);
                   }
                }
                catch (Exception ex)
                {
                   await logHandler.LogarEtapaAsync(nota.IdSalao, nota.IdAgendamento, nota.IdNotaFiscal, "Falha Consulta Nacional", $"Erro ao baixar status: {ex.Message}");
                   _logger.LogError(ex, $"Erro ao verificar nota.");
                }
            }
        }
    }
}
