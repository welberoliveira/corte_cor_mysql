using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace CorteCor.Tests
{
    public class LembreteAdvancedTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataReader> _mockReader;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly LembreteHandler _handler;

        public LembreteAdvancedTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockReader = new Mock<IDataReader>();
            _mockParameters = new Mock<IDataParameterCollection>();

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockParameters.Object);

            _handler = new LembreteHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void GerarLembretes_DeveExcluirPendentesAntesDeGerar()
        {
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var readerSeq = new MockSequence();
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now.AddDays(10));
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns("Assunto Teste");
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            _handler.GerarLembretes(100);

            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado")), Times.AtLeastOnce());
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.AtLeastOnce());
        }

        [Fact]
        public void GerarLembretes_NaoDeveInserir_SeDataEnvioJaPassou()
        {
            var dataAgendamento = DateTime.Now; 
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var readerSeq = new MockSequence();
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-5));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns("Assunto Teste");
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            _handler.GerarLembretes(100);

            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado")), Times.AtLeastOnce());
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Never());
        }

        [Fact]
        public void GerarLembretes_NaoDeveInserir_SeDataAgendamentoForaDoRangeDaRegra()
        {
            var dataAgendamento = DateTime.Now.AddDays(5); 
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var readerSeq = new MockSequence();
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(7)); 
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns("Assunto Teste");
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            _handler.GerarLembretes(100);

            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Never());
        }

        [Fact]
        public void ExcluirConfig_DeveExcluirLembretesPendentesCascateado()
        {
            _handler.ExcluirConfig(1);
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado") && s.Contains("IdConfig = @IdConfig")), Times.AtLeastOnce());
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteConfig") && s.Contains("IdConfig = @IdConfig")), Times.AtLeastOnce());
        }

        [Fact]
        public void ListarLogsEnvio_DeveRetornarListaDeLogs()
        {
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.SetupSequence(r => r.Read()).Returns(true).Returns(false);
            _mockReader.Setup(r => r["IdLog"]).Returns(1);
            _mockReader.Setup(r => r["IdLembrete"]).Returns(10);
            _mockReader.Setup(r => r["IdAgendamento"]).Returns(100);
            _mockReader.Setup(r => r["DataEnvio"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Destinatario"]).Returns("test@test.com");
            _mockReader.Setup(r => r["Assunto"]).Returns("Test Subject");
            _mockReader.Setup(r => r["Status"]).Returns("Sucesso");
            _mockReader.Setup(r => r["MensagemErro"]).Returns(DBNull.Value);

            var result = _handler.ListarLogsEnvio(null, null, null, null, null);

            Assert.Single(result.Items);
            Assert.Equal("test@test.com", result.Items[0].Destinatario);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("SELECT") && s.Contains("CorteCor_LogEnvioEmail")));
        }
    }

    public class LembreteBackgroundServiceAdvancedTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<ILembreteHandler> _mockLembreteHandler;
        private readonly Mock<BrevoEmailService> _mockEmailService;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>> _mockLogger;

        public LembreteBackgroundServiceAdvancedTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScope = new Mock<IServiceScope>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockLembreteHandler = new Mock<ILembreteHandler>();
            
            _mockEmailService = new Mock<BrevoEmailService>(new System.Net.Http.HttpClient(), null, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()); 
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>>();

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
            _mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
            
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILembreteHandler))).Returns(_mockLembreteHandler.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(BrevoEmailService))).Returns(_mockEmailService.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(LembreteService)))
                                .Returns(new LembreteService(new Mock<IDatabaseHandler>().Object, _mockEmailService.Object, new Mock<SMSMarketService>(new System.Net.Http.HttpClient(), null).Object, new Mock<Microsoft.Extensions.Logging.ILogger<LembreteService>>().Object, _mockLembreteHandler.Object));
        }

        [Fact]
        public async Task ProcessarLembretesAsync_DeveEnviarEmail_ERegistrarLog()
        {
            var service = new CorteCor.Pages.Webhooks.LembreteBackgroundService(_mockServiceProvider.Object, _mockLogger.Object);
            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes()).Returns(new List<CorteCor.Models.LembreteAgendado> { 
                new CorteCor.Models.LembreteAgendado { IdLembrete = 1, IdAgendamento = 100 } 
            });
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(1)).Returns(new CorteCor.Models.LembreteEnvioDTO {
                NomeCliente = "Fulano", EmailCliente = "fulano@teste.com", DataHoraAgendamento = DateTime.Now.AddDays(1),
                NomeServico = "Corte", NomeProfissional = "Barbeiro", NomeSalao = "Tonni", IdSalao = 1,
                AssuntoModelo = "Lembrete", CorpoModelo = "Olá {NomeCliente}"
            });
            int e = 0; int l = 100;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(1, out e, out l)).Returns(false);
            _mockEmailService.Setup(e => e.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((true, null));

            var method = typeof(CorteCor.Pages.Webhooks.LembreteBackgroundService).GetMethod("ProcessarLembretesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(service, new object[] { CancellationToken.None });

            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("fulano@teste.com", "Fulano", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockLembreteHandler.Verify(h => h.RegistrarLogEnvio(1, 100, "fulano@teste.com", "Lembrete", "Sucesso", null, "Email", null), Times.Once);
            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(1, "Enviado"), Times.Once);
        }

        [Fact]
        public void AplicarRegraRetroativa_DeveInserirLembretesParaAgendamentosFuturos()
        {
            var mockDbHandler = new Mock<IDatabaseHandler>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();
            var mockReader = new Mock<IDataReader>();

            mockDbHandler.Setup(db => db.GetConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
            mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(mockReader.Object);
            mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            mockCommand.Setup(cmd => cmd.Parameters).Returns(new Mock<IDataParameterCollection>().Object);

            var readerSeq = new MockSequence();
            mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            mockReader.Setup(r => r["IdConfig"]).Returns(1);
            mockReader.Setup(r => r["IdSalao"]).Returns(1);
            mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            mockReader.Setup(r => r["Ativo"]).Returns(true);
            mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);
            mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);

            mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            mockReader.Setup(r => r["IdAgendamento"]).Returns(500);
            mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now.AddDays(1));
            mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            var handler = new LembreteHandler(mockDbHandler.Object);
            handler.AplicarRegraRetroativa(1);

            mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Once());
        }
    }
}
