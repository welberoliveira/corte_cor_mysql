using System.Data;
using CorteCor;
using CorteCor.Handlers;
using Moq;
using Xunit;

namespace CorteCor.Tests
{
    public class CategoriaProdutoHandlerTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly Mock<IDataParameterCollection> _mockParameters;
        private readonly Mock<IDbDataParameter> _mockParameter;
        private readonly Mock<IDataReader> _mockReader;
        private readonly CategoriaProdutoHandler _handler;

        public CategoriaProdutoHandlerTests()
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

            _handler = new CategoriaProdutoHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void ExcluirPorSalao_DeveRealizarSoftDelete()
        {
            _handler.ExcluirPorSalao(4, 1);

            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(sql =>
                sql.Contains("UPDATE CorteCor_CategoriaProduto") &&
                sql.Contains("SET Ativo = 0")), Times.Once);
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void ListarPaginadoPorSalao_DeveFiltrarCategoriasAtivasPorPadrao()
        {
            _mockCommand.Setup(cmd => cmd.ExecuteScalar()).Returns(0);
            _mockCommand.Setup(cmd => cmd.ExecuteReader()).Returns(_mockReader.Object);
            _mockReader.Setup(r => r.Read()).Returns(false);

            var result = _handler.ListarPaginadoPorSalao(1, "colo", false, 2, 10);

            Assert.Equal(2, result.PageIndex);
            Assert.Equal(0, result.TotalCount);
            _mockCommand.VerifySet(cmd => cmd.CommandText = It.Is<string>(sql =>
                sql.Contains("(@IncluirInativas = 1 OR Ativo = 1)") &&
                sql.Contains("Nome LIKE @Pesquisa")), Times.AtLeastOnce);
        }
    }
}
