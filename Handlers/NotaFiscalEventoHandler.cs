using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class NotaFiscalEventoHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public NotaFiscalEventoHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task InserirAsync(NotaFiscalEvento evento)
        {
            string query = @"
                INSERT INTO CorteCor_NotaFiscalEvento (
                    IdEvento, IdNotaFiscal, IdSalao, TipoEvento, Justificativa, 
                    ProtocoloEvento, XmlEnvio, XmlRetorno, Status, DataRegistro
                ) VALUES (
                    @IdEvento, @IdNotaFiscal, @IdSalao, @TipoEvento, @Justificativa, 
                    @ProtocoloEvento, @XmlEnvio, @XmlRetorno, @Status, @DataRegistro
                );";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (evento.IdEvento == Guid.Empty)
                evento.IdEvento = Guid.NewGuid();

            command.AddWithValue("@IdEvento", evento.IdEvento);
            command.AddWithValue("@IdNotaFiscal", evento.IdNotaFiscal);
            command.AddWithValue("@IdSalao", evento.IdSalao);
            command.AddWithValue("@TipoEvento", evento.TipoEvento);
            command.AddWithValue("@Justificativa", evento.Justificativa);
            command.AddWithValue("@ProtocoloEvento", evento.ProtocoloEvento ?? (object)DBNull.Value);
            command.AddWithValue("@XmlEnvio", evento.XmlEnvio ?? (object)DBNull.Value);
            command.AddWithValue("@XmlRetorno", evento.XmlRetorno ?? (object)DBNull.Value);
            command.AddWithValue("@Status", evento.Status);
            command.AddWithValue("@DataRegistro", evento.DataRegistro == default ? DateTime.Now : evento.DataRegistro);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task<List<NotaFiscalEvento>> ListarPorNotaAsync(Guid idNotaFiscal)
        {
            var eventos = new List<NotaFiscalEvento>();

            string query = @"
                SELECT IdEvento, IdNotaFiscal, IdSalao, TipoEvento, Justificativa, 
                       ProtocoloEvento, XmlEnvio, XmlRetorno, Status, DataRegistro
                FROM CorteCor_NotaFiscalEvento
                WHERE IdNotaFiscal = @IdNotaFiscal
                ORDER BY DataRegistro DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdNotaFiscal", idNotaFiscal);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                eventos.Add(new NotaFiscalEvento
                {
                    IdEvento = Guid.Parse(reader["IdEvento"].ToString()),
                    IdNotaFiscal = Guid.Parse(reader["IdNotaFiscal"].ToString()),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    TipoEvento = reader["TipoEvento"].ToString(),
                    Justificativa = reader["Justificativa"].ToString(),
                    ProtocoloEvento = reader["ProtocoloEvento"] is DBNull ? null : reader["ProtocoloEvento"].ToString(),
                    XmlEnvio = reader["XmlEnvio"] is DBNull ? null : reader["XmlEnvio"].ToString(),
                    XmlRetorno = reader["XmlRetorno"] is DBNull ? null : reader["XmlRetorno"].ToString(),
                    Status = reader["Status"].ToString(),
                    DataRegistro = Convert.ToDateTime(reader["DataRegistro"])
                });
            }

            return eventos;
        }
    }
}
