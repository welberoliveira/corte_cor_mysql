using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class NotaFiscalLogHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public NotaFiscalLogHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task LogarEtapaAsync(int idSalao, int? idAgendamento, Guid? idNotaFiscal, string etapa, string mensagemStatus, string? conteudoXml = null)
        {
            var log = new NotaFiscalLog
            {
                IdLog = Guid.NewGuid(),
                IdSalao = idSalao,
                IdAgendamento = idAgendamento,
                IdNotaFiscal = idNotaFiscal,
                DataHora = DateTime.Now,
                Etapa = etapa,
                MensagemStatus = mensagemStatus,
                ConteudoXml = conteudoXml
            };

            await InserirAsync(log);
        }

        private async Task InserirAsync(NotaFiscalLog log)
        {
            string query = @"
                INSERT INTO CorteCor_NotaFiscalLog 
                (IdLog, IdNotaFiscal, IdAgendamento, IdSalao, DataHora, Etapa, MensagemStatus, ConteudoXml) 
                VALUES 
                (@IdLog, @IdNotaFiscal, @IdAgendamento, @IdSalao, @DataHora, @Etapa, @MensagemStatus, @ConteudoXml);";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            command.AddWithValue("@IdLog", log.IdLog);
            command.AddWithValue("@IdNotaFiscal", log.IdNotaFiscal ?? (object)DBNull.Value);
            command.AddWithValue("@IdAgendamento", log.IdAgendamento ?? (object)DBNull.Value);
            command.AddWithValue("@IdSalao", log.IdSalao);
            command.AddWithValue("@DataHora", log.DataHora);
            command.AddWithValue("@Etapa", log.Etapa);
            command.AddWithValue("@MensagemStatus", log.MensagemStatus);
            command.AddWithValue("@ConteudoXml", log.ConteudoXml ?? (object)DBNull.Value);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task<List<NotaFiscalLog>> ListarPorSalaoAsync(int idSalao)
        {
            var logs = new List<NotaFiscalLog>();

            string query = @"
                SELECT IdLog, IdNotaFiscal, IdAgendamento, IdSalao, DataHora, Etapa, MensagemStatus, ConteudoXml
                FROM CorteCor_NotaFiscalLog
                WHERE IdSalao = @IdSalao
                ORDER BY DataHora DESC;"; // Most recent first

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                logs.Add(new NotaFiscalLog
                {
                    IdLog = Guid.Parse(reader["IdLog"].ToString()!),
                    IdNotaFiscal = reader["IdNotaFiscal"] is DBNull ? null : Guid.Parse(reader["IdNotaFiscal"].ToString()!),
                    IdAgendamento = reader["IdAgendamento"] is DBNull ? null : Convert.ToInt32(reader["IdAgendamento"]),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    DataHora = Convert.ToDateTime(reader["DataHora"]),
                    Etapa = reader["Etapa"].ToString()!,
                    MensagemStatus = reader["MensagemStatus"].ToString()!,
                    ConteudoXml = reader["ConteudoXml"] is DBNull ? null : reader["ConteudoXml"].ToString()
                });
            }

            return logs;
        }
        
        public async Task<List<NotaFiscalLog>> ListarPorNotaFiscalAsync(Guid idNotaFiscal, int idSalao)
        {
            var logs = new List<NotaFiscalLog>();

            string query = @"
                SELECT IdLog, IdNotaFiscal, IdAgendamento, IdSalao, DataHora, Etapa, MensagemStatus, ConteudoXml
                FROM CorteCor_NotaFiscalLog
                WHERE IdNotaFiscal = @IdNotaFiscal AND IdSalao = @IdSalao
                ORDER BY DataHora ASC;"; // Chronological

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdNotaFiscal", idNotaFiscal);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                logs.Add(new NotaFiscalLog
                {
                    IdLog = Guid.Parse(reader["IdLog"].ToString()!),
                    IdNotaFiscal = Guid.Parse(reader["IdNotaFiscal"].ToString()!),
                    IdAgendamento = reader["IdAgendamento"] is DBNull ? null : Convert.ToInt32(reader["IdAgendamento"]),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    DataHora = Convert.ToDateTime(reader["DataHora"]),
                    Etapa = reader["Etapa"].ToString()!,
                    MensagemStatus = reader["MensagemStatus"].ToString()!,
                    ConteudoXml = reader["ConteudoXml"] is DBNull ? null : reader["ConteudoXml"].ToString()
                });
            }

            return logs;
        }
    }
}
