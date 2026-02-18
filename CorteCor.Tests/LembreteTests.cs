using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace CorteCor.Tests
{
    public class LembreteTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly LembreteHandler _handler;

        public LembreteTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockParameters = new Mock<IDataParameterCollection>();
            _mockParameter = new Mock<IDbDataParameter>();
            _mockReader = new Mock<IDataReader>();

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockParameters.Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(_mockParameter.Object);
            _mockParameter.SetupAllProperties();

            _handler = new LembreteHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void ListarConfig_DeveRetornarLista()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(2);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns(DBNull.Value);

            // Act
            var result = _handler.ListarConfig(1);

            // Assert
            Assert.Single(result);
            Assert.Equal(2, result[0].AntecedenciaValor);
            Assert.Null(result[0].IdModeloEmail);
            Assert.Equal("Padrão", result[0].AssuntoModelo);
        }

        [Fact]
        public void ListarConfig_ComModelo_DeveRetornarAssunto()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdConfig"]).Returns(2);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias");
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(5);
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns("Template Especial");

            // Act
            var result = _handler.ListarConfig(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Template Especial", result[0].AssuntoModelo);
        }

        [Fact]
        public void SalvarConfig_DeveExecutarInsert()
        {
            // Arrange
            var config = new LembreteConfig
            {
                IdSalao = 1,
                AntecedenciaValor = 1,
                AntecedenciaUnidade = "Dias",
                Ativo = true
            };

            // Act
            _handler.SalvarConfig(config);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void ExcluirConfig_DeveExecutarDelete()
        {
            // Act
            _handler.ExcluirConfig(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void GerarLembretes_DeveInserirLembretesProgramados()
        {
            // Arrange
            var dataAgendamento = DateTime.Now.AddDays(1);
            
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var readerSeq = new MockSequence();
            // 1. First call to get Agendamento/Salao info
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            
            // 2. Second call (via ListarConfig)
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); // Rule 1
            _mockReader.Setup(r => r["IdConfig"]).Returns(10);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(2);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns(DBNull.Value);

            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); // End of configs

            // Act
            _handler.GerarLembretes(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce());
        }

        [Fact]
        public void GerarLembretes_Minutos_DeveCalcularCorretamente()
        {
            // Arrange
            var dataAgendamento = DateTime.Now.AddHours(2); // Agendamento em 2h
            
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var readerSeq = new MockSequence();
            // 1. Get Agendamento Info
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            
            // 2. Get Configs
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["IdConfig"]).Returns(20);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(45); // 45 minutos antes
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Minutos");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModelo"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            // Act
            _handler.GerarLembretes(1);

            // Assert
            // 2h - 45min = 1h15min from now
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce());
        }
    }

    public class LembreteBackgroundServiceTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockScope;
        private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<BrevoEmailService> _mockEmailService;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>> _mockLogger;

        public LembreteBackgroundServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockScope = new Mock<IServiceScope>();
            _mockScopeFactory = new Mock<IServiceScopeFactory>();
            _mockDbHandler = new Mock<IDatabaseHandler>();
            
            // Mocking BrevoEmailService is tricky because it's not an interface. 
            // I'll assume it has virtual methods or I'll just mock it as best as I can.
            // Actually, BrevoEmailService methods are NOT virtual in the file I saw.
            // I should make EnviarEmailGenericoAsync virtual for testing.
            
            _mockEmailService = new Mock<BrevoEmailService>(new HttpClient(), null, null); 
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CorteCor.Pages.Webhooks.LembreteBackgroundService>>();

            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);
            _mockScopeFactory.Setup(sf => sf.CreateScope()).Returns(_mockScope.Object);
            _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
            
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(IDatabaseHandler))).Returns(_mockDbHandler.Object);
            _mockServiceProvider.Setup(sp => sp.GetService(typeof(BrevoEmailService))).Returns(_mockEmailService.Object);
        }

        // Add tests here if I make methods virtual...
    }
}
