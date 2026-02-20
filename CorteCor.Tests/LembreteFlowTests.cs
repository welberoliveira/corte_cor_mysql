using System;
using System.Collections.Generic;
using System.Data;
using Moq;
using Xunit;
using CorteCor;
using static CorteCor.Models;

namespace CorteCor.Tests
{
    public class LembreteFlowTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataReader> _mockReader;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly AgendamentoHandler _agendamentoHandler;

        public LembreteFlowTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockReader = new Mock<IDataReader>();
            _mockParameters = new Mock<IDataParameterCollection>();

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);

            _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockParameters.Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);

            _agendamentoHandler = new AgendamentoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarAgendamento_DeveGerarLembrete_QuandoHorarioPermite()
        {
            // Arrange
            // 1. Agendamento para DAQUI A 1 DIA (Futuro distante)
            var dataAgendamento = DateTime.Now.AddDays(1);
            var agendamento = new Agendamento 
            { 
                DataHora = dataAgendamento, 
                IdServico = 10, 
                IdPessoa = 5, 
                IdFuncionario = 2, 
                Status = "Agendado" 
            };

            // Setup sequence for DataReaders
            var readerSeq = new MockSequence();
            
            // 1. CadastrarAgendamento (INSERT + SELECT SCOPE_IDENTITY)
            _mockCommand.Setup(c => c.ExecuteScalar()).Returns(100); // IdAgendamento = 100

            // 2. LembreteHandler.GerarLembretes starts
            // 2a. ExcluirLembretesPendentes (DELETE) - No reader needed, ExecuteNonQuery

            // 2b. Query Agendamento Info (SELECT DataHora, IdSalao)
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1); // Salao 1

            // 2c. ListarConfig (SELECT * FROM LembreteConfig WHERE IdSalao = 1)
            // Config: 1 Hour Before
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["IdModeloSMS"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["TipoLembrete"]).Returns("Email");
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConteudoModeloSMS"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-30));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            // Act
            _agendamentoHandler.CadastrarAgendamento(agendamento);

            // Assert
            // Verifica se houve INSERT na tabela de lembretes
            // Data do lembrete seria (Agora + 1 dia) - 1 hora = (Agora + 23 horas) > Agora -> DEVE INSERIR
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.AtLeastOnce(), "Deveria ter inserido um lembrete.");
        }

        [Fact]
        public void CadastrarAgendamento_NAODeveGerarLembrete_QuandoJaPassouDoTempo()
        {
            // Arrange
            // 1. Agendamento para DAQUI A 30 MINUTOS (Futuro proximo)
            var dataAgendamento = DateTime.Now.AddMinutes(30);
            var agendamento = new Agendamento 
            { 
                DataHora = dataAgendamento, 
                IdServico = 10, 
                IdPessoa = 5, 
                IdFuncionario = 2, 
                Status = "Agendado" 
            };

            // Setup sequence
            var readerSeq = new MockSequence();

            // 1. CadastrarAgendamento
            _mockCommand.Setup(c => c.ExecuteScalar()).Returns(100);

            // 2b. Query Info
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // 2c. ListarConfig
            // Config: 1 Hour Before
            // Lembrete seria para: (Agora + 30m) - 60m = (Agora - 30m) -> PASSADO
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1); // 1
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas"); // Hora
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["IdModeloSMS"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["TipoLembrete"]).Returns("Email");
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConteudoModeloSMS"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-30));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            // Act
            _agendamentoHandler.CadastrarAgendamento(agendamento);

            // Assert
            // Verifica se o INSERT de lembrete FOI CHAMADO
            // Pela NOVA lógica, DEVE ser chamado (Immediate send)
            _mockCommand.VerifySet(c => c.CommandText = It.Is<string>(s => s.Contains("INSERT INTO CorteCor_LembreteAgendado")), Times.AtLeastOnce(), "Deveria inserir lembrete imediato pois evento ainda é futuro.");
        }
    }
}
