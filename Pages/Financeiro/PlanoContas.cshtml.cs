using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Financeiro
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PlanoContasModel : PageModel
    {
        private readonly FinanceiroService _financeiroService;

        public PlanoContasModel(FinanceiroService financeiroService)
        {
            _financeiroService = financeiroService;
        }

        public List<PlanoContas> Planos { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? id { get; set; }

        [BindProperty]
        public PlanoContas PlanoInput { get; set; } = new() { Ativo = true, Tipo = "R" };

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
                await _financeiroService.SavePlanoContasAsync(ObterIdSalao(), PlanoInput);
                FlashMessage = "Plano de contas salvo com sucesso.";
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
            Planos = await _financeiroService.ListarPlanoContasAsync(ObterIdSalao());
            if (id.HasValue)
            {
                PlanoInput = Planos.FirstOrDefault(p => p.IdPlano == id.Value) ?? PlanoInput;
            }
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
