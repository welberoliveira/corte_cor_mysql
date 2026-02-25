using CorteCor.Models;
using System;
using System.Threading.Tasks;

namespace CorteCor.Services
{
    public class FiscalActionService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;

        public FiscalActionService(CertificadoFiscalFactory certificadoFactory)
        {
            _certificadoFactory = certificadoFactory;
        }

        public async Task<NotaFiscalEvento> CancelarNfceAsync(SalaoConfigFiscal config, string chaveAcesso, string justificativa, string protocoloAutorizacao)
        {
            // Placeholder: Enviar o evento de cancelamento da NFC-e via Unimake.DFe

            var evento = new NotaFiscalEvento
            {
                TipoEvento = "Cancelamento",
                Justificativa = justificativa,
                Status = "Rejeitado - Funcionalidade Simulada",
                XmlEnvio = "<xml>Envio Cancelamento Mock</xml>",
                XmlRetorno = "<xml>Retorno Cancelamento Mock</xml>"
            };

            await Task.CompletedTask;
            return evento;
        }

        public async Task<NotaFiscalEvento> CancelarNfseAsync(SalaoConfigFiscal config, string rps, string justificativa)
        {
            // Placeholder: Cancelar NFS-e (Regras dependem do município / Padrão)

            var evento = new NotaFiscalEvento
            {
                TipoEvento = "Cancelamento NFS-e",
                Justificativa = justificativa,
                Status = "Rejeitado - Funcionalidade Simulada"
            };

            await Task.CompletedTask;
            return evento;
        }

        public async Task<NotaFiscalInutilizacao> InutilizarNfceAsync(SalaoConfigFiscal config, int ano, int serie, int numeroInicial, int numeroFinal, string justificativa)
        {
            // Placeholder: Enviar pedido de inutilização de faixa via Unimake.DFe

            var inutilizacao = new NotaFiscalInutilizacao
            {
                Ano = ano,
                Serie = serie,
                NumeroInicial = numeroInicial,
                NumeroFinal = numeroFinal,
                Justificativa = justificativa,
                Status = "Homologado - Simulação",
                Modelo = 65,
                Protocolo = "1234567890"
            };

            await Task.CompletedTask;
            return inutilizacao;
        }
    }
}
