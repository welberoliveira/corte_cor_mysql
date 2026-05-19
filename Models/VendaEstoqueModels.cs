using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models;

public static class VendaProdutoStatus
{
    public const string Finalizada = "Finalizada";
    public const string Ajustada = "Ajustada";
    public const string Cancelada = "Cancelada";
}

public static class VendaProdutoTipoItem
{
    public const string Produto = "Produto";
    public const string Servico = "Servico";
}

public static class MovimentoEstoqueTipo
{
    public const string Entrada = "Entrada";
    public const string Saida = "Saida";
    public const string Ajuste = "Ajuste";
    public const string Estorno = "Estorno";
}

public static class MovimentoEstoqueOrigem
{
    public const string Venda = "Venda";
    public const string Compra = "Compra";
    public const string AjusteManual = "AjusteManual";
    public const string CancelamentoVenda = "CancelamentoVenda";
    public const string CancelamentoCompra = "CancelamentoCompra";
    public const string PosVenda = "PosVenda";
}

public static class VendaPosVendaTipo
{
    public const string Devolucao = "Devolucao";
    public const string Troca = "Troca";
    public const string CancelamentoParcial = "CancelamentoParcial";
    public const string CancelamentoTotal = "CancelamentoTotal";
}

public static class VendaPosVendaStatus
{
    public const string Processada = "Processada";
}

public static class VendaPosVendaRegistroTipo
{
    public const string Origem = "Origem";
    public const string Reposicao = "Reposicao";
}

public class VendaProduto
{
    public int IdVendaProduto { get; set; }
    public int IdSalao { get; set; }
    public int? IdPessoa { get; set; }
    public int? IdMeioPagamento { get; set; }
    [StringLength(40)]
    public string Status { get; set; } = VendaProdutoStatus.Finalizada;
    [StringLength(80)]
    public string? TipoPagamento { get; set; }
    [StringLength(20)]
    public string Recorrencia { get; set; } = RecorrenciaTipo.Nenhuma;
    public bool RecebidoNaHora { get; set; } = true;
    public bool SolicitarEmissaoFiscalServico { get; set; }
    public decimal SubtotalProdutos { get; set; }
    public decimal SubtotalServicos { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal ValorTotal { get; set; }
    [StringLength(1000)]
    public string? Observacoes { get; set; }
    [StringLength(40)]
    public string Origem { get; set; } = "Manual";
    [StringLength(160)]
    public string? UsuarioOperador { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.Now;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    public string? NomeCliente { get; set; }
    public string? StatusFiscal { get; set; }
    public string? ClasseStatusFiscal { get; set; }
    public bool PossuiNotaAtiva { get; set; }
    public string? TipoDocumentoFiscal { get; set; }
}

public class VendaProdutoItem
{
    public int IdItemVenda { get; set; }
    public int IdVendaProduto { get; set; }
    public int IdSalao { get; set; }
    [StringLength(20)]
    public string TipoItem { get; set; } = VendaProdutoTipoItem.Produto;
    public int? IdProduto { get; set; }
    public int? IdServico { get; set; }
    [Required]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; } = 1m;
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    [StringLength(10)]
    public string Unidade { get; set; } = "UN";
    public bool ControlaEstoque { get; set; }
    [StringLength(20)]
    public string? CodigoTributacaoMunicipio { get; set; }
    public decimal? AliquotaIss { get; set; }
    [StringLength(20)]
    public string? Ncm { get; set; }
    [StringLength(10)]
    public string? Cfop { get; set; }
    public decimal QuantidadeCancelada { get; set; }
    public decimal QuantidadeDevolvida { get; set; }
    public decimal QuantidadeTrocada { get; set; }

    public decimal QuantidadeDisponivelPosVenda =>
        Math.Max(0m, Quantidade - QuantidadeCancelada - QuantidadeDevolvida - QuantidadeTrocada);
}

public class VendaPosVenda
{
    public int IdPosVenda { get; set; }
    public int IdSalao { get; set; }
    public int IdVendaProduto { get; set; }
    [StringLength(30)]
    public string TipoOperacao { get; set; } = VendaPosVendaTipo.Devolucao;
    [StringLength(20)]
    public string Status { get; set; } = VendaPosVendaStatus.Processada;
    public decimal ValorCredito { get; set; }
    public decimal ValorReposicao { get; set; }
    public decimal DiferencaFinanceira { get; set; }
    [StringLength(1000)]
    public string? Observacoes { get; set; }
    [StringLength(160)]
    public string? UsuarioOperador { get; set; }
    public DateTime DataOperacao { get; set; } = DateTime.Now;

    public string? NomeCliente { get; set; }
}

public class VendaPosVendaItem
{
    public int IdPosVendaItem { get; set; }
    public int IdPosVenda { get; set; }
    public int IdSalao { get; set; }
    public int IdVendaProduto { get; set; }
    public int? IdItemVenda { get; set; }
    [StringLength(20)]
    public string TipoRegistro { get; set; } = VendaPosVendaRegistroTipo.Origem;
    [StringLength(20)]
    public string TipoItem { get; set; } = VendaProdutoTipoItem.Produto;
    public int? IdProduto { get; set; }
    public int? IdServico { get; set; }
    [Required]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    [StringLength(10)]
    public string Unidade { get; set; } = "UN";
    public bool ControlaEstoque { get; set; }
}

public class MovimentoEstoque
{
    public Guid IdMovimento { get; set; } = Guid.NewGuid();
    public int IdSalao { get; set; }
    public int IdProduto { get; set; }
    public int? IdVendaProduto { get; set; }
    [StringLength(20)]
    public string TipoMovimento { get; set; } = MovimentoEstoqueTipo.Ajuste;
    [StringLength(40)]
    public string Origem { get; set; } = MovimentoEstoqueOrigem.AjusteManual;
    public decimal Quantidade { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoPosterior { get; set; }
    [StringLength(500)]
    public string? Observacao { get; set; }
    [StringLength(160)]
    public string? UsuarioOperador { get; set; }
    public DateTime DataMovimento { get; set; } = DateTime.Now;

    public string? NomeProduto { get; set; }
}

public class VendaProdutoFiltro
{
    public string? Pesquisa { get; set; }
    public int? IdPessoa { get; set; }
    public string? Status { get; set; }
    public string? Recorrencia { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class EstoqueMovimentoFiltro
{
    public string? Pesquisa { get; set; }
    public int? IdProduto { get; set; }
    public string? TipoMovimento { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public class ProdutoEstoquePosicao
{
    public int IdProduto { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoProprio { get; set; }
    public string? CategoriaNome { get; set; }
    public bool ControlarEstoque { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal? PrecoCusto { get; set; }
    public bool EstoqueBaixo => ControlarEstoque && EstoqueAtual <= EstoqueMinimo;
}

public class VendaCheckoutContexto
{
    public List<Pessoa> Clientes { get; set; } = new();
    public List<Produto> Produtos { get; set; } = new();
    public List<Servico> Servicos { get; set; } = new();
    public List<MeioPagamento> MeiosPagamento { get; set; } = new();
    public PagedResult<VendaProduto> VendasRecentes { get; set; } = new();
}

public class VendaDetalheContexto
{
    public VendaProduto? Venda { get; set; }
    public List<VendaProdutoItem> Itens { get; set; } = new();
    public List<FinanceiroTitulo> Titulos { get; set; } = new();
    public List<NotaFiscal> Notas { get; set; } = new();
    public List<VendaPosVenda> HistoricoPosVenda { get; set; } = new();
    public List<Produto> Produtos { get; set; } = new();
    public List<Servico> Servicos { get; set; } = new();
}

public class VendaItemInput
{
    public string TipoItem { get; set; } = VendaProdutoTipoItem.Produto;
    public int? IdProduto { get; set; }
    public int? IdServico { get; set; }
    public decimal Quantidade { get; set; } = 1m;
    public decimal? ValorUnitario { get; set; }
}

public class VendaCheckoutInput
{
    public int? IdPessoa { get; set; }
    public int? IdMeioPagamento { get; set; }
    public int? IdPlano { get; set; }
    public int? IdConta { get; set; }
    public string? TipoPagamento { get; set; }
    public bool RecebidoNaHora { get; set; } = true;
    public bool EmitirNotaFiscalServico { get; set; }
    public string? Recorrencia { get; set; }
    public int NumeroParcelas { get; set; } = 1;
    public DateTime? PrimeiroVencimento { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public string? Observacoes { get; set; }
    public List<VendaItemInput> Itens { get; set; } = new();
}

public class VendaOperacaoResult
{
    public bool Success { get; set; }
    public int? IdVendaProduto { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string MensagemTipo { get; set; } = "info";
    public Guid? IdTituloFinanceiro { get; set; }
    public List<Guid> NotasFiscaisGeradas { get; set; } = new();
    public List<string> Logs { get; set; } = new();
}

public class VendaPosVendaItemInput
{
    public int IdItemVenda { get; set; }
    public decimal Quantidade { get; set; }
}

public class VendaPosVendaInput
{
    public string TipoOperacao { get; set; } = VendaPosVendaTipo.Devolucao;
    public string? Observacoes { get; set; }
    public List<VendaPosVendaItemInput> ItensOriginais { get; set; } = new();
    public List<VendaItemInput> ItensReposicao { get; set; } = new();
}

public class VendaPosVendaOperacaoResult
{
    public bool Success { get; set; }
    public int? IdVendaProduto { get; set; }
    public int? IdPosVenda { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string MensagemTipo { get; set; } = "info";
    public decimal ValorCredito { get; set; }
    public decimal ValorReposicao { get; set; }
    public decimal DiferencaFinanceira { get; set; }
    public string? ResumoFinanceiro { get; set; }
}

public class EstoqueResumo
{
    public int ProdutosComControle { get; set; }
    public int ProdutosComEstoqueBaixo { get; set; }
    public decimal ValorCustoEstoque { get; set; }
    public decimal ValorVendaEstoque { get; set; }
}
