using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor;
using CorteCor.Handlers;
using Microsoft.Extensions.Logging;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloSMSListaModel : PageModel
    {
        private readonly ModeloSMSHandler _handler;
        private readonly ILogger<ModeloSMSListaModel> _logger;

        public List<ModeloSMS> Modelos { get; set; } = new List<ModeloSMS>();
        public string Mensagem { get; set; }

        public ModeloSMSListaModel(ModeloSMSHandler handler, ILogger<ModeloSMSListaModel> logger)
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
                    Modelos = _handler.ListarPorSalao(idSalao);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar modelos de SMS para sala {IdSalao}.", idSalao);
                    Mensagem = "Nao foi possivel carregar os modelos de SMS no momento.";
                    Modelos = new List<ModeloSMS>();
                }
            }
            else
            {
                Mensagem = "Erro ao identificar a empresa do usuário.";
            }
        }
    }
}


