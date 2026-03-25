using CorteCor.Handlers;
using CorteCor.Logs;
using CorteCor.Models;
using Microsoft.Extensions.Logging;

namespace CorteCor.Services
{
    public class LembreteService
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly ILembreteHandler _lembreteHandler;
        private readonly BrevoEmailService _emailService;
        private readonly SMSMarketService _smsService;
        private readonly IWhatsappService? _whatsappService;
        private readonly FornecedoresHandler _fornecedoresHandler;
        private readonly ILogger<LembreteService> _logger;

        public LembreteService(
            IDatabaseHandler dbHandler,
            BrevoEmailService emailService,
            SMSMarketService smsService,
            FornecedoresHandler fornecedoresHandler,
            ILogger<LembreteService> logger,
            ILembreteHandler? lembreteHandler = null,
            IWhatsappService? whatsappService = null)
        {
            _dbHandler = dbHandler;
            _emailService = emailService;
            _smsService = smsService;
            _whatsappService = whatsappService;
            _fornecedoresHandler = fornecedoresHandler;
            _logger = logger;
            _lembreteHandler = lembreteHandler ?? new LembreteHandler(dbHandler);
        }

        public async Task<int> ProcessarLembretesAsync(CancellationToken stoppingToken = default)
        {
            var enviados = 0;
            var pendentes = _lembreteHandler.ObterLembretesPendentes();

            // Evita consultas repetidas de limite por salao durante o mesmo batch.
            var cacheDbInfo = new Dictionary<int, (int EnviadosDb, int Limite)>();
            var localEnvios = new Dictionary<int, int>();

            foreach (var lembrete in pendentes)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var dados = _lembreteHandler.ObterDadosEnvio(lembrete.IdLembrete);
                    if (dados == null)
                    {
                        _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "ErroDados");
                        continue;
                    }

                    var idSalao = dados.IdSalao;
                    if (!cacheDbInfo.ContainsKey(idSalao))
                    {
                        _lembreteHandler.VerificarLimiteEmail(idSalao, out var enviadosDb, out var limite);
                        cacheDbInfo[idSalao] = (enviadosDb, limite);
                    }

                    var dbInfo = cacheDbInfo[idSalao];
                    var enviadosAgora = localEnvios.ContainsKey(idSalao) ? localEnvios[idSalao] : 0;
                    var totalEnviados = dbInfo.EnviadosDb + enviadosAgora;

                    if (totalEnviados >= dbInfo.Limite)
                    {
                        if (enviadosAgora == 0 || totalEnviados == dbInfo.Limite)
                        {
                            _logger.LogWarning("Limite de envios atingido para o salao {IdSalao} ({Total}/{Limite}). Marcando lembrete {IdLembrete} como FaltaCredito.", idSalao, totalEnviados, dbInfo.Limite, lembrete.IdLembrete);
                        }

                        var infoDestino = dados.TipoLembrete == "SMS" ? dados.TelefoneCliente : dados.EmailCliente;
                        _lembreteHandler.RegistrarLogEnvio(
                            lembrete.IdLembrete,
                            (int)lembrete.IdAgendamento,
                            infoDestino,
                            dados.TipoLembrete == "SMS" ? "SMS" : dados.AssuntoModelo,
                            "Falha",
                            "Falta de Credito",
                            dados.TipoLembrete ?? "Email",
                            dados.TipoLembrete == "SMS" ? dados.TelefoneCliente : null);

                        _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "FaltaCredito");
                        continue;
                    }

                    var tipoEnvio = dados.TipoLembrete ?? "Email";
                    var destino = tipoEnvio == "SMS"
                        ? dados.TelefoneCliente
                        : (tipoEnvio == "Whatsapp" ? dados.TelefoneCliente : dados.EmailCliente);

                    var (enviado, erroApi) = await ProcessarEnvioAsync(dados, tipoEnvio);

                    _lembreteHandler.RegistrarLogEnvio(
                        lembrete.IdLembrete,
                        (int)lembrete.IdAgendamento,
                        destino,
                        tipoEnvio == "Email" ? dados.AssuntoModelo : tipoEnvio,
                        enviado ? "Sucesso" : "ErroEnvio",
                        erroApi,
                        tipoEnvio,
                        (tipoEnvio == "SMS" || tipoEnvio == "Whatsapp") ? dados.TelefoneCliente : null);

                    _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, enviado ? "Enviado" : "ErroEnvio");

                    if (enviado)
                    {
                        enviados++;
                        if (localEnvios.ContainsKey(idSalao))
                        {
                            localEnvios[idSalao]++;
                        }
                        else
                        {
                            localEnvios[idSalao] = 1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar lembrete {IdLembrete}", lembrete.IdLembrete);
                    _lembreteHandler.RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, "Desconhecido", "Erro no processamento", "ErroExcecao", ex.Message, "Email", null);
                    _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "ErroExcecao");
                }
            }

            return enviados;
        }

        private async Task<(bool Enviado, string? ErroApi)> ProcessarEnvioAsync(LembreteEnvioDTO dados, string tipoEnvio)
        {
            if (tipoEnvio == "SMS")
            {
                var conteudoSms = AplicarPlaceholders(dados.CorpoModelo ?? $"Lembrete: {dados.NomeServico} em {dados.DataHoraAgendamento:dd/MM/yyyy HH:mm}", dados);
                var fornecedorSms = _fornecedoresHandler.ObterSMSAtivo();

                if (fornecedorSms != null && fornecedorSms.Nome.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
                {
                    return await _emailService.EnviarSmsAsync(dados.TelefoneCliente, conteudoSms);
                }

                if (fornecedorSms != null && fornecedorSms.Nome.Equals("SMSMarket", StringComparison.OrdinalIgnoreCase))
                {
                    return await _smsService.EnviarSmsAsync(dados.TelefoneCliente, conteudoSms);
                }

            return (false, "Nenhum fornecedor de SMS ativo ou fornecedor não suportado.");
            }

            if (tipoEnvio == "Whatsapp")
            {
                if (_whatsappService == null)
                {
            return (false, "Serviço de WhatsApp não configurado no ambiente.");
                }

                var conteudoWhatsapp = AplicarPlaceholders(dados.CorpoModelo ?? $"Lembrete: {dados.NomeServico} em {dados.DataHoraAgendamento:dd/MM/yyyy HH:mm}", dados);
                return await _whatsappService.EnviarMensagemAsync(dados.TelefoneCliente, conteudoWhatsapp);
            }

            var assunto = AplicarPlaceholders(dados.AssuntoModelo ?? $"Lembrete de Agendamento - {dados.NomeSalao}", dados);
            var corpo = AplicarPlaceholders(dados.CorpoModelo ?? "<p>Ola, este e um lembrete do seu agendamento.</p>", dados);
            var fornecedorEmail = _fornecedoresHandler.ObterEmailAtivo();

            if (fornecedorEmail != null && fornecedorEmail.Nome.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
            {
                return await _emailService.EnviarEmailGenericoAsync(dados.EmailCliente, dados.NomeCliente, assunto, corpo);
            }

            return (false, "Nenhum fornecedor de e-mail ativo suportado (apenas Brevo configurado).");
        }

        private static string AplicarPlaceholders(string template, LembreteEnvioDTO dados)
        {
            return (template ?? string.Empty)
                .Replace("{NomeCliente}", dados.NomeCliente)
                .Replace("{DataAgendamento}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                .Replace("{NomeServico}", dados.NomeServico)
                .Replace("{NomeFuncionario}", dados.NomeProfissional);
        }
    }
}
