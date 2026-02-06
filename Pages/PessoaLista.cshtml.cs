using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PessoaListaModel : PageModel
    {
        public List<Pessoa> Pessoas { get; set; }
        public string Mensagem { get; set; }

        public void OnGet()
        {
            var handler = new PessoaHandler();
            Pessoas = handler.Listar() ?? new List<Pessoa>();
        }

        public void OnPost()
        {
            var handler = new PessoaHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
            {
                try
                {
                    handler.Excluir(id);
                    Mensagem = "Pessoa excluída com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir esta Pessoa porque ela está associada a outros registros.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/PessoaCadastro?id={id}");
            }

            OnGet();
        }
    }
}
