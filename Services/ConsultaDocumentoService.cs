using System.Text.Json;
using System.Text.Json.Serialization;
using CorteCor.Logs;

namespace CorteCor.Services
{
    // DTO para resultado da consulta CNPJ (ReceitaWS)
    public class CnpjResultDto
    {
        [JsonPropertyName("nome")]
        public string RazaoSocial { get; set; }

        [JsonPropertyName("fantasia")]
        public string NomeFantasia { get; set; }

        [JsonPropertyName("atividade_principal")]
        public List<AtividadePrincipal> AtividadePrincipal { get; set; }

        [JsonPropertyName("situacao")]
        public string Situacao { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("telefone")]
        public string Telefone { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("abertura")]
        public string Abertura { get; set; } // Formato: dd/MM/yyyy

        // Campos de Endereço
        [JsonPropertyName("logradouro")]
        public string Logradouro { get; set; }

        [JsonPropertyName("numero")]
        public string Numero { get; set; }

        [JsonPropertyName("complemento")]
        public string Complemento { get; set; }

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; }

        [JsonPropertyName("cep")]
        public string Cep { get; set; }

        [JsonPropertyName("municipio")]
        public string Municipio { get; set; }

        [JsonPropertyName("uf")]
        public string UF { get; set; }

        // Campo calculado
        public string Cnae => AtividadePrincipal?.FirstOrDefault()?.Code ?? "";
    }

    public class AtividadePrincipal
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    // DTO para resultado da consulta CEP (ViaCEP)
    public class CepResultDto
    {
        [JsonPropertyName("logradouro")]
        public string Logradouro { get; set; }

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; }

        [JsonPropertyName("localidade")]
        public string Cidade { get; set; }

        [JsonPropertyName("uf")]
        public string UF { get; set; }

        [JsonPropertyName("erro")]
        public bool? Erro { get; set; }
    }

    public class ConsultaDocumentoService
    {
        private readonly HttpClient _httpClient;
        private readonly Log _logger = new Log();

        public ConsultaDocumentoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        /// <summary>
        /// Consulta dados de um CNPJ na ReceitaWS (API gratuita).
        /// </summary>
        public async Task<CnpjResultDto> ConsultarCnpjAsync(string cnpj)
        {
            try
            {
                // Limpar caracteres não numéricos
                cnpj = new string(cnpj.Where(char.IsDigit).ToArray());

                if (cnpj.Length != 14)
                    return null;

                var url = $"https://receitaws.com.br/v1/cnpj/{cnpj}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Write($"ConsultarCnpjAsync: HTTP {(int)response.StatusCode} para CNPJ {cnpj}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<CnpjResultDto>(json);

                // A ReceitaWS retorna status "ERROR" quando o CNPJ não é encontrado
                if (resultado?.Status == "ERROR")
                {
                    _logger.Write($"ConsultarCnpjAsync: CNPJ {cnpj} não encontrado na ReceitaWS.");
                    return null;
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                return null;
            }
        }

        /// <summary>
        /// Consulta dados de um CEP no ViaCEP (API gratuita).
        /// </summary>
        public async Task<CepResultDto> ConsultarCepAsync(string cep)
        {
            try
            {
                // Limpar caracteres não numéricos
                cep = new string(cep.Where(char.IsDigit).ToArray());

                if (cep.Length != 8)
                    return null;

                var url = $"https://viacep.com.br/ws/{cep}/json/";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Write($"ConsultarCepAsync: HTTP {(int)response.StatusCode} para CEP {cep}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<CepResultDto>(json);

                // ViaCEP retorna { "erro": true } quando CEP não existe
                if (resultado?.Erro == true)
                {
                    _logger.Write($"ConsultarCepAsync: CEP {cep} não encontrado no ViaCEP.");
                    return null;
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
                return null;
            }
        }
    }
}
