using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ProdutoListaModel : PageModel
    {
        public List<Produto> Produtos { get; set; } = new();
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public int? IdCategoria { get; set; }
        public string Mensagem { get; set; }

        public void OnGet(int? idCategoria = null)
        {
            IdCategoria = idCategoria;
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");

            var catHandler = new CategoriaProdutoHandler();
            Categorias = catHandler.ListarPorSalao(idSalao) ?? new List<CategoriaProduto>();

            var handler = new ProdutoHandler();
            Produtos = handler.ListarPorSalao(idSalao, idCategoria) ?? new List<Produto>();

            foreach (var p in Produtos)
            {
                p.CategoriaNome = Categorias.FirstOrDefault(c => c.IdCategoria == p.IdCategoria)?.Nome;
            }
        }

        public void OnPost()
        {
            var handler = new ProdutoHandler();
            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
            {
                try
                {
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Produto inativado com sucesso.";
                }
                catch (Exception ex)
                {
                    Mensagem = $"Erro ao inativar produto: {ex.Message}";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/ProdutoCadastro?id={id}");
            }

            OnGet();
        }
    }
}
