using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CategoriaProdutoCadastroModel : PageModel
    {
        [BindProperty]
        public CategoriaProduto Categoria { get; set; }
        public string Mensagem { get; set; }
        public string ButtonText { get; set; } = "Cadastrar";

        public void OnGet(int? id)
        {
            if (id.HasValue && id > 0)
            {
                int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                var handler = new CategoriaProdutoHandler();
                
                var lista = handler.ListarPorSalao(idSalao);
                Categoria = lista?.FirstOrDefault(c => c.IdCategoria == id.Value);

                if (Categoria != null)
                {
                    ButtonText = "Atualizar";
                }
                else
                {
                    Categoria = new CategoriaProduto { Ativo = true };
                }
            }
            else
            {
                Categoria = new CategoriaProduto { Ativo = true };
            }
        }

        public IActionResult OnPost()
        {
            var handler = new CategoriaProdutoHandler();
            string action = Request.Form["action"];

            if (action == "salvar" && Categoria != null)
            {
                int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                Categoria.IdSalao = idSalao;

                // Lê checkbox "ativo" manualmente do form para mapear checked
                Categoria.Ativo = Request.Form["ativo"] == "on";

                if (Categoria.IdCategoria > 0)
                {
                    handler.Atualizar(Categoria);
                    Mensagem = "Categoria atualizada com sucesso.";
                    ButtonText = "Atualizar";
                }
                else
                {
                    Categoria.DataCadastro = DateTime.Now;
                    Categoria.IdCategoria = handler.CadastrarCategoria(Categoria);
                    Mensagem = "Categoria cadastrada com sucesso.";
                    ButtonText = "Atualizar";
                }
            }
            return Page();
        }
    }
}
