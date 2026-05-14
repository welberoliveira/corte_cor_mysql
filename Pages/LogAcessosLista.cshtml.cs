using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class LogAcessosListaModel : PageModel
    {
        public List<LogAcesso> Logs { get; set; } = new List<LogAcesso>();

        // Filtros
        [BindProperty(SupportsGet = true)]
        public string FiltroUsuario { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FiltroDataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FiltroDataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FiltroIP { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FiltroCredencial { get; set; }

        [BindProperty(SupportsGet = true)]
        public string FiltroResultado { get; set; } // "todos", "sucesso", "falha"

        public void OnGet()
        {
            var handler = new LogAcessoHandler();
            var todos = handler.Listar(1000);

            // Aplicar filtros
            var filtrado = todos.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroUsuario))
                filtrado = filtrado.Where(l => l.Usuario != null && l.Usuario.Contains(FiltroUsuario, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroDataInicio) && DateTime.TryParse(FiltroDataInicio, out var dtInicio))
                filtrado = filtrado.Where(l => l.DataHora >= dtInicio);

            if (!string.IsNullOrWhiteSpace(FiltroDataFim) && DateTime.TryParse(FiltroDataFim, out var dtFim))
                filtrado = filtrado.Where(l => l.DataHora <= dtFim.AddDays(1));

            if (!string.IsNullOrWhiteSpace(FiltroIP))
                filtrado = filtrado.Where(l => l.IP_Origem != null && l.IP_Origem.Contains(FiltroIP, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroCredencial))
                filtrado = filtrado.Where(l => l.CredencialUsada != null && l.CredencialUsada.Contains(FiltroCredencial, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FiltroResultado) && FiltroResultado != "todos")
            {
                bool sucesso = FiltroResultado == "sucesso";
                filtrado = filtrado.Where(l => l.Sucesso == sucesso);
            }

            Logs = filtrado.ToList();
        }
    }
}
