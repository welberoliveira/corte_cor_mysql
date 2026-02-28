using System;

namespace CorteCor.Models
{
    public class NotaFiscalLog
    {
        public Guid IdLog { get; set; } = Guid.NewGuid();
        public Guid? IdNotaFiscal { get; set; }
        public int? IdAgendamento { get; set; }
        public int IdSalao { get; set; }
        public DateTime DataHora { get; set; } = DateTime.Now;
        public string Etapa { get; set; } = string.Empty;
        public string MensagemStatus { get; set; } = string.Empty;
        public string? ConteudoXml { get; set; }
    }
}
