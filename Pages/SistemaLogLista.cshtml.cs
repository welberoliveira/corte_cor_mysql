using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor.Models;
using CorteCor.Handlers;

namespace CorteCor.Pages
{
    [Authorize]
    public class SistemaLogListaModel : PageModel
    {
        private readonly NotaFiscalLogHandler _sistemaLogHandler;

        public SistemaLogListaModel(NotaFiscalLogHandler sistemaLogHandler)
        {
            _sistemaLogHandler = sistemaLogHandler;
        }

        public List<NotaFiscalLog> Logs { get; set; } = new List<NotaFiscalLog>();

        [BindProperty(SupportsGet = true)]
        public DateTime? DtInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DtFim { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var idSalaoClaim = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(idSalaoClaim) || !int.TryParse(idSalaoClaim, out int idSalao))
            {
                return RedirectToPage("/Index");
            }

            Logs = await _sistemaLogHandler.ListarPorSalaoAsync(idSalao, DtInicio, DtFim);

            return Page();
        }
    }
}
