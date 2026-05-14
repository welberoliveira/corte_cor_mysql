using CorteCor.Models;
using CorteCor.Handlers;
using Xunit;
using Moq;
using CorteCor;
using CorteCor.Services;

using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System;
using System.Collections.Generic;

namespace CorteCor.Tests
{
    public class SalaoServiceTests
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly Salaoervice _service;

        public SalaoServiceTests()
        {
            _mockCache = new Mock<IMemoryCache>();
            
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

            // Setup Cache Mocking (Basic)
            var cacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntry.Object);

            _service = new Salaoervice(_mockCache.Object, _mockDbHandler.Object);
        }

        [Fact]
        public void ObterSalao_SemCache_DeveConsultarBanco()
        {
            // Arrange
            object cacheKey = "Salao_1";
            object outValue;
            _mockCache.Setup(m => m.TryGetValue(cacheKey, out outValue)).Returns(false);

            // DB Returns
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(true);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["Nome"]).Returns("Salao Teste");

            // Ignore property sets on reader defaults (Handle via loose behavior or setup all)
            SetupReaderDefaults(_mockReader);

            // Act
            var result = _service.ObterSalao(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Salao Teste", result.Nome);
            _mockCommand.Verify(cmd => cmd.ExecuteReader(), Times.Once); // Called DB
            _mockCache.Verify(m => m.CreateEntry(cacheKey), Times.Once); // Set Cache
        }

        [Fact]
        public void InvalidarCache_DeveRemoverChave()
        {
            // Act
            _service.InvalidarCache(1);

            // Assert
            _mockCache.Verify(m => m.Remove("Salao_1"), Times.Once);
        }
        
        private void SetupReaderDefaults(Mock<IDataReader> reader)
        {
             reader.Setup(r => r["Responsavel"]).Returns("Resp");
             reader.Setup(r => r["Email"]).Returns("email");
             reader.Setup(r => r["Telefone"]).Returns("123");
             reader.Setup(r => r["Endereco"]).Returns("End");
             reader.Setup(r => r["CNPJ"]).Returns("000");
             reader.Setup(r => r["Status"]).Returns("Ativo");
             reader.Setup(r => r["DataCadastro"]).Returns(DateTime.Now);
             // reader.Setup(r => r["Observacao"]).Returns(DBNull.Value); // Optional access check
        }
    }
}

