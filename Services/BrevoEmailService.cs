using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;


namespace CorteCor.Services
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly ModeloEmailHandler _modeloEmailHandler;
        private readonly FornecedoresHandler _fornecedoresHandler;

        public BrevoEmailService(HttpClient httpClient, ModeloEmailHandler modeloEmailHandler, FornecedoresHandler fornecedoresHandler)
        {
            _httpClient = httpClient;
            _modeloEmailHandler = modeloEmailHandler;
            _fornecedoresHandler = fornecedoresHandler;
        }

        public virtual async Task<(bool Success, string ErrorMessage)> EnviarEmailTemplateAsync(int idSalao, string tipoEvento, string emailDestino, Dictionary<string, string> variaveis)
        {
            var fornecedor = _fornecedoresHandler.ObterEmailAtivo();
            if (fornecedor == null || fornecedor.Nome != "Brevo")
            {
                return (false, "Brevo is not the active email provider or doesn't exist.");
            }
            string apiKeyEmail = fornecedor.ApiKey;

            try
            {
                // 1. Get Template from DB
                var modelo = _modeloEmailHandler.ObterPorEvento(idSalao, tipoEvento);
                if (modelo == null || !modelo.Ativo)
                {
                    string msg = $"[BrevoEmailService] Template not found or inactive for Salon {idSalao} / Event {tipoEvento}";
                    Console.WriteLine(msg);
                    return (false, msg);
                }

                // 2. Proccess Variables
                string assunto = modelo.Assunto;
                string corpo = modelo.CorpoHTML;

                foreach (var kvp in variaveis)
                {
                    assunto = assunto.Replace($"{{{kvp.Key}}}", kvp.Value);
                    corpo = corpo.Replace($"{{{kvp.Key}}}", kvp.Value);
                }

                // 3. Prepare Brevo Payload
                var payload = new
                {
                    sender = new { name = "CorteCor", email = "no-reply@cortecor.com.br" }, // Adjust sender if needed
                    to = new[] { new { email = emailDestino } },
                    subject = assunto,
                    htmlContent = corpo
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKeyEmail);
                request.Content = content;

                // 4. Send
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    string msg = $"Error sending email: {response.StatusCode} - {error}";
                    Console.WriteLine($"[BrevoEmailService] {msg}");
                    return (false, msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Exception: {ex.Message}";
                Console.WriteLine($"[BrevoEmailService] {msg}");
                return (false, msg);
            }
        }

        public virtual async Task<(bool Success, string ErrorMessage)> EnviarEmailGenericoAsync(string emailDestino, string nomeDestino, string assunto, string corpoHtml)
        {
            var fornecedor = _fornecedoresHandler.ObterEmailAtivo();
            if (fornecedor == null || fornecedor.Nome != "Brevo")
            {
                return (false, "Brevo is not the active email provider or doesn't exist.");
            }
            string apiKeyEmail = fornecedor.ApiKey;

            try
            {
                var payload = new
                {
                    sender = new { name = "CorteCor", email = "no-reply@cortecor.com.br" },
                    to = new[] { new { email = emailDestino, name = nomeDestino } },
                    subject = assunto,
                    htmlContent = corpoHtml
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKeyEmail);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    string msg = $"Error sending email: {response.StatusCode} - {error}";
                    Console.WriteLine($"[BrevoEmailService] {msg}");
                    return (false, msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"Exception: {ex.Message}";
                Console.WriteLine($"[BrevoEmailService] {msg}");
                return (false, msg);
            }
        }

        public virtual async Task<(bool Success, string ErrorMessage)> EnviarSmsAsync(string telefoneDestino, string conteudo)
        {
            var fornecedor = _fornecedoresHandler.ObterSMSAtivo();
            if (fornecedor == null || fornecedor.Nome != "Brevo")
            {
                return (false, "Brevo is not the active SMS provider or doesn't exist.");
            }
            string apiKeySMS = fornecedor.ApiKey;

            try
            {
                // Format phone number: Ensure it starts with country code if missing (assuming BR +55 for now or handled by caller)
                // Brevo requires international format.
                // Assuming input might be (XX) XXXXX-XXXX or similar.
                string numbersOnly = new string(telefoneDestino.Where(char.IsDigit).ToArray());
                if (numbersOnly.Length <= 11) // No country code
                    numbersOnly = "55" + numbersOnly;

                var payload = new
                {
                    sender = "CorteCor", // Max 11 alphanumeric chars
                    recipient = numbersOnly,
                    content = conteudo
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/transactionalSMS/send");
                request.Headers.Add("api-key", apiKeySMS);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    string msg = $"Error sending SMS: {response.StatusCode} - {error}";
                    Console.WriteLine($"[BrevoEmailService] {msg}");
                    return (false, msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"SMS Exception: {ex.Message}";
                Console.WriteLine($"[BrevoEmailService] {msg}");
                return (false, msg);
            }
        }
    }
}

