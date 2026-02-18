using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CorteCor.Pages.Webhooks
{
    [IgnoreAntiforgeryToken]
    public class TriggerLembretesModel : PageModel
    {
        private readonly LembreteService _lembreteService;
        private readonly ILogger<TriggerLembretesModel> _logger;

        public TriggerLembretesModel(LembreteService lembreteService, ILogger<TriggerLembretesModel> logger)
        {
            _lembreteService = lembreteService;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Gatilho manual de lembretes recebido via POST.");
            
            try
            {
                int enviados = await _lembreteService.ProcessarLembretesAsync();
                return new JsonResult(new { success = true, message = $"Processamento concluído. Lembretes enviados: {enviados}" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar lembretes via gatilho manual.");
                return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = 500 };
            }
        }

        public IActionResult OnGet()
        {
            return Content("Este endpoint aceita apenas requisições POST para disparar o processamento de lembretes.");
        }
    }
}
