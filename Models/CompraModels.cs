using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models;

public static class CompraStatus
{
    public const string Aberta = "Aberta";
    public const string Lancada = "Lancada";
    public const string Cancelada = "Cancelada";
}

public class Compra
{
    public int IdCompra { get; set; }
    public int IdSalao { get; set; }
    public int? IdPessoaFornecedor { get; set; }
    public int? IdPlano { get; set; }
    public int? IdConta { get; set; }
    [StringLength(40)]
    public string Status { get; set; } = CompraStatus.Lancada;
    [StringLength(20)]
    public string Recorrencia { get; set; } = RecorrenciaTipo.Nenhuma;
    public bool PagaNaHora { get; set; }
    public decimal ValorTotal { get; set; }
    [StringLength(60)]
    public string? Documento { get; set; }
    [StringLength(1000)]
    public string? Observacoes { get; set; }
    [StringLength(160)]
    public string? UsuarioOperador { get; set; }
    public Guid? IdTituloFinanceiro { get; set; }
    public DateTime DataCompra { get; set; } = DateTime.Now;
    public DateTime DataVencimento { get; set; } = DateTime.Today;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;

    public string? NomeFornecedor { get; set; }
    public string? NomePlano { get; set; }
    public string? NomeConta { get; set; }
    public int QuantidadeItens { get; set; }
}

public class CompraItem
{
    public int IdCompraItem { get; set; }
    public int IdCompra { get; set; }
    public int IdSalao { get; set; }
    public int IdProduto { get; set; }
    [Required]
    [StringLength(160)]
    public string NomeProduto { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class CompraItemInput
{
    public int IdProduto { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}

public class CompraInput
{
    public int? IdPessoaFornecedor { get; set; }
    public int? IdPlano { get; set; }
    public int? IdConta { get; set; }
    public bool PagaNaHora { get; set; }
    public DateTime DataCompra { get; set; } = DateTime.Today;
    public DateTime DataVencimento { get; set; } = DateTime.Today;
    public string? Documento { get; set; }
    public string? Observacoes { get; set; }
    public List<CompraItemInput> Itens { get; set; } = new();
}

public class CompraFiltro
{
    public string? Pesquisa { get; set; }
    public string? Status { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

public class CompraCancelamentoResult
{
    public bool CompraLocalizada { get; set; } = true;
    public bool CanceladaAgora { get; set; }
    public bool EstoqueAjustado { get; set; }
    public bool TituloFinanceiroCancelado { get; set; }
    public int IdCompra { get; set; }
    public int QuantidadeMovimentosEstoque { get; set; }
}
