using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models;

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
    public int LimiteEnvioEmail { get; set; }
    public int LimiteEnvioSMS { get; set; }
    public int LimiteEnvioWhatsapp { get; set; }

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
    [Required(ErrorMessage = "Informe o nome do funcionário.")]
    [StringLength(120, ErrorMessage = "O nome do funcionário deve ter no máximo 120 caracteres.")]
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
    [Required(ErrorMessage = "Informe o nome do serviço.")]
    [StringLength(160, ErrorMessage = "O nome do serviço deve ter no máximo 160 caracteres.")]
    public string Nome { get; set; }
    [Range(typeof(decimal), "0.01", "9999999", ErrorMessage = "O preço do serviço deve ser maior que zero.")]
    public decimal Preco { get; set; }
    public decimal? PrecoCusto { get; set; }
    public decimal? MargemContribuicao { get; set; }
    public int IdSalao { get; set; }
    public TimeSpan Duracao { get; set; }
    public int? IdCategoria { get; set; }
    public string? CategoriaNome { get; set; }

    // Dados Gerais e Complementares
    public string? Tags { get; set; }
    public string? Anotacoes { get; set; }
    public bool Arquivado { get; set; }

    // Campos Fiscais
    public string? CodigoTributacaoMunicipio { get; set; }
    public string? Cnae { get; set; }
    public decimal? AliquotaISS { get; set; }
    public string? ItemListaServicoLC116 { get; set; }
    public string? IdCnae { get; set; }
    public string? CodTributacaoNacional { get; set; }
    public string? CodNBS { get; set; }
}

public class FuncionarioServico
{
    public int IdFuncionario { get; set; }
    public int IdServico { get; set; }
}

public class Pessoa
{
    public int IdPessoa { get; set; }
    [Required(ErrorMessage = "Informe o nome da pessoa.")]
    [StringLength(160, ErrorMessage = "O nome deve ter no máximo 160 caracteres.")]
    public string Nome { get; set; }
    [Phone(ErrorMessage = "Informe um telefone válido.")]
    public string Telefone { get; set; }
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; }
    public DateTime? DataNascimento { get; set; }
    public int IdSalao { get; set; }
    public bool Excluido { get; set; }

    // Campos Fiscais e de Endereço
    [StringLength(18, ErrorMessage = "CPF/CNPJ deve ter no máximo 18 caracteres.")]
    public string? CpfCnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? UF { get; set; }

    // Campos de Consulta CNPJ (ReceitaWS)
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cnae { get; set; }

    // Tipo de Contato
    public bool IsCliente { get; set; } = true;
    public bool IsFornecedor { get; set; }
    public bool IsTransportador { get; set; }

    // Dados Contato e Estrangeiro
    public string? NomeContato { get; set; }
    public string? Pais { get; set; }
    public string? IdEstrangeiro { get; set; }

    // Endereço de Entrega
    public string? EntCep { get; set; }
    public string? EntUf { get; set; }
    public string? EntCidade { get; set; }
    public string? EntNome { get; set; }
    public string? EntCpfCnpj { get; set; }
    public string? EntInscricaoEstadual { get; set; }
    public string? EntLogradouro { get; set; }
    public string? EntNumero { get; set; }
    public string? EntComplemento { get; set; }
    public string? EntBairro { get; set; }
    public string? EntEmail { get; set; }
    public string? EntTelefone { get; set; }

    // Fiscais (Adicionais)
    public bool? ConsumidorFinal { get; set; }
    public int? IndicadorIE { get; set; }
    public string? IESubstTrib { get; set; }
    public string? Suframa { get; set; }

    // Outras Informações
    public string? Tags { get; set; }
    public DateTime? DataComemorativa { get; set; }
    public string? DescricaoComemoracao { get; set; }
    public string? BasesLegais { get; set; }
    public string? Observacoes { get; set; }
}

public class Agendamento
{
    public int IdAgendamento { get; set; }
    public DateTime DataHora { get; set; }
    public string Status { get; set; }

    public int IdServico { get; set; }
    public int IdPessoa { get; set; }
    public int IdFuncionario { get; set; }
    public bool Excluido { get; set; }
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

    // Mercado Pago Config
    public string? MpAccessTokenProd { get; set; }
    public string? MpAccessTokenSandbox { get; set; }
    public string? MpPublicKeyProd { get; set; }
    public string? MpPublicKeySandbox { get; set; }

    // True = Produção, False = Sandbox
    public bool MpProduction { get; set; }
}

public class Pagamento
{
    public Guid IdPagamento { get; set; }
    public int? IdSalao { get; set; }
    public int? IdAgendamento { get; set; }
    public int? IdPedido { get; set; }
    public int? IdVendaProduto { get; set; }
    public string OrigemPagamento { get; set; } = CorteCor.Models.OrigemPagamento.Avulso;
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

public static class OrigemPagamento
{
    public const string Agendamento = "Agendamento";
    public const string Pedido = "Pedido";
    public const string Venda = "Venda";
    public const string Avulso = "Avulso";
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
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ModeloEmail
{
    public int IdModelo { get; set; }
    public int IdSalao { get; set; }
    public string TipoEvento { get; set; } // 'BoasVindas', 'ConfirmacaoAgendamento', etc.
    public string Assunto { get; set; }
    public string CorpoHTML { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class ModeloSMS
{
    public int IdModelo { get; set; }
    public int IdSalao { get; set; }
    public string TipoEvento { get; set; } // 'BoasVindas', 'ConfirmacaoAgendamento', etc.
    public string Conteudo { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class LembreteConfig
{
    public int IdConfig { get; set; }
    public int IdSalao { get; set; }
    public int AntecedenciaValor { get; set; } // 1, 2, 24
    public string AntecedenciaUnidade { get; set; } // "Horas", "Dias"
    public int? IdModeloEmail { get; set; }
    public int? IdModeloSMS { get; set; }
    public string TipoLembrete { get; set; } = "Email"; // Email, SMS
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }

    [NotMapped]
    public string? AssuntoModelo { get; set; }
}

public class LogEnvioEmail
{
    public int IdLog { get; set; }
    public int IdLembrete { get; set; }
    public int IdAgendamento { get; set; }
    public DateTime DataEnvio { get; set; }
    public string Destinatario { get; set; }
    public string Assunto { get; set; }
    public string Status { get; set; }
    public string? MensagemErro { get; set; }
    public string TipoLembrete { get; set; } = "Email";
    public string? Telefone { get; set; }
}

public class LembreteAgendado
{
    public int IdLembrete { get; set; }
    public int IdAgendamento { get; set; }
    public int IdConfig { get; set; }
    public DateTime DataEnvioProgramada { get; set; }
    public string Status { get; set; } // Pendente, Enviado, Erro
    public int Tentativas { get; set; }
    public string? UltimoErro { get; set; }
    public DateTime? DataEnvioReal { get; set; }
    public DateTime DataCriacao { get; set; }
}

public class LembreteEnvioDTO
{
    public string NomeCliente { get; set; }
    public string EmailCliente { get; set; }
    public string TelefoneCliente { get; set; }
    public DateTime DataHoraAgendamento { get; set; }
    public string NomeServico { get; set; }
    public string NomeProfissional { get; set; }
    public string NomeSalao { get; set; }
    public int IdSalao { get; set; }
    public string? AssuntoModelo { get; set; }
    public string? CorpoModelo { get; set; }
    public string TipoLembrete { get; set; } // "Email" or "SMS"
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;
}

public class FornecedorEmail
{
    public int IdFornecedor { get; set; }
    public string Nome { get; set; }
    public string ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string Endpoint { get; set; }
    public string RemetenteNome { get; set; }
    public string RemetenteEmail { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class FornecedorSMS
{
    public int IdFornecedor { get; set; }
    public string Nome { get; set; }
    public string ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string Endpoint { get; set; }
    public string Remetente { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class FornecedorWhatsapp
{
    public int IdFornecedor { get; set; }
    public string Nome { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string Endpoint { get; set; }
    public string? InstanceId { get; set; }
    public string? Token { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataAtualizacao { get; set; }
}

public class SalaoConfigFiscal
{
    [Key]
    public Guid IdConfigFiscal { get; set; }
    public int IdSalao { get; set; }
    public string Cnpj { get; set; }
    public string RazaoSocial { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public int Ambiente { get; set; }
    public int CodigoMunicipioIBGE { get; set; }
    public int CodigoUFIBGE { get; set; }
    public int RegimeTributario { get; set; }
    public byte[]? CertificadoPfx { get; set; }
    public string? CertificadoBase64 { get; set; }
    public byte[]? CertificadoSenha { get; set; }
    public string? CertificadoSenhaTexto { get; set; }
    public DateTime? CertificadoValidade { get; set; }
    public string? TokenNfse { get; set; }
    public string? CSC { get; set; }
    public string? IdCSC { get; set; }
    public int SerieNFCe { get; set; } = 1;
    public int NumeroNFCe { get; set; } = 1;
    public int SerieNFSe { get; set; } = 1;
    public int NumeroNFSe { get; set; } = 1;
    public int RegimeEspecialTributacao { get; set; }
    public int IssExigibilidade { get; set; } = 1;
    public int IssRetido { get; set; } = 2;
    public string? EnderecoLogradouro { get; set; }
    public string? EnderecoNumero { get; set; }
    public string? EnderecoBairro { get; set; }
    public string? EnderecoCep { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public bool EmissaoAutomatica { get; set; }
    public string? EnderecoCidade { get; set; }
    public string? EnderecoUF { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class NotaFiscal
{
    [Key]
    public Guid IdNotaFiscal { get; set; }
    public int IdSalao { get; set; }
    public int? IdAgendamento { get; set; }
    public int? IdVendaProduto { get; set; }
    public string TipoNota { get; set; }
    public int Ambiente { get; set; }
    public int Numero { get; set; }
    public int Serie { get; set; }
    public decimal ValorTotal { get; set; }
    public string Status { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? ChaveAcessoNacional { get; set; }
    public string? NumeroNFSeNacional { get; set; }
    public string? NumeroRecibo { get; set; }
    public string? ProtocoloAutorizacao { get; set; }
    public string? JustificativaRejeicao { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public DateTime DataEmissao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class NotaFiscalEvento
{
    [Key]
    public Guid IdEvento { get; set; }
    public Guid IdNotaFiscal { get; set; }
    public int IdSalao { get; set; }
    public string TipoEvento { get; set; }
    public string Justificativa { get; set; }
    public string? ProtocoloEvento { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string Status { get; set; }
    public DateTime DataRegistro { get; set; }
}

public class NotaFiscalInutilizacao
{
    [Key]
    public Guid IdInutilizacao { get; set; }
    public int IdSalao { get; set; }
    public int Ano { get; set; }
    public int Modelo { get; set; }
    public int Serie { get; set; }
    public int NumeroInicial { get; set; }
    public int NumeroFinal { get; set; }
    public string Justificativa { get; set; }
    public string Status { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlRetorno { get; set; }
    public DateTime DataInutilizacao { get; set; }
}

public class RetornoEmissaoDto
{
    public bool Autorizada { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? NumeroDocumentoFiscal { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? Motivo { get; set; }
    public int? CodigoStatusSefaz { get; set; }
}

public class CategoriaProduto
{
    public int IdCategoria { get; set; }
    public int IdSalao { get; set; }
    [Required(ErrorMessage = "Informe o nome da categoria.")]
    [StringLength(120, ErrorMessage = "O nome da categoria deve ter no máximo 120 caracteres.")]
    public string Nome { get; set; }
    public bool Ativo { get; set; }
    public DateTime DataCadastro { get; set; }
}

public class Produto
{
    public int IdProduto { get; set; }
    public int IdSalao { get; set; }
    [Required(ErrorMessage = "Informe o nome do produto.")]
    [StringLength(160, ErrorMessage = "O nome do produto deve ter no máximo 160 caracteres.")]
    public string Nome { get; set; }
    public string? CodigoProprio { get; set; }
    public int? IdCategoria { get; set; }
    public string? CategoriaNome { get; set; }
    public string? Tags { get; set; }
    public string? TipoUso { get; set; }
    public bool Arquivado { get; set; }
    public string? Anotacoes { get; set; }
    
    public decimal? PrecoCusto { get; set; }
    [Range(typeof(decimal), "0", "9999999", ErrorMessage = "O preço de venda deve ser maior ou igual a zero.")]
    public decimal PrecoVenda { get; set; }
    public decimal? MargemContribuicao { get; set; }
    
    public bool ControlarEstoque { get; set; }
    public decimal? EstoqueAtual { get; set; }
    public decimal? EstoqueMinimo { get; set; }
    
    public int? Origem { get; set; }
    public string? ReferenciaEAN { get; set; }
    public decimal? PesoLiquido { get; set; }
    public decimal? PesoBruto { get; set; }
    public string? NCM { get; set; }
    public string? CEST { get; set; }
    public string? UnidadeComercial { get; set; }
    public int? ExcecaoIPI { get; set; }
    public string? CodBeneficioFiscalUF { get; set; }
    
    public bool UnidadeTributadaDiferente { get; set; }
    public string? EANTributada { get; set; }
    public string? UnidadeTributada { get; set; }
    public decimal? QuantidadeTributada { get; set; }
    public bool IgnorarTribPrecoVenda { get; set; }
    public string? AnotacoesFiscaisNFe { get; set; }
    
    public int? GrupoTributarioVinculado { get; set; }
    
    public DateTime DataCadastro { get; set; }
    public bool Excluido { get; set; }
}

public class ItemListaServico
{
    public int IdItemListaServico { get; set; }
    public string Codigo { get; set; }
    public string Descricao { get; set; }
}

public class ConfigGeral
{
    [Key]
    public int IdSalao { get; set; }
    public string? NomeFantasia { get; set; }
    public string? LogoUrl { get; set; }
    public string TemaCor { get; set; } = "#0d6efd";
    public bool ModoPDV { get; set; }
    public bool ModoEstoque { get; set; } = true;
    public bool AgendamentoOnline { get; set; }
    public int MinutosAntecedencia { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

public class PlanoContas
{
    [Key]
    public int IdPlano { get; set; }
    public int IdSalao { get; set; }
    public string? Codigo { get; set; }
    public string Descricao { get; set; }
    public string Tipo { get; set; } // 'R' or 'D'
    public bool Ativo { get; set; } = true;
}

public class ContaCaixa
{
    [Key]
    public int IdConta { get; set; }
    public int IdSalao { get; set; }
    public string Nome { get; set; }
    public string? Tipo { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? Conta { get; set; }
    public decimal SaldoInicial { get; set; }
    public bool Ativo { get; set; } = true;
}

public class ConfigPix
{
    [Key]
    public int IdSalao { get; set; }
    public string? ChavePix { get; set; }
    public string? PSP { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public byte[]? Certificado { get; set; }
    public bool Ativo { get; set; }
}

public class ConfigApi
{
    [Key]
    public int IdApi { get; set; }
    public int IdSalao { get; set; }
    public string? NomeApp { get; set; }
    public Guid ApiKey { get; set; } = Guid.NewGuid();
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime? UltimoAcesso { get; set; }
    public bool Ativo { get; set; } = true;
}
