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

namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class LembreteConfigCadastroModel : PageModel
    {
        private readonly ILembreteHandler _handler;
        private readonly ModeloEmailHandler _modeloEmailHandler;
        private readonly ModeloSMSHandler _modeloSmsHandler;

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

        public LembreteConfigCadastroModel(ILembreteHandler handler, ModeloEmailHandler modeloEmailHandler, ModeloSMSHandler modeloSmsHandler)
        {
            _handler = handler;
            _modeloEmailHandler = modeloEmailHandler;
            _modeloSmsHandler = modeloSmsHandler;
            
            UnidadeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Minutos", Text = "Minutos" },
                new SelectListItem { Value = "Horas", Text = "Horas" },
                new SelectListItem { Value = "Dias", Text = "Dias" }
            };
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

                // Load Email Models
                var modeloEmails = _modeloEmailHandler.ListarPorSalao(idSalao);
                ModeloEmailOptions = modeloEmails.Select(m => new SelectListItem
                {
                    Value = m.IdModelo.ToString(),
                    Text = m.Assunto
                }).ToList();

                // Load SMS Models
                var modeloSms = _modeloSmsHandler.ListarPorSalao(idSalao);
                ModeloSmsOptions = modeloSms.Select(m => new SelectListItem
                {
                    Value = m.IdModelo.ToString(),
                    Text = m.TipoEvento + " - " + (m.Conteudo.Length > 20 ? m.Conteudo.Substring(0, 20) + "..." : m.Conteudo)
                }).ToList();
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
                var configs = _handler.ListarConfig(Config.IdSalao);
                var existing = configs.FirstOrDefault(c => c.IdConfig == id.Value);
                if (existing != null)
                {
                    Config = existing;
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
                    var modeloEmails = _modeloEmailHandler.ListarPorSalao(idS);
                    ModeloEmailOptions = modeloEmails.Select(m => new SelectListItem
                    {
                        Value = m.IdModelo.ToString(),
                        Text = m.Assunto
                    }).ToList();

                    var modeloSms = _modeloSmsHandler.ListarPorSalao(idS);
                    ModeloSmsOptions = modeloSms.Select(m => new SelectListItem
                    {
                        Value = m.IdModelo.ToString(),
                        Text = m.TipoEvento + " - " + (m.Conteudo.Length > 20 ? m.Conteudo.Substring(0, 20) + "..." : m.Conteudo)
                    }).ToList();
                }
                return Page();
            }

            var idSalaoClm = User.FindFirst("IdSalao");
            if (idSalaoClm != null && int.TryParse(idSalaoClm.Value, out int idSalao))
            {
                Config.IdSalao = idSalao;
            }

            Config.Ativo = true; // Forçar sempre como ativa conforme solicitação
            _handler.SalvarConfig(Config);

            // Aplicar regra retroativamente para agendamentos já existentes
            if (Config.Ativo)
            {
                try
                {
                    _handler.AplicarRegraRetroativa(Config.IdConfig);
                }
                catch (System.Exception ex)
                {
                    // Logar erro mas não impedir o fluxo, pois a regra foi salva
                    System.Console.WriteLine($"Erro ao aplicar regra retroativa: {ex.Message}");
                }
            }

            return RedirectToPage("/LembreteConfigLista");
        }
    }
}

