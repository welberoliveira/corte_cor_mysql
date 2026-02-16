using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using static CorteCor.Models;

namespace CorteCor
{
    public class BrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ModeloEmailHandler _modeloEmailHandler;

        public BrevoEmailService(HttpClient httpClient, ModeloEmailHandler modeloEmailHandler, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Brevo:ApiKey"];
            _modeloEmailHandler = modeloEmailHandler;
        }

        public async Task<bool> EnviarEmailTemplateAsync(int idSalao, string tipoEvento, string emailDestino, Dictionary<string, string> variaveis)
        {
            try
            {
                // 1. Get Template from DB
                var modelo = _modeloEmailHandler.ObterPorEvento(idSalao, tipoEvento);
                if (modelo == null || !modelo.Ativo)
                {
                    Console.WriteLine($"[BrevoEmailService] Template not found or inactive for Salon {idSalao} / Event {tipoEvento}");
                    return false;
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
                request.Headers.Add("api-key", _apiKey);
                request.Content = content;

                // 4. Send
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[BrevoEmailService] Error sending email: {response.StatusCode} - {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrevoEmailService] Exception: {ex.Message}");
                return false;
            }
        }
    }
}
