using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;

namespace CorteCor.Services
{
    public class NFCeEmissorService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;

        public NFCeEmissorService(CertificadoFiscalFactory certificadoFactory)
        {
            _certificadoFactory = certificadoFactory;
        }

        public async Task<RetornoEmissaoDto> EmitirNFCeSincronoAsync(SalaoConfigFiscal config, NFe xmlBuilderNfe)
        {
            /* var certificado = _certificadoFactory.InstanciarCertificado(config);

            // 1. Configurar ParamÃªtros Unimake
            var configuracao = new Configuracao
            {
                TipoDFe = TipoDFe.NFCe,
                TipoEmissao = TipoEmissao.Normal,
                CertificadoDigital = certificado,
                Ambiente = config.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao,
                Estado = (UFBrasil)config.CodigoUFIBGE // Enum UF do Unimake mapeado a partir do IBGE
            };

            // 2. Wrap no lote sÃ­ncrono
            var enviNFe = new EnviNFe
            {
                Versao = "4.00",
                IdLote = DateTime.Now.Ticks.ToString(), // ID unÃ­voco do lote
                IndSinc = SimNao.Sim, // <-- Lote sÃ­ncrono para NFC-e (comum)
                NFe = new List<NFe> { xmlBuilderNfe }
            };

            // 3. Montar AutorizaÃ§Ã£o e Executar
            var autorizacao = new Autorizacao(enviNFe, configuracao);
            autorizacao.Executar();
            
            var resultado = autorizacao.Result;
            var retornoProtocolo = resultado.ProtNFe?.FirstOrDefault()?.InfProt;

            var dto = new RetornoEmissaoDto
            {
                XmlEnvio = autorizacao.EnviNFe.GerarXML().OuterXml,
                XmlRetorno = autorizacao.RetEnviNFe.GerarXML().OuterXml,
                Motivo = retornoProtocolo?.XMotivo ?? resultado.XMotivo,
                CodigoStatusSefaz = retornoProtocolo?.CStat ?? resultado.CStat
            };

            // Status 100 = Autorizada
            if (dto.CodigoStatusSefaz == 100 && retornoProtocolo != null)
            {
                dto.Autorizada = true;
                dto.Protocolo = retornoProtocolo.NProt;
                dto.ChaveAcesso = retornoProtocolo.ChNFe;
            }
            else
            {
                dto.Autorizada = false;
            }

            return dto; */ return new RetornoEmissaoDto();
        }
    }
}
