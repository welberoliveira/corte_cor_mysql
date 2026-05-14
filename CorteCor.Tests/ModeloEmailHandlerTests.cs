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
    public class ModeloEmailHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly ModeloEmailHandler _handler;

        public ModeloEmailHandlerTests()
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
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(false);
            _mockParameter.SetupAllProperties();

            _handler = new ModeloEmailHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void Cadastrar_DeveExecutarNonQuery()
        {
            // Arrange
            var modelo = new ModeloEmail
            {
                IdSalao = 1,
                TipoEvento = "BoasVindas",
                Assunto = "Bem-vindo",
                CorpoHTML = "Olá {NomeCliente}",
                Ativo = true
            };

            // Act
            _handler.Cadastrar(modelo);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void Cadastrar_QuandoEventoJaExiste_DeveBloquearAntesDoInsert()
        {
            var modelo = new ModeloEmail
            {
                IdSalao = 1,
                TipoEvento = "LembreteAgendamento",
                Assunto = "Lembrete",
                CorpoHTML = "Corpo",
                Ativo = true
            };

            _mockReader.SetupSequence(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdModelo"]).Returns(7);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["TipoEvento"]).Returns("LembreteAgendamento");
            _mockReader.Setup(r => r["Assunto"]).Returns("Existente");
            _mockReader.Setup(r => r["CorpoHTML"]).Returns("Corpo existente");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataAtualizacao"]).Returns(DateTime.Now);

            var ex = Assert.Throws<InvalidOperationException>(() => _handler.Cadastrar(modelo));

            Assert.Contains("Já existe um modelo de e-mail", ex.Message);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Never);
        }

        [Fact]
        public void ObterPorId_DeveRetornarModelo()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdModelo"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["TipoEvento"]).Returns("ConfirmacaoAgendamento");
            _mockReader.Setup(r => r["Assunto"]).Returns("Agendamento Confirmado");
            _mockReader.Setup(r => r["CorpoHTML"]).Returns("Corpo");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataAtualizacao"]).Returns(DateTime.Now);

            // Act
            var result = _handler.ObterPorId(1, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.IdModelo);
            Assert.Equal("ConfirmacaoAgendamento", result.TipoEvento);
        }

        [Fact]
        public void ObterPorEvento_DeveRetornarModeloAtivo()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdModelo"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["TipoEvento"]).Returns("BoasVindas");
            _mockReader.Setup(r => r["Assunto"]).Returns("Assunto");
            _mockReader.Setup(r => r["CorpoHTML"]).Returns("Corpo");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataAtualizacao"]).Returns(DateTime.Now);

            // Act
            var result = _handler.ObterPorEvento(1, "BoasVindas");

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Ativo);
        }

        [Fact]
        public void ListarPorSalao_DeveRetornarLista()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdModelo"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["TipoEvento"]).Returns("BoasVindas");
            _mockReader.Setup(r => r["Assunto"]).Returns("Assunto");
            _mockReader.Setup(r => r["CorpoHTML"]).Returns("Corpo");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataAtualizacao"]).Returns(DateTime.Now);

            // Act
            var result = _handler.ListarPorSalao(1);

            // Assert
            Assert.Single(result);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(), Times.Once);
        }

        [Fact]
        public void Atualizar_DeveExecutarNonQuery()
        {
            // Arrange
            var modelo = new ModeloEmail { IdModelo = 1, IdSalao = 1, TipoEvento = "Lembrete", Assunto = "Sub", CorpoHTML = "Body", Ativo = true };

            // Act
            _handler.Atualizar(modelo);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void Excluir_DeveExecutarNonQuery()
        {
            // Act
            _handler.Excluir(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void AtivarDesativar_DeveExecutarNonQuery()
        {
            // Act
            _handler.AtivarDesativar(1, false);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
    }
}

