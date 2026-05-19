using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using CorteCor;
using Microsoft.Extensions.Logging;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloSMSCadastroModel : PageModel
    {
        private readonly ModeloSMSHandler _handler;
        private readonly ILogger<ModeloSMSCadastroModel> _logger;

        public ModeloSMSCadastroModel(ModeloSMSHandler handler, ILogger<ModeloSMSCadastroModel> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [BindProperty]
        public ModeloSMS Modelo { get; set; } = new();

        public List<SelectListItem> EventosOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Boas Vindas", "BoasVindas"),
            new SelectListItem("Confirmação de Agendamento", "ConfirmacaoAgendamento"),
            new SelectListItem("Lembrete de Agendamento", "LembreteAgendamento"),
            //new SelectListItem("Cancelamento de Agendamento", "CancelamentoAgendamento"), // Removed as backend logic doesn't support yet in LembreteService? Actually LembreteService only does reminders. But let's include it for future use.
            new SelectListItem("Lembrete de Pagamento", "LembretePagamento")
        };

        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                if (id.HasValue && id.Value > 0)
                {
                    try
                    {
                        Modelo = _handler.ObterPorId(id.Value, idSalao);
                        if (Modelo == null) Mensagem = "Modelo nao encontrado.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao carregar modelo de SMS {IdModelo} para sala {IdSalao}.", id.Value, idSalao);
                        Modelo = new ModeloSMS { Ativo = true, IdSalao = idSalao };
                        Mensagem = "Nao foi possivel carregar o modelo de SMS.";
                    }
                }
                else
                {
                    Modelo = new ModeloSMS { Ativo = true, IdSalao = idSalao };
                }
            }
            else
            {
                Mensagem = "Erro ao identificar a empresa.";
            }
        }

        public IActionResult OnPost()
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                Modelo.IdSalao = idSalao;

                try
                {
                    Modelo.TipoEvento ??= string.Empty;
                    Modelo.Conteudo ??= string.Empty;
                    _handler.Salvar(Modelo);
                    return RedirectToPage("/ModeloSMSLista");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar modelo de SMS para sala {IdSalao}.", idSalao);
                    Mensagem = "Nao foi possivel salvar o modelo de SMS. Tente novamente em instantes.";
                    return Page();
                }
            }
            return Page();
        }
    }
}


