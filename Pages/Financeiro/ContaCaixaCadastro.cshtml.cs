using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ContaCaixaCadastroModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public ContaCaixaCadastroModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

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
                return RedirectToPage("/Financeiro/ContasCaixa");
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
            if (id.HasValue)
            {
                var contas = await _financeiroService.ListarContasCaixaAsync(ObterIdSalao());
                ContaInput = contas.FirstOrDefault(c => c.IdConta == id.Value) ?? ContaInput;
            }
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
