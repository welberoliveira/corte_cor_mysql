using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CategoriaProdutoListaModel : PageModel
    {
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public string Mensagem { get; set; }

        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var handler = new CategoriaProdutoHandler();
            Categorias = handler.ListarPorSalao(idSalao) ?? new List<CategoriaProduto>();
        }

        public void OnPost()
        {
            var handler = new CategoriaProdutoHandler();
            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "excluir")
            {
                try
                {
                    int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                    handler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Categoria excluída com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir esta Categoria. Pode estar em uso por algum Produto.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/CategoriaProdutoCadastro?id={id}");
            }

            OnGet();
        }
    }
}
