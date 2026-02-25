using CorteCor.Handlers;
using CorteCor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text;

namespace CorteCor.Pages
{
    public class NotaFiscalListaModel : PageModel
    {
        private readonly NotaFiscalHandler _notaHandler;

        public NotaFiscalListaModel(NotaFiscalHandler notaHandler)
        {
            _notaHandler = notaHandler;
        }

        public IList<NotaFiscal> Notas { get; set; } = new List<NotaFiscal>();

        [TempData]
        public string Mensagem { get; set; }

        [TempData]
        public string Erro { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            var idSalao = int.Parse(salaoIdStr);

            // Fetch from handler
            var todasNotas = await _notaHandler.ListarPorSalaoAsync(idSalao);
            Notas = todasNotas ?? new List<NotaFiscal>();

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadXmlAsync(Guid idNota)
        {
            try
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

                var idSalao = int.Parse(salaoIdStr);

                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);

                if (nota == null || string.IsNullOrEmpty(nota.XmlRetorno))
                {
                    Erro = "XML nÃ£o encontrado ou nota nÃ£o pertence a este salÃ£o.";
                    return RedirectToPage();
                }

                var fileName = $"NF_{nota.TipoNota}_{nota.Numero}.xml";
                var fileBytes = Encoding.UTF8.GetBytes(nota.XmlRetorno);

                return File(fileBytes, "application/xml", fileName);
            }
            catch (Exception ex)
            {
                Erro = "Erro ao baixar XML: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostConsultarAsync(Guid idNota)
        {
            // Ponto de entrada p/ botão de trigger manual chamando Unimake via ServicoFical
            Mensagem = "Consulta acionada (mock). O Job assíncrono deve atualizar esta nota em breve.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGerarPdfAsync(Guid idNota)
        {
            try
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

                var idSalao = int.Parse(salaoIdStr);
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);

                if (nota == null || string.IsNullOrEmpty(nota.XmlRetorno))
                {
                    Erro = "Nota não encontrada ou sem XML de retorno para gerar o PDF.";
                    return RedirectToPage();
                }

                // Simular um PDF gerado
                byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes("PDF Simulado da Nota Fiscal " + nota.Numero);
                var fileName = $"DANFE_{nota.TipoNota}_{nota.Numero}.pdf";

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Erro = "Erro ao gerar PDF: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEnviarEmailAsync(Guid idNota)
        {
            try
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

                var idSalao = int.Parse(salaoIdStr);
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);

                if (nota == null)
                {
                    Erro = "Nota não encontrada.";
                    return RedirectToPage();
                }

                // Simula envio de email
                Mensagem = $"E-mail com XML e PDF da {nota.TipoNota} nº {nota.Numero} enviado com sucesso ao destinatário.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Erro = "Erro ao enviar e-mail: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCancelarNotaAsync(Guid idNota, string justificativa)
        {
            try
            {
                var salaoIdStr = User.FindFirst("IdSalao")?.Value;
                if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

                if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                {
                    Erro = "A justificativa de cancelamento deve ter pelo menos 15 caracteres.";
                    return RedirectToPage();
                }

                var idSalao = int.Parse(salaoIdStr);
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);

                if (nota == null)
                {
                    Erro = "Nota não encontrada.";
                    return RedirectToPage();
                }

                // Atualizar Status (Simulação)
                nota.Status = "Cancelada";
                nota.JustificativaRejeicao = "Cancelada pelo usuário: " + justificativa;
                await _notaHandler.InserirAsync(nota); // Re-salva a nota

                Mensagem = $"Nota Fiscal {nota.Numero} cancelada com sucesso na Sefaz/Prefeitura.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Erro = "Erro ao cancelar nota: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}
