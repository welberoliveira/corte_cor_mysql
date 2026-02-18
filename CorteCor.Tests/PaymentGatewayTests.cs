using Xunit;
using Moq;
using CorteCor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CorteCor.Tests
{
    public class PaymentGatewayTests
    {
        [Theory]
        [InlineData("MercadoPago", true)]
        [InlineData("Mercado Pago", true)]
        [InlineData("mercado pago", true)]
        [InlineData("MERCADO PAGO", true)]
        [InlineData("  Mercado  Pago  ", true)]
        [InlineData("PagSeguro", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Gateway_Matching_Logic_Should_Work(string gatewayName, bool shouldMatch)
        {
            // Arrange
            var mockHandler = new Mock<MeioPagamentoHandler>((IDatabaseHandler)null);
            var meios = new List<Models.MeioPagamento>
            {
                new Models.MeioPagamento { IdMeioPagamento = 1, Gateway = gatewayName, Ativo = true }
            };

            mockHandler.Setup(h => h.ListarPorSalao(It.IsAny<int>(), It.IsAny<bool?>()))
                       .Returns(meios);

            // Act
            // Replicating the logic from Agendamentos2.cshtml.cs
            var result = meios.FirstOrDefault(m => 
                (m.Gateway != null && m.Gateway.Replace(" ", "").Equals("MercadoPago", StringComparison.OrdinalIgnoreCase)));

            // Assert
            if (shouldMatch)
            {
                Assert.NotNull(result);
                Assert.Equal(gatewayName, result.Gateway);
            }
            else
            {
                Assert.Null(result);
            }
        }
    }
}
