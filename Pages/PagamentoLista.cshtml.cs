using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using CorteCor.Services;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PagamentoListaModel : PageModel
    {
        public PagedResult<Pagamento> Pagamentos { get; set; }
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

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public decimal TotalValor { get; set; }
        public int TotalContagem { get; set; }

        public void OnGet()
        {
            var handler = new PagamentoHandler();
            var filtro = new PagamentoFiltroDTO
            {
                DataInicio = DataInicio,
                DataFim = DataFim,
                Status = Status,
                NomeCliente = NomeCliente,
                DataAgendamento = DataAgendamento,
                PageIndex = p > 0 ? p : 1,
                PageSize = 10
            };

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            Pagamentos = handler.ListarPorSalao(idSalao, filtro);
            if (Pagamentos == null) Pagamentos = new PagedResult<Pagamento>();

            var resumo = handler.ObterResumo(idSalao, filtro);
            TotalValor = resumo.totalValor;
            TotalContagem = resumo.totalContagem;
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
                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                var mpService = new MercadoPagoService(HttpContext.RequestServices.GetRequiredService<IConfiguration>());
                bool synced = await handler.SincronizarPagamento(id, mpService, idSalao);
                if (synced) Mensagem = "Status sincronizado com sucesso!";
                else Mensagem = "Não foi possível sincronizar ou pagamento não encontrado no Mercado Pago.";
            }

            OnGet();
            return Page();
        }
    }
}
