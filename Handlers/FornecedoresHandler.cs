using System.Data;
using System.Data.SqlClient;
using Dapper;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class FornecedoresHandler
    {
        private readonly IDatabaseHandler _databaseHandler;

        public FornecedoresHandler(IDatabaseHandler databaseHandler)
        {
            _databaseHandler = databaseHandler;
        }

        private IDbConnection GetConnection() => _databaseHandler.GetConnection();

        #region Email
        public IEnumerable<FornecedorEmail> ObterEmails()
        {
            using var conn = GetConnection();
            return conn.Query<FornecedorEmail>("SELECT * FROM CorteCor_FornecedoresEmail");
        }

        public virtual FornecedorEmail ObterEmailAtivo()
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorEmail>("SELECT * FROM CorteCor_FornecedoresEmail WHERE Ativo = 1");
        }

        public FornecedorEmail ObterEmailPorId(int id)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorEmail>("SELECT * FROM CorteCor_FornecedoresEmail WHERE IdFornecedor = @Id", new { Id = id });
        }

        public void SalvarEmail(FornecedorEmail model)
        {
            using var conn = GetConnection();
            if (model.IdFornecedor == 0)
            {
                var sql = @"INSERT INTO CorteCor_FornecedoresEmail (Nome, ApiKey, ApiSecret, Endpoint, RemetenteNome, RemetenteEmail, Ativo, DataCriacao, DataAtualizacao) 
                            VALUES (@Nome, @ApiKey, @ApiSecret, @Endpoint, @RemetenteNome, @RemetenteEmail, @Ativo, GETDATE(), GETDATE())";
                conn.Execute(sql, model);
            }
            else
            {
                var sql = @"UPDATE CorteCor_FornecedoresEmail 
                            SET Nome = @Nome, ApiKey = @ApiKey, ApiSecret = @ApiSecret, Endpoint = @Endpoint, 
                                RemetenteNome = @RemetenteNome, RemetenteEmail = @RemetenteEmail, Ativo = @Ativo, DataAtualizacao = GETDATE()
                            WHERE IdFornecedor = @IdFornecedor";
                conn.Execute(sql, model);
            }

            if (model.Ativo)
            {
                AtivarEmail(model.IdFornecedor); // Desativa outros e garante este como ativo
            }
        }

        public void AtivarEmail(int id)
        {
            using var conn = GetConnection();
            // Desativa todos
            conn.Execute("UPDATE CorteCor_FornecedoresEmail SET Ativo = 0");
            // Ativa o selecionado (se ID > 0, pois ID 0 pode ser usado para desativar todos se desejado, mas aqui assumimos que um deve ser ativado)
            if (id > 0)
                conn.Execute("UPDATE CorteCor_FornecedoresEmail SET Ativo = 1 WHERE IdFornecedor = @Id", new { Id = id });
        }
        
        public void ExcluirEmail(int id)
        {
             using var conn = GetConnection();
             conn.Execute("DELETE FROM CorteCor_FornecedoresEmail WHERE IdFornecedor = @Id", new { Id = id });
        }
        #endregion

        #region SMS
        public IEnumerable<FornecedorSMS> ObterSMS()
        {
            using var conn = GetConnection();
            return conn.Query<FornecedorSMS>("SELECT * FROM CorteCor_FornecedoresSMS");
        }

        public virtual FornecedorSMS ObterSMSAtivo()
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorSMS>("SELECT * FROM CorteCor_FornecedoresSMS WHERE Ativo = 1");
        }

        public FornecedorSMS ObterSMSPorId(int id)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorSMS>("SELECT * FROM CorteCor_FornecedoresSMS WHERE IdFornecedor = @Id", new { Id = id });
        }

        public void SalvarSMS(FornecedorSMS model)
        {
            using var conn = GetConnection();
            if (model.IdFornecedor == 0)
            {
                var sql = @"INSERT INTO CorteCor_FornecedoresSMS (Nome, ApiKey, ApiSecret, Endpoint, Remetente, Ativo, DataCriacao, DataAtualizacao) 
                            VALUES (@Nome, @ApiKey, @ApiSecret, @Endpoint, @Remetente, @Ativo, GETDATE(), GETDATE())";
                conn.Execute(sql, model);
            }
            else
            {
                var sql = @"UPDATE CorteCor_FornecedoresSMS 
                            SET Nome = @Nome, ApiKey = @ApiKey, ApiSecret = @ApiSecret, Endpoint = @Endpoint, 
                                Remetente = @Remetente, Ativo = @Ativo, DataAtualizacao = GETDATE()
                            WHERE IdFornecedor = @IdFornecedor";
                conn.Execute(sql, model);
            }

            if (model.Ativo)
            {
                AtivarSMS(model.IdFornecedor);
            }
        }

        public void AtivarSMS(int id)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE CorteCor_FornecedoresSMS SET Ativo = 0");
            if (id > 0)
                conn.Execute("UPDATE CorteCor_FornecedoresSMS SET Ativo = 1 WHERE IdFornecedor = @Id", new { Id = id });
        }

        public void ExcluirSMS(int id)
        {
             using var conn = GetConnection();
             conn.Execute("DELETE FROM CorteCor_FornecedoresSMS WHERE IdFornecedor = @Id", new { Id = id });
        }
        #endregion

        #region Whatsapp
        public IEnumerable<FornecedorWhatsapp> ObterWhatsapp()
        {
            using var conn = GetConnection();
            return conn.Query<FornecedorWhatsapp>("SELECT * FROM CorteCor_FornecedoresWhatsapp");
        }

        public virtual FornecedorWhatsapp ObterWhatsappAtivo()
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorWhatsapp>("SELECT * FROM CorteCor_FornecedoresWhatsapp WHERE Ativo = 1");
        }

        public FornecedorWhatsapp ObterWhatsappPorId(int id)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<FornecedorWhatsapp>("SELECT * FROM CorteCor_FornecedoresWhatsapp WHERE IdFornecedor = @Id", new { Id = id });
        }

        public void SalvarWhatsapp(FornecedorWhatsapp model)
        {
            using var conn = GetConnection();
            if (model.IdFornecedor == 0)
            {
                var sql = @"INSERT INTO CorteCor_FornecedoresWhatsapp (Nome, ApiKey, ApiSecret, Endpoint, InstanceId, Token, Ativo, DataCriacao, DataAtualizacao) 
                            VALUES (@Nome, @ApiKey, @ApiSecret, @Endpoint, @InstanceId, @Token, @Ativo, GETDATE(), GETDATE())";
                conn.Execute(sql, model);
            }
            else
            {
                var sql = @"UPDATE CorteCor_FornecedoresWhatsapp 
                            SET Nome = @Nome, ApiKey = @ApiKey, ApiSecret = @ApiSecret, Endpoint = @Endpoint, 
                                InstanceId = @InstanceId, Token = @Token, Ativo = @Ativo, DataAtualizacao = GETDATE()
                            WHERE IdFornecedor = @IdFornecedor";
                conn.Execute(sql, model);
            }

            if (model.Ativo)
            {
                AtivarWhatsapp(model.IdFornecedor);
            }
        }

        public void AtivarWhatsapp(int id)
        {
            using var conn = GetConnection();
            conn.Execute("UPDATE CorteCor_FornecedoresWhatsapp SET Ativo = 0");
            if (id > 0)
                conn.Execute("UPDATE CorteCor_FornecedoresWhatsapp SET Ativo = 1 WHERE IdFornecedor = @Id", new { Id = id });
        }

        public void ExcluirWhatsapp(int id)
        {
             using var conn = GetConnection();
             conn.Execute("DELETE FROM CorteCor_FornecedoresWhatsapp WHERE IdFornecedor = @Id", new { Id = id });
        }
        #endregion
    }
}

