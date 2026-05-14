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
        public List<PlanoContas> Planos { get; private set; } = new();
        public List<ContaCaixa> Contas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Tipo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Pesquisa { get; set; }

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

            return RedirectToPage(new { Tipo, Status, Pesquisa, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostReabrirAsync(Guid id)
        {
            await _financeiroService.ReabrirTituloAsync(ObterIdSalao(), id);
            FlashMessage = "Titulo reaberto.";
            FlashType = "warning";
            return RedirectToPage(new { Tipo, Status, Pesquisa, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostCancelarAsync(Guid id)
        {
            await _financeiroService.CancelarTituloAsync(ObterIdSalao(), id);
            FlashMessage = "Titulo cancelado.";
            FlashType = "secondary";
            return RedirectToPage(new { Tipo, Status, Pesquisa, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        public async Task<IActionResult> OnPostConciliarAsync(Guid id, bool conciliado)
        {
            await _financeiroService.AlternarConciliacaoAsync(ObterIdSalao(), id, conciliado);
            FlashMessage = conciliado ? "Titulo conciliado." : "Conciliacao removida.";
            FlashType = "info";
            return RedirectToPage(new { Tipo, Status, Pesquisa, IdPlano, IdConta, DataInicio, DataFim, SomenteVencidos, p });
        }

        private async Task CarregarAsync()
        {
            var idSalao = ObterIdSalao();
            Planos = await _financeiroService.ListarPlanoContasAsync(idSalao);
            Contas = await _financeiroService.ListarContasCaixaAsync(idSalao);
            Titulos = await _financeiroService.ListarTitulosAsync(idSalao, new FinanceiroTituloFiltro
            {
                Tipo = Tipo,
                Status = Status,
                Pesquisa = Pesquisa,
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
