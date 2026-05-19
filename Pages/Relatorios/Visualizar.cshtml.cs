using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages.Relatorios;

[Authorize]
public class VisualizarModel : PageModel
{
    private readonly RelatorioCentralService _relatorioService;
    private readonly FinanceiroService _financeiroService;

    public VisualizarModel(RelatorioCentralService relatorioService, FinanceiroService financeiroService)
    {
        _relatorioService = relatorioService;
        _financeiroService = financeiroService;
    }

    public RelatorioResultado Resultado { get; private set; } = new();
    public RelatorioFiltrosContexto Contexto { get; private set; } = new();
    public RelatorioCatalogItem Definicao => Resultado.Definicao;

    [BindProperty(SupportsGet = true)] public string tipo { get; set; } = RelatorioTipos.Clientes;
    [BindProperty(SupportsGet = true)] public string? q { get; set; }
    [BindProperty(SupportsGet = true)] public string? status { get; set; }
    [BindProperty(SupportsGet = true)] public string? tipoFiltro { get; set; }
    [BindProperty(SupportsGet = true)] public string? canal { get; set; }
    [BindProperty(SupportsGet = true)] public string? segmento { get; set; }
    [BindProperty(SupportsGet = true)] public string? tipoLembrete { get; set; }
    [BindProperty(SupportsGet = true)] public string? usuario { get; set; }
    [BindProperty(SupportsGet = true)] public string? codigoErro { get; set; }
    [BindProperty(SupportsGet = true)] public int? idPessoa { get; set; }
    [BindProperty(SupportsGet = true)] public int? idFuncionario { get; set; }
    [BindProperty(SupportsGet = true)] public int? idCategoria { get; set; }
    [BindProperty(SupportsGet = true)] public int? idServico { get; set; }
    [BindProperty(SupportsGet = true)] public int? idProduto { get; set; }
    [BindProperty(SupportsGet = true)] public int? idGrupoPlano { get; set; }
    [BindProperty(SupportsGet = true)] public int? idPlano { get; set; }
    [BindProperty(SupportsGet = true)] public int? idConta { get; set; }
    [BindProperty(SupportsGet = true)] public int? ambiente { get; set; }
    [BindProperty(SupportsGet = true)] public bool? ativo { get; set; }
    [BindProperty(SupportsGet = true)] public bool? emissaoAutomatica { get; set; }
    [BindProperty(SupportsGet = true)] public bool somenteVigentes { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? dataInicio { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? dataFim { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await CarregarAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportCsvAsync()
    {
        await CarregarAsync();
        var bytes = _relatorioService.GerarCsv(Resultado);
        return File(bytes, "text/csv", $"{Definicao.Tipo}-{DateTime.Now:yyyyMMddHHmm}.csv");
    }

    public async Task<IActionResult> OnGetExportPdfAsync()
    {
        await CarregarAsync();
        var bytes = _relatorioService.GerarPdf(Resultado, CriarFiltro());
        return File(bytes, "application/pdf", $"{Definicao.Tipo}-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }

    public async Task<JsonResult> OnGetContasPlanoAsync(int idGrupoPlano)
    {
        var contas = await _financeiroService.ListarContasAnaliticasPorGrupoAsync(ObterIdSalao(), idGrupoPlano);
        return new JsonResult(contas.Select(conta => new
        {
            id = conta.IdPlano,
            codigo = conta.Codigo,
            nome = conta.NomeExibicao,
            rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
        }));
    }

    public bool UsaFiltro(string filtro) => Definicao.Filtros.Contains(filtro, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<RelatorioOpcao> OpcoesStatus()
    {
        return tipo switch
        {
            RelatorioTipos.Pedidos => Status("Aberto", "Vencido", "Convertido", "Cancelado"),
            RelatorioTipos.Vendas => Status("Finalizada", "Ajustada", "Cancelada"),
            RelatorioTipos.Agendamentos => Status("Agendado", "Pendente", "Pago", "Cancelado"),
            RelatorioTipos.LogEnvios => Status("Sucesso", "Erro"),
            RelatorioTipos.CrmTarefas => Status("Aberta", "Concluida", "Cancelada"),
            RelatorioTipos.CrmOportunidades => Status("Aberta", "Ganha", "Perdida"),
            RelatorioTipos.CrmCampanhas => Status("Rascunho", "Enviada", "Pausada", "Concluida"),
            RelatorioTipos.FinanceiroPagarReceber => Status("Aberto", "Liquidado", "Vencido", "Cancelado"),
            RelatorioTipos.FinanceiroRelatorios => Status("Aberto", "Liquidado", "Vencido", "Cancelado"),
            RelatorioTipos.PagamentosPendencias => Status("Pendente", "Pago", "Cancelado", "Aprovado"),
            RelatorioTipos.NotasFiscais => Status("Autorizada", "Rejeitada", "Cancelada", "Pendente"),
            _ => Array.Empty<RelatorioOpcao>()
        };
    }

    public IEnumerable<RelatorioOpcao> OpcoesTipo()
    {
        return tipo switch
        {
            RelatorioTipos.ModelosEmail => Tipo("BoasVindas", "ConfirmacaoAgendamento", "Lembrete", "Campanha"),
            RelatorioTipos.ModelosSms => Tipo("BoasVindas", "ConfirmacaoAgendamento", "Lembrete", "Campanha"),
            RelatorioTipos.FinanceiroPagarReceber => Tipo("Receber", "Pagar"),
            RelatorioTipos.FinanceiroRelatorios => Tipo("Receber", "Pagar"),
            RelatorioTipos.FinanceiroPlanoContas => Tipo("R", "D"),
            RelatorioTipos.FinanceiroContasCaixa => Tipo("Conta Corrente", "Carteira", "Pix", "Caixa"),
            RelatorioTipos.Estoque => Tipo("OK", "Atenção", "Sem controle"),
            _ => Array.Empty<RelatorioOpcao>()
        };
    }

    public IEnumerable<RelatorioOpcao> OpcoesCanal() =>
        Tipo("Email", "SMS", "WhatsApp", "Telefone", "Presencial", "Sistema");

    public IEnumerable<RelatorioOpcao> OpcoesSegmento() =>
        Tipo("TodosClientes", "Inativos", "AniversariantesDoMes", "PorTag", "ClienteEspecifico");

    public IEnumerable<RelatorioOpcao> OpcoesTipoLembrete() =>
        Tipo("Email", "SMS", "WhatsApp");

    public IEnumerable<RelatorioOpcao> OpcoesAmbiente() =>
        new[]
        {
            new RelatorioOpcao { Valor = "1", Rotulo = "Produção" },
            new RelatorioOpcao { Valor = "2", Rotulo = "Homologação" }
        };

    public string TituloPagina => $"Relatório - {Definicao.Titulo}";

    public string ExportCsvUrl => BuildExportUrl("ExportCsv");
    public string ExportPdfUrl => BuildExportUrl("ExportPdf");

    private async Task CarregarAsync()
    {
        var idSalao = ObterIdSalao();
        var definicao = _relatorioService.ObterDefinicao(tipo);
        Contexto = await _relatorioService.ObterContextoFiltrosAsync(idSalao);

        if (definicao.Filtros.Contains(RelatorioFiltros.Plano, StringComparer.OrdinalIgnoreCase))
        {
            var gruposPlano = await _financeiroService.ListarGruposPlanoContasAsync(idSalao);
            Contexto.GruposPlano = gruposPlano
                .Select(grupo => new RelatorioOpcao
                {
                    Valor = grupo.IdPlano.ToString(),
                    Rotulo = $"{grupo.Codigo} - {grupo.NomeExibicao}"
                })
                .ToList();

            if (!idGrupoPlano.HasValue && idPlano.HasValue)
            {
                idGrupoPlano = (await _financeiroService.ObterGrupoNivel2DoPlanoAsync(idSalao, idPlano))?.IdPlano;
            }

            Contexto.Planos = idGrupoPlano.HasValue && idGrupoPlano > 0
                ? (await _financeiroService.ListarContasAnaliticasPorGrupoAsync(idSalao, idGrupoPlano.Value))
                    .Select(conta => new RelatorioOpcao
                    {
                        Valor = conta.IdPlano.ToString(),
                        Rotulo = $"{conta.Codigo} - {conta.NomeExibicao}"
                    })
                    .ToList()
                : new List<RelatorioOpcao>();
        }

        Resultado = await _relatorioService.GerarAsync(idSalao, CriarFiltro());
    }

    private RelatorioFiltroInput CriarFiltro() => new()
    {
        Tipo = tipo,
        q = q,
        status = status,
        tipo = tipoFiltro,
        canal = canal,
        segmento = segmento,
        tipoLembrete = tipoLembrete,
        usuario = usuario,
        codigoErro = codigoErro,
        idPessoa = idPessoa,
        idFuncionario = idFuncionario,
        idCategoria = idCategoria,
        idServico = idServico,
        idProduto = idProduto,
        idGrupoPlano = idGrupoPlano,
        idPlano = idPlano,
        idConta = idConta,
        ambiente = ambiente,
        ativo = ativo,
        emissaoAutomatica = emissaoAutomatica,
        somenteVigentes = somenteVigentes,
        dataInicio = dataInicio,
        dataFim = dataFim
    };

    private int ObterIdSalao() =>
        int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao) ? idSalao : 0;

    private static IEnumerable<RelatorioOpcao> Status(params string[] valores) => Tipo(valores);

    private static IEnumerable<RelatorioOpcao> Tipo(params string[] valores) =>
        valores.Select(valor => new RelatorioOpcao { Valor = valor, Rotulo = valor });

    private string BuildExportUrl(string handler)
    {
        var filtro = CriarFiltro();
        var query = new QueryBuilder
        {
            { "handler", handler },
            { "tipo", filtro.Tipo }
        };

        AddIfNotEmpty(query, "q", filtro.q);
        AddIfNotEmpty(query, "status", filtro.status);
        AddIfNotEmpty(query, "tipoFiltro", filtro.tipo);
        AddIfNotEmpty(query, "canal", filtro.canal);
        AddIfNotEmpty(query, "segmento", filtro.segmento);
        AddIfNotEmpty(query, "tipoLembrete", filtro.tipoLembrete);
        AddIfNotEmpty(query, "usuario", filtro.usuario);
        AddIfNotEmpty(query, "codigoErro", filtro.codigoErro);
        AddIfHasValue(query, "idPessoa", filtro.idPessoa);
        AddIfHasValue(query, "idFuncionario", filtro.idFuncionario);
        AddIfHasValue(query, "idCategoria", filtro.idCategoria);
        AddIfHasValue(query, "idServico", filtro.idServico);
        AddIfHasValue(query, "idProduto", filtro.idProduto);
        AddIfHasValue(query, "idGrupoPlano", filtro.idGrupoPlano);
        AddIfHasValue(query, "idPlano", filtro.idPlano);
        AddIfHasValue(query, "idConta", filtro.idConta);
        AddIfHasValue(query, "ambiente", filtro.ambiente);
        AddIfHasValue(query, "ativo", filtro.ativo);
        AddIfHasValue(query, "emissaoAutomatica", filtro.emissaoAutomatica);

        if (filtro.somenteVigentes)
        {
            query.Add("somenteVigentes", "true");
        }

        if (filtro.dataInicio.HasValue)
        {
            query.Add("dataInicio", filtro.dataInicio.Value.ToString("yyyy-MM-dd"));
        }

        if (filtro.dataFim.HasValue)
        {
            query.Add("dataFim", filtro.dataFim.Value.ToString("yyyy-MM-dd"));
        }

        return $"/Relatorios/Visualizar{query.ToQueryString()}";
    }

    private static void AddIfNotEmpty(QueryBuilder query, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add(key, value);
        }
    }

    private static void AddIfHasValue<T>(QueryBuilder query, string key, T? value) where T : struct
    {
        if (value.HasValue)
        {
            query.Add(key, value.Value.ToString());
        }
    }
}
