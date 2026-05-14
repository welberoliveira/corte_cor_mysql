using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM;

[Authorize(Policy = "UsuarioPolicy")]
public abstract class CrmClientePageModelBase : PageModel
{
    protected readonly CrmService CrmService;

    protected CrmClientePageModelBase(CrmService crmService)
    {
        CrmService = crmService;
    }

    [BindProperty(SupportsGet = true)]
    public int IdPessoa { get; set; }

    public CrmClienteDetalhe Detalhe { get; protected set; } = new();

    [TempData]
    public string? FlashMessage { get; set; }

    [TempData]
    public string? FlashType { get; set; }

    protected IActionResult? CarregarCliente()
    {
        if (!TryObterIdSalao(out var idSalao))
        {
            return RedirectToPage("/Index");
        }

        if (IdPessoa <= 0)
        {
            return RedirectToPage("/CRM/Index");
        }

        Detalhe = CrmService.ObterClienteDetalhe(idSalao, IdPessoa);
        return null;
    }

    protected RedirectToPageResult RedirecionarParaCliente()
    {
        return RedirectToPage("/CRM/Cliente", new { idPessoa = IdPessoa });
    }

    protected bool TryObterIdSalao(out int idSalao)
    {
        return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
    }

    protected int? ObterIdUsuario()
    {
        return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var idUsuario) && idUsuario > 0
            ? idUsuario
            : null;
    }
}
