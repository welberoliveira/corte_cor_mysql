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
    public class AdministradorHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly AdministradorHandler _handler;

        public AdministradorHandlerTests()
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

            _handler = new AdministradorHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarAdministrador_DeveRetornarId()
        {
            // Arrange
            var admin = new Administrador
            {
                Nome = "Admin",
                Email = "admin@teste.com",
                Senha = "123",
                Perfil = "Master",
                Status = "Ativo",
                DataCriacao = DateTime.Now
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(5);

            // Act
            var result = _handler.CadastrarAdministrador(admin);

            // Assert
            Assert.Equal(5, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void Listar_DeveRetornarAdmins()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdUsuario"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Admin 1");
            _mockReader.Setup(r => r["Email"]).Returns("admin@test.com");
            _mockReader.Setup(r => r["Senha"]).Returns("123");
            _mockReader.Setup(r => r["Perfil"]).Returns("Master");
            _mockReader.Setup(r => r["Status"]).Returns("Ativo");
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);

            // Act
            var result = _handler.Listar();

            // Assert
            Assert.Single(result);
            Assert.Equal("Admin 1", result[0].Nome);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var admin = new Administrador { IdUsuario = 1, Nome = "Novo Nome" };

            // Act
            _handler.Atualizar(admin);

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
            _handler.AtivarDesativar(1, false);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
    }
}

