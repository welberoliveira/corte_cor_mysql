using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ModeloEmailListaModel : PageModel
    {
        private readonly ModeloEmailHandler _handler;

        public List<Models.ModeloEmail> Modelos { get; set; } = new List<Models.ModeloEmail>();
        public string Mensagem { get; set; }

        public ModeloEmailListaModel(ModeloEmailHandler handler)
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
                Mensagem = "Erro ao identificar o salão do usuário.";
            }
        }
    }
}
