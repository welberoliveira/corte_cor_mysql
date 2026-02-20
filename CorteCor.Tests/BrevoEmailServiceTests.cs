using CorteCor.Models;
using CorteCor.Handlers;
using Xunit;
using Moq;
using Moq.Protected;
using CorteCor;
using CorteCor.Services;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System;
using System.Collections.Generic;

namespace CorteCor.Tests
{
    public class BrevoEmailServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ModeloEmailHandler> _mockModeloHandler;
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfiguration;
        private readonly BrevoEmailService _service;

        public BrevoEmailServiceTests()
        {
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            
            _mockModeloHandler = new Mock<ModeloEmailHandler>((IDatabaseHandler)null);
            _mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _mockConfiguration.Setup(c => c["Brevo:ApiKeyEmail"]).Returns("test-key-email");
            _mockConfiguration.Setup(c => c["Brevo:ApiKeySMS"]).Returns("test-key-sms");

            var mockDbHandler = new Mock<IDatabaseHandler>();
            var mockFornecedoresHandler = new Mock<FornecedoresHandler>(mockDbHandler.Object);
            mockFornecedoresHandler.Setup(f => f.ObterEmailAtivo()).Returns(new FornecedorEmail { Nome = "Brevo", ApiKey = "test-key-email", Ativo = true });

            _service = new BrevoEmailService(httpClient, _mockModeloHandler.Object, mockFornecedoresHandler.Object);
        }

        [Fact]
        public async Task EnviarEmailTemplateAsync_Success_ReturnsTrue()
        {
            // Arrange
            var modelo = new ModeloEmail
            {
                IdModelo = 1,
                IdSalao = 1,
                TipoEvento = "BoasVindas",
                Assunto = "Bem-vindo {NomeCliente}",
                CorpoHTML = "Olá {NomeCliente}, seu agendamento é {Data}",
                Ativo = true
            };

            _mockModeloHandler.Setup(h => h.ObterPorEvento(1, "BoasVindas")).Returns(modelo);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            var variaveis = new Dictionary<string, string>
            {
                { "NomeCliente", "Welber" },
                { "Data", "10/10/2026" }
            };

            // Act
            var result = await _service.EnviarEmailTemplateAsync(1, "BoasVindas", "test@test.com", variaveis);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
            _mockHttpMessageHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Post && 
                    req.Headers.Contains("api-key")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task EnviarEmailTemplateAsync_TemplateNotFound_ReturnsFalse()
        {
            // Arrange
            _mockModeloHandler.Setup(h => h.ObterPorEvento(1, "Inexistente")).Returns((ModeloEmail)null);

            // Act
            var result = await _service.EnviarEmailTemplateAsync(1, "Inexistente", "test@test.com", new Dictionary<string, string>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Template not found", result.ErrorMessage);
        }

        [Fact]
        public async Task EnviarEmailTemplateAsync_ApiError_ReturnsFalse()
        {
            // Arrange
            var modelo = new ModeloEmail { IdSalao = 1, TipoEvento = "Erro", Assunto = "X", CorpoHTML = "Y", Ativo = true };
            _mockModeloHandler.Setup(h => h.ObterPorEvento(1, "Erro")).Returns(modelo);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("{\"message\": \"Invalid API Key\"}")
                });

            // Act
            var result = await _service.EnviarEmailTemplateAsync(1, "Erro", "test@test.com", new Dictionary<string, string>());

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid API Key", result.ErrorMessage);
        }

        [Fact]
        public async Task EnviarEmailGenericoAsync_Success_ReturnsTrue()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });

            // Act
            var result = await _service.EnviarEmailGenericoAsync("test@test.com", "User", "Subject", "<p>Body</p>");

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);
        }
    }
}

