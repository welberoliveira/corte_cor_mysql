using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorteCor.Pages.CRM;

public class ClienteOportunidadesModel : CrmClientePageModelBase
{
    public ClienteOportunidadesModel(CrmService crmService) : base(crmService)
    {
    }

    [BindProperty(SupportsGet = true)]
    public int? IdOportunidadeEdicao { get; set; }

    [BindProperty]
    public CrmOportunidade OportunidadeInput { get; set; } = new();

    public List<CrmEtapaFunil> Etapas { get; private set; } = new();
    public List<CrmOportunidade> Oportunidades { get; private set; } = new();

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

            OportunidadeInput.IdPessoa = IdPessoa;
            CrmService.SalvarOportunidade(idSalao, OportunidadeInput);
            FlashMessage = OportunidadeInput.IdOportunidade > 0
                ? "Oportunidade atualizada com sucesso."
                : "Oportunidade criada com sucesso.";
            FlashType = "success";
            return RedirecionarParaCliente();
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            return RedirectToPage(new { idPessoa = IdPessoa, idOportunidadeEdicao = OportunidadeInput.IdOportunidade > 0 ? OportunidadeInput.IdOportunidade : IdOportunidadeEdicao });
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

        Etapas = CrmService.ListarEtapas(idSalao);
        Oportunidades = CrmService.ListarOportunidades(idSalao, IdPessoa, null);

        var oportunidadeEdicao = IdOportunidadeEdicao.HasValue
            ? Oportunidades.FirstOrDefault(item => item.IdOportunidade == IdOportunidadeEdicao.Value)
            : null;

        OportunidadeInput = oportunidadeEdicao != null
            ? new CrmOportunidade
            {
                IdOportunidade = oportunidadeEdicao.IdOportunidade,
                IdPessoa = oportunidadeEdicao.IdPessoa,
                IdSalao = oportunidadeEdicao.IdSalao,
                IdEtapa = oportunidadeEdicao.IdEtapa,
                Titulo = oportunidadeEdicao.Titulo,
                Descricao = oportunidadeEdicao.Descricao,
                ValorEstimado = oportunidadeEdicao.ValorEstimado,
                Probabilidade = oportunidadeEdicao.Probabilidade,
                Status = oportunidadeEdicao.Status,
                Origem = oportunidadeEdicao.Origem,
                PrevisaoFechamento = oportunidadeEdicao.PrevisaoFechamento,
                DataFechamento = oportunidadeEdicao.DataFechamento
            }
            : new CrmOportunidade
            {
                Status = CrmStatusOportunidade.Aberta,
                Probabilidade = 50,
                IdEtapa = Etapas.FirstOrDefault()?.IdEtapa ?? 0,
                PrevisaoFechamento = DateTime.Today.AddDays(30)
            };

        return null;
    }
}
