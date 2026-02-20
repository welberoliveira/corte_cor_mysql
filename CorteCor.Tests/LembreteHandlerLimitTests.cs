using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Xunit;
using Moq;
using CorteCor;

using System.Data;
using System;

namespace CorteCor.Tests
{
    public class LembreteHandlerLimitTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<IDbConnection> _mockConnection;
        private readonly Mock<IDbCommand> _mockCommand;
        private readonly LembreteHandler _handler;

        public LembreteHandlerLimitTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockConnection = new Mock<IDbConnection>();
            _mockCommand = new Mock<IDbCommand>();

            _mockDbHandler.Setup(db => db.GetConnection()).Returns(_mockConnection.Object);
            _mockConnection.Setup(conn => conn.CreateCommand()).Returns(_mockCommand.Object);
            _mockCommand.Setup(cmd => cmd.Parameters).Returns(new Mock<IDataParameterCollection>().Object);
            _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(new Mock<IDbDataParameter>().Object);

            _handler = new LembreteHandler(_mockDbHandler.Object);
        }

        [Fact]
        public void VerificarLimiteSMS_WhenLimitIsZeroAndNoneSent_ShouldReturnTrue()
        {
            // Setup: Limit 0 (result from ExecuteScalar for queryLimit)
            _mockCommand.Setup(c => c.ExecuteScalar()).Returns(0);
            
            // Setup: 0 sent (result from ExecuteScalar for queryCount)
            // Wait, we need to handle sequential calls to ExecuteScalar or use different setups.
            // Since they are exactly the same call signatures...
            var seq = new MockSequence();
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(0); // For Limit
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(0); // For Sent Count

            bool reached = _handler.VerificarLimiteSMS(1, out int enviados, out int limite);

            Assert.True(reached);
            Assert.Equal(0, enviados);
            Assert.Equal(0, limite);
        }

        [Fact]
        public void VerificarLimiteSMS_WhenLimitIs10And10Sent_ShouldReturnTrue()
        {
            var seq = new MockSequence();
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(10); // For Limit
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(10); // For Sent Count

            bool reached = _handler.VerificarLimiteSMS(1, out int enviados, out int limite);

            Assert.True(reached);
            Assert.Equal(10, enviados);
            Assert.Equal(10, limite);
        }

        [Fact]
        public void VerificarLimiteSMS_WhenLimitIs10And5Sent_ShouldReturnFalse()
        {
            var seq = new MockSequence();
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(10); // For Limit
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(5);  // For Sent Count

            bool reached = _handler.VerificarLimiteSMS(1, out int enviados, out int limite);

            Assert.False(reached);
            Assert.Equal(5, enviados);
            Assert.Equal(10, limite);
        }

        [Fact]
        public void VerificarLimiteEmail_ShouldImplicitlyTestNullTipoLembreteInSql()
        {
            // This test verifies that the logic still works. 
            // The actual "ISNULL(TipoLembrete, 'Email')" is in the SQL string, which we can't test easily with mocks 
            // without inspecting the command text.
            
            _mockCommand.Setup(c => c.CommandText).Returns(string.Empty);
            
            var seq = new MockSequence();
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(50); // Limit
            _mockCommand.InSequence(seq).Setup(c => c.ExecuteScalar()).Returns(30); // Sent

            bool reached = _handler.VerificarLimiteEmail(1, out int enviados, out int limite);

            Assert.False(reached);
            Assert.Equal(30, enviados);
            Assert.Equal(50, limite);
        }
    }
}

