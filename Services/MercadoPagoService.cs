using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace CorteCor.Services;

public class MercadoPagoService
{
    private readonly string _accessToken;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MercadoPagoService(IConfiguration configuration, HttpClient httpClient = null)
    {
        _configuration = configuration;
        // Pura conveniÃªncia para o teste, buscando do appsettings
        _accessToken = _configuration["MercadoPago:AccessToken"] ?? "";
        _httpClient = httpClient ?? new HttpClient();
    }

    public virtual async Task<(MpPreferenceResponse? preference, string? errorMessage)> CreatePreferenceAsync(
        string accessToken, 
        Guid idPagamento, 
        string title, 
        decimal price, 
        string emailCliente, 
        string baseUrl)
    {
        var client = _httpClient; 
        // Se jÃ¡ tiver header de auth, remove para garantir que usaremos o token passado ou limpa
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var requestBody = new
        {
            items = new[]
            {
                new
                {
                    title = title,
                    quantity = 1,
                    currency_id = "BRL",
                    unit_price = price
                }
            },
            payer = new
            {
                email = emailCliente
            },
            external_reference = idPagamento.ToString(),
            back_urls = new
            {
                success = $"{baseUrl}/Agendamentos",
                failure = $"{baseUrl}/Agendamentos",
                pending = $"{baseUrl}/Agendamentos"
            },
            auto_return = "approved",
            notification_url = $"{baseUrl}/Webhooks/MercadoPago"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences", content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorObj = JsonSerializer.Deserialize<MpErrorResponse>(responseJson);
                var detail = errorObj?.Message ?? "Erro desconhecido na API do Mercado Pago";
                if (errorObj?.Cause != null && errorObj.Cause.Any())
                {
                    detail += ": " + string.Join(", ", errorObj.Cause.Select(c => c.Description));
                }
                return (null, detail);
            }
            catch
            {
                return (null, $"Erro na API do Mercado Pago (Status {response.StatusCode}): {responseJson}");
            }
        }

        var pref = JsonSerializer.Deserialize<MpPreferenceResponse>(responseJson);
        return (pref, null);
    }

    public virtual async Task<MpPaymentResponse?> GetPaymentDetailsAsync(string paymentId)
    {
        var client = _httpClient;
        if (client.DefaultRequestHeaders.Authorization == null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{paymentId}");
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MpPaymentResponse>(responseJson);
    }
}

public class MpPreferenceResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("init_point")]
    public string InitPoint { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("sandbox_init_point")]
    public string SandboxInitPoint { get; set; } = "";
}

public class MpPaymentResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public long Id { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("status_detail")]
    public string StatusDetail { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("external_reference")]
    public string ExternalReference { get; set; } = "";

    [System.Text.Json.Serialization.JsonPropertyName("date_approved")]
    public DateTime? DateApproved { get; set; }
}

public class MpErrorResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? Message { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public int Status { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("cause")]
    public List<MpErrorCause>? Cause { get; set; }
}

public class MpErrorCause
{
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
}

