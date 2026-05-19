using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LancamentosModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public LancamentosModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public PagedResult<FinanceiroTitulo> Titulos { get; private set; } = new();
        public List<PlanoContas> GruposPlano { get; private set; } = new();
        public List<PlanoContas> ContasPlano { get; private set; } = new();
        public List<ContaCaixa> Contas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Tipo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Recorrencia { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Pesquisa { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdGrupoPlano { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdPlano { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdConta { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool SomenteVencidos { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        public async Task<IActionResult> OnPostLiquidarAsync(Guid id, decimal? valorLiquidado, DateTime? dataLiquidacao, bool conciliado)
        {
            try
            {
                await _financeiroService.LiquidarTituloAsync(ObterIdSalao(), id, valorLiquidado, dataLiquidacao, conciliado);
                FlashMessage = "Titulo liquidado com sucesso.";
                FlashType = "success";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { Tipo, Status, Recorrencia, Pesquisa, IdGrupoPlano, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostReabrirAsync(Guid id)
        {
            await _financeiroService.ReabrirTituloAsync(ObterIdSalao(), id);
            FlashMessage = "Titulo reaberto.";
            FlashType = "warning";
            return RedirectToPage(new { Tipo, Status, Recorrencia, Pesquisa, IdGrupoPlano, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostCancelarAsync(Guid id)
        {
            try
            {
                FlashMessage = await _financeiroService.CancelarTituloAsync(ObterIdSalao(), id);
                FlashType = "secondary";
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
            }

            return RedirectToPage(new { Tipo, Status, Recorrencia, Pesquisa, IdGrupoPlano, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostConciliarAsync(Guid id, bool conciliado)
        {
            await _financeiroService.AlternarConciliacaoAsync(ObterIdSalao(), id, conciliado);
            FlashMessage = conciliado ? "Titulo conciliado." : "Conciliacao removida.";
            FlashType = "info";
            return RedirectToPage(new { Tipo, Status, Recorrencia, Pesquisa, IdGrupoPlano, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<JsonResult> OnGetContasFilhasAsync(int idGrupo, string? tipo)
        {
            var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupo, tipo ?? Tipo);
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

        private async Task CarregarAsync()
        {
            var idSalao = ObterIdSalao();
            GruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(idSalao, Tipo);
            Contas = await _financeiroService.ListarContasCaixaAsync(idSalao);
            if (!IdGrupoPlano.HasValue && IdPlano.HasValue)
            {
                IdGrupoPlano = (await _financeiroService.ObterGrupoNivel2DoPlanoAsync(idSalao, IdPlano))?.IdPlano;
            }

            if (IdGrupoPlano.HasValue && !GruposPlano.Any(grupo => grupo.IdPlano == IdGrupoPlano.Value))
            {
                IdGrupoPlano = null;
                IdPlano = null;
            }

            ContasPlano = IdGrupoPlano.HasValue && IdGrupoPlano > 0
                ? await _financeiroService.ListarContasAnaliticasPorGrupoAsync(idSalao, IdGrupoPlano.Value, Tipo)
                : new List<PlanoContas>();

            if (IdPlano.HasValue && !ContasPlano.Any(plano => plano.IdPlano == IdPlano.Value))
            {
                IdPlano = null;
            }

            Titulos = await _financeiroService.ListarTitulosAsync(idSalao, new FinanceiroTituloFiltro
            {
                Tipo = Tipo,
                Status = Status,
                Recorrencia = Recorrencia,
                Pesquisa = Pesquisa,
                IdGrupoPlano = IdGrupoPlano,
                IdPlano = IdPlano,
                IdConta = IdConta,
                DataInicio = DataInicio,
                DataFim = DataFim,
                SomenteVencidos = SomenteVencidos,
                PageIndex = p > 0 ? p : 1,
                PageSize = 15
            });
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
