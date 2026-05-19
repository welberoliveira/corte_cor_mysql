using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ServicoListaModel : PageModel
    {
        private readonly CategoriaProdutoHandler _categoriaHandler;
        private readonly ServicoHandler _servicoHandler;

        public PagedResult<Servico> Servicos { get; set; } = new();
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public int? IdCategoria { get; set; }
        public string? q { get; set; }
        public bool incluirArquivados { get; set; }
        public int p { get; set; } = 1;
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "info";

        [TempData]
        public string? FlashMensagem { get; set; }

        [TempData]
        public string? FlashMensagemTipo { get; set; }

        public ServicoListaModel(CategoriaProdutoHandler categoriaHandler, ServicoHandler servicoHandler)
        {
            _categoriaHandler = categoriaHandler;
            _servicoHandler = servicoHandler;
        }

        public void OnGet(int? idCategoria = null, string? q = null, bool incluirArquivados = false, int p = 1)
        {
            IdCategoria = idCategoria;
            this.q = q;
            this.incluirArquivados = incluirArquivados;
            this.p = p;
            Mensagem = FlashMensagem ?? string.Empty;
            MensagemTipo = FlashMensagemTipo ?? "info";

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                return;
            }

            Categorias = _categoriaHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
            Servicos = _servicoHandler.ListarPaginadoPorSalao(idSalao, idCategoria, q, incluirArquivados, p, 10);

            foreach (var servico in Servicos.Items)
            {
                servico.CategoriaNome = Categorias.FirstOrDefault(c => c.IdCategoria == servico.IdCategoria)?.Nome;
            }
        }

        public IActionResult OnPost(int id, string action, int? idCategoria, string? q, bool incluirArquivados, int p = 1)
        {
            if (action == "alterar")
            {
                if (id <= 0)
                {
                    FlashMensagem = "Não foi possível identificar o serviço selecionado.";
                    FlashMensagemTipo = "danger";
                    return RedirectToPage(new { idCategoria, q, incluirArquivados, p });
                }

                return RedirectToPage("/ServicoCadastro", new { id });
            }

            if (!TryObterIdSalao(out var idSalao))
            {
                FlashMensagem = "Não foi possível identificar a empresa atual.";
                FlashMensagemTipo = "danger";
                return RedirectToPage(new { idCategoria, q, incluirArquivados, p });
            }

            if (action == "excluir")
            {
                try
                {
                    _servicoHandler.ExcluirPorSalao(id, idSalao);
                    FlashMensagem = "Serviço inativado com sucesso.";
                    FlashMensagemTipo = "success";
                }
                catch (Exception)
                {
                    FlashMensagem = "Não foi possível inativar este serviço porque ele está associado a outros registros.";
                    FlashMensagemTipo = "warning";
                }
            }

            return RedirectToPage(new { idCategoria, q, incluirArquivados, p });
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}


