using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class MeioPagamentoListaModel : PageModel
    {
        public List<MeioPagamento> MeiosPagamento { get; set; } = new();
        public string Mensagem { get; set; }

        public void OnGet()
        {
            var handler = new MeioPagamentoHandler();
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);
            MeiosPagamento = handler.ListarPorSalao(idSalao, null) ?? new List<MeioPagamento>();
        }

        public void OnPost()
        {
            var handler = new MeioPagamentoHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var meio = handler.ObterPorId(id);
            if (meio == null || meio.IdSalao != idSalao)
            {
                Mensagem = "Meio de pagamento não encontrado ou acesso negado.";
                OnGet();
                return;
            }

            if (action == "ativar")
            {
                handler.AtivarDesativar(id, true);
                Mensagem = "Meio de pagamento ativado com sucesso.";
            }
            else if (action == "desativar")
            {
                handler.AtivarDesativar(id, false);
                Mensagem = "Meio de pagamento desativado com sucesso.";
            }
            else if (action == "excluir")
            {
                try
                {
                    handler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Meio de pagamento excluído com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir este Meio de Pagamento porque ele está associado a outros registros.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/MeioPagamentoCadastro?id={id}");
            }

            OnGet();
        }
    }
}

