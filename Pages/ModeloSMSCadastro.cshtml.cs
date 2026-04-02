using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using CorteCor;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloSMSCadastroModel : PageModel
    {
        private readonly ModeloSMSHandler _handler;

        public ModeloSMSCadastroModel(ModeloSMSHandler handler)
        {
            _handler = handler;
        }

        [BindProperty]
        public ModeloSMS Modelo { get; set; }

        public List<SelectListItem> EventosOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Boas Vindas", "BoasVindas"),
            new SelectListItem("ConfirmaÃ§Ã£o de Agendamento", "ConfirmacaoAgendamento"),
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
                    Modelo = _handler.ObterPorId(id.Value, idSalao);
                    if (Modelo == null) Mensagem = "Modelo nÃ£o encontrado.";
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

                if (Modelo.IdModelo > 0)
                {
                    _handler.Salvar(Modelo); // Salvar handles update if Id > 0
                    Mensagem = "Modelo atualizado com sucesso!";
                }
                else
                {
                    _handler.Salvar(Modelo);
                    Mensagem = "Modelo cadastrado com sucesso!";
                }
                return RedirectToPage("/ModeloSMSLista");
            }
            return Page();
        }
    }
}


