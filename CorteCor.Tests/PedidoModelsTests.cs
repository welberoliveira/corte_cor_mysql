using CorteCor.Models;

namespace CorteCor.Tests;

public class PedidoModelsTests
{
    [Fact]
    public void Expirado_DeveSerTrue_QuandoPedidoAbertoEstaComValidadeNoPassado()
    {
        var pedido = new Pedido
        {
            Status = PedidoStatus.Aberto,
            ValidoAte = DateTime.Today.AddDays(-1)
        };

        Assert.True(pedido.Expirado);
    }

    [Fact]
    public void Expirado_DeveSerFalse_QuandoPedidoNaoEstaAberto()
    {
        var pedido = new Pedido
        {
            Status = PedidoStatus.Vencido,
            ValidoAte = DateTime.Today.AddDays(-3)
        };

        Assert.False(pedido.Expirado);
    }

    [Fact]
    public void PodeConverter_DevePermitirAbertoOuVencido_SemVendaVinculada()
    {
        var pedidoAberto = new Pedido
        {
            Status = PedidoStatus.Aberto,
            IdVendaProduto = null
        };

        var pedidoVencido = new Pedido
        {
            Status = PedidoStatus.Vencido,
            IdVendaProduto = null
        };

        Assert.True(pedidoAberto.PodeConverter);
        Assert.True(pedidoVencido.PodeConverter);
    }

    [Fact]
    public void PodeConverter_DeveSerFalse_QuandoPedidoJaFoiConvertidoOuCancelado()
    {
        var pedidoConvertido = new Pedido
        {
            Status = PedidoStatus.Convertido,
            IdVendaProduto = 10
        };

        var pedidoCancelado = new Pedido
        {
            Status = PedidoStatus.Cancelado,
            IdVendaProduto = null
        };

        Assert.False(pedidoConvertido.PodeConverter);
        Assert.False(pedidoCancelado.PodeConverter);
    }
}
