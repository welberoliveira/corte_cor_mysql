using CorteCor.Logs;
using CorteCor.Models;
using CorteCor.Handlers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CorteCor.Handlers;
using Microsoft.Extensions.Logging;

namespace CorteCor.Services
{
    public class LembreteService
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly ILembreteHandler _lembreteHandler;
        private readonly BrevoEmailService _emailService;
        private readonly SMSMarketService _smsService;
        private readonly FornecedoresHandler _fornecedoresHandler;
        private readonly ILogger<LembreteService> _logger;

        public LembreteService(IDatabaseHandler dbHandler, BrevoEmailService emailService, SMSMarketService smsService, FornecedoresHandler fornecedoresHandler, ILogger<LembreteService> logger, ILembreteHandler lembreteHandler = null)
        {
            _dbHandler = dbHandler;
            _emailService = emailService;
            _smsService = smsService;
            _fornecedoresHandler = fornecedoresHandler;
            _logger = logger;
            _lembreteHandler = lembreteHandler ?? new LembreteHandler(dbHandler);
        }

        public async Task<int> ProcessarLembretesAsync(CancellationToken stoppingToken = default)
        {
            int enviados = 0;
            var pendentes = _lembreteHandler.ObterLembretesPendentes();
            
            // Cache para evitar consultar o banco repetidamente para o mesmo salão
            // Key: IdSalao, Value: (EnviadosDb, Limite)
            var cacheDbInfo = new Dictionary<int, (int EnviadosDb, int Limite)>();
            
            // Contador local para envios feitos NESTA execução (batch)
            // Key: IdSalao, Value: Quantidade enviada agora
            var localEnvios = new Dictionary<int, int>();

            foreach (var lembrete in pendentes)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var dados = _lembreteHandler.ObterDadosEnvio(lembrete.IdLembrete);

                    if (dados == null)
                    {
                        _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "ErroDados");
                        continue;
                    }

                    int idSalao = dados.IdSalao;

                    // 1. Obter informações do DB (apenas uma vez por salão neste batch)
                    if (!cacheDbInfo.ContainsKey(idSalao))
                    {
                        _lembreteHandler.VerificarLimiteEmail(idSalao, out int enviadosDb, out int limite);
                        cacheDbInfo[idSalao] = (enviadosDb, limite);
                    }

                    var dbInfo = cacheDbInfo[idSalao];
                    int enviadosAgora = localEnvios.ContainsKey(idSalao) ? localEnvios[idSalao] : 0;
                    int totalEnviados = dbInfo.EnviadosDb + enviadosAgora;

                    // 2. Verificar se o limite foi atingido (considerando DB + Local)
                    if (totalEnviados >= dbInfo.Limite)
                    {
                        // Log apenas na primeira vez que exceder neste batch para não spammar
                        if (enviadosAgora == 0 || (totalEnviados == dbInfo.Limite)) 
                        {
                            _logger.LogWarning($"Limite de envios atingido para o Salão {idSalao} ({totalEnviados}/{dbInfo.Limite}). Marcando lembrete {lembrete.IdLembrete} como 'FaltaCredito'.");
                        }
                        
                        string infoDestino = dados.TipoLembrete == "SMS" ? dados.TelefoneCliente : dados.EmailCliente;
                        _lembreteHandler.RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, infoDestino, dados.TipoLembrete == "SMS" ? "SMS" : dados.AssuntoModelo, "Falha", "Falta de Crédito", dados.TipoLembrete ?? "Email", dados.TipoLembrete == "SMS" ? dados.TelefoneCliente : null);
                        
                        // Atualiza o status para não tentar enviar novamente depois (mesmo se o limite renovar)
                        _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "FaltaCredito");
                        continue; 
                    }

                    bool enviado = false;
                    string erroApi = null;
                    string tipoEnvio = dados.TipoLembrete ?? "Email";
                    string destino = tipoEnvio == "SMS" ? dados.TelefoneCliente : (tipoEnvio == "Whatsapp" ? dados.TelefoneCliente : dados.EmailCliente);

                    if (tipoEnvio == "SMS")
                    {
                         string conteudo = dados.CorpoModelo ?? $"Lembrete: {dados.NomeServico} em {dados.DataHoraAgendamento:dd/MM/yyyy HH:mm}";
                         conteudo = conteudo.Replace("{NomeCliente}", dados.NomeCliente)
                                            .Replace("{DataAgendamento}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                            .Replace("{NomeServico}", dados.NomeServico)
                                            .Replace("{NomeFuncionario}", dados.NomeProfissional);

                         var fornecedorSms = _fornecedoresHandler.ObterSMSAtivo();
                         if (fornecedorSms != null && fornecedorSms.Nome.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
                         {
                             (enviado, erroApi) = await _emailService.EnviarSmsAsync(dados.TelefoneCliente, conteudo);
                         }
                         else if (fornecedorSms != null && fornecedorSms.Nome.Equals("SMSMarket", StringComparison.OrdinalIgnoreCase))
                         {
                             (enviado, erroApi) = await _smsService.EnviarSmsAsync(dados.TelefoneCliente, conteudo);
                         }
                         else
                         {
                             erroApi = "Nenhum fornecedor de SMS ativo ou fornecedor não suportado.";
                             enviado = false;
                         }
                    }
                    else if (tipoEnvio == "Whatsapp")
                    {
                        string conteudo = dados.CorpoModelo ?? $"Lembrete: {dados.NomeServico} em {dados.DataHoraAgendamento:dd/MM/yyyy HH:mm}";
                        conteudo = conteudo.Replace("{NomeCliente}", dados.NomeCliente)
                                           .Replace("{DataAgendamento}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                           .Replace("{NomeServico}", dados.NomeServico)
                                           .Replace("{NomeFuncionario}", dados.NomeProfissional);

                        var fornecedorWa = _fornecedoresHandler.ObterWhatsappAtivo();
                        // Aqui seria chamado o serviço de WhatsApp dependendo do fornecedor (ex: Z-API, WppConnect, etc).
                        // Atualmente não há um WhatsappService implementado, mas a estrutura está pronta.
                        erroApi = "Integração de WhatsApp ainda não implementada no sistema.";
                        enviado = false;
                    }
                    else // Email
                    {
                        string assunto = dados.AssuntoModelo ?? $"Lembrete de Agendamento - {dados.NomeSalao}";
                        string corpo = dados.CorpoModelo ?? "<p>Olá, este é um lembrete do seu agendamento.</p>";
                        
                        corpo = corpo.Replace("{NomeCliente}", dados.NomeCliente)
                                     .Replace("{DataAgendamento}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                     .Replace("{NomeServico}", dados.NomeServico)
                                     .Replace("{NomeFuncionario}", dados.NomeProfissional);
                        
                        assunto = assunto.Replace("{NomeCliente}", dados.NomeCliente)
                                         .Replace("{DataAgendamento}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                         .Replace("{NomeServico}", dados.NomeServico)
                                         .Replace("{NomeFuncionario}", dados.NomeProfissional);

                        var fornecedorEmail = _fornecedoresHandler.ObterEmailAtivo();
                        if (fornecedorEmail != null && fornecedorEmail.Nome.Equals("Brevo", StringComparison.OrdinalIgnoreCase))
                        {
                            (enviado, erroApi) = await _emailService.EnviarEmailGenericoAsync(dados.EmailCliente, dados.NomeCliente, assunto, corpo);
                        }
                        else
                        {
                             erroApi = "Nenhum fornecedor de E-mail ativo suportado (apenas Brevo configurado).";
                             enviado = false;
                        }
                    }

                    _lembreteHandler.RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, destino, tipoEnvio == "Email" ? dados.AssuntoModelo : tipoEnvio, enviado ? "Sucesso" : "ErroEnvio", erroApi, tipoEnvio, (tipoEnvio == "SMS" || tipoEnvio == "Whatsapp") ? dados.TelefoneCliente : null);


                    _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, enviado ? "Enviado" : "ErroEnvio");
                    
                    if (enviado) 
                    {
                        enviados++;
                        // Incrementar contador local
                        if (localEnvios.ContainsKey(idSalao))
                            localEnvios[idSalao]++;
                        else
                            localEnvios[idSalao] = 1;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao enviar lembrete {lembrete.IdLembrete}");
                    _lembreteHandler.RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, "Desconhecido", "Erro no processamento", "ErroExcecao", ex.Message, "Email", null);
                    _lembreteHandler.AtualizarStatusLembrete(lembrete.IdLembrete, "ErroExcecao");
                }
            }
            return enviados;
        }
    }
}

