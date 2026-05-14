using CorteCor;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CorteCor.Tests
{
    public class AgendamentoFiscalPreparationServiceTests
    {
        private readonly Mock<AgendamentoHandler> _mockAgendamentoHandler = new((IDatabaseHandler)null);
        private readonly Mock<ServicoHandler> _mockServicoHandler = new((IDatabaseHandler)null);
        private readonly Mock<PessoaHandler> _mockPessoaHandler = new((IDatabaseHandler)null);
        private readonly Mock<NotaFiscalHandler> _mockNotaFiscalHandler = new((IDatabaseHandler)null);
        private readonly Mock<NotaFiscalLogHandler> _mockNotaFiscalLogHandler = new((IDatabaseHandler)null);

        [Fact]
        public async Task ObterSituacaoFiscalAsync_DevePermitirEmissaoQuandoAgendamentoPagoNaoTemNotaAtiva()
        {
            var service = CriarService();
            _mockNotaFiscalHandler
                .Setup(h => h.ListarPorAgendamentoAsync(1, 90))
                .ReturnsAsync(new List<NotaFiscal>
                {
                    new NotaFiscal
                    {
                        IdNotaFiscal = Guid.NewGuid(),
                        IdSalao = 1,
                        IdAgendamento = 90,
                        TipoNota = "NFS-e",
                        Numero = 12,
                        Serie = 1,
                        Status = NotaFiscalStatus.Cancelada
                    }
                });

            var situacao = await service.ObterSituacaoFiscalAsync(1, 90, AgendamentoStatus.Pago);

            Assert.True(situacao.PossuiNota);
            Assert.False(situacao.PossuiNotaAtiva);
            Assert.True(situacao.PodeEmitir);
            Assert.True(situacao.PodeAbrirNota);
            Assert.Equal(NotaFiscalStatus.Cancelada, situacao.StatusFiscal);
        }

        [Fact]
        public async Task ObterSituacaoFiscalAsync_DeveBloquearEmissaoQuandoHaNotaAtiva()
        {
            var service = CriarService();
            var idNota = Guid.NewGuid();
            _mockNotaFiscalHandler
                .Setup(h => h.ListarPorAgendamentoAsync(1, 91))
                .ReturnsAsync(new List<NotaFiscal>
                {
                    new NotaFiscal
                    {
                        IdNotaFiscal = idNota,
                        IdSalao = 1,
                        IdAgendamento = 91,
                        TipoNota = "NFS-e",
                        Numero = 33,
                        Serie = 1,
                        Status = NotaFiscalStatus.Autorizada
                    },
                    new NotaFiscal
                    {
                        IdNotaFiscal = Guid.NewGuid(),
                        IdSalao = 1,
                        IdAgendamento = 91,
                        TipoNota = "NFS-e",
                        Numero = 32,
                        Serie = 1,
                        Status = NotaFiscalStatus.Rejeitada
                    }
                });

            var situacao = await service.ObterSituacaoFiscalAsync(1, 91, AgendamentoStatus.Pago);

            Assert.True(situacao.PossuiNota);
            Assert.True(situacao.PossuiNotaAtiva);
            Assert.False(situacao.PodeEmitir);
            Assert.True(situacao.PodeAbrirNota);
            Assert.Equal(NotaFiscalStatus.Autorizada, situacao.StatusFiscal);
            Assert.Equal(idNota, situacao.IdNotaFiscal);
            Assert.Equal(33, situacao.NumeroNota);
        }

        [Fact]
        public async Task PrepararEnvelopeAsync_DeveSanitizarCodigoTributacaoDoServico()
        {
            var service = CriarService();
            _mockAgendamentoHandler
                .Setup(h => h.ObterPorId(101))
                .Returns(new Agendamento
                {
                    IdAgendamento = 101,
                    IdPessoa = 5,
                    IdServico = 10,
                    Status = AgendamentoStatus.Pago,
                    DataHora = new DateTime(2026, 3, 19, 10, 0, 0)
                });
            _mockServicoHandler
                .Setup(h => h.ObterPorId(10))
                .Returns(new Servico
                {
                    IdServico = 10,
                    IdSalao = 1,
                    Nome = "Desenvolvimento de Sistema Simples",
                    Preco = 101m,
                    CodigoTributacaoMunicipio = "01.01.01",
                    AliquotaISS = 3m,
                    CodNBS = "1.01.01"
                });
            _mockPessoaHandler
                .Setup(h => h.ObterPorId(5))
                .Returns(new Pessoa
                {
                    IdPessoa = 5,
                    IdSalao = 1,
                    Nome = "Jeane Ferreira da Silva",
                    CpfCnpj = "05528366640",
                    Email = "welberoliveira3@gmail.com",
                    Logradouro = "Rua Joaquim Pereira",
                    Numero = "521",
                    Bairro = "Santa Rita",
                    Cidade = "Montes Claros",
                    UF = "MG",
                    Cep = "39402000"
                });

            var envelope = await service.PrepararEnvelopeAsync(1, 101);

            Assert.Single(envelope.Itens);
            Assert.Equal("010101", envelope.Itens[0].CodigoTributacaoMunicipio);
            Assert.Equal("10101", envelope.Itens[0].Ncm);
            Assert.Equal(3m, envelope.Itens[0].AliquotaIss);
        }

        [Fact]
        public async Task PrepararEnvelopeAsync_DeveFalharQuandoServicoNaoPossuiCodigoFiscalValido()
        {
            var service = CriarService();
            _mockAgendamentoHandler
                .Setup(h => h.ObterPorId(102))
                .Returns(new Agendamento
                {
                    IdAgendamento = 102,
                    IdPessoa = 5,
                    IdServico = 11,
                    Status = AgendamentoStatus.Pago,
                    DataHora = new DateTime(2026, 3, 19, 11, 0, 0)
                });
            _mockServicoHandler
                .Setup(h => h.ObterPorId(11))
                .Returns(new Servico
                {
                    IdServico = 11,
                    IdSalao = 1,
                    Nome = "Servico sem fiscal",
                    Preco = 80m,
                    CodigoTributacaoMunicipio = ".."
                });
            _mockPessoaHandler
                .Setup(h => h.ObterPorId(5))
                .Returns(new Pessoa
                {
                    IdPessoa = 5,
                    IdSalao = 1,
                    Nome = "Cliente Teste",
                    Cidade = "Montes Claros",
                    UF = "MG"
                });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.PrepararEnvelopeAsync(1, 102));

            Assert.Contains("c\u00F3digo de tributa\u00E7\u00E3o fiscal v\u00E1lido", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private AgendamentoFiscalPreparationService CriarService()
        {
            var fakeOrigemPreparationService = new FiscalOrigemPreparationService();
            var fakeNotaFiscalAvulsaService = new NotaFiscalAvulsaService(
                null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            return new AgendamentoFiscalPreparationService(
                _mockAgendamentoHandler.Object,
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                _mockNotaFiscalHandler.Object,
                _mockNotaFiscalLogHandler.Object,
                fakeOrigemPreparationService,
                fakeNotaFiscalAvulsaService);
        }
    }
}
