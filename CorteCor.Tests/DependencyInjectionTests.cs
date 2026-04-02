using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CorteCor.Handlers;
using Xunit;
using CorteCor;
using CorteCor.Services;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using CorteCor.Models;

namespace CorteCor.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void Verify_DI_Configuration_For_All_PageModels()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        var configuration = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["FiscalSettings:MasterKey"] = "12345678901234567890123456789012",
                                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Fake;Trusted_Connection=True;"
                            })
                            .Build();

                        services.AddSingleton<IConfiguration>(configuration);
                        services.AddRazorPages();
                        services.AddLogging();
                        services.AddHttpClient();
                        services.AddMemoryCache();
                        services.AddAuthentication("CookieAuth").AddCookie("CookieAuth", o => { });
                        services.AddAuthorization(o => {
                            o.AddPolicy("AdminPolicy", p => p.RequireClaim("Role"));
                            o.AddPolicy("UsuarioPolicy", p => p.RequireClaim("Role"));
                        });

                        var mockDbHandler = new Mock<IDatabaseHandler>();
                        services.AddScoped<IDatabaseHandler>(_ => mockDbHandler.Object);
                        
                        services.AddScoped<SalaoHandler>();
                        services.AddScoped<ServicoHandler>();
                        services.AddScoped<PessoaHandler>();
                        services.AddScoped<ProdutoHandler>();
                        services.AddScoped<CategoriaProdutoHandler>();
                        services.AddScoped<ItemListaServicoHandler>();
                        services.AddScoped<AgendamentoHandler>();
                        services.AddScoped<FuncionarioHandler>();
                        services.AddScoped<FuncionarioServicoHandler>();
                        services.AddScoped<PagamentoHandler>();
                        services.AddScoped<FinanceiroHandler>();
                        services.AddScoped<IFinanceiroModuloHandler, FakeFinanceiroModuloHandler>();
                        services.AddScoped<FinanceiroService>();
                        services.AddScoped<IntegracaoHandler>();
                        services.AddScoped<MercadoPagoService>();
                        services.AddScoped<ModeloEmailHandler>();
                        services.AddScoped<ModeloSMSHandler>();
                        services.AddScoped<MeioPagamentoHandler>();
                        services.AddHttpClient<BrevoEmailService>();
                        services.AddHttpClient<SMSMarketService>();
                        services.AddScoped<IWhatsappService, FakeWhatsappService>();
                        services.AddHttpClient<ConsultaDocumentoService>();
                        services.AddScoped<SalaoConfigFiscalHandler>();
                        services.AddScoped<NotaFiscalInutilizacaoHandler>();
                        services.AddScoped<NotaFiscalHandler>();
                        services.AddScoped<NotaFiscalLogHandler>();
                        services.AddSingleton<ICriptografiaService, CriptografiaService>();
                        services.AddScoped<CertificadoFiscalFactory>();
                        services.AddScoped<FiscalBuilderService>();
                        services.AddScoped<FiscalActionService>();
                        services.AddScoped<FiscalPdfGenerator>();
                        services.AddScoped<FiscalOrigemPreparationService>();
                        services.AddScoped<AgendamentoPreparationService>();
                        services.AddScoped<AgendamentoFiscalPreparationService>();
                        services.AddScoped<IValidaParametrosMunicipioService, ValidaParametrosMunicipioService>();
                        services.AddScoped<NotaFiscalEventoHandler>();
                        services.AddScoped<NotaFiscalAvulsaService>();
                        services.AddTransient<NFSeEmissorService>();
                        services.AddTransient<NFCeEmissorService>();
                        // Critical Fix Verification: Register ILembreteHandler -> LembreteHandler
                        services.AddScoped<ILembreteHandler, LembreteHandler>();
                        services.AddScoped<LembreteService>();
                        services.AddScoped<FornecedoresHandler>();
                        services.AddScoped<ICrmHandler, MockCrmHandler>();
                        services.AddScoped<CrmService>();
                        services.AddScoped<PedidoHandler>();
                        services.AddScoped<PedidoService>();
                        services.AddScoped<VendaEstoqueHandler>();
                        services.AddScoped<VendaFiscalPreparationService>();
                        services.AddScoped<VendaService>();
                        services.AddHostedService<LembreteBackgroundService>();

                        // Manually register PageModels since we don't have MapRazorPages logic here
                        var assembly = typeof(LembreteService).Assembly;
                        var pageModels = assembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(PageModel)) && !t.IsAbstract)
                            .ToList();

                        foreach (var pageModel in pageModels)
                        {
                            services.AddTransient(pageModel);
                        }
                    });
                    webBuilder.Configure(app => { });
                });

            using var host = hostBuilder.Build();
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var assemblyToTest = typeof(LembreteService).Assembly;
            var pageModelTypes = assemblyToTest.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PageModel)) && !t.IsAbstract)
                .ToList();

            var failures = new List<string>();

            foreach (var type in pageModelTypes)
            {
                try
                {
                    serviceProvider.GetRequiredService(type);
                }
                catch (Exception ex)
                {
                    failures.Add($"Failed to resolve {type.Name}: {ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail($"Dependency Injection verification failed for the following PageModels:\n{string.Join("\n", failures)}");
            }
        }
    }

    internal sealed class MockCrmHandler : ICrmHandler
    {
        public void AdicionarInteracao(CrmInteracao interacao) { }
        public void AtualizarEtapaOportunidade(int idSalao, int idOportunidade, int idEtapa, string status, DateTime? dataFechamento) { }
        public void AtualizarResumoCampanha(int idSalao, int idCampanha, string status, int totalDestinatarios, int totalSucesso, int totalFalha, DateTime? ultimoEnvioEm) { }
        public void AtualizarStatusTarefa(int idSalao, int idTarefa, string status, DateTime? dataConclusao) { }
        public void GarantirEtapasPadrao(int idSalao) { }
        public CrmCampanha? ObterCampanha(int idSalao, int idCampanha) => new CrmCampanha { IdCampanha = idCampanha, IdSalao = idSalao, Nome = "Campanha Teste", Canal = CrmCanal.Email, Segmento = CrmSegmentoCampanha.TodosClientes, Conteudo = "Teste", Assunto = "Teste" };
        public CrmClienteResumo ObterClienteResumo(int idSalao, int idPessoa) => new CrmClienteResumo { IdPessoa = idPessoa, Nome = "Cliente Teste" };
        public CrmDashboardResumo ObterDashboard(int idSalao) => new CrmDashboardResumo();
        public CrmRelatorioResumo ObterRelatorios(int idSalao, DateTime dataInicio, DateTime dataFim) => new CrmRelatorioResumo();
        public CrmPessoaPerfil ObterOuCriarPerfil(int idSalao, int idPessoa) => new CrmPessoaPerfil { IdPerfil = 1, IdSalao = idSalao, IdPessoa = idPessoa };
        public List<CrmCampanhaDestino> ListarDestinosCampanha(int idSalao, int idCampanha, int limit = 100) => new();
        public List<CrmEtapaFunil> ListarEtapas(int idSalao) => new() { new CrmEtapaFunil { IdEtapa = 1, IdSalao = idSalao, Nome = "Novo Lead", Ordem = 1, Ativa = true } };
        public List<CrmInteracao> ListarInteracoes(int idSalao, int idPessoa, int limit = 50) => new();
        public List<CrmOportunidade> ListarOportunidades(int idSalao, int? idPessoa, string? status) => new();
        public List<CrmContatoCampanha> ListarPublicoCampanha(int idSalao, string segmento, string? filtroTag, int? diasInatividade, int? idPessoa) => new();
        public PagedResult<CrmCampanha> ListarCampanhas(int idSalao, int pageIndex, int pageSize) => new() { PageIndex = pageIndex, PageSize = pageSize };
        public PagedResult<CrmClienteResumo> ListarClientesResumo(int idSalao, string? pesquisa, int pageIndex, int pageSize) => new() { PageIndex = pageIndex, PageSize = pageSize };
        public PagedResult<CrmTarefa> ListarTarefas(int idSalao, int? idPessoa, string? status, int? idUsuarioResponsavel, int pageIndex, int pageSize, string? pesquisa = null, DateTime? dataVencimentoInicio = null, DateTime? dataVencimentoFim = null) => new() { PageIndex = pageIndex, PageSize = pageSize };
        public List<CrmTimelineItem> ListarTimeline(int idSalao, int idPessoa, int limit = 100) => new();
        public void RegistrarDestinoCampanha(CrmCampanhaDestino destino) { }
        public void SalvarPerfil(CrmPessoaPerfil perfil) { }
        public int SalvarCampanha(CrmCampanha campanha) => 1;
        public int SalvarInteracao(CrmInteracao interacao) => 1;
        public PagedResult<CrmOportunidade> ListarOportunidadesPaginadas(int idSalao, int? idPessoa, string? status, DateTime? dataInicio, DateTime? dataFim, int pageIndex, int pageSize) => new() { PageIndex = pageIndex, PageSize = pageSize };
        public int SalvarOportunidade(CrmOportunidade oportunidade) => 1;
        public int SalvarTarefa(CrmTarefa tarefa) => 1;
    }

    internal sealed class FakeWhatsappService : IWhatsappService
    {
        public Task<(bool Success, string? ErrorMessage)> EnviarMensagemAsync(string telefone, string mensagem)
        {
            return Task.FromResult<(bool Success, string? ErrorMessage)>((true, null));
        }
    }

    internal sealed class FakeFinanceiroModuloHandler : IFinanceiroModuloHandler
    {
        public Task AtualizarStatusTituloAsync(int idSalao, Guid idTitulo, string status, DateTime? dataLiquidacao, decimal? valorLiquidado, bool? conciliado) => Task.CompletedTask;
        public Task AtualizarValoresTituloAsync(int idSalao, Guid idTitulo, decimal valorOriginal, decimal valorLiquidado, decimal valorAberto, string status, DateTime? dataLiquidacao, bool conciliado, string? observacoes) => Task.CompletedTask;
        public Task<FinanceiroDashboardResumo> ObterDashboardAsync(int idSalao, DateTime dataInicio, DateTime dataFim) => Task.FromResult(new FinanceiroDashboardResumo());
        public Task<FinanceiroRelatorioResumo> ObterRelatoriosAsync(int idSalao, DateTime dataInicio, DateTime dataFim) => Task.FromResult(new FinanceiroRelatorioResumo());
        public Task<FinanceiroTitulo?> ObterTituloAsync(int idSalao, Guid idTitulo) => Task.FromResult<FinanceiroTitulo?>(null);
        public Task<List<ContaCaixa>> ListarContasCaixaAsync(int idSalao) => Task.FromResult(new List<ContaCaixa>());
        public Task<List<PlanoContas>> ListarPlanoContasAsync(int idSalao) => Task.FromResult(new List<PlanoContas>());
        public Task<List<FinanceiroTitulo>> ListarTitulosPorVendaAsync(int idSalao, int idVendaProduto) => Task.FromResult(new List<FinanceiroTitulo>());
        public Task<PagedResult<FinanceiroTitulo>> ListarTitulosAsync(int idSalao, FinanceiroTituloFiltro filtro) => Task.FromResult(new PagedResult<FinanceiroTitulo>());
        public Task<Guid> SalvarTituloAsync(FinanceiroTitulo titulo) => Task.FromResult(Guid.NewGuid());
        public Task SaveContaCaixaAsync(ContaCaixa conta) => Task.CompletedTask;
        public Task SavePlanoContasAsync(PlanoContas plano) => Task.CompletedTask;
        public Task SincronizarTitulosPagamentoAsync(int idSalao) => Task.CompletedTask;
    }
}

