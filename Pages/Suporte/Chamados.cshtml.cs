using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Suporte;

[Authorize]
public class ChamadosModel : PageModel
{
    private readonly SuporteService _suporteService;

    public ChamadosModel(SuporteService suporteService)
    {
        _suporteService = suporteService;
    }

    public PagedResult<SuporteChamado> Chamados { get; private set; } = new();
    public IReadOnlyList<string> StatusDisponiveis => SuporteChamadoStatus.Todos;
    public bool IsAdministrador => User.HasClaim(c => c.Type == "Role" && c.Value == "Admin");

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Pesquisa { get; set; }

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

    public async Task<IActionResult> OnPostAtualizarStatusAsync(Guid idChamado, string statusNovo)
    {
        if (!IsAdministrador)
        {
            FlashMessage = "Somente usuário administrador pode alterar status de chamados.";
            FlashType = "danger";
            return RedirectToPage(new { Status, Pesquisa, p });
        }

        try
        {
            await _suporteService.AtualizarStatusChamadoAsync(ObterIdSalao(), idChamado, statusNovo);
            FlashMessage = "Status do chamado atualizado.";
            FlashType = "success";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        return RedirectToPage(new { Status, Pesquisa, p });
    }

    private async Task CarregarAsync()
    {
        Chamados = await _suporteService.ListarChamadosAsync(ObterIdSalao(), new SuporteChamadoFiltro
        {
            Status = Status,
            Pesquisa = Pesquisa,
            PageIndex = p > 0 ? p : 1,
            PageSize = 15
        });
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    public string ObterNomeUsuarioAtual() =>
        User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value ?? string.Empty;
}
