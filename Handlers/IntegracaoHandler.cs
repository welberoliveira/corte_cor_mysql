using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class IntegracaoHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public IntegracaoHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        #region Configuração Geral

        public async Task<ConfigGeral?> ObterConfigGeralAsync(int idSalao)
        {
            string query = "SELECT IdSalao, NomeFantasia, LogoUrl, TemaCor, ModoPDV, ModoEstoque, AgendamentoOnline, MinutosAntecedencia, DataAtualizacao FROM CorteCor_ConfigGeral WHERE IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return new ConfigGeral
                {
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    NomeFantasia = reader["NomeFantasia"]?.ToString(),
                    LogoUrl = reader["LogoUrl"]?.ToString(),
                    TemaCor = reader["TemaCor"].ToString()!,
                    ModoPDV = Convert.ToBoolean(reader["ModoPDV"]),
                    ModoEstoque = Convert.ToBoolean(reader["ModoEstoque"]),
                    AgendamentoOnline = Convert.ToBoolean(reader["AgendamentoOnline"]),
                    MinutosAntecedencia = Convert.ToInt32(reader["MinutosAntecedencia"]),
                    DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
                };
            }
            return null;
        }

        public async Task SalvarConfigGeralAsync(ConfigGeral config)
        {
            string query = @"
                INSERT INTO CorteCor_ConfigGeral
                    (IdSalao, NomeFantasia, LogoUrl, TemaCor, ModoPDV, ModoEstoque, AgendamentoOnline, MinutosAntecedencia, DataAtualizacao)
                VALUES
                    (@IdSalao, @NomeFantasia, @LogoUrl, @TemaCor, @ModoPDV, @ModoEstoque, @AgendamentoOnline, @MinutosAntecedencia, NOW())
                ON DUPLICATE KEY UPDATE
                    NomeFantasia = VALUES(NomeFantasia),
                    LogoUrl = VALUES(LogoUrl),
                    TemaCor = VALUES(TemaCor),
                    ModoPDV = VALUES(ModoPDV),
                    ModoEstoque = VALUES(ModoEstoque),
                    AgendamentoOnline = VALUES(AgendamentoOnline),
                    MinutosAntecedencia = VALUES(MinutosAntecedencia),
                    DataAtualizacao = NOW();";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", config.IdSalao);
            command.AddWithValue("@NomeFantasia", config.NomeFantasia ?? (object)DBNull.Value);
            command.AddWithValue("@LogoUrl", config.LogoUrl ?? (object)DBNull.Value);
            command.AddWithValue("@TemaCor", config.TemaCor);
            command.AddWithValue("@ModoPDV", config.ModoPDV);
            command.AddWithValue("@ModoEstoque", config.ModoEstoque);
            command.AddWithValue("@AgendamentoOnline", config.AgendamentoOnline);
            command.AddWithValue("@MinutosAntecedencia", config.MinutosAntecedencia);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        #endregion

        #region Configuração Pix

        public async Task<ConfigPix?> ObterConfigPixAsync(int idSalao)
        {
            string query = "SELECT IdSalao, ChavePix, PSP, ClientId, ClientSecret, Certificado, Ativo FROM CorteCor_ConfigPix WHERE IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            if (reader.Read())
            {
                return new ConfigPix
                {
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    ChavePix = reader["ChavePix"]?.ToString(),
                    PSP = reader["PSP"]?.ToString(),
                    ClientId = reader["ClientId"]?.ToString(),
                    ClientSecret = reader["ClientSecret"]?.ToString(),
                    Certificado = reader["Certificado"] as byte[],
                    Ativo = Convert.ToBoolean(reader["Ativo"])
                };
            }
            return null;
        }

        public async Task SalvarConfigPixAsync(ConfigPix config)
        {
            string query = @"
                INSERT INTO CorteCor_ConfigPix
                    (IdSalao, ChavePix, PSP, ClientId, ClientSecret, Certificado, Ativo)
                VALUES
                    (@IdSalao, @ChavePix, @PSP, @ClientId, @ClientSecret, @Certificado, @Ativo)
                ON DUPLICATE KEY UPDATE
                    ChavePix = VALUES(ChavePix),
                    PSP = VALUES(PSP),
                    ClientId = VALUES(ClientId),
                    ClientSecret = VALUES(ClientSecret),
                    Certificado = VALUES(Certificado),
                    Ativo = VALUES(Ativo);";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", config.IdSalao);
            command.AddWithValue("@ChavePix", config.ChavePix ?? (object)DBNull.Value);
            command.AddWithValue("@PSP", config.PSP ?? (object)DBNull.Value);
            command.AddWithValue("@ClientId", config.ClientId ?? (object)DBNull.Value);
            command.AddWithValue("@ClientSecret", config.ClientSecret ?? (object)DBNull.Value);
            command.AddWithValue("@Certificado", config.Certificado ?? (object)DBNull.Value);
            command.AddWithValue("@Ativo", config.Ativo);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        #endregion

        #region API Access

        public async Task<List<ConfigApi>> ListarApisAsync(int idSalao)
        {
            var lista = new List<ConfigApi>();
            string query = "SELECT IdApi, IdSalao, NomeApp, ApiKey, DataCriacao, UltimoAcesso, Ativo FROM CorteCor_ConfigApi WHERE IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                lista.Add(new ConfigApi
                {
                    IdApi = Convert.ToInt32(reader["IdApi"]),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    NomeApp = reader["NomeApp"]?.ToString(),
                    ApiKey = Guid.Parse(reader["ApiKey"].ToString()!),
                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                    UltimoAcesso = reader["UltimoAcesso"] is DBNull ? null : Convert.ToDateTime(reader["UltimoAcesso"]),
                    Ativo = Convert.ToBoolean(reader["Ativo"])
                });
            }
            return lista;
        }

        public async Task SalvarConfigApiAsync(ConfigApi config)
        {
            string query = config.IdApi == 0
                ? "INSERT INTO CorteCor_ConfigApi (IdSalao, NomeApp, ApiKey, DataCriacao, Ativo) VALUES (@IdSalao, @NomeApp, @ApiKey, @DataCriacao, @Ativo);"
                : "UPDATE CorteCor_ConfigApi SET NomeApp = @NomeApp, Ativo = @Ativo WHERE IdApi = @IdApi AND IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (config.IdApi > 0) command.AddWithValue("@IdApi", config.IdApi);
            command.AddWithValue("@IdSalao", config.IdSalao);
            command.AddWithValue("@NomeApp", config.NomeApp ?? (object)DBNull.Value);
            command.AddWithValue("@ApiKey", config.ApiKey);
            command.AddWithValue("@DataCriacao", config.DataCriacao);
            command.AddWithValue("@Ativo", config.Ativo);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        #endregion
    }
}
