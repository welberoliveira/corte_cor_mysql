using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigCadastroModel : PageModel
    {
        private readonly LembreteHandler _handler;
        private readonly ModeloEmailHandler _modeloHandler;

        [BindProperty]
        public Models.LembreteConfig Config { get; set; } = new Models.LembreteConfig();

        public List<SelectListItem> UnidadeOptions { get; set; }
        public List<SelectListItem> ModeloOptions { get; set; }

        public string Mensagem { get; set; }

        public LembreteConfigCadastroModel(LembreteHandler handler, ModeloEmailHandler modeloHandler)
        {
            _handler = handler;
            _modeloHandler = modeloHandler;
            UnidadeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Minutos", Text = "Minutos" },
                new SelectListItem { Value = "Horas", Text = "Horas" },
                new SelectListItem { Value = "Dias", Text = "Dias" }
            };
        }

        public void OnGet(int? id)
        {
            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                Config.IdSalao = idSalao;
                Config.Ativo = true; // Default for new
                Config.DataInicio = DateTime.Today; // Default starting today

                // Load Email Models
                var modelos = _modeloHandler.ListarPorSalao(idSalao);
                ModeloOptions = modelos.Select(m => new SelectListItem
                {
                    Value = m.IdModelo.ToString(),
                    Text = m.Assunto
                }).ToList();
            }

            // Logic to load existing if ID is provided (Not fully implemented in handler yet as we only have List, but logic below assumes new)
            // If I want edit, I need ObterPorId in Handler.
            // But list page passes ID.
            // For now, I'll rely on "New" only or implement GetById if strictly required.
            // The prompt says "create screen to configure rules... list screen with button to register new".
            // It doesn't explicitly demand Edit, but it's good practice. Use ListarConfig and filter by memory if needed or add Get method.
            // I'll skip Edit for now to save tokens/time unless I add GetById to Handler. 
            // Wait, ListarConfig returns all. I can filter here.
            
            if (id.HasValue)
            {
                var configs = _handler.ListarConfig(Config.IdSalao);
                var existing = configs.FirstOrDefault(c => c.IdConfig == id.Value);
                if (existing != null)
                {
                    Config = existing;
                }
            }
        }

        public IActionResult OnPost()
        {
            // Validação customizada
            if (Config.AntecedenciaUnidade == "Minutos" && Config.AntecedenciaValor < 30)
            {
                ModelState.AddModelError("Config.AntecedenciaValor", "Para minutos, o valor mínimo é 30.");
            }

            if (!ModelState.IsValid)
            {
                // Repopular SelectLists em caso de erro
                var idSalaoClaim = User.FindFirst("IdSalao");
                if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idS))
                {
                    var modelos = _modeloHandler.ListarPorSalao(idS);
                    ModeloOptions = modelos.Select(m => new SelectListItem
                    {
                        Value = m.IdModelo.ToString(),
                        Text = m.Assunto
                    }).ToList();
                }
                return Page();
            }

            var idSalaoClm = User.FindFirst("IdSalao");
            if (idSalaoClm != null && int.TryParse(idSalaoClm.Value, out int idSalao))
            {
                Config.IdSalao = idSalao;
            }

            _handler.SalvarConfig(Config);

            return RedirectToPage("/LembreteConfigLista");
        }
    }
}
