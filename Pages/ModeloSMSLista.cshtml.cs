using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor;
using CorteCor.Handlers;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloSMSListaModel : PageModel
    {
        private readonly ModeloSMSHandler _handler;

        public List<ModeloSMS> Modelos { get; set; } = new List<ModeloSMS>();
        public string Mensagem { get; set; }

        public ModeloSMSListaModel(ModeloSMSHandler handler)
        {
            _handler = handler;
        }

        public void OnGet()
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                Modelos = _handler.ListarPorSalao(idSalao);
            }
            else
            {
                Mensagem = "Erro ao identificar a empresa do usuário.";
            }
        }
    }
}


