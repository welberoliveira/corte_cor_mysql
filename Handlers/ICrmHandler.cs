using CorteCor.Models;

namespace CorteCor.Handlers
{
    public interface ICrmHandler
    {
        void GarantirEtapasPadrao(int idSalao);
        CrmPessoaPerfil ObterOuCriarPerfil(int idSalao, int idPessoa);
        void SalvarPerfil(CrmPessoaPerfil perfil);
        PagedResult<CrmClienteResumo> ListarClientesResumo(int idSalao, string? pesquisa, int pageIndex, int pageSize);
        CrmClienteResumo ObterClienteResumo(int idSalao, int idPessoa);
        List<CrmTimelineItem> ListarTimeline(int idSalao, int idPessoa, int limit = 100);
        List<CrmInteracao> ListarInteracoes(int idSalao, int idPessoa, int limit = 50);
        void AdicionarInteracao(CrmInteracao interacao);
        int SalvarInteracao(CrmInteracao interacao);
        PagedResult<CrmTarefa> ListarTarefas(int idSalao, int? idPessoa, string? status, int? idUsuarioResponsavel, int pageIndex, int pageSize, string? pesquisa = null, DateTime? dataVencimentoInicio = null, DateTime? dataVencimentoFim = null);
        int SalvarTarefa(CrmTarefa tarefa);
        void AtualizarStatusTarefa(int idSalao, int idTarefa, string status, DateTime? dataConclusao);
        List<CrmEtapaFunil> ListarEtapas(int idSalao);
        List<CrmOportunidade> ListarOportunidades(int idSalao, int? idPessoa, string? status);
        PagedResult<CrmOportunidade> ListarOportunidadesPaginadas(int idSalao, int? idPessoa, string? status, DateTime? dataInicio, DateTime? dataFim, int pageIndex, int pageSize);
        int SalvarOportunidade(CrmOportunidade oportunidade);
        void AtualizarEtapaOportunidade(int idSalao, int idOportunidade, int idEtapa, string status, DateTime? dataFechamento);
        PagedResult<CrmCampanha> ListarCampanhas(int idSalao, int pageIndex, int pageSize);
        int SalvarCampanha(CrmCampanha campanha);
        CrmCampanha? ObterCampanha(int idSalao, int idCampanha);
        List<CrmContatoCampanha> ListarPublicoCampanha(int idSalao, string segmento, string? filtroTag, int? diasInatividade, int? idPessoa);
        void RegistrarDestinoCampanha(CrmCampanhaDestino destino);
        void AtualizarResumoCampanha(int idSalao, int idCampanha, string status, int totalDestinatarios, int totalSucesso, int totalFalha, DateTime? ultimoEnvioEm);
        List<CrmCampanhaDestino> ListarDestinosCampanha(int idSalao, int idCampanha, int limit = 100);
        CrmDashboardResumo ObterDashboard(int idSalao);
        CrmRelatorioResumo ObterRelatorios(int idSalao, DateTime dataInicio, DateTime dataFim);
    }
}
