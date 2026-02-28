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
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var handler = new PessoaHandler();
            var todasPessoas = handler.ListarPorSalao(idSalao);
            
            // Apply memory pagination to maintain PagedResult
            int pageIndex = p > 0 ? p : 1;
            int pageSize = 10;
            
            Pessoas = new PagedResult<Pessoa> 
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalCount = todasPessoas.Count,
                Items = todasPessoas.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList()
            };
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
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
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

