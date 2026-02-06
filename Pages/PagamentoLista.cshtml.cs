using Microsoft.AspNetCore.Authorization;
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

        public void OnGet()
        {
            var handler = new PagamentoHandler();
            Pagamentos = handler.Listar() ?? new List<Pagamento>();
        }

        public void OnPost()
        {
            var handler = new PagamentoHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
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
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/PagamentoCadastro?id={id}");
            }

            OnGet();
        }
    }
}
