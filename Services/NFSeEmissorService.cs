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
                if (!(xmlBuilderNfse is CorteCor.Models.Ginfes.EnviarLoteRpsEnvio loteRps))
                {
                    throw new ArgumentException("O objeto fornecido não é um modelo válido do Ginfes V3.");
                }

                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Inicialização Ginfes", "Iniciando serialização do modelo Ginfes...");

                // Serializar o objeto para XML String
                var serializer = new XmlSerializer(typeof(CorteCor.Models.Ginfes.EnviarLoteRpsEnvio));
                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true }))
                    {
                        var namespaces = new XmlSerializerNamespaces();
                        namespaces.Add("", "http://www.ginfes.com.br/servico_enviar_lote_rps_envio_v03.xsd");
                        serializer.Serialize(xmlWriter, loteRps, namespaces);
                        xmlEnvio = stringWriter.ToString();
                    }
                }

                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Assinatura do XML", "Montagem do XML concluída, XML Raw disponível", xmlEnvio);

                // Carregar certificado (Supondo que config seja usado no contexto do Certificado)
                var cert = _certificadoFactory.InstanciarCertificado(config);

                // Para prefeituras GINFES normalmente o endereço é https://producao.ginfes.com.br/ServiceGinfesImpl
                string urlGinfes = config.Ambiente == 1 ? "https://producao.ginfes.com.br/ServiceGinfesImpl?wsdl" : "https://homologacao.ginfes.com.br/ServiceGinfesImpl?wsdl";
                
                string envelopeSoap = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://servicos.ginfes.com.br/"">
                                       <soapenv:Header/>
                                       <soapenv:Body>
                                          <ser:RecepcionarLoteRpsV3>
                                             <arg0><![CDATA[{xmlEnvio}]]></arg0>
                                          </ser:RecepcionarLoteRpsV3>
                                       </soapenv:Body>
                                    </soapenv:Envelope>";

                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Conexão Ginfes", $"Enviando Lote SOAP para URL: {urlGinfes}");

                using (var handler = new HttpClientHandler())
                {
                    handler.ClientCertificates.Add(cert);
                    
                    using (var client = new HttpClient(handler))
                    {
                        var content = new StringContent(envelopeSoap, Encoding.UTF8, "text/xml");
                        content.Headers.Add("SOAPAction", "");

                        var response = await client.PostAsync(urlGinfes, content);
                        string soapResponse = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Retorno Ginfes Sucesso", $"Resposta SOAP HTTP 200 recebida", soapResponse);
                            retorno.Autorizada = true;
                            retorno.Motivo = "Lote RPS enviado com sucesso para o servidor Ginfes.";
                            retorno.XmlEnvio = xmlEnvio;
                            retorno.XmlRetorno = soapResponse;
                            
                            // A extração do Protocolo exige parsing do soapResponse buscando a tag <Protocolo> mas o lote ainda precisa ser consultado.
                        }
                        else
                        {
                            retorno.Motivo = $"Falha na comunicação: {(int)response.StatusCode} - {response.ReasonPhrase}";
                            retorno.XmlRetorno = soapResponse;
                            await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Falha Ginfes Rejeição HTTP", retorno.Motivo, soapResponse);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retorno.Autorizada = false;
                retorno.Motivo = "Erro interno fatal ao despachar GINFES: " + ex.Message;
                await _logHandler.LogarEtapaAsync(config.IdSalao, idAgendamento, nfseLogId, "Exceção Interna Emissor Ginfes", ex.Message, xmlEnvio);
            }

            return retorno;
        }
    }
}
