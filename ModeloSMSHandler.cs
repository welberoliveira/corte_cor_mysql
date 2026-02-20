using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static CorteCor.Models;

namespace CorteCor
{
    public class ModeloSMSHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public ModeloSMSHandler(IDatabaseHandler dbHandler)
        {
            _dbHandler = dbHandler;
        }

        public List<ModeloSMS> ListarPorSalao(int idSalao)
        {
            var lista = new List<ModeloSMS>();
            string query = "SELECT * FROM CorteCor_ModeloSMS WHERE IdSalao = @IdSalao AND Ativo = 1";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new ModeloSMS
                        {
                            IdModelo = Convert.ToInt32(reader["IdModelo"]),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            TipoEvento = reader["TipoEvento"].ToString(),
                            Conteudo = reader["Conteudo"].ToString(),
                            Ativo = Convert.ToBoolean(reader["Ativo"]),
                            DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
                        });
                    }
                }
            }
            return lista;
        }

        public ModeloSMS ObterPorId(int idModelo, int idSalao)
        {
            string query = "SELECT * FROM CorteCor_ModeloSMS WHERE IdModelo = @IdModelo AND IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdModelo", idModelo);
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ModeloSMS
                        {
                            IdModelo = Convert.ToInt32(reader["IdModelo"]),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            TipoEvento = reader["TipoEvento"].ToString(),
                            Conteudo = reader["Conteudo"].ToString(),
                            Ativo = Convert.ToBoolean(reader["Ativo"]),
                            DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
                        };
                    }
                }
            }
            return null;
        }

        public ModeloSMS ObterPorEvento(int idSalao, string tipoEvento)
        {
            string query = "SELECT TOP 1 * FROM CorteCor_ModeloSMS WHERE IdSalao = @IdSalao AND TipoEvento = @TipoEvento AND Ativo = 1 ORDER BY DataAtualizacao DESC";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@TipoEvento", tipoEvento);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ModeloSMS
                        {
                            IdModelo = Convert.ToInt32(reader["IdModelo"]),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            TipoEvento = reader["TipoEvento"].ToString(),
                            Conteudo = reader["Conteudo"].ToString(),
                            Ativo = Convert.ToBoolean(reader["Ativo"]),
                            DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
                        };
                    }
                }
            }
            return null;
        }

        public void Salvar(ModeloSMS modelo)
        {
            string query;
            if (modelo.IdModelo == 0)
            {
                query = @"INSERT INTO CorteCor_ModeloSMS (IdSalao, TipoEvento, Conteudo, Ativo, DataAtualizacao)
                          VALUES (@IdSalao, @TipoEvento, @Conteudo, @Ativo, GETDATE())";
            }
            else
            {
                query = @"UPDATE CorteCor_ModeloSMS 
                          SET TipoEvento = @TipoEvento, Conteudo = @Conteudo, Ativo = @Ativo, DataAtualizacao = GETDATE()
                          WHERE IdModelo = @IdModelo AND IdSalao = @IdSalao";
            }

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                if (modelo.IdModelo > 0)
                {
                    command.AddWithValue("@IdModelo", modelo.IdModelo);
                }
                command.AddWithValue("@IdSalao", modelo.IdSalao);
                command.AddWithValue("@TipoEvento", modelo.TipoEvento);
                command.AddWithValue("@Conteudo", modelo.Conteudo);
                command.AddWithValue("@Ativo", modelo.Ativo);

                command.ExecuteNonQuery();
            }
        }

        public void Excluir(int idModelo, int idSalao)
        {
            // Soft delete or hard delete? Since it's config, maybe soft delete via Ativo=0 or hard.
            // Requirement doesn't specify, but safer to set Ativo = 0.
            // However, ModelEmailHandler usually might do hard delete. Let's do Hard delete for now as per "Excluir" naming convention in codebase.
            
            string query = "DELETE FROM CorteCor_ModeloSMS WHERE IdModelo = @IdModelo AND IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdModelo", idModelo);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }
    }
}
