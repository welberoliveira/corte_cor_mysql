using Xunit;
using Moq;
using Moq.Protected;
using CorteCor;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System;
using System.Collections.Generic;

namespace CorteCor.Tests
{
    public class MercadoPagoServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly MercadoPagoService _service;

        public MercadoPagoServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["MercadoPago:AccessToken"]).Returns("TEST_TOKEN");

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.mercadopago.com/")
            };

            _service = new MercadoPagoService(_mockConfig.Object, httpClient);
        }

        [Fact]
        public async Task CreatePreferenceAsync_Success_ReturnsPreference()
        {
            // Arrange
            var responseJson = "{\"id\": \"pref_123\", \"init_point\": \"http://init\", \"sandbox_init_point\": \"http://sandbox\"}";
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _service.CreatePreferenceAsync(Guid.NewGuid(), "Titulo", 10.0m, "email@test.com", "http://base");

            // Assert
            Assert.NotNull(result.preference);
            Assert.Null(result.errorMessage);
            Assert.Equal("pref_123", result.preference.Id);
        }

        [Fact]
        public async Task CreatePreferenceAsync_Failure_ReturnsError()
        {
            // Arrange
            var errorJson = "{\"message\": \"Error creating preference\"}";
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(errorJson)
                });

            // Act
            var result = await _service.CreatePreferenceAsync(Guid.NewGuid(), "Titulo", 10.0m, "email@test.com", "http://base");

            // Assert
            Assert.Null(result.preference);
            Assert.NotNull(result.errorMessage);
            Assert.Contains("Error creating preference", result.errorMessage);
        }

        [Fact]
        public async Task GetPaymentDetailsAsync_Success_ReturnsPayment()
        {
            // Arrange
            var responseJson = "{\"id\": 12345, \"status\": \"approved\", \"status_detail\": \"accredited\", \"external_reference\": \"ref_123\"}";
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri.ToString().Contains("/v1/payments/12345")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                });

            // Act
            var result = await _service.GetPaymentDetailsAsync("12345");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(12345, result.Id);
            Assert.Equal("approved", result.Status);
        }

        [Fact]
        public async Task GetPaymentDetailsAsync_NotFound_ReturnsNull()
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
                    StatusCode = HttpStatusCode.NotFound
                });

            // Act
            var result = await _service.GetPaymentDetailsAsync("99999");

            // Assert
            Assert.Null(result);
        }
    }
}
