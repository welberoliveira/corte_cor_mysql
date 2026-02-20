using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CorteCor
{
    public class LembreteService
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly ILembreteHandler _lembreteHandler;
        private readonly BrevoEmailService _emailService;
        private readonly SMSMarketService _smsService;
        private readonly ILogger<LembreteService> _logger;

        public LembreteService(IDatabaseHandler dbHandler, BrevoEmailService emailService, SMSMarketService smsService, ILogger<LembreteService> logger, ILembreteHandler lembreteHandler = null)
        {
            _dbHandler = dbHandler;
            _emailService = emailService;
            _smsService = smsService;
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
                    string destino = tipoEnvio == "SMS" ? dados.TelefoneCliente : dados.EmailCliente;

                    if (tipoEnvio == "SMS")
                    {
                         string conteudo = dados.CorpoModelo ?? $"Lembrete: {dados.NomeServico} em {dados.DataHoraAgendamento:dd/MM HH:mm}";
                         conteudo = conteudo.Replace("{NomeCliente}", dados.NomeCliente)
                                            .Replace("{DataHora}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                            .Replace("{Servico}", dados.NomeServico)
                                            .Replace("{Profissional}", dados.NomeProfissional);

                         (enviado, erroApi) = await _smsService.EnviarSmsAsync(dados.TelefoneCliente, conteudo);
                    }
                    else // Email
                    {
                        string assunto = dados.AssuntoModelo ?? $"Lembrete de Agendamento - {dados.NomeSalao}";
                        string corpo = dados.CorpoModelo ?? "<p>Olá, este é um lembrete do seu agendamento.</p>";
                        
                        corpo = corpo.Replace("{NomeCliente}", dados.NomeCliente)
                                     .Replace("{DataHora}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                     .Replace("{Servico}", dados.NomeServico)
                                     .Replace("{Profissional}", dados.NomeProfissional);

                        (enviado, erroApi) = await _emailService.EnviarEmailGenericoAsync(dados.EmailCliente, dados.NomeCliente, assunto, corpo);
                    }

                    _lembreteHandler.RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, destino, tipoEnvio == "SMS" ? "SMS" : dados.AssuntoModelo, enviado ? "Sucesso" : "ErroEnvio", erroApi, tipoEnvio, tipoEnvio == "SMS" ? dados.TelefoneCliente : null);


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
