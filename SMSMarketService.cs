using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CorteCor
{
    public class SMSMarketService
    {
        private readonly HttpClient _httpClient;
        private readonly FornecedoresHandler _fornecedoresHandler;

        public SMSMarketService(HttpClient httpClient, FornecedoresHandler fornecedoresHandler)
        {
            _httpClient = httpClient;
            _fornecedoresHandler = fornecedoresHandler;
            _httpClient.BaseAddress = new Uri("https://api.smsmarket.com.br/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public virtual async Task<(bool Success, string ErrorMessage)> EnviarSmsAsync(string telefoneDestino, string conteudo)
        {
            var fornecedor = _fornecedoresHandler.ObterSMSAtivo();
            if (fornecedor == null || fornecedor.Nome != "SMSMarket")
            {
                return (false, "SMSMarket is not the active SMS provider or doesn't exist.");
            }

            string user = fornecedor.ApiKey;
            string password = fornecedor.ApiSecret;
            
            // Configurar a autenticação Basic auth sob demanda.
            var authBytes = Encoding.ASCII.GetBytes($"{user}:{password}");
            var base64Auth = Convert.ToBase64String(authBytes);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

            try
            {
                // Format phone number: extrai apenas números.
                string numbersOnly = new string(telefoneDestino.Where(char.IsDigit).ToArray());
                
                // Conforme documentação, o DDI padrao é 55, e envia-se o numero sem ele através do param 'number'
                // Caso queira garantir o DDI a parte:
                int countryCode = 55;
                if (numbersOnly.StartsWith("55") && numbersOnly.Length > 11)
                {
                    numbersOnly = numbersOnly.Substring(2);
                }

                // Payload do Envio Individual de SMS (type = 2 para SMS Interativo/Standard)
                // A URL espera "application/x-www-form-urlencoded" para chamadas GET individuais com rest client, ou application/json para o lote/send-multiple
                // Pela apiary a URL /send-single tb suporta GET/POST x-www-form-urlencoded.
                
                var postData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("country_code", countryCode.ToString()),
                    new KeyValuePair<string, string>("number", numbersOnly),
                    new KeyValuePair<string, string>("content", conteudo),
                    // new KeyValuePair<string, string>("campaign_id", "xyz"), // Opcional
                    new KeyValuePair<string, string>("type", "2") // 0=(SMS), 1=(SMS Flash) ou 2=(SMS Interativo)
                };

                var content = new FormUrlEncodedContent(postData);

                // As credenciais tb poderiam ser passadas na url, mas ja estao no header Basic Auth
                var response = await _httpClient.PostAsync("webservice-rest/send-single", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    // Lendo JSON
                    using (JsonDocument doc = JsonDocument.Parse(responseString))
                    {
                        JsonElement root = doc.RootElement;
                        bool success = root.GetProperty("success").GetBoolean();
                        
                        if (success)
                        {
                            return (true, null);
                        }
                        else
                        {
                            string respDesc = root.GetProperty("responseDescription").GetString();
                            string msg = $"SMSMarket Error: {respDesc}";
                            Console.WriteLine($"[SMSMarketService] {msg}");
                            return (false, msg);
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    string msg = $"Error sending SMS via SMSMarket: {response.StatusCode} - {error}";
                    Console.WriteLine($"[SMSMarketService] {msg}");
                    return (false, msg);
                }
            }
            catch (Exception ex)
            {
                string msg = $"SMSMarket Exception: {ex.Message}";
                Console.WriteLine($"[SMSMarketService] {msg}");
                return (false, msg);
            }
        }
    }
}
