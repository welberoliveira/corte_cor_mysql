using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class FinanceiroHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public FinanceiroHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        #region Plano de Contas

        public async Task<List<PlanoContas>> ListarPlanoContasAsync(int idSalao)
        {
            var lista = new List<PlanoContas>();
            string query = @"
SELECT
    IdPlano,
    IdSalao,
    Codigo,
    Descricao,
    Tipo,
    Ativo,
    Nome,
    IdPlanoPai,
    COALESCE(NULLIF(Nivel, 0), 1 + LENGTH(Codigo) - LENGTH(REPLACE(Codigo, '.', ''))) AS Nivel,
    TipoConta,
    NaturezaSaldo,
    COALESCE(AceitaLancamento, 1) AS AceitaLancamento,
    GrupoDRE,
    OrdemDRE
FROM CorteCor_PlanoContas
WHERE IdSalao = @IdSalao
ORDER BY Codigo, Descricao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                lista.Add(new PlanoContas
                {
                    IdPlano = Convert.ToInt32(reader["IdPlano"]),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    Codigo = reader["Codigo"]?.ToString(),
                    Descricao = reader["Descricao"].ToString()!,
                    Tipo = reader["Tipo"].ToString()!,
                    Ativo = Convert.ToBoolean(reader["Ativo"]),
                    Nome = reader["Nome"] is DBNull ? null : reader["Nome"].ToString(),
                    IdPlanoPai = reader["IdPlanoPai"] is DBNull ? null : Convert.ToInt32(reader["IdPlanoPai"]),
                    Nivel = reader["Nivel"] is DBNull ? 0 : Convert.ToInt32(reader["Nivel"]),
                    TipoConta = reader["TipoConta"] is DBNull ? null : reader["TipoConta"].ToString(),
                    NaturezaSaldo = reader["NaturezaSaldo"] is DBNull ? null : reader["NaturezaSaldo"].ToString(),
                    AceitaLancamento = !(reader["AceitaLancamento"] is DBNull) && Convert.ToBoolean(reader["AceitaLancamento"]),
                    GrupoDRE = reader["GrupoDRE"] is DBNull ? null : reader["GrupoDRE"].ToString(),
                    OrdemDRE = reader["OrdemDRE"] is DBNull ? null : Convert.ToInt32(reader["OrdemDRE"])
                });
            }
            return lista;
        }

        public async Task SavePlanoContasAsync(PlanoContas plano)
        {
            string query = plano.IdPlano == 0
                ? @"
INSERT INTO CorteCor_PlanoContas
    (IdSalao, Codigo, Descricao, Tipo, Ativo, Nome, IdPlanoPai, Nivel, TipoConta, NaturezaSaldo, AceitaLancamento, GrupoDRE, OrdemDRE)
VALUES
    (@IdSalao, @Codigo, @Descricao, @Tipo, @Ativo, @Nome, @IdPlanoPai, @Nivel, @TipoConta, @NaturezaSaldo, @AceitaLancamento, @GrupoDRE, @OrdemDRE);"
                : @"
UPDATE CorteCor_PlanoContas
SET Codigo = @Codigo,
    Descricao = @Descricao,
    Tipo = @Tipo,
    Ativo = @Ativo,
    Nome = @Nome,
    IdPlanoPai = @IdPlanoPai,
    Nivel = @Nivel,
    TipoConta = @TipoConta,
    NaturezaSaldo = @NaturezaSaldo,
    AceitaLancamento = @AceitaLancamento,
    GrupoDRE = @GrupoDRE,
    OrdemDRE = @OrdemDRE
WHERE IdPlano = @IdPlano
  AND IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            
            if (plano.IdPlano > 0) command.AddWithValue("@IdPlano", plano.IdPlano);
            command.AddWithValue("@IdSalao", plano.IdSalao);
            command.AddWithValue("@Codigo", plano.Codigo ?? (object)DBNull.Value);
            command.AddWithValue("@Descricao", plano.Descricao);
            command.AddWithValue("@Tipo", plano.Tipo);
            command.AddWithValue("@Ativo", plano.Ativo);
            command.AddWithValue("@Nome", string.IsNullOrWhiteSpace(plano.Nome) ? (object)DBNull.Value : plano.Nome);
            command.AddWithValue("@IdPlanoPai", plano.IdPlanoPai ?? (object)DBNull.Value);
            command.AddWithValue("@Nivel", plano.Nivel);
            command.AddWithValue("@TipoConta", string.IsNullOrWhiteSpace(plano.TipoConta) ? (object)DBNull.Value : plano.TipoConta);
            command.AddWithValue("@NaturezaSaldo", string.IsNullOrWhiteSpace(plano.NaturezaSaldo) ? (object)DBNull.Value : plano.NaturezaSaldo);
            command.AddWithValue("@AceitaLancamento", plano.AceitaLancamento);
            command.AddWithValue("@GrupoDRE", string.IsNullOrWhiteSpace(plano.GrupoDRE) ? (object)DBNull.Value : plano.GrupoDRE);
            command.AddWithValue("@OrdemDRE", plano.OrdemDRE ?? (object)DBNull.Value);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        #endregion

        #region Contas Caixa

        public async Task<List<ContaCaixa>> ListarContasCaixaAsync(int idSalao)
        {
            var lista = new List<ContaCaixa>();
            string query = "SELECT IdConta, IdSalao, Nome, Tipo, Banco, Agencia, Conta, SaldoInicial, Ativo FROM CorteCor_ContaCaixa WHERE IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", idSalao);

            using var reader = await Task.Run(() => command.ExecuteReader());
            while (reader.Read())
            {
                lista.Add(new ContaCaixa
                {
                    IdConta = Convert.ToInt32(reader["IdConta"]),
                    IdSalao = Convert.ToInt32(reader["IdSalao"]),
                    Nome = reader["Nome"].ToString()!,
                    Tipo = reader["Tipo"]?.ToString(),
                    Banco = reader["Banco"]?.ToString(),
                    Agencia = reader["Agencia"]?.ToString(),
                    Conta = reader["Conta"]?.ToString(),
                    SaldoInicial = Convert.ToDecimal(reader["SaldoInicial"]),
                    Ativo = Convert.ToBoolean(reader["Ativo"])
                });
            }
            return lista;
        }

        public async Task SaveContaCaixaAsync(ContaCaixa conta)
        {
            string query = conta.IdConta == 0
                ? "INSERT INTO CorteCor_ContaCaixa (IdSalao, Nome, Tipo, Banco, Agencia, Conta, SaldoInicial, Ativo) VALUES (@IdSalao, @Nome, @Tipo, @Banco, @Agencia, @Conta, @SaldoInicial, @Ativo);"
                : "UPDATE CorteCor_ContaCaixa SET Nome = @Nome, Tipo = @Tipo, Banco = @Banco, Agencia = @Agencia, Conta = @Conta, SaldoInicial = @SaldoInicial, Ativo = @Ativo WHERE IdConta = @IdConta AND IdSalao = @IdSalao;";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);

            if (conta.IdConta > 0) command.AddWithValue("@IdConta", conta.IdConta);
            command.AddWithValue("@IdSalao", conta.IdSalao);
            command.AddWithValue("@Nome", conta.Nome);
            command.AddWithValue("@Tipo", conta.Tipo ?? (object)DBNull.Value);
            command.AddWithValue("@Banco", conta.Banco ?? (object)DBNull.Value);
            command.AddWithValue("@Agencia", conta.Agencia ?? (object)DBNull.Value);
            command.AddWithValue("@Conta", conta.Conta ?? (object)DBNull.Value);
            command.AddWithValue("@SaldoInicial", conta.SaldoInicial);
            command.AddWithValue("@Ativo", conta.Ativo);

            await Task.Run(() => command.ExecuteNonQuery());
        }

        #endregion
    }
}
