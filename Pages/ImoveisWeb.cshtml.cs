using System.Globalization;
using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages;

[AllowAnonymous]
public class ImoveisWebModel : PageModel
{
    private readonly ImovelHandler _imovelHandler;

    public PagedResult<Imovel> Imoveis { get; private set; } = new() { PageIndex = 1, PageSize = 8 };
    public string Mensagem { get; private set; } = string.Empty;

    [BindProperty(SupportsGet = true)] public string? q { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Finalidade { get; set; }
    [BindProperty(SupportsGet = true)] public string? TipoImovel { get; set; }
    [BindProperty(SupportsGet = true)] public string? Estado { get; set; }
    [BindProperty(SupportsGet = true)] public string? Cidade { get; set; }
    [BindProperty(SupportsGet = true)] public string? Bairro { get; set; }
    [BindProperty(SupportsGet = true)] public string? ValorMinimo { get; set; }
    [BindProperty(SupportsGet = true)] public string? ValorMaximo { get; set; }
    [BindProperty(SupportsGet = true)] public string? AreaMinima { get; set; }
    [BindProperty(SupportsGet = true)] public string? AreaMaxima { get; set; }
    [BindProperty(SupportsGet = true)] public int? QuartosMinimo { get; set; }
    [BindProperty(SupportsGet = true)] public int? SuitesMinimo { get; set; }
    [BindProperty(SupportsGet = true)] public int? BanheirosMinimo { get; set; }
    [BindProperty(SupportsGet = true)] public int? VagasMinimo { get; set; }
    [BindProperty(SupportsGet = true)] public bool Publicados { get; set; } = true;
    [BindProperty(SupportsGet = true)] public bool Disponiveis { get; set; } = true;
    [BindProperty(SupportsGet = true)] public bool DestaqueNaBusca { get; set; }
    [BindProperty(SupportsGet = true)] public bool PrecoSobConsulta { get; set; }
    [BindProperty(SupportsGet = true)] public bool AceitaFinanciamento { get; set; }
    [BindProperty(SupportsGet = true)] public bool AceitaPermuta { get; set; }
    [BindProperty(SupportsGet = true)] public bool ComVideo { get; set; }
    [BindProperty(SupportsGet = true)] public bool ComTourVirtual { get; set; }
    [BindProperty(SupportsGet = true)] public bool ComFotos { get; set; }
    [BindProperty(SupportsGet = true)] public bool Piscina { get; set; }
    [BindProperty(SupportsGet = true)] public bool ArCondicionado { get; set; }
    [BindProperty(SupportsGet = true)] public bool Churrasqueira { get; set; }
    [BindProperty(SupportsGet = true)] public bool Sauna { get; set; }
    [BindProperty(SupportsGet = true)] public bool Jardim { get; set; }
    [BindProperty(SupportsGet = true)] public bool AreaGourmet { get; set; }
    [BindProperty(SupportsGet = true)] public bool Jacuzzi { get; set; }
    [BindProperty(SupportsGet = true)] public bool Hidromassagem { get; set; }
    [BindProperty(SupportsGet = true)] public bool Escritorio { get; set; }
    [BindProperty(SupportsGet = true)] public bool SalaTV { get; set; }
    [BindProperty(SupportsGet = true)] public bool CozinhaPlanejada { get; set; }
    [BindProperty(SupportsGet = true)] public bool Closet { get; set; }
    [BindProperty(SupportsGet = true)] public bool Varanda { get; set; }
    [BindProperty(SupportsGet = true)] public bool Lavabo { get; set; }
    [BindProperty(SupportsGet = true)] public string? Ordenacao { get; set; }
    [BindProperty(SupportsGet = true, Name = "id")] public int? IdSalao { get; set; }
    [BindProperty(SupportsGet = true)] public int p { get; set; } = 1;

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Ativo", "Inativo", "Vendido", "Alugado", "Rascunho" };
    public IReadOnlyList<string> FinalidadeOptions { get; } = new[] { "Venda", "Aluguel", "Temporada" };
    public IReadOnlyList<string> TipoOptions { get; } = new[] { "Casa", "Apartamento", "Lote", "Sala", "Cobertura", "Terreno", "Comercial", "Rural", "Outro" };
    public IReadOnlyList<string> EstadoOptions { get; } = new[]
    {
        "Acre", "Alagoas", "Amapa", "Amazonas", "Bahia", "Ceara", "Distrito Federal", "Espirito Santo",
        "Goias", "Maranhao", "Mato Grosso", "Mato Grosso do Sul", "Minas Gerais", "Para", "Paraiba",
        "Parana", "Pernambuco", "Piaui", "Rio de Janeiro", "Rio Grande do Norte", "Rio Grande do Sul",
        "Rondonia", "Roraima", "Santa Catarina", "Sao Paulo", "Sergipe", "Tocantins"
    };

    public ImoveisWebModel(ImovelHandler imovelHandler)
    {
        _imovelHandler = imovelHandler;
    }

    public void OnGet()
    {
        if (!TryObterIdSalao(out var idSalao))
        {
            Mensagem = "Nao foi possivel identificar a empresa atual.";
            Imoveis = new PagedResult<Imovel> { PageIndex = Math.Max(1, p), PageSize = 8 };
            return;
        }

        try
        {
            p = p < 1 ? 1 : p;
            Imoveis = _imovelHandler.ListarWebPorSalao(idSalao, MontarFiltro(), p, 8);
        }
        catch (Exception ex)
        {
            Mensagem = $"Nao foi possivel carregar a vitrine de imoveis. Detalhe: {ex.Message}";
            Imoveis = new PagedResult<Imovel> { PageIndex = Math.Max(1, p), PageSize = 8 };
        }
    }

    public Dictionary<string, string> RouteValues(int pagina)
    {
        var routes = new Dictionary<string, string>
        {
            ["p"] = pagina.ToString(CultureInfo.InvariantCulture),
            ["Publicados"] = Publicados.ToString().ToLowerInvariant(),
            ["Disponiveis"] = Disponiveis.ToString().ToLowerInvariant(),
            ["DestaqueNaBusca"] = DestaqueNaBusca.ToString().ToLowerInvariant(),
            ["PrecoSobConsulta"] = PrecoSobConsulta.ToString().ToLowerInvariant(),
            ["AceitaFinanciamento"] = AceitaFinanciamento.ToString().ToLowerInvariant(),
            ["AceitaPermuta"] = AceitaPermuta.ToString().ToLowerInvariant(),
            ["ComVideo"] = ComVideo.ToString().ToLowerInvariant(),
            ["ComTourVirtual"] = ComTourVirtual.ToString().ToLowerInvariant(),
            ["ComFotos"] = ComFotos.ToString().ToLowerInvariant(),
            ["Piscina"] = Piscina.ToString().ToLowerInvariant(),
            ["ArCondicionado"] = ArCondicionado.ToString().ToLowerInvariant(),
            ["Churrasqueira"] = Churrasqueira.ToString().ToLowerInvariant(),
            ["Sauna"] = Sauna.ToString().ToLowerInvariant(),
            ["Jardim"] = Jardim.ToString().ToLowerInvariant(),
            ["AreaGourmet"] = AreaGourmet.ToString().ToLowerInvariant(),
            ["Jacuzzi"] = Jacuzzi.ToString().ToLowerInvariant(),
            ["Hidromassagem"] = Hidromassagem.ToString().ToLowerInvariant(),
            ["Escritorio"] = Escritorio.ToString().ToLowerInvariant(),
            ["SalaTV"] = SalaTV.ToString().ToLowerInvariant(),
            ["CozinhaPlanejada"] = CozinhaPlanejada.ToString().ToLowerInvariant(),
            ["Closet"] = Closet.ToString().ToLowerInvariant(),
            ["Varanda"] = Varanda.ToString().ToLowerInvariant(),
            ["Lavabo"] = Lavabo.ToString().ToLowerInvariant()
        };

        if (IdSalao.HasValue && IdSalao.Value > 0)
        {
            routes["id"] = IdSalao.Value.ToString(CultureInfo.InvariantCulture);
        }

        AddRoute(routes, "q", q);
        AddRoute(routes, "Status", Status);
        AddRoute(routes, "Finalidade", Finalidade);
        AddRoute(routes, "TipoImovel", TipoImovel);
        AddRoute(routes, "Estado", Estado);
        AddRoute(routes, "Cidade", Cidade);
        AddRoute(routes, "Bairro", Bairro);
        AddRoute(routes, "ValorMinimo", ValorMinimo);
        AddRoute(routes, "ValorMaximo", ValorMaximo);
        AddRoute(routes, "AreaMinima", AreaMinima);
        AddRoute(routes, "AreaMaxima", AreaMaxima);
        AddRoute(routes, "QuartosMinimo", QuartosMinimo?.ToString(CultureInfo.InvariantCulture));
        AddRoute(routes, "SuitesMinimo", SuitesMinimo?.ToString(CultureInfo.InvariantCulture));
        AddRoute(routes, "BanheirosMinimo", BanheirosMinimo?.ToString(CultureInfo.InvariantCulture));
        AddRoute(routes, "VagasMinimo", VagasMinimo?.ToString(CultureInfo.InvariantCulture));
        AddRoute(routes, "Ordenacao", Ordenacao);

        return routes;
    }

    public string FotoOuPlaceholder(Imovel imovel)
    {
        return !string.IsNullOrWhiteSpace(imovel.FotoCapaUrl)
            ? imovel.FotoCapaUrl
            : "/img/cortecor.png";
    }

    public string FormatarValor(Imovel imovel)
    {
        if (imovel.PrecoSobConsulta)
        {
            return "Sob consulta";
        }

        var valor = string.Equals(imovel.Finalidade, "Aluguel", StringComparison.OrdinalIgnoreCase)
            ? imovel.ValorAluguel
            : imovel.ValorVenda;

        return valor.HasValue && valor > 0
            ? $"R$ {valor.Value:N2}"
            : "Sob consulta";
    }

    public IReadOnlyList<string> CaracteristicasResumo(Imovel imovel)
    {
        var itens = new List<string>();
        AddNumber(itens, imovel.AreaConstruidaPrivativa, "m2");
        AddInt(itens, imovel.Quartos, "quarto", "quartos");
        AddInt(itens, imovel.Suites, "suite", "suites");
        AddInt(itens, imovel.Banheiros, "banheiro", "banheiros");
        AddInt(itens, imovel.VagasGaragem, "vaga", "vagas");
        return itens;
    }

    private ImovelWebFiltro MontarFiltro()
    {
        return new ImovelWebFiltro
        {
            Pesquisa = q,
            StatusAnuncio = Status,
            Finalidade = Finalidade,
            TipoImovel = TipoImovel,
            Estado = Estado,
            Cidade = Cidade,
            Bairro = Bairro,
            ValorMinimo = ParseNullableDecimal(ValorMinimo),
            ValorMaximo = ParseNullableDecimal(ValorMaximo),
            AreaMinima = ParseNullableDecimal(AreaMinima),
            AreaMaxima = ParseNullableDecimal(AreaMaxima),
            QuartosMinimo = QuartosMinimo,
            SuitesMinimo = SuitesMinimo,
            BanheirosMinimo = BanheirosMinimo,
            VagasMinimo = VagasMinimo,
            SomentePublicados = Publicados,
            SomenteDisponiveis = Disponiveis,
            DestaqueNaBusca = DestaqueNaBusca,
            PrecoSobConsulta = PrecoSobConsulta,
            AceitaFinanciamento = AceitaFinanciamento,
            AceitaPermuta = AceitaPermuta,
            ComVideo = ComVideo,
            ComTourVirtual = ComTourVirtual,
            ComFotos = ComFotos,
            Piscina = Piscina,
            ArCondicionado = ArCondicionado,
            Churrasqueira = Churrasqueira,
            Sauna = Sauna,
            Jardim = Jardim,
            AreaGourmet = AreaGourmet,
            Jacuzzi = Jacuzzi,
            Hidromassagem = Hidromassagem,
            Escritorio = Escritorio,
            SalaTV = SalaTV,
            CozinhaPlanejada = CozinhaPlanejada,
            Closet = Closet,
            Varanda = Varanda,
            Lavabo = Lavabo,
            Ordenacao = Ordenacao
        };
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = value.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (decimal.TryParse(cleaned, NumberStyles.Number, new CultureInfo("pt-BR"), out var parsed) ||
            decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private bool TryObterIdSalao(out int idSalao)
    {
        if (IdSalao.HasValue && IdSalao.Value > 0)
        {
            idSalao = IdSalao.Value;
            return true;
        }

        if (int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao) && idSalao > 0)
        {
            IdSalao = idSalao;
            return true;
        }

        var idSalaoPadrao = _imovelHandler.ObterIdSalaoPadraoVitrine();
        if (idSalaoPadrao.HasValue && idSalaoPadrao.Value > 0)
        {
            idSalao = idSalaoPadrao.Value;
            IdSalao = idSalao;
            return true;
        }

        idSalao = 0;
        return false;
    }

    private static void AddNumber(List<string> itens, decimal? value, string suffix)
    {
        if (value.HasValue && value > 0)
        {
            itens.Add($"{value.Value:N0} {suffix}");
        }
    }

    private static void AddInt(List<string> itens, int? value, string singular, string plural)
    {
        if (value.HasValue && value > 0)
        {
            itens.Add($"{value.Value} {(value.Value == 1 ? singular : plural)}");
        }
    }

    private static void AddRoute(Dictionary<string, string> routes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            routes[key] = value;
        }
    }
}
