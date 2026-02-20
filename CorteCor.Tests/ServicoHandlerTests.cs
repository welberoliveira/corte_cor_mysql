using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Xunit;
using Moq;
using CorteCor;

using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class ServicoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly ServicoHandler _handler;

        public ServicoHandlerTests()
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

            _handler = new ServicoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void ListarPorSalao_DeveRetornarServicos()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdServico"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Corte");
            _mockReader.Setup(r => r["Preco"]).Returns(50.0m);
            _mockReader.Setup(r => r["Duracao"]).Returns(TimeSpan.FromMinutes(30));

            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // Act
            var result = _handler.ListarPorSalao(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Corte", result[0].Nome);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var servico = new Servico { IdServico = 1, Nome = "Novo Nome", Preco = 60m, Duracao = TimeSpan.FromMinutes(45), IdSalao = 1 };

            // Act
            _handler.Atualizar(servico);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
        [Fact]
        public void CadastrarServico_DeveRetornarId()
        {
            // Arrange
            var servico = new Servico { Nome = "Servico Novo", Preco = 10m, Duracao = TimeSpan.FromHours(1), IdSalao = 1 };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(88);

            // Act
            var result = _handler.CadastrarServico(servico);

            // Assert
            Assert.Equal(88, result);
        }

        [Fact]
        public void ObterPorId_DeveRetornarServico()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdServico"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Servico 1");
            _mockReader.Setup(r => r["Preco"]).Returns(50m);
            _mockReader.Setup(r => r["Duracao"]).Returns(TimeSpan.FromMinutes(30));
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // Act
            var result = _handler.ObterPorId(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Servico 1", result.Nome);
        }

        [Fact]
        public void Excluir_DeveExecutarDelete()
        {
            // Act
            _handler.Excluir(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
    }
}

