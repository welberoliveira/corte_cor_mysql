using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Adicionado para IActionResult e BindProperty
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PagamentoListaModel : PageModel
    {
        public List<Pagamento> Pagamentos { get; set; }
        public string Mensagem { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? NomeCliente { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataAgendamento { get; set; }

        public void OnGet()
        {
            var handler = new PagamentoHandler();
            var filtro = new PagamentoFiltroDTO
            {
                DataInicio = DataInicio,
                DataFim = DataFim,
                Status = Status,
                NomeCliente = NomeCliente,
                DataAgendamento = DataAgendamento
            };
            Pagamentos = handler.Listar(filtro) ?? new List<Pagamento>();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var handler = new PagamentoHandler();

            Guid id = Guid.Empty;
            Guid.TryParse(Request.Form["id"], out id);
            string action = Request.Form["action"];

            if (action == "excluir" && id != Guid.Empty)
            {
                try
                {
                    handler.Excluir(id);
                    Mensagem = "Pagamento excluído com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir este Pagamento porque ele está associado a outros registros.";
                }
            }
            else if (action == "alterar" && id != Guid.Empty)
            {
                return RedirectToPage("/PagamentoCadastro", new { id = id });
            }
            else if (action == "sincronizar" && id != Guid.Empty)
            {
                // Sincronização manual
                var mpService = new MercadoPagoService(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
                bool synced = await handler.SincronizarPagamento(id, mpService);
                if (synced) Mensagem = "Status sincronizado com sucesso!";
                else Mensagem = "Não foi possível sincronizar ou pagamento não encontrado no Mercado Pago.";
            }

            OnGet();
            return Page();
        }
    }
}