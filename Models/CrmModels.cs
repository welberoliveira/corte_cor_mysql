using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models
{
    public static class CrmCanal
    {
        public const string Email = "Email";
        public const string Sms = "SMS";
        public const string Whatsapp = "WhatsApp";
        public const string Telefone = "Telefone";
        public const string Presencial = "Presencial";
        public const string Sistema = "Sistema";
    }

    public static class CrmStatusTarefa
    {
        public const string Aberta = "Aberta";
        public const string Concluida = "Concluida";
        public const string Cancelada = "Cancelada";
    }

    public static class CrmStatusOportunidade
    {
        public const string Aberta = "Aberta";
        public const string Ganha = "Ganha";
        public const string Perdida = "Perdida";
    }

    public static class CrmSegmentoCampanha
    {
        public const string TodosClientes = "TodosClientes";
        public const string Inativos = "Inativos";
        public const string AniversariantesDoMes = "AniversariantesDoMes";
        public const string PorTag = "PorTag";
        public const string ClienteEspecifico = "ClienteEspecifico";

        public static IReadOnlyList<string> Todos => new[]
        {
            TodosClientes,
            Inativos,
            AniversariantesDoMes,
            PorTag,
            ClienteEspecifico
        };
    }

    public class CrmPessoaPerfil
    {
        public int IdPerfil { get; set; }
        public int IdSalao { get; set; }
        public int IdPessoa { get; set; }
        [StringLength(40)]
        public string StatusRelacionamento { get; set; } = "Cliente";
        [StringLength(80)]
        public string? OrigemLead { get; set; }
        [StringLength(20)]
        public string Temperatura { get; set; } = "Morno";
        public int ScoreRelacionamento { get; set; }
        public bool PermiteEmail { get; set; } = true;
        public bool PermiteSms { get; set; } = true;
        public bool PermiteWhatsapp { get; set; } = true;
        public bool NaoPerturbe { get; set; }
        public DateTime? UltimoContatoEm { get; set; }
        public DateTime? ProximaAcaoEm { get; set; }
        public string? ObservacoesInternas { get; set; }
        public DateTime DataAtualizacao { get; set; }
    }

    public class CrmInteracao
    {
        public int IdInteracao { get; set; }
        public int IdSalao { get; set; }
        public int IdPessoa { get; set; }
        public int? IdUsuario { get; set; }
        [Required]
        [StringLength(30)]
        public string Canal { get; set; } = CrmCanal.Sistema;
        [Required]
        [StringLength(40)]
        public string Tipo { get; set; } = "Manual";
        [Required]
        [StringLength(160)]
        public string Assunto { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public DateTime DataInteracao { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? Referencia { get; set; }
        public bool OrigemSistema { get; set; }
    }

    public class CrmTarefa
    {
        public int IdTarefa { get; set; }
        public int IdSalao { get; set; }
        public int? IdPessoa { get; set; }
        public int? IdUsuarioResponsavel { get; set; }
        [Required]
        [StringLength(160)]
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        [StringLength(20)]
        public string Prioridade { get; set; } = "Media";
        [StringLength(20)]
        public string Status { get; set; } = CrmStatusTarefa.Aberta;
        [StringLength(20)]
        public string? CanalSugerido { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataConclusao { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public string? NomePessoa { get; set; }
        public string? NomeResponsavel { get; set; }
    }

    public class CrmEtapaFunil
    {
        public int IdEtapa { get; set; }
        public int IdSalao { get; set; }
        [Required]
        [StringLength(80)]
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Ganha { get; set; }
        public bool Perdida { get; set; }
        public bool Ativa { get; set; } = true;
    }

    public class CrmOportunidade
    {
        public int IdOportunidade { get; set; }
        public int IdSalao { get; set; }
        public int IdPessoa { get; set; }
        public int IdEtapa { get; set; }
        [Required]
        [StringLength(160)]
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal ValorEstimado { get; set; }
        public int Probabilidade { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = CrmStatusOportunidade.Aberta;
        [StringLength(80)]
        public string? Origem { get; set; }
        public DateTime? PrevisaoFechamento { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime DataAtualizacao { get; set; } = DateTime.Now;
        public DateTime? DataFechamento { get; set; }
        public string? NomePessoa { get; set; }
        public string? NomeEtapa { get; set; }
    }

    public class CrmCampanha
    {
        public int IdCampanha { get; set; }
        public int IdSalao { get; set; }
        [Required]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;
        [Required]
        [StringLength(20)]
        public string Canal { get; set; } = CrmCanal.Email;
        [Required]
        [StringLength(40)]
        public string Segmento { get; set; } = CrmSegmentoCampanha.TodosClientes;
        [StringLength(120)]
        public string? FiltroTag { get; set; }
        public int? DiasInatividade { get; set; }
        public int? IdPessoa { get; set; }
        [StringLength(160)]
        public string? Assunto { get; set; }
        public string Conteudo { get; set; } = string.Empty;
        [StringLength(20)]
        public string Status { get; set; } = "Rascunho";
        public int TotalDestinatarios { get; set; }
        public int TotalSucesso { get; set; }
        public int TotalFalha { get; set; }
        public DateTime? UltimoEnvioEm { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }

    public class CrmCampanhaDestino
    {
        public int IdDestino { get; set; }
        public int IdCampanha { get; set; }
        public int IdSalao { get; set; }
        public int IdPessoa { get; set; }
        public string Canal { get; set; } = CrmCanal.Email;
        public string Destino { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? MensagemErro { get; set; }
        public DateTime DataEnvio { get; set; } = DateTime.Now;
        public string? NomePessoa { get; set; }
    }

    public class CrmContatoCampanha
    {
        public int IdPessoa { get; set; }
        public int IdSalao { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Tags { get; set; }
        public DateTime? DataNascimento { get; set; }
        public DateTime? UltimoAgendamentoEm { get; set; }
        public bool PermiteEmail { get; set; }
        public bool PermiteSms { get; set; }
        public bool PermiteWhatsapp { get; set; }
        public bool NaoPerturbe { get; set; }
    }

    public class CrmClienteResumo
    {
        public int IdPessoa { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? Tags { get; set; }
        public string StatusRelacionamento { get; set; } = "Cliente";
        public string Temperatura { get; set; } = "Morno";
        public DateTime? UltimoAgendamentoEm { get; set; }
        public DateTime? UltimoPagamentoEm { get; set; }
        public DateTime? UltimoContatoEm { get; set; }
        public DateTime? ProximaAcaoEm { get; set; }
        public int TarefasAbertas { get; set; }
        public int OportunidadesAbertas { get; set; }
        public decimal ValorOportunidadesAbertas { get; set; }
    }

    public class CrmEtapaResumo
    {
        public string NomeEtapa { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class CrmDashboardResumo
    {
        public int TotalClientes { get; set; }
        public int ClientesInativos { get; set; }
        public int AniversariantesMes { get; set; }
        public int TarefasAbertas { get; set; }
        public int TarefasAtrasadas { get; set; }
        public int OportunidadesAbertas { get; set; }
        public decimal ValorOportunidadesAbertas { get; set; }
        public int CampanhasEnviadas30Dias { get; set; }
        public List<CrmEtapaResumo> Funil { get; set; } = new();
        public List<CrmTarefa> ProximasTarefas { get; set; } = new();
        public List<CrmClienteResumo> ClientesEmRisco { get; set; } = new();
    }

    public class CrmTimelineItem
    {
        public DateTime Data { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string BadgeClass { get; set; } = "bg-secondary";
    }

    public class CrmClienteDetalhe
    {
        public Pessoa Pessoa { get; set; } = new();
        public CrmPessoaPerfil Perfil { get; set; } = new();
        public CrmClienteResumo Resumo { get; set; } = new();
        public List<CrmTimelineItem> Timeline { get; set; } = new();
        public List<CrmTarefa> TarefasAbertas { get; set; } = new();
        public List<CrmOportunidade> OportunidadesAbertas { get; set; } = new();
        public List<CrmCampanhaDestino> UltimasCampanhas { get; set; } = new();
    }
}
