using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class NotaFiscalHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public NotaFiscalHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task<List<NotaFiscal>> ListarPorSalaoAsync(int idSalao)
        {
            var notas = new List<NotaFiscal>();

            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao
                ORDER BY DataEmissao DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                notas.Add(MapFromReader(reader));
            }

            return notas;
        }

        public async Task<NotaFiscal?> ObterPorIdAsync(Guid idNotaFiscal, int idSalao)
        {
            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdNotaFiscal = @IdNotaFiscal AND IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdNotaFiscal", idNotaFiscal);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        private NotaFiscal MapFromReader(IDataReader reader)
        {
            return new NotaFiscal
            {
                IdNotaFiscal = Guid.Parse(reader["IdNotaFiscal"].ToString()),
                IdSalao = Convert.ToInt32(reader["IdSalao"]),
                IdAgendamento = reader["IdAgendamento"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdAgendamento"]),
                IdVendaProduto = reader["IdVendaProduto"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdVendaProduto"]),
                TipoNota = reader["TipoNota"] is DBNull ? "" : reader["TipoNota"].ToString(),
                Ambiente = Convert.ToInt32(reader["Ambiente"]),
                Numero = Convert.ToInt32(reader["Numero"]),
                Serie = Convert.ToInt32(reader["Serie"]),
                ValorTotal = Convert.ToDecimal(reader["ValorTotal"]),
                Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                ChaveAcesso = reader["ChaveAcesso"] is DBNull ? null : reader["ChaveAcesso"].ToString(),
                NumeroRecibo = reader["NumeroRecibo"] is DBNull ? null : reader["NumeroRecibo"].ToString(),
                ProtocoloAutorizacao = reader["ProtocoloAutorizacao"] is DBNull ? null : reader["ProtocoloAutorizacao"].ToString(),
                JustificativaRejeicao = reader["JustificativaRejeicao"] is DBNull ? null : reader["JustificativaRejeicao"].ToString(),
                XmlEnvio = reader["XmlEnvio"] is DBNull ? null : reader["XmlEnvio"].ToString(),
                XmlRetorno = reader["XmlRetorno"] is DBNull ? null : reader["XmlRetorno"].ToString(),
                DataEmissao = Convert.ToDateTime(reader["DataEmissao"]),
                DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
            };
        }

        public async Task InserirAsync(NotaFiscal nota)
        {
            string query = @"
                INSERT INTO CorteCor_NotaFiscal (
                    IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                    Numero, Serie, ValorTotal, Status, ChaveAcesso, NumeroRecibo, 
                    ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                    DataEmissao, DataAtualizacao
                ) VALUES (
                    @IdNotaFiscal, @IdSalao, @IdAgendamento, @IdVendaProduto, @TipoNota, @Ambiente, 
                    @Numero, @Serie, @ValorTotal, @Status, @ChaveAcesso, @NumeroRecibo, 
                    @ProtocoloAutorizacao, @JustificativaRejeicao, @XmlEnvio, @XmlRetorno, 
                    @DataEmissao, @DataAtualizacao
                );";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (nota.IdNotaFiscal == Guid.Empty)
                nota.IdNotaFiscal = Guid.NewGuid();

            command.AddWithValue("@IdNotaFiscal", nota.IdNotaFiscal);
            command.AddWithValue("@IdSalao", nota.IdSalao);
            command.AddWithValue("@IdAgendamento", nota.IdAgendamento ?? (object)DBNull.Value);
            command.AddWithValue("@IdVendaProduto", nota.IdVendaProduto ?? (object)DBNull.Value);
            command.AddWithValue("@TipoNota", nota.TipoNota);
            command.AddWithValue("@Ambiente", nota.Ambiente);
            command.AddWithValue("@Numero", nota.Numero);
            command.AddWithValue("@Serie", nota.Serie);
            command.AddWithValue("@ValorTotal", nota.ValorTotal);
            command.AddWithValue("@Status", nota.Status ?? "Pendente");
            command.AddWithValue("@ChaveAcesso", nota.ChaveAcesso ?? (object)DBNull.Value);
            command.AddWithValue("@NumeroRecibo", nota.NumeroRecibo ?? (object)DBNull.Value);
            command.AddWithValue("@ProtocoloAutorizacao", nota.ProtocoloAutorizacao ?? (object)DBNull.Value);
            command.AddWithValue("@JustificativaRejeicao", nota.JustificativaRejeicao ?? (object)DBNull.Value);
            command.AddWithValue("@XmlEnvio", nota.XmlEnvio ?? (object)DBNull.Value);
            command.AddWithValue("@XmlRetorno", nota.XmlRetorno ?? (object)DBNull.Value);
            command.AddWithValue("@DataEmissao", nota.DataEmissao == default ? DateTime.Now : nota.DataEmissao);
            command.AddWithValue("@DataAtualizacao", DateTime.Now);

            await Task.Run(() => command.ExecuteNonQuery());
        }
    }
}
