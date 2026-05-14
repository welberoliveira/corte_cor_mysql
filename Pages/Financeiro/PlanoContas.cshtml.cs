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

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            Planos = await _financeiroService.ListarPlanoContasAsync(ObterIdSalao());
        }

        private int ObterIdSalao()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
        }
    }
}
