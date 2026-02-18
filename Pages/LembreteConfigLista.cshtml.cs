using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigListaModel : PageModel
    {
        private readonly LembreteHandler _handler;

        public List<Models.LembreteConfig> Configs { get; set; } = new List<Models.LembreteConfig>();
        [TempData]
        public string Mensagem { get; set; }

        public LembreteConfigListaModel(LembreteHandler handler)
        {
            _handler = handler;
        }

        public void OnGet()
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                Configs = _handler.ListarConfig(idSalao);
            }
            else
            {
                Mensagem = "Erro ao identificar o salão do usuário.";
            }
        }

        public IActionResult OnPostExcluir(int id)
        {
            _handler.ExcluirConfig(id);
            Mensagem = "Regra excluída com sucesso!";
            return RedirectToPage();
        }
    }
}
