using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.Extensions.Logging;

namespace CorteCor.Services
{
    public class WhatsappService : IWhatsappService
    {
        private readonly HttpClient _httpClient;
        private readonly FornecedoresHandler _fornecedoresHandler;
        private readonly ILogger<WhatsappService> _logger;

        public WhatsappService(
            HttpClient httpClient,
            FornecedoresHandler fornecedoresHandler,
            ILogger<WhatsappService> logger)
        {
            _httpClient = httpClient;
            _fornecedoresHandler = fornecedoresHandler;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> EnviarMensagemAsync(string telefone, string mensagem)
        {
            var fornecedor = _fornecedoresHandler.ObterWhatsappAtivo();
            if (fornecedor == null)
            {
                return (false, "Nenhum fornecedor de WhatsApp ativo foi configurado.");
            }

            if (string.IsNullOrWhiteSpace(telefone))
            {
                return (false, "Informe um telefone para o envio por WhatsApp.");
            }

            if (string.IsNullOrWhiteSpace(mensagem))
            {
                return (false, "Informe a mensagem para o envio por WhatsApp.");
            }

            var nomeFornecedor = fornecedor.Nome?.Trim() ?? string.Empty;
            if (nomeFornecedor.Contains("Evolution", StringComparison.OrdinalIgnoreCase))
            {
                return await EnviarViaEvolutionAsync(fornecedor, telefone, mensagem);
            }

            if (nomeFornecedor.Contains("Z-API", StringComparison.OrdinalIgnoreCase) ||
                nomeFornecedor.Contains("ZAPI", StringComparison.OrdinalIgnoreCase))
            {
                return await EnviarViaZApiAsync(fornecedor, telefone, mensagem);
            }

                return (false, $"Fornecedor de WhatsApp '{fornecedor.Nome}' ainda não suportado para envio automático.");
        }

        private async Task<(bool Success, string? ErrorMessage)> EnviarViaEvolutionAsync(FornecedorWhatsapp fornecedor, string telefone, string mensagem)
        {
            var endpoint = MontarEndpointEvolution(fornecedor);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return (false, "Configure o endpoint e o InstanceId do fornecedor Evolution API.");
            }

            var apiKey = PrimeiroPreenchido(fornecedor.ApiKey, fornecedor.Token, fornecedor.ApiSecret);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "Configure a ApiKey do fornecedor Evolution API.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("apikey", apiKey);

            var payload = new
            {
                number = NormalizarTelefone(telefone),
                textMessage = new
                {
                    text = mensagem
                }
            };

            request.Content = CriarConteudoJson(payload);
            return await EnviarAsync(request, "Evolution API");
        }

        private async Task<(bool Success, string? ErrorMessage)> EnviarViaZApiAsync(FornecedorWhatsapp fornecedor, string telefone, string mensagem)
        {
            var endpoint = MontarEndpointZApi(fornecedor);
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return (false, "Configure o endpoint, o InstanceId e o Token do fornecedor Z-API.");
            }

            var clientToken = PrimeiroPreenchido(fornecedor.ApiKey, fornecedor.Token, fornecedor.ApiSecret);
            if (string.IsNullOrWhiteSpace(clientToken))
            {
                return (false, "Configure o client-token do fornecedor Z-API.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("client-token", clientToken);

            var payload = new
            {
                phone = NormalizarTelefone(telefone),
                message = mensagem
            };

            request.Content = CriarConteudoJson(payload);
            return await EnviarAsync(request, "Z-API");
        }

        private async Task<(bool Success, string? ErrorMessage)> EnviarAsync(HttpRequestMessage request, string provedor)
        {
            try
            {
                using var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var mensagemErro = string.IsNullOrWhiteSpace(body)
                        ? $"Falha no envio via {provedor}: {(int)response.StatusCode} {response.ReasonPhrase}"
                        : body;
                    return (false, mensagemErro);
                }

                if (BodyIndicaErro(body))
                {
                    return (false, string.IsNullOrWhiteSpace(body) ? $"Falha no envio via {provedor}." : body);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem por WhatsApp via {Provedor}.", provedor);
                return (false, ex.Message);
            }
        }

        private static StringContent CriarConteudoJson(object payload)
        {
            return new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");
        }

        private static bool BodyIndicaErro(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            return body.Contains("\"error\":true", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("\"success\":false", StringComparison.OrdinalIgnoreCase) ||
                   body.Contains("\"status\":\"error\"", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizarTelefone(string telefone)
        {
            var digits = new string((telefone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits))
            {
                return string.Empty;
            }

            if (!digits.StartsWith("55") && (digits.Length == 10 || digits.Length == 11))
            {
                digits = "55" + digits;
            }

            return digits;
        }

        private static string? PrimeiroPreenchido(params string?[] valores)
        {
            return valores.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        private static string? MontarEndpointEvolution(FornecedorWhatsapp fornecedor)
        {
            var endpointBase = AplicarPlaceholders(fornecedor.Endpoint, fornecedor);
            if (string.IsNullOrWhiteSpace(endpointBase))
            {
                return null;
            }

            if (endpointBase.Contains("/message/sendText/", StringComparison.OrdinalIgnoreCase))
            {
                return endpointBase;
            }

            if (string.IsNullOrWhiteSpace(fornecedor.InstanceId))
            {
                return null;
            }

            return $"{endpointBase.TrimEnd('/')}/message/sendText/{fornecedor.InstanceId.Trim()}";
        }

        private static string? MontarEndpointZApi(FornecedorWhatsapp fornecedor)
        {
            var endpointBase = AplicarPlaceholders(fornecedor.Endpoint, fornecedor);
            if (string.IsNullOrWhiteSpace(endpointBase))
            {
                return null;
            }

            if (endpointBase.EndsWith("/send-text", StringComparison.OrdinalIgnoreCase))
            {
                return endpointBase;
            }

            var token = fornecedor.Token?.Trim();
            var instanceId = fornecedor.InstanceId?.Trim();
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return $"{endpointBase.TrimEnd('/')}/instances/{instanceId}/token/{token}/send-text";
        }

        private static string AplicarPlaceholders(string? endpoint, FornecedorWhatsapp fornecedor)
        {
            var valor = (endpoint ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            return valor
                .Replace("{instanceId}", fornecedor.InstanceId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{token}", fornecedor.Token ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("{apiKey}", fornecedor.ApiKey ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
