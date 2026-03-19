using CorteCor;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Pages;
using CorteCor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CorteCor.Tests
{
    public class Agendamentos2ModelTests
    {
        private readonly Mock<ServicoHandler> _mockServicoHandler;
        private readonly Mock<PessoaHandler> _mockPessoaHandler;
        private readonly Mock<AgendamentoHandler> _mockAgendamentoHandler;
        private readonly Mock<FuncionarioHandler> _mockFuncionarioHandler;
        private readonly Mock<FuncionarioServicoHandler> _mockFsHandler;
        private readonly Mock<PagamentoHandler> _mockPagamentoHandler;
        private readonly Mock<MeioPagamentoHandler> _mockMeioPagamentoHandler;
        private readonly Mock<MercadoPagoService> _mockMpService;
        private readonly Mock<AgendamentoPreparationService> _mockAgendamentoPreparationService;
        private readonly Mock<AgendamentoFiscalPreparationService> _mockAgendamentoFiscalPreparationService;
        private readonly Agendamentos2Model _pageModel;

        public Agendamentos2ModelTests()
        {
            _mockServicoHandler = new Mock<ServicoHandler>((IDatabaseHandler)null);
            _mockPessoaHandler = new Mock<PessoaHandler>((IDatabaseHandler)null);
            _mockAgendamentoHandler = new Mock<AgendamentoHandler>((IDatabaseHandler)null);
            _mockFuncionarioHandler = new Mock<FuncionarioHandler>((IDatabaseHandler)null);
            _mockFsHandler = new Mock<FuncionarioServicoHandler>((IDatabaseHandler)null);
            _mockPagamentoHandler = new Mock<PagamentoHandler>((IDatabaseHandler)null);
            _mockMeioPagamentoHandler = new Mock<MeioPagamentoHandler>((IDatabaseHandler)null);

            var config = new FakeConfiguration();
            _mockMpService = new Mock<MercadoPagoService>(config, (System.Net.Http.HttpClient)null);
            _mockAgendamentoPreparationService = new Mock<AgendamentoPreparationService>(
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                _mockAgendamentoHandler.Object,
                _mockFuncionarioHandler.Object,
                _mockFsHandler.Object);

            var fakeNotaFiscalHandler = new Mock<NotaFiscalHandler>((IDatabaseHandler)null);
            var fakeNotaFiscalLogHandler = new Mock<NotaFiscalLogHandler>((IDatabaseHandler)null);
            var fakeOrigemPreparationService = new FiscalOrigemPreparationService();
            var fakeNotaFiscalAvulsaService = new NotaFiscalAvulsaService(
                null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockAgendamentoFiscalPreparationService = new Mock<AgendamentoFiscalPreparationService>(
                _mockAgendamentoHandler.Object,
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                fakeNotaFiscalHandler.Object,
                fakeNotaFiscalLogHandler.Object,
                fakeOrigemPreparationService,
                fakeNotaFiscalAvulsaService);

            _pageModel = new Agendamentos2Model(
                _mockAgendamentoHandler.Object,
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                _mockMeioPagamentoHandler.Object,
                _mockPagamentoHandler.Object,
                _mockMpService.Object,
                _mockAgendamentoPreparationService.Object,
                _mockAgendamentoFiscalPreparationService.Object
            );

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim("IdSalao", "1"),
                new Claim("Role", "Usuario"),
                new Claim("Email", "teste@cortecor.com")
            }, "mock"));

            _pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void OnGetEvents_DeveRetornarAgendamentosFormatados()
        {
            var start = DateTime.Now;
            var end = start.AddDays(7);

            _mockAgendamentoPreparationService.Setup(h => h.ListarEventos(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<AgendamentoEventoCalendario>
                {
                    new AgendamentoEventoCalendario
                    {
                        Id = "1",
                        Title = "Joao - Corte",
                        Start = start,
                        End = start.AddMinutes(30),
                        Color = "#3788d8"
                    }
                });

            var result = _pageModel.OnGetEvents(start, end);

            Assert.IsType<JsonResult>(result);
            _mockAgendamentoPreparationService.Verify(h => h.ListarEventos(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public void OnPostCreate_ComDadosValidos_DeveCriarAgendamento()
        {
            var today = DateTime.Today;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            var start = today.AddDays(daysUntilMonday).AddHours(10);

            var req = new Agendamentos2Model.CreateRequest
            {
                Start = start.ToString("O"),
                IdPessoa = 1,
                IdServico = 1
            };

            _mockServicoHandler.Setup(h => h.ObterPorId(1)).Returns(new Servico { IdServico = 1, IdSalao = 1, Duracao = TimeSpan.FromMinutes(30), Nome = "Corte" });
            _mockPessoaHandler.Setup(h => h.ObterPorId(1)).Returns(new Pessoa { IdPessoa = 1, IdSalao = 1, Nome = "Joao" });

            _mockAgendamentoPreparationService.Setup(h => h.ValidarHorarioServico(1, It.IsAny<DateTime>(), null, 1))
                .Returns(new AgendamentoHorarioValidado { Inicio = start, Fim = start.AddMinutes(30) });
            _mockAgendamentoPreparationService.Setup(h => h.ObterFuncionarioDisponivelId(1, start, 1, null))
                .Returns(1);

            _mockAgendamentoHandler.Setup(h => h.CadastrarAgendamento(It.IsAny<Agendamento>()))
                .Returns(123);

            var result = _pageModel.OnPostCreate(req);

            Assert.IsType<JsonResult>(result);
            _mockAgendamentoHandler.Verify(h => h.CadastrarAgendamento(It.IsAny<Agendamento>()), Times.Once);
        }

        [Fact]
        public async Task OnGetDetails_DeveRetornarSituacaoFiscalDoAgendamento()
        {
            _mockAgendamentoHandler.Setup(h => h.ObterPorId(10))
                .Returns(new Agendamento
                {
                    IdAgendamento = 10,
                    IdPessoa = 2,
                    IdServico = 3,
                    Status = AgendamentoStatus.Pago,
                    DataHora = DateTime.Today.AddHours(10)
                });
            _mockServicoHandler.Setup(h => h.ObterPorId(3))
                .Returns(new Servico { IdServico = 3, Nome = "Coloracao" });
            _mockAgendamentoFiscalPreparationService
                .Setup(h => h.ObterSituacaoFiscalAsync(1, 10, AgendamentoStatus.Pago))
                .ReturnsAsync(new AgendamentoSituacaoFiscalResult
                {
                    IdAgendamento = 10,
                    StatusAgendamento = AgendamentoStatus.Pago,
                    PossuiNota = true,
                    PossuiNotaAtiva = true,
                    PodeEmitir = false,
                    PodeAbrirNota = true,
                    StatusFiscal = NotaFiscalStatus.Autorizada,
                    ClasseStatusFiscal = "bg-success",
                    Mensagem = "Ja existe NFS-e 123/1 com status Autorizada.",
                    NumeroNota = 123,
                    SerieNota = 1,
                    TipoNota = "NFS-e",
                    IdNotaFiscal = Guid.NewGuid()
                });

            var result = await _pageModel.OnGetDetails(10);

            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            var payload = json.Value!;
            Assert.Equal(AgendamentoStatus.Pago, payload.GetType().GetProperty("status")?.GetValue(payload)?.ToString());

            var fiscal = payload.GetType().GetProperty("fiscal")?.GetValue(payload);
            Assert.NotNull(fiscal);
            Assert.Equal(NotaFiscalStatus.Autorizada, fiscal!.GetType().GetProperty("StatusFiscal")?.GetValue(fiscal)?.ToString());
            Assert.Equal("123", fiscal.GetType().GetProperty("NumeroNota")?.GetValue(fiscal)?.ToString());
        }

        [Fact]
        public async Task OnPostEmitirNota_DeveRetornarFalhaDeNegocioQuandoJaExisteNotaAtiva()
        {
            _mockAgendamentoFiscalPreparationService
                .Setup(h => h.EmitirNotaServicoAsync(1, 55, It.IsAny<string?>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Ja existe NFS-e 321/1 com status Autorizada."));

            _mockAgendamentoFiscalPreparationService
                .Setup(h => h.ObterSituacaoFiscalAsync(1, 55, null))
                .ReturnsAsync(new AgendamentoSituacaoFiscalResult
                {
                    IdAgendamento = 55,
                    StatusAgendamento = AgendamentoStatus.Pago,
                    PossuiNota = true,
                    PossuiNotaAtiva = true,
                    PodeEmitir = false,
                    PodeAbrirNota = true,
                    StatusFiscal = NotaFiscalStatus.Autorizada,
                    ClasseStatusFiscal = "bg-success",
                    Mensagem = "Ja existe NFS-e 321/1 com status Autorizada.",
                    NumeroNota = 321,
                    SerieNota = 1,
                    TipoNota = "NFS-e",
                    IdNotaFiscal = Guid.NewGuid()
                });

            var result = await _pageModel.OnPostEmitirNota(new Agendamentos2Model.EmitirNotaRequest
            {
                IdAgendamento = 55
            });

            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            var payload = json.Value!;
            Assert.Equal("False", payload.GetType().GetProperty("success")?.GetValue(payload)?.ToString());
            Assert.Contains("Ja existe", payload.GetType().GetProperty("message")?.GetValue(payload)?.ToString());
        }

        [Fact]
        public async Task OnPostEmitirNota_DeveRetornarSucessoEAtalhoParaNota()
        {
            var idNota = Guid.NewGuid();
            _mockAgendamentoFiscalPreparationService
                .Setup(h => h.EmitirNotaServicoAsync(1, 77, It.IsAny<string?>(), It.IsAny<string>()))
                .ReturnsAsync(new NotaFiscalOperacaoResult
                {
                    Mensagem = "NFS-e autorizada com sucesso.",
                    NotaFiscal = new NotaFiscal
                    {
                        IdNotaFiscal = idNota,
                        Status = NotaFiscalStatus.Autorizada,
                        Numero = 777,
                        Serie = 1,
                        TipoNota = "NFS-e"
                    }
                });

            _mockAgendamentoFiscalPreparationService
                .Setup(h => h.ObterSituacaoFiscalAsync(1, 77, null))
                .ReturnsAsync(new AgendamentoSituacaoFiscalResult
                {
                    IdAgendamento = 77,
                    StatusAgendamento = AgendamentoStatus.Pago,
                    PossuiNota = true,
                    PossuiNotaAtiva = true,
                    PodeEmitir = false,
                    PodeAbrirNota = true,
                    StatusFiscal = NotaFiscalStatus.Autorizada,
                    ClasseStatusFiscal = "bg-success",
                    Mensagem = "Ja existe NFS-e 777/1 com status Autorizada.",
                    NumeroNota = 777,
                    SerieNota = 1,
                    TipoNota = "NFS-e",
                    IdNotaFiscal = idNota
                });

            var result = await _pageModel.OnPostEmitirNota(new Agendamentos2Model.EmitirNotaRequest
            {
                IdAgendamento = 77
            });

            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);

            var payload = json.Value!;
            Assert.Equal("True", payload.GetType().GetProperty("success")?.GetValue(payload)?.ToString());
            Assert.Contains("/NotaFiscalLista", payload.GetType().GetProperty("redirectUrl")?.GetValue(payload)?.ToString());
        }
    }

    public class FakeConfiguration : Microsoft.Extensions.Configuration.IConfiguration
    {
        public string? this[string key] { get => "mock-token"; set { } }
        public IEnumerable<Microsoft.Extensions.Configuration.IConfigurationSection> GetChildren() => throw new NotImplementedException();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotImplementedException();
        public Microsoft.Extensions.Configuration.IConfigurationSection GetSection(string key) => throw new NotImplementedException();
    }
}
