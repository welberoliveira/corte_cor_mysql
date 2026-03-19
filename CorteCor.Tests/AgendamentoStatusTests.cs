using CorteCor.Services;
using Xunit;

namespace CorteCor.Tests
{
    public class AgendamentoStatusTests
    {
        [Theory]
        [InlineData("Confirmado", AgendamentoStatus.Pago)]
        [InlineData("Pago", AgendamentoStatus.Pago)]
        [InlineData("Agendado", AgendamentoStatus.Agendado)]
        [InlineData("Cancelado", AgendamentoStatus.Cancelado)]
        public void Normalizar_DeveRetornarStatusCanonico(string statusEntrada, string statusEsperado)
        {
            var resultado = AgendamentoStatus.Normalizar(statusEntrada);
            Assert.Equal(statusEsperado, resultado);
        }

        [Fact]
        public void PodeEmitirNota_DevePermitirApenasStatusPago()
        {
            Assert.True(AgendamentoStatus.PodeEmitirNota("Pago"));
            Assert.True(AgendamentoStatus.PodeEmitirNota("Confirmado"));
            Assert.False(AgendamentoStatus.PodeEmitirNota("Agendado"));
            Assert.False(AgendamentoStatus.PodeEmitirNota("Cancelado"));
        }

        [Theory]
        [InlineData("Agendado", "badge-success")]
        [InlineData("Pendente", "badge-warning text-dark")]
        [InlineData("Pago", "badge-primary")]
        [InlineData("Cancelado", "badge-danger")]
        public void ObterClasseBadgeBootstrap_DeveRetornarClasseEsperada(string status, string classeEsperada)
        {
            Assert.Equal(classeEsperada, AgendamentoStatus.ObterClasseBadgeBootstrap(status));
        }
    }
}
