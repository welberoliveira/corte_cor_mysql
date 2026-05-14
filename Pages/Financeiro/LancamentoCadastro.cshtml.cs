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

        public List<PlanoContas> Planos { get; private set; } = new();
        public List<ContaCaixa> Contas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public Guid? idTitulo { get; set; }

        [BindProperty]
        public FinanceiroTitulo TituloInput { get; set; } = new()
        {
            Tipo = FinanceiroTipoTitulo.Receber,
            Origem = FinanceiroOrigemTitulo.Manual,
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

        public async Task<IActionResult> OnPostSalvarAsync()
        {
            try
            {
                await _financeiroService.SalvarTituloAsync(ObterIdSalao(), TituloInput);
                FlashMessage = "Lançamento financeiro salvo com sucesso.";
                FlashType = "success";
                return RedirectToPage("/Financeiro/Lancamentos");
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                await CarregarAsync();
                return Page();
            }
        }

        private async Task CarregarAsync()
        {
            var idSalao = ObterIdSalao();
            Planos = await _financeiroService.ListarPlanoContasAsync(idSalao);
            Contas = await _financeiroService.ListarContasCaixaAsync(idSalao);

            if (idTitulo.HasValue && idTitulo.Value != Guid.Empty)
            {
                TituloInput = await _financeiroService.ObterTituloAsync(idSalao, idTitulo.Value) ?? TituloInput;
            }
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
