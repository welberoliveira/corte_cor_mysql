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
    public class FuncionarioHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly FuncionarioHandler _handler;

        public FuncionarioHandlerTests()
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

            _handler = new FuncionarioHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarFuncionario_DeveRetornarId()
        {
            // Arrange
            var func = new Funcionario
            {
                Nome = "Funcionario Teste",
                seg = true, seg_ini = TimeSpan.Parse("09:00"), seg_fim = TimeSpan.Parse("18:00"),
                IdSalao = 1
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(100);

            // Act
            var result = _handler.CadastrarFuncionario(func);

            // Assert
            Assert.Equal(100, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void ListarPorSalao_DeveRetornarFuncionarios()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdFuncionario"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Func 1");
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            
            // Setup all boolean/timespan fields to default to avoid null issues
            SetupFuncionarioReaderDefaults(_mockReader);

            // Act
            var result = _handler.ListarPorSalao(1);

            // Assert
            Assert.Single(result);
            Assert.Equal("Func 1", result[0].Nome);
        }

        [Fact]
        public void ObterPorId_DeveRetornarFuncionarioComDiasConfigurados()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);

            _mockReader.Setup(r => r["IdFuncionario"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Func 1");
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            SetupFuncionarioReaderDefaults(_mockReader);
            // Specific overrides
            _mockReader.Setup(r => r["seg"]).Returns(true);
            _mockReader.Setup(r => r["seg_ini"]).Returns(TimeSpan.FromHours(9));
            _mockReader.Setup(r => r["seg_fim"]).Returns(TimeSpan.FromHours(18));

            // Act
            var result = _handler.ObterPorId(1);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.seg);
            Assert.Equal(TimeSpan.FromHours(9), result.seg_ini);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var func = new Funcionario { IdFuncionario = 1, Nome = "Novo Nome" };

            // Act
            _handler.Atualizar(func);

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

        private void SetupFuncionarioReaderDefaults(Mock<IDataReader> reader)
        {
            var days = new[] { "seg", "ter", "qua", "qui", "sex", "sab", "dom" };
            foreach (var d in days)
            {
                reader.Setup(r => r[d]).Returns(false);
                reader.Setup(r => r[$"{d}_ini"]).Returns(DBNull.Value);
                reader.Setup(r => r[$"{d}_fim"]).Returns(DBNull.Value);
            }
        }
    }
}

