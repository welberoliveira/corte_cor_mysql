using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CorteCor.Models;
using CorteCor.Services;

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
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
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

        public async Task<PagedResult<NotaFiscal>> ListarPorSalaoPaginadoAsync(int idSalao, int page = 1, int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            using var connection = _dbHandler.GetConnection();

            using var countCommand = connection.CreateCommand("SELECT COUNT(1) FROM CorteCor_NotaFiscal WHERE IdSalao = @IdSalao;");
            countCommand.AddWithValue("@IdSalao", idSalao);
            var totalCount = Convert.ToInt32(await Task.Run(() => countCommand.ExecuteScalar()));

            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            if (page > totalPages)
            {
                page = totalPages;
            }

            var result = new PagedResult<NotaFiscal>
            {
                TotalCount = totalCount,
                PageIndex = page,
                PageSize = pageSize
            };

            if (totalCount == 0)
            {
                return result;
            }

            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao
                ORDER BY DataEmissao DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@Offset", (page - 1) * pageSize);
            command.AddWithValue("@PageSize", pageSize);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                result.Items.Add(MapFromReader(reader));
            }

            return result;
        }

        public virtual async Task<List<NotaFiscal>> ListarPorAgendamentoAsync(int idSalao, int idAgendamento)
        {
            var notas = new List<NotaFiscal>();

            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao AND IdAgendamento = @IdAgendamento
                ORDER BY DataEmissao DESC, DataAtualizacao DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@IdAgendamento", idAgendamento);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                notas.Add(MapFromReader(reader));
            }

            return notas;
        }

        public virtual async Task<List<NotaFiscal>> ListarPorVendaAsync(int idSalao, int idVendaProduto)
        {
            var notas = new List<NotaFiscal>();

            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao AND IdVendaProduto = @IdVendaProduto
                ORDER BY DataEmissao DESC, DataAtualizacao DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@IdVendaProduto", idVendaProduto);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                notas.Add(MapFromReader(reader));
            }

            return notas;
        }

        public virtual async Task<NotaFiscal?> ObterNotaAtivaPorAgendamentoAsync(int idSalao, int idAgendamento)
        {
            string query = @"
                SELECT TOP 1
                       IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao
                  AND IdAgendamento = @IdAgendamento
                  AND Status NOT IN (@Cancelada, @Rejeitada)
                ORDER BY DataEmissao DESC, DataAtualizacao DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@IdAgendamento", idAgendamento);
            command.AddWithValue("@Cancelada", NotaFiscalStatus.Cancelada);
            command.AddWithValue("@Rejeitada", NotaFiscalStatus.Rejeitada);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public virtual async Task<NotaFiscal?> ObterNotaAtivaPorVendaAsync(int idSalao, int idVendaProduto)
        {
            string query = @"
                SELECT TOP 1
                       IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao
                  AND IdVendaProduto = @IdVendaProduto
                  AND Status NOT IN (@Cancelada, @Rejeitada)
                ORDER BY DataEmissao DESC, DataAtualizacao DESC;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@IdVendaProduto", idVendaProduto);
            command.AddWithValue("@Cancelada", NotaFiscalStatus.Cancelada);
            command.AddWithValue("@Rejeitada", NotaFiscalStatus.Rejeitada);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<NotaFiscal?> ObterPorChaveAsync(string chaveAcesso, int? idSalao = null)
        {
            // Tenta buscar tanto pela chave estadual quanto pela nacional (NFSe)
            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE (ChaveAcesso = @Chave OR ChaveAcessoNacional = @Chave)";

            if (idSalao.HasValue)
            {
                query += " AND IdSalao = @IdSalao";
            }

            query += ";";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@Chave", chaveAcesso);
            if (idSalao.HasValue)
            {
                command.AddWithValue("@IdSalao", idSalao.Value);
            }

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public async Task<NotaFiscal?> ObterPorIdAsync(Guid idNotaFiscal, int idSalao)
        {
            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
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

        public async Task<List<NotaFiscal>> ObterPorStatusAsync(string status, string tipoNota)
        {
            var notas = new List<NotaFiscal>();

            string query = @"
                SELECT IdNotaFiscal, IdSalao, IdAgendamento, IdVendaProduto, TipoNota, Ambiente, 
                       Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                       ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                       DataEmissao, DataAtualizacao
                FROM CorteCor_NotaFiscal
                WHERE Status = @Status AND TipoNota = @TipoNota;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@Status", status);
            command.AddWithValue("@TipoNota", tipoNota);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                notas.Add(MapFromReader(reader));
            }

            return notas;
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
                ChaveAcessoNacional = reader.GetSchemaTable().Select("ColumnName = 'ChaveAcessoNacional'").Length > 0 && !(reader["ChaveAcessoNacional"] is DBNull) ? reader["ChaveAcessoNacional"].ToString() : null,
                NumeroNFSeNacional = reader.GetSchemaTable().Select("ColumnName = 'NumeroNFSeNacional'").Length > 0 && !(reader["NumeroNFSeNacional"] is DBNull) ? reader["NumeroNFSeNacional"].ToString() : null,
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
                    Numero, Serie, ValorTotal, Status, ChaveAcesso, ChaveAcessoNacional, NumeroNFSeNacional, NumeroRecibo, 
                    ProtocoloAutorizacao, JustificativaRejeicao, XmlEnvio, XmlRetorno, 
                    DataEmissao, DataAtualizacao
                ) VALUES (
                    @IdNotaFiscal, @IdSalao, @IdAgendamento, @IdVendaProduto, @TipoNota, @Ambiente, 
                    @Numero, @Serie, @ValorTotal, @Status, @ChaveAcesso, @ChaveAcessoNacional, @NumeroNFSeNacional, @NumeroRecibo, 
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
            command.AddWithValue("@ChaveAcessoNacional", nota.ChaveAcessoNacional ?? (object)DBNull.Value);
            command.AddWithValue("@NumeroNFSeNacional", nota.NumeroNFSeNacional ?? (object)DBNull.Value);
            command.AddWithValue("@NumeroRecibo", nota.NumeroRecibo ?? (object)DBNull.Value);
            command.AddWithValue("@ProtocoloAutorizacao", nota.ProtocoloAutorizacao ?? (object)DBNull.Value);
            command.AddWithValue("@JustificativaRejeicao", nota.JustificativaRejeicao ?? (object)DBNull.Value);
            command.AddWithValue("@XmlEnvio", nota.XmlEnvio ?? (object)DBNull.Value);
            command.AddWithValue("@XmlRetorno", nota.XmlRetorno ?? (object)DBNull.Value);
            command.AddWithValue("@DataEmissao", nota.DataEmissao == default ? DateTime.Now : nota.DataEmissao);
            command.AddWithValue("@DataAtualizacao", DateTime.Now);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task<int> ObterProximoNumeroAsync(int idSalao, string tipoNota, int serie, int ambiente)
        {
            string query = @"
                SELECT ISNULL(MAX(Numero), 0) + 1
                FROM CorteCor_NotaFiscal
                WHERE IdSalao = @IdSalao AND TipoNota = @TipoNota AND Serie = @Serie AND Ambiente = @Ambiente;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);
            command.AddWithValue("@TipoNota", tipoNota);
            command.AddWithValue("@Serie", serie);
            command.AddWithValue("@Ambiente", ambiente);

            var result = await Task.Run(() => command.ExecuteScalar());
            return Convert.ToInt32(result);
        }

        public async Task UpdateAsync(NotaFiscal nota)
        {
            string query = @"
                UPDATE CorteCor_NotaFiscal 
                SET IdAgendamento = ISNULL(@IdAgendamento, IdAgendamento),
                    IdVendaProduto = ISNULL(@IdVendaProduto, IdVendaProduto),
                    Status = @Status,
                    ChaveAcesso = ISNULL(@ChaveAcesso, ChaveAcesso),
                    ChaveAcessoNacional = ISNULL(@ChaveAcessoNacional, ChaveAcessoNacional),
                    NumeroNFSeNacional = ISNULL(@NumeroNFSeNacional, NumeroNFSeNacional),
                    NumeroRecibo = ISNULL(@NumeroRecibo, NumeroRecibo),
                    ProtocoloAutorizacao = ISNULL(@ProtocoloAutorizacao, ProtocoloAutorizacao),
                    JustificativaRejeicao = ISNULL(@JustificativaRejeicao, JustificativaRejeicao),
                    XmlEnvio = ISNULL(@XmlEnvio, XmlEnvio),
                    XmlRetorno = ISNULL(@XmlRetorno, XmlRetorno),
                    DataAtualizacao = @DataAtualizacao
                WHERE IdNotaFiscal = @IdNotaFiscal;";
                
            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            command.AddWithValue("@IdAgendamento", nota.IdAgendamento ?? (object)DBNull.Value);
            command.AddWithValue("@IdVendaProduto", nota.IdVendaProduto ?? (object)DBNull.Value);
            command.AddWithValue("@Status", nota.Status);
            command.AddWithValue("@ChaveAcesso", nota.ChaveAcesso ?? (object)DBNull.Value);
            command.AddWithValue("@ChaveAcessoNacional", nota.ChaveAcessoNacional ?? (object)DBNull.Value);
            command.AddWithValue("@NumeroNFSeNacional", nota.NumeroNFSeNacional ?? (object)DBNull.Value);
            command.AddWithValue("@NumeroRecibo", nota.NumeroRecibo ?? (object)DBNull.Value);
            command.AddWithValue("@ProtocoloAutorizacao", nota.ProtocoloAutorizacao ?? (object)DBNull.Value);
            command.AddWithValue("@JustificativaRejeicao", nota.JustificativaRejeicao ?? (object)DBNull.Value);
            command.AddWithValue("@XmlEnvio", nota.XmlEnvio ?? (object)DBNull.Value);
            command.AddWithValue("@XmlRetorno", nota.XmlRetorno ?? (object)DBNull.Value);
            command.AddWithValue("@DataAtualizacao", DateTime.Now);
            command.AddWithValue("@IdNotaFiscal", nota.IdNotaFiscal);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task AtualizarAsync(NotaFiscal nota) => await UpdateAsync(nota);
    }
}
