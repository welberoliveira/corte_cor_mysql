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
            // Ensure CreateCommand returns a NEW mock each time or handles properties correctly
            // For simplicity, we stick to one mock but reset it if needed or just track calls.
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockParameters.Object);

            _handler = new LembreteHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void GerarLembretes_DeveExcluirPendentesAntesDeGerar()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            // Setup for ExcluirLembretesPendentes
            // It runs a DELETE command first.
            
            // Setup for GerarLembretes internals
            var readerSeq = new MockSequence();
            
            // 1. Get Agendamento
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now.AddDays(10));
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // 2. ListarConfig
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns("Assunto Teste");
            
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            // Act
            _handler.GerarLembretes(100);

            // Assert
            // Verify DELETE was called
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado")), Times.AtLeastOnce());
            
            // Verify INSERT was called
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.AtLeastOnce());
        }

        [Fact]
        public void GerarLembretes_NaoDeveInserir_SeDataEnvioJaPassou()
        {
            // Arrange
            // Agendamento is TODAY
            var dataAgendamento = DateTime.Now; 
            
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var readerSeq = new MockSequence();
            
            // 1. Get Agendamento
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // 2. ListarConfig (Rule: 1 Day Before)
            // Send time would be Yesterday -> OUTDATED
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias"); // 1 day before
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns("Assunto Teste");
            
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            // Act
            _handler.GerarLembretes(100);

            // Assert
            // Verify DELETE was called (always cleans up)
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado")), Times.AtLeastOnce());

            // Verify INSERT was NOT called for the outdated reminder
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Never());
        }

        [Fact]
        public void GerarLembretes_NaoDeveInserir_SeDataAgendamentoForaDoRangeDaRegra()
        {
            // Arrange
            var dataAgendamento = DateTime.Now.AddDays(5); 
            
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var readerSeq = new MockSequence();
            
            // 1. Get Agendamento
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // 2. ListarConfig (Rule: Start next week)
            // Agendamento is in 5 days, but rule starts in 7 days -> Rule NOT active yet
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
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns("Assunto Teste");
            
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            // Act
            _handler.GerarLembretes(100);

            // Assert
            // Verify INSERT was NEVER called because rule is out of range
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Never());
        }

        [Fact]
        public void ExcluirConfig_DeveExcluirLembretesPendentesCascateado()
        {
            // Act
            _handler.ExcluirConfig(1);

            // Assert
            // 1. Verify delete reminders
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteAgendado") && s.Contains("IdConfig = @IdConfig")), Times.AtLeastOnce());
            
            // 2. Verify delete config
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("DELETE FROM CorteCor_LembreteConfig") && s.Contains("IdConfig = @IdConfig")), Times.AtLeastOnce());
        }

        [Fact]
        public void ListarLogsEnvio_DeveRetornarListaDeLogs()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            _mockReader.SetupSequence(r => r.Read())
                .Returns(true)
                .Returns(false);

            _mockReader.Setup(r => r["IdLog"]).Returns(1);
            _mockReader.Setup(r => r["IdLembrete"]).Returns(10);
            _mockReader.Setup(r => r["IdAgendamento"]).Returns(100);
            _mockReader.Setup(r => r["DataEnvio"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Destinatario"]).Returns("test@test.com");
            _mockReader.Setup(r => r["Assunto"]).Returns("Test Subject");
            _mockReader.Setup(r => r["Status"]).Returns("Sucesso");
            _mockReader.Setup(r => r["MensagemErro"]).Returns(DBNull.Value);

            // Act
            var result = _handler.ListarLogsEnvio(null, null);

            // Assert
            Assert.Single(result);
            Assert.Equal("test@test.com", result[0].Destinatario);
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("SELECT") && s.Contains("CorteCor_LogEnvioEmail")));
        }
    }

    public class LembreteBackgroundServiceAdvancedTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDataReader> _mockReader;
        private readonly Mock<BrevoEmailService> _mockEmailService;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>> _mockLogger;

        public LembreteBackgroundServiceAdvancedTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScope = new Mock<IServiceScope>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockParameters = new Mock<IDataParameterCollection>();
            _mockReader = new Mock<IDataReader>();
            
            _mockEmailService = new Mock<BrevoEmailService>(new System.Net.Http.HttpClient(), null, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()); 
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>>();

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
            _mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
            
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IDatabaseHandler))).Returns(_mockDbHandler.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(BrevoEmailService))).Returns(_mockEmailService.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(LembreteService)))
                                .Returns(new LembreteService(_mockDbHandler.Object, _mockEmailService.Object, new Mock<Microsoft.Extensions.Logging.ILogger<LembreteService>>().Object));

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockParameters.Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        }

        [Fact]
        public async Task ProcessarLembretesAsync_DeveEnviarEmail_ERegistrarLog()
        {
            // Arrange
            var service = new CorteCor.Pages.Webhooks.LembreteBackgroundService(_mockServiceProvider.Object, _mockLogger.Object);
            
            var readerSeq = new MockSequence();
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);

            // 1. ObterLembretesPendentes
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdLembrete"]).Returns(1);
            _mockReader.Setup(r => r["IdAgendamento"]).Returns(100);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            // 2. ObterDadosEnvio
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["NomeCliente"]).Returns("Fulano");
            _mockReader.Setup(r => r["EmailCliente"]).Returns("fulano@teste.com");
            _mockReader.Setup(r => r["DataHoraAgendamento"]).Returns(DateTime.Now.AddDays(1));
            _mockReader.Setup(r => r["NomeServico"]).Returns("Corte");
            _mockReader.Setup(r => r["NomeProfissional"]).Returns("Barbeiro");
            _mockReader.Setup(r => r["NomeSalao"]).Returns("Tonni");
            _mockReader.Setup(r => r["Assunto"]).Returns("Lembrete");
            _mockReader.Setup(r => r["CorpoHTML"]).Returns("Olá {NomeCliente}");

            // 3. Mock Email Send
            _mockEmailService.Setup(e => e.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync(true);

            // Act
            // BackgroundService.ProcessarLembretesAsync is private, but we can call it if we use a helper or make it internal.
            // Since I cannot change the code to internal easily without more edits, I'll use reflection for the test.
            var method = typeof(CorteCor.Pages.Webhooks.LembreteBackgroundService).GetMethod("ProcessarLembretesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method.Invoke(service, new object[] { CancellationToken.None });

            // Assert
            // Verify Email was sent
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("fulano@teste.com", "Fulano", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            // Verify Log was inserted
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LogEnvioEmail")), Times.Once);
            
            // Verify Status was updated
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("UPDATE CorteCor_LembreteAgendado SET Status = @Status")), Times.Once);
        }

        [Fact]
        public void AplicarRegraRetroativa_DeveInserirLembretesParaAgendamentosFuturos()
        {
            // Arrange
            var readerSeq = new MockSequence();
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);

            // 1. Get Config
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);

            // 2. Get Agendamentos matching the rule
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdAgendamento"]).Returns(500);
            _mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now.AddDays(1));
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            // Act
            _handler.AplicarRegraRetroativa(1);

            // Assert
            // Verify INSERT was called for the existing appointment
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.Once());
        }
    }
}
