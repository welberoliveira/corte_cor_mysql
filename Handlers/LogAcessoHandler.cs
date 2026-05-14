using CorteCor.Logs;

namespace CorteCor.Handlers
{
    public class LogAcessoHandler
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly Log _logger = new Log();

        public LogAcessoHandler(IDatabaseHandler dbHandler = null)
        {
            _dbHandler = dbHandler ?? new DatabaseHandler();
        }

        /// <summary>
        /// Registra uma tentativa de login (sucesso ou falha) na tabela CorteCor_LogAcessos.
        /// </summary>
        public void Registrar(string usuario, string ipOrigem, string credencialUsada, bool sucesso)
        {
            try
            {
                string query = @"
                    INSERT INTO CorteCor_LogAcessos 
                        (Usuario, DataHora, IP_Origem, CredencialUsada, Sucesso)
                    VALUES 
                        (@Usuario, GETDATE(), @IP_Origem, @CredencialUsada, @Sucesso);";

                using (var connection = _dbHandler.GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.AddWithValue("@Usuario", usuario ?? "");
                    command.AddWithValue("@IP_Origem", ipOrigem ?? "desconhecido");
                    command.AddWithValue("@CredencialUsada", credencialUsada ?? "");
                    command.AddWithValue("@Sucesso", sucesso);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                // Não deve impedir o fluxo de login — apenas logar o erro
                _logger.WriteException(ex);
            }
        }

        /// <summary>
        /// Lista todos os registros de log de acessos (mais recentes primeiro).
        /// </summary>
        public List<CorteCor.Models.LogAcesso> Listar(int top = 200)
        {
            var lista = new List<CorteCor.Models.LogAcesso>();
            try
            {
                string query = $@"
                    SELECT TOP ({top}) Id, Usuario, DataHora, IP_Origem, CredencialUsada, Sucesso
                    FROM CorteCor_LogAcessos
                    ORDER BY DataHora DESC;";

                using (var connection = _dbHandler.GetConnection())
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new CorteCor.Models.LogAcesso
                            {
                                Id = reader["Id"] is DBNull ? 0 : Convert.ToInt32(reader["Id"]),
                                Usuario = reader["Usuario"] is DBNull ? "" : reader["Usuario"].ToString(),
                                DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                                IP_Origem = reader["IP_Origem"] is DBNull ? "" : reader["IP_Origem"].ToString(),
                                CredencialUsada = reader["CredencialUsada"] is DBNull ? "" : reader["CredencialUsada"].ToString(),
                                Sucesso = reader["Sucesso"] is DBNull ? false : Convert.ToBoolean(reader["Sucesso"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteException(ex);
            }
            return lista;
        }
    }
}
