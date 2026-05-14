using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;
using CorteCor.Logs;
using CorteCor.Models;
using System.Text.Json.Serialization;

namespace CorteCor.Services
{
    public class SefinParametrosMunicipioResponse
    {
        // Resposta da API Sefin para parâmetros do município costuma usar
        // campos indicando uso do modelo nacional ou exigência de tabela PRÓPRIA (local).
        [JsonPropertyName("exigeCodigoTributacaoMunicipal")]
        public bool? ExigeCodigoTributacaoMunicipal { get; set; }

        // Variantes comuns na API REST Sefin
        [JsonPropertyName("tabelaTributacaoPropria")]
        public bool? TabelaTributacaoPropria { get; set; }
        
        [JsonPropertyName("dispensaCTribMun")]
        public bool? DispensaCTribMun { get; set; }
    }

    public class ValidaParametrosMunicipioService : IValidaParametrosMunicipioService
    {
        private readonly CertificadoFiscalFactory _certificadoFactory;
        private readonly Log _logger = new Log();

        public ValidaParametrosMunicipioService(CertificadoFiscalFactory certificadoFactory)
        {
            _certificadoFactory = certificadoFactory;
        }

        public async Task ValidateAsync(SalaoConfigFiscal config, CorteCor.Models.Servico servico)
        {
            try
            {
                var ibge = config.CodigoMunicipioIBGE;
                var cert = _certificadoFactory.InstanciarCertificado(config);

                if (!cert.HasPrivateKey)
                {
                    throw new Exception("O certificado instanciado não possui chave privada para a consulta na Sefin Nacional.");
                }

                using var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                };
                handler.ClientCertificates.Add(cert);

                using var client = new HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(10);

                // Endpoint Oficial para consulta de parâmetros de um município no Padrão Nacional
                // Pode variar entre instâncias/ambientes (Produção / Homologação).
                // Tentando o endpoint mais comum.
                var ambienteUrl = config.Ambiente == 1 ? "sefin.nfse.gov.br" : "homologacao.sefin.nfse.gov.br";
                var url = $"https://{ambienteUrl}/SefinNacional/api/v1/municipio/{ibge}/parametros";

                var response = await client.GetAsync(url);
                string json = null;

                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsStringAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Se der 404, assume-se que o município não configurou parâmetros locais específicos ou o fallback opera 
                    // para o modelo unificado nacional (Dispensa CTM = verdadeiro).
                    json = "{\"dispensaCTribMun\": true, \"tabelaTributacaoPropria\": false}";
                }
                else
                {
                    // Em caso de falha de conexão (403, 500) ou bloqueio, assumimos a exigência para proteção.
                    _logger.Write($"Falha ao consultar parâmetros município {ibge}. Status: {response.StatusCode}. Assumindo exigência por fallback de segurança.");
                    json = "{\"exigeCodigoTributacaoMunicipal\": true}";
                }

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        var parametros = JsonSerializer.Deserialize<SefinParametrosMunicipioResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        bool exigeLocal = (parametros?.ExigeCodigoTributacaoMunicipal == true) 
                                          || (parametros?.TabelaTributacaoPropria == true)
                                          || (parametros?.DispensaCTribMun == false);

                        if (exigeLocal)
                        {
                            if (string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) || servico.CodigoTributacaoMunicipio == "000")
                            {
                                throw new Exception($"Erro de Validação (Sefin Padrão Nacional): O município de incidência (IBGE: {ibge}) possui tabela tributária própria e " +
                                                    "EXIGE o preenchimento do Código de Tributação Municipal (cTribMun). " +
                                                    "Por favor, configure o código tributário municipal no cadastro deste serviço para evitar rejeição E0314.");
                            }
                        }
                        else
                        {
                            // Município utiliza a tabela nacional unificada, dispensando o código local.
                            // Vamos limpar/omitir a tag cTribMun marcando um código especial que o FiscalBuilderService irá anular
                            servico.CodigoTributacaoMunicipio = null; 
                        }
                    }
                    catch (JsonException)
                    {
                        _logger.Write($"Falha ao fazer parse do JSON de parâmetros Sefin: {json}");
                    }
                }
            }
            catch (Exception ex) when (!ex.Message.Contains("Erro de Validação"))
            {
                // Erros de rede ou criptografia não devem paralisar com throw fatal se a intenção for tentar emitir de qualquer jeito e ver o que a Sefin diz (graceful degradation)
                _logger.WriteException(ex);
            }
        }
    }
}
