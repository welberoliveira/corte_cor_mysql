using CorteCor.Models;
using CorteCor.Handlers;
using Xunit;
using Moq;
using CorteCor.Pages;
using CorteCor;
using CorteCor.Services;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
        private readonly Mock<MeioPagamentoHandler> _mockMeioPagamentoHandler; // Added
        private readonly Mock<MercadoPagoService> _mockMpService;
        private readonly Mock<IValidaParametrosMunicipioService> _mockValidaParametrosMunicipioService;
        private readonly Agendamentos2Model _pageModel;

        public Agendamentos2ModelTests()
        {
            _mockServicoHandler = new Mock<ServicoHandler>((IDatabaseHandler)null);
            _mockPessoaHandler = new Mock<PessoaHandler>((IDatabaseHandler)null);
            _mockAgendamentoHandler = new Mock<AgendamentoHandler>((IDatabaseHandler)null);
            _mockFuncionarioHandler = new Mock<FuncionarioHandler>((IDatabaseHandler)null);
            _mockFsHandler = new Mock<FuncionarioServicoHandler>((IDatabaseHandler)null);
            _mockPagamentoHandler = new Mock<PagamentoHandler>((IDatabaseHandler)null);
            _mockMeioPagamentoHandler = new Mock<MeioPagamentoHandler>((IDatabaseHandler)null); // Init
            _mockValidaParametrosMunicipioService = new Mock<IValidaParametrosMunicipioService>();
            
            var config = new FakeConfiguration();
            _mockMpService = new Mock<MercadoPagoService>(config, (System.Net.Http.HttpClient)null);

            _pageModel = new Agendamentos2Model(
                _mockServicoHandler.Object,
                _mockPessoaHandler.Object,
                _mockAgendamentoHandler.Object,
                _mockFuncionarioHandler.Object,
                _mockFsHandler.Object,
                _mockMeioPagamentoHandler.Object, // Pass it
                _mockPagamentoHandler.Object,
                _mockMpService.Object,
                config,
                null,
                null,
                null,
                null,
                null,
                null,
                _mockValidaParametrosMunicipioService.Object
            );

            // Setup User Context
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim("IdSalao", "1"),
                new Claim("Role", "Admin")
            }, "mock"));
            _pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void OnGetEvents_DeveRetornarAgendamentosFormatados()
        {
            // Arrange
            var start = DateTime.Now;
            var end = start.AddDays(7);
            
            _mockAgendamentoHandler.Setup(h => h.ListarPorIntervalo(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<Agendamento>
                {
                    new Agendamento { IdAgendamento = 1, DataHora = start, Status = "Agendado", IdServico = 1, IdPessoa = 1 }
                });

            _mockServicoHandler.Setup(h => h.ListarPorSalao(1, It.IsAny<int?>()))
                .Returns(new List<Servico> { new Servico { IdServico = 1, Nome = "Corte", Duracao = TimeSpan.FromMinutes(30) } });
            
            _mockPessoaHandler.Setup(h => h.ListarPorSalao(1))
                .Returns(new List<Pessoa> { new Pessoa { IdPessoa = 1, Nome = "Joao Silva" } });

            // Act
            var result = _pageModel.OnGetEvents(start, end);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            // Verification of content is complex with dynamic/anonymous types in JsonResult, 
            // but we ensure it returns JSON and handler was called.
            _mockAgendamentoHandler.Verify(h => h.ListarPorIntervalo(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public void OnPostCreate_ComDadosValidos_DeveCriarAgendamento()
        {
            // Arrange
            // Ensure next Monday at 10:00 AM
            var today = DateTime.Today;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            var start = today.AddDays(daysUntilMonday).AddHours(10);

            var req = new Agendamentos2Model.CreateRequest
            {
                Start = start.ToString("O"), // ISO 8601
                IdPessoa = 1,
                IdServico = 1
            };

            _mockServicoHandler.Setup(h => h.ObterPorId(1)).Returns(new Servico { IdServico = 1, IdSalao = 1, Duracao = TimeSpan.FromMinutes(30), Nome = "Corte" });
            _mockPessoaHandler.Setup(h => h.ObterPorId(1)).Returns(new Pessoa { IdPessoa = 1, IdSalao = 1, Nome = "Joao" });
            
            _mockFsHandler.Setup(h => h.ListarFuncionariosDoServico(1)).Returns(new List<int> { 1 });
            _mockFuncionarioHandler.Setup(h => h.ObterPorId(1)).Returns(new Funcionario { 
                IdFuncionario = 1, 
                IdSalao = 1, 
                seg = true, seg_ini = TimeSpan.FromHours(8), seg_fim = TimeSpan.FromHours(18) 
            });
            
            _mockAgendamentoHandler.Setup(h => h.VerificarDisponibilidade(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .Returns(true);

            _mockAgendamentoHandler.Setup(h => h.CadastrarAgendamento(It.IsAny<Agendamento>()))
                .Returns(123);

            // Act
            var result = _pageModel.OnPostCreate(req);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            // Could check value.id == 123
            _mockAgendamentoHandler.Verify(h => h.CadastrarAgendamento(It.IsAny<Agendamento>()), Times.Once);
        }
    }

    public class FakeConfiguration : IConfiguration
    {
        public string? this[string key] { get => "mock-token"; set { } }
        public IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotImplementedException();
        public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
    }
}

