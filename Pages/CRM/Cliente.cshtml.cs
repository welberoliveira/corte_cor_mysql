using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.CRM
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class ClienteModel : PageModel
    {
        private const int TimelinePageSize = 12;
        private readonly CrmService _crmService;

        public ClienteModel(CrmService crmService)
        {
            _crmService = crmService;
        }

        [BindProperty(SupportsGet = true)]
        public int IdPessoa { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TipoFiltro { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataInicio { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DataFim { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PesquisaTimeline { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        public CrmClienteDetalhe Detalhe { get; private set; } = new();
        public List<CrmTimelineItem> TimelineFiltrada { get; private set; } = new();
        public List<string> TiposTimelineDisponiveis { get; private set; } = new();
        public int TimelineTotalCount { get; private set; }
        public int TimelineTotalPages { get; private set; }

        [TempData]
        public string? FlashMessage { get; set; }

        [TempData]
        public string? FlashType { get; set; }

        public IActionResult OnGet()
        {
            if (!TryObterIdSalao(out var idSalao))
            {
                return RedirectToPage("/Index");
            }

            if (IdPessoa <= 0)
            {
                return RedirectToPage("/CRM/Index");
            }

            PageIndex = PageIndex <= 0 ? 1 : PageIndex;

            Detalhe = _crmService.ObterClienteDetalhe(idSalao, IdPessoa);
            TiposTimelineDisponiveis = Detalhe.Timeline
                .Select(item => item.Categoria)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item)
                .ToList();

            var timelineQuery = Detalhe.Timeline.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TipoFiltro))
            {
                timelineQuery = timelineQuery.Where(item => string.Equals(item.Categoria, TipoFiltro, StringComparison.OrdinalIgnoreCase));
            }

            if (DataInicio.HasValue)
            {
                var inicio = DataInicio.Value.Date;
                timelineQuery = timelineQuery.Where(item => item.Data >= inicio);
            }

            if (DataFim.HasValue)
            {
                var fim = DataFim.Value.Date.AddDays(1).AddTicks(-1);
                timelineQuery = timelineQuery.Where(item => item.Data <= fim);
            }

            if (!string.IsNullOrWhiteSpace(PesquisaTimeline))
            {
                var pesquisa = PesquisaTimeline.Trim();
                timelineQuery = timelineQuery.Where(item =>
                    item.Titulo.Contains(pesquisa, StringComparison.OrdinalIgnoreCase) ||
                    item.Descricao.Contains(pesquisa, StringComparison.OrdinalIgnoreCase) ||
                    item.Categoria.Contains(pesquisa, StringComparison.OrdinalIgnoreCase));
            }

            var timelineLista = timelineQuery.ToList();
            TimelineTotalCount = timelineLista.Count;
            TimelineTotalPages = Math.Max(1, (int)Math.Ceiling(TimelineTotalCount / (double)TimelinePageSize));

            if (PageIndex > TimelineTotalPages)
            {
                PageIndex = TimelineTotalPages;
            }

            TimelineFiltrada = timelineLista
                .Skip((PageIndex - 1) * TimelinePageSize)
                .Take(TimelinePageSize)
                .ToList();

            return Page();
        }

        public bool PodeVoltarPagina => PageIndex > 1;
        public bool PodeAvancarPagina => PageIndex < TimelineTotalPages;

        private bool TryObterIdSalao(out int idSalao)
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0;
        }
    }
}
