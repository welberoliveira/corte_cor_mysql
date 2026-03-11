using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ServicoCadastroModel : PageModel
    {
        public Servico Servico { get; set; }
        public List<ItemListaServico> ItensLC116 { get; set; }
        public List<CategoriaProduto> Categorias { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            var itemHandler = new ItemListaServicoHandler();
            ItensLC116 = itemHandler.Listar() ?? new List<ItemListaServico>();

            if (id.HasValue)
            {
                int idSalao = 0;
                int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

                var handler = new ServicoHandler();
                Servico = handler.ObterPorId(id.Value);

                if (Servico != null && Servico.IdSalao != idSalao)
                {
                    Response.Redirect(HttpContext.Request.PathBase + $"/ServicoLista");
                    return;
                }

                ButtonText = "Atualizar";
            }

            CarregarCategorias();
        }

        private void CarregarCategorias()
        {
            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            var catHandler = new CategoriaProdutoHandler();
            Categorias = catHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
        }

        private static decimal ParsePrecoBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;

            // Ex: "1.234,56" -> "1234.56"
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static TimeSpan ParseDuracao(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return TimeSpan.Zero;

            // Esperado do input type="time": "HH:mm"
            if (TimeSpan.TryParseExact(valor.Trim(), @"hh\:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                out var ts))
                return ts;

            // fallback
            return TimeSpan.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }


        public void OnPost()
        {
            int id = 0;
            int.TryParse(Request.Form["id"], out id);

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            decimal preco = ParsePrecoBR(Request.Form["preco"]);
            
            decimal? precoCusto = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["precoCusto"]))
            {
                precoCusto = ParsePrecoBR(Request.Form["precoCusto"]);
            }

            decimal? margemContribuicao = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["margemContribuicao"]))
            {
                margemContribuicao = ParsePrecoBR(Request.Form["margemContribuicao"]);
            }

            TimeSpan duracao = ParseDuracao(Request.Form["duracao"]);

            decimal? aliquotaIss = null;
            if (!string.IsNullOrWhiteSpace(Request.Form["aliquotaISS"]))
            {
                aliquotaIss = ParsePrecoBR(Request.Form["aliquotaISS"]);
            }

            var servico = new Servico
            {
                IdServico = id,
                Nome = Request.Form["nome"],
                Preco = preco,
                PrecoCusto = precoCusto,
                MargemContribuicao = margemContribuicao,

                Duracao = duracao,
                IdSalao = idSalao,

                CodigoTributacaoMunicipio = Request.Form["codigoTributacaoMunicipio"],
                Cnae = Request.Form["cnae"].ToString()?.Replace(".", "").Replace("-", "").Replace("/", ""),
                AliquotaISS = aliquotaIss,

                Tags = Request.Form["tags"],
                Anotacoes = Request.Form["anotacoes"],
                ItemListaServicoLC116 = Request.Form["itemListaServicoLC116"],
                IdCnae = Request.Form["idCnae"],
                CodTributacaoNacional = Request.Form["codTributacaoNacional"],
                CodNBS = Request.Form["codNBS"],
                IdCategoria = string.IsNullOrWhiteSpace(Request.Form["idCategoria"]) ? (int?)null : int.Parse(Request.Form["idCategoria"]),
                Arquivado = Request.Form["arquivado"] == "on"
            };

            var handler = new ServicoHandler();

            if (id > 0)
            {
                handler.Atualizar(servico);
                Mensagem = "Serviço atualizado com sucesso!";
            }
            else
            {
                id = handler.CadastrarServico(servico);
                Mensagem = "Serviço cadastrado com sucesso!";
            }

            // Redirect back to edit mode
            Response.Redirect(HttpContext.Request.PathBase + $"/ServicoCadastro?id={id}&success=1");
        }

        public IActionResult OnPostSaveCategory(string nome, string descricao)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return new Microsoft.AspNetCore.Mvc.JsonResult(new { success = false, message = "Nome é obrigatório." });

            int idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
            var cat = new CategoriaProduto
            {
                Nome = nome,
                IdSalao = idSalao,
                Ativo = true,
                DataCadastro = System.DateTime.Now
            };

            var catHandler = new CategoriaProdutoHandler();
            int id = catHandler.CadastrarCategoria(cat);

            if (id > 0)
            {
                return new Microsoft.AspNetCore.Mvc.JsonResult(new { success = true, id = id, nome = nome });
            }

            return new Microsoft.AspNetCore.Mvc.JsonResult(new { success = false, message = "Erro ao cadastrar categoria." });
        }
    }
}

