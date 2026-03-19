using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CategoriaProdutoListaModel : PageModel
    {
        private readonly CategoriaProdutoHandler _categoriaHandler;

        public PagedResult<CategoriaProduto> Categorias { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";

        [BindProperty(SupportsGet = true)]
        public string? q { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool incluirInativas { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public CategoriaProdutoListaModel(CategoriaProdutoHandler categoriaHandler)
        {
            _categoriaHandler = categoriaHandler;
        }

        public void OnGet()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Categorias = new PagedResult<CategoriaProduto> { PageIndex = 1, PageSize = 10 };
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                return;
            }

            Categorias = _categoriaHandler.ListarPaginadoPorSalao(idSalao, q, incluirInativas, p, 10);
        }

        public IActionResult OnPost(int id, string action, string? q, bool incluirInativas, int p = 1)
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                OnGet();
                return Page();
            }

            if (action == "alterar")
            {
                return Redirect($"{HttpContext.Request.PathBase}/CategoriaProdutoCadastro?id={id}");
            }

            if (action == "excluir")
            {
                try
                {
                    _categoriaHandler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Categoria inativada com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception)
                {
                    Mensagem = "Nao foi possivel inativar esta categoria.";
                    MensagemTipo = "danger";
                }
            }

            this.q = q;
            this.incluirInativas = incluirInativas;
            this.p = p;
            OnGet();
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
