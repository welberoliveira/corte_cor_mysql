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
    public class MeioPagamentoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly MeioPagamentoHandler _handler;

        public MeioPagamentoHandlerTests()
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

            _handler = new MeioPagamentoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void ListarPorSalao_DeveRetornarMeiosPagamentoComNovosCampos()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            SetupMockReaderRow(_mockReader);

            // Act
            var result = _handler.ListarPorSalao(1);

            // Assert
            Assert.Single(result);
            var item = result[0];
            Assert.Equal("Mercado Pago", item.Nome);
            Assert.Equal("TOKEN_PROD", item.MpAccessTokenProd);
            Assert.Equal("TOKEN_SANDBOX", item.MpAccessTokenSandbox);
            Assert.True(item.MpProduction);
        }

        [Fact]
        public void CadastrarMeioPagamento_DeveRetornarId()
        {
            // Arrange
            var meio = new MeioPagamento 
            { 
                Nome = "Novo Meio", 
                Gateway = "MercadoPago",
                MpAccessTokenProd = "P",
                MpAccessTokenSandbox = "S",
                MpProduction = true
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(99);

            // Act
            var result = _handler.CadastrarMeioPagamento(meio);

            // Assert
            Assert.Equal(99, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void ObterPorId_DeveRetornarMeioComNovosCampos()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            SetupMockReaderRow(_mockReader);

            // Act
            var result = _handler.ObterPorId(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TOKEN_PROD", result.MpAccessTokenProd);
            Assert.True(result.MpProduction);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var meio = new MeioPagamento 
            { 
                IdMeioPagamento = 1, 
                Nome = "Editado", 
                MpProduction = false 
            };

            // Act
            _handler.Atualizar(meio);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        private void SetupMockReaderRow(Mock<IDataReader> reader)
        {
            reader.Setup(r => r["IdMeioPagamento"]).Returns(1);
            reader.Setup(r => r["Nome"]).Returns("Mercado Pago");
            reader.Setup(r => r["Tipo"]).Returns("PIX");
            reader.Setup(r => r["Gateway"]).Returns("MercadoPago");
            reader.Setup(r => r["PermiteParcelamento"]).Returns(false);
            reader.Setup(r => r["ParcelasMax"]).Returns(DBNull.Value);
            reader.Setup(r => r["TaxaPercentual"]).Returns(0m);
            reader.Setup(r => r["TaxaFixa"]).Returns(0m);
            reader.Setup(r => r["PrazoRecebimentoDias"]).Returns((short)0);
            reader.Setup(r => r["Ativo"]).Returns(true);
            reader.Setup(r => r["IdSalao"]).Returns(1);
            reader.Setup(r => r["DataCadastro"]).Returns(DateTime.Now);
            reader.Setup(r => r["MpAccessTokenProd"]).Returns("TOKEN_PROD");
            reader.Setup(r => r["MpAccessTokenSandbox"]).Returns("TOKEN_SANDBOX");
            reader.Setup(r => r["MpPublicKeyProd"]).Returns("KEY_PROD");
            reader.Setup(r => r["MpPublicKeySandbox"]).Returns("KEY_SANDBOX");
            reader.Setup(r => r["MpProduction"]).Returns(true);
        }
    }
}

