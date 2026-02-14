using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class ComplexFlowTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        
        private readonly PagamentoHandler _pagamentoHandler;
        private readonly AgendamentoHandler _agendamentoHandler;

        public ComplexFlowTests()
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

            _pagamentoHandler = new PagamentoHandler(_mockDbHandler.Object);
            _agendamentoHandler = new AgendamentoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void WebhookFlow_Complete_ShouldUpdatePayment()
        {
            // Scenario:
            // 1. Webhook receives "approved" status from Mercado Pago.
            // 2. PagamentoHandler updates payment status.
            
            // Arrange
            var idPagamento = Guid.NewGuid();
            var status = "Pago";
            long? mpId = 998877;
            var mpStatus = "approved";
            var mpStatusDetail = "accredited";
            var pagoEm = DateTime.Now;

            // This mimics calling AtualizarStatusWebhook which calls internal logic.
            // Since AtualizarStatusWebhook logic is:
            /*
             public void AtualizarStatusWebhook(...) {
                 ... calls UPDATE command ...
             }
            */
            
            // Act
            _pagamentoHandler.AtualizarStatusWebhook(idPagamento, status, mpId, mpStatus, mpStatusDetail, pagoEm);

            // Assert
            // Verify that UPDATE commands were executed (Payment status + Appointment status)
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Exactly(2));
            
            // Verify parameters were set
            _mockParameters.Verify(p => p.Add(It.IsAny<IDbDataParameter>()), Times.AtLeast(5));
        }

        // Note: Real integration test would verify that Agendamento also updates, 
        // but since Handlers are decoupled and don't call each other directly in this architecture (Service layer usually does that), 
        // we test them individually here.
        // If there was a Service method coordinating this, we would test that Service method here.
    }
}
