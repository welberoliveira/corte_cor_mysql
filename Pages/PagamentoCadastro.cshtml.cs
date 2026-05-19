using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PagamentoCadastroModel : PageModel
    {
        private readonly PagamentoHandler _pagamentoHandler;
        private readonly AgendamentoHandler _agendamentoHandler;
        private readonly ServicoHandler _servicoHandler;
        private readonly PessoaHandler _pessoaHandler;
        private readonly SalaoConfigFiscalHandler _salaoConfigFiscalHandler;
        private readonly NotaFiscalHandler _notaFiscalHandler;
        private readonly AgendamentoFiscalPreparationService _agendamentoFiscalPreparationService;
        private readonly PedidoHandler _pedidoHandler;
        private readonly VendaEstoqueHandler _vendaEstoqueHandler;
        private readonly FinanceiroService _financeiroService;
        private readonly ILogger<PagamentoCadastroModel> _logger;

        public PagamentoCadastroModel(
            PagamentoHandler pagamentoHandler,
            AgendamentoHandler agendamentoHandler,
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            SalaoConfigFiscalHandler salaoConfigFiscalHandler,
            NotaFiscalHandler notaFiscalHandler,
            AgendamentoFiscalPreparationService agendamentoFiscalPreparationService,
            PedidoHandler pedidoHandler,
            VendaEstoqueHandler vendaEstoqueHandler,
            FinanceiroService financeiroService,
            ILogger<PagamentoCadastroModel> logger)
        {
            _pagamentoHandler = pagamentoHandler;
            _agendamentoHandler = agendamentoHandler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _salaoConfigFiscalHandler = salaoConfigFiscalHandler;
            _notaFiscalHandler = notaFiscalHandler;
            _agendamentoFiscalPreparationService = agendamentoFiscalPreparationService;
            _pedidoHandler = pedidoHandler;
            _vendaEstoqueHandler = vendaEstoqueHandler;
            _financeiroService = financeiroService;
            _logger = logger;
        }

        public Pagamento Pagamento { get; set; } = new();
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; } = string.Empty;

        public void OnGet(Guid? id, int? idAgendamento, int? idPedido, int? idVendaProduto)
        {
            int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao);
            Pagamento = new Pagamento
            {
                Data = DateTime.Now,
                IdSalao = idSalao > 0 ? idSalao : null,
                OrigemPagamento = OrigemPagamento.Avulso
            };

            if (id.HasValue && id.Value != Guid.Empty)
            {
                Pagamento = _pagamentoHandler.ObterPorId(id.Value);
                ButtonText = "Atualizar";

                if (idAgendamento.HasValue && idAgendamento.Value > 0 && Pagamento != null)
                {
                    Pagamento.IdAgendamento = idAgendamento.Value;
                }

                return;
            }

            if (idPedido.HasValue && idPedido.Value > 0)
            {
                Pagamento.IdPedido = idPedido.Value;
                Pagamento.OrigemPagamento = OrigemPagamento.Pedido;
                Pagamento.Descricao = $"Pagamento ref. pedido {idPedido.Value}";
                Pagamento.Contos = Pagamento.Descricao;
            }

            if (idVendaProduto.HasValue && idVendaProduto.Value > 0)
            {
                Pagamento.IdVendaProduto = idVendaProduto.Value;
                Pagamento.OrigemPagamento = OrigemPagamento.Venda;
                Pagamento.Descricao = $"Pagamento ref. venda {idVendaProduto.Value}";
                Pagamento.Contos = Pagamento.Descricao;
            }

            if (!idAgendamento.HasValue || idAgendamento.Value <= 0)
            {
                return;
            }

            var agendamento = _agendamentoHandler.ObterPorId(idAgendamento.Value);
            if (agendamento == null)
            {
                return;
            }

            var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
            var pessoa = _pessoaHandler.ObterPorId(agendamento.IdPessoa);

            Pagamento = new Pagamento
            {
                IdSalao = idSalao > 0 ? idSalao : null,
                IdAgendamento = idAgendamento.Value,
                OrigemPagamento = OrigemPagamento.Agendamento,
                Valor = servico?.Preco ?? 0,
                Descricao = servico != null ? $"Pagamento ref. serviço {servico.Nome}" : string.Empty,
                Contos = servico != null ? $"Pagamento ref. serviço {servico.Nome}" : string.Empty,
                Data = DateTime.Now,
                NomeCliente = pessoa?.Nome,
                NomeServico = servico?.Nome
            };
        }

        public JsonResult OnGetDadosAgendamento(int id)
        {
            var agendamento = _agendamentoHandler.ObterPorId(id);
            if (agendamento != null)
            {
                var servico = _servicoHandler.ObterPorId(agendamento.IdServico);
                if (servico != null)
                {
                    return new JsonResult(new
                    {
                        success = true,
                        valor = servico.Preco,
                        descricao = $"Pagamento ref. serviço {servico.Nome}"
                    });
                }
            }

            return new JsonResult(new { success = false });
        }

        private static decimal ParseDecimalBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return 0m;
            }

            valor = valor.Trim();
            if (decimal.TryParse(valor, NumberStyles.Currency, new CultureInfo("pt-BR"), out var valorPtBr))
            {
                return valorPtBr;
            }

            var normalizado = Regex.Replace(valor, @"[^\d,.-]", string.Empty)
                .Replace(".", string.Empty)
                .Replace(",", ".");
            return decimal.Parse(normalizado, CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDateTimeLocal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return DateTime.Now;
            }

            return DateTime.Parse(valor);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Guid.TryParse(Request.Form["id"], out var id);
                var idAgendamento = LerIdPositivo("idAgendamento");
                var idPedido = LerIdPositivo("idPedido");
                var idVendaProduto = LerIdPositivo("idVendaProduto");
                int.TryParse(Request.Form["idMeioPagamento"], out var idMeioPagamento);

                var pagoEmInformado = string.IsNullOrWhiteSpace(Request.Form["pagoEm"])
                    ? (DateTime?)null
                    : DateTime.Parse(Request.Form["pagoEm"]);

                int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao);

                var pagamento = new Pagamento
                {
                    IdPagamento = id == Guid.Empty ? Guid.NewGuid() : id,
                    IdSalao = idSalao > 0 ? idSalao : null,
                    IdAgendamento = idAgendamento,
                    IdPedido = idPedido,
                    IdVendaProduto = idVendaProduto,
                    OrigemPagamento = ObterOrigemPagamento(idAgendamento, idPedido, idVendaProduto),
                    IdMeioPagamento = idMeioPagamento,
                    Tipo = Request.Form["tipo"],
                    Valor = ParseDecimalBR(Request.Form["valor"]),
                    Data = ParseDateTimeLocal(Request.Form["data"]),
                    PagoEm = pagoEmInformado ?? DateTime.Now,
                    Contos = Request.Form["contos"],
                    Campos = Request.Form["campos"],
                    Descricao = Request.Form["contos"],
                    Ativo = true,
                    Status = AgendamentoStatus.Pago,
                    Moeda = "BRL",
                    CriadoEm = DateTime.UtcNow
                };

                await ValidarOrigemAsync(pagamento, idSalao);

                if (id != Guid.Empty)
                {
                    _pagamentoHandler.AtualizarPagamento(pagamento, idSalao);
                    Mensagem = "Pagamento atualizado com sucesso!";
                }
                else
                {
                    _pagamentoHandler.CadastrarPagamento(pagamento);
                    id = pagamento.IdPagamento;
                    Mensagem = "Pagamento cadastrado com sucesso!";
                }

                if (pagamento.Status == AgendamentoStatus.Pago && idAgendamento.HasValue && idSalao > 0)
                {
                    _agendamentoHandler.AtualizarStatus(idAgendamento.Value, AgendamentoStatus.Pago, idSalao);
                }

                if (pagamento.Status == AgendamentoStatus.Pago && idAgendamento.HasValue)
                {
                    await ProcessarEmissaoAutomaticaAsync(idAgendamento.Value);
                }

                if (idSalao > 0)
                {
                    await _financeiroService.SincronizarTitulosPagamentoAsync(idSalao);
                }

                OnGet(id != Guid.Empty ? id : null, null, null, null);
            }
            catch (InvalidOperationException ex)
            {
                Mensagem = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar pagamento.");
                Mensagem = "Não foi possível salvar o pagamento. Verifique os dados informados e tente novamente.";
            }

            return Page();
        }

        private int? LerIdPositivo(string campo)
        {
            return int.TryParse(Request.Form[campo], out var id) && id > 0 ? id : null;
        }

        private static string ObterOrigemPagamento(int? idAgendamento, int? idPedido, int? idVendaProduto)
        {
            if (idAgendamento.HasValue) return OrigemPagamento.Agendamento;
            if (idPedido.HasValue) return OrigemPagamento.Pedido;
            if (idVendaProduto.HasValue) return OrigemPagamento.Venda;
            return OrigemPagamento.Avulso;
        }

        private async Task ValidarOrigemAsync(Pagamento pagamento, int idSalao)
        {
            if (idSalao <= 0)
            {
                throw new InvalidOperationException("Não foi possível identificar a empresa do pagamento.");
            }

            if (pagamento.OrigemPagamento == OrigemPagamento.Agendamento)
            {
                if (!pagamento.IdAgendamento.HasValue || _agendamentoHandler.ObterPorId(pagamento.IdAgendamento.Value) == null)
                {
                    throw new InvalidOperationException("Selecione um agendamento válido para este pagamento.");
                }
            }
            else if (pagamento.OrigemPagamento == OrigemPagamento.Pedido)
            {
                if (!pagamento.IdPedido.HasValue || await _pedidoHandler.ObterPedidoAsync(idSalao, pagamento.IdPedido.Value) == null)
                {
                    throw new InvalidOperationException("Selecione um pedido válido para este pagamento.");
                }
            }
            else if (pagamento.OrigemPagamento == OrigemPagamento.Venda)
            {
                if (!pagamento.IdVendaProduto.HasValue || await _vendaEstoqueHandler.ObterVendaAsync(idSalao, pagamento.IdVendaProduto.Value) == null)
                {
                    throw new InvalidOperationException("Selecione uma venda válida para este pagamento.");
                }
            }
        }

        private async Task ProcessarEmissaoAutomaticaAsync(int idAgendamento)
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (!int.TryParse(salaoIdStr, out var idSalaoConfig))
            {
                return;
            }

            try
            {
                var config = await _salaoConfigFiscalHandler.ObterPorSalaoAsync(idSalaoConfig);
                if (config == null || !config.EmissaoAutomatica)
                {
                    return;
                }

                var notasExistentes = await _notaFiscalHandler.ListarPorSalaoAsync(idSalaoConfig);
                var jaExiste = notasExistentes.Any(n =>
                    n.IdAgendamento == idAgendamento &&
                    n.Status != NotaFiscalStatus.Cancelada &&
                    n.Status != NotaFiscalStatus.Rejeitada);

                if (jaExiste)
                {
                    Mensagem += " (Ja existe nota fiscal ativa vinculada a este agendamento.)";
                    return;
                }

                var usuario = User.Identity?.Name ?? User.FindFirst("Email")?.Value;
                var resultado = await _agendamentoFiscalPreparationService.EmitirNotaServicoAsync(
                    idSalaoConfig,
                    idAgendamento,
                    usuario,
                    "Automatica");

                Mensagem += resultado.NotaFiscal?.Status == NotaFiscalStatus.Autorizada
                    ? " (Nota fiscal emitida automaticamente!)"
                    : $" (Emissao fiscal automatica processada: {resultado.NotaFiscal?.Status ?? "sem status"}.)";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar emissão fiscal automática: {ex.Message}");
                Mensagem += $" (Pagamento confirmado, mas a emissão fiscal automática falhou: {ex.Message})";
            }
        }
    }
}
