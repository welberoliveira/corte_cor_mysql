using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models
{
    public static class FinanceiroTipoTitulo
    {
        public const string Receber = "Receber";
        public const string Pagar = "Pagar";
    }

    public static class FinanceiroStatusTitulo
    {
        public const string Aberto = "Aberto";
        public const string Liquidado = "Liquidado";
        public const string Vencido = "Vencido";
        public const string Cancelado = "Cancelado";
    }

public static class FinanceiroOrigemTitulo
{
    public const string Manual = "Manual";
    public const string PagamentoAgendamento = "PagamentoAgendamento";
    public const string PagamentoAvulso = "PagamentoAvulso";
    public const string Venda = "Venda";
    public const string Compra = "Compra";
    public const string PosVenda = "PosVenda";
}

public static class RecorrenciaTipo
{
    public const string Nenhuma = "Nenhuma";
    public const string Mensal = "Mensal";
}

    public class FinanceiroTitulo
    {
        public Guid IdTitulo { get; set; }
        public int IdSalao { get; set; }
        [Required]
        [StringLength(10)]
        public string Tipo { get; set; } = FinanceiroTipoTitulo.Receber;
        [StringLength(40)]
        public string Origem { get; set; } = FinanceiroOrigemTitulo.Manual;
        public int? IdPessoa { get; set; }
        public int? IdAgendamento { get; set; }
        public int? IdVendaProduto { get; set; }
        public Guid? IdPagamento { get; set; }
        public int? IdPlano { get; set; }
        public int? IdConta { get; set; }
        [Required]
        [StringLength(160)]
        public string Descricao { get; set; } = string.Empty;
        [StringLength(60)]
        public string? Documento { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = FinanceiroStatusTitulo.Aberto;
        [StringLength(20)]
        public string Recorrencia { get; set; } = RecorrenciaTipo.Nenhuma;
        public decimal ValorOriginal { get; set; }
        public decimal ValorLiquidado { get; set; }
        public decimal ValorAberto { get; set; }
        public DateTime DataCompetencia { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataLiquidacao { get; set; }
        public bool Conciliado { get; set; }
        public string? Observacoes { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAtualizacao { get; set; }

        public string? NomePessoa { get; set; }
        public string? NomePlano { get; set; }
        public string? NomeConta { get; set; }
        public string? NomeServico { get; set; }
        public string? TipoPagamento { get; set; }
        public string? CategoriaFluxo { get; set; }
    }

    public class FinanceiroTituloFiltro
    {
        public string? Tipo { get; set; }
        public string? Status { get; set; }
        public string? Recorrencia { get; set; }
        public int? IdGrupoPlano { get; set; }
        public int? IdPlano { get; set; }
        public int? IdConta { get; set; }
        public string? Pesquisa { get; set; }
        public bool SomenteVencidos { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class FinanceiroIndicador
    {
        public string Rotulo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class FinanceiroFluxoCaixaItem
    {
        public DateTime Data { get; set; }
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
        public decimal SaldoAcumulado { get; set; }
    }

    public class FinanceiroFluxoCaixaResumo
    {
        public string Visao { get; set; } = "Mes";
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<FinanceiroFluxoCaixaItem> Linhas { get; set; } = new();
        public decimal TotalEntradas => Linhas.Sum(linha => linha.Entradas);
        public decimal TotalSaidas => Linhas.Sum(linha => linha.Saidas);
        public decimal SaldoFinal => Linhas.LastOrDefault()?.SaldoAcumulado ?? 0m;
    }

    public class FinanceiroDreLinha
    {
        public string Grupo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class FinanceiroDreMovimento
    {
        public string GrupoDRE { get; set; } = string.Empty;
        public int OrdemDRE { get; set; }
        public int? IdPlano { get; set; }
        public string? Codigo { get; set; }
        public string NomePlano { get; set; } = "Sem plano de contas";
        public string? TipoConta { get; set; }
        public string Tipo { get; set; } = FinanceiroTipoTitulo.Receber;
        public int Mes { get; set; }
        public decimal Valor { get; set; }
    }

    public class FinanceiroDreLinhaDemonstrativo
    {
        public string Descricao { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public bool Destaque { get; set; }
        public bool Subtotal { get; set; }
        public bool ResultadoFinal { get; set; }
        public Dictionary<int, decimal> ValoresPorMes { get; set; } = new();
        public decimal Total { get; set; }

        public decimal ObterValor(int mes)
        {
            return ValoresPorMes.TryGetValue(mes, out var valor) ? valor : 0m;
        }
    }

    public class FinanceiroDreResumo
    {
        public string TipoPeriodo { get; set; } = "Mensal";
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<int> Meses { get; set; } = new();
        public List<FinanceiroDreLinhaDemonstrativo> Linhas { get; set; } = new();
        public decimal ReceitaLiquida { get; set; }
        public decimal LucroBruto { get; set; }
        public decimal ResultadoOperacional { get; set; }
        public decimal ResultadoAntesTributos { get; set; }
        public decimal ResultadoLiquido { get; set; }
    }

    public class FinanceiroResumoCategoria
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
    }

    public class FinanceiroClienteResumo
    {
        public string NomeCliente { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
    }

    public class FinanceiroDashboardResumo
    {
        public decimal ReceitasLiquidadas { get; set; }
        public decimal DespesasLiquidadas { get; set; }
        public decimal SaldoOperacional { get; set; }
        public decimal AReceberAberto { get; set; }
        public decimal APagarAberto { get; set; }
        public decimal ReceitasVencidas { get; set; }
        public decimal DespesasVencidas { get; set; }
        public decimal SaldoProjetado { get; set; }
        public decimal TicketMedioRecebido { get; set; }
        public int QuantidadeTitulosAbertos { get; set; }
        public int QuantidadeTitulosVencidos { get; set; }
        public List<FinanceiroFluxoCaixaItem> FluxoCaixa { get; set; } = new();
        public List<FinanceiroDreLinha> DreGerencial { get; set; } = new();
        public List<FinanceiroResumoCategoria> ReceitasPorForma { get; set; } = new();
        public List<FinanceiroResumoCategoria> DespesasPorPlano { get; set; } = new();
        public List<FinanceiroClienteResumo> TopClientes { get; set; } = new();
        public List<FinanceiroTitulo> TitulosCriticos { get; set; } = new();
    }

    public class FinanceiroRelatorioResumo
    {
        public List<FinanceiroTitulo> Titulos { get; set; } = new();
        public List<FinanceiroResumoCategoria> ReceitasPorPlano { get; set; } = new();
        public List<FinanceiroResumoCategoria> DespesasPorPlano { get; set; } = new();
        public List<FinanceiroResumoCategoria> ReceitasPorForma { get; set; } = new();
        public List<FinanceiroResumoCategoria> InadimplenciaPorFaixa { get; set; } = new();
        public List<FinanceiroFluxoCaixaItem> FluxoProjetado { get; set; } = new();
    }
}
