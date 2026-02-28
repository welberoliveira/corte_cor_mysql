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
    public class FuncionarioListaModel : PageModel
    {
        public List<Funcionario> Funcionarios { get; set; } = new();
        public string Mensagem { get; set; }

        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var handler = new FuncionarioHandler();
            Funcionarios = handler.ListarPorSalao(idSalao) ?? new List<Funcionario>();
        }

        public void OnPost()
        {
            var handler = new FuncionarioHandler();

            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
            {
                try
                {
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Funcionário excluído com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir este Funcionário porque ele está associado a outros registros.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/FuncionarioCadastro?id={id}");
            }

            OnGet();
        }
    }
}

