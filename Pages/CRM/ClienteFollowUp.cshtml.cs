using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Mvc;

namespace CorteCor.Pages.CRM;

public class ClienteFollowUpModel : CrmClientePageModelBase
{
    public ClienteFollowUpModel(CrmService crmService) : base(crmService)
    {
    }

    [BindProperty(SupportsGet = true)]
    public int? IdTarefaEdicao { get; set; }

    [BindProperty]
    public CrmTarefa TarefaInput { get; set; } = new();

    public List<CrmTarefa> Tarefas { get; private set; } = new();

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

            TarefaInput.IdPessoa = IdPessoa;
            TarefaInput.IdUsuarioResponsavel ??= ObterIdUsuario();
            CrmService.SalvarTarefa(idSalao, TarefaInput);
            FlashMessage = TarefaInput.IdTarefa > 0
                ? "Acompanhamento atualizado com sucesso."
                : "Acompanhamento criado com sucesso.";
            FlashType = "success";
            return RedirecionarParaCliente();
        }
        catch (Exception ex)
        {
            FlashMessage = ex.Message;
            FlashType = "danger";
            return RedirectToPage(new { idPessoa = IdPessoa, idTarefaEdicao = TarefaInput.IdTarefa > 0 ? TarefaInput.IdTarefa : IdTarefaEdicao });
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

        Tarefas = CrmService.ListarTarefas(idSalao, IdPessoa, null, null, 1, 50).Items;
        var tarefaEdicao = IdTarefaEdicao.HasValue
            ? Tarefas.FirstOrDefault(item => item.IdTarefa == IdTarefaEdicao.Value)
            : null;

        TarefaInput = tarefaEdicao != null
            ? new CrmTarefa
            {
                IdTarefa = tarefaEdicao.IdTarefa,
                IdSalao = tarefaEdicao.IdSalao,
                IdPessoa = tarefaEdicao.IdPessoa,
                IdUsuarioResponsavel = tarefaEdicao.IdUsuarioResponsavel,
                Titulo = tarefaEdicao.Titulo,
                Descricao = tarefaEdicao.Descricao,
                Prioridade = tarefaEdicao.Prioridade,
                Status = tarefaEdicao.Status,
                CanalSugerido = tarefaEdicao.CanalSugerido,
                DataVencimento = tarefaEdicao.DataVencimento,
                DataConclusao = tarefaEdicao.DataConclusao
            }
            : new CrmTarefa
            {
                IdPessoa = IdPessoa,
                Prioridade = "Media",
                Status = CrmStatusTarefa.Aberta,
                DataVencimento = DateTime.Now.AddDays(1),
                IdUsuarioResponsavel = ObterIdUsuario()
            };

        return null;
    }
}
