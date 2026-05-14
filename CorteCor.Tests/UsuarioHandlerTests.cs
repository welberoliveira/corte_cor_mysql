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
    public class UsuarioHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly UsuarioHandler _handler;

        public UsuarioHandlerTests()
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
            
            // Setup properties for the parameter mock so they can be set
            _mockParameter.SetupAllProperties();

            _handler = new UsuarioHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarUsuario_Sucesso_DeveRetornarNovoId()
        {
            // Arrange
            var usuario = new Usuario 
            { 
                Nome = "Teste User", 
                Email = "teste@example.com", 
                Senha = "senha123",
                IdSalao = 1
            };
            
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(123);

            // Act
            int result = _handler.CadastrarUsuario(usuario);

            // Assert
            Assert.Equal(123, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
            _mockParameters.Verify(p => p.Add(It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Listar_DeveRetornarListaDeUsuarios()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            // Simulating 2 rows then end of read
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            // Setup reader data
            _mockReader.Setup(r => r["IdUsuario"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("User 1");
            _mockReader.Setup(r => r["Sobrenome"]).Returns("Test");
            _mockReader.Setup(r => r["CPF"]).Returns("123");
            _mockReader.Setup(r => r["Email"]).Returns("user1@example.com");
            _mockReader.Setup(r => r["Telefone"]).Returns("123");
            _mockReader.Setup(r => r["DataEntrada"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["Status"]).Returns("Ativo");
            _mockReader.Setup(r => r["Senha"]).Returns("123");
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);

            // Act
            var result = _handler.Listar();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(), Times.Once);
        }

        [Fact]
        public void AtivarDesativar_DeveExecutarComandoUpdate()
        {
            // Arrange
            int idUsuario = 5;
            bool ativar = false;

            // Act
            _handler.AtivarDesativar(idUsuario, ativar);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
            // Verify correct parameter setup implies correct SQL logic flow entered
            _mockParameters.Verify(p => p.Add(It.IsAny<object>()), Times.Exactly(2)); // @Ativo and @IdUsuario
        }
        
        [Fact]
        public void Excluir_DeveExecutarComandoDelete()
        {
             // Arrange
            int idUsuario = 10;

            // Act
            _handler.Excluir(idUsuario);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void Atualizar_DeveExecutarComandoUpdate()
        {
            // Arrange
            var usuario = new Usuario 
            { 
                IdUsuario = 1,
                Nome = "Teste Alterado", 
                Email = "teste@example.com", 
                IdSalao = 1
            };

            // Act
            _handler.Atualizar(usuario);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }
        [Fact]
        public void CadastrarUsuario_ComCamposNulos_DeveTratarCorretamente()
        {
            // Arrange
            var usuario = new Usuario 
            { 
                Nome = "User Null",
                DataEntrada = DateTime.MinValue // Should be DBNull
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(99);

            // Act
            _handler.CadastrarUsuario(usuario);

            // Assert
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
             // Verify that DBNull.Value was used for @DataEntrada parameter if possible, 
             // or just ensure no exception was thrown.
        }
    }
}

