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
    public class FuncionarioServicoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly FuncionarioServicoHandler _handler;

        public FuncionarioServicoHandlerTests()
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

            _handler = new FuncionarioServicoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void Vincular_DeveExecutarInsert()
        {
            // Act
            _handler.Vincular(1, 2);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void Desvincular_DeveExecutarDelete()
        {
            // Act
            _handler.Desvincular(1, 2);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void ListarFuncionariosDoServico_DeveRetornarIds()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);
            
            _mockReader.Setup(r => r["IdFuncionario"]).Returns(10);

            // Act
            var result = _handler.ListarFuncionariosDoServico(5);

            // Assert
            Assert.Single(result);
            Assert.Equal(10, result[0]);
        }
    }
}

