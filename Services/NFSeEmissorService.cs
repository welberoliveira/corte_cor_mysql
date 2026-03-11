using CorteCor.Models;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Utility;
using System;
using System.Threading.Tasks;
using CorteCor.Handlers;
using System.Xml.Serialization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using Unimake.Business.DFe.Xml.NFSe;

namespace CorteCor.Services
{
    public class NFSeEmissorService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;
        private readonly NotaFiscalLogHandler _logHandler;

        public NFSeEmissorService(CertificadoFiscalFactory certificadoFactory, NotaFiscalLogHandler logHandler)
        {
            _certificadoFactory = certificadoFactory;
            _logHandler = logHandler;
        }

        public async Task<RetornoEmissaoDto> EmitirNFSeAsync(SalaoConfigFiscal config, object xmlBuilderNfse, int? idAgendamento)
        {
            var retorno = new RetornoEmissaoDto { Autorizada = false };
            string xmlEnvio = "";
            var nfseLogId = Guid.NewGuid();

            try
            {
                if (!(xmlBuilderNfse is Unimake.Business.DFe.Xml.NFSe.NACIONAL.DPS dpsOrigem))
                {
                    throw new ArgumentException("O objeto fornecido não é um modelo válido de DPS (Padrão Nacional).");
                }

                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Inicialização Padrão Nacional", "Injetando modelo DPS no motor Unimake Nacional...");

                // 1. Extrair XmlDocument do objeto nativo Unimake
                var xmlDoc = dpsOrigem.GerarXML();
                xmlEnvio = xmlDoc.OuterXml;

                var cert = _certificadoFactory.InstanciarCertificado(config);

                if (!cert.HasPrivateKey)
                {
                    throw new Exception("O certificado digital instanciado não possui chave privada. A assinatura do XML Nacional falhará.");
                }

                var configuracao = new Unimake.Business.DFe.Servicos.Configuracao
                {
                    TipoDFe = Unimake.Business.DFe.Servicos.TipoDFe.NFSe,
                    TipoEmissao = Unimake.Business.DFe.Servicos.TipoEmissao.Normal,
                    CertificadoDigital = cert,
                    CodigoMunicipio = 1001058, // IBGE reservado/mapeado internamente pela Unimake para forçar a rota Nacional sem sobrepor
                    TipoAmbiente = config.Ambiente == 1 ? Unimake.Business.DFe.Servicos.TipoAmbiente.Producao : Unimake.Business.DFe.Servicos.TipoAmbiente.Homologacao,
                    PadraoNFSe = Unimake.Business.DFe.Servicos.PadraoNFSe.NACIONAL,
                    Servico = Unimake.Business.DFe.Servicos.Servico.NFSeGerarNfse,
                    SchemaVersao = "1.01" // Obrigatório declarar a string, senão XMLUtility não extrai do root natural (DPS).
                };

                var servicoEmissao = new Unimake.Business.DFe.Servicos.NFSe.GerarNfse(xmlDoc, configuracao);

                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Conexão SOAP Iniciada", "Autenticação SSL e Assinatura Ocorrendo. Postando na PGN...");

                servicoEmissao.Executar();

                var xmlStringRetorno = servicoEmissao.RetornoWSString ?? "";
                
                // LOG BRUTO DO RETORNO para diagnóstico
                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, 
                    "Retorno Bruto Sefaz (RetornoWSString)", 
                    $"Tamanho: {xmlStringRetorno.Length} chars | Primeiros 500: {(xmlStringRetorno.Length > 500 ? xmlStringRetorno.Substring(0, 500) : xmlStringRetorno)}", 
                    xmlStringRetorno);

                // Tentar parse como XML
                var retDoc = new XmlDocument();
                bool xmlValido = false;
                try { retDoc.LoadXml(xmlStringRetorno); xmlValido = true; } catch { }

                if (xmlValido)
                {
                    // Buscar tags de SUCESSO (case insensitive scan via GetElementsByTagName)
                    var sucessoNode = retDoc.GetElementsByTagName("chNFSe");
                    var nfseNode = retDoc.GetElementsByTagName("nNFSe");
                    // Alternativas do Padrão Nacional
                    if (sucessoNode.Count == 0) sucessoNode = retDoc.GetElementsByTagName("ChNFSe");
                    if (nfseNode.Count == 0) nfseNode = retDoc.GetElementsByTagName("NNFSe");

                    if (sucessoNode.Count > 0 || nfseNode.Count > 0)
                    {
                        string chave = sucessoNode.Count > 0 ? sucessoNode[0].InnerText : "-";
                        string nfseNum = nfseNode.Count > 0 ? nfseNode[0].InnerText : "-";
                        
                        retorno.Autorizada = true;
                        retorno.Motivo = "NFS-e Nacional emitido com sucesso na PGN.";
                        retorno.Protocolo = $"CHAVE: {chave} | NUM: {nfseNum}";
                        retorno.XmlEnvio = xmlEnvio;
                        retorno.XmlRetorno = xmlStringRetorno;
                        await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Retorno PGN Sucesso", $"Emissão Efetuada. {retorno.Protocolo}", xmlStringRetorno);
                    }
                    else
                    {
                        // Buscar tags de ERRO em múltiplos formatos conhecidos do Padrão Nacional
                        string erros = "";
                        
                        // Formato 1: <descricao> (serpro minúsculo)
                        var erroDescricoes = retDoc.GetElementsByTagName("descricao");
                        foreach (XmlNode desc in erroDescricoes)
                            erros += $"[descricao] {desc.InnerText} ";

                        // Formato 2: <Descricao> (maiúsculo)
                        var erroDescricoes2 = retDoc.GetElementsByTagName("Descricao");
                        foreach (XmlNode desc in erroDescricoes2)
                            erros += $"[Descricao] {desc.InnerText} ";

                        // Formato 3: <Mensagem>
                        var mensagensGerais = retDoc.GetElementsByTagName("Mensagem");
                        foreach (XmlNode desc in mensagensGerais)
                            erros += $"[Mensagem] {desc.InnerText} ";

                        // Formato 4: <mensagem> (minúsculo)
                        var mensagensMin = retDoc.GetElementsByTagName("mensagem");
                        foreach (XmlNode desc in mensagensMin)
                            erros += $"[mensagem] {desc.InnerText} ";

                        // Formato 5: <MensagemRetorno> / <Codigo> / <Correcao>
                        var msgRetorno = retDoc.GetElementsByTagName("MensagemRetorno");
                        foreach (XmlNode desc in msgRetorno)
                            erros += $"[MensagemRetorno] {desc.InnerText} ";

                        var codigos = retDoc.GetElementsByTagName("Codigo");
                        foreach (XmlNode desc in codigos)
                            erros += $"[Codigo] {desc.InnerText} ";

                        var codigo2 = retDoc.GetElementsByTagName("codigo");
                        foreach (XmlNode desc in codigo2)
                            erros += $"[codigo] {desc.InnerText} ";

                        var correcao = retDoc.GetElementsByTagName("Correcao");
                        foreach (XmlNode desc in correcao)
                            erros += $"[Correcao] {desc.InnerText} ";

                        // Formato 6: <xMotivo> ou <cStat> (herdado NF-e)
                        var xMotivo = retDoc.GetElementsByTagName("xMotivo");
                        foreach (XmlNode desc in xMotivo)
                            erros += $"[xMotivo] {desc.InnerText} ";

                        var cStat = retDoc.GetElementsByTagName("cStat");
                        foreach (XmlNode desc in cStat)
                            erros += $"[cStat] {desc.InnerText} ";

                        if (string.IsNullOrEmpty(erros))
                        {
                            // Último recurso: logar o XML inteiro cortado para que o usuário veja na tela
                            string xmlCortado = xmlStringRetorno.Length > 800 ? xmlStringRetorno.Substring(0, 800) + "..." : xmlStringRetorno;
                            erros = $"Resposta não reconhecida do Sefaz. Conteúdo bruto: {xmlCortado}";
                        }

                        retorno.Motivo = $"NFS-e Rejeitada: {erros}";
                        retorno.XmlRetorno = xmlStringRetorno;
                        await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Falha PGN Rejeição", retorno.Motivo, xmlStringRetorno);
                    }
                }
                else
                {
                    // Retorno não é XML válido — pode ser JSON ou texto puro da API REST
                    string conteudo = xmlStringRetorno.Length > 800 ? xmlStringRetorno.Substring(0, 800) + "..." : xmlStringRetorno;
                    retorno.Motivo = $"NFS-e Rejeitada (resposta não-XML): {conteudo}";
                    retorno.XmlRetorno = xmlStringRetorno;
                    await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Falha PGN (Não-XML)", retorno.Motivo, xmlStringRetorno);
                }
            }
            catch (Exception ex)
            {
                string fullError = ex.Message + " | Stack: " + ex.StackTrace;
                Exception inner = ex.InnerException;
                while (inner != null)
                {
                    fullError += " | Inner: " + inner.Message + " | InnerStack: " + inner.StackTrace;
                    inner = inner.InnerException;
                }

                retorno.Autorizada = false;
                retorno.Motivo = "Erro interno fatal ao despachar NFS-e Nacional: " + fullError;
                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Exceção Interna Emissor PGN (Unimake)", fullError, xmlEnvio);
            }

            return retorno;
        }
    }
}
