using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Moq;

namespace CorteCor.Tests
{
    public class FinanceiroServiceTests
    {
        private readonly Mock<IFinanceiroModuloHandler> _handler = new();

        [Fact]
        public async Task GarantirEstruturaBaseAsync_DeveCriarPlanosEContaPadrao_QuandoNaoExistiremRegistros()
        {
            _handler.Setup(h => h.ListarPlanoContasAsync(1)).ReturnsAsync(new List<PlanoContas>());
            _handler.Setup(h => h.ListarContasCaixaAsync(1)).ReturnsAsync(new List<ContaCaixa>());

            var service = CriarService();

            await service.GarantirEstruturaBaseAsync(1);

            _handler.Verify(h => h.SavePlanoContasAsync(It.IsAny<PlanoContas>()), Times.Exactly(5));
            _handler.Verify(h => h.SaveContaCaixaAsync(It.Is<ContaCaixa>(c =>
                c.IdSalao == 1 &&
                c.Nome == "Caixa Principal" &&
                c.Tipo == "Caixa" &&
                c.Ativo)), Times.Once);
        }

        [Fact]
        public async Task SalvarTituloAsync_DeveNormalizarCamposEStatusAntesDePersistir()
        {
            ConfigurarEstruturaExistente(1);
            _handler.Setup(h => h.SalvarTituloAsync(It.IsAny<FinanceiroTitulo>())).ReturnsAsync(Guid.NewGuid());

            var service = CriarService();

            await service.SalvarTituloAsync(1, new FinanceiroTitulo
            {
                Tipo = "qualquer-coisa",
                Origem = " ",
                Descricao = "  Servico premium  ",
                Documento = " DOC-9 ",
                Observacoes = "  observacao interna  ",
                ValorOriginal = 250m,
                DataCompetencia = default,
                DataVencimento = DateTime.Today.AddDays(-2)
            });

            _handler.Verify(h => h.SalvarTituloAsync(It.Is<FinanceiroTitulo>(t =>
                t.IdSalao == 1 &&
                t.Tipo == FinanceiroTipoTitulo.Receber &&
                t.Origem == FinanceiroOrigemTitulo.Manual &&
                t.Descricao == "Servico premium" &&
                t.Documento == "DOC-9" &&
                t.Observacoes == "observacao interna" &&
                t.Status == FinanceiroStatusTitulo.Vencido &&
                t.ValorAberto == 250m &&
                t.ValorLiquidado == 0m &&
                t.DataCompetencia.Date == DateTime.Today &&
                t.DataVencimento.Date == DateTime.Today.AddDays(-2))), Times.Once);
        }

        [Fact]
        public async Task SalvarTituloAsync_DeveFalhar_QuandoValorNaoForMaiorQueZero()
        {
            ConfigurarEstruturaExistente(1);
            var service = CriarService();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SalvarTituloAsync(1, new FinanceiroTitulo
            {
                Descricao = "Lancamento invalido",
                ValorOriginal = 0m
            }));

            Assert.Equal("Informe um valor maior que zero para o lançamento.", ex.Message);
            _handler.Verify(h => h.SalvarTituloAsync(It.IsAny<FinanceiroTitulo>()), Times.Never);
        }

        [Fact]
        public async Task ObterRelatoriosAsync_DeveSincronizarTitulosAntesDeMontarResumo()
        {
            ConfigurarEstruturaExistente(1);
            _handler.Setup(h => h.ObterRelatoriosAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new FinanceiroRelatorioResumo());

            var service = CriarService();

            await service.ObterRelatoriosAsync(1, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

            _handler.Verify(h => h.SincronizarTitulosPagamentoAsync(1), Times.Once);
            _handler.Verify(h => h.ObterRelatoriosAsync(1, new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)), Times.Once);
        }

        [Fact]
        public async Task LiquidarTituloAsync_DeveAtualizarStatusComoLiquidado()
        {
            var service = CriarService();
            var idTitulo = Guid.NewGuid();
            var dataLiquidacao = new DateTime(2026, 3, 23, 15, 0, 0);

            await service.LiquidarTituloAsync(1, idTitulo, 180m, dataLiquidacao, true);

            _handler.Verify(h => h.AtualizarStatusTituloAsync(
                1,
                idTitulo,
                FinanceiroStatusTitulo.Liquidado,
                dataLiquidacao,
                180m,
                true), Times.Once);
        }

        [Fact]
        public async Task AplicarAjustePosVendaAsync_DeveCriarReceber_QuandoDiferencaForPositiva()
        {
            ConfigurarEstruturaExistente(1);
            _handler.Setup(h => h.SalvarTituloAsync(It.IsAny<FinanceiroTitulo>())).ReturnsAsync(Guid.NewGuid());

            var service = CriarService();

            var resumo = await service.AplicarAjustePosVendaAsync(1, 10, 54, 35m, "Complemento pós-venda", "ajuste");

            Assert.Contains("título a receber", resumo, StringComparison.OrdinalIgnoreCase);
            _handler.Verify(h => h.SalvarTituloAsync(It.Is<FinanceiroTitulo>(t =>
                t.IdSalao == 1 &&
                t.IdVendaProduto == 10 &&
                t.IdPessoa == 54 &&
                t.Tipo == FinanceiroTipoTitulo.Receber &&
                t.Origem == FinanceiroOrigemTitulo.PosVenda &&
                t.ValorOriginal == 35m &&
                t.ValorAberto == 35m)), Times.Once);
        }

        [Fact]
        public async Task AplicarAjustePosVendaAsync_DeveAbaterTituloAbertoAntesDeGerarCredito()
        {
            ConfigurarEstruturaExistente(1);
            var idTitulo = Guid.NewGuid();
            _handler.Setup(h => h.ListarTitulosPorVendaAsync(1, 9)).ReturnsAsync(new List<FinanceiroTitulo>
            {
                new()
                {
                    IdTitulo = idTitulo,
                    IdSalao = 1,
                    IdVendaProduto = 9,
                    Tipo = FinanceiroTipoTitulo.Receber,
                    Status = FinanceiroStatusTitulo.Aberto,
                    ValorOriginal = 100m,
                    ValorLiquidado = 0m,
                    ValorAberto = 100m,
                    DataVencimento = DateTime.Today.AddDays(1)
                }
            });

            var service = CriarService();

            var resumo = await service.AplicarAjustePosVendaAsync(1, 9, 54, -40m, "Crédito pós-venda", "ajuste");

            Assert.Contains("ajustados", resumo, StringComparison.OrdinalIgnoreCase);
            _handler.Verify(h => h.AtualizarValoresTituloAsync(
                1,
                idTitulo,
                60m,
                0m,
                60m,
                FinanceiroStatusTitulo.Aberto,
                null,
                false,
                "ajuste"), Times.Once);
            _handler.Verify(h => h.SalvarTituloAsync(It.IsAny<FinanceiroTitulo>()), Times.Never);
        }

        private FinanceiroService CriarService() => new(_handler.Object);

        private void ConfigurarEstruturaExistente(int idSalao)
        {
            _handler.Setup(h => h.ListarPlanoContasAsync(idSalao))
                .ReturnsAsync(new List<PlanoContas> { new() { IdPlano = 1, IdSalao = idSalao, Descricao = "Receitas de Servicos", Tipo = "R", Ativo = true } });
            _handler.Setup(h => h.ListarContasCaixaAsync(idSalao))
                .ReturnsAsync(new List<ContaCaixa> { new() { IdConta = 1, IdSalao = idSalao, Nome = "Caixa Principal", Tipo = "Caixa", Ativo = true } });
        }
    }
}
