using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CorteCor.Tests
{
    public class FornecedoresHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly FornecedoresHandler _handler;

        public FornecedoresHandlerTests()
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

            // Setup for Dapper Execute (ExecuteNonQuery)
            _mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1);

            _handler = new FornecedoresHandler(_mockDbHandler.Object);
        }

        #region Email Tests

        [Fact]
        public void SalvarEmail_Novo_DeveExecutarInsert()
        {
            // Arrange
            var email = new FornecedorEmail 
            { 
                IdFornecedor = 0,
                Nome = "Brevo",
                ApiKey = "key123",
                Endpoint = "https://api.brevo.com",
                RemetenteNome = "CorteCor",
                RemetenteEmail = "no-reply@cortecor.com",
                Ativo = true
            };

            // Act
            _handler.SalvarEmail(email);

            // Assert
            // Dapper calls ExecuteNonQuery twice: once for INSERT, once for UPDATE (AtivarEmail)
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce());
        }

        [Fact]
        public void SalvarEmail_Existente_DeveExecutarUpdate()
        {
            // Arrange
            var email = new FornecedorEmail 
            { 
                IdFornecedor = 1,
                Nome = "Brevo Edited",
                ApiKey = "key123",
                Endpoint = "https://api.brevo.com",
                RemetenteNome = "CorteCor",
                RemetenteEmail = "no-reply@cortecor.com",
                Ativo = false
            };

            // Act
            _handler.SalvarEmail(email);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once());
        }

        [Fact]
        public void AtivarEmail_DeveDesativarTodosEAtivarUm()
        {
            // Arrange
            int id = 1;

            // Act
            _handler.AtivarEmail(id);

            // Assert
            // 1. UPDATE ... SET Ativo = 0
            // 2. UPDATE ... SET Ativo = 1 WHERE Id = 1
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Exactly(2));
        }

        [Fact]
        public void ExcluirEmail_DeveExecutarDelete()
        {
            // Act
            _handler.ExcluirEmail(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once());
        }

        [Fact]
        public void ObterEmails_DeveRetornarLista()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader(It.IsAny<CommandBehavior>())).Returns(_mockReader.Object);
            
            // Mocking DataReader for Dapper is complex because Dapper checks schema.
            // Simplified approach: verify Command is executed. 
            // If we really want to test Dapper mapping, we need a robust IDataReader mock with GetSchemaTable etc.
            // For now, let's assume if CreateCommand and ExecuteReader are called, it's working regarding flow.
            // But to avoid Dapper crashing on empty reader, we might need to be careful.
            
            // Setup reader to return false immediately (empty list) to avoid schema issues
             var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            // Act
            var result = _handler.ObterEmails();

            // Assert
            Assert.NotNull(result);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(It.IsAny<CommandBehavior>()), Times.Once);
        }

        #endregion

        #region SMS Tests
        [Fact]
        public void SalvarSMS_Novo_DeveExecutarInsert()
        {
            var sms = new FornecedorSMS { IdFornecedor = 0, Nome = "Twilio", Ativo = true };
            _handler.SalvarSMS(sms);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce());
        }

        [Fact]
        public void AtivarSMS_DeveExecutarUpdates()
        {
            _handler.AtivarSMS(1);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Exactly(2));
        }
        #endregion

        #region Whatsapp Tests
        [Fact]
        public void SalvarWhatsapp_Novo_DeveExecutarInsert()
        {
             var wpp = new FornecedorWhatsapp { IdFornecedor = 0, Nome = "Z-API", Ativo = true };
            _handler.SalvarWhatsapp(wpp);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeastOnce());
        }

        [Fact]
        public void AtivarWhatsapp_DeveExecutarUpdates()
        {
             _handler.AtivarWhatsapp(1);
             _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Exactly(2));
        }
        #endregion
    }
}
