using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

using CorteCor.Services;

namespace CorteCor.Pages.Webhooks
{
    [IgnoreAntiforgeryToken]
    public class MercadoPagoWebhookModel : PageModel
    {
        private readonly ILogger<MercadoPagoWebhookModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly FinanceiroService _financeiroService;

        public MercadoPagoWebhookModel(
            ILogger<MercadoPagoWebhookModel> logger,
            IConfiguration configuration,
            FinanceiroService financeiroService)
        {
            _logger = logger;
            _configuration = configuration;
            _financeiroService = financeiroService;
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
                                p.MpStatus = payment.Status;
                                p.MpStatusDetail = payment.StatusDetail;
                                var idSalaoFinanceiro = p.IdSalao.GetValueOrDefault();
                                var agendamento = p.IdAgendamento.HasValue
                                    ? agendamentoHandler.ObterPorId(p.IdAgendamento.Value)
                                    : null;
                                Servico? servicoAgendamento = null;
                                if (agendamento != null)
                                {
                                    var servicoHandler = new ServicoHandler();
                                    servicoAgendamento = servicoHandler.ObterPorId(agendamento.IdServico);
                                    if (servicoAgendamento != null)
                                    {
                                        idSalaoFinanceiro = idSalaoFinanceiro > 0 ? idSalaoFinanceiro : servicoAgendamento.IdSalao;
                                    }
                                }

                                if (payment.Status == "approved")
                                {
                                    p.Status = "Pago";
                                    p.PagoEm = payment.DateApproved ?? DateTime.UtcNow;

                                    if (agendamento != null && servicoAgendamento != null)
                                    {
                                        agendamentoHandler.AtualizarStatus(agendamento.IdAgendamento, "Pago", servicoAgendamento.IdSalao);
                                    }
                                }
                                else if (payment.Status == "rejected" || payment.Status == "cancelled")
                                {
                                    // Se rejeitado, cancelamos APENAS a tentativa de pagamento.
                                    // O agendamento permanece "Pendente" (ou "Agendado") para permitir nova tentativa.
                                    p.Status = "Cancelado";
                                    p.Ativo = false;
                                    
                                    // Opcional: Voltar status do agendamento para Agendado se estava Pendente?
                                    // agendamentoHandler.AtualizarStatus(p.IdAgendamento, "Agendado", idSalao);
                                }

                                pagHandler.AtualizarStatusWebhook(p.IdPagamento, p.Status, payment.Id, p.MpStatus, p.MpStatusDetail, p.PagoEm);
                                if (idSalaoFinanceiro > 0)
                                {
                                    await _financeiroService.SincronizarTitulosPagamentoAsync(idSalaoFinanceiro);
                                }
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

