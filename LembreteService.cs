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
        private readonly BrevoEmailService _emailService;
        private readonly ILogger<LembreteService> _logger;

        public LembreteService(IDatabaseHandler dbHandler, BrevoEmailService emailService, ILogger<LembreteService> logger)
        {
            _dbHandler = dbHandler;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<int> ProcessarLembretesAsync(CancellationToken stoppingToken = default)
        {
            int enviados = 0;
            var pendentes = ObterLembretesPendentes();
            var cacheLimites = new Dictionary<int, bool>(); // IdSalao -> LimitReached
            var lembreteHandler = new LembreteHandler(_dbHandler);

            foreach (var lembrete in pendentes)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    var dados = ObterDadosEnvio(lembrete.IdLembrete);

                    if (dados == null)
                    {
                        AtualizarStatusLembrete(lembrete.IdLembrete, "ErroDados");
                        continue;
                    }

                    // Check Limit
                    int idSalao = dados.IdSalao;
                    if (!cacheLimites.ContainsKey(idSalao))
                    {
                        bool atingido = lembreteHandler.VerificarLimiteEmail(idSalao, out int used, out int limit);
                        cacheLimites[idSalao] = atingido;
                        if (atingido)
                        {
                            _logger.LogWarning($"Limite de envios atingido para o Salão {idSalao} ({used}/{limit}).");
                        }
                    }

                    if (cacheLimites[idSalao])
                    {
                        _logger.LogInformation($"Skipping lembrete {lembrete.IdLembrete} due to email limit.");
                        continue; 
                    }

                    string assunto = dados.AssuntoModelo ?? $"Lembrete de Agendamento - {dados.NomeSalao}";
                    string corpo = dados.CorpoModelo ?? "<p>Olá, este é um lembrete do seu agendamento.</p>";
                    
                    corpo = corpo.Replace("{NomeCliente}", dados.NomeCliente)
                                 .Replace("{DataHora}", dados.DataHoraAgendamento.ToString("dd/MM/yyyy HH:mm"))
                                 .Replace("{Servico}", dados.NomeServico)
                                 .Replace("{Profissional}", dados.NomeProfissional);

                    bool enviado = await _emailService.EnviarEmailGenericoAsync(dados.EmailCliente, dados.NomeCliente, assunto, corpo);

                    RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, dados.EmailCliente, assunto, enviado ? "Sucesso" : "ErroEnvio");
                    AtualizarStatusLembrete(lembrete.IdLembrete, enviado ? "Enviado" : "ErroEnvio");
                    
                    if (enviado) enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao enviar lembrete {lembrete.IdLembrete}");
                    RegistrarLogEnvio(lembrete.IdLembrete, (int)lembrete.IdAgendamento, "Desconhecido", "Erro no processamento", "ErroExcecao", ex.Message);
                    AtualizarStatusLembrete(lembrete.IdLembrete, "ErroExcecao");
                }
            }
            return enviados;
        }

        private List<dynamic> ObterLembretesPendentes()
        {
            var lista = new List<dynamic>();
            string query = @"SELECT IdLembrete, IdAgendamento FROM CorteCor_LembreteAgendado 
                             WHERE Status = 'Pendente' AND DataEnvioProgramada <= @Agora";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Agora", DateTime.Now);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new { IdLembrete = (int)reader["IdLembrete"], IdAgendamento = (int)reader["IdAgendamento"] });
                    }
                }
            }
            return lista;
        }

        private dynamic ObterDadosEnvio(int idLembrete)
        {
            string query = @"
                SELECT 
                    P.Nome AS NomeCliente, P.Email AS EmailCliente,
                    A.DataHora AS DataHoraAgendamento,
                    S.Nome AS NomeServico,
                    F.Nome AS NomeProfissional,
                    TS.Nome AS NomeSalao, TS.IdSalao,
                    M.Assunto, M.CorpoHTML
                FROM CorteCor_LembreteAgendado LA
                JOIN CorteCor_Agendamento A ON LA.IdAgendamento = A.IdAgendamento
                JOIN CorteCor_Pessoa P ON A.IdPessoa = P.IdPessoa
                JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
                JOIN CorteCor_Funcionario F ON A.IdFuncionario = F.IdFuncionario
                JOIN CorteCor_LembreteConfig C ON LA.IdConfig = C.IdConfig
                LEFT JOIN CorteCor_ModeloEmail M ON C.IdModeloEmail = M.IdModelo
                LEFT JOIN CorteCor_Salao TS ON S.IdSalao = TS.IdSalao
                WHERE LA.IdLembrete = @IdLembrete";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdLembrete", idLembrete);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            NomeCliente = reader["NomeCliente"].ToString(),
                            EmailCliente = reader["EmailCliente"].ToString(),
                            DataHoraAgendamento = Convert.ToDateTime(reader["DataHoraAgendamento"]),
                            NomeServico = reader["NomeServico"].ToString(),
                            NomeProfissional = reader["NomeProfissional"].ToString(),
                            NomeSalao = reader["NomeSalao"] is DBNull ? "Salão" : reader["NomeSalao"].ToString(),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            AssuntoModelo = reader["Assunto"] is DBNull ? null : reader["Assunto"].ToString(),
                            CorpoModelo = reader["CorpoHTML"] is DBNull ? null : reader["CorpoHTML"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        private void AtualizarStatusLembrete(int idLembrete, string status)
        {
            string query = @"UPDATE CorteCor_LembreteAgendado SET Status = @Status, DataEnvioReal = @Data WHERE IdLembrete = @Id";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@Data", DateTime.Now);
                command.AddWithValue("@Id", idLembrete);
                command.ExecuteNonQuery();
            }
        }

        private void RegistrarLogEnvio(int idLembrete, int idAgendamento, string destinatario, string assunto, string status, string mensagemErro = null)
        {
            try
            {
                string query = @"INSERT INTO CorteCor_LogEnvioEmail (IdLembrete, IdAgendamento, DataEnvio, Destinatario, Assunto, Status, MensagemErro)
                                 VALUES (@IdLembrete, @IdAgendamento, @DataEnvio, @Destinatario, @Assunto, @Status, @MensagemErro)";
                
                using (var connection = _dbHandler.GetConnection())
                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdLembrete", idLembrete);
                    command.AddWithValue("@IdAgendamento", idAgendamento);
                    command.AddWithValue("@DataEnvio", DateTime.Now);
                    command.AddWithValue("@Destinatario", destinatario ?? "");
                    command.AddWithValue("@Assunto", assunto ?? "");
                    command.AddWithValue("@Status", status);
                    command.AddWithValue("@MensagemErro", mensagemErro ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar log de envio de e-mail.");
            }
        }
    }
}
