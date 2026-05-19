using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloEmailCadastroModel : PageModel
    {
        private readonly ModeloEmailHandler _handler;
        private readonly ILogger<ModeloEmailCadastroModel> _logger;

        public ModeloEmailCadastroModel(ModeloEmailHandler handler, ILogger<ModeloEmailCadastroModel> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        [BindProperty]
        public ModeloEmail Modelo { get; set; } = new();

        public List<SelectListItem> EventosOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem("Boas Vindas", "BoasVindas"),
            new SelectListItem("Confirmação de Agendamento", "ConfirmacaoAgendamento"),
            new SelectListItem("Lembrete de Agendamento", "LembreteAgendamento"),
            new SelectListItem("Cancelamento de Agendamento", "CancelamentoAgendamento"),
            new SelectListItem("Lembrete de Pagamento", "LembretePagamento")
        };

        public string Mensagem { get; set; }
        public int? ModeloExistenteId { get; set; }

        public void OnGet(int? id)
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                if (id.HasValue)
                {
                    Modelo = _handler.ObterPorId(id.Value, idSalao);
                    if (Modelo == null) Mensagem = "Modelo não encontrado.";
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
                try
                {
                    Modelo.IdSalao = idSalao;
                    Modelo.TipoEvento ??= string.Empty;
                    Modelo.Assunto ??= string.Empty;
                    Modelo.CorpoHTML ??= string.Empty;

                    var existente = _handler.ObterPorEventoIncluindoInativos(idSalao, Modelo.TipoEvento, Modelo.IdModelo > 0 ? Modelo.IdModelo : null);
                    if (existente != null)
                    {
                        ModeloExistenteId = existente.IdModelo;
                        Mensagem = "Ja existe um modelo de e-mail criado para este evento. Edite o modelo existente ou escolha outro evento.";
                        return Page();
                    }

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
                catch (Exception ex) when (ModeloEmailHandler.IsDuplicateKeyException(ex))
                {
                    _logger.LogError(ex, "Tentativa duplicada de modelo de e-mail para sala {IdSalao} e evento {TipoEvento}.", idSalao, Modelo.TipoEvento);
                    try
                    {
                        var modeloExistente = _handler.ObterPorEventoIncluindoInativos(idSalao, Modelo.TipoEvento);
                        ModeloExistenteId = modeloExistente?.IdModelo;
                    }
                    catch (Exception lookupEx)
                    {
                        _logger.LogError(lookupEx, "Erro ao localizar modelo de e-mail duplicado para sala {IdSalao} e evento {TipoEvento}.", idSalao, Modelo.TipoEvento);
                    }
                    Mensagem = "Ja existe um modelo de e-mail criado para este evento. Edite o modelo existente ou escolha outro evento.";
                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao salvar modelo de e-mail para sala {IdSalao}.", idSalao);
                    Mensagem = "Nao foi possivel salvar o modelo de e-mail. Tente novamente em instantes.";
                    return Page();
                }
            }
            return Page();
        }
    }
}


