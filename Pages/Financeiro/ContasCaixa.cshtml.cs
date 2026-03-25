using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ContasCaixaModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public ContasCaixaModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public List<ContaCaixa> Contas { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        [BindProperty]
        public ContaCaixa ContaInput { get; set; } = new() { Ativo = true, Tipo = "Caixa" };

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
                await _financeiroService.SaveContaCaixaAsync(ObterIdSalao(), ContaInput);
                FlashMessage = "Conta caixa salva com sucesso.";
                FlashType = "success";
                return RedirectToPage();
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
            Contas = await _financeiroService.ListarContasCaixaAsync(ObterIdSalao());
            if (id.HasValue)
            {
                ContaInput = Contas.FirstOrDefault(c => c.IdConta == id.Value) ?? ContaInput;
            }
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
