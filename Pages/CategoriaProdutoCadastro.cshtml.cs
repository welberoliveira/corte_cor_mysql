using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class CategoriaProdutoCadastroModel : PageModel
    {
        private readonly CategoriaProdutoHandler _categoriaHandler;

        [BindProperty]
        public CategoriaProduto Categoria { get; set; } = new CategoriaProduto();

        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";
        public string ButtonText { get; set; } = "Cadastrar";

        public CategoriaProdutoCadastroModel(CategoriaProdutoHandler categoriaHandler)
        {
            _categoriaHandler = categoriaHandler;
        }

        public void OnGet(int? id)
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Categoria = new CategoriaProduto { Ativo = true };
                Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                return;
            }

            if (id.HasValue && id > 0)
            {
                Categoria = _categoriaHandler.ObterPorIdESalao(id.Value, idSalao) ?? new CategoriaProduto { Ativo = true };
                ButtonText = Categoria.IdCategoria > 0 ? "Atualizar" : "Cadastrar";
                return;
            }

            Categoria = new CategoriaProduto { Ativo = true };
        }

        public IActionResult OnPost()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Não foi possível identificar a empresa atual.";
                MensagemTipo = "danger";
                return Page();
            }

            ButtonText = Categoria.IdCategoria > 0 ? "Atualizar" : "Cadastrar";
            Categoria.IdSalao = idSalao;
            Categoria.Nome = NormalizarTexto(Categoria.Nome);

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(Categoria.Nome))
            {
                Mensagem = "Revise os campos obrigatórios da categoria.";
                MensagemTipo = "danger";
                return Page();
            }

            if (_categoriaHandler.ExisteNomePorSalao(Categoria.Nome, idSalao, Categoria.IdCategoria > 0 ? Categoria.IdCategoria : null))
            {
                Mensagem = "Já existe uma categoria com esse nome.";
                MensagemTipo = "warning";
                return Page();
            }

            if (Categoria.IdCategoria > 0)
            {
                _categoriaHandler.Atualizar(Categoria);
                Mensagem = "Categoria atualizada com sucesso.";
                MensagemTipo = "success";
                return Page();
            }

            Categoria.DataCadastro = DateTime.Now;
            Categoria.IdCategoria = _categoriaHandler.CadastrarCategoria(Categoria);
            Mensagem = "Categoria cadastrada com sucesso.";
            MensagemTipo = "success";
            ButtonText = "Atualizar";
            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }

        private static string NormalizarTexto(string? valor)
        {
            return (valor ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
        }
    }
}


