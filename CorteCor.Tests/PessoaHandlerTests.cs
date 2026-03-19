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
            SetupPessoaReaderDefaults(_mockReader);

            // Act
            var result = _handler.ObterPorId(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Maria", result.Nome);
        }

        [Fact]
        public void ListarPaginadoPorSalao_DevePesquisarEmCamposPrincipais()
        {
            _mockCommand.SetupSequence(cmd => cmd.ExecuteScalar()).Returns(2);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(false);

            var result = _handler.ListarPaginadoPorSalao(1, "maria", 2, 10);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.PageIndex);
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(sql =>
                sql.Contains("Nome LIKE @Pesquisa") &&
                sql.Contains("Email LIKE @Pesquisa") &&
                sql.Contains("Telefone LIKE @Pesquisa") &&
                sql.Contains("CpfCnpj LIKE @Pesquisa")), Times.AtLeastOnce);
        }

        [Fact]
        public void ExisteCpfCnpjPorSalao_ComValorVazio_DeveRetornarFalseSemAcessarBanco()
        {
            var result = _handler.ExisteCpfCnpjPorSalao(string.Empty, 1);

            Assert.False(result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Never);
        }

        [Fact]
        public void ExisteEmailPorSalao_ComValorVazio_DeveRetornarFalseSemAcessarBanco()
        {
            var result = _handler.ExisteEmailPorSalao(" ", 1);

            Assert.False(result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Never);
        }


    [Fact]
    public void Excluir_DeveRealizarSoftDelete()
    {
        // Arrange
        int idPessoa = 1;
        var mockDbHandler = new Mock<IDatabaseHandler>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();
        var mockParameter = new Mock<IDbDataParameter>();
        var parameters = new Mock<IDataParameterCollection>();

        mockDbHandler.Setup(db => db.GetConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(cmd => cmd.Parameters).Returns(parameters.Object);
        mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(mockParameter.Object);

        var handler = new PessoaHandler(mockDbHandler.Object);

        // Act
        handler.Excluir(idPessoa);

        // Assert
        mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("UPDATE CorteCor_Pessoa SET Excluido = 1")), Times.Once);
        mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
    }

    [Fact]
        public void Listar_DeveFiltrarExcluidos()
        {
        // Arrange
        var mockDbHandler = new Mock<IDatabaseHandler>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();
        var mockReader = new Mock<IDataReader>();

        mockDbHandler.Setup(db => db.GetConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(mockReader.Object);
        mockCommand.Setup(cmd => cmd.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
        mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);
        
        // Simular 2 registros, assumindo que a query já filtra
        mockReader.SetupSequence(r => r.Read())
            .Returns(true)
            .Returns(true)
            .Returns(false);
            
        // Mocking validation logic for columns if necessary
        mockReader.Setup(r => r["IdPessoa"]).Returns(1);
        mockReader.Setup(r => r["Nome"]).Returns("Teste");
        mockReader.Setup(r => r["Telefone"]).Returns("11999999999");
        mockReader.Setup(r => r["Email"]).Returns("teste@email.com");
        mockReader.Setup(r => r["DataNascimento"]).Returns(DateTime.Now);
        mockReader.Setup(r => r["IdSalao"]).Returns(1);
        mockReader.Setup(r => r["Excluido"]).Returns(false);
        SetupPessoaReaderDefaults(mockReader);

        var handler = new PessoaHandler(mockDbHandler.Object);

        // Act
        handler.Listar();

        // Assert
        mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("Excluido = 0") || s.Contains("Excluido IS NULL")), Times.AtLeastOnce());
    }

    [Fact]
    public void ListarExcluidos_DeveRetornarApenasExcluidos()
    {
        // Arrange
        int idSalao = 1;
        var mockDbHandler = new Mock<IDatabaseHandler>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();
        var mockReader = new Mock<IDataReader>();
        var parameters = new Mock<IDataParameterCollection>();
        var mockParameter = new Mock<IDbDataParameter>();

        mockDbHandler.Setup(db => db.GetConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(cmd => cmd.Parameters).Returns(parameters.Object);
        mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(mockParameter.Object);
        mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(mockReader.Object);

        var handler = new PessoaHandler(mockDbHandler.Object);

        // Act
        handler.ListarExcluidos(idSalao);

        // Assert
        mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("Excluido = 1")), Times.Once);
    }

    [Fact]
    public void Restaurar_DeveAtualizarExcluidoParaZero()
    {
        // Arrange
        int idPessoa = 1;
        var mockDbHandler = new Mock<IDatabaseHandler>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();
        var mockParameter = new Mock<IDbDataParameter>();
        var parameters = new Mock<IDataParameterCollection>();

        mockDbHandler.Setup(db => db.GetConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        mockCommand.Setup(cmd => cmd.Parameters).Returns(parameters.Object);
        mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(mockParameter.Object);

        var handler = new PessoaHandler(mockDbHandler.Object);

        // Act
        handler.Restaurar(idPessoa);

        // Assert
        mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(s => s.Contains("UPDATE CorteCor_Pessoa SET Excluido = 0")), Times.Once);
        mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
    }

    private static void SetupPessoaReaderDefaults(Mock<IDataReader> reader)
    {
        reader.Setup(r => r["CpfCnpj"]).Returns(DBNull.Value);
        reader.Setup(r => r["InscricaoEstadual"]).Returns(DBNull.Value);
        reader.Setup(r => r["InscricaoMunicipal"]).Returns(DBNull.Value);
        reader.Setup(r => r["Cep"]).Returns(DBNull.Value);
        reader.Setup(r => r["Logradouro"]).Returns(DBNull.Value);
        reader.Setup(r => r["Numero"]).Returns(DBNull.Value);
        reader.Setup(r => r["Complemento"]).Returns(DBNull.Value);
        reader.Setup(r => r["Bairro"]).Returns(DBNull.Value);
        reader.Setup(r => r["Cidade"]).Returns(DBNull.Value);
        reader.Setup(r => r["UF"]).Returns(DBNull.Value);
        reader.Setup(r => r["RazaoSocial"]).Returns(DBNull.Value);
        reader.Setup(r => r["NomeFantasia"]).Returns(DBNull.Value);
        reader.Setup(r => r["Cnae"]).Returns(DBNull.Value);
        reader.Setup(r => r["IsCliente"]).Returns(true);
        reader.Setup(r => r["IsFornecedor"]).Returns(false);
        reader.Setup(r => r["IsTransportador"]).Returns(false);
        reader.Setup(r => r["NomeContato"]).Returns(DBNull.Value);
        reader.Setup(r => r["Pais"]).Returns(DBNull.Value);
        reader.Setup(r => r["IdEstrangeiro"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntCep"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntUf"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntCidade"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntNome"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntCpfCnpj"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntInscricaoEstadual"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntLogradouro"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntNumero"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntComplemento"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntBairro"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntEmail"]).Returns(DBNull.Value);
        reader.Setup(r => r["EntTelefone"]).Returns(DBNull.Value);
        reader.Setup(r => r["ConsumidorFinal"]).Returns(DBNull.Value);
        reader.Setup(r => r["IndicadorIE"]).Returns(DBNull.Value);
        reader.Setup(r => r["IESubstTrib"]).Returns(DBNull.Value);
        reader.Setup(r => r["Suframa"]).Returns(DBNull.Value);
        reader.Setup(r => r["Tags"]).Returns(DBNull.Value);
        reader.Setup(r => r["DataComemorativa"]).Returns(DBNull.Value);
        reader.Setup(r => r["DescricaoComemoracao"]).Returns(DBNull.Value);
        reader.Setup(r => r["BasesLegais"]).Returns(DBNull.Value);
        reader.Setup(r => r["Observacoes"]).Returns(DBNull.Value);
    }
}
}

