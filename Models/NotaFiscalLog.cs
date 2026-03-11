using System;

namespace CorteCor.Models
{
    public class NotaFiscalLog
    {
        public int IdLog { get; set; }
        public Guid? IdNotaFiscal { get; set; }
        public int? IdAgendamento { get; set; }
        public int IdSalao { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string TipoEvento { get; set; } = string.Empty;
        public string RequestPayload { get; set; } = string.Empty;
        public string? ResponsePayload { get; set; }
        public string? CodigoErro { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? Usuario { get; set; }
    }
}
