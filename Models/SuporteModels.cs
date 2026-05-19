using System.ComponentModel.DataAnnotations;

namespace CorteCor.Models;

public static class SuporteChamadoStatus
{
    public const string Solicitado = "Solicitado";
    public const string EmAnalise = "Em análise";
    public const string Concluido = "Concluído";
    public const string Cancelado = "Cancelado";

    public static readonly string[] Todos =
    {
        Solicitado,
        EmAnalise,
        Concluido,
        Cancelado
    };
}

public class SuporteChamado
{
    public Guid IdChamado { get; set; } = Guid.NewGuid();
    public int IdSalao { get; set; }
    [StringLength(160)]
    public string? NomeUsuario { get; set; }
    [StringLength(160)]
    public string? EmailUsuario { get; set; }
    [Required]
    [StringLength(4000)]
    public string Mensagem { get; set; } = string.Empty;
    [StringLength(500)]
    public string? UrlOrigem { get; set; }
    [StringLength(80)]
    public string Status { get; set; } = SuporteChamadoStatus.Solicitado;
    [StringLength(1000)]
    public string? ErroEmail { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public DateTime DataAtualizacao { get; set; } = DateTime.Now;
}

public class SuporteChamadoFiltro
{
    public string? Status { get; set; }
    public string? Pesquisa { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
