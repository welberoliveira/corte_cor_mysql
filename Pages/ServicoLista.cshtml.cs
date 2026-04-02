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
                return Redirect($"{HttpContext.Request.PathBase}/ServicoCadastro?id={id}");
            }

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                OnGet(idCategoria, q, incluirArquivados, p);
                return Page();
            }

            if (action == "excluir")
            {
                try
                {
                    _servicoHandler.ExcluirPorSalao(id, idSalao);
                    Mensagem = "Serviço excluído com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir este serviço porque ele está associado a outros registros.";
                    MensagemTipo = "warning";
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


