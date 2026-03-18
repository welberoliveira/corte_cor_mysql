using System;
using System.Collections.Generic;

namespace CorteCor.Services
{
    public enum FiscalOrigemTipo
    {
        Avulsa = 1,
        Agendamento = 2,
        Venda = 3
    }

    public class FiscalOrigemRequest
    {
        public FiscalOrigemTipo Origem { get; set; } = FiscalOrigemTipo.Avulsa;
        public Guid? IdOrigem { get; set; }
        public int IdSalao { get; set; }
        public string? ReferenciaExterna { get; set; }
    }

    public class FiscalOrigemCliente
    {
        public int? IdPessoa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? CpfCnpj { get; set; }
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Logradouro { get; set; }
        public string? Numero { get; set; }
        public string? Bairro { get; set; }
        public string? Cep { get; set; }
        public string? Cidade { get; set; }
        public string? UF { get; set; }
        public int? CodigoMunicipioIbge { get; set; }
    }

    public class FiscalOrigemItem
    {
        public int? IdServico { get; set; }
        public int? IdProduto { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Quantidade { get; set; } = 1;
        public decimal ValorUnitario { get; set; }
        public string? CodigoTributacaoMunicipio { get; set; }
        public decimal? AliquotaIss { get; set; }
        public string? Ncm { get; set; }
        public string? Cfop { get; set; }
    }

    public class FiscalOrigemEnvelope
    {
        public FiscalOrigemRequest Origem { get; set; } = new();
        public string TipoNota { get; set; } = "NFS-e";
        public string NaturezaOperacao { get; set; } = "Prestacao de servico";
        public DateTime DataCompetencia { get; set; } = DateTime.Now;
        public FiscalOrigemCliente Cliente { get; set; } = new();
        public List<FiscalOrigemItem> Itens { get; set; } = new();
        public string? Observacoes { get; set; }
    }

    public class FiscalOrigemAgendamentoPayload
    {
        public int IdSalao { get; set; }
        public int IdAgendamento { get; set; }
        public DateTime DataHora { get; set; }
        public FiscalOrigemCliente Cliente { get; set; } = new();
        public List<FiscalOrigemItem> Itens { get; set; } = new();
        public string? Observacoes { get; set; }
    }

    public class FiscalOrigemVendaPayload
    {
        public int IdSalao { get; set; }
        public int IdVendaProduto { get; set; }
        public DateTime DataVenda { get; set; } = DateTime.Now;
        public FiscalOrigemCliente Cliente { get; set; } = new();
        public List<FiscalOrigemItem> Itens { get; set; } = new();
        public string? Observacoes { get; set; }
    }
}
