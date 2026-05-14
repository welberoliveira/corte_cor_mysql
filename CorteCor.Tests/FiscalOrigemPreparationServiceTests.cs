using System;
using System.Collections.Generic;
using CorteCor.Services;
using Xunit;

namespace CorteCor.Tests
{
    public class FiscalOrigemPreparationServiceTests
    {
        [Fact]
        public void PrepararAgendamento_DeveNormalizarOrigemServico()
        {
            var service = new FiscalOrigemPreparationService();
            var envelope = service.PrepararAgendamento(new FiscalOrigemAgendamentoPayload
            {
                IdSalao = 4,
                IdAgendamento = 123,
                DataHora = new DateTime(2026, 3, 18, 10, 0, 0),
                Cliente = new FiscalOrigemCliente
                {
                    IdPessoa = 10,
                    Nome = "Cliente Teste"
                },
                Itens = new List<FiscalOrigemItem>
                {
                    new FiscalOrigemItem
                    {
                        IdServico = 99,
                        Descricao = " Corte + Escova ",
                        Quantidade = 1,
                        ValorUnitario = 150
                    }
                }
            });

            Assert.Equal(FiscalOrigemTipo.Agendamento, envelope.Origem.Origem);
            Assert.Equal("AG-123", envelope.Origem.ReferenciaExterna);
            Assert.Equal("NFS-e", envelope.TipoNota);
            Assert.Equal("Prestacao de servico", envelope.NaturezaOperacao);
            Assert.Single(envelope.Itens);
            Assert.Equal("Corte + Escova", envelope.Itens[0].Descricao);
            Assert.Equal("010501", envelope.Itens[0].CodigoTributacaoMunicipio);
            Assert.Equal(5, envelope.Itens[0].AliquotaIss);
        }

        [Fact]
        public void PrepararVenda_DeveFalharSemItens()
        {
            var service = new FiscalOrigemPreparationService();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                service.PrepararVenda(new FiscalOrigemVendaPayload
                {
                    IdSalao = 4,
                    IdVendaProduto = 55,
                    Cliente = new FiscalOrigemCliente
                    {
                        Nome = "Cliente Teste"
                    }
                }));

            Assert.Contains("ao menos um item", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
