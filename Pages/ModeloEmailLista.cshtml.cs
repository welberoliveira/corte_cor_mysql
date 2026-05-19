using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Handlers;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloEmailListaModel : PageModel
    {
        private readonly ModeloEmailHandler _handler;
        private readonly ILogger<ModeloEmailListaModel> _logger;

        public List<ModeloEmail> Modelos { get; set; } = new List<ModeloEmail>();
        public string Mensagem { get; set; }

        public ModeloEmailListaModel(ModeloEmailHandler handler, ILogger<ModeloEmailListaModel> logger)
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
                    _logger.LogError(ex, "Erro ao carregar modelos de e-mail para sala {IdSalao}.", idSalao);
                    Mensagem = "Nao foi possivel carregar os modelos de e-mail no momento.";
                    Modelos = new List<ModeloEmail>();
                }
            }
            else
            {
                Mensagem = "Erro ao identificar a empresa do usuário.";
            }
        }
    }
}


