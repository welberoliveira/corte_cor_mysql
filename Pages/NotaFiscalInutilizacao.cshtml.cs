using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorteCor.Pages
{
    public class NotaFiscalInutilizacaoModel : PageModel
    {
        private readonly NotaFiscalInutilizacaoHandler _inutHandler;
        private readonly SalaoConfigFiscalHandler _configHandler;
        private readonly FiscalActionService _fiscalActionService;

        public NotaFiscalInutilizacaoModel(NotaFiscalInutilizacaoHandler inutHandler, SalaoConfigFiscalHandler configHandler, FiscalActionService fiscalActionService)
        {
            _inutHandler = inutHandler;
            _configHandler = configHandler;
            _fiscalActionService = fiscalActionService;
        }

        public List<NotaFiscalInutilizacao> Historico { get; set; } = new List<NotaFiscalInutilizacao>();

        [TempData]
        public string Mensagem { get; set; } = string.Empty;

        [TempData]
        public string Erro { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            var idSalao = int.Parse(salaoIdStr);
            Historico = await _inutHandler.ListarPorSalaoAsync(idSalao) ?? new List<NotaFiscalInutilizacao>();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int ano, int serie, int numeroInicial, int numeroFinal, string justificativa)
        {
            try
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

                var idSalao = int.Parse(salaoIdStr);

                if (numeroFinal < numeroInicial)
                {
                    Erro = "O Número Final não pode ser menor do que o Inicial.";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                {
                    Erro = "Justificativa muito curta. Informe no mínimo 15 caracteres.";
                    return RedirectToPage();
                }

                var config = await _configHandler.ObterPorSalaoAsync(idSalao);
                if (config == null)
                {
                    Erro = "Configuração Fiscal do salão não preenchida. Configure-a primeiro.";
                    return RedirectToPage();
                }

                // Dispara o serviço Fiscal responsável por solicitar Inutilizacao na Sefaz
                var inut = await _fiscalActionService.InutilizarNfceAsync(config, ano, serie, numeroInicial, numeroFinal, justificativa);
                inut.IdSalao = idSalao;

                // Registra banco
                await _inutHandler.InserirAsync(inut);

                Mensagem = $"Faixa de {numeroInicial} a {numeroFinal} inutilizada com sucesso.";
            }
            catch (Exception ex)
            {
                Erro = "Ocorreu um erro ao inutilizar a faixa: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
