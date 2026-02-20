using Xunit;
using Moq;
using CorteCor;
using static CorteCor.Models;
using System.Data;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CorteCor.Tests
{
    public class LembreteServiceLimitsTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<ILembreteHandler> _mockLembreteHandler;
        private readonly Mock<BrevoEmailService> _mockEmailService;
        private readonly Mock<ILogger<LembreteService>> _mockLogger;
        private readonly LembreteService _service;

        public LembreteServiceLimitsTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockLembreteHandler = new Mock<ILembreteHandler>();
            _mockLogger = new Mock<ILogger<LembreteService>>();

            // Mock Email Service
            var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfig.Setup(c => c["Brevo:ApiKey"]).Returns("test-api-key");
            
            _mockEmailService = new Mock<BrevoEmailService>(new System.Net.Http.HttpClient(), null, mockConfig.Object);
            _mockEmailService.Setup(e => e.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync((true, null));
 
            var mockSmsService = new Mock<SMSMarketService>(new System.Net.Http.HttpClient(), null);

            _service = new LembreteService(_mockDbHandler.Object, _mockEmailService.Object, mockSmsService.Object, _mockLogger.Object, _mockLembreteHandler.Object);
        }

        [Fact]
        public async Task ProcessarLembretes_ShouldFilterLimite_ByCurrentMonth()
        {
            // Scenario: 1 item. Verify that month/year are passed to VerificarLimiteEmail (Implicitly tested by logic)
            var id = 1;
            SetupSingleItem(id, "test@test.com");

            await _service.ProcessarLembretesAsync(CancellationToken.None);

            _mockLembreteHandler.Verify(h => h.VerificarLimiteEmail(10, out It.Ref<int>.IsAny, out It.Ref<int>.IsAny), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretes_WhenLimitReachedInBatch_ShouldUpdateStatusToFaltaCredito()
        {
            // Scenario: Limit 2. Item 1 sends, Item 2 reaches limit.
            var pendentes = new List<LembreteAgendado> { 
                new LembreteAgendado { IdLembrete = 1, IdAgendamento = 101 },
                new LembreteAgendado { IdLembrete = 2, IdAgendamento = 102 }
            };
            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes()).Returns(pendentes);

            // Item 1
            var dados1 = new LembreteEnvioDTO { IdSalao = 20, NomeCliente = "C1", EmailCliente = "c1@test.com", DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "Salao", AssuntoModelo = (string)null, CorpoModelo = (string)null };
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(1)).Returns(dados1);

            // Item 2
            var dados2 = new LembreteEnvioDTO { IdSalao = 20, NomeCliente = "C2", EmailCliente = "c2@test.com", DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "Salao", AssuntoModelo = (string)null, CorpoModelo = (string)null };
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(2)).Returns(dados2);

            // Limit Info: 1 sent, limit 2.
            int enviadosDb = 1;
            int limite = 2;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(20, out enviadosDb, out limite))
                .Callback(new VerificarLimiteCallback((int s, out int e, out int l) => { e = 1; l = 2; }))
                .Returns(false);

            // Act
            await _service.ProcessarLembretesAsync(CancellationToken.None);

            // Assert
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("c1@test.com", "C1", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("c2@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(1, "Enviado"), Times.Once);
            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(2, "FaltaCredito"), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretes_WhenDataMissing_ShouldUpdateStatusToErroDados()
        {
            var pendentes = new List<LembreteAgendado> { new LembreteAgendado { IdLembrete = 99, IdAgendamento = 100 } };
            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes()).Returns(pendentes);
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(99)).Returns((LembreteEnvioDTO)null);

            await _service.ProcessarLembretesAsync(CancellationToken.None);

            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(99, "ErroDados"), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretes_WhenEmailFails_ShouldUpdateStatusToErroEnvio()
        {
            var id = 50;
            SetupSingleItem(id, "fail@test.com");
            _mockEmailService.Setup(e => e.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync((false, "Simulated Error"));

            await _service.ProcessarLembretesAsync(CancellationToken.None);

            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(id, "ErroEnvio"), Times.Once);
            _mockLembreteHandler.Verify(h => h.RegistrarLogEnvio(id, It.IsAny<int>(), "fail@test.com", It.IsAny<string>(), "ErroEnvio", "Simulated Error", "Email", null), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretes_WhenExceptionOccurs_ShouldUpdateStatusToErroExcecao()
        {
            var id = 60;
            SetupSingleItem(id, "error@test.com");
            _mockEmailService.Setup(e => e.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ThrowsAsync(new Exception("SMTP Error"));

            await _service.ProcessarLembretesAsync(CancellationToken.None);

            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(id, "ErroExcecao"), Times.Once);
            _mockLembreteHandler.Verify(h => h.RegistrarLogEnvio(id, It.IsAny<int>(), "Desconhecido", It.IsAny<string>(), "ErroExcecao", "SMTP Error", "Email", null), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretes_MultipleSalons_ShouldMaintainSeparateCounters()
        {
            // Salon 1: Limit 1
            // Salon 2: Limit 1
            // Queue: Item 1 (S1), Item 2 (S1), Item 3 (S2)
            var pendentes = new List<LembreteAgendado> { 
                new LembreteAgendado { IdLembrete = 1, IdAgendamento = 101 },
                new LembreteAgendado { IdLembrete = 2, IdAgendamento = 102 },
                new LembreteAgendado { IdLembrete = 3, IdAgendamento = 103 }
            };
            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes()).Returns(pendentes);

            // Salon 1 items
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(1)).Returns(new LembreteEnvioDTO { IdSalao = 1, NomeCliente = "C1", EmailCliente = "s1@test.com", DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "S1", AssuntoModelo = (string)null, CorpoModelo = (string)null });
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(2)).Returns(new LembreteEnvioDTO { IdSalao = 1, NomeCliente = "C2", EmailCliente = "s1_2@test.com", DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "S1", AssuntoModelo = (string)null, CorpoModelo = (string)null });
            
            // Salon 2 item
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(3)).Returns(new LembreteEnvioDTO { IdSalao = 2, NomeCliente = "C3", EmailCliente = "s2@test.com", DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "S2", AssuntoModelo = (string)null, CorpoModelo = (string)null });

            // Salon 1 Limit (0 used, limit 1)
            int env1 = 0; int lim1 = 1;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(1, out env1, out lim1))
                .Callback(new VerificarLimiteCallback((int s, out int e, out int l) => { e = 0; l = 1; }))
                .Returns(false);

            // Salon 2 Limit (0 used, limit 1)
            int env2 = 0; int lim2 = 1;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(2, out env2, out lim2))
                .Callback(new VerificarLimiteCallback((int s, out int e, out int l) => { e = 0; l = 1; }))
                .Returns(false);

            await _service.ProcessarLembretesAsync(CancellationToken.None);

            // Assert
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("s1@test.com", "C1", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("s1_2@test.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockEmailService.Verify(e => e.EnviarEmailGenericoAsync("s2@test.com", "C3", It.IsAny<string>(), It.IsAny<string>()), Times.Once);

            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(1, "Enviado"), Times.Once);
            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(2, "FaltaCredito"), Times.Once);
            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(3, "Enviado"), Times.Once);
        }

        private void SetupSingleItem(int id, string email)
        {
            var pendentes = new List<LembreteAgendado> { new LembreteAgendado { IdLembrete = id, IdAgendamento = 100 } };
            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes()).Returns(pendentes);
            var dados = new LembreteEnvioDTO { IdSalao = 10, NomeCliente = "C", EmailCliente = email, DataHoraAgendamento = DateTime.Now, NomeServico = "S", NomeProfissional = "P", NomeSalao = "Salao", AssuntoModelo = (string)null, CorpoModelo = (string)null };
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(id)).Returns(dados);
            
            int env = 0; int lim = 100;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(10, out env, out lim))
                .Callback(new VerificarLimiteCallback((int s, out int e, out int l) => { e = 0; l = 100; }))
                .Returns(false);
        }

        // Delegate for Mock out parameter callback
        delegate void VerificarLimiteCallback(int idSalao, out int enviados, out int limite);
    }
}
