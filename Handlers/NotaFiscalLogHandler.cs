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
                IdSalao = idSalao,
                IdAgendamento = idAgendamento,
                IdNotaFiscal = idNotaFiscal,
                DataHora = DateTime.Now,
                TipoEvento = etapa,
                Mensagem = mensagemStatus,
                RequestPayload = conteudoXml ?? string.Empty
            };

            await InserirAsync(log);
        }

        public async Task InserirAsync(NotaFiscalLog log)
        {
            string query = @"
                INSERT INTO CorteCor_NotaFiscalLog 
                (IdSalao, IdNotaFiscal, IdAgendamento, DataHora, TipoEvento, RequestPayload, ResponsePayload, CodigoErro, Mensagem, Usuario) 
                VALUES 
                (@IdSalao, @IdNotaFiscal, @IdAgendamento, @DataHora, @TipoEvento, @RequestPayload, @ResponsePayload, @CodigoErro, @Mensagem, @Usuario);";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            command.AddWithValue("@IdSalao", log.IdSalao);
            command.AddWithValue("@IdNotaFiscal", log.IdNotaFiscal ?? (object)DBNull.Value);
            command.AddWithValue("@IdAgendamento", log.IdAgendamento ?? (object)DBNull.Value);
            command.AddWithValue("@DataHora", log.DataHora == default ? DateTime.Now : log.DataHora);
            command.AddWithValue("@TipoEvento", string.IsNullOrEmpty(log.TipoEvento) ? (object)DBNull.Value : log.TipoEvento);
            command.AddWithValue("@RequestPayload", string.IsNullOrEmpty(log.RequestPayload) ? (object)DBNull.Value : log.RequestPayload);
            command.AddWithValue("@ResponsePayload", string.IsNullOrEmpty(log.ResponsePayload) ? (object)DBNull.Value : log.ResponsePayload);
            command.AddWithValue("@CodigoErro", string.IsNullOrEmpty(log.CodigoErro) ? (object)DBNull.Value : log.CodigoErro);
            command.AddWithValue("@Mensagem", string.IsNullOrEmpty(log.Mensagem) ? (object)DBNull.Value : log.Mensagem);
            command.AddWithValue("@Usuario", string.IsNullOrEmpty(log.Usuario) ? (object)DBNull.Value : log.Usuario);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task<List<NotaFiscalLog>> ListarPorSalaoAsync(int idSalao, DateTime? dtInicio = null, DateTime? dtFim = null)
        {
            var logs = new List<NotaFiscalLog>();

            var query = "SELECT * FROM CorteCor_NotaFiscalLog WHERE IdSalao = @IdSalao";
            if (dtInicio.HasValue) query += " AND DataHora >= @DtInicio";
            if (dtFim.HasValue) query += " AND DataHora <= @DtFim";
            
            query += " ORDER BY DataHora DESC";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            command.AddWithValue("@IdSalao", idSalao);
            if (dtInicio.HasValue) command.AddWithValue("@DtInicio", dtInicio.Value);
            if (dtFim.HasValue) command.AddWithValue("@DtFim", dtFim.Value.AddDays(1).AddSeconds(-1));

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                logs.Add(MapFromReader(reader));
            }

            return logs;
        }

        public async Task<List<NotaFiscalLog>> ListarPorNotaFiscalAsync(Guid idNotaFiscal, int idSalao)
        {
            var logs = new List<NotaFiscalLog>();

            string query = @"
                SELECT *
                FROM CorteCor_NotaFiscalLog
                WHERE IdNotaFiscal = @IdNotaFiscal AND IdSalao = @IdSalao
                ORDER BY DataHora ASC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdNotaFiscal", idNotaFiscal);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                logs.Add(MapFromReader(reader));
            }

            return logs;
        }

        private NotaFiscalLog MapFromReader(IDataReader reader)
        {
            return new NotaFiscalLog
            {
                IdLog = Convert.ToInt32(reader["IdLog"]),
                IdSalao = Convert.ToInt32(reader["IdSalao"]),
                IdNotaFiscal = reader["IdNotaFiscal"] is DBNull ? null : Guid.Parse(reader["IdNotaFiscal"].ToString()!),
                IdAgendamento = reader["IdAgendamento"] is DBNull ? null : Convert.ToInt32(reader["IdAgendamento"]),
                DataHora = Convert.ToDateTime(reader["DataHora"]),
                TipoEvento = reader["TipoEvento"].ToString()!,
                RequestPayload = reader["RequestPayload"] is DBNull ? string.Empty : reader["RequestPayload"].ToString()!,
                ResponsePayload = reader["ResponsePayload"] is DBNull ? null : reader["ResponsePayload"].ToString(),
                CodigoErro = reader["CodigoErro"] is DBNull ? null : reader["CodigoErro"].ToString(),
                Mensagem = reader["Mensagem"] is DBNull ? string.Empty : reader["Mensagem"].ToString()!,
                Usuario = reader["Usuario"] is DBNull ? null : reader["Usuario"].ToString()
            };
        }
    }
}
