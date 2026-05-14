using CorteCor.Handlers;
using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Xml.NFe;
using Unimake.Business.DFe.Servicos.NFe;

namespace CorteCor.Services
{
    public class NFCeEmissorService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;
        private readonly NotaFiscalLogHandler _logHandler;

        public NFCeEmissorService(CertificadoFiscalFactory certificadoFactory, NotaFiscalLogHandler logHandler)
        {
            _certificadoFactory = certificadoFactory;
            _logHandler = logHandler;
        }

        public async Task<RetornoEmissaoDto> EmitirNFCeSincronoAsync(SalaoConfigFiscal config, NFe xmlBuilderNfe)
        {
            var certificado = _certificadoFactory.InstanciarCertificado(config);

            // 1. Configurar Parametros Unimake
            var configuracao = new Configuracao
            {
                TipoDFe = TipoDFe.NFCe,
                TipoEmissao = TipoEmissao.Normal,
                CertificadoDigital = certificado
            };

            // 2. Wrap no lote síncrono
            var enviNFe = new EnviNFe
            {
                Versao = "4.00",
                IdLote = DateTime.Now.Ticks.ToString(), // ID unívoco do lote
                IndSinc = SimNao.Sim, // <-- Lote síncrono para NFC-e (comum)
                NFe = new List<NFe> { xmlBuilderNfe }
            };

            // 3. Montar Autorização e Executar
            var autorizacao = new Autorizacao(enviNFe, configuracao);
            
            try 
            {
               await _logHandler.LogarEtapaAsync(config.IdSalao, null, null, "Envio Sefaz", "Iniciando comunicação com Web Service Estadual.");
               autorizacao.Executar();
            }
            catch (Exception ex)
            {
                await _logHandler.LogarEtapaAsync(config.IdSalao, null, null, "Falha Sefaz", "Erro de conexão/timeout com a Sefaz: " + ex.Message);
                return new RetornoEmissaoDto
                {
                    Autorizada = false,
                    Motivo = "Erro ao conectar Sefaz: " + ex.Message
                };
            }
            
            var resultado = autorizacao.Result;
            var retornoProtocolo = resultado.ProtNFe?.InfProt;

            await _logHandler.LogarEtapaAsync(config.IdSalao, null, null, "Resposta Sefaz", $"Lote retornado. Código Sefaz: {retornoProtocolo?.CStat ?? resultado.CStat} - Motivo: {retornoProtocolo?.XMotivo ?? resultado.XMotivo}");

            var dto = new RetornoEmissaoDto
            {
                XmlEnvio = autorizacao.EnviNFe.GerarXML().OuterXml,
                XmlRetorno = resultado.GerarXML().OuterXml,
                Motivo = retornoProtocolo?.XMotivo ?? resultado.XMotivo,
                CodigoStatusSefaz = retornoProtocolo?.CStat ?? resultado.CStat
            };

            // Status 100 = Autorizada / 104 = Lote Processado
            if ((dto.CodigoStatusSefaz == 100 || dto.CodigoStatusSefaz == 104) && retornoProtocolo != null)
            {
                dto.Autorizada = true;
                dto.Protocolo = retornoProtocolo.NProt;
                dto.ChaveAcesso = retornoProtocolo.ChNFe;
            }
            else
            {
                dto.Autorizada = false;
            }

            return dto;
        }
    }
}
