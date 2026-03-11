namespace CorteCor.Models
{
    public class LogAcesso
    {
        public int Id { get; set; }
        public string Usuario { get; set; }
        public DateTime DataHora { get; set; }
        public string IP_Origem { get; set; }
        public string CredencialUsada { get; set; }
        public bool Sucesso { get; set; }
    }
}
