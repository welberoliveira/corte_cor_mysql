using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigListaModel : PageModel
    {
        private readonly ILembreteHandler _handler;
        private readonly ILogger<LembreteConfigListaModel> _logger;

        public List<LembreteConfig> Configs { get; set; } = new List<LembreteConfig>();
        [TempData]
        public string Mensagem { get; set; }

        public LembreteConfigListaModel(ILembreteHandler handler, ILogger<LembreteConfigListaModel> logger)
        {
            _handler = handler;
            _logger = logger;
        }

        public void OnGet()
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                try
                {
                    Configs = _handler.ListarConfig(idSalao);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar regras de lembrete para sala {IdSalao}.", idSalao);
                    Mensagem = "Nao foi possivel carregar as regras de lembrete no momento.";
                    Configs = new List<LembreteConfig>();
                }
            }
            else
            {
                Mensagem = "Erro ao identificar a empresa do usuário.";
            }
        }

        public IActionResult OnPostExcluir(int id)
        {
            try
            {
                _handler.ExcluirConfig(id);
                Mensagem = "Regra excluida com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir regra de lembrete {IdConfig}.", id);
                Mensagem = "Nao foi possivel excluir a regra de lembrete.";
            }
            return RedirectToPage();
        }
    }
}


