using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigListaModel : PageModel
    {
        private readonly ILembreteHandler _handler;

        public List<LembreteConfig> Configs { get; set; } = new List<LembreteConfig>();
        [TempData]
        public string Mensagem { get; set; }

        public LembreteConfigListaModel(ILembreteHandler handler)
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
                Mensagem = "Erro ao identificar a empresa do usuário.";
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


