using System;
using System.Collections.Generic;
using System.Data;
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
                       CertificadoPfx, CertificadoSenha, CertificadoValidade, DataAtualizacao,
                       TokenNfse, CSC, IdCSC, SerieNFCe, NumeroNFCe, SerieNFSe, NumeroNFSe,
                       RegimeEspecialTributacao, IssExigibilidade, IssRetido,
                       EnderecoLogradouro, EnderecoNumero, EnderecoBairro, EnderecoCep, EnderecoCidade, EnderecoUF, Telefone, Email
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
                    DataAtualizacao = reader["DataAtualizacao"] is DBNull ? DateTime.Now : Convert.ToDateTime(reader["DataAtualizacao"]),
                    TokenNfse = reader["TokenNfse"]?.ToString(),
                    CSC = reader["CSC"]?.ToString(),
                    IdCSC = reader["IdCSC"]?.ToString(),
                    SerieNFCe = reader["SerieNFCe"] is DBNull ? 1 : Convert.ToInt32(reader["SerieNFCe"]),
                    NumeroNFCe = reader["NumeroNFCe"] is DBNull ? 1 : Convert.ToInt32(reader["NumeroNFCe"]),
                    SerieNFSe = reader["SerieNFSe"] is DBNull ? 1 : Convert.ToInt32(reader["SerieNFSe"]),
                    NumeroNFSe = reader["NumeroNFSe"] is DBNull ? 1 : Convert.ToInt32(reader["NumeroNFSe"]),
                    RegimeEspecialTributacao = reader["RegimeEspecialTributacao"] is DBNull ? 0 : Convert.ToInt32(reader["RegimeEspecialTributacao"]),
                    IssExigibilidade = reader["IssExigibilidade"] is DBNull ? 1 : Convert.ToInt32(reader["IssExigibilidade"]),
                    IssRetido = reader["IssRetido"] is DBNull ? 2 : Convert.ToInt32(reader["IssRetido"]),
                    EnderecoLogradouro = reader["EnderecoLogradouro"]?.ToString(),
                    EnderecoNumero = reader["EnderecoNumero"]?.ToString(),
                    EnderecoBairro = reader["EnderecoBairro"]?.ToString(),
                    EnderecoCep = reader["EnderecoCep"]?.ToString(),
                    EnderecoCidade = reader["EnderecoCidade"]?.ToString(),
                    EnderecoUF = reader["EnderecoUF"]?.ToString(),
                    Telefone = reader["Telefone"]?.ToString(),
                    Email = reader["Email"]?.ToString()
                };
            }

            return null;
        }

        public async Task SalvarAsync(SalaoConfigFiscal config)
        {
            var existing = await ObterPorSalaoAsync(config.IdSalao);
            if (existing == null)
            {
                await AddAsync(config);
            }
            else
            {
                config.IdConfigFiscal = existing.IdConfigFiscal;
                await UpdateAsync(config);
            }
        }

        public async Task AddAsync(SalaoConfigFiscal config)
        {
            string query = @"
                INSERT INTO CorteCor_SalaoConfigFiscal (
                    IdConfigFiscal, IdSalao, Cnpj, RazaoSocial, InscricaoEstadual, InscricaoMunicipal, 
                    Ambiente, EmissaoAutomatica, CodigoMunicipioIBGE, CodigoUFIBGE, RegimeTributario, 
                    CertificadoPfx, CertificadoSenha, CertificadoValidade, DataAtualizacao,
                    TokenNfse, CSC, IdCSC, SerieNFCe, NumeroNFCe, SerieNFSe, NumeroNFSe, 
                    RegimeEspecialTributacao, IssExigibilidade, IssRetido,
                    EnderecoLogradouro, EnderecoNumero, EnderecoBairro, EnderecoCep, EnderecoCidade, EnderecoUF, Telefone, Email
                ) VALUES (
                    @IdConfigFiscal, @IdSalao, @Cnpj, @RazaoSocial, @InscricaoEstadual, @InscricaoMunicipal, 
                    @Ambiente, @EmissaoAutomatica, @CodigoMunicipioIBGE, @CodigoUFIBGE, @RegimeTributario, 
                    @CertificadoPfx, @CertificadoSenha, @CertificadoValidade, @DataAtualizacao,
                    @TokenNfse, @CSC, @IdCSC, @SerieNFCe, @NumeroNFCe, @SerieNFSe, @NumeroNFSe,
                    @RegimeEspecialTributacao, @IssExigibilidade, @IssRetido,
                    @EnderecoLogradouro, @EnderecoNumero, @EnderecoBairro, @EnderecoCep, @EnderecoCidade, @EnderecoUF, @Telefone, @Email
                );";

            if (!string.IsNullOrEmpty(config.CertificadoBase64))
            {
                // Converte de Base64 para byte array se um novo valor foi enviado pela UI
                try { config.CertificadoPfx = Convert.FromBase64String(config.CertificadoBase64); } catch { }
            }

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
            command.AddWithBinaryValue("@CertificadoPfx", config.CertificadoPfx);
            command.AddWithBinaryValue("@CertificadoSenha", config.CertificadoSenha);
            command.AddWithValue("@CertificadoValidade", config.CertificadoValidade ?? (object)DBNull.Value);
            command.AddWithValue("@DataAtualizacao", config.DataAtualizacao);
            command.AddWithValue("@TokenNfse", config.TokenNfse ?? (object)DBNull.Value);
            command.AddWithValue("@CSC", config.CSC ?? (object)DBNull.Value);
            command.AddWithValue("@IdCSC", config.IdCSC ?? (object)DBNull.Value);
            command.AddWithValue("@SerieNFCe", config.SerieNFCe);
            command.AddWithValue("@NumeroNFCe", config.NumeroNFCe);
            command.AddWithValue("@SerieNFSe", config.SerieNFSe);
            command.AddWithValue("@NumeroNFSe", config.NumeroNFSe);
            command.AddWithValue("@RegimeEspecialTributacao", config.RegimeEspecialTributacao);
            command.AddWithValue("@IssExigibilidade", config.IssExigibilidade);
            command.AddWithValue("@IssRetido", config.IssRetido);
            command.AddWithValue("@EnderecoLogradouro", config.EnderecoLogradouro ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoNumero", config.EnderecoNumero ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoBairro", config.EnderecoBairro ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoCep", config.EnderecoCep ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoCidade", config.EnderecoCidade ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoUF", config.EnderecoUF ?? (object)DBNull.Value);
            command.AddWithValue("@Telefone", config.Telefone ?? (object)DBNull.Value);
            command.AddWithValue("@Email", config.Email ?? (object)DBNull.Value);

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
                    DataAtualizacao = @DataAtualizacao,
                    TokenNfse = @TokenNfse, 
                    CSC = @CSC, 
                    IdCSC = @IdCSC, 
                    SerieNFCe = @SerieNFCe, 
                    NumeroNFCe = @NumeroNFCe, 
                    SerieNFSe = @SerieNFSe, 
                    NumeroNFSe = @NumeroNFSe, 
                    RegimeEspecialTributacao = @RegimeEspecialTributacao, 
                    IssExigibilidade = @IssExigibilidade, 
                    IssRetido = @IssRetido, 
                    EnderecoLogradouro = @EnderecoLogradouro, 
                    EnderecoNumero = @EnderecoNumero, 
                    EnderecoBairro = @EnderecoBairro, 
                    EnderecoCep = @EnderecoCep, 
                    EnderecoCidade = @EnderecoCidade,
                    EnderecoUF = @EnderecoUF,
                    Telefone = @Telefone, 
                    Email = @Email
                WHERE IdConfigFiscal = @IdConfigFiscal;";

            if (!string.IsNullOrEmpty(config.CertificadoBase64))
            {
                try { config.CertificadoPfx = Convert.FromBase64String(config.CertificadoBase64); } catch { }
            }

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
            command.AddWithBinaryValue("@CertificadoPfx", config.CertificadoPfx);
            command.AddWithBinaryValue("@CertificadoSenha", config.CertificadoSenha);
            command.AddWithValue("@CertificadoValidade", config.CertificadoValidade ?? (object)DBNull.Value);
            command.AddWithValue("@DataAtualizacao", config.DataAtualizacao == default ? DateTime.Now : config.DataAtualizacao);
            command.AddWithValue("@TokenNfse", config.TokenNfse ?? (object)DBNull.Value);
            command.AddWithValue("@CSC", config.CSC ?? (object)DBNull.Value);
            command.AddWithValue("@IdCSC", config.IdCSC ?? (object)DBNull.Value);
            command.AddWithValue("@SerieNFCe", config.SerieNFCe);
            command.AddWithValue("@NumeroNFCe", config.NumeroNFCe);
            command.AddWithValue("@SerieNFSe", config.SerieNFSe);
            command.AddWithValue("@NumeroNFSe", config.NumeroNFSe);
            command.AddWithValue("@RegimeEspecialTributacao", config.RegimeEspecialTributacao);
            command.AddWithValue("@IssExigibilidade", config.IssExigibilidade);
            command.AddWithValue("@IssRetido", config.IssRetido);
            command.AddWithValue("@EnderecoLogradouro", config.EnderecoLogradouro ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoNumero", config.EnderecoNumero ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoBairro", config.EnderecoBairro ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoCep", config.EnderecoCep ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoCidade", config.EnderecoCidade ?? (object)DBNull.Value);
            command.AddWithValue("@EnderecoUF", config.EnderecoUF ?? (object)DBNull.Value);
            command.AddWithValue("@Telefone", config.Telefone ?? (object)DBNull.Value);
            command.AddWithValue("@Email", config.Email ?? (object)DBNull.Value);
            command.AddWithValue("@IdConfigFiscal", config.IdConfigFiscal);

            await Task.Run(() => command.ExecuteNonQuery());
        }
    }
}
