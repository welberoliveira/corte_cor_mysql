using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CorteCor.Handlers;
using CorteCor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorteCor.Pages
{
    [Authorize]
    public class NotaFiscalLogListaModel : PageModel
    {
        private readonly NotaFiscalLogHandler _logHandler;

        public NotaFiscalLogListaModel(NotaFiscalLogHandler logHandler)
        {
            _logHandler = logHandler;
        }

        public List<NotaFiscalLog> Logs { get; set; } = new List<NotaFiscalLog>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (int.TryParse(User.FindFirst("IdSalao")?.Value, out int idSalao))
            {
                Logs = await _logHandler.ListarPorSalaoAsync(idSalao);
            }
            
            return Page();
        }
    }
}
