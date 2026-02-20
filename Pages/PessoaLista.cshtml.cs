using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc; 


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PessoaListaModel : PageModel
    {
        public PagedResult<Pessoa> Pessoas { get; set; } = new PagedResult<Pessoa>();
        public string Mensagem { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public void OnGet()
        {
            var handler = new PessoaHandler();
            Pessoas = handler.Listar(p > 0 ? p : 1, 10);
            if (Pessoas == null) Pessoas = new PagedResult<Pessoa>();
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

