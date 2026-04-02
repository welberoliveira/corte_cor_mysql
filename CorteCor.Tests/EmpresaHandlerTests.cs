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
    public class SalaoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly SalaoHandler _handler;

        public SalaoHandlerTests()
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

            _handler = new SalaoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarSalao_DeveRetornarId()
        {
            // Arrange
            var salao = new Salao
            {
                Nome = "Salao Teste",
                Responsavel = "Jose",
                Email = "email@teste.com",
                Telefone = "123456789",
                Endereco = "Rua Teste",
                CNPJ = "12345678000199",
                Status = "Ativo",
                DataCadastro = DateTime.Now,
                Observacao = "Obs"
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(10);

            // Act
            var result = _handler.CadastrarSalao(salao);

            // Assert
            Assert.Equal(10, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void Listar_DeveRetornarSaloes()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Salao 1");
            _mockReader.Setup(r => r["Responsavel"]).Returns("Resp 1");
            _mockReader.Setup(r => r["Email"]).Returns("email@test.com");
            _mockReader.Setup(r => r["Telefone"]).Returns("123");
            _mockReader.Setup(r => r["Endereco"]).Returns("End");
            _mockReader.Setup(r => r["CNPJ"]).Returns("123");
            _mockReader.Setup(r => r["Status"]).Returns("Ativo");
            _mockReader.Setup(r => r["DataCadastro"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Observacao"]).Returns("Obs");

            // Act
            var result = _handler.Listar();

            // Assert
            Assert.Single(result);
            Assert.Equal("Salao 1", result[0].Nome);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var salao = new Salao { IdSalao = 1, Nome = "Novo Nome" };

            // Act
            _handler.Atualizar(salao);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void Excluir_DeveExecutarDelete()
        {
            // Act
            _handler.Excluir(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void AtivarDesativar_DeveExecutarUpdate()
        {
            // Act
            _handler.AtivarDesativar(1, true);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
    }
}

