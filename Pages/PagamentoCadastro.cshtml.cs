using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Adicionado para JsonResult
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class PagamentoCadastroModel : PageModel
    {
        public Pagamento Pagamento { get; set; }
        public string ButtonText = "Cadastrar";
        public string Mensagem { get; set; }

        public void OnGet(Guid? id, int? idAgendamento)
        {
            var handler = new PagamentoHandler();

            if (id.HasValue && id.Value != Guid.Empty)
            {
                Pagamento = handler.ObterPorId(id.Value);
                ButtonText = "Atualizar";

                // Se foi passado um idAgendamento via URL, atualiza o modelo
                if (idAgendamento.HasValue && Pagamento != null)
                    Pagamento.IdAgendamento = idAgendamento.Value;
            }
            else if (idAgendamento.HasValue)
            {
                // Pre-fill based on Scheduling
                var agendamentoHandler = new AgendamentoHandler();
                var agendamento = agendamentoHandler.ObterPorId(idAgendamento.Value);
                if (agendamento != null)
                {
                    var servicoHandler = new ServicoHandler();
                    var servico = servicoHandler.ObterPorId(agendamento.IdServico);

                    var pessoaHandler = new PessoaHandler();
                    var pessoa = pessoaHandler.ObterPorId(agendamento.IdPessoa);

                    Pagamento = new Pagamento
                    {
                        IdAgendamento = idAgendamento.Value,
                        Valor = servico?.Preco ?? 0,
                        Descricao = servico != null ? $"Pagamento Ref. ServiÃ§o {servico.Nome}" : "",
                        Data = DateTime.Now,
                        NomeCliente = pessoa?.Nome,
                        NomeServico = servico?.Nome
                    };
                }
            }
        }

        public JsonResult OnGetDadosAgendamento(int id)
        {
            var agendamentoHandler = new AgendamentoHandler();
            var agendamento = agendamentoHandler.ObterPorId(id);
            if (agendamento != null)
            {
                var servicoHandler = new ServicoHandler();
                var servico = servicoHandler.ObterPorId(agendamento.IdServico);
                if (servico != null)
                {
                    return new JsonResult(new { success = true, valor = servico.Preco, descricao = $"Pagamento Ref. ServiÃ§o {servico.Nome}" });
                }
            }
            return new JsonResult(new { success = false });
        }

        private static decimal ParseDecimalBR(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return 0m;
            valor = valor.Trim().Replace(".", "").Replace(",", ".");
            return decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static DateTime ParseDateTimeLocal(string valor)
        {
            // Espera "yyyy-MM-ddTHH:mm"
            if (string.IsNullOrWhiteSpace(valor)) return DateTime.Now;
            return DateTime.Parse(valor);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Guid id = Guid.Empty;
            Guid.TryParse(Request.Form["id"], out id);

            int idAgendamento = 0;
            int.TryParse(Request.Form["idAgendamento"], out idAgendamento);

            int idMeioPagamento = 0;
            int.TryParse(Request.Form["idMeioPagamento"], out idMeioPagamento);

            decimal valor = ParseDecimalBR(Request.Form["valor"]);
            DateTime data = ParseDateTimeLocal(Request.Form["data"]);
            DateTime? pagoEm = string.IsNullOrWhiteSpace(Request.Form["pagoEm"]) ? null : DateTime.Parse(Request.Form["pagoEm"]);

            var pagamento = new Pagamento
            {
                IdPagamento = id == Guid.Empty ? Guid.NewGuid() : id,
                IdAgendamento = idAgendamento,
                IdMeioPagamento = idMeioPagamento,

                Tipo = Request.Form["tipo"],
                Valor = valor,
                Data = data,
                PagoEm = pagoEm,

                Contos = Request.Form["contos"],
                Campos = Request.Form["campos"],

                Ativo = true,
                Status = "Pago", // Cadastro manual assume Pago? Se houver seletor, deveria pegar de form, mas original era string fixa. 
                Moeda = "BRL",
                CriadoEm = DateTime.UtcNow
            };

            var handler = new PagamentoHandler();

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            if (id != Guid.Empty)
            {
                handler.AtualizarPagamento(pagamento, idSalao);
                Mensagem = "Pagamento atualizado com sucesso!";
            }
            else
            {
                handler.CadastrarPagamento(pagamento);
                id = pagamento.IdPagamento;
                Mensagem = "Pagamento cadastrado com sucesso!";
            }

            // GATILHO FISCAL
            if (pagamento.Status == "Pago" && idAgendamento > 0)
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (int.TryParse(salaoIdStr, out int idSalaoConfig))
                {
                    try
                    {
                        var db = new DatabaseHandler();
                        var configHandler = new SalaoConfigFiscalHandler(db);
                        var notaHandler = new NotaFiscalHandler(db);

                        var config = await configHandler.ObterPorSalaoAsync(idSalaoConfig);
                        // Verifica se existe config fiscal para emissÃ£o e se a emissÃ£o automÃ¡tica estÃ¡ habilitada
                        if (config != null && config.EmissaoAutomatica)
                        {
                            // Verifica se jÃ¡ nÃ£o existe nota para este agendamento (previne duplicar)
                            var notasExistentes = await notaHandler.ListarPorSalaoAsync(idSalaoConfig);
                            bool jaExiste = notasExistentes.Any(n => n.IdAgendamento == idAgendamento && n.Status != "Cancelada");

                            if (!jaExiste)
                            {
                                var novaNota = new NotaFiscal
                                {
                                    IdNotaFiscal = Guid.NewGuid(),
                                    IdSalao = idSalaoConfig,
                                    IdAgendamento = idAgendamento,
                                    TipoNota = "NFS-e", // PadrÃ£o assumido para serviÃ§o de salÃ£o
                                    Ambiente = config.Ambiente,
                                    Numero = 0, // A ser gerado pelo emissor fiscal
                                    Serie = 1,
                                    ValorTotal = pagamento.Valor,
                                    Status = "Pendente",
                                    DataEmissao = DateTime.Now,
                                    DataAtualizacao = DateTime.Now
                                };

                                await notaHandler.InserirAsync(novaNota);
                                Mensagem += " (Nota fiscal enfileirada para emissÃ£o automÃ¡tica!)";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Apenas logar, nÃ£o deve quebrar a pÃ¡gina de pagamento
                        Console.WriteLine($"Erro ao gerar emissÃ£o fiscal automÃ¡tica: {ex.Message}");
                    }
                }
            }

            OnGet(id != Guid.Empty ? id : (Guid?)null, null);
            return Page();
        }
    }
}


