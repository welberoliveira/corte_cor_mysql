using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

        public PagamentoCadastroModel(
            PagamentoHandler pagamentoHandler,
            AgendamentoHandler agendamentoHandler,
            ServicoHandler servicoHandler,
            PessoaHandler pessoaHandler,
            SalaoConfigFiscalHandler salaoConfigFiscalHandler,
            NotaFiscalHandler notaFiscalHandler,
            AgendamentoFiscalPreparationService agendamentoFiscalPreparationService)
        {
            _pagamentoHandler = pagamentoHandler;
            _agendamentoHandler = agendamentoHandler;
            _servicoHandler = servicoHandler;
            _pessoaHandler = pessoaHandler;
            _salaoConfigFiscalHandler = salaoConfigFiscalHandler;
            _notaFiscalHandler = notaFiscalHandler;
            _agendamentoFiscalPreparationService = agendamentoFiscalPreparationService;
        }

        public Pagamento Pagamento { get; set; } = new();
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; } = string.Empty;

        public void OnGet(Guid? id, int? idAgendamento)
        {
            Pagamento = new Pagamento { Data = DateTime.Now };

            if (id.HasValue && id.Value != Guid.Empty)
            {
                Pagamento = _pagamentoHandler.ObterPorId(id.Value);
                ButtonText = "Atualizar";

                if (idAgendamento.HasValue && Pagamento != null)
                {
                    Pagamento.IdAgendamento = idAgendamento.Value;
                }

                return;
            }

            if (!idAgendamento.HasValue)
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
                IdAgendamento = idAgendamento.Value,
                Valor = servico?.Preco ?? 0,
                Descricao = servico != null ? $"Pagamento Ref. Servico {servico.Nome}" : string.Empty,
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
                        descricao = $"Pagamento Ref. Servico {servico.Nome}"
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

            valor = valor.Trim().Replace(".", string.Empty).Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
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
            Guid.TryParse(Request.Form["id"], out var id);
            int.TryParse(Request.Form["idAgendamento"], out var idAgendamento);
            int.TryParse(Request.Form["idMeioPagamento"], out var idMeioPagamento);

            var pagamento = new Pagamento
            {
                IdPagamento = id == Guid.Empty ? Guid.NewGuid() : id,
                IdAgendamento = idAgendamento,
                IdMeioPagamento = idMeioPagamento,
                Tipo = Request.Form["tipo"],
                Valor = ParseDecimalBR(Request.Form["valor"]),
                Data = ParseDateTimeLocal(Request.Form["data"]),
                PagoEm = string.IsNullOrWhiteSpace(Request.Form["pagoEm"]) ? null : DateTime.Parse(Request.Form["pagoEm"]),
                Contos = Request.Form["contos"],
                Campos = Request.Form["campos"],
                Ativo = true,
                Status = AgendamentoStatus.Pago,
                Moeda = "BRL",
                CriadoEm = DateTime.UtcNow
            };

            int.TryParse(User.FindFirst("IdSalao")?.Value, out var idSalao);

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

            if (pagamento.Status == AgendamentoStatus.Pago && idAgendamento > 0 && idSalao > 0)
            {
                _agendamentoHandler.AtualizarStatus(idAgendamento, AgendamentoStatus.Pago, idSalao);
            }

            if (pagamento.Status == AgendamentoStatus.Pago && idAgendamento > 0)
            {
                await ProcessarEmissaoAutomaticaAsync(idAgendamento);
            }

            OnGet(id != Guid.Empty ? id : null, null);
            return Page();
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
                Console.WriteLine($"Erro ao gerar emissao fiscal automatica: {ex.Message}");
                Mensagem += $" (Pagamento confirmado, mas a emissao fiscal automatica falhou: {ex.Message})";
            }
        }
    }
}
