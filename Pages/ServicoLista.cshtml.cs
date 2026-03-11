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
    public class ServicoListaModel : PageModel
    {
        public List<Servico> Servicos { get; set; } = new();
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public int? IdCategoria { get; set; }
        public string Mensagem { get; set; }

        public void OnGet(int? idCategoria = null)
        {
            IdCategoria = idCategoria;
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");

            var catHandler = new CategoriaProdutoHandler();
            Categorias = catHandler.ListarPorSalao(idSalao) ?? new List<CategoriaProduto>();

            var handler = new ServicoHandler();
            Servicos = handler.ListarPorSalao(idSalao, idCategoria) ?? new List<Servico>();

            foreach (var s in Servicos)
            {
                s.CategoriaNome = Categorias.FirstOrDefault(c => c.IdCategoria == s.IdCategoria)?.Nome;
            }
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
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
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

