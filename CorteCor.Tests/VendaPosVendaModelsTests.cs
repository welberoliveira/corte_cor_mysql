using CorteCor.Models;

namespace CorteCor.Tests;

public class VendaPosVendaModelsTests
{
    [Fact]
    public void QuantidadeDisponivelPosVenda_DeveDescontarCanceladaDevolvidaETrocada()
    {
        var item = new VendaProdutoItem
        {
            Quantidade = 10m,
            QuantidadeCancelada = 2m,
            QuantidadeDevolvida = 1.5m,
            QuantidadeTrocada = 0.5m
        };

        Assert.Equal(6m, item.QuantidadeDisponivelPosVenda);
    }

    [Fact]
    public void QuantidadeDisponivelPosVenda_NaoDeveRetornarNegativo()
    {
        var item = new VendaProdutoItem
        {
            Quantidade = 2m,
            QuantidadeCancelada = 2m,
            QuantidadeDevolvida = 1m
        };

        Assert.Equal(0m, item.QuantidadeDisponivelPosVenda);
    }
}
