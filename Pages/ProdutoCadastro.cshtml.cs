using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ProdutoCadastroModel : PageModel
    {
        private readonly ProdutoHandler _produtoHandler;
        private readonly CategoriaProdutoHandler _categoriaHandler;

        [BindProperty]
        public Produto Produto { get; set; } = new();

        public List<CategoriaProduto> Categorias { get; set; } = new();
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";
        public string ButtonText { get; set; } = "Cadastrar";

        public ProdutoCadastroModel(ProdutoHandler produtoHandler, CategoriaProdutoHandler categoriaHandler)
        {
            _produtoHandler = produtoHandler;
            _categoriaHandler = categoriaHandler;
        }

        public void OnGet(int? id)
        {
            CarregarListas();

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                Produto = new Produto { DataCadastro = DateTime.Now, ControlarEstoque = true };
                return;
            }

            if (id.HasValue && id > 0)
            {
                Produto = _produtoHandler.ObterPorIdESalao(id.Value, idSalao) ?? new Produto { DataCadastro = DateTime.Now, ControlarEstoque = true };
                ButtonText = Produto.IdProduto > 0 ? "Atualizar" : "Cadastrar";
            }
            else
            {
                Produto = new Produto { DataCadastro = DateTime.Now, ControlarEstoque = true };
            }
        }

        public IActionResult OnPost()
        {
            CarregarListas();

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Nao foi possivel identificar o salao atual.";
                MensagemTipo = "danger";
                return Page();
            }

            string action = Request.Form["action"];
            if (action != "salvar")
            {
                return Page();
            }

            Produto.IdSalao = idSalao;
            Produto.Arquivado = Request.Form["arquivado"] == "on";
            Produto.ControlarEstoque = Request.Form["controlarEstoque"] == "on";
            Produto.UnidadeTributadaDiferente = Request.Form["unidadeTributadaDiferente"] == "on";
            Produto.IgnorarTribPrecoVenda = Request.Form["ignorarTribPrecoVenda"] == "on";
            Produto.PrecoCusto = ParseNullableDecimal(Request.Form["precoCusto"]);
            Produto.PrecoVenda = ParseDecimal(Request.Form["precoVenda"]);
            Produto.MargemContribuicao = ParseNullableDecimal(Request.Form["margemContribuicao"]);
            Produto.EstoqueAtual = ParseNullableDecimal(Request.Form["estoqueAtual"]);
            Produto.EstoqueMinimo = ParseNullableDecimal(Request.Form["estoqueMinimo"]);
            Produto.PesoLiquido = ParseNullableDecimal(Request.Form["pesoLiquido"]);
            Produto.PesoBruto = ParseNullableDecimal(Request.Form["pesoBruto"]);
            Produto.QuantidadeTributada = ParseNullableDecimal(Request.Form["quantidadeTributada"]);

            ButtonText = Produto.IdProduto > 0 ? "Atualizar" : "Cadastrar";

            if (!ValidarProduto())
            {
                return Page();
            }

            if (Produto.IdProduto > 0)
            {
                var existente = _produtoHandler.ObterPorIdESalao(Produto.IdProduto, idSalao);
                if (existente == null)
                {
                    Mensagem = "Produto nao encontrado para o salao atual.";
                    MensagemTipo = "danger";
                    return Page();
                }

                _produtoHandler.Atualizar(Produto);
                Mensagem = "Produto atualizado com sucesso.";
                MensagemTipo = "success";
                return Page();
            }

            Produto.DataCadastro = DateTime.Now;
            Produto.Excluido = false;
            Produto.IdProduto = _produtoHandler.CadastrarProduto(Produto);
            ButtonText = "Atualizar";
            Mensagem = "Produto cadastrado com sucesso.";
            MensagemTipo = "success";
            return Page();
        }

        public IActionResult OnPostSaveCategory(string nome, string descricao)
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return new JsonResult(new { success = false, message = "Salao nao identificado." });
            }

            nome = (nome ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                return new JsonResult(new { success = false, message = "Nome e obrigatorio." });
            }

            if (_categoriaHandler.ExisteNomePorSalao(nome, idSalao))
            {
                return new JsonResult(new { success = false, message = "Ja existe uma categoria com esse nome." });
            }

            var categoria = new CategoriaProduto
            {
                Nome = nome,
                IdSalao = idSalao,
                Ativo = true,
                DataCadastro = DateTime.Now
            };

            int id = _categoriaHandler.CadastrarCategoria(categoria);
            return id > 0
                ? new JsonResult(new { success = true, id, nome })
                : new JsonResult(new { success = false, message = "Erro ao cadastrar categoria." });
        }

        private void CarregarListas()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                Categorias = new List<CategoriaProduto>();
                return;
            }

            Categorias = _categoriaHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
        }

        private bool ValidarProduto()
        {
            if (string.IsNullOrWhiteSpace(Produto.Nome))
            {
                Mensagem = "Informe o nome do produto.";
                MensagemTipo = "warning";
                return false;
            }

            if (Produto.PrecoVenda < 0)
            {
                Mensagem = "O preco de venda nao pode ser negativo.";
                MensagemTipo = "warning";
                return false;
            }

            if (Produto.PrecoCusto.HasValue && Produto.PrecoCusto < 0)
            {
                Mensagem = "O preco de custo nao pode ser negativo.";
                MensagemTipo = "warning";
                return false;
            }

            if (Produto.ControlarEstoque)
            {
                if (Produto.EstoqueAtual.HasValue && Produto.EstoqueAtual < 0 || Produto.EstoqueMinimo.HasValue && Produto.EstoqueMinimo < 0)
                {
                    Mensagem = "Os valores de estoque nao podem ser negativos.";
                    MensagemTipo = "warning";
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(Produto.NCM) && !Regex.IsMatch(Produto.NCM, @"^\d{8}$"))
            {
                Mensagem = "O NCM deve conter exatamente 8 digitos.";
                MensagemTipo = "warning";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Produto.CEST) && !Regex.IsMatch(Produto.CEST, @"^\d{7}$"))
            {
                Mensagem = "O CEST deve conter exatamente 7 digitos.";
                MensagemTipo = "warning";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Produto.ReferenciaEAN) && !Regex.IsMatch(Produto.ReferenciaEAN, @"^\d{8,14}$"))
            {
                Mensagem = "O GTIN/EAN deve conter entre 8 e 14 digitos numericos.";
                MensagemTipo = "warning";
                return false;
            }

            return true;
        }

        private static decimal ParseDecimal(string valor)
        {
            return ParseNullableDecimal(valor) ?? 0m;
        }

        private static decimal? ParseNullableDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var normalizado = valor.Replace("R$", string.Empty).Trim().Replace(".", "").Replace(",", ".");
            return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : null;
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
