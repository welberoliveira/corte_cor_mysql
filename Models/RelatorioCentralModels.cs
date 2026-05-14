namespace CorteCor.Models;

public static class RelatorioTipos
{
    public const string Clientes = "clientes";
    public const string Funcionarios = "funcionarios";
    public const string Servicos = "servicos";
    public const string Produtos = "produtos";
    public const string CategoriasProdutos = "categorias-produtos";
    public const string Pedidos = "pedidos";
    public const string Vendas = "vendas";
    public const string Estoque = "estoque";
    public const string Agendamentos = "agendamentos";
    public const string ModelosEmail = "modelos-email";
    public const string ModelosSms = "modelos-sms";
    public const string RegrasLembrete = "regras-lembrete";
    public const string LogEnvios = "log-envios";
    public const string LogsSistema = "logs-sistema";
    public const string LogAcessos = "log-acessos";
    public const string CrmTarefas = "crm-tarefas";
    public const string CrmOportunidades = "crm-oportunidades";
    public const string CrmCampanhas = "crm-campanhas";
    public const string FinanceiroPagarReceber = "financeiro-pagar-receber";
    public const string FinanceiroRelatorios = "financeiro-relatorios";
    public const string FinanceiroPlanoContas = "financeiro-plano-contas";
    public const string FinanceiroContasCaixa = "financeiro-contas-caixa";
    public const string PagamentosPendencias = "pagamentos-pendencias";
    public const string ConfiguracoesSistema = "configuracoes-sistema";
    public const string NotasFiscais = "notas-fiscais";
    public const string AuditoriaFiscal = "auditoria-fiscal";
    public const string DiagnosticoCertificado = "diagnostico-certificado";
}

public static class RelatorioFiltros
{
    public const string Pesquisa = "pesquisa";
    public const string Status = "status";
    public const string Data = "data";
    public const string Cliente = "cliente";
    public const string Funcionario = "funcionario";
    public const string Categoria = "categoria";
    public const string Servico = "servico";
    public const string Produto = "produto";
    public const string Tipo = "tipo";
    public const string Ativo = "ativo";
    public const string Plano = "plano";
    public const string Conta = "conta";
    public const string Canal = "canal";
    public const string Segmento = "segmento";
    public const string TipoLembrete = "tipo-lembrete";
    public const string Usuario = "usuario";
    public const string CodigoErro = "codigo-erro";
    public const string Ambiente = "ambiente";
    public const string EmissaoAutomatica = "emissao-automatica";
    public const string SomenteVigentes = "somente-vigentes";
}

public sealed class RelatorioCatalogItem
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public List<string> Filtros { get; set; } = new();
}

public sealed class RelatorioColuna
{
    public string Chave { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
}

public sealed class RelatorioOpcao
{
    public string Valor { get; set; } = string.Empty;
    public string Rotulo { get; set; } = string.Empty;
}

public sealed class RelatorioFiltroInput
{
    public string Tipo { get; set; } = string.Empty;
    public string? q { get; set; }
    public string? status { get; set; }
    public string? tipo { get; set; }
    public string? canal { get; set; }
    public string? segmento { get; set; }
    public string? tipoLembrete { get; set; }
    public string? usuario { get; set; }
    public string? codigoErro { get; set; }
    public int? idPessoa { get; set; }
    public int? idFuncionario { get; set; }
    public int? idCategoria { get; set; }
    public int? idServico { get; set; }
    public int? idProduto { get; set; }
    public int? idPlano { get; set; }
    public int? idConta { get; set; }
    public int? ambiente { get; set; }
    public bool? ativo { get; set; }
    public bool? emissaoAutomatica { get; set; }
    public bool somenteVigentes { get; set; }
    public DateTime? dataInicio { get; set; }
    public DateTime? dataFim { get; set; }
}

public sealed class RelatorioResultado
{
    public RelatorioCatalogItem Definicao { get; set; } = new();
    public List<RelatorioColuna> Colunas { get; set; } = new();
    public List<Dictionary<string, string>> Linhas { get; set; } = new();
    public int TotalLinhas { get; set; }
    public string MensagemVazia { get; set; } = "Nenhum dado encontrado para os filtros informados.";
}

public sealed class RelatorioFiltrosContexto
{
    public List<RelatorioOpcao> Clientes { get; set; } = new();
    public List<RelatorioOpcao> Funcionarios { get; set; } = new();
    public List<RelatorioOpcao> Categorias { get; set; } = new();
    public List<RelatorioOpcao> Servicos { get; set; } = new();
    public List<RelatorioOpcao> Produtos { get; set; } = new();
    public List<RelatorioOpcao> Planos { get; set; } = new();
    public List<RelatorioOpcao> Contas { get; set; } = new();
}
