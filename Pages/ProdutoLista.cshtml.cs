using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ProdutoListaModel : PageModel
    {
        private readonly ProdutoHandler _produtoHandler;
        private readonly CategoriaProdutoHandler _categoriaHandler;

        public PagedResult<Produto> Produtos { get; set; } = new();
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public int? IdCategoria { get; set; }
        public string? q { get; set; }
        public bool incluirArquivados { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";
        public int p { get; set; } = 1;

        public ProdutoListaModel(ProdutoHandler produtoHandler, CategoriaProdutoHandler categoriaHandler)
        {
            _produtoHandler = produtoHandler;
            _categoriaHandler = categoriaHandler;
        }

        public void OnGet(int? idCategoria = null, string? q = null, bool incluirArquivados = false, int p = 1)
        {
            IdCategoria = idCategoria;
            this.q = q;
            this.incluirArquivados = incluirArquivados;
            this.p = p;

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "";
                MensagemTipo = "danger";
                return;
            }

            Categorias = _categoriaHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
            Produtos = _produtoHandler.ListarPaginadoPorSalao(idSalao, idCategoria, q, incluirArquivados, p, 10);

            foreach (var produto in Produtos.Items)
            {
                produto.CategoriaNome = Categorias.FirstOrDefault(c => c.IdCategoria == produto.IdCategoria)?.Nome;
            }
        }

        public IActionResult OnPost(int id, string action, int? idCategoria, string? q, bool incluirArquivados, int p = 1)
        {
            if (action == "alterar")
            {
                return Redirect($"{HttpContext.Request.PathBase}/ProdutoCadastro?id={id}");
            }

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "";
                MensagemTipo = "danger";
                OnGet(idCategoria, q, incluirArquivados, p);
                return Page();
            }

            if (action == "excluir")
            {
                try
                {
                    _produtoHandler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Produto inativado com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception ex)
                {
                    Mensagem = $"Erro ao inativar produto: {ex.Message}";
                    MensagemTipo = "danger";
                }
            }

            OnGet(idCategoria, q, incluirArquivados, p);
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}


