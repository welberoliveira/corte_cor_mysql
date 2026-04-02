using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorteCor.Pages.CRM;

public class ClienteInteracoesModel : CrmClientePageModelBase
{
    public ClienteInteracoesModel(CrmService crmService) : base(crmService)
    {
    }

    [BindProperty(SupportsGet = true)]
    public int? IdInteracaoEdicao { get; set; }

    [BindProperty]
    public CrmInteracao InteracaoInput { get; set; } = new();

    public List<CrmInteracao> Interacoes { get; private set; } = new();

    public IActionResult OnGet()
    {
        var redirect = CarregarPagina();
        return redirect ?? Page();
    }

    public IActionResult OnPost()
    {
        try
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            InteracaoInput.IdPessoa = IdPessoa;
            InteracaoInput.IdUsuario ??= ObterIdUsuario();
            CrmService.SalvarInteracao(idSalao, InteracaoInput);
            FlashMessage = InteracaoInput.IdInteracao > 0
                ? "Interacao atualizada com sucesso."
                : "Interacao registrada com sucesso.";
            FlashType = "success";
            return RedirecionarParaCliente();
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            return RedirectToPage(new { idPessoa = IdPessoa, idInteracaoEdicao = InteracaoInput.IdInteracao > 0 ? InteracaoInput.IdInteracao : IdInteracaoEdicao });
        }
    }

    private IActionResult? CarregarPagina()
    {
        var redirect = CarregarCliente();
        if (redirect != null)
        {
            return redirect;
        }

        if (!TryObterIdSalao(out var idSalao))
        {
            return RedirectToPage("/Index");
        }

        Interacoes = CrmService.ListarInteracoes(idSalao, IdPessoa, 50);
        var interacaoEdicao = IdInteracaoEdicao.HasValue
            ? Interacoes.FirstOrDefault(item => item.IdInteracao == IdInteracaoEdicao.Value)
            : null;

        InteracaoInput = interacaoEdicao != null
            ? new CrmInteracao
            {
                IdInteracao = interacaoEdicao.IdInteracao,
                IdSalao = interacaoEdicao.IdSalao,
                IdPessoa = interacaoEdicao.IdPessoa,
                IdUsuario = interacaoEdicao.IdUsuario,
                Canal = interacaoEdicao.Canal,
                Tipo = interacaoEdicao.Tipo,
                Assunto = interacaoEdicao.Assunto,
                Descricao = interacaoEdicao.Descricao,
                DataInteracao = interacaoEdicao.DataInteracao,
                Referencia = interacaoEdicao.Referencia,
                OrigemSistema = interacaoEdicao.OrigemSistema
            }
            : new CrmInteracao
            {
                Canal = CrmCanal.Telefone,
                Tipo = "Manual",
                DataInteracao = DateTime.Now,
                IdPessoa = IdPessoa,
                IdUsuario = ObterIdUsuario()
            };

        return null;
    }
}
