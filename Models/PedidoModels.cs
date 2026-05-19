using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models;

public static class PedidoStatus
{
    public const string Aberto = "Aberto";
    public const string Vencido = "Vencido";
    public const string Convertido = "Convertido";
    public const string Cancelado = "Cancelado";
}

public class Pedido
{
    public int IdPedido { get; set; }
    public int IdSalao { get; set; }
    public int? IdPessoa { get; set; }
    public int? IdMeioPagamento { get; set; }
    [StringLength(40)]
    public string Status { get; set; } = PedidoStatus.Aberto;
    [StringLength(80)]
    public string? TipoPagamento { get; set; }
    public DateTime ValidoAte { get; set; } = DateTime.Today.AddDays(7);
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
    public int? IdVendaProduto { get; set; }
    public DateTime DataPedido { get; set; } = DateTime.Now;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    public string? NomeCliente { get; set; }
    public string? StatusFiscal { get; set; }
    public string? ClasseStatusFiscal { get; set; }
    public bool PossuiServico { get; set; }
    public int QuantidadeItens { get; set; }
    public bool Expirado => Status == PedidoStatus.Aberto && ValidoAte.Date < DateTime.Today;
    public bool PodeConverter => (Status == PedidoStatus.Aberto || Status == PedidoStatus.Vencido) && !IdVendaProduto.HasValue;
}

public class PedidoItem
{
    public int IdItemPedido { get; set; }
    public int IdPedido { get; set; }
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
}

public class PedidoFiltro
{
    public string? Pesquisa { get; set; }
    public int? IdPessoa { get; set; }
    public string? Status { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public bool SomenteVigentes { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PedidoCheckoutInput
{
    public int? IdPessoa { get; set; }
    public int? IdMeioPagamento { get; set; }
    public string? TipoPagamento { get; set; }
    public DateTime ValidoAte { get; set; } = DateTime.Today.AddDays(7);
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public string? Observacoes { get; set; }
    public List<VendaItemInput> Itens { get; set; } = new();
}

public class PedidoConversaoInput
{
    public int IdPedido { get; set; }
    public int? IdMeioPagamento { get; set; }
    public int? IdPlano { get; set; }
    public int? IdConta { get; set; }
    public bool RecebidoNaHora { get; set; } = true;
    public bool EmitirNotaFiscalServico { get; set; }
    public int NumeroParcelas { get; set; } = 1;
    public DateTime? PrimeiroVencimento { get; set; }
    public string? ObservacoesConversao { get; set; }
}

public class PedidoOperacaoResult
{
    public bool Success { get; set; }
    public int? IdPedido { get; set; }
    public int? IdVendaProduto { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string MensagemTipo { get; set; } = "info";
}

public class PedidoContexto
{
    public List<Pessoa> Clientes { get; set; } = new();
    public List<Produto> Produtos { get; set; } = new();
    public List<Servico> Servicos { get; set; } = new();
    public List<MeioPagamento> MeiosPagamento { get; set; } = new();
    public PagedResult<Pedido> PedidosRecentes { get; set; } = new();
}

public class PedidoPainelResumo
{
    public int PedidosListados { get; set; }
    public int PedidosVigentes { get; set; }
    public int PedidosVencendoHoje { get; set; }
    public int PedidosConvertidos { get; set; }
    public decimal ValorListagem { get; set; }
}
