namespace CorteCor.Models
{
    public class CrmResumoFaixa
    {
        public string Nome { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal Valor { get; set; }
    }

    public class CrmRelatorioResumo
    {
        public int TotalClientes { get; set; }
        public int ClientesSemContato30Dias { get; set; }
        public int ClientesComTarefasAtrasadas { get; set; }
        public int OportunidadesAbertas { get; set; }
        public decimal ValorPipelineAberto { get; set; }
        public int CampanhasEnviadasPeriodo { get; set; }
        public List<CrmResumoFaixa> ClientesPorStatus { get; set; } = new();
        public List<CrmResumoFaixa> ClientesPorTemperatura { get; set; } = new();
        public List<CrmResumoFaixa> InteracoesPorCanal { get; set; } = new();
        public List<CrmResumoFaixa> TarefasPorStatus { get; set; } = new();
        public List<CrmResumoFaixa> OportunidadesPorEtapa { get; set; } = new();
        public List<CrmResumoFaixa> CampanhasPorCanal { get; set; } = new();
        public List<CrmClienteResumo> ClientesEmRisco { get; set; } = new();
        public List<CrmClienteResumo> ProximasAcoes { get; set; } = new();
        public List<CrmCampanha> UltimasCampanhas { get; set; } = new();
    }
}
