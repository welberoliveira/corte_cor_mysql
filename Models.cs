using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CorteCor;

public class Models
{
    public class Administrador
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }
        public string Perfil { get; set; }
        public string Status { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nome { get; set; }
        public string Sobrenome { get; set; }
        public string CPF { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public DateTime DataEntrada { get; set; }
        public string Status { get; set; }
        public bool Relacionado { get; set; }
        public string Senha { get; set; }
        public int IdSalao { get; set; }
    }


    // vai ser o salao
    public class Salao
    {
        public int IdSalao { get; set; }
        public string Nome { get; set; }
        public string Responsavel { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string Endereco { get; set; }
        public string CNPJ { get; set; }
        public string Status { get; set; }
        public string Observacao { get; set; }
        public DateTime DataCadastro { get; set; }

    }

    //cliente
    public class PessoaFicha
    {
        public int PessoaID { get; set; }
        public int FichaID { get; set; }
        public string? Nome { get; set; }
        public string? Filiacao { get; set; }
        public string? RG { get; set; }
        public string? CPF { get; set; }
        public DateTime? DataNascimento { get; set; }
        public string? Nacionalidade { get; set; }
        public string? NIS { get; set; }
        public string? EstadoCivil { get; set; }
        public string? RegimeCasamento { get; set; }
        public string? SituacaoProfissional { get; set; }
        public string? Profissao { get; set; }
        public string? GrauInstrucao { get; set; }
        public bool? Iletrado { get; set; }
        public string? Empresa { get; set; }
        public bool? CarteiraAssinada { get; set; }
        public decimal? RendaMensal { get; set; }
        public string? Endereco { get; set; }
        public string? Quadra { get; set; }
        public string? PontoReferencia { get; set; }
        public string? Bairro { get; set; }
        public int? Lote { get; set; }
        public string? MunicipioResidencia { get; set; }
        public string? Telefone { get; set; }
        public string? Celular { get; set; }
        
        // Campos do cônjuge
        public string? ConjugeNome { get; set; }
        public string? ConjugeFiliacao { get; set; }
        public string? ConjugeRG { get; set; }
        public string? ConjugeCPF { get; set; }
        public int? ConjugeIdade { get; set; }
        public string? ConjugeNacionalidade { get; set; }
        public string? ConjugeSituacaoProfissional { get; set; }
        public string? ConjugeProfissao { get; set; }
        public string? ConjugeGrauInstrucao { get; set; }
        public bool? ConjugeIletrado { get; set; }
        public string? ConjugeEmpresa { get; set; }
        public bool? ConjugeCarteiraAssinada { get; set; }
        public decimal? ConjugeRendaMensal { get; set; }
    }


    public class Funcionario
    {
        public int IdFuncionario { get; set; }
        public string Nome { get; set; }

        public bool seg { get; set; }
        public TimeSpan? seg_ini { get; set; }
        public TimeSpan? seg_fim { get; set; }

        public bool ter { get; set; }
        public TimeSpan? ter_ini { get; set; }
        public TimeSpan? ter_fim { get; set; }

        public bool qua { get; set; }
        public TimeSpan? qua_ini { get; set; }
        public TimeSpan? qua_fim { get; set; }

        public bool qui { get; set; }
        public TimeSpan? qui_ini { get; set; }
        public TimeSpan? qui_fim { get; set; }

        public bool sex { get; set; }
        public TimeSpan? sex_ini { get; set; }
        public TimeSpan? sex_fim { get; set; }

        public bool sab { get; set; }
        public TimeSpan? sab_ini { get; set; }
        public TimeSpan? sab_fim { get; set; }

        public bool dom { get; set; }
        public TimeSpan? dom_ini { get; set; }
        public TimeSpan? dom_fim { get; set; }

        public int IdSalao { get; set; }
    }

    public class Servico
    {
        public int IdServico { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public int IdSalao { get; set; }
        public TimeSpan Duracao { get; set; }

    }

    public class FuncionarioServico
    {
        public int IdFuncionario { get; set; }
        public int IdServico { get; set; }
    }

    public class Pessoa
    {
        public int IdPessoa { get; set; }
        public string Nome { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }
        public DateTime? DataNascimento { get; set; }
        public int IdSalao { get; set; }
    }

    public class Agendamento
    {
        public int IdAgendamento { get; set; }
        public DateTime DataHora { get; set; }
        public string Status { get; set; }

        public int IdServico { get; set; }
        public int IdPessoa { get; set; }
        public int IdFuncionario { get; set; }
    }

    public class MeioPagamento
    {
        public int IdMeioPagamento { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Gateway { get; set; }

        public bool PermiteParcelamento { get; set; }
        public byte? ParcelasMax { get; set; }

        public decimal TaxaPercentual { get; set; }
        public decimal TaxaFixa { get; set; }

        public short PrazoRecebimentoDias { get; set; }
        public bool Ativo { get; set; }

        public int IdSalao { get; set; }
        public DateTime DataCadastro { get; set; }
    }

    public class Pagamento
    {
        public Guid IdPagamento { get; set; }
        public int IdAgendamento { get; set; }
        public bool Ativo { get; set; }
        public string Status { get; set; }
        public decimal Valor { get; set; }
        public string Moeda { get; set; }
        public string? Descricao { get; set; }

        public string? MercadoPagoPreferenceId { get; set; }
        public string? MercadoPagoPaymentId { get; set; }
        public string? CheckoutUrl { get; set; }

        public string? MpStatus { get; set; }
        public string? MpStatusDetail { get; set; }

        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
        public DateTime? PagoEm { get; set; }

        // Legacy fields for backward compatibility
        public int IdMeioPagamento { get; set; }
        public string? Tipo { get; set; }
        public DateTime Data { get; set; }
        public string? Contos { get; set; }
        public string? Campos { get; set; }

        [NotMapped]
        public string? NomeCliente { get; set; }
        [NotMapped]
        public string? NomeServico { get; set; }
        [NotMapped]
        public DateTime? DataAgendamento { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? Code { get; set; }
    }

    public class PagamentoFiltroDTO
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? Status { get; set; }
        public string? NomeCliente { get; set; }
        public DateTime? DataAgendamento { get; set; }
    }
}
