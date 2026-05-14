using CorteCor.Models;
using CorteCor.Handlers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using CorteCor;
using CorteCor.Services;

using System.Net.Http;

namespace CorteCor.Tests
{
    public class LembreteServiceSmsTests
    {
        private readonly Mock<IDatabaseHandler> _mockDbHandler;
        private readonly Mock<ILembreteHandler> _mockLembreteHandler;
        private readonly Mock<BrevoEmailService> _mockEmailService;
        private readonly Mock<SMSMarketService> _mockSmsService;
        private readonly Mock<FornecedoresHandler> _mockFornecedoresHandler;
        private readonly Mock<ILogger<LembreteService>> _mockLogger;
        private readonly LembreteService _service;

        public LembreteServiceSmsTests()
        {
            _mockDbHandler = new Mock<IDatabaseHandler>();
            _mockLembreteHandler = new Mock<ILembreteHandler>();
            
            var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            mockConfig.Setup(c => c["Brevo:ApiKey"]).Returns("fake-api-key");
            
            // Mock BrevoEmailService since it's a class, not an interface
            var mockDbH = new Mock<IDatabaseHandler>();
            var mockFH = new Mock<FornecedoresHandler>(mockDbH.Object);
            _mockEmailService = new Mock<BrevoEmailService>(new HttpClient(), null, mockFH.Object);
            _mockSmsService = new Mock<SMSMarketService>(new HttpClient(), mockFH.Object);
            _mockLogger = new Mock<ILogger<LembreteService>>();
            
            _mockFornecedoresHandler = new Mock<FornecedoresHandler>(_mockDbHandler.Object);
            _mockFornecedoresHandler.Setup(f => f.ObterEmailAtivo()).Returns(new FornecedorEmail { Nome = "Brevo", ApiKey = "fake-api-key" });
            _mockFornecedoresHandler.Setup(f => f.ObterSMSAtivo()).Returns(new FornecedorSMS { Nome = "SMSMarket", ApiKey = "fake-api-key" });

            _service = new LembreteService(
                _mockDbHandler.Object,
                _mockEmailService.Object,
                _mockSmsService.Object,
                _mockFornecedoresHandler.Object,
                _mockLogger.Object,
                _mockLembreteHandler.Object
            );
        }

        [Fact]
        public async Task ProcessarLembretesAsync_ShouldSendSms_WhenTipoLembreteIsSms()
        {
            // Arrange
            var lembrete = new LembreteAgendado { IdLembrete = 1, IdAgendamento = 100, IdConfig = 10 };
            var pendentes = new List<LembreteAgendado> { lembrete };

            var dadosEnvio = new LembreteEnvioDTO
            {
                IdSalao = 1,
                TipoLembrete = "SMS",
                TelefoneCliente = "5511999999999",
                EmailCliente = "test@example.com",
                CorpoModelo = "Olá {NomeCliente}, seu agendamento é {DataAgendamento}",
                NomeCliente = "João",
                DataHoraAgendamento = new DateTime(2023, 10, 20, 14, 0, 0),
                NomeServico = "Corte",
                NomeProfissional = "Maria",
                NomeSalao = "Salão Teste"
            };

            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes())
                                .Returns(pendentes);
            
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(lembrete.IdLembrete))
                                .Returns(dadosEnvio);

            // Mock verification of limit (always return plenty)
            int enviadosDb = 0;
            int limite = 100;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(1, out enviadosDb, out limite));

            _mockSmsService.Setup(s => s.EnviarSmsAsync(It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync((true, null));

            // Act
            int result = await _service.ProcessarLembretesAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1, result);
            
            // Verify SMS was called with correct number and processed content
            _mockSmsService.Verify(s => s.EnviarSmsAsync(
                "5511999999999", 
                It.Is<string>(c => c.Contains("João") && c.Contains("20/10/2023 14:00"))
            ), Times.Once);

            // Verify LogEnvio was called with "SMS" type
            _mockLembreteHandler.Verify(h => h.RegistrarLogEnvio(
                lembrete.IdLembrete, 
                (int)lembrete.IdAgendamento, 
                "5511999999999", 
                "SMS", 
                "Sucesso", 
                null, 
                "SMS", 
                "5511999999999"
            ), Times.Once);

            // Verify Status update
            _mockLembreteHandler.Verify(h => h.AtualizarStatusLembrete(lembrete.IdLembrete, "Enviado"), Times.Once);
        }

        [Fact]
        public async Task ProcessarLembretesAsync_ShouldSendEmail_WhenTipoLembreteIsEmail()
        {
            // Arrange
            var lembrete = new LembreteAgendado { IdLembrete = 2, IdAgendamento = 101, IdConfig = 11 };
            var pendentes = new List<LembreteAgendado> { lembrete };

            var dadosEnvio = new LembreteEnvioDTO
            {
                IdSalao = 1,
                TipoLembrete = "Email",
                EmailCliente = "test@example.com",
                TelefoneCliente = "5511999999999",
                AssuntoModelo = "Lembrete",
                CorpoModelo = "Olá {NomeCliente}",
                NomeCliente = "Ana",
                DataHoraAgendamento = DateTime.Now,
                NomeServico = "Manicure",
                NomeProfissional = "Bia",
                NomeSalao = "Salão Teste"
            };

            _mockLembreteHandler.Setup(h => h.ObterLembretesPendentes())
                                .Returns(pendentes);
            
            _mockLembreteHandler.Setup(h => h.ObterDadosEnvio(lembrete.IdLembrete))
                                .Returns(dadosEnvio);

            int enviadosDb = 0;
            int limite = 100;
            _mockLembreteHandler.Setup(h => h.VerificarLimiteEmail(1, out enviadosDb, out limite));

            _mockEmailService.Setup(s => s.EnviarEmailGenericoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync((true, null));

            // Act
            await _service.ProcessarLembretesAsync(CancellationToken.None);

            // Assert
            // Verify SMS was NOT called
            _mockSmsService.Verify(s => s.EnviarSmsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // Verify Email was called
            _mockEmailService.Verify(s => s.EnviarEmailGenericoAsync(
                "test@example.com", "Ana", "Lembrete", It.Is<string>(c => c.Contains("Ana"))
            ), Times.Once);
        }
    }
}

