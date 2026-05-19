using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LancamentoCadastroModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public LancamentoCadastroModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public List<PlanoContas> GruposPlano { get; private set; } = new();
        public List<PlanoContas> ContasPlano { get; private set; } = new();
        public List<ContaCaixa> Contas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public Guid? idTitulo { get; set; }

        [BindProperty]
        public int? GrupoPlanoContaId { get; set; }

        [BindProperty]
        public FinanceiroTitulo TituloInput { get; set; } = new()
        {
            Tipo = FinanceiroTipoTitulo.Receber,
            Origem = FinanceiroOrigemTitulo.Manual,
            Recorrencia = RecorrenciaTipo.Nenhuma,
            DataCompetencia = DateTime.Today,
            DataVencimento = DateTime.Today,
            Status = FinanceiroStatusTitulo.Aberto
        };

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        public async Task<JsonResult> OnGetContasFilhasAsync(int idGrupo, string? tipo)
        {
            var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupo, tipo);
            return new JsonResult(contas.Select(conta => new
            {
                id = conta.IdPlano,
                codigo = conta.Codigo,
                nome = conta.NomeExibicao,
                rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
            }));
        }

        public async Task<JsonResult> OnGetGruposPlanoAsync(string? tipo)
        {
            var grupos = await _financeiroService.ListarGruposPlanoContasAsync(ObterIdSalao(), tipo);
            return new JsonResult(grupos.Select(grupo => new
            {
                id = grupo.IdPlano,
                codigo = grupo.Codigo,
                nome = grupo.NomeExibicao,
                rotulo = $"{grupo.Codigo} - {grupo.NomeExibicao}"
            }));
        }

        public async Task<IActionResult> OnPostSalvarAsync()
        {
            try
            {
                var ids = await _financeiroService.SalvarTituloComRecorrenciaAsync(ObterIdSalao(), TituloInput);
                FlashMessage = ids.Count > 1
                    ? $"{ids.Count} lançamentos financeiros mensais foram criados para o ano corrente."
                    : "Lançamento financeiro salvo com sucesso.";
                FlashType = "success";
                return RedirectToPage("/Financeiro/Lancamentos");
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                await CarregarAsync(carregarTitulo: false);
                return Page();
            }
        }

        private async Task CarregarAsync(bool carregarTitulo = true)
        {
            var idSalao = ObterIdSalao();
            if (carregarTitulo && idTitulo.HasValue && idTitulo.Value != Guid.Empty)
            {
                TituloInput = await _financeiroService.ObterTituloAsync(idSalao, idTitulo.Value) ?? TituloInput;
            }

            GruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(idSalao, TituloInput.Tipo);
            Contas = await _financeiroService.ListarContasCaixaAsync(idSalao);

            if (!GrupoPlanoContaId.HasValue && TituloInput.IdPlano.HasValue)
            {
                GrupoPlanoContaId = (await _financeiroService.ObterGrupoNivel2DoPlanoAsync(idSalao, TituloInput.IdPlano))?.IdPlano;
            }

            if (GrupoPlanoContaId.HasValue && !GruposPlano.Any(grupo => grupo.IdPlano == GrupoPlanoContaId.Value))
            {
                GrupoPlanoContaId = null;
                TituloInput.IdPlano = null;
            }

            ContasPlano = GrupoPlanoContaId.HasValue && GrupoPlanoContaId > 0
                ? await _financeiroService.ListarContasAnaliticasPorGrupoAsync(idSalao, GrupoPlanoContaId.Value, TituloInput.Tipo)
                : new List<PlanoContas>();

            if (TituloInput.IdPlano.HasValue && !ContasPlano.Any(plano => plano.IdPlano == TituloInput.IdPlano.Value))
            {
                TituloInput.IdPlano = null;
            }
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
