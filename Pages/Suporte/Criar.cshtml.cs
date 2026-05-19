using System.Security.Claims;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Suporte;

[Authorize]
public class CriarModel : PageModel
{
    private readonly SuporteService _suporteService;

    public CriarModel(SuporteService suporteService)
    {
        _suporteService = suporteService;
    }

    [TempData] public string? FlashMessage { get; set; }
    [TempData] public string? FlashType { get; set; }

    public IActionResult OnGet() => RedirectToPage("/Dashboard");

    public async Task<IActionResult> OnPostAsync(string mensagem, string? returnUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mensagem))
            {
                throw new InvalidOperationException("Informe a mensagem do chamado de suporte.");
            }

            var idChamado = await _suporteService.RegistrarChamadoAsync(new SuporteChamado
            {
                IdSalao = ObterIdSalao(),
                NomeUsuario = User.Identity?.Name ?? User.FindFirst("Nome")?.Value,
                EmailUsuario = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirst("Email")?.Value,
                Mensagem = mensagem,
                UrlOrigem = string.IsNullOrWhiteSpace(returnUrl) ? Request.Headers.Referer.ToString() : returnUrl
            });

            FlashMessage = $"Chamado de suporte registrado. Codigo: {idChamado}.";
            FlashType = "success";
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage("/Dashboard");
    }

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;
}
