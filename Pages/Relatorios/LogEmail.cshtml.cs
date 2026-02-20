using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CorteCor;
using static CorteCor.Models;
using static LembreteHandler;

namespace CorteCor.Pages.Relatorios
{
    [Authorize]
    public class LogEmailModel : PageModel
    {
        private readonly LembreteHandler _handler;

        public LogEmailModel()
        {
            _handler = new LembreteHandler();
        }

        public PagedResult<LogEnvioEmail> Logs { get; set; } = new PagedResult<LogEnvioEmail>();

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Destinatario { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string Assunto { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TipoLembrete { get; set; }

        [BindProperty(SupportsGet = true)]
        public int p { get; set; } = 1;

        public void OnGet()
        {
            if (!DataInicio.HasValue)
                DataInicio = DateTime.Today.AddDays(-7);
            
            if (!DataFim.HasValue)
                DataFim = DateTime.Today;

            // Ajustar o filtro para pegar o dia inteiro
            // DataInicio: Começo do dia (00:00:00) - Já é o padrão se vier do date picker/Today
            DateTime? filtroInicio = DataInicio?.Date;

            // DataFim: Final do dia (23:59:59)
            DateTime? filtroFim = DataFim?.Date.AddDays(1).AddSeconds(-1);

            Logs = _handler.ListarLogsEnvio(filtroInicio, filtroFim, Destinatario, Assunto, Status, p > 0 ? p : 1, 10, TipoLembrete);
            if (Logs == null) Logs = new PagedResult<LogEnvioEmail>();
        }
    }
}
