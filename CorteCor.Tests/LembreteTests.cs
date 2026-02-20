using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace CorteCor.Tests
{
    public class LembreteTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly LembreteHandler _handler;

        public LembreteTests()
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

            _handler = new LembreteHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void ListarConfig_DeveRetornarLista()
        {
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdConfig"]).Returns(1);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(2);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);

            var result = _handler.ListarConfig(1);

            Assert.Single(result);
            Assert.Equal(2, result[0].AntecedenciaValor);
            Assert.Null(result[0].IdModeloEmail);
        }

        [Fact]
        public void ListarConfig_ComModelo_DeveRetornarAssunto()
        {
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var seq = new MockSequence();
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(true);
            _mockReader.InSequence(seq).Setup(r => r.Read()).Returns(false);

            _mockReader.Setup(r => r["IdConfig"]).Returns(2);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(1);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Dias");
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(5);
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns("Template Especial");

            var result = _handler.ListarConfig(1);

            Assert.Single(result);
            Assert.Equal("Template Especial", result[0].AssuntoModelo);
        }

        [Fact]
        public void SalvarConfig_DeveExecutarInsert()
        {
            var config = new LembreteConfig { IdSalao = 1, AntecedenciaValor = 1, AntecedenciaUnidade = "Dias", Ativo = true, DataInicio = DateTime.Now };
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(1);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(false);

            _handler.SalvarConfig(config);
            _mockCommand.Verify(cmd => cmd.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void ExcluirConfig_DeveExecutarDelete()
        {
            _handler.ExcluirConfig(1);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Exactly(2));
        }

        [Fact]
        public void GerarLembretes_DeveInserirLembretesProgramados()
        {
            var dataAgendamento = DateTime.Now.AddDays(1);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var readerSeq = new MockSequence();
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["IdConfig"]).Returns(10);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(2);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Horas");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false); 

            _handler.GerarLembretes(1);

            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeast(2));
        }

        [Fact]
        public void GerarLembretes_Minutos_DeveCalcularCorretamente()
        {
            var dataAgendamento = DateTime.Now.AddHours(2);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            var readerSeq = new MockSequence();
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["DataHora"]).Returns(dataAgendamento);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(true); 
            _mockReader.Setup(r => r["IdConfig"]).Returns(20);
            _mockReader.Setup(r => r["AntecedenciaValor"]).Returns(45);
            _mockReader.Setup(r => r["AntecedenciaUnidade"]).Returns("Minutos");
            _mockReader.Setup(r => r["Ativo"]).Returns(true);
            _mockReader.Setup(r => r["IdModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["AssuntoModeloEmail"]).Returns(DBNull.Value);
            _mockReader.Setup(r => r["IdSalao"]).Returns(1);
            _mockReader.Setup(r => r["DataCriacao"]).Returns(DateTime.Now);
            _mockReader.Setup(r => r["DataInicio"]).Returns(DateTime.Now.AddDays(-1));
            _mockReader.Setup(r => r["DataFim"]).Returns(DBNull.Value);
            _mockReader.InSequence(readerSeq).Setup(r => r.Read()).Returns(false);

            _handler.GerarLembretes(1);

            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.AtLeast(2));
        }
    }
}
