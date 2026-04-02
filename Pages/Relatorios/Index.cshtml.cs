using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Relatorios;

[Authorize]
public class IndexModel : PageModel
{
    private readonly RelatorioCentralService _relatorioService;

    public IndexModel(RelatorioCentralService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public IReadOnlyList<IGrouping<string, RelatorioCatalogItem>> Grupos { get; private set; } =
        Array.Empty<IGrouping<string, RelatorioCatalogItem>>();

    public void OnGet()
    {
        Grupos = _relatorioService.ListarCatalogo()
            .OrderBy(item => item.Grupo)
            .ThenBy(item => item.Titulo)
            .GroupBy(item => item.Grupo)
            .ToList();
    }
}
