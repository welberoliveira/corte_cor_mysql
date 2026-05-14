using CorteCor;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Moq;
using Xunit;

namespace CorteCor.Tests
{
    public class AgendamentoPreparationServiceTests
    {
        private readonly Mock<ServicoHandler> _mockServicoHandler = new((IDatabaseHandler)null);
        private readonly Mock<PessoaHandler> _mockPessoaHandler = new((IDatabaseHandler)null);
        private readonly Mock<AgendamentoHandler> _mockAgendamentoHandler = new((IDatabaseHandler)null);
        private readonly Mock<FuncionarioHandler> _mockFuncionarioHandler = new((IDatabaseHandler)null);
        private readonly Mock<FuncionarioServicoHandler> _mockFuncionarioServicoHandler = new((IDatabaseHandler)null);

        [Fact]
        public void ObterFuncionarioDisponivelId_DeveRetornarFuncionarioLivre()
        {
            var service = CriarService();
            var inicio = new DateTime(2026, 3, 23, 10, 0, 0);

            _mockServicoHandler.Setup(h => h.ObterPorId(7))
                .Returns(new Servico { IdServico = 7, IdSalao = 1, Duracao = TimeSpan.FromMinutes(40) });
            _mockFuncionarioServicoHandler.Setup(h => h.ListarFuncionariosDoServico(7))
                .Returns(new List<int> { 11 });
            _mockFuncionarioHandler.Setup(h => h.ObterPorId(11))
                .Returns(new Funcionario
                {
                    IdFuncionario = 11,
                    IdSalao = 1,
                    seg = true,
                    seg_ini = TimeSpan.FromHours(8),
                    seg_fim = TimeSpan.FromHours(18)
                });
            _mockAgendamentoHandler.Setup(h => h.VerificarDisponibilidade(11, inicio, inicio.AddMinutes(40), null))
                .Returns(true);

            var funcionarioId = service.ObterFuncionarioDisponivelId(7, inicio, 1);

            Assert.Equal(11, funcionarioId);
        }

        [Fact]
        public void ValidarHorarioServico_DeveFalharQuandoFimNaoConfereComDuracao()
        {
            var service = CriarService();
            var inicio = DateTime.Today.AddHours(9);

            _mockServicoHandler.Setup(h => h.ObterPorId(5))
                .Returns(new Servico { IdServico = 5, IdSalao = 1, Duracao = TimeSpan.FromMinutes(45) });

            var ex = Assert.Throws<InvalidOperationException>(() =>
                service.ValidarHorarioServico(5, inicio, inicio.AddMinutes(30), 1));

            Assert.Contains("dura\u00E7\u00E3o", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private AgendamentoPreparationService CriarService() =>
            new(
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                _mockAgendamentoHandler.Object,
                _mockFuncionarioHandler.Object,
                _mockFuncionarioServicoHandler.Object);
    }
}
