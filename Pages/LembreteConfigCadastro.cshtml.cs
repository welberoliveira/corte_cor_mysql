using CorteCor.Models;
using CorteCor.Handlers;
using System.Collections.Generic;
using System.Security.Claims;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigCadastroModel : PageModel
    {
        private readonly ILembreteHandler _handler;
        private readonly ModeloEmailHandler _modeloEmailHandler;
        private readonly ModeloSMSHandler _modeloSmsHandler;
        private readonly ILogger<LembreteConfigCadastroModel> _logger;

        [BindProperty]
        public LembreteConfig Config { get; set; } = new LembreteConfig();

        public bool IsViewMode { get; set; }

        public List<SelectListItem> UnidadeOptions { get; set; } = new();
        public List<SelectListItem> ModeloEmailOptions { get; set; } = new();
        public List<SelectListItem> ModeloSmsOptions { get; set; } = new();
        
        public List<SelectListItem> TipoOptions { get; set; } = new List<SelectListItem> 
        { 
            new SelectListItem { Value = "Email", Text = "E-mail" },
            new SelectListItem { Value = "SMS", Text = "SMS" }
        };

        public string Mensagem { get; set; }

        public LembreteConfigCadastroModel(
            ILembreteHandler handler,
            ModeloEmailHandler modeloEmailHandler,
            ModeloSMSHandler modeloSmsHandler,
            ILogger<LembreteConfigCadastroModel> logger)
        {
            _handler = handler;
            _modeloEmailHandler = modeloEmailHandler;
            _modeloSmsHandler = modeloSmsHandler;
            _logger = logger;
            
            UnidadeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Minutos", Text = "Minutos" },
                new SelectListItem { Value = "Horas", Text = "Horas" },
                new SelectListItem { Value = "Dias", Text = "Dias" }
            };
        }

        private void CarregarOpcoesModelos(int idSalao)
        {
            try
            {
                var modeloEmails = _modeloEmailHandler.ListarPorSalao(idSalao);
                ModeloEmailOptions = modeloEmails.Select(m => new SelectListItem
                {
                    Value = m.IdModelo.ToString(),
                    Text = string.IsNullOrWhiteSpace(m.Assunto) ? m.TipoEvento : m.Assunto
                }).ToList();

                var modeloSms = _modeloSmsHandler.ListarPorSalao(idSalao);
                ModeloSmsOptions = modeloSms.Select(m => new SelectListItem
                {
                    Value = m.IdModelo.ToString(),
                    Text = DescreverModeloSms(m)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar modelos para regra de lembrete da sala {IdSalao}.", idSalao);
                ModeloEmailOptions = new List<SelectListItem>();
                ModeloSmsOptions = new List<SelectListItem>();
                Mensagem = "Nao foi possivel carregar os modelos de e-mail/SMS no momento.";
            }
        }

        private static string DescreverModeloSms(ModeloSMS modelo)
        {
            var evento = string.IsNullOrWhiteSpace(modelo.TipoEvento) ? "SMS" : modelo.TipoEvento;
            var conteudo = modelo.Conteudo ?? string.Empty;
            var resumo = conteudo.Length > 20 ? conteudo.Substring(0, 20) + "..." : conteudo;
            return string.IsNullOrWhiteSpace(resumo) ? evento : $"{evento} - {resumo}";
        }

        public void OnGet(int? id, bool view = false)
        {
            IsViewMode = view;

            var idSalaoClaim = User.FindFirst("IdSalao");
            if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idSalao))
            {
                Config.IdSalao = idSalao;
                Config.Ativo = true; // Default for new
                Config.DataInicio = DateTime.Today; // Default starting today
                Config.TipoLembrete = "Email"; // Default
                Config.AntecedenciaValor = 30; // Default: 30 minutes

                CarregarOpcoesModelos(idSalao);
            }

            // Logic to load existing if ID is provided (Not fully implemented in handler yet as we only have List, but logic below assumes new)
            // If I want edit, I need ObterPorId in Handler.
            // But list page passes ID.
            // For now, I'll rely on "New" only or implement GetById if strictly required.
            // The prompt says "create screen to configure rules... list screen with button to register new".
            // It doesn't explicitly demand Edit, but it's good practice. Use ListarConfig and filter by memory if needed or add Get method.
            // I'll skip Edit for now to save tokens/time unless I add GetById to Handler. 
            // Wait, ListarConfig returns all. I can filter here.
            
            if (id.HasValue)
            {
                try
                {
                    var configs = _handler.ListarConfig(Config.IdSalao);
                    var existing = configs.FirstOrDefault(c => c.IdConfig == id.Value);
                    if (existing != null)
                    {
                        Config = existing;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao carregar regra de lembrete {IdConfig} para sala {IdSalao}.", id.Value, Config.IdSalao);
                    Mensagem = "Nao foi possivel carregar a regra de lembrete.";
                }
            }
        }

        public IActionResult OnPost()
        {
            // Validação customizada
            if (Config.AntecedenciaUnidade == "Minutos" && Config.AntecedenciaValor < 30)
            {
                ModelState.AddModelError("Config.AntecedenciaValor", "Para minutos, o valor mínimo é 30.");
            }

            if (!ModelState.IsValid)
            {
                // Repopular SelectLists em caso de erro
                var idSalaoClaim = User.FindFirst("IdSalao");
                if (idSalaoClaim != null && int.TryParse(idSalaoClaim.Value, out int idS))
                {
                    CarregarOpcoesModelos(idS);
                }
                return Page();
            }

            var idSalaoClm = User.FindFirst("IdSalao");
            if (idSalaoClm != null && int.TryParse(idSalaoClm.Value, out int idSalao))
            {
                Config.IdSalao = idSalao;
            }

            Config.Ativo = true; // Forcar sempre como ativa conforme solicitacao
            try
            {
                _handler.SalvarConfig(Config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar regra de lembrete para sala {IdSalao}.", Config.IdSalao);
                Mensagem = "Nao foi possivel salvar a regra de lembrete.";
                CarregarOpcoesModelos(Config.IdSalao);
                return Page();
            }

            // Aplicar regra retroativamente para agendamentos já existentes
            if (Config.Ativo)
            {
                try
                {
                    _handler.AplicarRegraRetroativa(Config.IdConfig);
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Regra de lembrete {IdConfig} salva, mas houve erro ao aplicar retroativamente.", Config.IdConfig);
                }
            }

            return RedirectToPage("/LembreteConfigLista");
        }
    }
}

