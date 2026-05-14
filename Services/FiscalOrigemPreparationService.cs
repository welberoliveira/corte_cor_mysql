using System;
using System.Linq;

namespace CorteCor.Services
{
    public class FiscalOrigemPreparationService
    {
        public FiscalOrigemEnvelope PrepararAgendamento(FiscalOrigemAgendamentoPayload payload)
        {
            ValidarPayload(payload.IdSalao, payload.Itens.Count, payload.Cliente.Nome, "agendamento");

            return new FiscalOrigemEnvelope
            {
                Origem = new FiscalOrigemRequest
                {
                    Origem = FiscalOrigemTipo.Agendamento,
                    IdOrigem = new Guid($"00000000-0000-0000-0000-{payload.IdAgendamento:D12}"),
                    IdSalao = payload.IdSalao,
                    ReferenciaExterna = $"AG-{payload.IdAgendamento}"
                },
                TipoNota = "NFS-e",
                NaturezaOperacao = "Prestacao de servico",
                DataCompetencia = payload.DataHora,
                Cliente = payload.Cliente,
                Itens = payload.Itens
                    .Select(NormalizarItemServico)
                    .ToList(),
                Observacoes = payload.Observacoes
            };
        }

        public FiscalOrigemEnvelope PrepararVenda(FiscalOrigemVendaPayload payload)
        {
            ValidarPayload(payload.IdSalao, payload.Itens.Count, payload.Cliente.Nome, "venda");

            return new FiscalOrigemEnvelope
            {
                Origem = new FiscalOrigemRequest
                {
                    Origem = FiscalOrigemTipo.Venda,
                    IdOrigem = new Guid($"00000000-0000-0000-0000-{payload.IdVendaProduto:D12}"),
                    IdSalao = payload.IdSalao,
                    ReferenciaExterna = $"VD-{payload.IdVendaProduto}"
                },
                TipoNota = "NFS-e",
                NaturezaOperacao = "Prestacao de servico",
                DataCompetencia = payload.DataVenda,
                Cliente = payload.Cliente,
                Itens = payload.Itens
                    .Select(NormalizarItemServico)
                    .ToList(),
                Observacoes = payload.Observacoes
            };
        }

        private static FiscalOrigemItem NormalizarItemServico(FiscalOrigemItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Descricao))
            {
                throw new InvalidOperationException("Todo item fiscal de origem deve possuir descrição.");
            }

            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException("Todo item fiscal de origem deve possuir quantidade maior que zero.");
            }

            if (item.ValorUnitario < 0)
            {
                throw new InvalidOperationException("Todo item fiscal de origem deve possuir valor unitário não negativo.");
            }

            return new FiscalOrigemItem
            {
                IdServico = item.IdServico,
                IdProduto = item.IdProduto,
                Descricao = item.Descricao.Trim(),
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
                CodigoTributacaoMunicipio = string.IsNullOrWhiteSpace(item.CodigoTributacaoMunicipio) ? "010501" : item.CodigoTributacaoMunicipio.Trim(),
                AliquotaIss = item.AliquotaIss ?? 5,
                Ncm = string.IsNullOrWhiteSpace(item.Ncm) ? "00" : item.Ncm.Trim(),
                Cfop = string.IsNullOrWhiteSpace(item.Cfop) ? "5102" : item.Cfop.Trim()
            };
        }

        private static void ValidarPayload(int idSalao, int quantidadeItens, string? nomeCliente, string origem)
        {
            if (idSalao <= 0)
            {
                throw new InvalidOperationException($"A origem fiscal de {origem} precisa informar um IdSalao válido.");
            }

            if (quantidadeItens <= 0)
            {
                throw new InvalidOperationException($"A origem fiscal de {origem} precisa informar ao menos um item.");
            }

            if (string.IsNullOrWhiteSpace(nomeCliente))
            {
                throw new InvalidOperationException($"A origem fiscal de {origem} precisa informar o nome do cliente.");
            }
        }
    }
}
