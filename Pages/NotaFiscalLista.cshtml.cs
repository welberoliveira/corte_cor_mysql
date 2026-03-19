using CorteCor.Handlers;
using CorteCor.Models;
using CorteCor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class NotaFiscalListaModel : PageModel
    {
        private readonly NotaFiscalHandler _notaHandler;
        private readonly NotaFiscalAvulsaService _notaFiscalAvulsaService;

        public NotaFiscalListaModel(NotaFiscalHandler notaHandler, NotaFiscalAvulsaService notaFiscalAvulsaService)
        {
            _notaHandler = notaHandler;
            _notaFiscalAvulsaService = notaFiscalAvulsaService;
        }

        public IList<NotaFiscal> Notas { get; set; } = new List<NotaFiscal>();

        [BindProperty(SupportsGet = true)]
        public Guid? IdNotaFiscal { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IdAgendamento { get; set; }

        public string FiltroDescricao { get; set; } = string.Empty;

        [TempData]
        public string Mensagem { get; set; } = string.Empty;

        [TempData]
        public string Erro { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var salaoIdStr = User.FindFirst("IdSalao")?.Value;
            if (string.IsNullOrEmpty(salaoIdStr)) return RedirectToPage("/Index");

            var idSalao = int.Parse(salaoIdStr);
            var notas = await _notaHandler.ListarPorSalaoAsync(idSalao) ?? new List<NotaFiscal>();

            if (IdAgendamento.HasValue)
            {
                notas = notas.Where(n => n.IdAgendamento == IdAgendamento.Value).ToList();
                FiltroDescricao = $"Exibindo notas vinculadas ao agendamento #{IdAgendamento.Value}.";
            }

            if (IdNotaFiscal.HasValue)
            {
                notas = notas.Where(n => n.IdNotaFiscal == IdNotaFiscal.Value).ToList();
                FiltroDescricao = string.IsNullOrWhiteSpace(FiltroDescricao)
                    ? "Exibindo a nota fiscal selecionada."
                    : $"{FiltroDescricao} Filtro adicional pela nota selecionada.";
            }

            Notas = notas;
            return Page();
        }

        public async Task<IActionResult> OnPostDownloadXmlAsync(Guid idNota)
        {
            try
            {
                var idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);
                if (nota == null || string.IsNullOrEmpty(nota.XmlRetorno))
                {
                    Erro = "XML nao encontrado ou nota nao pertence a este salao.";
                    return RedirectToPage();
                }

                var fileName = $"NF_{nota.TipoNota}_{nota.Numero}.xml";
                return File(Encoding.UTF8.GetBytes(nota.XmlRetorno), "application/xml", fileName);
            }
            catch (Exception ex)
            {
                Erro = "Erro ao baixar XML: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostConsultarAsync(Guid idNota)
        {
            try
            {
                var idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);
                if (nota == null)
                {
                    Erro = "Nota nao encontrada.";
                    return RedirectToPage();
                }

                var modelo = nota.TipoNota == "NFS-e" ? "NFSE" : nota.TipoNota == "NFC-e" ? "65" : "55";
                var chave = nota.TipoNota == "NFS-e" ? nota.ChaveAcessoNacional : nota.ChaveAcesso;
                if (string.IsNullOrWhiteSpace(chave))
                {
                    Erro = "A nota nao possui chave para consulta.";
                    return RedirectToPage();
                }

                var resultado = await _notaFiscalAvulsaService.ConsultarAsync(idSalao, modelo, nota.Ambiente, chave);
                Mensagem = resultado.Mensagem;
            }
            catch (Exception ex)
            {
                Erro = "Erro ao consultar nota: " + ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGerarPdfAsync(Guid idNota)
        {
            try
            {
                var idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);
                if (nota == null)
                {
                    Erro = "Nota nao encontrada.";
                    return RedirectToPage();
                }

                var chave = nota.TipoNota == "NFS-e" ? nota.ChaveAcessoNacional : nota.ChaveAcesso;
                if (string.IsNullOrWhiteSpace(chave))
                {
                    Erro = "Nota sem chave fiscal para gerar o PDF.";
                    return RedirectToPage();
                }

                var pdf = await _notaFiscalAvulsaService.GerarPdfAsync(idSalao, chave);
                return File(pdf.Bytes, "application/pdf", pdf.FileName);
            }
            catch (Exception ex)
            {
                Erro = "Erro ao gerar PDF: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEnviarEmailAsync(Guid idNota, string emailDestino, string? nomeDestino)
        {
            try
            {
                var idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);
                if (nota == null)
                {
                    Erro = "Nota nao encontrada.";
                    return RedirectToPage();
                }

                var chave = nota.TipoNota == "NFS-e" ? nota.ChaveAcessoNacional : nota.ChaveAcesso;
                if (string.IsNullOrWhiteSpace(chave))
                {
                    Erro = "Nota sem chave fiscal para envio por e-mail.";
                    return RedirectToPage();
                }

                var resultado = await _notaFiscalAvulsaService.EnviarEmailAsync(idSalao, chave, emailDestino, nomeDestino);
                Mensagem = resultado.Mensagem;
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                Erro = "Erro ao preparar envio do e-mail: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCancelarNotaAsync(Guid idNota, string justificativa)
        {
            try
            {
                var idSalao = int.Parse(User.FindFirst("IdSalao")?.Value ?? "0");
                if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15)
                {
                    Erro = "A justificativa de cancelamento deve ter pelo menos 15 caracteres.";
                    return RedirectToPage();
                }

                var nota = await _notaHandler.ObterPorIdAsync(idNota, idSalao);
                if (nota == null)
                {
                    Erro = "Nota nao encontrada.";
                    return RedirectToPage();
                }

                var chave = nota.TipoNota == "NFS-e" ? nota.ChaveAcessoNacional : nota.ChaveAcesso;
                if (string.IsNullOrWhiteSpace(chave))
                {
                    Erro = "Nota sem chave fiscal para cancelamento.";
                    return RedirectToPage();
                }

                var resultado = await _notaFiscalAvulsaService.CancelarAsync(idSalao, chave, justificativa);
                Mensagem = resultado.Mensagem;
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
