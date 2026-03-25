using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ServicoCadastroModel : PageModel
    {
        private readonly ServicoHandler _servicoHandler;
        private readonly CategoriaProdutoHandler _categoriaHandler;
        private readonly ItemListaServicoHandler _itemListaServicoHandler;

        public Servico Servico { get; set; } = new();
        public List<ItemListaServico> ItensLC116 { get; set; } = new();
        public List<CategoriaProduto> Categorias { get; set; } = new();
        public string ButtonText { get; set; } = "Cadastrar";
        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";
        public bool ServicoFiscalIncompleto { get; set; }

        public ServicoCadastroModel(
            ServicoHandler servicoHandler,
            CategoriaProdutoHandler categoriaHandler,
            ItemListaServicoHandler itemListaServicoHandler)
        {
            _servicoHandler = servicoHandler;
            _categoriaHandler = categoriaHandler;
            _itemListaServicoHandler = itemListaServicoHandler;
        }

        public void OnGet(int? id)
        {
            CarregarListas();

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Não foi possível identificar o salão atual.";
                MensagemTipo = "danger";
                return;
            }

            if (id.HasValue)
            {
                Servico = _servicoHandler.ObterPorIdESalao(id.Value, idSalao) ?? new Servico();
                if (Servico.IdServico > 0)
                {
                    ButtonText = "Atualizar";
                }
            }

            AtualizarAvisoFiscal();
        }

        private void CarregarListas()
        {
            ItensLC116 = _itemListaServicoHandler.Listar() ?? new List<ItemListaServico>();
            if (TryObterIdSalao(out var idSalao))
            {
                Categorias = _categoriaHandler.ListarPorSalao(idSalao)?.Where(c => c.Ativo).ToList() ?? new List<CategoriaProduto>();
            }
        }

        private static decimal ParsePrecoBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, CultureInfo.InvariantCulture);
        }

        private static TimeSpan ParseDuracao(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return TimeSpan.Zero;
            if (TimeSpan.TryParseExact(valor.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var ts))
            {
                return ts;
            }

            return TimeSpan.Parse(valor, CultureInfo.InvariantCulture);
        }

        public IActionResult OnPost()
        {
            CarregarListas();

            if (!TryObterIdSalao(out var idSalao))
            {
                Mensagem = "Não foi possível identificar o salão atual.";
                MensagemTipo = "danger";
                return Page();
            }

            int.TryParse(Request.Form["id"], out var id);

            decimal preco = ParsePrecoBR(Request.Form["preco"]);
            decimal? precoCusto = string.IsNullOrWhiteSpace(Request.Form["precoCusto"]) ? null : ParsePrecoBR(Request.Form["precoCusto"]);
            decimal? margemContribuicao = string.IsNullOrWhiteSpace(Request.Form["margemContribuicao"]) ? null : ParsePrecoBR(Request.Form["margemContribuicao"]);
            decimal? aliquotaIss = string.IsNullOrWhiteSpace(Request.Form["aliquotaISS"]) ? null : ParsePrecoBR(Request.Form["aliquotaISS"]);

            Servico = new Servico
            {
                IdServico = id,
                Nome = Request.Form["nome"].ToString().Trim(),
                Preco = preco,
                PrecoCusto = precoCusto,
                MargemContribuicao = margemContribuicao,
                Duracao = ParseDuracao(Request.Form["duracao"]),
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

            ButtonText = Servico.IdServico > 0 ? "Atualizar" : "Cadastrar";
            AtualizarAvisoFiscal();

            if (!ValidarServico())
            {
                return Page();
            }

            if (Servico.IdServico > 0)
            {
                _servicoHandler.Atualizar(Servico);
                Mensagem = "Serviço atualizado com sucesso.";
            }
            else
            {
                Servico.IdServico = _servicoHandler.CadastrarServico(Servico);
                Mensagem = "Serviço cadastrado com sucesso.";
                ButtonText = "Atualizar";
            }

            MensagemTipo = "success";
            AtualizarAvisoFiscal();
            return Page();
        }

        public IActionResult OnPostSaveCategory(string nome, string descricao)
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return new JsonResult(new { success = false, message = "Salão não identificado." });
            }

            nome = (nome ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(nome))
            {
                return new JsonResult(new { success = false, message = "Nome é obrigatório." });
            }

            if (_categoriaHandler.ExisteNomePorSalao(nome, idSalao))
            {
                return new JsonResult(new { success = false, message = "Já existe uma categoria com esse nome." });
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

        private bool ValidarServico()
        {
            if (string.IsNullOrWhiteSpace(Servico.Nome))
            {
                Mensagem = "Informe o nome do serviço.";
                MensagemTipo = "warning";
                return false;
            }

            if (_servicoHandler.ExisteNomePorSalao(Servico.Nome, Servico.IdSalao, Servico.IdServico > 0 ? Servico.IdServico : null))
            {
                Mensagem = "Já existe um serviço com esse nome.";
                MensagemTipo = "warning";
                return false;
            }

            if (Servico.Preco < 0)
            {
                Mensagem = "O preço de venda não pode ser negativo.";
                MensagemTipo = "warning";
                return false;
            }

            if (Servico.PrecoCusto.HasValue && Servico.PrecoCusto < 0)
            {
                Mensagem = "O preço de custo não pode ser negativo.";
                MensagemTipo = "warning";
                return false;
            }

            if (Servico.Duracao <= TimeSpan.Zero)
            {
                Mensagem = "Informe uma duração maior que zero.";
                MensagemTipo = "warning";
                return false;
            }

            if (Servico.AliquotaISS.HasValue && (Servico.AliquotaISS < 0 || Servico.AliquotaISS > 100))
            {
                Mensagem = "A alíquota ISS deve estar entre 0 e 100.";
                MensagemTipo = "warning";
                return false;
            }

            var preencheuAlgumCampoFiscal = !string.IsNullOrWhiteSpace(Servico.CodigoTributacaoMunicipio)
                || !string.IsNullOrWhiteSpace(Servico.CodTributacaoNacional)
                || !string.IsNullOrWhiteSpace(Servico.ItemListaServicoLC116)
                || !string.IsNullOrWhiteSpace(Servico.CodNBS)
                || !string.IsNullOrWhiteSpace(Servico.Cnae)
                || Servico.AliquotaISS.HasValue;

            if (preencheuAlgumCampoFiscal)
            {
                if (string.IsNullOrWhiteSpace(Servico.CodTributacaoNacional) || string.IsNullOrWhiteSpace(Servico.ItemListaServicoLC116))
                {
                    Mensagem = "Para usar o serviço na emissão fiscal, preencha o código de tributação nacional e o item da LC 116/03.";
                    MensagemTipo = "warning";
                    return false;
                }
            }

            return true;
        }

        private void AtualizarAvisoFiscal()
        {
            ServicoFiscalIncompleto = !Servico.Arquivado
                && (string.IsNullOrWhiteSpace(Servico.CodTributacaoNacional)
                    || string.IsNullOrWhiteSpace(Servico.ItemListaServicoLC116));
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}

