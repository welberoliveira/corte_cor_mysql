using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;

namespace CorteCor.Tests
{
    public class PessoaFichaHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly PessoaFichaHandler _handler;

        public PessoaFichaHandlerTests()
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

            _handler = new PessoaFichaHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void CadastrarPessoa_DeveRetornarId()
        {
            // Arrange
            var pessoa = new PessoaFicha
            {
                FichaID = 1,
                Nome = "Pessoa Teste",
                // ... outros campos ...
            };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(55);

            // Act
            var result = _handler.CadastrarPessoa(pessoa);

            // Assert
            Assert.Equal(55, result);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void Listar_DeveRetornarPessoas()
        {
            // Arrange
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["PessoaID"]).Returns(1);
            _mockReader.Setup(r => r["FichaID"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Pessoa 1");
            // Set fields being read to avoid null ref or format exceptions if handler reads them
            _mockReader.Setup(r => r["Filiacao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["RG"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["CPF"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataNascimento"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Nacionalidade"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["NIS"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["EstadoCivil"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["RegimeCasamento"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["SituacaoProfissional"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Profissao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["GrauInstrucao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Iletrado"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Empresa"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["CarteiraAssinada"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["RendaMensal"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Endereco"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Quadra"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["PontoReferencia"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Bairro"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Lote"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["MunicipioResidencia"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Telefone"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Celular"]).Returns(DBNull.Value);
            
            // Spouse fields
            _mockReader.Setup(r => r["ConjugeNome"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeFiliacao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeRG"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeCPF"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeIdade"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeNacionalidade"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeSituacaoProfissional"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeProfissao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeGrauInstrucao"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeIletrado"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeEmpresa"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeCarteiraAssinada"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["ConjugeRendaMensal"]).Returns(DBNull.Value);

            // Act
            var result = _handler.Listar();

            // Assert
            Assert.Single(result);
            Assert.Equal("Pessoa 1", result[0].Nome);
        }

        [Fact]
        public void Atualizar_DeveExecutarUpdate()
        {
            // Arrange
            var pessoa = new PessoaFicha { FichaID = 1, Nome = "Nome Atualizado" };

            // Act
            _handler.Atualizar(pessoa);

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
    }
}
