using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class AgendamentoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly AgendamentoHandler _handler;

        public AgendamentoHandlerTests()
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

            _handler = new AgendamentoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarAgendamento_DeveRetornarId()
        {
            // Arrange
            var agendamento = new Agendamento
            {
                DataHora = DateTime.Now,
                IdServico = 1,
                IdPessoa = 1,
                IdFuncionario = 1,
                Status = "Agendado"
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(55);

            // Act
            var result = _handler.CadastrarAgendamento(agendamento);

            // Assert
            Assert.Equal(55, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void ObterPorId_DeveRetornarAgendamento()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdAgendamento"]).Returns(10);
            _mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Status"]).Returns("Confirmado");
            _mockReader.Setup(r => r["IdServico"]).Returns(1);
            _mockReader.Setup(r => r["IdPessoa"]).Returns(1);
            _mockReader.Setup(r => r["IdFuncionario"]).Returns(1);

            // Act
            var result = _handler.ObterPorId(10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.IdAgendamento);
        }

        [Fact]
        public void ListarPorIntervalo_DeveRetornarLista()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdAgendamento"]).Returns(1);
            _mockReader.Setup(r => r["DataHora"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Status"]).Returns("Agendado");
            _mockReader.Setup(r => r["IdServico"]).Returns(1);
            _mockReader.Setup(r => r["IdPessoa"]).Returns(1);
            _mockReader.Setup(r => r["IdFuncionario"]).Returns(1);

            // Act
            var result = _handler.ListarPorIntervalo(1, DateTime.Today, DateTime.Today.AddDays(1));

            // Assert
            Assert.Single(result);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(), Times.Once);
        }
    }
}
