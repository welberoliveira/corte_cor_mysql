using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ClienteModel : PageModel
    {
        private readonly CrmService _crmService;

        public ClienteModel(CrmService crmService)
        {
            _crmService = crmService;
        }

        [BindProperty(SupportsGet = true)]
        public int IdPessoa { get; set; }

        [BindProperty]
        public CrmPessoaPerfil PerfilInput { get; set; } = new();

        [BindProperty]
        public CrmInteracao InteracaoInput { get; set; } = new();

        [BindProperty]
        public CrmTarefa TarefaInput { get; set; } = new() { Prioridade = "Media", Status = CrmStatusTarefa.Aberta };

        [BindProperty]
        public CrmOportunidade OportunidadeInput { get; set; } = new() { Status = CrmStatusOportunidade.Aberta, Probabilidade = 50 };

        public CrmClienteDetalhe Detalhe { get; private set; } = new();
        public List<CrmEtapaFunil> Etapas { get; private set; } = new();

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            return CarregarPagina();
        }

        public IActionResult OnPostSalvarPerfil()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                PerfilInput.IdPessoa = IdPessoa;
                _crmService.SalvarPerfil(idSalao, PerfilInput);
                FlashMessage = "Perfil CRM atualizado com sucesso.";
                FlashType = "success";
                return RedirectToPage(new { IdPessoa });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage(new { IdPessoa });
            }
        }

        public IActionResult OnPostRegistrarInteracao()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                InteracaoInput.IdPessoa = IdPessoa;
                InteracaoInput.IdUsuario = ObterIdUsuario();
                _crmService.RegistrarInteracao(idSalao, InteracaoInput);
                FlashMessage = "Interação registrada com sucesso.";
                FlashType = "success";
                return RedirectToPage(new { IdPessoa });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage(new { IdPessoa });
            }
        }

        public IActionResult OnPostSalvarTarefa()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                TarefaInput.IdPessoa = IdPessoa;
                if (!TarefaInput.IdUsuarioResponsavel.HasValue || TarefaInput.IdUsuarioResponsavel <= 0)
                {
                    TarefaInput.IdUsuarioResponsavel = ObterIdUsuario();
                }

                _crmService.SalvarTarefa(idSalao, TarefaInput);
                FlashMessage = "Tarefa CRM criada com sucesso.";
                FlashType = "success";
                return RedirectToPage(new { IdPessoa });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage(new { IdPessoa });
            }
        }

        public IActionResult OnPostSalvarOportunidade()
        {
            try
            {
                if (!TryObterIdSalao(out var idSalao))
                {
                    return RedirectToPage("/Index");
                }

                OportunidadeInput.IdPessoa = IdPessoa;
                _crmService.SalvarOportunidade(idSalao, OportunidadeInput);
                FlashMessage = "Oportunidade salva com sucesso.";
                FlashType = "success";
                return RedirectToPage(new { IdPessoa });
            }
            catch (Exception ex)
            {
                FlashMessage = ex.Message;
                FlashType = "danger";
                return RedirectToPage(new { IdPessoa });
            }
        }

        private IActionResult CarregarPagina()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            if (IdPessoa <= 0)
            {
                return RedirectToPage("/CRM/Index");
            }

            Detalhe = _crmService.ObterClienteDetalhe(idSalao, IdPessoa);
            Etapas = _crmService.ListarEtapas(idSalao);

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

            InteracaoInput = new CrmInteracao { Canal = CrmCanal.Telefone, Tipo = "Manual" };
            TarefaInput = new CrmTarefa
            {
                Prioridade = "Media",
                Status = CrmStatusTarefa.Aberta,
                DataVencimento = DateTime.Now.AddDays(1)
            };
            OportunidadeInput = new CrmOportunidade
            {
                Status = CrmStatusOportunidade.Aberta,
                Probabilidade = 50,
                IdEtapa = Etapas.FirstOrDefault()?.IdEtapa ?? 0,
                PrevisaoFechamento = DateTime.Today.AddDays(30)
            };

            return Page();
        }

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }

        private int? ObterIdUsuario()
        {
            return int.TryParse(User.FindFirst("IdUsuario")?.Value, out var idUsuario) && idUsuario > 0
                ? idUsuario
                : null;
        }
    }
}
