using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class PessoaHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly PessoaHandler _handler;

        public PessoaHandlerTests()
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

            _handler = new PessoaHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarPessoa_DeveRetornarId()
        {
            // Arrange
            var pessoa = new Pessoa { Nome = "Maria", Telefone = "123", IdSalao = 1 };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(55);

            // Act
            var result = _handler.CadastrarPessoa(pessoa);

            // Assert
            Assert.Equal(55, result);
        }

        [Fact]
        public void ObterPorId_DeveRetornarPessoa()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdPessoa"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Maria");
            _mockReader.Setup(r => r["Telefone"]).Returns("123");
            _mockReader.Setup(r => r["Email"]).Returns("maria@test.com");
            _mockReader.Setup(r => r["DataNascimento"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // Act
            var result = _handler.ObterPorId(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Maria", result.Nome);
        }
    }
}
