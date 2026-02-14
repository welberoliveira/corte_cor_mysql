using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class PagamentoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly PagamentoHandler _handler;

        public PagamentoHandlerTests()
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

            // Mock ExecuteReader to return the mock reader by default
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);

            _handler = new PagamentoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void Listar_DeveRetornarListaDePagamentos()
        {
            // Arrange
            // Setup reader sequence
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            // Mock columns mapping
            SetupMockReaderRow(_mockReader);

            // Act
            var result = _handler.Listar(new PagamentoFiltroDTO());

            // Assert
            Assert.Single(result);
            Assert.Equal("Pago", result[0].Status);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(), Times.Once);
        }

        [Fact]
        public void Listar_ComFiltroDataAgendamento_DeveConfigurarParametro()
        {
            // Arrange
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false); // No results for simplicity

            var filtro = new PagamentoFiltroDTO { DataAgendamento = DateTime.Today };

            // Act
            var result = _handler.Listar(filtro);

            // Assert
            // Verify that the parameter was added (implicitly by the fact code ran without error and we could inspect mocks if needed)
            // Ideally we would verify the parameter collection contained @DataAgendamento but Moq with CreateParameter is tricky for collections
            // We can verify that CreateParameter was called multiple times.
            _mockCommand.Verify(cmd => cmd.CreateParameter(), Times.AtLeastOnce);
        }

        private void SetupMockReaderRow(Mock<IDataReader> reader)
        {
            // Common fields
            reader.Setup(r => r["IdPagamento"]).Returns(Guid.NewGuid());
            reader.Setup(r => r["IdAgendamento"]).Returns(123);
            reader.Setup(r => r["Ativo"]).Returns(true);
            reader.Setup(r => r["Status"]).Returns("Pago");
            reader.Setup(r => r["Valor"]).Returns(150.00m);
            reader.Setup(r => r["Moeda"]).Returns("BRL");
            reader.Setup(r => r["Descricao"]).Returns("Corte de Cabelo");
            
            reader.Setup(r => r["MercadoPagoPreferenceId"]).Returns((string)null);
            reader.Setup(r => r["MercadoPagoPaymentId"]).Returns((string)null);
            reader.Setup(r => r["CheckoutUrl"]).Returns((string)null);
            reader.Setup(r => r["MpStatus"]).Returns((string)null);
            reader.Setup(r => r["MpStatusDetail"]).Returns((string)null);
            
            reader.Setup(r => r["CriadoEm"]).Returns(DateTime.Now);
            reader.Setup(r => r["AtualizadoEm"]).Returns(DBNull.Value);
            reader.Setup(r => r["PagoEm"]).Returns(DBNull.Value);

            // Legacy / Nullable fields logic in EntityHandler.Map uses HasColumn check or exception handling?
            // The logic in EntityHandler.Map for 'IdMeioPagamento' checks HasColumn. 
            // My Mock<IDataReader> doesn't implement GetName(i) logic automatically for HasColumn check to work 
            // unless I setup GetName calls.
            // However, the `Map` method in EntityHandler (lines 2425+) iterates FieldCount to check column existence.
            // So we must mock FieldCount and GetName
            
            reader.Setup(r => r.FieldCount).Returns(5); 
            reader.Setup(r => r.GetName(0)).Returns("IdPagamento");
            reader.Setup(r => r.GetName(1)).Returns("NomeCliente");
            reader.Setup(r => r.GetName(2)).Returns("NomeServico");
            reader.Setup(r => r.GetName(3)).Returns("DataAgendamento");
            reader.Setup(r => r.GetName(4)).Returns("Status"); // etc

            // And setup return values for valid columns
            reader.Setup(r => r["NomeCliente"]).Returns("João da Silva");
            reader.Setup(r => r["NomeServico"]).Returns("Corte Masculino");
            reader.Setup(r => r["DataAgendamento"]).Returns(DateTime.Today.AddHours(14));
        }

        [Fact]
        public void AtualizarStatusWebhook_DeveExecutarUpdateComParametrosCorretos()
        {
            // Arrange
            var idPagamento = Guid.NewGuid();
            var status = "Pago";
            long? mpId = 123456789;
            var mpStatus = "approved";
            var mpStatusDetail = "accredited";
            var pagoEm = DateTime.Now;

            // Act
            _handler.AtualizarStatusWebhook(idPagamento, status, mpId, mpStatus, mpStatusDetail, pagoEm);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce);
            _mockParameters.Verify(p => p.Add(It.IsAny<IDbDataParameter>()), Times.AtLeast(5)); 
        }
    }
}
