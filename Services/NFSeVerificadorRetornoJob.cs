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
            /* 
            using var scope = _serviceProvider.CreateScope();
            var notaHandler = scope.ServiceProvider.GetRequiredService<dynamic>();
            var configHandler = scope.ServiceProvider.GetRequiredService<dynamic>();
            var certFactory = scope.ServiceProvider.GetRequiredService<CertificadoFiscalFactory>();

            // Mock de chamada para buscar todas as notas em Processamento.
            // Para produção, criar um método específico em EntityHandler para "ObterPorStatus"
            var todasNotas = await Task.FromResult(new List<NotaFiscal>()); // MOCK - Implementar GetByStatus
            var notasProcessando = todasNotas.Where(n => n.Status == "Processando" && n.TipoNota == "NFS-e").ToList();

            if (!notasProcessando.Any()) return;

            foreach (var nota in notasProcessando)
            {
                // Obter a configuração a partir do Salão
                var configuracoes = await Task.FromResult(new List<SalaoConfigFiscal>()); // MOCK - Implementar GetBySalaoId
                var configSalao = configuracoes.FirstOrDefault(c => c.IdSalao == nota.IdSalao);
                
                if (configSalao == null) continue;

                var certificado = certFactory.InstanciarCertificado(configSalao);

                // Configuração base da prefeitura
                var cfgParams = new Configuracao
                {
                    TipoDFe = TipoDFe.NFSe,
                    CertificadoDigital = certificado,
                    CodigoMunicipio = configSalao.CodigoMunicipioIBGE, 
                    Ambiente = configSalao.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao
                };

                var consultaLoteRps = new ConsultarLoteRps(nota.NumeroRecibo, cfgParams);
                consultaLoteRps.Executar();

                var resposta = consultaLoteRps.Result;
                
                // Padrão ABRASF/Nacional de Retorno
                if (resposta.SituacaoLote == SituacaoLoteNFSe.ProcessadoComSucesso)
                {
                    nota.Status = "Autorizada";
                    nota.ProtocoloAutorizacao = resposta.Protocolo;
                    nota.XmlRetorno = consultaLoteRps.RetornoLoteRps.GerarXML().OuterXml;
                }
                else if (resposta.SituacaoLote == SituacaoLoteNFSe.ProcessadoComErros)
                {
                    nota.Status = "Rejeitada";
                    nota.JustificativaRejeicao = resposta.ListaMensagemRetorno?.FirstOrDefault()?.Mensagem;
                    nota.XmlRetorno = consultaLoteRps.RetornoLoteRps.GerarXML().OuterXml;
                }

                // Salva a alteração
                await notaHandler.UpdateAsync(nota);
            }
            */
        }
    }
}
