using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorteCor.Pages.CRM;

public class ClientePerfilModel : CrmClientePageModelBase
{
    public ClientePerfilModel(CrmService crmService) : base(crmService)
    {
    }

    [BindProperty]
    public CrmPessoaPerfil PerfilInput { get; set; } = new();

    public IActionResult OnGet()
    {
        var redirect = CarregarCliente();
        if (redirect != null)
        {
            return redirect;
        }

        PerfilInput = new CrmPessoaPerfil
        {
            IdPerfil = Detalhe.Perfil.IdPerfil,
            IdPessoa = Detalhe.Perfil.IdPessoa,
            IdSalao = Detalhe.Perfil.IdSalao,
            StatusRelacionamento = Detalhe.Perfil.StatusRelacionamento,
            OrigemLead = Detalhe.Perfil.OrigemLead,
            Temperatura = Detalhe.Perfil.Temperatura,
            ScoreRelacionamento = Detalhe.Perfil.ScoreRelacionamento,
            PermiteEmail = Detalhe.Perfil.PermiteEmail,
            PermiteSms = Detalhe.Perfil.PermiteSms,
            PermiteWhatsapp = Detalhe.Perfil.PermiteWhatsapp,
            NaoPerturbe = Detalhe.Perfil.NaoPerturbe,
            ProximaAcaoEm = Detalhe.Perfil.ProximaAcaoEm,
            ObservacoesInternas = Detalhe.Perfil.ObservacoesInternas
        };

        return Page();
    }

    public IActionResult OnPost()
    {
        try
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            PerfilInput.IdPessoa = IdPessoa;
            CrmService.SalvarPerfil(idSalao, PerfilInput);
            FlashMessage = "Perfil CRM salvo com sucesso.";
            FlashType = "success";
            return RedirecionarParaCliente();
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            return RedirectToPage(new { idPessoa = IdPessoa });
        }
    }
}
