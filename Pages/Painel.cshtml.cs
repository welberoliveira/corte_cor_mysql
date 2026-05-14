using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using CorteCor.Handlers;

namespace CorteCor.Pages
{
    [Authorize(Policy = "AdminPolicy")]
    public class PainelModel : PageModel
    {
        public sealed record SalaoOpcao(int Id, string Nome);
        private readonly SalaoHandler _salaoHandler;

        public PainelModel(IDatabaseHandler dbHandler)
        {
            _salaoHandler = new SalaoHandler(dbHandler);
        }

        [BindProperty]
        public int SelectedIdSalao { get; set; }

        public IReadOnlyList<SalaoOpcao> Empresas { get; private set; } = Array.Empty<SalaoOpcao>();

        public Dictionary<string, int> DocumentacaoChartData { get; private set; } = new();
        public Dictionary<string, int> TipoImovelChartData { get; private set; } = new();
        public Dictionary<string, int> TipoEdificacaoChartData { get; private set; } = new();
        public Dictionary<string, int> AcabamentoChartData { get; private set; } = new();
        public Dictionary<string, int> AguaPotavelChartData { get; private set; } = new();
        public Dictionary<string, int> EsgotamentoSanitarioChartData { get; private set; } = new();
        public Dictionary<string, int> EnergiaEletricaChartData { get; private set; } = new();
        public Dictionary<string, int> DestinoLixoChartData { get; private set; } = new();
        public Dictionary<string, int> CondicaoOcupacaoLoteChartData { get; private set; } = new();
        public Dictionary<string, int> NumeroOcupacaoChartData { get; private set; } = new();
        public Dictionary<string, int> PossuiOutroImovelChartData { get; private set; } = new();
        public Dictionary<string, int> PossuiIPTUChartData { get; private set; } = new();

        public void OnGet()
        {
            LoadEmpresas();
            SelectedIdSalao = ParseSalaoClaim() ?? Empresas.FirstOrDefault()?.Id ?? 0;
        }

        public async Task<IActionResult> OnPostAcessarAsync()
        {
            LoadEmpresas();

            if (!Empresas.Any(e => e.Id == SelectedIdSalao))
            {
                ModelState.AddModelError(string.Empty, "Selecione uma empresa válida.");
                SelectedIdSalao = Empresas.FirstOrDefault()?.Id ?? 0;
                return Page();
            }

            var nomeUsuario = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(nomeUsuario))
            {
                return Redirect(HttpContext.Request.PathBase + "/adm");
            }

            var claims = User.Claims
                .Where(c => c.Type != "Role" && c.Type != "IdSalao")
                .ToList();

            claims.Add(new Claim(ClaimTypes.Name, nomeUsuario));
            claims.Add(new Claim("Role", "Admin"));
            claims.Add(new Claim("Role", "Usuario"));
            claims.Add(new Claim("IdSalao", SelectedIdSalao.ToString()));

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);

            return Redirect(HttpContext.Request.PathBase + "/Dashboard");
        }

        private int? ParseSalaoClaim()
        {
            return int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : null;
        }

        private void LoadEmpresas()
        {
            Empresas = (_salaoHandler.Listar() ?? new List<CorteCor.Models.Salao>())
                .Where(s => s.IdSalao > 0)
                .Select(s => new SalaoOpcao(s.IdSalao, string.IsNullOrWhiteSpace(s.Nome) ? $"Salão {s.IdSalao}" : s.Nome))
                .OrderBy(s => s.Nome)
                .ToList();
        }
    }
}
