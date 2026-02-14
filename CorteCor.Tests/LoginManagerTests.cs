using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class LoginManagerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataReader> _mockReader;
        private readonly LoginManager _manager;

        public LoginManagerTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();
            _mockReader = new Mock<IDataReader>();

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            
            // Allow parameter creation
            var mockParameters = new Mock<IDataParameterCollection>();
            var mockParameter = new Mock<IDbDataParameter>();
            mockParameter.SetupAllProperties();
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(mockParameters.Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(mockParameter.Object);

            _manager = new LoginManager(_mockDbHandler.Object);
        }

        [Fact]
        public void AutenticarAdministrador_Sucesso_DeveRetornarTrue()
        {
            // Arrange
            string email = "admin@test.com";
            string senha = "123";
            string senhaHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(senha)); // Matching logic in manager

            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["Senha"]).Returns(senhaHash);

            // Act
            var result = _manager.AutenticarAdministrador(email, senha);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AutenticarAdministrador_SenhaIncorreta_DeveRetornarFalse()
        {
            // Arrange
            string email = "admin@test.com";
            string senha = "123";
            string senhaHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("wrong"));

            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["Senha"]).Returns(senhaHash);

            // Act
            var result = _manager.AutenticarAdministrador(email, senha);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RegistrarUsuario_DeveExecutarInsert()
        {
            // Arrange
            string nome = "Novo User";
            string email = "new@test.com";
            string senha = "123";
            string perfil = "User";

            // Act
            _manager.RegistrarUsuario(nome, email, senha, perfil);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void AlterarSenha_DeveExecutarUpdate()
        {
            // Act
            _manager.AlterarSenha("email@test.com", "novaSenha");

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void DesativarUsuario_DeveExecutarUpdate()
        {
            // Act
            _manager.DesativarUsuario(1);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
    }
}
