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

        [TempData]
        public string? FlashMensagem { get; set; }

        [TempData]
        public string? FlashMensagemTipo { get; set; }

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
            Mensagem = FlashMensagem ?? string.Empty;
            MensagemTipo = FlashMensagemTipo ?? "info";

            if (!TryObterIdSalao(out var idSalao))
            {
                Categorias = new PagedResult<CategoriaProduto> { PageIndex = 1, PageSize = 10 };
                Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                return;
            }

            Categorias = _categoriaHandler.ListarPaginadoPorSalao(idSalao, q, incluirInativas, p, 10);
        }

        public IActionResult OnPost(int id, string action, string? q, bool incluirInativas, int p = 1)
        {
            if (action == "alterar")
            {
                if (id <= 0)
                {
                    FlashMensagem = "Não foi possível identificar a categoria selecionada.";
                    FlashMensagemTipo = "danger";
                    return RedirectToPage(new { q, incluirInativas, p });
                }

                return RedirectToPage("/CategoriaProdutoCadastro", new { id });
            }

            if (!TryObterIdSalao(out var idSalao))
            {
                FlashMensagem = "Não foi possível identificar a empresa atual.";
                FlashMensagemTipo = "danger";
                return RedirectToPage(new { q, incluirInativas, p });
            }

            if (action == "excluir")
            {
                try
                {
                    _categoriaHandler.ExcluirPorSalao(id, idSalao);
                    FlashMensagem = "Categoria inativada com sucesso.";
                    FlashMensagemTipo = "success";
                }
                catch (Exception)
                {
                    FlashMensagem = "Não foi possível inativar esta categoria.";
                    FlashMensagemTipo = "danger";
                }
            }

            return RedirectToPage(new { q, incluirInativas, p });
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}


