using CorteCor.Models;

namespace CorteCor.Handlers
{
    public interface IFinanceiroModuloHandler
    {
        Task SincronizarTitulosPagamentoAsync(int idSalao);
        Task<PagedResult<FinanceiroTitulo>> ListarTitulosAsync(int idSalao, FinanceiroTituloFiltro filtro);
        Task<FinanceiroTitulo?> ObterTituloAsync(int idSalao, Guid idTitulo);
        Task<List<FinanceiroTitulo>> ListarTitulosPorVendaAsync(int idSalao, int idVendaProduto);
        Task<Guid> SalvarTituloAsync(FinanceiroTitulo titulo);
        Task AtualizarStatusTituloAsync(int idSalao, Guid idTitulo, string status, DateTime? dataLiquidacao, decimal? valorLiquidado, bool? conciliado);
        Task AtualizarValoresTituloAsync(int idSalao, Guid idTitulo, decimal valorOriginal, decimal valorLiquidado, decimal valorAberto, string status, DateTime? dataLiquidacao, bool conciliado, string? observacoes);
        Task<List<PlanoContas>> ListarPlanoContasAsync(int idSalao);
        Task<List<PlanoContas>> ListarGruposPlanoContasAsync(int idSalao);
        Task<List<PlanoContas>> ListarContasAnaliticasPorGrupoAsync(int idSalao, int idGrupoPlano);
        Task SavePlanoContasAsync(PlanoContas plano);
        Task<List<ContaCaixa>> ListarContasCaixaAsync(int idSalao);
        Task SaveContaCaixaAsync(ContaCaixa conta);
        Task<FinanceiroDashboardResumo> ObterDashboardAsync(int idSalao, DateTime dataInicio, DateTime dataFim);
        Task<FinanceiroRelatorioResumo> ObterRelatoriosAsync(int idSalao, DateTime dataInicio, DateTime dataFim);
        Task<List<FinanceiroFluxoCaixaItem>> ObterFluxoCaixaAsync(int idSalao, DateTime dataInicio, DateTime dataFim, bool projetado);
        Task<List<FinanceiroDreMovimento>> ObterMovimentosDreAsync(int idSalao, DateTime dataInicio, DateTime dataFim);
    }
}
