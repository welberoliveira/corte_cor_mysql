using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class NotaFiscalInutilizacaoHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public NotaFiscalInutilizacaoHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task InserirAsync(NotaFiscalInutilizacao inut)
        {
            string query = @"
                INSERT INTO CorteCor_NotaFiscalInutilizacao (
                    IdInutilizacao, IdSalao, Ano, Serie, NumeroInicial, NumeroFinal, TipoNota, 
                    Justificativa, Protocolo, XmlEnvio, XmlRetorno, Status, DataInutilizacao
                ) VALUES (
                    @IdInutilizacao, @IdSalao, @Ano, @Serie, @NumeroInicial, @NumeroFinal, @TipoNota, 
                    @Justificativa, @Protocolo, @XmlEnvio, @XmlRetorno, @Status, @DataInutilizacao
                );";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (inut.IdInutilizacao == Guid.Empty)
                inut.IdInutilizacao = Guid.NewGuid();

            command.AddWithValue("@IdInutilizacao", inut.IdInutilizacao);
            command.AddWithValue("@IdSalao", inut.IdSalao);
            command.AddWithValue("@Ano", inut.Ano);
            command.AddWithValue("@Serie", inut.Serie);
            command.AddWithValue("@NumeroInicial", inut.NumeroInicial);
            command.AddWithValue("@NumeroFinal", inut.NumeroFinal);
            command.AddWithValue("@TipoNota", (inut.Modelo == 55 ? "NF-e" : "NFC-e"));
            command.AddWithValue("@Justificativa", inut.Justificativa);
            command.AddWithValue("@Protocolo", inut.Protocolo ?? (object)DBNull.Value);
            command.AddWithValue("@XmlEnvio", (object)DBNull.Value); // Não salvamos o XML de envio separadamente se preferir, mas está na tabela
            command.AddWithValue("@XmlRetorno", inut.XmlRetorno ?? (object)DBNull.Value);
            command.AddWithValue("@Status", inut.Status ?? "");
            command.AddWithValue("@DataInutilizacao", inut.DataInutilizacao == default ? DateTime.Now : inut.DataInutilizacao);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task<List<NotaFiscalInutilizacao>> ListarPorSalaoAsync(int idSalao)
        {
            var lista = new List<NotaFiscalInutilizacao>();
            string query = "SELECT * FROM CorteCor_NotaFiscalInutilizacao WHERE IdSalao = @IdSalao ORDER BY DataInutilizacao DESC";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                lista.Add(new NotaFiscalInutilizacao
                {
                    IdInutilizacao = Guid.Parse(reader["IdInutilizacao"].ToString()),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    Ano = Convert.ToInt32(reader["Ano"]),
                    Serie = Convert.ToInt32(reader["Serie"]),
                    NumeroInicial = Convert.ToInt32(reader["NumeroInicial"]),
                    NumeroFinal = Convert.ToInt32(reader["NumeroFinal"]),
                    Modelo = reader["TipoNota"].ToString() == "NF-e" ? 55 : 65,
                    Justificativa = reader["Justificativa"].ToString(),
                    Protocolo = reader["Protocolo"]?.ToString(),
                    XmlRetorno = reader["XmlRetorno"]?.ToString(),
                    Status = reader["Status"]?.ToString(),
                    DataInutilizacao = Convert.ToDateTime(reader["DataInutilizacao"])
                });
            }
            return lista;
        }
    }
}
