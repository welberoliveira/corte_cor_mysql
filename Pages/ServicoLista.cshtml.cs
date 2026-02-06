using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ServicoListaModel : PageModel
    {
        public List<Servico> Servicos { get; set; }
        public string Mensagem { get; set; }

        public void OnGet()
        {
            var handler = new ServicoHandler();
            Servicos = handler.Listar() ?? new List<Servico>();
        }

        public void OnPost()
        {
            var handler = new ServicoHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
            {
                try
                {
                    handler.Excluir(id);
                    Mensagem = "Serviço excluído com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir este Serviço porque ele está associado a outros registros.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/ServicoCadastro?id={id}");
            }

            OnGet();
        }
    }
}
