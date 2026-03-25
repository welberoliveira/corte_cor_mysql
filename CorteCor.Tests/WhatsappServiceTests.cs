using System.Net;
using System.Net.Http;
using System.Text.Json;
using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorteCor.Tests
{
    public class WhatsappServiceTests
    {
        [Fact]
        public async Task EnviarMensagemAsync_ZApi_DeveMontarUrlHeaderEPayload()
        {
            string? urlCapturada = null;
            string? headerCapturado = null;
            string? bodyCapturado = null;
            var httpClient = CriarHttpClient((request, _) =>
            {
                urlCapturada = request.RequestUri!.ToString();
                headerCapturado = request.Headers.GetValues("client-token").Single();
                bodyCapturado = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"zaapId\":\"1\"}")
                };
            });

            var dbHandler = new Mock<IDatabaseHandler>();
            var fornecedoresHandler = new Mock<FornecedoresHandler>(dbHandler.Object);
            fornecedoresHandler.Setup(h => h.ObterWhatsappAtivo()).Returns(new FornecedorWhatsapp
            {
                Nome = "Z-API",
                Endpoint = "https://api.z-api.io",
                InstanceId = "inst-1",
                Token = "token-instancia",
                ApiKey = "client-token-1"
            });

            var service = new WhatsappService(httpClient, fornecedoresHandler.Object, NullLogger<WhatsappService>.Instance);
            var resultado = await service.EnviarMensagemAsync("(31) 99888-7766", "Teste CRM");

            Assert.True(resultado.Success);
            Assert.Equal("https://api.z-api.io/instances/inst-1/token/token-instancia/send-text", urlCapturada);
            Assert.Equal("client-token-1", headerCapturado);
            Assert.Contains("\"phone\":\"5531998887766\"", bodyCapturado);
            Assert.Contains("\"message\":\"Teste CRM\"", bodyCapturado);
        }

        [Fact]
        public async Task EnviarMensagemAsync_Evolution_DeveMontarUrlHeaderEPayload()
        {
            string? urlCapturada = null;
            string? headerCapturado = null;
            string? bodyCapturado = null;
            var httpClient = CriarHttpClient((request, _) =>
            {
                urlCapturada = request.RequestUri!.ToString();
                headerCapturado = request.Headers.GetValues("apikey").Single();
                bodyCapturado = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{\"status\":\"PENDING\"}")
                };
            });

            var dbHandler = new Mock<IDatabaseHandler>();
            var fornecedoresHandler = new Mock<FornecedoresHandler>(dbHandler.Object);
            fornecedoresHandler.Setup(h => h.ObterWhatsappAtivo()).Returns(new FornecedorWhatsapp
            {
                Nome = "Evolution API",
                Endpoint = "https://evolution.exemplo.com",
                InstanceId = "inst-evo",
                ApiKey = "apikey-evo"
            });

            var service = new WhatsappService(httpClient, fornecedoresHandler.Object, NullLogger<WhatsappService>.Instance);
            var resultado = await service.EnviarMensagemAsync("31998887766", "Ola!");

            Assert.True(resultado.Success);
            Assert.Equal("https://evolution.exemplo.com/message/sendText/inst-evo", urlCapturada);
            Assert.Equal("apikey-evo", headerCapturado);
            using var document = JsonDocument.Parse(bodyCapturado!);
            Assert.Equal("5531998887766", document.RootElement.GetProperty("number").GetString());
            Assert.Equal("Ola!", document.RootElement.GetProperty("textMessage").GetProperty("text").GetString());
        }

        private static HttpClient CriarHttpClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        {
            return new HttpClient(new FakeHttpMessageHandler(handler));
        }

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _handler;

            public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_handler(request, cancellationToken));
            }
        }
    }
}
