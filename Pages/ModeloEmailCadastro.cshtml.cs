using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloEmailCadastroModel : PageModel
    {
        private readonly ModeloEmailHandler _handler;

        public ModeloEmailCadastroModel(ModeloEmailHandler handler)
        {
            _handler = handler;
        }

        [BindProperty]
        public ModeloEmail Modelo { get; set; }

        public List<SelectListItem> EventosOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Boas Vindas", "BoasVindas"),
            new SelectListItem("ConfirmaÃ§Ã£o de Agendamento", "ConfirmacaoAgendamento"),
            new SelectListItem("Lembrete de Agendamento", "LembreteAgendamento"),
            new SelectListItem("Cancelamento de Agendamento", "CancelamentoAgendamento"),
            new SelectListItem("Lembrete de Pagamento", "LembretePagamento")
        };

        public string Mensagem { get; set; }

        public void OnGet(int? id)
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                if (id.HasValue)
                {
                    Modelo = _handler.ObterPorId(id.Value, idSalao);
                    if (Modelo == null) Mensagem = "Modelo nÃ£o encontrado.";
                }
                else
                {
                    Modelo = new ModeloEmail { Ativo = true, IdSalao = idSalao };
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

                // Validate if event type already exists? Ideally yes, but let's allow overwrite logic or multiple same event types (though usually 1 per event).
                // Database script didn't enforce UNIQUE constraint on (IdSalao, TipoEvento), just an index.
                // Assuming it's fine for now, or the handler will handle it.

                if (Modelo.IdModelo > 0)
                {
                    _handler.Atualizar(Modelo);
                    Mensagem = "Modelo atualizado com sucesso!";
                }
                else
                {
                    _handler.Cadastrar(Modelo);
                    Mensagem = "Modelo cadastrado com sucesso!";
                }
                return RedirectToPage("/ModeloEmailLista");
            }
            return Page();
        }
    }
}


