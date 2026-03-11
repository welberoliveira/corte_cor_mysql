using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ProdutoCadastroModel : PageModel
    {
        [BindProperty]
        public Produto Produto { get; set; }
        public List<CategoriaProduto> Categorias { get; set; }
        public string Mensagem { get; set; }
        public string ButtonText { get; set; } = "Cadastrar";

        public void OnGet(int? id)
        {
            CarregarListas();

            if (id.HasValue && id > 0)
            {
                var handler = new ProdutoHandler();
                Produto = handler.ObterPorId(id.Value);

                if (Produto != null)
                {
                    ButtonText = "Atualizar";
                }
                else
                {
                    Produto = new Produto { DataCadastro = DateTime.Now, ControlarEstoque = true };
                }
            }
            else
            {
                Produto = new Produto { DataCadastro = DateTime.Now, ControlarEstoque = true };
            }
        }

        public IActionResult OnPost()
        {
            var handler = new ProdutoHandler();
            string action = Request.Form["action"];

            if (action == "salvar" && Produto != null)
            {
                int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                Produto.IdSalao = idSalao;

                // Tenta extrair Checkboxes
                Produto.Arquivado = Request.Form["arquivado"] == "on";
                Produto.ControlarEstoque = Request.Form["controlarEstoque"] == "on";
                Produto.UnidadeTributadaDiferente = Request.Form["unidadeTributadaDiferente"] == "on";
                Produto.IgnorarTribPrecoVenda = Request.Form["ignorarTribPrecoVenda"] == "on";

                // Ajustes de Numéricos com culture dependendo do que vier da tela (. ou ,)
                if (decimal.TryParse(Request.Form["precoCusto"].ToString()?.Replace("R$", "")?.Trim()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precoCustoOut))
                {
                    Produto.PrecoCusto = precoCustoOut;
                }
                
                if (decimal.TryParse(Request.Form["precoVenda"].ToString()?.Replace("R$", "")?.Trim()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal precoVendaOut))
                {
                    Produto.PrecoVenda = precoVendaOut;
                }

                if (decimal.TryParse(Request.Form["margemContribuicao"], out decimal mcOut)) Produto.MargemContribuicao = mcOut;
                if (decimal.TryParse(Request.Form["estoqueAtual"].ToString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal estAtualOut)) Produto.EstoqueAtual = estAtualOut;
                if (decimal.TryParse(Request.Form["estoqueMinimo"].ToString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal estMinOut)) Produto.EstoqueMinimo = estMinOut;

                if (Produto.IdProduto > 0)
                {
                    handler.Atualizar(Produto);
                    Mensagem = "Produto atualizado com sucesso.";
                    ButtonText = "Atualizar";
                }
                else
                {
                    Produto.DataCadastro = DateTime.Now;
                    Produto.Excluido = false;
                    Produto.IdProduto = handler.CadastrarProduto(Produto);
                    Mensagem = "Produto cadastrado com sucesso.";
                    ButtonText = "Atualizar";
                }
            }
            
            CarregarListas();
            return Page();
        }

        public IActionResult OnPostSaveCategory(string nome, string descricao)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return new JsonResult(new { success = false, message = "Nome é obrigatório." });

            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            var cat = new CategoriaProduto
            {
                Nome = nome,
                IdSalao = idSalao,
                Ativo = true,
                DataCadastro = DateTime.Now
            };

            var catHandler = new CategoriaProdutoHandler();
            int id = catHandler.CadastrarCategoria(cat);

            if (id > 0)
            {
                return new JsonResult(new { success = true, id = id, nome = nome });
            }

            return new JsonResult(new { success = false, message = "Erro ao cadastrar categoria." });
        }

        private void CarregarListas()
        {
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            var catHandler = new CategoriaProdutoHandler();
            Categorias = catHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
        }
    }
}
