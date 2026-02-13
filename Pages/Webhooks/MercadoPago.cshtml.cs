using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using static CorteCor.Models;

namespace CorteCor.Pages.Webhooks
{
    [IgnoreAntiforgeryToken]
    public class MercadoPagoWebhookModel : PageModel
    {
        private readonly ILogger<MercadoPagoWebhookModel> _logger;
        private readonly IConfiguration _configuration;

        public MercadoPagoWebhookModel(ILogger<MercadoPagoWebhookModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try 
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                _logger.LogInformation("Webhook Mercado Pago recebido: {Body}", body);

                if (string.IsNullOrWhiteSpace(body)) 
                {
                    _logger.LogWarning("Webhook recebeu corpo vazio.");
                    return StatusCode(200); 
                }

                JsonDocument notification;
                try
                {
                    notification = JsonDocument.Parse(body);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Erro ao fazer parse do JSON do webhook.");
                    return BadRequest();
                }
                string action = "";
                if (notification.RootElement.TryGetProperty("action", out var actionProp))
                    action = actionProp.GetString() ?? "";

                // Notificação de pagamento
                if (action == "payment.created" || action == "payment.updated" || body.Contains("data.id"))
                {
                    string paymentId = "";
                    if (notification.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
                    {
                        paymentId = idProp.GetString() ?? "";
                    }
                    else if (Request.Query.ContainsKey("id"))
                    {
                        paymentId = Request.Query["id"];
                    }

                    if (!string.IsNullOrEmpty(paymentId))
                    {
                        var mpService = new MercadoPagoService(_configuration);
                        var payment = await mpService.GetPaymentDetailsAsync(paymentId);

                        if (payment != null)
                        {
                            var pagHandler = new PagamentoHandler();
                            var agendamentoHandler = new AgendamentoHandler();

                            // 1. Tenta localizar pelo ID do Pagamento (External Reference agora é GUID)
                            Pagamento p = null;
                            if (Guid.TryParse(payment.ExternalReference, out Guid idPagamentoRef))
                            {
                                p = pagHandler.ObterPorId(idPagamentoRef);
                            }
                            else if (int.TryParse(payment.ExternalReference, out int idAgendamentoRef))
                            {
                                // Fallback para pagamentos antigos que usavam ID Agendamento
                                p = pagHandler.ObterPorIdAgendamento(idAgendamentoRef);
                            }

                            if (p != null)
                            {
                                p.MercadoPagoPaymentId = payment.Id.ToString();
                                p.MpStatus = payment.Status;
                                p.MpStatusDetail = payment.StatusDetail;
                                
                                if (payment.Status == "approved")
                                {
                                    p.Status = "Pago";
                                    p.PagoEm = payment.DateApproved ?? DateTime.UtcNow;
                                    agendamentoHandler.AtualizarStatus(p.IdAgendamento, "Pago");
                                }
                                else if (payment.Status == "rejected" || payment.Status == "cancelled")
                                {
                                    // Se rejeitado, cancelamos APENAS a tentativa de pagamento.
                                    // O agendamento permanece "Pendente" (ou "Agendado") para permitir nova tentativa.
                                    p.Status = "Cancelado";
                                    p.Ativo = false;
                                    
                                    // Opcional: Voltar status do agendamento para Agendado se estava Pendente?
                                    // agendamentoHandler.AtualizarStatus(p.IdAgendamento, "Agendado");
                                }

                                pagHandler.AtualizarPagamento(p);
                            }
                        }
                    }
                }

                return StatusCode(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook Mercado Pago");
                return StatusCode(500);
            }
        }
    }
}
