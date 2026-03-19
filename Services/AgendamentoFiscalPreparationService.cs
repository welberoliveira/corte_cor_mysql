using CorteCor.Handlers;
using CorteCor.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CorteCor.Services
{
    public class AgendamentoSituacaoFiscalResult
    {
        public int IdAgendamento { get; set; }
        public string StatusAgendamento { get; set; } = AgendamentoStatus.Agendado;
        public bool PossuiNota { get; set; }
        public bool PossuiNotaAtiva { get; set; }
        public bool PodeEmitir { get; set; }
        public bool PodeAbrirNota { get; set; }
        public string StatusFiscal { get; set; } = "Sem nota";
        public string ClasseStatusFiscal { get; set; } = "bg-secondary";
        public string Mensagem { get; set; } = "Agendamento sem nota fiscal vinculada.";
        public Guid? IdNotaFiscal { get; set; }
        public int? NumeroNota { get; set; }
        public int? SerieNota { get; set; }
        public string? TipoNota { get; set; }
        public string? ChaveFiscal { get; set; }
    }

    public class AgendamentoFiscalPreparationService
    {
        private readonly AgendamentoHandler _agendamentoHandler;
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly NotaFiscalHandler _notaFiscalHandler;
        private readonly NotaFiscalLogHandler _notaFiscalLogHandler;
        private readonly FiscalOrigemPreparationService _origemPreparationService;
        private readonly NotaFiscalAvulsaService _notaFiscalAvulsaService;

        public AgendamentoFiscalPreparationService(
            AgendamentoHandler agendamentoHandler,
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            NotaFiscalHandler notaFiscalHandler,
            NotaFiscalLogHandler notaFiscalLogHandler,
            FiscalOrigemPreparationService origemPreparationService,
            NotaFiscalAvulsaService notaFiscalAvulsaService)
        {
            _agendamentoHandler = agendamentoHandler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _notaFiscalHandler = notaFiscalHandler;
            _notaFiscalLogHandler = notaFiscalLogHandler;
            _origemPreparationService = origemPreparationService;
            _notaFiscalAvulsaService = notaFiscalAvulsaService;
        }

        public virtual async Task<FiscalOrigemEnvelope> PrepararEnvelopeAsync(int idSalao, int idAgendamento)
        {
            var agendamento = _agendamentoHandler.ObterPorId(idAgendamento)
                ?? throw new InvalidOperationException("Agendamento nao encontrado.");

            if (AgendamentoStatus.Normalizar(agendamento.Status) != AgendamentoStatus.Pago)
            {
                throw new InvalidOperationException("Somente agendamentos pagos podem emitir nota fiscal.");
            }

            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            if (servico == null || servico.IdSalao != idSalao)
            {
                throw new InvalidOperationException("Servico do agendamento nao encontrado para este salao.");
            }

            var cliente = _pessoaHandler.ObterPorId(agendamento.IdPessoa);
            if (cliente == null || cliente.IdSalao != idSalao)
            {
                throw new InvalidOperationException("Cliente do agendamento nao encontrado para este salao.");
            }

            var codigoTributacao = ObterCodigoTributacaoServico(servico);
            var codigoNbs = NormalizarSomenteDigitos(servico.CodNBS);

            return await Task.FromResult(_origemPreparationService.PrepararAgendamento(new FiscalOrigemAgendamentoPayload
            {
                IdSalao = idSalao,
                IdAgendamento = agendamento.IdAgendamento,
                DataHora = agendamento.DataHora,
                Cliente = new FiscalOrigemCliente
                {
                    IdPessoa = cliente.IdPessoa,
                    Nome = cliente.Nome,
                    CpfCnpj = cliente.CpfCnpj,
                    Email = cliente.Email,
                    Telefone = cliente.Telefone,
                    Logradouro = cliente.Logradouro,
                    Numero = cliente.Numero,
                    Bairro = cliente.Bairro,
                    Cep = cliente.Cep,
                    Cidade = cliente.Cidade,
                    UF = string.IsNullOrWhiteSpace(cliente.UF) ? "MG" : cliente.UF,
                    CodigoMunicipioIbge = InferirCodigoMunicipio(cliente.Cidade, cliente.UF, cliente.Cep)
                },
                Itens = new List<FiscalOrigemItem>
                {
                    new FiscalOrigemItem
                    {
                        IdServico = servico.IdServico,
                        Descricao = servico.Nome,
                        Quantidade = 1,
                        ValorUnitario = servico.Preco,
                        CodigoTributacaoMunicipio = codigoTributacao,
                        AliquotaIss = servico.AliquotaISS > 0 ? servico.AliquotaISS : 5,
                        Ncm = string.IsNullOrWhiteSpace(codigoNbs) ? servico.CodNBS : codigoNbs,
                        Cfop = "5933"
                    }
                },
                Observacoes = $"Agendamento {agendamento.IdAgendamento} emitido pelo nucleo fiscal compartilhado."
            }));
        }

        public virtual async Task<AgendamentoSituacaoFiscalResult> ObterSituacaoFiscalAsync(int idSalao, int idAgendamento, string? statusAgendamento = null)
        {
            var statusCanonico = string.IsNullOrWhiteSpace(statusAgendamento)
                ? AgendamentoStatus.Normalizar(_agendamentoHandler.ObterPorId(idAgendamento)?.Status)
                : AgendamentoStatus.Normalizar(statusAgendamento);

            var notas = await _notaFiscalHandler.ListarPorAgendamentoAsync(idSalao, idAgendamento);
            var notaAtiva = notas.FirstOrDefault(NotaEstaAtiva);
            var notaReferencia = notaAtiva ?? notas.FirstOrDefault();

            var result = new AgendamentoSituacaoFiscalResult
            {
                IdAgendamento = idAgendamento,
                StatusAgendamento = statusCanonico,
                PossuiNota = notaReferencia != null,
                PossuiNotaAtiva = notaAtiva != null,
                PodeEmitir = AgendamentoStatus.PodeEmitirNota(statusCanonico) && notaAtiva == null,
                PodeAbrirNota = notaReferencia != null
            };

            if (notaReferencia == null)
            {
                result.Mensagem = result.PodeEmitir
                    ? "Agendamento pago e pronto para emitir nota fiscal."
                    : "Agendamento ainda nao possui nota fiscal vinculada.";
                return result;
            }

            result.IdNotaFiscal = notaReferencia.IdNotaFiscal;
            result.NumeroNota = notaReferencia.Numero;
            result.SerieNota = notaReferencia.Serie;
            result.TipoNota = notaReferencia.TipoNota;
            result.StatusFiscal = notaReferencia.Status;
            result.ClasseStatusFiscal = NotaFiscalAvulsaService.ObterClasseStatus(notaReferencia.Status);
            result.ChaveFiscal = NotaFiscalAvulsaService.ObterChaveFiscal(notaReferencia);
            result.Mensagem = notaAtiva != null
                ? $"Ja existe {notaReferencia.TipoNota} {notaReferencia.Numero}/{notaReferencia.Serie} com status {notaReferencia.Status}."
                : $"Ultima nota vinculada: {notaReferencia.TipoNota} {notaReferencia.Numero}/{notaReferencia.Serie} com status {notaReferencia.Status}.";

            return result;
        }

        public virtual async Task<NotaFiscalAvulsaRequest> PrepararRequestAsync(int idSalao, int idAgendamento)
        {
            var envelope = await PrepararEnvelopeAsync(idSalao, idAgendamento);
            var contexto = await _notaFiscalAvulsaService.ObterContextoTelaAsync(idSalao, "NFSE", 0, 0, 0);

            return new NotaFiscalAvulsaRequest
            {
                Ambiente = contexto.Ambiente,
                Modelo = "NFSE",
                NaturezaOperacao = envelope.NaturezaOperacao,
                Serie = contexto.Serie,
                Numero = contexto.NumeroSugerido,
                DataEmissao = envelope.DataCompetencia,
                EmitenteCnpj = contexto.EmitenteCnpj,
                EmitenteNome = contexto.EmitenteNome,
                EmitenteIE = contexto.EmitenteIE,
                EmitenteIM = contexto.EmitenteIM,
                EmitenteCRT = contexto.EmitenteCRT,
                EmitenteLogradouro = contexto.EmitenteLogradouro,
                EmitenteNumero = contexto.EmitenteNumero,
                EmitenteBairro = contexto.EmitenteBairro,
                EmitenteCep = contexto.EmitenteCep,
                EmitenteCidade = contexto.EmitenteCidade,
                EmitenteUF = contexto.EmitenteUF,
                EmitenteCodMun = contexto.EmitenteCodMun,
                DestinatarioCpfCnpj = envelope.Cliente.CpfCnpj,
                DestinatarioNome = envelope.Cliente.Nome,
                DestinatarioLogradouro = envelope.Cliente.Logradouro,
                DestinatarioNumero = envelope.Cliente.Numero,
                DestinatarioBairro = envelope.Cliente.Bairro,
                DestinatarioCep = envelope.Cliente.Cep,
                DestinatarioCidade = envelope.Cliente.Cidade,
                DestinatarioUF = envelope.Cliente.UF,
                DestinatarioCodMun = envelope.Cliente.CodigoMunicipioIbge ?? contexto.EmitenteCodMun,
                DestinatarioEmail = envelope.Cliente.Email,
                Itens = envelope.Itens.Select((item, index) => new NotaFiscalAvulsaItemRequest
                {
                    CProd = (item.IdServico ?? (index + 1)).ToString(),
                    XProd = item.Descricao,
                    NCM = string.IsNullOrWhiteSpace(item.Ncm) ? "00" : item.Ncm!,
                    CFOP = string.IsNullOrWhiteSpace(item.Cfop) ? "5933" : item.Cfop!,
                    UCom = "UN",
                    QCom = item.Quantidade,
                    VUnCom = item.ValorUnitario,
                    VProd = decimal.Round(item.Quantidade * item.ValorUnitario, 2),
                    CodigoTributacao = item.CodigoTributacaoMunicipio,
                    AliquotaISS = item.AliquotaIss ?? 5
                }).ToList()
            };
        }

        public virtual async Task<NotaFiscalOperacaoResult> EmitirNotaServicoAsync(
            int idSalao,
            int idAgendamento,
            string? usuario = null,
            string origemAcionamento = "Manual")
        {
            var agendamento = _agendamentoHandler.ObterPorId(idAgendamento)
                ?? throw new InvalidOperationException("Agendamento nao encontrado.");

            var situacaoFiscal = await ObterSituacaoFiscalAsync(idSalao, idAgendamento, agendamento.Status);
            if (!situacaoFiscal.PodeEmitir)
            {
                throw new InvalidOperationException(situacaoFiscal.PossuiNotaAtiva
                    ? situacaoFiscal.Mensagem
                    : "Somente agendamentos pagos podem emitir nota fiscal.");
            }

            var origemNormalizada = string.IsNullOrWhiteSpace(origemAcionamento)
                ? "Manual"
                : origemAcionamento.Trim();

            await _notaFiscalLogHandler.LogarEtapaAsync(
                idSalao,
                idAgendamento,
                null,
                "PREPARACAO_AGENDAMENTO",
                $"Preparando emissao fiscal a partir do agendamento. Origem do disparo: {origemNormalizada}.",
                usuario: usuario);

            var request = await PrepararRequestAsync(idSalao, idAgendamento);
            var resultado = await _notaFiscalAvulsaService.EmitirAsync(idSalao, request, usuario);

            if (resultado.NotaFiscal != null)
            {
                resultado.NotaFiscal.IdAgendamento = idAgendamento;
                await _notaFiscalHandler.UpdateAsync(resultado.NotaFiscal);
            }

            await _notaFiscalLogHandler.LogarEtapaAsync(
                idSalao,
                idAgendamento,
                resultado.NotaFiscal?.IdNotaFiscal,
                "EMISSAO_AGENDAMENTO",
                $"{resultado.Mensagem} Origem do disparo: {origemNormalizada}.",
                usuario: usuario);

            return resultado;
        }

        private static bool NotaEstaAtiva(NotaFiscal nota)
        {
            return !string.Equals(nota.Status, NotaFiscalStatus.Cancelada, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(nota.Status, NotaFiscalStatus.Rejeitada, StringComparison.OrdinalIgnoreCase);
        }

        private static int InferirCodigoMunicipio(string? cidade, string? uf, string? cep)
        {
            if (!string.IsNullOrWhiteSpace(cidade) &&
                cidade.Trim().Equals("Montes Claros", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(uf) || uf.Trim().Equals("MG", StringComparison.OrdinalIgnoreCase)))
            {
                return 3143302;
            }

            if (!string.IsNullOrWhiteSpace(cep) && cep.Replace("-", "").StartsWith("394", StringComparison.Ordinal))
            {
                return 3143302;
            }

            return 3143302;
        }

        private static string ObterCodigoTributacaoServico(Servico servico)
        {
            var candidatos = new[]
            {
                servico.CodTributacaoNacional,
                servico.CodigoTributacaoMunicipio,
                servico.ItemListaServicoLC116
            };

            foreach (var candidato in candidatos)
            {
                var normalizado = NormalizarSomenteDigitos(candidato);
                if (!string.IsNullOrWhiteSpace(normalizado) && normalizado.Length >= 4)
                {
                    return normalizado;
                }
            }

            throw new InvalidOperationException(
                $"O servico '{servico.Nome}' nao possui codigo de tributacao fiscal valido. Revise a aba de informacoes fiscais do servico.");
        }

        private static string? NormalizarSomenteDigitos(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            var somenteDigitos = new string(valor.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(somenteDigitos) ? null : somenteDigitos;
        }
    }
}
