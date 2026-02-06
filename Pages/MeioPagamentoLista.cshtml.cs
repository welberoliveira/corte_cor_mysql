using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class MeioPagamentoListaModel : PageModel
    {
        public List<MeioPagamento> MeiosPagamento { get; set; }
        public string Mensagem { get; set; }

        public void OnGet()
        {
            var handler = new MeioPagamentoHandler();
            MeiosPagamento = handler.Listar() ?? new List<MeioPagamento>();
        }

        public void OnPost()
        {
            var handler = new MeioPagamentoHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

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
                    handler.Excluir(id);
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
