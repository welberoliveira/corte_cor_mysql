using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using CorteCor.Models;
using Microsoft.Extensions.Configuration;

namespace CorteCor.Handlers
{
    public class SalaoConfigFiscalHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public SalaoConfigFiscalHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public async Task<SalaoConfigFiscal?> ObterPorSalaoAsync(int idSalao)
        {
            string query = @"
                SELECT IdConfigFiscal, IdSalao, Cnpj, RazaoSocial, InscricaoEstadual, InscricaoMunicipal, 
                       Ambiente, EmissaoAutomatica, CodigoMunicipioIBGE, CodigoUFIBGE, RegimeTributario, 
                       CertificadoPfx, CertificadoSenha, CertificadoValidade, DataAtualizacao
                FROM CorteCor_SalaoConfigFiscal
                WHERE IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader()); // ExecuteReaderAsync isn't guaranteed if not async base
            if (reader.Read())
            {
                return new SalaoConfigFiscal
                {
                    IdConfigFiscal = Guid.Parse(reader["IdConfigFiscal"].ToString()),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    Cnpj = reader["Cnpj"] is DBNull ? "" : reader["Cnpj"].ToString(),
                    RazaoSocial = reader["RazaoSocial"] is DBNull ? "" : reader["RazaoSocial"].ToString(),
                    InscricaoEstadual = reader["InscricaoEstadual"] is DBNull ? null : reader["InscricaoEstadual"].ToString(),
                    InscricaoMunicipal = reader["InscricaoMunicipal"] is DBNull ? null : reader["InscricaoMunicipal"].ToString(),
                    Ambiente = Convert.ToInt32(reader["Ambiente"]),
                    EmissaoAutomatica = reader["EmissaoAutomatica"] is DBNull ? false : Convert.ToBoolean(reader["EmissaoAutomatica"]),
                    CodigoMunicipioIBGE = reader["CodigoMunicipioIBGE"] is DBNull ? 0 : Convert.ToInt32(reader["CodigoMunicipioIBGE"]),
                    CodigoUFIBGE = reader["CodigoUFIBGE"] is DBNull ? 0 : Convert.ToInt32(reader["CodigoUFIBGE"]),
                    RegimeTributario = reader["RegimeTributario"] is DBNull ? 0 : Convert.ToInt32(reader["RegimeTributario"]),
                    CertificadoPfx = reader["CertificadoPfx"] as byte[],
                    CertificadoSenha = reader["CertificadoSenha"] as byte[],
                    CertificadoValidade = reader["CertificadoValidade"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["CertificadoValidade"]),
                    DataAtualizacao = reader["DataAtualizacao"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["DataAtualizacao"])
                };
            }

            return null;
        }

        public async Task AddAsync(SalaoConfigFiscal config)
        {
            string query = @"
                INSERT INTO CorteCor_SalaoConfigFiscal (
                    IdConfigFiscal, IdSalao, Cnpj, RazaoSocial, InscricaoEstadual, InscricaoMunicipal, 
                    Ambiente, EmissaoAutomatica, CodigoMunicipioIBGE, CodigoUFIBGE, RegimeTributario, 
                    CertificadoPfx, CertificadoSenha, CertificadoValidade, DataAtualizacao
                ) VALUES (
                    @IdConfigFiscal, @IdSalao, @Cnpj, @RazaoSocial, @InscricaoEstadual, @InscricaoMunicipal, 
                    @Ambiente, @EmissaoAutomatica, @CodigoMunicipioIBGE, @CodigoUFIBGE, @RegimeTributario, 
                    @CertificadoPfx, @CertificadoSenha, @CertificadoValidade, @DataAtualizacao
                );";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (config.IdConfigFiscal == Guid.Empty)
                config.IdConfigFiscal = Guid.NewGuid();

            command.AddWithValue("@IdConfigFiscal", config.IdConfigFiscal);
            command.AddWithValue("@IdSalao", config.IdSalao);
            command.AddWithValue("@Cnpj", config.Cnpj ?? (object)DBNull.Value);
            command.AddWithValue("@RazaoSocial", config.RazaoSocial ?? (object)DBNull.Value);
            command.AddWithValue("@InscricaoEstadual", config.InscricaoEstadual ?? (object)DBNull.Value);
            command.AddWithValue("@InscricaoMunicipal", config.InscricaoMunicipal ?? (object)DBNull.Value);
            command.AddWithValue("@Ambiente", config.Ambiente);
            command.AddWithValue("@EmissaoAutomatica", config.EmissaoAutomatica ? 1 : 0);
            command.AddWithValue("@CodigoMunicipioIBGE", config.CodigoMunicipioIBGE);
            command.AddWithValue("@CodigoUFIBGE", config.CodigoUFIBGE);
            command.AddWithValue("@RegimeTributario", config.RegimeTributario);
            command.AddWithValue("@CertificadoPfx", config.CertificadoPfx ?? (object)DBNull.Value);
            command.AddWithValue("@CertificadoSenha", config.CertificadoSenha ?? (object)DBNull.Value);
            command.AddWithValue("@CertificadoValidade", config.CertificadoValidade ?? (object)DBNull.Value);
            command.AddWithValue("@DataAtualizacao", config.DataAtualizacao);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        public async Task UpdateAsync(SalaoConfigFiscal config)
        {
            string query = @"
                UPDATE CorteCor_SalaoConfigFiscal SET 
                    IdSalao = @IdSalao, 
                    Cnpj = @Cnpj, 
                    RazaoSocial = @RazaoSocial, 
                    InscricaoEstadual = @InscricaoEstadual, 
                    InscricaoMunicipal = @InscricaoMunicipal, 
                    Ambiente = @Ambiente, 
                    EmissaoAutomatica = @EmissaoAutomatica,
                    CodigoMunicipioIBGE = @CodigoMunicipioIBGE, 
                    CodigoUFIBGE = @CodigoUFIBGE, 
                    RegimeTributario = @RegimeTributario, 
                    CertificadoPfx = @CertificadoPfx, 
                    CertificadoSenha = @CertificadoSenha, 
                    CertificadoValidade = @CertificadoValidade, 
                    DataAtualizacao = @DataAtualizacao
                WHERE IdConfigFiscal = @IdConfigFiscal;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            command.AddWithValue("@IdSalao", config.IdSalao);
            command.AddWithValue("@Cnpj", config.Cnpj ?? (object)DBNull.Value);
            command.AddWithValue("@RazaoSocial", config.RazaoSocial ?? (object)DBNull.Value);
            command.AddWithValue("@InscricaoEstadual", config.InscricaoEstadual ?? (object)DBNull.Value);
            command.AddWithValue("@InscricaoMunicipal", config.InscricaoMunicipal ?? (object)DBNull.Value);
            command.AddWithValue("@Ambiente", config.Ambiente);
            command.AddWithValue("@EmissaoAutomatica", config.EmissaoAutomatica ? 1 : 0);
            command.AddWithValue("@CodigoMunicipioIBGE", config.CodigoMunicipioIBGE);
            command.AddWithValue("@CodigoUFIBGE", config.CodigoUFIBGE);
            command.AddWithValue("@RegimeTributario", config.RegimeTributario);
            command.AddWithValue("@CertificadoPfx", config.CertificadoPfx ?? (object)DBNull.Value);
            command.AddWithValue("@CertificadoSenha", config.CertificadoSenha ?? (object)DBNull.Value);
            command.AddWithValue("@CertificadoValidade", config.CertificadoValidade ?? (object)DBNull.Value);
            command.AddWithValue("@DataAtualizacao", config.DataAtualizacao);

            command.AddWithValue("@IdConfigFiscal", config.IdConfigFiscal);

            await Task.Run(() => command.ExecuteNonQuery());
        }
    }
}
