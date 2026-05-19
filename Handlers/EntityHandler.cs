using CorteCor.Logs;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using CorteCor.Models;
using CorteCor;
using CorteCor.Services;
using System.Data;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace CorteCor.Handlers
{

    public abstract class EntityHandler<T>
    {
        protected readonly IDatabaseHandler _dbHandler;
        protected EntityHandler(IDatabaseHandler dbHandler = null)
        {
            _dbHandler = dbHandler ?? new DatabaseHandler();
        }

        public abstract void Cadastrar(T entity);
        public abstract void AtivarDesativar(int id, bool ativar);
        public abstract List<T> Listar();
        public abstract void Excluir(int id);
    }

    public class UsuarioHandler : EntityHandler<Usuario>
    {
        public UsuarioHandler(IDatabaseHandler dbHandler = null) : base(dbHandler)
        {
        }

        public int CadastrarUsuario(Usuario Usuario)
        {
            int novoId = 0;
            string query = @"INSERT INTO CorteCor_Usuario 
                     (Nome, Email, Telefone, DataEntrada, Status, Sobrenome, CPF, Senha, IdSalao) 
                     VALUES 
                     (@Nome, @Email, @Telefone, @DataEntrada, @Status, @Sobrenome, @CPF, @Senha, @IdSalao);
                     SELECT SCOPE_IDENTITY();"; // Retorna o ID gerado

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", Usuario.Nome);
                command.AddWithValue("@Sobrenome", Usuario.Sobrenome);
                command.AddWithValue("@CPF", Usuario.CPF);
                command.AddWithValue("@Email", Usuario.Email);
                command.AddWithValue("@Telefone", Usuario.Telefone);
                // Se DataEntrada for nullable ou MinValue, tratar:
                if (Usuario.DataEntrada == DateTime.MinValue)
                    command.AddWithValue("@DataEntrada", DBNull.Value);
                else
                    command.AddWithValue("@DataEntrada", Usuario.DataEntrada);
                command.AddWithValue("@Status", Usuario.Status);
                command.AddWithValue("@Senha", Usuario.Senha);
                command.AddWithValue("@IdSalao", Usuario.IdSalao);

                object result = command.ExecuteScalar(); // Obtém o ID gerado
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId; // Retorna o ID inserido
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string status = ativar ? "Ativo" : "Inativo";
            string query = "UPDATE CorteCor_Usuario SET Status = @Status WHERE IdUsuario = @IdUsuario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@IdUsuario", id);
                command.ExecuteNonQuery();
            }
        }

        public override List<Usuario> Listar()
        {
            string query = @"SELECT IdUsuario, Nome, Email, Telefone, DataEntrada, Status, Sobrenome, CPF, Senha, IdSalao
                         FROM CorteCor_Usuario 
                         ORDER BY IdSalao, Nome";
            var Usuarios = new List<Usuario>();
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Usuarios.Add(new Usuario
                        {
                            IdUsuario = reader["IdUsuario"] is DBNull ? 0 : Convert.ToInt32(reader["IdUsuario"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            DataEntrada = reader["DataEntrada"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataEntrada"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            Sobrenome = reader["Sobrenome"] is DBNull ? "" : reader["Sobrenome"].ToString(),
                            CPF = reader["CPF"] is DBNull ? "" : reader["CPF"].ToString(),
                            Senha = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString(),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                        });
                    }
                }
            }
            return Usuarios;
        }

        public Usuario ObterPorId(int idUsuario)
        {
            string query = @"SELECT IdUsuario, Nome, Email, Telefone, DataEntrada, Status, Sobrenome, CPF, Senha, IdSalao
                         FROM CorteCor_Usuario 
                         WHERE IdUsuario = @IdUsuario";
            var Usuario = new Usuario();
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdUsuario", idUsuario);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Usuario = new Usuario
                        {
                            IdUsuario = reader["IdUsuario"] is DBNull ? 0 : Convert.ToInt32(reader["IdUsuario"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            DataEntrada = reader["DataEntrada"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataEntrada"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            Sobrenome = reader["Sobrenome"] is DBNull ? "" : reader["Sobrenome"].ToString(),
                            CPF = reader["CPF"] is DBNull ? "" : reader["CPF"].ToString(),
                            Senha = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString(),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                        };
                        return Usuario;
                    }
                }
            }
            return null;
        }

        public List<Usuario> ListarPorSalao(int idSalao)
        {
            string query = @"SELECT IdUsuario, Nome, Email, Telefone, DataEntrada, Status, Sobrenome, CPF, Senha, IdSalao
                         FROM CorteCor_Usuario 
                         WHERE IdSalao = @IdSalao
                         ORDER BY Nome";
            var Usuarios = new List<Usuario>();
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Usuarios.Add(new Usuario
                        {
                            IdUsuario = reader["IdUsuario"] is DBNull ? 0 : Convert.ToInt32(reader["IdUsuario"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            DataEntrada = reader["DataEntrada"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataEntrada"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            Sobrenome = reader["Sobrenome"] is DBNull ? "" : reader["Sobrenome"].ToString(),
                            CPF = reader["CPF"] is DBNull ? "" : reader["CPF"].ToString(),
                            Senha = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString(),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                        });
                    }
                }
            }
            return Usuarios;
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_Usuario WHERE IdUsuario = @IdUsuario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdUsuario", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idUsuario, int idSalao)
        {
            string query = "DELETE FROM CorteCor_Usuario WHERE IdUsuario = @IdUsuario AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdUsuario", idUsuario);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public void Atualizar(Usuario Usuario)
        {
            string query = @"UPDATE CorteCor_Usuario 
                         SET Nome = @Nome, 
                             Email = @Email, 
                             Telefone = @Telefone, 
                             DataEntrada = @DataEntrada, 
                             Sobrenome = @Sobrenome, 
                             CPF = @CPF,
                             IdSalao = @IdSalao
                         WHERE IdUsuario = @IdUsuario AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", Usuario.Nome);
                command.AddWithValue("@Sobrenome", Usuario.Sobrenome);
                command.AddWithValue("@CPF", Usuario.CPF);
                command.AddWithValue("@Email", Usuario.Email);
                command.AddWithValue("@Telefone", Usuario.Telefone);
                command.AddWithValue("@DataEntrada", Usuario.DataEntrada);
                command.AddWithValue("@IdUsuario", Usuario.IdUsuario);
                command.AddWithValue("@IdSalao", Usuario.IdSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void Cadastrar(Usuario entity)
        {
            throw new NotImplementedException();
        }
    }

    public class LoginManager
    {
        private readonly IDatabaseHandler _dbHandler;
        private readonly IConfiguration _configuration;

        public LoginManager(IDatabaseHandler dbHandler = null, IConfiguration? configuration = null)
        {
            _configuration = configuration ?? AppConfigurationFactory.Build();
            _dbHandler = dbHandler ?? new DatabaseHandler(_configuration);
        }

        public virtual bool AutenticarAdministrador(string email, string senha) =>
            Autenticar("CorteCor_Administrador", email, senha);

        public bool AutenticarUsuario(string email, string senha) =>
            Autenticar("CorteCor_Usuario", email, senha);

        public void RegistrarUsuario(string nome, string email, string senha, string perfil)
        {
            string senhaHash = GerarHashSenha(senha);
            string query = @"
            INSERT INTO CorteCor_Administrador 
                (Nome, Email, Senha, Perfil, Status) 
            VALUES 
                (@Nome, @Email, @Senha, @Perfil, 'Ativo');";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", nome);
                command.AddWithValue("@Email", email);
                command.AddWithValue("@Senha", senhaHash);
                command.AddWithValue("@Perfil", perfil);
                command.ExecuteNonQuery();
            }
        }

        public void AlterarSenha(string email, string novaSenha)
        {
            string senhaHash = GerarHashSenha(novaSenha);
            string query = @"
            UPDATE CorteCor_Administrador 
            SET Senha = @Senha 
            WHERE Email = @Email 
              AND Status = 'Ativo' ;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Senha", senhaHash);
                command.AddWithValue("@Email", email);
                command.ExecuteNonQuery();
            }
        }

        public void DesativarUsuario(int idUsuario)
        {
            string query = @"
            UPDATE CorteCor_Administrador 
            SET Status = 'Inativo' 
            WHERE IdUsuario = @IdUsuario;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdUsuario", idUsuario);
                command.ExecuteNonQuery();
            }
        }

        private string GerarHashSenha(string senha)
        {
            return PasswordSecurity.HashPassword(senha);
        }

        private bool VerificarSenha(string senha, string senhaHash)
        {
            return PasswordSecurity.VerifyPassword(senha, senhaHash);
        }

        public void EnviarEmail(string email, string from, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["Smtp:Host"];
                var smtpUsername = _configuration["Smtp:Username"];
                var smtpPassword = _configuration["Smtp:Password"];
                var fromAddress = _configuration["Smtp:FromAddress"];
                var fromName = _configuration["Smtp:FromName"];

                if (string.IsNullOrWhiteSpace(smtpHost) ||
                    string.IsNullOrWhiteSpace(smtpUsername) ||
                    string.IsNullOrWhiteSpace(smtpPassword))
                {
                    return;
                }

                using (var client = new SmtpClient())
                {
                    client.Host = smtpHost;
                    client.Port = int.TryParse(_configuration["Smtp:Port"], out var smtpPort) ? smtpPort : 587;
                    client.EnableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var enableSsl) || enableSsl;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    using (var message = new MailMessage())
                    {
                        var resolvedFrom = string.IsNullOrWhiteSpace(fromAddress) ? from : fromAddress;
                        var resolvedFromName = string.IsNullOrWhiteSpace(fromName) ? "Tonni Corte & Cor" : fromName;
                        message.From = new MailAddress(resolvedFrom, resolvedFromName);
                        message.To.Add(new MailAddress(email));
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Tratar a exceção conforme necessário.
            }
        }

        private bool Autenticar(string tableName, string email, string senha)
        {
            string query = $@"
            SELECT IdUsuario, Senha
            FROM {tableName}
            WHERE Email = @Email
              AND Status = 'Ativo';";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Email", email);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return false;
                    }

                    var idUsuario = reader["IdUsuario"] is DBNull ? 0 : Convert.ToInt32(reader["IdUsuario"]);
                    string senhaHash = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString();
                    var autenticado = VerificarSenha(senha, senhaHash);

                    if (autenticado && PasswordSecurity.NeedsRehash(senhaHash))
                    {
                        reader.Close();
                        AtualizarHashSenha(tableName, idUsuario, senha);
                    }

                    return autenticado;
                }
            }
        }

        private void AtualizarHashSenha(string tableName, int idUsuario, string senha)
        {
            string query = $@"
            UPDATE {tableName}
            SET Senha = @Senha
            WHERE IdUsuario = @IdUsuario;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Senha", PasswordSecurity.HashPassword(senha));
                command.AddWithValue("@IdUsuario", idUsuario);
                command.ExecuteNonQuery();
            }
        }
    }

    public class Salaoervice
    {
        private readonly IMemoryCache _cache;
        private readonly IDatabaseHandler _dbHandler;

        public Salaoervice(IMemoryCache cache, IDatabaseHandler dbHandler = null)
        {
            _cache = cache;
            _dbHandler = dbHandler ?? new DatabaseHandler();
        }

        public Salao ObterSalao(int IdSalao)
        {
            var cacheKey = $"Salao_{IdSalao}";
            if (!_cache.TryGetValue(cacheKey, out Salao Salao))
            {
                Salao = CarregarSalaoDoBanco(IdSalao);
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                };
                _cache.Set(cacheKey, Salao, cacheOptions);
            }
            return Salao;
        }

        private Salao CarregarSalaoDoBanco(int IdSalao)
        {
            var query = @"
            SELECT IdSalao, Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro,
                   LimiteEnvioEmail, LimiteEnvioSMS, LimiteEnvioWhatsapp
            FROM CorteCor_Salao
            WHERE IdSalao = @IdSalao";

            using var connection = _dbHandler.GetConnection();
            using var command = connection.CreateCommand(query);
            command.AddWithValue("@IdSalao", IdSalao);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Salao
                {
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                    Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                    Responsavel = reader["Responsavel"] is DBNull ? "" : reader["Responsavel"].ToString(),
                    Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                    Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                    Endereco = reader["Endereco"] is DBNull ? "" : reader["Endereco"].ToString(),
                    CNPJ = reader["CNPJ"] is DBNull ? "" : reader["CNPJ"].ToString(),
                    Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                    DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                    LimiteEnvioEmail = reader["LimiteEnvioEmail"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioEmail"]),
                    LimiteEnvioSMS = reader["LimiteEnvioSMS"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioSMS"]),
                    LimiteEnvioWhatsapp = reader["LimiteEnvioWhatsapp"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioWhatsapp"])
                };
            }
            return null;
        }

        public void InvalidarCache(int IdSalao)
        {
            var cacheKey = $"Salao_{IdSalao}";
            _cache.Remove(cacheKey);
        }
    }

    public class SalaoHandler : EntityHandler<Salao>
    {
        public SalaoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public int CadastrarSalao(Salao Salao)
        {
            int novoId = 0;
            string query = @"
        INSERT INTO CorteCor_Salao 
            (Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro, Observacao,
             LimiteEnvioEmail, LimiteEnvioSMS, LimiteEnvioWhatsapp)
        VALUES 
            (@Nome, @Responsavel, @Email, @Telefone, @Endereco, @CNPJ, @Status, @DataCadastro, @Observacao,
             @LimiteEnvioEmail, @LimiteEnvioSMS, @LimiteEnvioWhatsapp);
        SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", Salao.Nome);
                command.AddWithValue("@Responsavel", Salao.Responsavel);
                command.AddWithValue("@Email", Salao.Email);
                command.AddWithValue("@Telefone", Salao.Telefone);
                command.AddWithValue("@Endereco", Salao.Endereco);
                command.AddWithValue("@CNPJ", Salao.CNPJ);
                command.AddWithValue("@Status", Salao.Status);
                command.AddWithValue("@DataCadastro", Salao.DataCadastro.ToString("yyyy-MM-dd"));
                command.AddWithValue("@Observacao", (object)Salao.Observacao ?? DBNull.Value);
                command.AddWithValue("@LimiteEnvioEmail", Salao.LimiteEnvioEmail);
                command.AddWithValue("@LimiteEnvioSMS", Salao.LimiteEnvioSMS);
                command.AddWithValue("@LimiteEnvioWhatsapp", Salao.LimiteEnvioWhatsapp);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId;
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string status = ativar ? "Ativo" : "Inativo";
            string query = "UPDATE CorteCor_Salao SET Status = @Status WHERE IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@IdSalao", id);
                command.ExecuteNonQuery();
            }
        }

        public override List<Salao> Listar()
        {
            string query = @"
        SELECT IdSalao, Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro, Observacao,
               LimiteEnvioEmail, LimiteEnvioSMS, LimiteEnvioWhatsapp
        FROM CorteCor_Salao
        ORDER BY Nome";

            var Saloes = new List<Salao>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Saloes.Add(new Salao
                        {
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Responsavel = reader["Responsavel"] is DBNull ? "" : reader["Responsavel"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            Endereco = reader["Endereco"] is DBNull ? "" : reader["Endereco"].ToString(),
                            CNPJ = reader["CNPJ"] is DBNull ? "" : reader["CNPJ"].ToString(),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                            Observacao = reader["Observacao"] is DBNull ? null : reader["Observacao"].ToString(),
                            LimiteEnvioEmail = reader["LimiteEnvioEmail"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioEmail"]),
                            LimiteEnvioSMS = reader["LimiteEnvioSMS"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioSMS"]),
                            LimiteEnvioWhatsapp = reader["LimiteEnvioWhatsapp"] is DBNull ? 0 : Convert.ToInt32(reader["LimiteEnvioWhatsapp"])
                        });
                    }
                }
            }
            return Saloes;
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_Salao WHERE IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", id);
                command.ExecuteNonQuery();
            }
        }

        public void Atualizar(Salao Salao)
        {
            string query = @"
        UPDATE CorteCor_Salao
        SET Nome = @Nome,
            Responsavel = @Responsavel,
            Email = @Email,
            Telefone = @Telefone,
            Endereco = @Endereco,
            CNPJ = @CNPJ,
            Status = @Status,
            DataCadastro = @DataCadastro,
            Observacao = @Observacao,
            LimiteEnvioEmail = @LimiteEnvioEmail,
            LimiteEnvioSMS = @LimiteEnvioSMS,
            LimiteEnvioWhatsapp = @LimiteEnvioWhatsapp
        WHERE IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", Salao.Nome);
                command.AddWithValue("@Responsavel", Salao.Responsavel);
                command.AddWithValue("@Email", Salao.Email);
                command.AddWithValue("@Telefone", Salao.Telefone);
                command.AddWithValue("@Endereco", Salao.Endereco);
                command.AddWithValue("@CNPJ", Salao.CNPJ);
                command.AddWithValue("@Status", Salao.Status);
                command.AddWithValue("@DataCadastro", Salao.DataCadastro);
                command.AddWithValue("@Observacao", (object)Salao.Observacao ?? DBNull.Value);
                command.AddWithValue("@LimiteEnvioEmail", Salao.LimiteEnvioEmail);
                command.AddWithValue("@LimiteEnvioSMS", Salao.LimiteEnvioSMS);
                command.AddWithValue("@LimiteEnvioWhatsapp", Salao.LimiteEnvioWhatsapp);
                command.AddWithValue("@IdSalao", Salao.IdSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void Cadastrar(Salao entity)
        {
            throw new NotImplementedException();
        }
    }

    public class AdministradorHandler : EntityHandler<Administrador>
    {
        public AdministradorHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public int CadastrarAdministrador(Administrador admin)
        {
            int novoId = 0;
            string query = @"
                INSERT INTO CorteCor_Administrador 
                    (Nome, Email, Senha, Perfil, Status, DataCriacao)
                VALUES 
                    (@Nome, @Email, @Senha, @Perfil, @Status, @DataCriacao);
                SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", admin.Nome);
                command.AddWithValue("@Email", admin.Email);
                command.AddWithValue("@Senha", admin.Senha);
                command.AddWithValue("@Perfil", admin.Perfil);
                command.AddWithValue("@Status", admin.Status);
                command.AddWithValue("@DataCriacao", admin.DataCriacao);
                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId;
        }

        public override List<Administrador> Listar()
        {
            string query = @"
                SELECT IdUsuario, Nome, Email, Senha, Perfil, Status, DataCriacao
                FROM CorteCor_Administrador
                ORDER BY Nome";
            var admins = new List<Administrador>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        admins.Add(new Administrador
                        {
                            IdUsuario = reader["IdUsuario"] is DBNull ? 0 : Convert.ToInt32(reader["IdUsuario"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            Senha = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString(),
                            Perfil = reader["Perfil"] is DBNull ? "" : reader["Perfil"].ToString(),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            DataCriacao = reader["DataCriacao"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCriacao"])
                        });
                    }
                }
            }
            return admins;
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string status = ativar ? "Ativo" : "Inativo";
            string query = "UPDATE CorteCor_Administrador SET Status = @Status WHERE IdUsuario = @IdUsuario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@IdUsuario", id);
                command.ExecuteNonQuery();
            }
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_Administrador WHERE IdUsuario = @IdUsuario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdUsuario", id);
                command.ExecuteNonQuery();
            }
        }

        public void Atualizar(Administrador admin)
        {
            string query = @"
                UPDATE CorteCor_Administrador SET 
                    Nome = @Nome,
                    Email = @Email,
                    Senha = @Senha,
                    Perfil = @Perfil,
                    Status = @Status,
                    DataCriacao = @DataCriacao
                WHERE IdUsuario = @IdUsuario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", admin.Nome);
                command.AddWithValue("@Email", admin.Email);
                command.AddWithValue("@Senha", admin.Senha);
                command.AddWithValue("@Perfil", admin.Perfil);
                command.AddWithValue("@Status", admin.Status);
                command.AddWithValue("@DataCriacao", admin.DataCriacao);
                command.AddWithValue("@IdUsuario", admin.IdUsuario);
                command.ExecuteNonQuery();
            }
        }

        public override void Cadastrar(Administrador entity)
        {
            throw new NotImplementedException();
        }
    }

    // ===============================
    // Handler para CorteCor_PessoaFicha
    // ===============================
    public class PessoaFichaHandler : EntityHandler<PessoaFicha>
    {
        public PessoaFichaHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public int CadastrarPessoa(PessoaFicha pessoa)
        {
            int novoId = 0;
            string query = @"INSERT INTO CorteCor_PessoaFicha 
                        (FichaID, Nome, Filiacao, RG, CPF, DataNascimento, Nacionalidade, NIS, EstadoCivil, RegimeCasamento,
                        SituacaoProfissional, Profissao, GrauInstrucao, Iletrado, Empresa, CarteiraAssinada, RendaMensal,
                        Endereco, Quadra, PontoReferencia, Bairro, Lote, MunicipioResidencia, Telefone, Celular,
                        ConjugeNome, ConjugeFiliacao, ConjugeRG, ConjugeCPF, ConjugeIdade, ConjugeNacionalidade, ConjugeSituacaoProfissional,
                        ConjugeProfissao, ConjugeGrauInstrucao, ConjugeIletrado, ConjugeEmpresa, ConjugeCarteiraAssinada, ConjugeRendaMensal)
                        VALUES
                        (@FichaID, @Nome, @Filiacao, @RG, @CPF, @DataNascimento, @Nacionalidade, @NIS, @EstadoCivil, @RegimeCasamento,
                        @SituacaoProfissional, @Profissao, @GrauInstrucao, @Iletrado, @Empresa, @CarteiraAssinada, @RendaMensal,
                        @Endereco, @Quadra, @PontoReferencia, @Bairro, @Lote, @MunicipioResidencia, @Telefone, @Celular,
                        @ConjugeNome, @ConjugeFiliacao, @ConjugeRG, @ConjugeCPF, @ConjugeIdade, @ConjugeNacionalidade, @ConjugeSituacaoProfissional,
                        @ConjugeProfissao, @ConjugeGrauInstrucao, @ConjugeIletrado, @ConjugeEmpresa, @ConjugeCarteiraAssinada, @ConjugeRendaMensal);
                        SELECT SCOPE_IDENTITY();";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@FichaID", pessoa.FichaID);
                command.AddWithValue("@Nome", (object)pessoa.Nome ?? DBNull.Value);
                command.AddWithValue("@Filiacao", (object)pessoa.Filiacao ?? DBNull.Value);
                command.AddWithValue("@RG", (object)pessoa.RG ?? DBNull.Value);
                command.AddWithValue("@CPF", (object)pessoa.CPF ?? DBNull.Value);
                command.AddWithValue("@DataNascimento", (object)pessoa.DataNascimento ?? DBNull.Value);
                command.AddWithValue("@Nacionalidade", (object)pessoa.Nacionalidade ?? DBNull.Value);
                command.AddWithValue("@NIS", (object)pessoa.NIS ?? DBNull.Value);
                command.AddWithValue("@EstadoCivil", (object)pessoa.EstadoCivil ?? DBNull.Value);
                command.AddWithValue("@RegimeCasamento", (object)pessoa.RegimeCasamento ?? DBNull.Value);
                command.AddWithValue("@SituacaoProfissional", (object)pessoa.SituacaoProfissional ?? DBNull.Value);
                command.AddWithValue("@Profissao", (object)pessoa.Profissao ?? DBNull.Value);
                command.AddWithValue("@GrauInstrucao", (object)pessoa.GrauInstrucao ?? DBNull.Value);
                command.AddWithValue("@Iletrado", (object)pessoa.Iletrado ?? DBNull.Value);
                command.AddWithValue("@Empresa", (object)pessoa.Empresa ?? DBNull.Value);
                command.AddWithValue("@CarteiraAssinada", (object)pessoa.CarteiraAssinada ?? DBNull.Value);
                command.AddWithValue("@RendaMensal", (object)pessoa.RendaMensal ?? DBNull.Value);
                command.AddWithValue("@Endereco", (object)pessoa.Endereco ?? DBNull.Value);
                command.AddWithValue("@Quadra", (object)pessoa.Quadra ?? DBNull.Value);
                command.AddWithValue("@PontoReferencia", (object)pessoa.PontoReferencia ?? DBNull.Value);
                command.AddWithValue("@Bairro", (object)pessoa.Bairro ?? DBNull.Value);
                command.AddWithValue("@Lote", (object)pessoa.Lote ?? DBNull.Value);
                command.AddWithValue("@MunicipioResidencia", pessoa.MunicipioResidencia);
                command.AddWithValue("@Telefone", (object)pessoa.Telefone ?? DBNull.Value);
                command.AddWithValue("@Celular", (object)pessoa.Celular ?? DBNull.Value);
                command.AddWithValue("@ConjugeNome", (object)pessoa.ConjugeNome ?? DBNull.Value);
                command.AddWithValue("@ConjugeFiliacao", (object)pessoa.ConjugeFiliacao ?? DBNull.Value);
                command.AddWithValue("@ConjugeRG", (object)pessoa.ConjugeRG ?? DBNull.Value);
                command.AddWithValue("@ConjugeCPF", (object)pessoa.ConjugeCPF ?? DBNull.Value);
                command.AddWithValue("@ConjugeIdade", (object)pessoa.ConjugeIdade ?? DBNull.Value);
                command.AddWithValue("@ConjugeNacionalidade", (object)pessoa.ConjugeNacionalidade ?? DBNull.Value);
                command.AddWithValue("@ConjugeSituacaoProfissional", (object)pessoa.ConjugeSituacaoProfissional ?? DBNull.Value);
                command.AddWithValue("@ConjugeProfissao", (object)pessoa.ConjugeProfissao ?? DBNull.Value);
                command.AddWithValue("@ConjugeGrauInstrucao", (object)pessoa.ConjugeGrauInstrucao ?? DBNull.Value);
                command.AddWithValue("@ConjugeIletrado", (object)pessoa.ConjugeIletrado ?? DBNull.Value);
                command.AddWithValue("@ConjugeEmpresa", (object)pessoa.ConjugeEmpresa ?? DBNull.Value);
                command.AddWithValue("@ConjugeCarteiraAssinada", (object)pessoa.ConjugeCarteiraAssinada ?? DBNull.Value);
                command.AddWithValue("@ConjugeRendaMensal", (object)pessoa.ConjugeRendaMensal ?? DBNull.Value);
                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId;
        }

        public override List<PessoaFicha> Listar()
        {
            string query = @"SELECT PessoaID, FichaID, Nome, Filiacao, RG, CPF, DataNascimento, Nacionalidade, NIS, EstadoCivil, 
                                    RegimeCasamento, SituacaoProfissional, Profissao, GrauInstrucao, Iletrado, Empresa, CarteiraAssinada,
                                    RendaMensal, Endereco, Quadra, PontoReferencia, Bairro, Lote, MunicipioResidencia, Telefone, Celular,
                                    ConjugeNome, ConjugeFiliacao, ConjugeRG, ConjugeCPF, ConjugeIdade, ConjugeNacionalidade, ConjugeSituacaoProfissional,
                                    ConjugeProfissao, ConjugeGrauInstrucao, ConjugeIletrado, ConjugeEmpresa, ConjugeCarteiraAssinada, ConjugeRendaMensal
                        FROM CorteCor_PessoaFicha
                        ORDER BY Nome";
            List<PessoaFicha> pessoas = new List<PessoaFicha>();
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pessoas.Add(new PessoaFicha
                        {
                            PessoaID = reader["PessoaID"] is DBNull ? 0 : Convert.ToInt32(reader["PessoaID"]),
                            FichaID = reader["FichaID"] is DBNull ? 0 : Convert.ToInt32(reader["FichaID"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Filiacao = reader["Filiacao"] is DBNull ? "" : reader["Filiacao"].ToString(),
                            RG = reader["RG"] is DBNull ? "" : reader["RG"].ToString(),
                            CPF = reader["CPF"] is DBNull ? "" : reader["CPF"].ToString(),
                            DataNascimento = reader["DataNascimento"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataNascimento"]),
                            Nacionalidade = reader["Nacionalidade"] is DBNull ? "" : reader["Nacionalidade"].ToString(),
                            NIS = reader["NIS"] is DBNull ? null : reader["NIS"].ToString(),
                            EstadoCivil = reader["EstadoCivil"] is DBNull ? "" : reader["EstadoCivil"].ToString(),
                            RegimeCasamento = reader["RegimeCasamento"] is DBNull ? null : reader["RegimeCasamento"].ToString(),
                            SituacaoProfissional = reader["SituacaoProfissional"] is DBNull ? "" : reader["SituacaoProfissional"].ToString(),
                            Profissao = reader["Profissao"] is DBNull ? "" : reader["Profissao"].ToString(),
                            GrauInstrucao = reader["GrauInstrucao"] is DBNull ? "" : reader["GrauInstrucao"].ToString(),
                            Iletrado = reader["Iletrado"] is DBNull ? false : Convert.ToBoolean(reader["Iletrado"]),
                            Empresa = reader["Empresa"] is DBNull ? null : reader["Empresa"].ToString(),
                            CarteiraAssinada = reader["CarteiraAssinada"] is DBNull ? false : Convert.ToBoolean(reader["CarteiraAssinada"]),
                            RendaMensal = reader["RendaMensal"] is DBNull ? 0 : Convert.ToDecimal(reader["RendaMensal"]),
                            Endereco = reader["Endereco"] is DBNull ? "" : reader["Endereco"].ToString(),
                            Quadra = reader["Quadra"] is DBNull ? null : reader["Quadra"].ToString(),
                            PontoReferencia = reader["PontoReferencia"] is DBNull ? null : reader["PontoReferencia"].ToString(),
                            Bairro = reader["Bairro"] is DBNull ? null : reader["Bairro"].ToString(),
                            Lote = reader["Lote"] is DBNull ? (int?)null : Convert.ToInt32(reader["Lote"]),
                            MunicipioResidencia = reader["MunicipioResidencia"] is DBNull ? "" : reader["MunicipioResidencia"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? null : reader["Telefone"].ToString(),
                            Celular = reader["Celular"] is DBNull ? null : reader["Celular"].ToString(),
                            ConjugeNome = reader["ConjugeNome"] is DBNull ? null : reader["ConjugeNome"].ToString(),
                            ConjugeFiliacao = reader["ConjugeFiliacao"] is DBNull ? null : reader["ConjugeFiliacao"].ToString(),
                            ConjugeRG = reader["ConjugeRG"] is DBNull ? null : reader["ConjugeRG"].ToString(),
                            ConjugeCPF = reader["ConjugeCPF"] is DBNull ? null : reader["ConjugeCPF"].ToString(),
                            ConjugeIdade = reader["ConjugeIdade"] is DBNull ? (int?)null : Convert.ToInt32(reader["ConjugeIdade"]),
                            ConjugeNacionalidade = reader["ConjugeNacionalidade"] is DBNull ? null : reader["ConjugeNacionalidade"].ToString(),
                            ConjugeSituacaoProfissional = reader["ConjugeSituacaoProfissional"] is DBNull ? null : reader["ConjugeSituacaoProfissional"].ToString(),
                            ConjugeProfissao = reader["ConjugeProfissao"] is DBNull ? null : reader["ConjugeProfissao"].ToString(),
                            ConjugeGrauInstrucao = reader["ConjugeGrauInstrucao"] is DBNull ? null : reader["ConjugeGrauInstrucao"].ToString(),
                            ConjugeIletrado = reader["ConjugeIletrado"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConjugeIletrado"]),
                            ConjugeEmpresa = reader["ConjugeEmpresa"] is DBNull ? null : reader["ConjugeEmpresa"].ToString(),
                            ConjugeCarteiraAssinada = reader["ConjugeCarteiraAssinada"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConjugeCarteiraAssinada"]),
                            ConjugeRendaMensal = reader["ConjugeRendaMensal"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["ConjugeRendaMensal"])
                        });
                    }
                }
            }
            return pessoas;
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_PessoaFicha WHERE PessoaID = @PessoaID";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@PessoaID", id);
                command.ExecuteNonQuery();
            }
        }

        public void Atualizar(PessoaFicha pessoa)
        {
            string query = @"UPDATE CorteCor_PessoaFicha SET 
                            Nome = @Nome, 
                            Filiacao = @Filiacao, 
                            RG = @RG, 
                            CPF = @CPF, 
                            DataNascimento = @DataNascimento,
                            Nacionalidade = @Nacionalidade,
                            NIS = @NIS,
                            EstadoCivil = @EstadoCivil,
                            RegimeCasamento = @RegimeCasamento,
                            SituacaoProfissional = @SituacaoProfissional,
                            Profissao = @Profissao,
                            GrauInstrucao = @GrauInstrucao,
                            Iletrado = @Iletrado,
                            Empresa = @Empresa,
                            CarteiraAssinada = @CarteiraAssinada,
                            RendaMensal = @RendaMensal,
                            Endereco = @Endereco,
                            Quadra = @Quadra,
                            PontoReferencia = @PontoReferencia,
                            Bairro = @Bairro,
                            Lote = @Lote,
                            MunicipioResidencia = @MunicipioResidencia,
                            Telefone = @Telefone,
                            Celular = @Celular,
                            ConjugeNome = @ConjugeNome,
                            ConjugeFiliacao = @ConjugeFiliacao,
                            ConjugeRG = @ConjugeRG,
                            ConjugeCPF = @ConjugeCPF,
                            ConjugeIdade = @ConjugeIdade,
                            ConjugeNacionalidade = @ConjugeNacionalidade,
                            ConjugeSituacaoProfissional = @ConjugeSituacaoProfissional,
                            ConjugeProfissao = @ConjugeProfissao,
                            ConjugeGrauInstrucao = @ConjugeGrauInstrucao,
                            ConjugeIletrado = @ConjugeIletrado,
                            ConjugeEmpresa = @ConjugeEmpresa,
                            ConjugeCarteiraAssinada = @ConjugeCarteiraAssinada,
                            ConjugeRendaMensal = @ConjugeRendaMensal
                            WHERE FichaID = @FichaID";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", (object)pessoa.Nome ?? DBNull.Value);
                command.AddWithValue("@Filiacao", (object)pessoa.Filiacao ?? DBNull.Value);
                command.AddWithValue("@RG", (object)pessoa.RG ?? DBNull.Value);
                command.AddWithValue("@CPF", (object)pessoa.CPF ?? DBNull.Value);
                command.AddWithValue("@DataNascimento", (object)pessoa.DataNascimento ?? DBNull.Value);
                command.AddWithValue("@Nacionalidade", (object)pessoa.Nacionalidade ?? DBNull.Value);
                command.AddWithValue("@NIS", (object)pessoa.NIS ?? DBNull.Value);
                command.AddWithValue("@EstadoCivil", (object)pessoa.EstadoCivil ?? DBNull.Value);
                command.AddWithValue("@RegimeCasamento", (object)pessoa.RegimeCasamento ?? DBNull.Value);
                command.AddWithValue("@SituacaoProfissional", (object)pessoa.SituacaoProfissional ?? DBNull.Value);
                command.AddWithValue("@Profissao", (object)pessoa.Profissao ?? DBNull.Value);
                command.AddWithValue("@GrauInstrucao", (object)pessoa.GrauInstrucao ?? DBNull.Value);
                command.AddWithValue("@Iletrado", (object)pessoa.Iletrado ?? DBNull.Value);
                command.AddWithValue("@Empresa", (object)pessoa.Empresa ?? DBNull.Value);
                command.AddWithValue("@CarteiraAssinada", (object)pessoa.CarteiraAssinada ?? DBNull.Value);
                command.AddWithValue("@RendaMensal", (object)pessoa.RendaMensal ?? DBNull.Value);
                command.AddWithValue("@Endereco", (object)pessoa.Endereco ?? DBNull.Value);
                command.AddWithValue("@Quadra", (object)pessoa.Quadra ?? DBNull.Value);
                command.AddWithValue("@PontoReferencia", (object)pessoa.PontoReferencia ?? DBNull.Value);
                command.AddWithValue("@Bairro", (object)pessoa.Bairro ?? DBNull.Value);
                command.AddWithValue("@Lote", (object)pessoa.Lote ?? DBNull.Value);
                command.AddWithValue("@MunicipioResidencia", (object)pessoa.MunicipioResidencia ?? DBNull.Value);
                command.AddWithValue("@Telefone", (object)pessoa.Telefone ?? DBNull.Value);
                command.AddWithValue("@Celular", (object)pessoa.Celular ?? DBNull.Value);
                command.AddWithValue("@ConjugeNome", (object)pessoa.ConjugeNome ?? DBNull.Value);
                command.AddWithValue("@ConjugeFiliacao", (object)pessoa.ConjugeFiliacao ?? DBNull.Value);
                command.AddWithValue("@ConjugeRG", (object)pessoa.ConjugeRG ?? DBNull.Value);
                command.AddWithValue("@ConjugeCPF", (object)pessoa.ConjugeCPF ?? DBNull.Value);
                command.AddWithValue("@ConjugeIdade", (object)pessoa.ConjugeIdade ?? DBNull.Value);
                command.AddWithValue("@ConjugeNacionalidade", (object)pessoa.ConjugeNacionalidade ?? DBNull.Value);
                command.AddWithValue("@ConjugeSituacaoProfissional", (object)pessoa.ConjugeSituacaoProfissional ?? DBNull.Value);
                command.AddWithValue("@ConjugeProfissao", (object)pessoa.ConjugeProfissao ?? DBNull.Value);
                command.AddWithValue("@ConjugeGrauInstrucao", (object)pessoa.ConjugeGrauInstrucao ?? DBNull.Value);
                command.AddWithValue("@ConjugeIletrado", (object)pessoa.ConjugeIletrado ?? DBNull.Value);
                command.AddWithValue("@ConjugeEmpresa", (object)pessoa.ConjugeEmpresa ?? DBNull.Value);
                command.AddWithValue("@ConjugeCarteiraAssinada", (object)pessoa.ConjugeCarteiraAssinada ?? DBNull.Value);
                command.AddWithValue("@ConjugeRendaMensal", (object)pessoa.ConjugeRendaMensal ?? DBNull.Value);
                command.AddWithValue("@FichaID", pessoa.FichaID);
                command.ExecuteNonQuery();
            }
        }

        public override void Cadastrar(PessoaFicha entity)
        {
            throw new NotImplementedException();
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotImplementedException();
        }
    }


    public class FuncionarioHandler : EntityHandler<Funcionario>
    {
        public FuncionarioHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public virtual int CadastrarFuncionario(Funcionario funcionario)
        {
            int novoId = 0;

            string query = @"
        INSERT INTO CorteCor_Funcionario
            (Nome,
             seg, seg_ini, seg_fim,
             ter, ter_ini, ter_fim,
             qua, qua_ini, qua_fim,
             qui, qui_ini, qui_fim,
             sex, sex_ini, sex_fim,
             sab, sab_ini, sab_fim,
             dom, dom_ini, dom_fim,
             IdSalao)
        VALUES
            (@Nome,
             @seg, @seg_ini, @seg_fim,
             @ter, @ter_ini, @ter_fim,
             @qua, @qua_ini, @qua_fim,
             @qui, @qui_ini, @qui_fim,
             @sex, @sex_ini, @sex_fim,
             @sab, @sab_ini, @sab_fim,
             @dom, @dom_ini, @dom_fim,
             @IdSalao);
        SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", funcionario.Nome ?? "");

                command.AddWithValue("@seg", funcionario.seg);
                command.AddWithValue("@seg_ini", (object?)funcionario.seg_ini ?? DBNull.Value);
                command.AddWithValue("@seg_fim", (object?)funcionario.seg_fim ?? DBNull.Value);

                command.AddWithValue("@ter", funcionario.ter);
                command.AddWithValue("@ter_ini", (object?)funcionario.ter_ini ?? DBNull.Value);
                command.AddWithValue("@ter_fim", (object?)funcionario.ter_fim ?? DBNull.Value);

                command.AddWithValue("@qua", funcionario.qua);
                command.AddWithValue("@qua_ini", (object?)funcionario.qua_ini ?? DBNull.Value);
                command.AddWithValue("@qua_fim", (object?)funcionario.qua_fim ?? DBNull.Value);

                command.AddWithValue("@qui", funcionario.qui);
                command.AddWithValue("@qui_ini", (object?)funcionario.qui_ini ?? DBNull.Value);
                command.AddWithValue("@qui_fim", (object?)funcionario.qui_fim ?? DBNull.Value);

                command.AddWithValue("@sex", funcionario.sex);
                command.AddWithValue("@sex_ini", (object?)funcionario.sex_ini ?? DBNull.Value);
                command.AddWithValue("@sex_fim", (object?)funcionario.sex_fim ?? DBNull.Value);

                command.AddWithValue("@sab", funcionario.sab);
                command.AddWithValue("@sab_ini", (object?)funcionario.sab_ini ?? DBNull.Value);
                command.AddWithValue("@sab_fim", (object?)funcionario.sab_fim ?? DBNull.Value);

                command.AddWithValue("@dom", funcionario.dom);
                command.AddWithValue("@dom_ini", (object?)funcionario.dom_ini ?? DBNull.Value);
                command.AddWithValue("@dom_fim", (object?)funcionario.dom_fim ?? DBNull.Value);

                command.AddWithValue("@IdSalao", funcionario.IdSalao);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    novoId = id;
            }

            return novoId;
        }

        public virtual Funcionario ObterPorId(int idFuncionario)
        {
            string query = @"
        SELECT IdFuncionario, Nome,
               seg, seg_ini, seg_fim,
               ter, ter_ini, ter_fim,
               qua, qua_ini, qua_fim,
               qui, qui_ini, qui_fim,
               sex, sex_ini, sex_fim,
               sab, sab_ini, sab_fim,
               dom, dom_ini, dom_fim,
               IdSalao
        FROM CorteCor_Funcionario
        WHERE IdFuncionario = @IdFuncionario;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new Funcionario
                    {
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),

                        seg = reader["seg"] is DBNull ? false : Convert.ToBoolean(reader["seg"]),
                        seg_ini = reader["seg_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_ini"],
                        seg_fim = reader["seg_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_fim"],

                        ter = reader["ter"] is DBNull ? false : Convert.ToBoolean(reader["ter"]),
                        ter_ini = reader["ter_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_ini"],
                        ter_fim = reader["ter_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_fim"],

                        qua = reader["qua"] is DBNull ? false : Convert.ToBoolean(reader["qua"]),
                        qua_ini = reader["qua_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_ini"],
                        qua_fim = reader["qua_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_fim"],

                        qui = reader["qui"] is DBNull ? false : Convert.ToBoolean(reader["qui"]),
                        qui_ini = reader["qui_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_ini"],
                        qui_fim = reader["qui_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_fim"],

                        sex = reader["sex"] is DBNull ? false : Convert.ToBoolean(reader["sex"]),
                        sex_ini = reader["sex_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_ini"],
                        sex_fim = reader["sex_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_fim"],

                        sab = reader["sab"] is DBNull ? false : Convert.ToBoolean(reader["sab"]),
                        sab_ini = reader["sab_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_ini"],
                        sab_fim = reader["sab_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_fim"],

                        dom = reader["dom"] is DBNull ? false : Convert.ToBoolean(reader["dom"]),
                        dom_ini = reader["dom_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_ini"],
                        dom_fim = reader["dom_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_fim"],

                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                    };
                }
            }
        }

        public virtual List<Funcionario> ListarPorSalao(int idSalao)
        {
            string query = @"
        SELECT IdFuncionario, Nome,
               seg, seg_ini, seg_fim,
               ter, ter_ini, ter_fim,
               qua, qua_ini, qua_fim,
               qui, qui_ini, qui_fim,
               sex, sex_ini, sex_fim,
               sab, sab_ini, sab_fim,
               dom, dom_ini, dom_fim,
               IdSalao
        FROM CorteCor_Funcionario
        WHERE IdSalao = @IdSalao
        ORDER BY Nome;";

            var funcionarios = new List<Funcionario>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        funcionarios.Add(new Funcionario
                        {
                            IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),

                            seg = reader["seg"] is DBNull ? false : Convert.ToBoolean(reader["seg"]),
                            seg_ini = reader["seg_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_ini"],
                            seg_fim = reader["seg_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_fim"],

                            ter = reader["ter"] is DBNull ? false : Convert.ToBoolean(reader["ter"]),
                            ter_ini = reader["ter_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_ini"],
                            ter_fim = reader["ter_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_fim"],

                            qua = reader["qua"] is DBNull ? false : Convert.ToBoolean(reader["qua"]),
                            qua_ini = reader["qua_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_ini"],
                            qua_fim = reader["qua_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_fim"],

                            qui = reader["qui"] is DBNull ? false : Convert.ToBoolean(reader["qui"]),
                            qui_ini = reader["qui_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_ini"],
                            qui_fim = reader["qui_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_fim"],

                            sex = reader["sex"] is DBNull ? false : Convert.ToBoolean(reader["sex"]),
                            sex_ini = reader["sex_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_ini"],
                            sex_fim = reader["sex_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_fim"],

                            sab = reader["sab"] is DBNull ? false : Convert.ToBoolean(reader["sab"]),
                            sab_ini = reader["sab_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_ini"],
                            sab_fim = reader["sab_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_fim"],

                            dom = reader["dom"] is DBNull ? false : Convert.ToBoolean(reader["dom"]),
                            dom_ini = reader["dom_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_ini"],
                            dom_fim = reader["dom_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_fim"],

                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                        });
                    }
                }
            }

            return funcionarios;
        }

        public virtual Funcionario? ObterPorIdESalao(int idFuncionario, int idSalao)
        {
            var funcionario = ObterPorId(idFuncionario);
            return funcionario != null && funcionario.IdSalao == idSalao ? funcionario : null;
        }

        public PagedResult<Funcionario> ListarPaginadoPorSalao(int idSalao, string? pesquisa, int pageIndex, int pageSize)
        {
            var result = new PagedResult<Funcionario>
            {
                PageIndex = pageIndex < 1 ? 1 : pageIndex,
                PageSize = pageSize < 1 ? 10 : pageSize
            };

            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            using (var connection = _dbHandler.GetConnection())
            {
                string countQuery = @"
        SELECT COUNT(*)
        FROM CorteCor_Funcionario
        WHERE IdSalao = @IdSalao
          AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa);";

                using (var countCommand = connection.CreateCommand(countQuery))
                {
                    countCommand.AddWithValue("@IdSalao", idSalao);
                    countCommand.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    result.TotalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                string query = @"
        SELECT IdFuncionario, Nome,
               seg, seg_ini, seg_fim,
               ter, ter_ini, ter_fim,
               qua, qua_ini, qua_fim,
               qui, qui_ini, qui_fim,
               sex, sex_ini, sex_fim,
               sab, sab_ini, sab_fim,
               dom, dom_ini, dom_fim,
               IdSalao
        FROM CorteCor_Funcionario
        WHERE IdSalao = @IdSalao
          AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa)
        ORDER BY Nome
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    command.AddWithValue("@Offset", (result.PageIndex - 1) * result.PageSize);
                    command.AddWithValue("@PageSize", result.PageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new Funcionario
                            {
                                IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                                seg = reader["seg"] is DBNull ? false : Convert.ToBoolean(reader["seg"]),
                                seg_ini = reader["seg_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_ini"],
                                seg_fim = reader["seg_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["seg_fim"],
                                ter = reader["ter"] is DBNull ? false : Convert.ToBoolean(reader["ter"]),
                                ter_ini = reader["ter_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_ini"],
                                ter_fim = reader["ter_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["ter_fim"],
                                qua = reader["qua"] is DBNull ? false : Convert.ToBoolean(reader["qua"]),
                                qua_ini = reader["qua_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_ini"],
                                qua_fim = reader["qua_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qua_fim"],
                                qui = reader["qui"] is DBNull ? false : Convert.ToBoolean(reader["qui"]),
                                qui_ini = reader["qui_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_ini"],
                                qui_fim = reader["qui_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["qui_fim"],
                                sex = reader["sex"] is DBNull ? false : Convert.ToBoolean(reader["sex"]),
                                sex_ini = reader["sex_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_ini"],
                                sex_fim = reader["sex_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sex_fim"],
                                sab = reader["sab"] is DBNull ? false : Convert.ToBoolean(reader["sab"]),
                                sab_ini = reader["sab_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_ini"],
                                sab_fim = reader["sab_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["sab_fim"],
                                dom = reader["dom"] is DBNull ? false : Convert.ToBoolean(reader["dom"]),
                                dom_ini = reader["dom_ini"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_ini"],
                                dom_fim = reader["dom_fim"] is DBNull ? (TimeSpan?)null : (TimeSpan)reader["dom_fim"],
                                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        public void Atualizar(Funcionario funcionario)
        {
            string query = @"
        UPDATE CorteCor_Funcionario
        SET Nome = @Nome,
            seg = @seg, seg_ini = @seg_ini, seg_fim = @seg_fim,
            ter = @ter, ter_ini = @ter_ini, ter_fim = @ter_fim,
            qua = @qua, qua_ini = @qua_ini, qua_fim = @qua_fim,
            qui = @qui, qui_ini = @qui_ini, qui_fim = @qui_fim,
            sex = @sex, sex_ini = @sex_ini, sex_fim = @sex_fim,
            sab = @sab, sab_ini = @sab_ini, sab_fim = @sab_fim,
            dom = @dom, dom_ini = @dom_ini, dom_fim = @dom_fim,
            IdSalao = @IdSalao
        WHERE IdFuncionario = @IdFuncionario AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", funcionario.Nome ?? "");
                command.AddWithValue("@seg", funcionario.seg);
                command.AddWithValue("@seg_ini", (object?)funcionario.seg_ini ?? DBNull.Value);
                command.AddWithValue("@seg_fim", (object?)funcionario.seg_fim ?? DBNull.Value);

                command.AddWithValue("@ter", funcionario.ter);
                command.AddWithValue("@ter_ini", (object?)funcionario.ter_ini ?? DBNull.Value);
                command.AddWithValue("@ter_fim", (object?)funcionario.ter_fim ?? DBNull.Value);

                command.AddWithValue("@qua", funcionario.qua);
                command.AddWithValue("@qua_ini", (object?)funcionario.qua_ini ?? DBNull.Value);
                command.AddWithValue("@qua_fim", (object?)funcionario.qua_fim ?? DBNull.Value);

                command.AddWithValue("@qui", funcionario.qui);
                command.AddWithValue("@qui_ini", (object?)funcionario.qui_ini ?? DBNull.Value);
                command.AddWithValue("@qui_fim", (object?)funcionario.qui_fim ?? DBNull.Value);

                command.AddWithValue("@sex", funcionario.sex);
                command.AddWithValue("@sex_ini", (object?)funcionario.sex_ini ?? DBNull.Value);
                command.AddWithValue("@sex_fim", (object?)funcionario.sex_fim ?? DBNull.Value);

                command.AddWithValue("@sab", funcionario.sab);
                command.AddWithValue("@sab_ini", (object?)funcionario.sab_ini ?? DBNull.Value);
                command.AddWithValue("@sab_fim", (object?)funcionario.sab_fim ?? DBNull.Value);

                command.AddWithValue("@dom", funcionario.dom);
                command.AddWithValue("@dom_ini", (object?)funcionario.dom_ini ?? DBNull.Value);
                command.AddWithValue("@dom_fim", (object?)funcionario.dom_fim ?? DBNull.Value);

                command.AddWithValue("@IdSalao", funcionario.IdSalao);
                command.AddWithValue("@IdFuncionario", funcionario.IdFuncionario);

                command.ExecuteNonQuery();
            }
        }

        public override List<Funcionario> Listar() => ListarPorSalao(0);

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_Funcionario WHERE IdFuncionario = @IdFuncionario";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idFuncionario, int idSalao)
        {
            string query = "DELETE FROM CorteCor_Funcionario WHERE IdFuncionario = @IdFuncionario AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotSupportedException("CorteCor_Funcionario não possui campo Status.");
        }

        public override void Cadastrar(Funcionario entity)
        {
            throw new NotImplementedException();
        }
    }

    public class ServicoHandler : EntityHandler<Servico>
    {
        public ServicoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler)
        {
        }

        public virtual int CadastrarServico(Servico servico)
        {
            int novoId = 0;

            string query = @"
        INSERT INTO CorteCor_Servico
            (Nome, Preco, PrecoCusto, MargemContribuicao, Duracao, IdSalao, IdCategoria, CodigoTributacaoMunicipio, Cnae, AliquotaISS,
             Tags, Anotacoes, ItemListaServicoLC116, IdCnae, CodTributacaoNacional, CodNBS, Arquivado)
        VALUES
            (@Nome, @Preco, @PrecoCusto, @MargemContribuicao, @Duracao, @IdSalao, @IdCategoria, @CodigoTributacaoMunicipio, @Cnae, @AliquotaISS,
             @Tags, @Anotacoes, @ItemListaServicoLC116, @IdCnae, @CodTributacaoNacional, @CodNBS, @Arquivado);
        SELECT SCOPE_IDENTITY();
";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", servico.Nome ?? "");
                command.AddWithValue("@Preco", servico.Preco);
                command.AddWithValue("@PrecoCusto", servico.PrecoCusto.HasValue ? (object)servico.PrecoCusto.Value : DBNull.Value);
                command.AddWithValue("@MargemContribuicao", servico.MargemContribuicao.HasValue ? (object)servico.MargemContribuicao.Value : DBNull.Value);
                command.AddWithValue("@Duracao", servico.Duracao);
                command.AddWithValue("@IdSalao", servico.IdSalao);
                command.AddWithValue("@IdCategoria", servico.IdCategoria.HasValue ? (object)servico.IdCategoria.Value : DBNull.Value);
                command.AddWithValue("@CodigoTributacaoMunicipio", string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) ? (object)DBNull.Value : servico.CodigoTributacaoMunicipio);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(servico.Cnae) ? (object)DBNull.Value : servico.Cnae);
                command.AddWithValue("@AliquotaISS", servico.AliquotaISS.HasValue ? (object)servico.AliquotaISS.Value : DBNull.Value);
                
                command.AddWithValue("@Tags", string.IsNullOrWhiteSpace(servico.Tags) ? (object)DBNull.Value : servico.Tags);
                command.AddWithValue("@Anotacoes", string.IsNullOrWhiteSpace(servico.Anotacoes) ? (object)DBNull.Value : servico.Anotacoes);
                command.AddWithValue("@ItemListaServicoLC116", string.IsNullOrWhiteSpace(servico.ItemListaServicoLC116) ? (object)DBNull.Value : servico.ItemListaServicoLC116);
                command.AddWithValue("@IdCnae", string.IsNullOrWhiteSpace(servico.IdCnae) ? (object)DBNull.Value : servico.IdCnae);
                command.AddWithValue("@CodTributacaoNacional", string.IsNullOrWhiteSpace(servico.CodTributacaoNacional) ? (object)DBNull.Value : servico.CodTributacaoNacional);
                command.AddWithValue("@CodNBS", string.IsNullOrWhiteSpace(servico.CodNBS) ? (object)DBNull.Value : servico.CodNBS);
                command.AddWithValue("@Arquivado", servico.Arquivado);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }

            return novoId;
        }

        public virtual Servico ObterPorId(int idServico)
        {
            string query = @"
        SELECT IdServico, Nome, Preco, PrecoCusto, MargemContribuicao, Duracao, IdSalao, IdCategoria, CodigoTributacaoMunicipio, Cnae, AliquotaISS,
               Tags, Anotacoes, ItemListaServicoLC116, IdCnae, CodTributacaoNacional, CodNBS, Arquivado
        FROM CorteCor_Servico
        WHERE IdServico = @IdServico;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdServico", idServico);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new Servico
                    {
                        IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                            PrecoCusto = reader["PrecoCusto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PrecoCusto"]),
                            MargemContribuicao = reader["MargemContribuicao"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["MargemContribuicao"]),
                            Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        IdCategoria = reader["IdCategoria"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdCategoria"]),
                        CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                        Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                        AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"]),
                        Tags = reader["Tags"] is DBNull ? null : reader["Tags"].ToString(),
                        Anotacoes = reader["Anotacoes"] is DBNull ? null : reader["Anotacoes"].ToString(),
                        ItemListaServicoLC116 = reader["ItemListaServicoLC116"] is DBNull ? null : reader["ItemListaServicoLC116"].ToString(),
                        IdCnae = reader["IdCnae"] is DBNull ? null : reader["IdCnae"].ToString(),
                        CodTributacaoNacional = reader["CodTributacaoNacional"] is DBNull ? null : reader["CodTributacaoNacional"].ToString(),
                        CodNBS = reader["CodNBS"] is DBNull ? null : reader["CodNBS"].ToString(),
                        Arquivado = reader["Arquivado"] is DBNull ? false : Convert.ToBoolean(reader["Arquivado"])
                    };
                }
            }
        }

        public override List<Servico> Listar()
        {
            string query = @"
        SELECT IdServico, Nome, Preco, PrecoCusto, MargemContribuicao, Duracao, IdSalao, IdCategoria, CodigoTributacaoMunicipio, Cnae, AliquotaISS,
               Tags, Anotacoes, ItemListaServicoLC116, IdCnae, CodTributacaoNacional, CodNBS, Arquivado
        FROM CorteCor_Servico
        ORDER BY Nome;";

            var servicos = new List<Servico>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    servicos.Add(new Servico
                    {
                        IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                            PrecoCusto = reader["PrecoCusto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PrecoCusto"]),
                            MargemContribuicao = reader["MargemContribuicao"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["MargemContribuicao"]),
                            Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            IdCategoria = reader["IdCategoria"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdCategoria"]),
                        CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                        Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                        AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"]),
                        Tags = reader["Tags"] is DBNull ? null : reader["Tags"].ToString(),
                        Anotacoes = reader["Anotacoes"] is DBNull ? null : reader["Anotacoes"].ToString(),
                        ItemListaServicoLC116 = reader["ItemListaServicoLC116"] is DBNull ? null : reader["ItemListaServicoLC116"].ToString(),
                        IdCnae = reader["IdCnae"] is DBNull ? null : reader["IdCnae"].ToString(),
                        CodTributacaoNacional = reader["CodTributacaoNacional"] is DBNull ? null : reader["CodTributacaoNacional"].ToString(),
                        CodNBS = reader["CodNBS"] is DBNull ? null : reader["CodNBS"].ToString(),
                        Arquivado = reader["Arquivado"] is DBNull ? false : Convert.ToBoolean(reader["Arquivado"])
                    });
                }
            }
            return servicos;
        }

        public virtual List<Servico> ListarPorSalao(int idSalao, int? idCategoria = null)
        {
            string query = @"
        SELECT IdServico, Nome, Preco, PrecoCusto, MargemContribuicao, Duracao, IdSalao, IdCategoria, CodigoTributacaoMunicipio, Cnae, AliquotaISS,
               Tags, Anotacoes, ItemListaServicoLC116, IdCnae, CodTributacaoNacional, CodNBS, Arquivado
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
          AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
        ORDER BY Nome;";

            var servicos = new List<Servico>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@IdCategoria", (object?)idCategoria ?? DBNull.Value);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        servicos.Add(new Servico
                        {
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                            PrecoCusto = reader["PrecoCusto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PrecoCusto"]),
                            MargemContribuicao = reader["MargemContribuicao"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["MargemContribuicao"]),
                            Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            IdCategoria = reader["IdCategoria"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdCategoria"]),
                            CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                            Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                            AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"]),
                            Tags = reader["Tags"] is DBNull ? null : reader["Tags"].ToString(),
                            Anotacoes = reader["Anotacoes"] is DBNull ? null : reader["Anotacoes"].ToString(),
                            ItemListaServicoLC116 = reader["ItemListaServicoLC116"] is DBNull ? null : reader["ItemListaServicoLC116"].ToString(),
                            IdCnae = reader["IdCnae"] is DBNull ? null : reader["IdCnae"].ToString(),
                            CodTributacaoNacional = reader["CodTributacaoNacional"] is DBNull ? null : reader["CodTributacaoNacional"].ToString(),
                            CodNBS = reader["CodNBS"] is DBNull ? null : reader["CodNBS"].ToString(),
                            Arquivado = reader["Arquivado"] is DBNull ? false : Convert.ToBoolean(reader["Arquivado"])
                        });
                    }
                }
            }
            return servicos;
        }

        public virtual Servico? ObterPorIdESalao(int idServico, int idSalao)
        {
            var servico = ObterPorId(idServico);
            return servico != null && servico.IdSalao == idSalao ? servico : null;
        }

        public bool ExisteNomePorSalao(string nome, int idSalao, int? ignorarIdServico = null)
        {
            string query = @"
        SELECT COUNT(*)
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
          AND Nome = @Nome
          AND (@IgnorarIdServico IS NULL OR IdServico <> @IgnorarIdServico);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@Nome", nome?.Trim() ?? "");
                command.AddWithValue("@IgnorarIdServico", (object?)ignorarIdServico ?? DBNull.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public PagedResult<Servico> ListarPaginadoPorSalao(int idSalao, int? idCategoria, string? pesquisa, bool incluirArquivados, int pageIndex, int pageSize)
        {
            var result = new PagedResult<Servico>
            {
                PageIndex = pageIndex < 1 ? 1 : pageIndex,
                PageSize = pageSize < 1 ? 10 : pageSize
            };

            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            using (var connection = _dbHandler.GetConnection())
            {
                string countQuery = @"
        SELECT COUNT(*)
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
          AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
          AND (@IncluirArquivados = 1 OR ISNULL(Arquivado, 0) = 0)
          AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR Tags LIKE @Pesquisa);";

                using (var countCommand = connection.CreateCommand(countQuery))
                {
                    countCommand.AddWithValue("@IdSalao", idSalao);
                    countCommand.AddWithValue("@IdCategoria", (object?)idCategoria ?? DBNull.Value);
                    countCommand.AddWithValue("@IncluirArquivados", incluirArquivados);
                    countCommand.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    result.TotalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                string query = @"
        SELECT IdServico, Nome, Preco, PrecoCusto, MargemContribuicao, Duracao, IdSalao, IdCategoria, CodigoTributacaoMunicipio, Cnae, AliquotaISS,
               Tags, Anotacoes, ItemListaServicoLC116, IdCnae, CodTributacaoNacional, CodNBS, Arquivado
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
          AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
          AND (@IncluirArquivados = 1 OR ISNULL(Arquivado, 0) = 0)
          AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR Tags LIKE @Pesquisa)
        ORDER BY Nome
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@IdCategoria", (object?)idCategoria ?? DBNull.Value);
                    command.AddWithValue("@IncluirArquivados", incluirArquivados);
                    command.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    command.AddWithValue("@Offset", (result.PageIndex - 1) * result.PageSize);
                    command.AddWithValue("@PageSize", result.PageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new Servico
                            {
                                IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                                Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                                PrecoCusto = reader["PrecoCusto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PrecoCusto"]),
                                MargemContribuicao = reader["MargemContribuicao"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["MargemContribuicao"]),
                                Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                                IdCategoria = reader["IdCategoria"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdCategoria"]),
                                CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                                Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                                AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"]),
                                Tags = reader["Tags"] is DBNull ? null : reader["Tags"].ToString(),
                                Anotacoes = reader["Anotacoes"] is DBNull ? null : reader["Anotacoes"].ToString(),
                                ItemListaServicoLC116 = reader["ItemListaServicoLC116"] is DBNull ? null : reader["ItemListaServicoLC116"].ToString(),
                                IdCnae = reader["IdCnae"] is DBNull ? null : reader["IdCnae"].ToString(),
                                CodTributacaoNacional = reader["CodTributacaoNacional"] is DBNull ? null : reader["CodTributacaoNacional"].ToString(),
                                CodNBS = reader["CodNBS"] is DBNull ? null : reader["CodNBS"].ToString(),
                                Arquivado = reader["Arquivado"] is DBNull ? false : Convert.ToBoolean(reader["Arquivado"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        public void Atualizar(Servico servico)
        {
            string query = @"
        UPDATE CorteCor_Servico
        SET Nome = @Nome,
            Preco = @Preco,
            PrecoCusto = @PrecoCusto,
            MargemContribuicao = @MargemContribuicao,
            Duracao = @Duracao,
            IdSalao = @IdSalao,
            IdCategoria = @IdCategoria,
            CodigoTributacaoMunicipio = @CodigoTributacaoMunicipio,
            Cnae = @Cnae,
            AliquotaISS = @AliquotaISS,
            Tags = @Tags,
            Anotacoes = @Anotacoes,
            ItemListaServicoLC116 = @ItemListaServicoLC116,
            IdCnae = @IdCnae,
            CodTributacaoNacional = @CodTributacaoNacional,
            CodNBS = @CodNBS,
            Arquivado = @Arquivado
        WHERE IdServico = @IdServico AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", servico.Nome ?? "");
                command.AddWithValue("@Preco", servico.Preco);
                command.AddWithValue("@PrecoCusto", servico.PrecoCusto.HasValue ? (object)servico.PrecoCusto.Value : DBNull.Value);
                command.AddWithValue("@MargemContribuicao", servico.MargemContribuicao.HasValue ? (object)servico.MargemContribuicao.Value : DBNull.Value);
                command.AddWithValue("@Duracao", servico.Duracao);
                command.AddWithValue("@IdSalao", servico.IdSalao);
                command.AddWithValue("@IdCategoria", servico.IdCategoria.HasValue ? (object)servico.IdCategoria.Value : DBNull.Value);
                command.AddWithValue("@CodigoTributacaoMunicipio", string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) ? (object)DBNull.Value : servico.CodigoTributacaoMunicipio);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(servico.Cnae) ? (object)DBNull.Value : servico.Cnae);
                command.AddWithValue("@AliquotaISS", servico.AliquotaISS.HasValue ? (object)servico.AliquotaISS.Value : DBNull.Value);
                
                command.AddWithValue("@Tags", string.IsNullOrWhiteSpace(servico.Tags) ? (object)DBNull.Value : servico.Tags);
                command.AddWithValue("@Anotacoes", string.IsNullOrWhiteSpace(servico.Anotacoes) ? (object)DBNull.Value : servico.Anotacoes);
                command.AddWithValue("@ItemListaServicoLC116", string.IsNullOrWhiteSpace(servico.ItemListaServicoLC116) ? (object)DBNull.Value : servico.ItemListaServicoLC116);
                command.AddWithValue("@IdCnae", string.IsNullOrWhiteSpace(servico.IdCnae) ? (object)DBNull.Value : servico.IdCnae);
                command.AddWithValue("@CodTributacaoNacional", string.IsNullOrWhiteSpace(servico.CodTributacaoNacional) ? (object)DBNull.Value : servico.CodTributacaoNacional);
                command.AddWithValue("@CodNBS", string.IsNullOrWhiteSpace(servico.CodNBS) ? (object)DBNull.Value : servico.CodNBS);
                command.AddWithValue("@Arquivado", servico.Arquivado);
                
                command.AddWithValue("@IdServico", servico.IdServico);

                command.ExecuteNonQuery();
            }
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_Servico WHERE IdServico = @IdServico";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdServico", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idServico, int idSalao)
        {
            string query = "DELETE FROM CorteCor_Servico WHERE IdServico = @IdServico AND IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdServico", idServico);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        // Esta tabela não tem Status. Mantive o método para bater com a base EntityHandler<T>.
        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotSupportedException("CorteCor_Servico não possui campo Status.");
        }

        public override void Cadastrar(Servico entity)
        {
            throw new NotImplementedException();
        }
    }


    public class FuncionarioServicoHandler : EntityHandler<FuncionarioServico>
    {
        public FuncionarioServicoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        /// <summary>
        /// Vincula um serviço a um funcionário (N:N).
        /// </summary>
        public virtual void Vincular(int idFuncionario, int idServico)
        {
            string query = @"
        INSERT INTO CorteCor_Funcionario_Servico (IdFuncionario, IdServico)
        VALUES (@IdFuncionario, @IdServico);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.AddWithValue("@IdServico", idServico);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Remove o vínculo entre um funcionário e um serviço.
        /// </summary>
        public virtual void Desvincular(int idFuncionario, int idServico)
        {
            string query = @"
        DELETE FROM CorteCor_Funcionario_Servico
        WHERE IdFuncionario = @IdFuncionario AND IdServico = @IdServico;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.AddWithValue("@IdServico", idServico);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Lista todos os vínculos (N:N).
        /// </summary>
        public override List<FuncionarioServico> Listar()
        {
            string query = @"
        SELECT IdFuncionario, IdServico
        FROM CorteCor_Funcionario_Servico
        ORDER BY IdFuncionario, IdServico;";

            var itens = new List<FuncionarioServico>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    itens.Add(new FuncionarioServico
                    {
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                        IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"])
                    });
                }
            }

            return itens;
        }

        /// <summary>
        /// Lista os serviços vinculados a um funcionário (retorna apenas os IDs).
        /// </summary>
        public virtual List<int> ListarServicosDoFuncionario(int idFuncionario)
        {
            string query = @"
        SELECT IdServico
        FROM CorteCor_Funcionario_Servico
        WHERE IdFuncionario = @IdFuncionario
        ORDER BY IdServico;";

            var servicos = new List<int>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        servicos.Add(reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]));
                    }
                }
            }

            return servicos;
        }

        /// <summary>
        /// Lista os funcionários vinculados a um serviço (retorna apenas os IDs).
        /// </summary>
        public virtual List<int> ListarFuncionariosDoServico(int idServico)
        {
            string query = @"
        SELECT IdFuncionario
        FROM CorteCor_Funcionario_Servico
        WHERE IdServico = @IdServico
        ORDER BY IdFuncionario;";

            var funcionarios = new List<int>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdServico", idServico);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        funcionarios.Add(reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]));
                    }
                }
            }

            return funcionarios;
        }

        /// <summary>
        /// Remove todos os vínculos de um funcionário.
        /// </summary>
        public void LimparServicosDoFuncionario(int idFuncionario)
        {
            string query = "DELETE FROM CorteCor_Funcionario_Servico WHERE IdFuncionario = @IdFuncionario;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Remove todos os vínculos de um serviço.
        /// </summary>
        public void LimparFuncionariosDoServico(int idServico)
        {
            string query = "DELETE FROM CorteCor_Funcionario_Servico WHERE IdServico = @IdServico;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdServico", idServico);
                command.ExecuteNonQuery();
            }
        }

        // Métodos do EntityHandler<T> que não fazem sentido aqui (não há PK única nem Status).
        public override void Excluir(int id)
        {
            throw new NotSupportedException("Use Desvincular() ou Limpar... para remover vínculos.");
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotSupportedException("CorteCor_Funcionario_Servico não possui campo Status.");
        }

        public override void Cadastrar(FuncionarioServico entity)
        {
            // Mantive o padrão da base, mas aqui o ideal é chamar Vincular().
            Vincular(entity.IdFuncionario, entity.IdServico);
        }

        public void ExcluirPorFuncionario(int idFuncionario)
        {
            string query = @"
                DELETE FROM CorteCor_Funcionario_Servico
                WHERE IdFuncionario = @IdFuncionario";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.ExecuteNonQuery();
            }
        }

        // (opcional, mas você já está usando no PageModel)
        public List<FuncionarioServico> ListarPorFuncionario(int idFuncionario)
        {
            string query = @"
                SELECT IdFuncionario, IdServico
                FROM CorteCor_Funcionario_Servico
                WHERE IdFuncionario = @IdFuncionario";

            var lista = new List<FuncionarioServico>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new FuncionarioServico
                        {
                            IdFuncionario = Convert.ToInt32(reader["IdFuncionario"]),
                            IdServico = Convert.ToInt32(reader["IdServico"])
                        });
                    }
                }
            }

            return lista;
        }

        //public override void Cadastrar(FuncionarioServico entity)
        //{
        //    string query = @"
        //            INSERT INTO CorteCor_Funcionario_Servico (IdFuncionario, IdServico)
        //            VALUES (@IdFuncionario, @IdServico)";

        //    using (var connection = _dbHandler.GetConnection())
        //    using (var command = connection.CreateCommand(query))
        //    {
        //        command.AddWithValue("@IdFuncionario", entity.IdFuncionario);
        //        command.AddWithValue("@IdServico", entity.IdServico);
        //        command.ExecuteNonQuery();
        //    }
        //}



    }

    public class PessoaHandler : EntityHandler<Pessoa>
    {
        public PessoaHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }

        private static object ValorEnderecoOpcional(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Trim();

        public virtual int CadastrarPessoa(Pessoa pessoa)
        {
            int novoId = 0;

            string query = @"
        INSERT INTO CorteCor_Pessoa
            (Nome, Telefone, Email, DataNascimento, IdSalao, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF, RazaoSocial, NomeFantasia, Cnae,
             IsCliente, IsFornecedor, IsLead, IsTransportador, NomeContato, Pais, IdEstrangeiro, 
             EntCep, EntUf, EntCidade, EntNome, EntCpfCnpj, EntInscricaoEstadual, EntLogradouro, EntNumero, EntComplemento, EntBairro, EntEmail, EntTelefone,
             ConsumidorFinal, IndicadorIE, IESubstTrib, Suframa, Tags, DataComemorativa, DescricaoComemoracao, BasesLegais, Observacoes)
        VALUES
            (@Nome, @Telefone, @Email, @DataNascimento, @IdSalao, @CpfCnpj, @InscricaoEstadual, @InscricaoMunicipal, @Cep, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @UF, @RazaoSocial, @NomeFantasia, @Cnae,
             @IsCliente, @IsFornecedor, @IsLead, @IsTransportador, @NomeContato, @Pais, @IdEstrangeiro,
             @EntCep, @EntUf, @EntCidade, @EntNome, @EntCpfCnpj, @EntInscricaoEstadual, @EntLogradouro, @EntNumero, @EntComplemento, @EntBairro, @EntEmail, @EntTelefone,
             @ConsumidorFinal, @IndicadorIE, @IESubstTrib, @Suframa, @Tags, @DataComemorativa, @DescricaoComemoracao, @BasesLegais, @Observacoes);
        SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", pessoa.Nome ?? "");

                command.AddWithValue("@Telefone", string.IsNullOrWhiteSpace(pessoa.Telefone)
                    ? (object)DBNull.Value
                    : pessoa.Telefone);

                command.AddWithValue("@Email", string.IsNullOrWhiteSpace(pessoa.Email)
                    ? (object)DBNull.Value
                    : pessoa.Email);

                command.AddWithValue("@DataNascimento", pessoa.DataNascimento.HasValue
                    ? (object)pessoa.DataNascimento.Value.Date
                    : DBNull.Value);

                command.AddWithValue("@IdSalao", pessoa.IdSalao);

                command.AddWithValue("@CpfCnpj", string.IsNullOrWhiteSpace(pessoa.CpfCnpj) ? (object)DBNull.Value : pessoa.CpfCnpj);
                command.AddWithValue("@InscricaoEstadual", string.IsNullOrWhiteSpace(pessoa.InscricaoEstadual) ? (object)DBNull.Value : pessoa.InscricaoEstadual);
                command.AddWithValue("@InscricaoMunicipal", string.IsNullOrWhiteSpace(pessoa.InscricaoMunicipal) ? (object)DBNull.Value : pessoa.InscricaoMunicipal);
                command.AddWithValue("@Cep", ValorEnderecoOpcional(pessoa.Cep));
                command.AddWithValue("@Logradouro", ValorEnderecoOpcional(pessoa.Logradouro));
                command.AddWithValue("@Numero", ValorEnderecoOpcional(pessoa.Numero));
                command.AddWithValue("@Complemento", ValorEnderecoOpcional(pessoa.Complemento));
                command.AddWithValue("@Bairro", ValorEnderecoOpcional(pessoa.Bairro));
                command.AddWithValue("@Cidade", ValorEnderecoOpcional(pessoa.Cidade));
                command.AddWithValue("@UF", ValorEnderecoOpcional(pessoa.UF));
                command.AddWithValue("@RazaoSocial", string.IsNullOrWhiteSpace(pessoa.RazaoSocial) ? (object)DBNull.Value : pessoa.RazaoSocial);
                command.AddWithValue("@NomeFantasia", string.IsNullOrWhiteSpace(pessoa.NomeFantasia) ? (object)DBNull.Value : pessoa.NomeFantasia);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(pessoa.Cnae) ? (object)DBNull.Value : pessoa.Cnae);

                // Novos Campos
                command.AddWithValue("@IsCliente", pessoa.IsCliente);
                command.AddWithValue("@IsFornecedor", pessoa.IsFornecedor);
                command.AddWithValue("@IsLead", pessoa.IsLead);
                command.AddWithValue("@IsTransportador", pessoa.IsTransportador);
                command.AddWithValue("@NomeContato", string.IsNullOrWhiteSpace(pessoa.NomeContato) ? (object)DBNull.Value : pessoa.NomeContato);
                command.AddWithValue("@Pais", string.IsNullOrWhiteSpace(pessoa.Pais) ? (object)DBNull.Value : pessoa.Pais);
                command.AddWithValue("@IdEstrangeiro", string.IsNullOrWhiteSpace(pessoa.IdEstrangeiro) ? (object)DBNull.Value : pessoa.IdEstrangeiro);

                command.AddWithValue("@EntCep", ValorEnderecoOpcional(pessoa.EntCep));
                command.AddWithValue("@EntUf", ValorEnderecoOpcional(pessoa.EntUf));
                command.AddWithValue("@EntCidade", ValorEnderecoOpcional(pessoa.EntCidade));
                command.AddWithValue("@EntNome", string.IsNullOrWhiteSpace(pessoa.EntNome) ? (object)DBNull.Value : pessoa.EntNome);
                command.AddWithValue("@EntCpfCnpj", string.IsNullOrWhiteSpace(pessoa.EntCpfCnpj) ? (object)DBNull.Value : pessoa.EntCpfCnpj);
                command.AddWithValue("@EntInscricaoEstadual", string.IsNullOrWhiteSpace(pessoa.EntInscricaoEstadual) ? (object)DBNull.Value : pessoa.EntInscricaoEstadual);
                command.AddWithValue("@EntLogradouro", ValorEnderecoOpcional(pessoa.EntLogradouro));
                command.AddWithValue("@EntNumero", ValorEnderecoOpcional(pessoa.EntNumero));
                command.AddWithValue("@EntComplemento", ValorEnderecoOpcional(pessoa.EntComplemento));
                command.AddWithValue("@EntBairro", ValorEnderecoOpcional(pessoa.EntBairro));
                command.AddWithValue("@EntEmail", string.IsNullOrWhiteSpace(pessoa.EntEmail) ? (object)DBNull.Value : pessoa.EntEmail);
                command.AddWithValue("@EntTelefone", string.IsNullOrWhiteSpace(pessoa.EntTelefone) ? (object)DBNull.Value : pessoa.EntTelefone);

                command.AddWithValue("@ConsumidorFinal", pessoa.ConsumidorFinal.HasValue ? (object)pessoa.ConsumidorFinal.Value : DBNull.Value);
                command.AddWithValue("@IndicadorIE", pessoa.IndicadorIE.HasValue ? (object)pessoa.IndicadorIE.Value : DBNull.Value);
                command.AddWithValue("@IESubstTrib", string.IsNullOrWhiteSpace(pessoa.IESubstTrib) ? (object)DBNull.Value : pessoa.IESubstTrib);
                command.AddWithValue("@Suframa", string.IsNullOrWhiteSpace(pessoa.Suframa) ? (object)DBNull.Value : pessoa.Suframa);

                command.AddWithValue("@Tags", string.IsNullOrWhiteSpace(pessoa.Tags) ? (object)DBNull.Value : pessoa.Tags);
                command.AddWithValue("@DataComemorativa", pessoa.DataComemorativa.HasValue ? (object)pessoa.DataComemorativa.Value.Date : DBNull.Value);
                command.AddWithValue("@DescricaoComemoracao", string.IsNullOrWhiteSpace(pessoa.DescricaoComemoracao) ? (object)DBNull.Value : pessoa.DescricaoComemoracao);
                command.AddWithValue("@BasesLegais", string.IsNullOrWhiteSpace(pessoa.BasesLegais) ? (object)DBNull.Value : pessoa.BasesLegais);
                command.AddWithValue("@Observacoes", string.IsNullOrWhiteSpace(pessoa.Observacoes) ? (object)DBNull.Value : pessoa.Observacoes);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    novoId = id;
            }

            return novoId;
        }

        public virtual Pessoa ObterPorId(int idPessoa)
        {
            string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF, RazaoSocial, NomeFantasia, Cnae,
               IsCliente, IsFornecedor, IsLead, IsTransportador, NomeContato, Pais, IdEstrangeiro,
               EntCep, EntUf, EntCidade, EntNome, EntCpfCnpj, EntInscricaoEstadual, EntLogradouro, EntNumero, EntComplemento, EntBairro, EntEmail, EntTelefone,
               ConsumidorFinal, IndicadorIE, IESubstTrib, Suframa, Tags, DataComemorativa, DescricaoComemoracao, BasesLegais, Observacoes
        FROM CorteCor_Pessoa
        WHERE IdPessoa = @IdPessoa;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdPessoa", idPessoa);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new Pessoa
                    {
                        IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                        Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                        Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                        DataNascimento = reader["DataNascimento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataNascimento"]),
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"]),
                        CpfCnpj = reader["CpfCnpj"] is DBNull ? "" : reader["CpfCnpj"].ToString(),
                        InscricaoEstadual = reader["InscricaoEstadual"] is DBNull ? "" : reader["InscricaoEstadual"].ToString(),
                        InscricaoMunicipal = reader["InscricaoMunicipal"] is DBNull ? "" : reader["InscricaoMunicipal"].ToString(),
                        Cep = reader["Cep"] is DBNull ? "" : reader["Cep"].ToString(),
                        Logradouro = reader["Logradouro"] is DBNull ? "" : reader["Logradouro"].ToString(),
                        Numero = reader["Numero"] is DBNull ? "" : reader["Numero"].ToString(),
                        Complemento = reader["Complemento"] is DBNull ? "" : reader["Complemento"].ToString(),
                        Bairro = reader["Bairro"] is DBNull ? "" : reader["Bairro"].ToString(),
                        Cidade = reader["Cidade"] is DBNull ? "" : reader["Cidade"].ToString(),
                        UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString(),
                        RazaoSocial = reader["RazaoSocial"] is DBNull ? "" : reader["RazaoSocial"].ToString(),
                        NomeFantasia = reader["NomeFantasia"] is DBNull ? "" : reader["NomeFantasia"].ToString(),
                        Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                        IsCliente = reader["IsCliente"] is DBNull ? false : Convert.ToBoolean(reader["IsCliente"]),
                        IsFornecedor = reader["IsFornecedor"] is DBNull ? false : Convert.ToBoolean(reader["IsFornecedor"]),
                        IsLead = reader["IsLead"] is DBNull ? false : Convert.ToBoolean(reader["IsLead"]),
                        IsTransportador = reader["IsTransportador"] is DBNull ? false : Convert.ToBoolean(reader["IsTransportador"]),
                        NomeContato = reader["NomeContato"] is DBNull ? "" : reader["NomeContato"].ToString(),
                        Pais = reader["Pais"] is DBNull ? "" : reader["Pais"].ToString(),
                        IdEstrangeiro = reader["IdEstrangeiro"] is DBNull ? "" : reader["IdEstrangeiro"].ToString(),
                        EntCep = reader["EntCep"] is DBNull ? "" : reader["EntCep"].ToString(),
                        EntUf = reader["EntUf"] is DBNull ? "" : reader["EntUf"].ToString(),
                        EntCidade = reader["EntCidade"] is DBNull ? "" : reader["EntCidade"].ToString(),
                        EntNome = reader["EntNome"] is DBNull ? "" : reader["EntNome"].ToString(),
                        EntCpfCnpj = reader["EntCpfCnpj"] is DBNull ? "" : reader["EntCpfCnpj"].ToString(),
                        EntInscricaoEstadual = reader["EntInscricaoEstadual"] is DBNull ? "" : reader["EntInscricaoEstadual"].ToString(),
                        EntLogradouro = reader["EntLogradouro"] is DBNull ? "" : reader["EntLogradouro"].ToString(),
                        EntNumero = reader["EntNumero"] is DBNull ? "" : reader["EntNumero"].ToString(),
                        EntComplemento = reader["EntComplemento"] is DBNull ? "" : reader["EntComplemento"].ToString(),
                        EntBairro = reader["EntBairro"] is DBNull ? "" : reader["EntBairro"].ToString(),
                        EntEmail = reader["EntEmail"] is DBNull ? "" : reader["EntEmail"].ToString(),
                        EntTelefone = reader["EntTelefone"] is DBNull ? "" : reader["EntTelefone"].ToString(),
                        ConsumidorFinal = reader["ConsumidorFinal"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConsumidorFinal"]),
                        IndicadorIE = reader["IndicadorIE"] is DBNull ? (int?)null : Convert.ToInt32(reader["IndicadorIE"]),
                        IESubstTrib = reader["IESubstTrib"] is DBNull ? "" : reader["IESubstTrib"].ToString(),
                        Suframa = reader["Suframa"] is DBNull ? "" : reader["Suframa"].ToString(),
                        Tags = reader["Tags"] is DBNull ? "" : reader["Tags"].ToString(),
                        DataComemorativa = reader["DataComemorativa"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataComemorativa"]),
                        DescricaoComemoracao = reader["DescricaoComemoracao"] is DBNull ? "" : reader["DescricaoComemoracao"].ToString(),
                        BasesLegais = reader["BasesLegais"] is DBNull ? "" : reader["BasesLegais"].ToString(),
                        Observacoes = reader["Observacoes"] is DBNull ? "" : reader["Observacoes"].ToString()
                    };
                }
            }
        }

        public override List<Pessoa> Listar()
        {
            return Listar(1, int.MaxValue).Items;
        }

        public PagedResult<Pessoa> Listar(int page, int pageSize)
        {
            var result = new PagedResult<Pessoa>
            {
                PageIndex = page,
                PageSize = pageSize
            };

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                string countQuery = "SELECT COUNT(*) FROM CorteCor_Pessoa WHERE (Excluido = 0 OR Excluido IS NULL)";
                using (var countCmd = connection.CreateCommand(countQuery))
                {
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string query = @"
            SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF, RazaoSocial, NomeFantasia, Cnae,
                   IsCliente, IsFornecedor, IsLead, IsTransportador, NomeContato, Pais, IdEstrangeiro,
                   EntCep, EntUf, EntCidade, EntNome, EntCpfCnpj, EntInscricaoEstadual, EntLogradouro, EntNumero, EntComplemento, EntBairro, EntEmail, EntTelefone,
                   ConsumidorFinal, IndicadorIE, IESubstTrib, Suframa, Tags, DataComemorativa, DescricaoComemoracao, BasesLegais, Observacoes
            FROM CorteCor_Pessoa
            WHERE (Excluido = 0 OR Excluido IS NULL)
            ORDER BY Nome
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.AddWithValue("@PageSize", pageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new Pessoa
                            {
                                IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                                Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                                Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                                DataNascimento = reader["DataNascimento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataNascimento"]),
                                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                                Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"]),
                                CpfCnpj = reader["CpfCnpj"] is DBNull ? "" : reader["CpfCnpj"].ToString(),
                                InscricaoEstadual = reader["InscricaoEstadual"] is DBNull ? "" : reader["InscricaoEstadual"].ToString(),
                                InscricaoMunicipal = reader["InscricaoMunicipal"] is DBNull ? "" : reader["InscricaoMunicipal"].ToString(),
                                Cep = reader["Cep"] is DBNull ? "" : reader["Cep"].ToString(),
                                Logradouro = reader["Logradouro"] is DBNull ? "" : reader["Logradouro"].ToString(),
                                Numero = reader["Numero"] is DBNull ? "" : reader["Numero"].ToString(),
                                Complemento = reader["Complemento"] is DBNull ? "" : reader["Complemento"].ToString(),
                                Bairro = reader["Bairro"] is DBNull ? "" : reader["Bairro"].ToString(),
                                Cidade = reader["Cidade"] is DBNull ? "" : reader["Cidade"].ToString(),
                                UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString(),
                            RazaoSocial = reader["RazaoSocial"] is DBNull ? "" : reader["RazaoSocial"].ToString(),
                            NomeFantasia = reader["NomeFantasia"] is DBNull ? "" : reader["NomeFantasia"].ToString(),
                            Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                            IsCliente = reader["IsCliente"] is DBNull ? false : Convert.ToBoolean(reader["IsCliente"]),
                            IsFornecedor = reader["IsFornecedor"] is DBNull ? false : Convert.ToBoolean(reader["IsFornecedor"]),
                            IsLead = reader["IsLead"] is DBNull ? false : Convert.ToBoolean(reader["IsLead"]),
                            IsTransportador = reader["IsTransportador"] is DBNull ? false : Convert.ToBoolean(reader["IsTransportador"]),
                            NomeContato = reader["NomeContato"] is DBNull ? "" : reader["NomeContato"].ToString(),
                            Pais = reader["Pais"] is DBNull ? "" : reader["Pais"].ToString(),
                            IdEstrangeiro = reader["IdEstrangeiro"] is DBNull ? "" : reader["IdEstrangeiro"].ToString(),
                            EntCep = reader["EntCep"] is DBNull ? "" : reader["EntCep"].ToString(),
                            EntUf = reader["EntUf"] is DBNull ? "" : reader["EntUf"].ToString(),
                            EntCidade = reader["EntCidade"] is DBNull ? "" : reader["EntCidade"].ToString(),
                            EntNome = reader["EntNome"] is DBNull ? "" : reader["EntNome"].ToString(),
                            EntCpfCnpj = reader["EntCpfCnpj"] is DBNull ? "" : reader["EntCpfCnpj"].ToString(),
                            EntInscricaoEstadual = reader["EntInscricaoEstadual"] is DBNull ? "" : reader["EntInscricaoEstadual"].ToString(),
                            EntLogradouro = reader["EntLogradouro"] is DBNull ? "" : reader["EntLogradouro"].ToString(),
                            EntNumero = reader["EntNumero"] is DBNull ? "" : reader["EntNumero"].ToString(),
                            EntComplemento = reader["EntComplemento"] is DBNull ? "" : reader["EntComplemento"].ToString(),
                            EntBairro = reader["EntBairro"] is DBNull ? "" : reader["EntBairro"].ToString(),
                            EntEmail = reader["EntEmail"] is DBNull ? "" : reader["EntEmail"].ToString(),
                            EntTelefone = reader["EntTelefone"] is DBNull ? "" : reader["EntTelefone"].ToString(),
                            ConsumidorFinal = reader["ConsumidorFinal"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConsumidorFinal"]),
                            IndicadorIE = reader["IndicadorIE"] is DBNull ? (int?)null : Convert.ToInt32(reader["IndicadorIE"]),
                            IESubstTrib = reader["IESubstTrib"] is DBNull ? "" : reader["IESubstTrib"].ToString(),
                            Suframa = reader["Suframa"] is DBNull ? "" : reader["Suframa"].ToString(),
                            Tags = reader["Tags"] is DBNull ? "" : reader["Tags"].ToString(),
                            DataComemorativa = reader["DataComemorativa"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataComemorativa"]),
                            DescricaoComemoracao = reader["DescricaoComemoracao"] is DBNull ? "" : reader["DescricaoComemoracao"].ToString(),
                            BasesLegais = reader["BasesLegais"] is DBNull ? "" : reader["BasesLegais"].ToString(),
                            Observacoes = reader["Observacoes"] is DBNull ? "" : reader["Observacoes"].ToString()
                            });
                        }
                    }
                }
            }
            return result;
        }

        public virtual List<Pessoa> ListarPorSalao(int idSalao)
        {
            string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF, RazaoSocial, NomeFantasia, Cnae,
               IsCliente, IsFornecedor, IsLead, IsTransportador, NomeContato, Pais, IdEstrangeiro,
               EntCep, EntUf, EntCidade, EntNome, EntCpfCnpj, EntInscricaoEstadual, EntLogradouro, EntNumero, EntComplemento, EntBairro, EntEmail, EntTelefone,
               ConsumidorFinal, IndicadorIE, IESubstTrib, Suframa, Tags, DataComemorativa, DescricaoComemoracao, BasesLegais, Observacoes
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao AND (Excluido = 0 OR Excluido IS NULL)
        ORDER BY Nome;";

            var pessoas = new List<Pessoa>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pessoas.Add(new Pessoa
                        {
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            DataNascimento = reader["DataNascimento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataNascimento"]),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"]),
                            CpfCnpj = reader["CpfCnpj"] is DBNull ? "" : reader["CpfCnpj"].ToString(),
                            InscricaoEstadual = reader["InscricaoEstadual"] is DBNull ? "" : reader["InscricaoEstadual"].ToString(),
                            InscricaoMunicipal = reader["InscricaoMunicipal"] is DBNull ? "" : reader["InscricaoMunicipal"].ToString(),
                            Cep = reader["Cep"] is DBNull ? "" : reader["Cep"].ToString(),
                            Logradouro = reader["Logradouro"] is DBNull ? "" : reader["Logradouro"].ToString(),
                            Numero = reader["Numero"] is DBNull ? "" : reader["Numero"].ToString(),
                            Complemento = reader["Complemento"] is DBNull ? "" : reader["Complemento"].ToString(),
                            Bairro = reader["Bairro"] is DBNull ? "" : reader["Bairro"].ToString(),
                            Cidade = reader["Cidade"] is DBNull ? "" : reader["Cidade"].ToString(),
                            UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString(),
                            RazaoSocial = reader["RazaoSocial"] is DBNull ? "" : reader["RazaoSocial"].ToString(),
                            NomeFantasia = reader["NomeFantasia"] is DBNull ? "" : reader["NomeFantasia"].ToString(),
                            Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                            IsCliente = reader["IsCliente"] is DBNull ? false : Convert.ToBoolean(reader["IsCliente"]),
                            IsFornecedor = reader["IsFornecedor"] is DBNull ? false : Convert.ToBoolean(reader["IsFornecedor"]),
                            IsLead = reader["IsLead"] is DBNull ? false : Convert.ToBoolean(reader["IsLead"]),
                            IsTransportador = reader["IsTransportador"] is DBNull ? false : Convert.ToBoolean(reader["IsTransportador"]),
                            NomeContato = reader["NomeContato"] is DBNull ? "" : reader["NomeContato"].ToString(),
                            Pais = reader["Pais"] is DBNull ? "" : reader["Pais"].ToString(),
                            IdEstrangeiro = reader["IdEstrangeiro"] is DBNull ? "" : reader["IdEstrangeiro"].ToString(),
                            EntCep = reader["EntCep"] is DBNull ? "" : reader["EntCep"].ToString(),
                            EntUf = reader["EntUf"] is DBNull ? "" : reader["EntUf"].ToString(),
                            EntCidade = reader["EntCidade"] is DBNull ? "" : reader["EntCidade"].ToString(),
                            EntNome = reader["EntNome"] is DBNull ? "" : reader["EntNome"].ToString(),
                            EntCpfCnpj = reader["EntCpfCnpj"] is DBNull ? "" : reader["EntCpfCnpj"].ToString(),
                            EntInscricaoEstadual = reader["EntInscricaoEstadual"] is DBNull ? "" : reader["EntInscricaoEstadual"].ToString(),
                            EntLogradouro = reader["EntLogradouro"] is DBNull ? "" : reader["EntLogradouro"].ToString(),
                            EntNumero = reader["EntNumero"] is DBNull ? "" : reader["EntNumero"].ToString(),
                            EntComplemento = reader["EntComplemento"] is DBNull ? "" : reader["EntComplemento"].ToString(),
                            EntBairro = reader["EntBairro"] is DBNull ? "" : reader["EntBairro"].ToString(),
                            EntEmail = reader["EntEmail"] is DBNull ? "" : reader["EntEmail"].ToString(),
                            EntTelefone = reader["EntTelefone"] is DBNull ? "" : reader["EntTelefone"].ToString(),
                            ConsumidorFinal = reader["ConsumidorFinal"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConsumidorFinal"]),
                            IndicadorIE = reader["IndicadorIE"] is DBNull ? (int?)null : Convert.ToInt32(reader["IndicadorIE"]),
                            IESubstTrib = reader["IESubstTrib"] is DBNull ? "" : reader["IESubstTrib"].ToString(),
                            Suframa = reader["Suframa"] is DBNull ? "" : reader["Suframa"].ToString(),
                            Tags = reader["Tags"] is DBNull ? "" : reader["Tags"].ToString(),
                            DataComemorativa = reader["DataComemorativa"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataComemorativa"]),
                            DescricaoComemoracao = reader["DescricaoComemoracao"] is DBNull ? "" : reader["DescricaoComemoracao"].ToString(),
                            BasesLegais = reader["BasesLegais"] is DBNull ? "" : reader["BasesLegais"].ToString(),
                            Observacoes = reader["Observacoes"] is DBNull ? "" : reader["Observacoes"].ToString()
                        });
                    }
                }
            }

            return pessoas;
        }

        public PagedResult<Pessoa> ListarPaginadoPorSalao(int idSalao, string? pesquisa, int pageIndex, int pageSize)
        {
            var result = new PagedResult<Pessoa>
            {
                PageIndex = pageIndex < 1 ? 1 : pageIndex,
                PageSize = pageSize < 1 ? 10 : pageSize
            };

            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            using (var connection = _dbHandler.GetConnection())
            {
                string countQuery = @"
        SELECT COUNT(*)
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND (Excluido = 0 OR Excluido IS NULL)
          AND (
                @Pesquisa IS NULL
                OR Nome LIKE @Pesquisa
                OR Email LIKE @Pesquisa
                OR Telefone LIKE @Pesquisa
                OR CpfCnpj LIKE @Pesquisa
              );";

                using (var countCommand = connection.CreateCommand(countQuery))
                {
                    countCommand.AddWithValue("@IdSalao", idSalao);
                    countCommand.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    result.TotalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj,
               IsCliente, IsFornecedor, IsLead, IsTransportador
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND (Excluido = 0 OR Excluido IS NULL)
          AND (
                @Pesquisa IS NULL
                OR Nome LIKE @Pesquisa
                OR Email LIKE @Pesquisa
                OR Telefone LIKE @Pesquisa
                OR CpfCnpj LIKE @Pesquisa
              )
        ORDER BY Nome
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    command.AddWithValue("@Offset", (result.PageIndex - 1) * result.PageSize);
                    command.AddWithValue("@PageSize", result.PageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new Pessoa
                            {
                                IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                                Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                                Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                                DataNascimento = reader["DataNascimento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataNascimento"]),
                                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                                Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"]),
                                CpfCnpj = reader["CpfCnpj"] is DBNull ? "" : reader["CpfCnpj"].ToString(),
                                IsCliente = reader["IsCliente"] is DBNull ? false : Convert.ToBoolean(reader["IsCliente"]),
                                IsFornecedor = reader["IsFornecedor"] is DBNull ? false : Convert.ToBoolean(reader["IsFornecedor"]),
                                IsLead = reader["IsLead"] is DBNull ? false : Convert.ToBoolean(reader["IsLead"]),
                                IsTransportador = reader["IsTransportador"] is DBNull ? false : Convert.ToBoolean(reader["IsTransportador"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<Pessoa>> BuscarParaSelecaoAsync(int idSalao, string? pesquisa, bool somenteClientes, bool somenteFornecedores, int limite = 20)
        {
            const string sql = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj,
               IsCliente, IsFornecedor, IsLead, IsTransportador
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND (Excluido = 0 OR Excluido IS NULL)
          AND (@SomenteClientes = 0 OR COALESCE(IsCliente, 0) = 1)
          AND (@SomenteFornecedores = 0 OR COALESCE(IsFornecedor, 0) = 1)
          AND (
                @Pesquisa IS NULL
                OR Nome LIKE @Pesquisa
                OR Email LIKE @Pesquisa
                OR Telefone LIKE @Pesquisa
                OR CpfCnpj LIKE @Pesquisa
              )
        ORDER BY
            CASE WHEN @PesquisaExata IS NOT NULL AND Nome = @PesquisaExata THEN 0 ELSE 1 END,
            Nome
        LIMIT @Limite;";

            var termo = string.IsNullOrWhiteSpace(pesquisa) ? null : pesquisa.Trim();
            using var connection = _dbHandler.GetConnection();
            return (await connection.QueryAsync<Pessoa>(sql, new
            {
                IdSalao = idSalao,
                Pesquisa = termo == null ? null : $"%{termo}%",
                PesquisaExata = termo,
                SomenteClientes = somenteClientes,
                SomenteFornecedores = somenteFornecedores,
                Limite = Math.Clamp(limite, 1, 50)
            })).ToList();
        }

        public bool ExisteCpfCnpjPorSalao(string cpfCnpj, int idSalao, int? ignorarIdPessoa = null)
        {
            if (string.IsNullOrWhiteSpace(cpfCnpj))
            {
                return false;
            }

            string query = @"
        SELECT COUNT(*)
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND CpfCnpj = @CpfCnpj
          AND (Excluido = 0 OR Excluido IS NULL)
          AND (@IgnorarIdPessoa IS NULL OR IdPessoa <> @IgnorarIdPessoa);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@CpfCnpj", cpfCnpj);
                command.AddWithValue("@IgnorarIdPessoa", (object?)ignorarIdPessoa ?? DBNull.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public bool ExisteEmailPorSalao(string email, int idSalao, int? ignorarIdPessoa = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            string query = @"
        SELECT COUNT(*)
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
          AND CONCAT(';', REPLACE(COALESCE(Email, ''), '; ', ';'), ';') LIKE CONCAT('%;', @Email, ';%')
          AND (Excluido = 0 OR Excluido IS NULL)
          AND (@IgnorarIdPessoa IS NULL OR IdPessoa <> @IgnorarIdPessoa);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@Email", email.Trim());
                command.AddWithValue("@IgnorarIdPessoa", (object?)ignorarIdPessoa ?? DBNull.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public virtual Pessoa? ObterPorIdESalao(int idPessoa, int idSalao)
        {
            var pessoa = ObterPorId(idPessoa);
            return pessoa != null && pessoa.IdSalao == idSalao ? pessoa : null;
        }

        public void Atualizar(Pessoa pessoa)
        {
            string query = @"
        UPDATE CorteCor_Pessoa
        SET Nome = @Nome,
            Telefone = @Telefone,
            Email = @Email,
            DataNascimento = @DataNascimento,
            IdSalao = @IdSalao,
            CpfCnpj = @CpfCnpj,
            InscricaoEstadual = @InscricaoEstadual,
            InscricaoMunicipal = @InscricaoMunicipal,
            Cep = @Cep,
            Logradouro = @Logradouro,
            Numero = @Numero,
            Complemento = @Complemento,
            Bairro = @Bairro,
            Cidade = @Cidade,
            UF = @UF,
            RazaoSocial = @RazaoSocial,
            NomeFantasia = @NomeFantasia,
            Cnae = @Cnae,
            IsCliente = @IsCliente,
            IsFornecedor = @IsFornecedor,
            IsLead = @IsLead,
            IsTransportador = @IsTransportador,
            NomeContato = @NomeContato,
            Pais = @Pais,
            IdEstrangeiro = @IdEstrangeiro,
            EntCep = @EntCep,
            EntUf = @EntUf,
            EntCidade = @EntCidade,
            EntNome = @EntNome,
            EntCpfCnpj = @EntCpfCnpj,
            EntInscricaoEstadual = @EntInscricaoEstadual,
            EntLogradouro = @EntLogradouro,
            EntNumero = @EntNumero,
            EntComplemento = @EntComplemento,
            EntBairro = @EntBairro,
            EntEmail = @EntEmail,
            EntTelefone = @EntTelefone,
            ConsumidorFinal = @ConsumidorFinal,
            IndicadorIE = @IndicadorIE,
            IESubstTrib = @IESubstTrib,
            Suframa = @Suframa,
            Tags = @Tags,
            DataComemorativa = @DataComemorativa,
            DescricaoComemoracao = @DescricaoComemoracao,
            BasesLegais = @BasesLegais,
            Observacoes = @Observacoes
        WHERE IdPessoa = @IdPessoa AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", pessoa.Nome ?? "");

                command.AddWithValue("@Telefone", string.IsNullOrWhiteSpace(pessoa.Telefone)
                    ? (object)DBNull.Value
                    : pessoa.Telefone);

                command.AddWithValue("@Email", string.IsNullOrWhiteSpace(pessoa.Email)
                    ? (object)DBNull.Value
                    : pessoa.Email);

                command.AddWithValue("@DataNascimento", pessoa.DataNascimento.HasValue
                    ? (object)pessoa.DataNascimento.Value.Date
                    : DBNull.Value);

                command.AddWithValue("@IdSalao", pessoa.IdSalao);

                command.AddWithValue("@CpfCnpj", string.IsNullOrWhiteSpace(pessoa.CpfCnpj) ? (object)DBNull.Value : pessoa.CpfCnpj);
                command.AddWithValue("@InscricaoEstadual", string.IsNullOrWhiteSpace(pessoa.InscricaoEstadual) ? (object)DBNull.Value : pessoa.InscricaoEstadual);
                command.AddWithValue("@InscricaoMunicipal", string.IsNullOrWhiteSpace(pessoa.InscricaoMunicipal) ? (object)DBNull.Value : pessoa.InscricaoMunicipal);
                command.AddWithValue("@Cep", ValorEnderecoOpcional(pessoa.Cep));
                command.AddWithValue("@Logradouro", ValorEnderecoOpcional(pessoa.Logradouro));
                command.AddWithValue("@Numero", ValorEnderecoOpcional(pessoa.Numero));
                command.AddWithValue("@Complemento", ValorEnderecoOpcional(pessoa.Complemento));
                command.AddWithValue("@Bairro", ValorEnderecoOpcional(pessoa.Bairro));
                command.AddWithValue("@Cidade", ValorEnderecoOpcional(pessoa.Cidade));
                command.AddWithValue("@UF", ValorEnderecoOpcional(pessoa.UF));
                command.AddWithValue("@RazaoSocial", string.IsNullOrWhiteSpace(pessoa.RazaoSocial) ? (object)DBNull.Value : pessoa.RazaoSocial);
                command.AddWithValue("@NomeFantasia", string.IsNullOrWhiteSpace(pessoa.NomeFantasia) ? (object)DBNull.Value : pessoa.NomeFantasia);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(pessoa.Cnae) ? (object)DBNull.Value : pessoa.Cnae);

                // Novos Campos
                command.AddWithValue("@IsCliente", pessoa.IsCliente);
                command.AddWithValue("@IsFornecedor", pessoa.IsFornecedor);
                command.AddWithValue("@IsLead", pessoa.IsLead);
                command.AddWithValue("@IsTransportador", pessoa.IsTransportador);
                command.AddWithValue("@NomeContato", string.IsNullOrWhiteSpace(pessoa.NomeContato) ? (object)DBNull.Value : pessoa.NomeContato);
                command.AddWithValue("@Pais", string.IsNullOrWhiteSpace(pessoa.Pais) ? (object)DBNull.Value : pessoa.Pais);
                command.AddWithValue("@IdEstrangeiro", string.IsNullOrWhiteSpace(pessoa.IdEstrangeiro) ? (object)DBNull.Value : pessoa.IdEstrangeiro);

                command.AddWithValue("@EntCep", ValorEnderecoOpcional(pessoa.EntCep));
                command.AddWithValue("@EntUf", ValorEnderecoOpcional(pessoa.EntUf));
                command.AddWithValue("@EntCidade", ValorEnderecoOpcional(pessoa.EntCidade));
                command.AddWithValue("@EntNome", string.IsNullOrWhiteSpace(pessoa.EntNome) ? (object)DBNull.Value : pessoa.EntNome);
                command.AddWithValue("@EntCpfCnpj", string.IsNullOrWhiteSpace(pessoa.EntCpfCnpj) ? (object)DBNull.Value : pessoa.EntCpfCnpj);
                command.AddWithValue("@EntInscricaoEstadual", string.IsNullOrWhiteSpace(pessoa.EntInscricaoEstadual) ? (object)DBNull.Value : pessoa.EntInscricaoEstadual);
                command.AddWithValue("@EntLogradouro", ValorEnderecoOpcional(pessoa.EntLogradouro));
                command.AddWithValue("@EntNumero", ValorEnderecoOpcional(pessoa.EntNumero));
                command.AddWithValue("@EntComplemento", ValorEnderecoOpcional(pessoa.EntComplemento));
                command.AddWithValue("@EntBairro", ValorEnderecoOpcional(pessoa.EntBairro));
                command.AddWithValue("@EntEmail", string.IsNullOrWhiteSpace(pessoa.EntEmail) ? (object)DBNull.Value : pessoa.EntEmail);
                command.AddWithValue("@EntTelefone", string.IsNullOrWhiteSpace(pessoa.EntTelefone) ? (object)DBNull.Value : pessoa.EntTelefone);

                command.AddWithValue("@ConsumidorFinal", pessoa.ConsumidorFinal.HasValue ? (object)pessoa.ConsumidorFinal.Value : DBNull.Value);
                command.AddWithValue("@IndicadorIE", pessoa.IndicadorIE.HasValue ? (object)pessoa.IndicadorIE.Value : DBNull.Value);
                command.AddWithValue("@IESubstTrib", string.IsNullOrWhiteSpace(pessoa.IESubstTrib) ? (object)DBNull.Value : pessoa.IESubstTrib);
                command.AddWithValue("@Suframa", string.IsNullOrWhiteSpace(pessoa.Suframa) ? (object)DBNull.Value : pessoa.Suframa);

                command.AddWithValue("@Tags", string.IsNullOrWhiteSpace(pessoa.Tags) ? (object)DBNull.Value : pessoa.Tags);
                command.AddWithValue("@DataComemorativa", pessoa.DataComemorativa.HasValue ? (object)pessoa.DataComemorativa.Value.Date : DBNull.Value);
                command.AddWithValue("@DescricaoComemoracao", string.IsNullOrWhiteSpace(pessoa.DescricaoComemoracao) ? (object)DBNull.Value : pessoa.DescricaoComemoracao);
                command.AddWithValue("@BasesLegais", string.IsNullOrWhiteSpace(pessoa.BasesLegais) ? (object)DBNull.Value : pessoa.BasesLegais);
                command.AddWithValue("@Observacoes", string.IsNullOrWhiteSpace(pessoa.Observacoes) ? (object)DBNull.Value : pessoa.Observacoes);

                command.AddWithValue("@IdPessoa", pessoa.IdPessoa);

                command.ExecuteNonQuery();
            }
        }

        public override void Excluir(int id)
        {
            string query = "UPDATE CorteCor_Pessoa SET Excluido = 1 WHERE IdPessoa = @IdPessoa";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdPessoa", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idPessoa, int idSalao)
        {
            string query = "UPDATE CorteCor_Pessoa SET Excluido = 1 WHERE IdPessoa = @IdPessoa AND IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdPessoa", idPessoa);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public List<Pessoa> ListarExcluidos(int idSalao)
        {
            string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF, RazaoSocial, NomeFantasia, Cnae,
               IsCliente, IsFornecedor, IsLead, IsTransportador, NomeContato, Pais, IdEstrangeiro,
               EntCep, EntUf, EntCidade, EntNome, EntCpfCnpj, EntInscricaoEstadual, EntLogradouro, EntNumero, EntComplemento, EntBairro, EntEmail, EntTelefone,
               ConsumidorFinal, IndicadorIE, IESubstTrib, Suframa, Tags, DataComemorativa, DescricaoComemoracao, BasesLegais, Observacoes
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao AND Excluido = 1
        ORDER BY Nome;";

            var pessoas = new List<Pessoa>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        pessoas.Add(new Pessoa
                        {
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Telefone = reader["Telefone"] is DBNull ? "" : reader["Telefone"].ToString(),
                            Email = reader["Email"] is DBNull ? "" : reader["Email"].ToString(),
                            DataNascimento = reader["DataNascimento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataNascimento"]),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"]),
                            CpfCnpj = reader["CpfCnpj"] is DBNull ? "" : reader["CpfCnpj"].ToString(),
                            InscricaoEstadual = reader["InscricaoEstadual"] is DBNull ? "" : reader["InscricaoEstadual"].ToString(),
                            InscricaoMunicipal = reader["InscricaoMunicipal"] is DBNull ? "" : reader["InscricaoMunicipal"].ToString(),
                            Cep = reader["Cep"] is DBNull ? "" : reader["Cep"].ToString(),
                            Logradouro = reader["Logradouro"] is DBNull ? "" : reader["Logradouro"].ToString(),
                            Numero = reader["Numero"] is DBNull ? "" : reader["Numero"].ToString(),
                            Complemento = reader["Complemento"] is DBNull ? "" : reader["Complemento"].ToString(),
                            Bairro = reader["Bairro"] is DBNull ? "" : reader["Bairro"].ToString(),
                            Cidade = reader["Cidade"] is DBNull ? "" : reader["Cidade"].ToString(),
                            UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString(),
                            RazaoSocial = reader["RazaoSocial"] is DBNull ? "" : reader["RazaoSocial"].ToString(),
                            NomeFantasia = reader["NomeFantasia"] is DBNull ? "" : reader["NomeFantasia"].ToString(),
                            Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                            IsCliente = reader["IsCliente"] is DBNull ? false : Convert.ToBoolean(reader["IsCliente"]),
                            IsFornecedor = reader["IsFornecedor"] is DBNull ? false : Convert.ToBoolean(reader["IsFornecedor"]),
                            IsLead = reader["IsLead"] is DBNull ? false : Convert.ToBoolean(reader["IsLead"]),
                            IsTransportador = reader["IsTransportador"] is DBNull ? false : Convert.ToBoolean(reader["IsTransportador"]),
                            NomeContato = reader["NomeContato"] is DBNull ? "" : reader["NomeContato"].ToString(),
                            Pais = reader["Pais"] is DBNull ? "" : reader["Pais"].ToString(),
                            IdEstrangeiro = reader["IdEstrangeiro"] is DBNull ? "" : reader["IdEstrangeiro"].ToString(),
                            EntCep = reader["EntCep"] is DBNull ? "" : reader["EntCep"].ToString(),
                            EntUf = reader["EntUf"] is DBNull ? "" : reader["EntUf"].ToString(),
                            EntCidade = reader["EntCidade"] is DBNull ? "" : reader["EntCidade"].ToString(),
                            EntNome = reader["EntNome"] is DBNull ? "" : reader["EntNome"].ToString(),
                            EntCpfCnpj = reader["EntCpfCnpj"] is DBNull ? "" : reader["EntCpfCnpj"].ToString(),
                            EntInscricaoEstadual = reader["EntInscricaoEstadual"] is DBNull ? "" : reader["EntInscricaoEstadual"].ToString(),
                            EntLogradouro = reader["EntLogradouro"] is DBNull ? "" : reader["EntLogradouro"].ToString(),
                            EntNumero = reader["EntNumero"] is DBNull ? "" : reader["EntNumero"].ToString(),
                            EntComplemento = reader["EntComplemento"] is DBNull ? "" : reader["EntComplemento"].ToString(),
                            EntBairro = reader["EntBairro"] is DBNull ? "" : reader["EntBairro"].ToString(),
                            EntEmail = reader["EntEmail"] is DBNull ? "" : reader["EntEmail"].ToString(),
                            EntTelefone = reader["EntTelefone"] is DBNull ? "" : reader["EntTelefone"].ToString(),
                            ConsumidorFinal = reader["ConsumidorFinal"] is DBNull ? (bool?)null : Convert.ToBoolean(reader["ConsumidorFinal"]),
                            IndicadorIE = reader["IndicadorIE"] is DBNull ? (int?)null : Convert.ToInt32(reader["IndicadorIE"]),
                            IESubstTrib = reader["IESubstTrib"] is DBNull ? "" : reader["IESubstTrib"].ToString(),
                            Suframa = reader["Suframa"] is DBNull ? "" : reader["Suframa"].ToString(),
                            Tags = reader["Tags"] is DBNull ? "" : reader["Tags"].ToString(),
                            DataComemorativa = reader["DataComemorativa"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataComemorativa"]),
                            DescricaoComemoracao = reader["DescricaoComemoracao"] is DBNull ? "" : reader["DescricaoComemoracao"].ToString(),
                            BasesLegais = reader["BasesLegais"] is DBNull ? "" : reader["BasesLegais"].ToString(),
                            Observacoes = reader["Observacoes"] is DBNull ? "" : reader["Observacoes"].ToString()
                        });
                    }
                }
            }

            return pessoas;
        }

        public void Restaurar(int id)
        {
            string query = "UPDATE CorteCor_Pessoa SET Excluido = 0 WHERE IdPessoa = @IdPessoa";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdPessoa", id);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotSupportedException("CorteCor_Pessoa não possui campo Status.");
        }

        public override void Cadastrar(Pessoa entity)
        {
            throw new NotImplementedException();
        }
    }

    public class AgendamentoHandler : EntityHandler<Agendamento>
    {
        public AgendamentoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public virtual int CadastrarAgendamento(Agendamento agendamento)
        {
            int novoId = 0;

            string query = @"
        INSERT INTO CorteCor_Agendamento
            (DataHora, Status, IdServico, IdPessoa, IdFuncionario)
        VALUES
            (@DataHora, @Status, @IdServico, @IdPessoa, @IdFuncionario);
        SELECT LAST_INSERT_ID();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@DataHora", agendamento.DataHora);
                command.AddWithValue("@Status", agendamento.Status ?? "Agendado");
                command.AddWithValue("@IdServico", agendamento.IdServico);
                command.AddWithValue("@IdPessoa", agendamento.IdPessoa);
                command.AddWithValue("@IdFuncionario", agendamento.IdFuncionario);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                    try
                    {
                        new LembreteHandler(_dbHandler).GerarLembretes(novoId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AgendamentoHandler] Erro ao gerar lembretes para Agendamento {novoId}: {ex.Message}");
                    }
                }
            }

            return novoId;
        }

        public virtual Agendamento ObterPorId(int idAgendamento)
        {
            string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario, Excluido
        FROM CorteCor_Agendamento
        WHERE IdAgendamento = @IdAgendamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdAgendamento", idAgendamento);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new Agendamento
                    {
                        IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                        DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                        Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                        IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                        IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                        Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                    };
                }
            }
        }

        public override List<Agendamento> Listar()
        {
            string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario, Excluido
        FROM CorteCor_Agendamento
        WHERE (Excluido = 0 OR Excluido IS NULL)
        ORDER BY DataHora;";

            var agendamentos = new List<Agendamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    agendamentos.Add(new Agendamento
                    {
                        IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                        DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                        Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                        IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                        IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                        Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                    });
                }
            }

            return agendamentos;
        }

        public List<Agendamento> ListarPorSalao(int idSalao)
        {
            string query = @"
        SELECT a.IdAgendamento, a.DataHora, a.Status, a.IdServico, a.IdPessoa, a.IdFuncionario, a.Excluido
        FROM CorteCor_Agendamento a
        INNER JOIN CorteCor_Servico s ON a.IdServico = s.IdServico
        WHERE s.IdSalao = @IdSalao AND (a.Excluido = 0 OR a.Excluido IS NULL)
        ORDER BY a.DataHora;";

            var agendamentos = new List<Agendamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        agendamentos.Add(new Agendamento
                        {
                            IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                            DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                        });
                    }
                }
            }

            return agendamentos;
        }

        public virtual List<Agendamento> ListarPorIntervalo(int idSalao, DateTime inicio, DateTime fim)
        {
            string query = @"
        SELECT a.IdAgendamento, a.DataHora, a.Status, a.IdServico, a.IdPessoa, a.IdFuncionario, a.Excluido
        FROM CorteCor_Agendamento a
        INNER JOIN CorteCor_Servico s ON s.IdServico = a.IdServico
        WHERE s.IdSalao = @IdSalao
          AND a.DataHora >= @Inicio
          AND a.DataHora < @Fim
          AND (a.Excluido = 0 OR a.Excluido IS NULL)
        ORDER BY a.DataHora;";

            var agendamentos = new List<Agendamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@Inicio", inicio);
                command.AddWithValue("@Fim", fim);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        agendamentos.Add(new Agendamento
                        {
                            IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                            DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                        });
                    }
                }
            }

            return agendamentos;
        }


        public List<Agendamento> ListarPorFuncionario(int idFuncionario, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario, Excluido
        FROM CorteCor_Agendamento
        WHERE IdFuncionario = @IdFuncionario
          AND (@DataInicio IS NULL OR DataHora >= @DataInicio)
          AND (@DataFim    IS NULL OR DataHora <  @DataFim)
          AND (Excluido = 0 OR Excluido IS NULL)
        ORDER BY DataHora;";

            var agendamentos = new List<Agendamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.AddWithValue("@DataInicio", (object?)dataInicio ?? DBNull.Value);
                command.AddWithValue("@DataFim", (object?)dataFim ?? DBNull.Value);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        agendamentos.Add(new Agendamento
                        {
                            IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                            DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                        });
                    }
                }
            }

            return agendamentos;
        }

        public virtual void Atualizar(Agendamento agendamento, int idSalao)
        {
            string query = @"
        UPDATE CorteCor_Agendamento
        SET DataHora = @DataHora,
            Status = @Status,
            IdServico = @IdServico,
            IdPessoa = @IdPessoa,
            IdFuncionario = @IdFuncionario
        WHERE IdAgendamento = @IdAgendamento
          AND IdServico IN (SELECT IdServico FROM CorteCor_Servico WHERE IdSalao = @IdSalao);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@DataHora", agendamento.DataHora);
                command.AddWithValue("@Status", agendamento.Status ?? "Agendado");
                command.AddWithValue("@IdServico", agendamento.IdServico);
                command.AddWithValue("@IdPessoa", agendamento.IdPessoa);
                command.AddWithValue("@IdFuncionario", agendamento.IdFuncionario);
                command.AddWithValue("@IdAgendamento", agendamento.IdAgendamento);
                command.AddWithValue("@IdSalao", idSalao);

                command.ExecuteNonQuery();

                // Regenerar lembretes (limpa os antigos e cria novos para a nova data)
                try
                {
                    new LembreteHandler(_dbHandler).GerarLembretes(agendamento.IdAgendamento);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AgendamentoHandler] Erro ao regenerar lembretes para Agendamento {agendamento.IdAgendamento}: {ex.Message}");
                }
            }
        }

        public virtual bool VerificarDisponibilidade(int idFuncionario, DateTime inicio, DateTime fim, int? idAgendamentoIgnorar = null)
        {
            string query = @"
        SELECT COUNT(1) 
        FROM CorteCor_Agendamento a
        INNER JOIN CorteCor_Servico s ON s.IdServico = a.IdServico
        WHERE a.IdFuncionario = @IdFuncionario
          AND a.Status <> 'Cancelado'
          AND (a.Excluido = 0 OR a.Excluido IS NULL)
          AND (@IdAgendamentoIgnorar IS NULL OR a.IdAgendamento <> @IdAgendamentoIgnorar)
          AND (
                (a.DataHora < @Fim) AND 
                (DATEADD(minute, DATEDIFF(minute, 0, s.Duracao), a.DataHora) > @Inicio)
              );";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdFuncionario", idFuncionario);
                command.AddWithValue("@Inicio", inicio);
                command.AddWithValue("@Fim", fim);
                command.AddWithValue("@IdAgendamentoIgnorar", (object?)idAgendamentoIgnorar ?? DBNull.Value);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count == 0;
            }
        }


        public override void AtivarDesativar(int id, bool ativar)
        {
            // Como "Status" é livre, padronizei:
            // ativar = "Agendado"
            // desativar = "Cancelado"
            string status = ativar ? "Agendado" : "Cancelado";

            string query = "UPDATE CorteCor_Agendamento SET Status = @Status WHERE IdAgendamento = @IdAgendamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@IdAgendamento", id);
                command.ExecuteNonQuery();

                if (!ativar && id > 0)
                {
                    try
                    {
                        new LembreteHandler(_dbHandler).ExcluirLembretesPendentes(id);
                    }
                    catch { }
                }
            }
        }

        public override void Excluir(int id)
        {
            string query = "UPDATE CorteCor_Agendamento SET Excluido = 1 WHERE IdAgendamento = @IdAgendamento";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdAgendamento", id);
                command.ExecuteNonQuery();

                try
                {
                    new LembreteHandler(_dbHandler).ExcluirLembretesPendentes(id);
                }
                catch { }
            }
        }

        public void ExcluirPorSalao(int idAgendamento, int idSalao)
        {
            string query = @"
                UPDATE CorteCor_Agendamento 
                SET Excluido = 1 
                WHERE IdAgendamento = @IdAgendamento
                  AND IdServico IN (SELECT IdServico FROM CorteCor_Servico WHERE IdSalao = @IdSalao)";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdAgendamento", idAgendamento);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();

                try
                {
                    new LembreteHandler(_dbHandler).ExcluirLembretesPendentes(idAgendamento);
                }
                catch { }
            }
        }

        public virtual void AtualizarStatus(int idAgendamento, string novoStatus, int idSalao)
        {
            string query = @"
        UPDATE CorteCor_Agendamento 
        SET Status = @Status 
        WHERE IdAgendamento = @IdAgendamento
          AND IdServico IN (SELECT IdServico FROM CorteCor_Servico WHERE IdSalao = @IdSalao);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", novoStatus);
                command.AddWithValue("@IdAgendamento", idAgendamento);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();

                if (novoStatus == "Cancelado")
                {
                    try
                    {
                        new LembreteHandler(_dbHandler).ExcluirLembretesPendentes(idAgendamento);
                    }
                    catch { }
                }
            }
        }

        public override void Cadastrar(Agendamento entity)
        {
            throw new NotImplementedException();
        }

        public virtual List<Agendamento> ListarTodos(int idSalao)
        {
            string query = @"
        SELECT a.IdAgendamento, a.DataHora, a.Status, a.IdServico, a.IdPessoa, a.IdFuncionario, a.Excluido
        FROM CorteCor_Agendamento a
        INNER JOIN CorteCor_Servico s ON s.IdServico = a.IdServico
        WHERE s.IdSalao = @IdSalao
        ORDER BY a.DataHora DESC;";

            var agendamentos = new List<Agendamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        agendamentos.Add(new Agendamento
                        {
                            IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                            DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                            Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                            IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                            Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                        });
                    }
                }
            }

            return agendamentos;
        }

        public virtual PagedResult<Agendamento> ListarFiltrado(int idSalao, DateTime? inicio, DateTime? fim, string status, int? idServico, int? idPessoa, int? idFuncionario, bool mostrarExcluidos, int page = 1, int pageSize = 10)
        {
            var result = new PagedResult<Agendamento>
            {
                PageIndex = page,
                PageSize = pageSize
            };

            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_Agendamento a ");
            sb.Append("INNER JOIN CorteCor_Servico s ON s.IdServico = a.IdServico ");
            sb.Append("WHERE s.IdSalao = @IdSalao ");

            if (inicio.HasValue) sb.Append("AND a.DataHora >= @Inicio ");
            if (fim.HasValue) sb.Append("AND a.DataHora <= @Fim ");
            if (!string.IsNullOrEmpty(status)) sb.Append("AND a.Status = @Status ");
            if (idServico.HasValue) sb.Append("AND a.IdServico = @IdServico ");
            if (idPessoa.HasValue) sb.Append("AND a.IdPessoa = @IdPessoa ");
            if (idFuncionario.HasValue) sb.Append("AND a.IdFuncionario = @IdFuncionario ");

            if (!mostrarExcluidos)
            {
                sb.Append("AND (a.Excluido = 0 OR a.Excluido IS NULL) ");
            }

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                using (var countCmd = connection.CreateCommand("SELECT COUNT(*) " + baseQuery))
                {
                    countCmd.AddWithValue("@IdSalao", idSalao);
                    if (inicio.HasValue) countCmd.AddWithValue("@Inicio", inicio.Value);
                    if (fim.HasValue) countCmd.AddWithValue("@Fim", fim.Value);
                    if (!string.IsNullOrEmpty(status)) countCmd.AddWithValue("@Status", status);
                    if (idServico.HasValue) countCmd.AddWithValue("@IdServico", idServico.Value);
                    if (idPessoa.HasValue) countCmd.AddWithValue("@IdPessoa", idPessoa.Value);
                    if (idFuncionario.HasValue) countCmd.AddWithValue("@IdFuncionario", idFuncionario.Value);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string dataQuery = "SELECT a.IdAgendamento, a.DataHora, a.Status, a.IdServico, a.IdPessoa, a.IdFuncionario, a.Excluido " +
                                   baseQuery +
                                   "ORDER BY a.DataHora DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = connection.CreateCommand(dataQuery))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.AddWithValue("@PageSize", pageSize);

                    if (inicio.HasValue) command.AddWithValue("@Inicio", inicio.Value);
                    if (fim.HasValue) command.AddWithValue("@Fim", fim.Value);
                    if (!string.IsNullOrEmpty(status)) command.AddWithValue("@Status", status);
                    if (idServico.HasValue) command.AddWithValue("@IdServico", idServico.Value);
                    if (idPessoa.HasValue) command.AddWithValue("@IdPessoa", idPessoa.Value);
                    if (idFuncionario.HasValue) command.AddWithValue("@IdFuncionario", idFuncionario.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new Agendamento
                            {
                                IdAgendamento = reader["IdAgendamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdAgendamento"]),
                                DataHora = reader["DataHora"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataHora"]),
                                Status = reader["Status"] is DBNull ? "" : reader["Status"].ToString(),
                                IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                                IdPessoa = reader["IdPessoa"] is DBNull ? 0 : Convert.ToInt32(reader["IdPessoa"]),
                                IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"]),
                                Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
                            });
                        }
                    }
                }
            }
            return result;
        }

    }


    public class MeioPagamentoHandler : EntityHandler<MeioPagamento>
    {
        public MeioPagamentoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
        public int CadastrarMeioPagamento(MeioPagamento meio)
        {
            int novoId = 0;

            if (meio.Ativo)
            {
                string updateAtivoQuery = "UPDATE CorteCor_MeioPagamento SET Ativo = 0 WHERE IdSalao = @IdSalao AND Ativo = 1;";
                using (var connection = _dbHandler.GetConnection())
                using (var command = connection.CreateCommand(updateAtivoQuery))
                {
                    command.AddWithValue("@IdSalao", meio.IdSalao);
                    command.ExecuteNonQuery();
                }
            }

            string query = @"
        INSERT INTO CorteCor_MeioPagamento
            (Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
             TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo,
             IdSalao, DataCadastro,
             MpAccessTokenProd, MpAccessTokenSandbox, MpPublicKeyProd, MpPublicKeySandbox,
             MpProduction)
        VALUES
            (@Nome, @Tipo, @Gateway, @PermiteParcelamento, @ParcelasMax,
             @TaxaPercentual, @TaxaFixa, @PrazoRecebimentoDias, @Ativo,
             @IdSalao, @DataCadastro,
             @MpAccessTokenProd, @MpAccessTokenSandbox, @MpPublicKeyProd, @MpPublicKeySandbox,
             @MpProduction);
        SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", meio.Nome ?? "");
                command.AddWithValue("@Tipo", meio.Tipo ?? "");
                command.AddWithValue("@Gateway", meio.Gateway ?? "");

                command.AddWithValue("@PermiteParcelamento", meio.PermiteParcelamento);
                command.AddWithValue("@ParcelasMax", (object?)meio.ParcelasMax ?? DBNull.Value);

                command.AddWithValue("@TaxaPercentual", meio.TaxaPercentual);
                command.AddWithValue("@TaxaFixa", meio.TaxaFixa);
                command.AddWithValue("@PrazoRecebimentoDias", meio.PrazoRecebimentoDias);

                command.AddWithValue("@Ativo", meio.Ativo);

                command.AddWithValue("@IdSalao", meio.IdSalao);
                command.AddWithValue("@DataCadastro", meio.DataCadastro == default ? DateTime.Now : meio.DataCadastro);

                command.AddWithValue("@MpAccessTokenProd", (object?)meio.MpAccessTokenProd ?? DBNull.Value);
                command.AddWithValue("@MpAccessTokenSandbox", (object?)meio.MpAccessTokenSandbox ?? DBNull.Value);
                command.AddWithValue("@MpPublicKeyProd", (object?)meio.MpPublicKeyProd ?? DBNull.Value);
                command.AddWithValue("@MpPublicKeySandbox", (object?)meio.MpPublicKeySandbox ?? DBNull.Value);
                command.AddWithValue("@MpProduction", meio.MpProduction);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    novoId = id;
            }

            return novoId;
        }

        public MeioPagamento ObterPorId(int idMeioPagamento)
        {
            string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro,
               MpAccessTokenProd, MpAccessTokenSandbox, MpPublicKeyProd, MpPublicKeySandbox,
               MpProduction
        FROM CorteCor_MeioPagamento
        WHERE IdMeioPagamento = @IdMeioPagamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdMeioPagamento", idMeioPagamento);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new MeioPagamento
                    {
                        IdMeioPagamento = reader["IdMeioPagamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdMeioPagamento"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                        Tipo = reader["Tipo"] is DBNull ? "" : reader["Tipo"].ToString(),
                        Gateway = reader["Gateway"] is DBNull ? "" : reader["Gateway"].ToString(),

                        PermiteParcelamento = reader["PermiteParcelamento"] is DBNull ? false : Convert.ToBoolean(reader["PermiteParcelamento"]),
                        ParcelasMax = reader["ParcelasMax"] is DBNull ? (byte?)null : Convert.ToByte(reader["ParcelasMax"]),

                        TaxaPercentual = reader["TaxaPercentual"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaPercentual"]),
                        TaxaFixa = reader["TaxaFixa"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaFixa"]),
                        PrazoRecebimentoDias = reader["PrazoRecebimentoDias"] is DBNull ? (short)0 : Convert.ToInt16(reader["PrazoRecebimentoDias"]),

                        Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),

                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                        MpAccessTokenProd = reader["MpAccessTokenProd"] as string,
                        MpAccessTokenSandbox = reader["MpAccessTokenSandbox"] as string,
                        MpPublicKeyProd = reader["MpPublicKeyProd"] as string,
                        MpPublicKeySandbox = reader["MpPublicKeySandbox"] as string,
                        MpProduction = reader["MpProduction"] is DBNull ? false : Convert.ToBoolean(reader["MpProduction"])
                    };
                }
            }
        }

        public override List<MeioPagamento> Listar()
        {
            string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro,
               MpAccessTokenProd, MpAccessTokenSandbox, MpPublicKeyProd, MpPublicKeySandbox,
               MpProduction
        FROM CorteCor_MeioPagamento
        ORDER BY Nome;";

            var itens = new List<MeioPagamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    itens.Add(new MeioPagamento
                    {
                        IdMeioPagamento = reader["IdMeioPagamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdMeioPagamento"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                        Tipo = reader["Tipo"] is DBNull ? "" : reader["Tipo"].ToString(),
                        Gateway = reader["Gateway"] is DBNull ? "" : reader["Gateway"].ToString(),

                        PermiteParcelamento = reader["PermiteParcelamento"] is DBNull ? false : Convert.ToBoolean(reader["PermiteParcelamento"]),
                        ParcelasMax = reader["ParcelasMax"] is DBNull ? (byte?)null : Convert.ToByte(reader["ParcelasMax"]),

                        TaxaPercentual = reader["TaxaPercentual"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaPercentual"]),
                        TaxaFixa = reader["TaxaFixa"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaFixa"]),
                        PrazoRecebimentoDias = reader["PrazoRecebimentoDias"] is DBNull ? (short)0 : Convert.ToInt16(reader["PrazoRecebimentoDias"]),

                        Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),

                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                        MpAccessTokenProd = reader["MpAccessTokenProd"] as string,
                        MpAccessTokenSandbox = reader["MpAccessTokenSandbox"] as string,
                        MpPublicKeyProd = reader["MpPublicKeyProd"] as string,
                        MpPublicKeySandbox = reader["MpPublicKeySandbox"] as string,
                        MpProduction = reader["MpProduction"] is DBNull ? false : Convert.ToBoolean(reader["MpProduction"])
                    });
                }
            }

            return itens;
        }

        public List<MeioPagamento> ListarPorSalao(int idSalao)
        {
            string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro,
               MpAccessTokenProd, MpAccessTokenSandbox, MpPublicKeyProd, MpPublicKeySandbox,
               MpProduction
        FROM CorteCor_MeioPagamento
        WHERE IdSalao = @IdSalao
        ORDER BY Nome;";

            var itens = new List<MeioPagamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        itens.Add(new MeioPagamento
                        {
                            IdMeioPagamento = reader["IdMeioPagamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdMeioPagamento"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Tipo = reader["Tipo"] is DBNull ? "" : reader["Tipo"].ToString(),
                            Gateway = reader["Gateway"] is DBNull ? "" : reader["Gateway"].ToString(),

                            PermiteParcelamento = reader["PermiteParcelamento"] is DBNull ? false : Convert.ToBoolean(reader["PermiteParcelamento"]),
                            ParcelasMax = reader["ParcelasMax"] is DBNull ? (byte?)null : Convert.ToByte(reader["ParcelasMax"]),

                            TaxaPercentual = reader["TaxaPercentual"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaPercentual"]),
                            TaxaFixa = reader["TaxaFixa"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaFixa"]),
                            PrazoRecebimentoDias = reader["PrazoRecebimentoDias"] is DBNull ? (short)0 : Convert.ToInt16(reader["PrazoRecebimentoDias"]),

                            Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),

                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                            MpAccessTokenProd = reader["MpAccessTokenProd"] as string,
                            MpAccessTokenSandbox = reader["MpAccessTokenSandbox"] as string,
                            MpPublicKeyProd = reader["MpPublicKeyProd"] as string,
                            MpPublicKeySandbox = reader["MpPublicKeySandbox"] as string,
                            MpProduction = reader["MpProduction"] is DBNull ? false : Convert.ToBoolean(reader["MpProduction"])
                        });
                    }
                }
            }
            return itens;
        }

        public virtual List<MeioPagamento> ListarPorSalao(int idSalao, bool? somenteAtivos = true)
        {
            string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro,
               MpAccessTokenProd, MpAccessTokenSandbox, MpPublicKeyProd, MpPublicKeySandbox,
               MpProduction
        FROM CorteCor_MeioPagamento
        WHERE IdSalao = @IdSalao
          AND (@SomenteAtivos IS NULL OR Ativo = @SomenteAtivos)
        ORDER BY Nome;";

            var itens = new List<MeioPagamento>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@SomenteAtivos", (object?)somenteAtivos ?? DBNull.Value);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        itens.Add(new MeioPagamento
                        {
                            IdMeioPagamento = reader["IdMeioPagamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdMeioPagamento"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Tipo = reader["Tipo"] is DBNull ? "" : reader["Tipo"].ToString(),
                            Gateway = reader["Gateway"] is DBNull ? "" : reader["Gateway"].ToString(),

                            PermiteParcelamento = reader["PermiteParcelamento"] is DBNull ? false : Convert.ToBoolean(reader["PermiteParcelamento"]),
                            ParcelasMax = reader["ParcelasMax"] is DBNull ? (byte?)null : Convert.ToByte(reader["ParcelasMax"]),

                            TaxaPercentual = reader["TaxaPercentual"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaPercentual"]),
                            TaxaFixa = reader["TaxaFixa"] is DBNull ? 0m : Convert.ToDecimal(reader["TaxaFixa"]),
                            PrazoRecebimentoDias = reader["PrazoRecebimentoDias"] is DBNull ? (short)0 : Convert.ToInt16(reader["PrazoRecebimentoDias"]),

                            Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),

                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                            MpAccessTokenProd = reader["MpAccessTokenProd"] as string,
                            MpAccessTokenSandbox = reader["MpAccessTokenSandbox"] as string,
                            MpPublicKeyProd = reader["MpPublicKeyProd"] as string,
                            MpPublicKeySandbox = reader["MpPublicKeySandbox"] as string,
                            MpProduction = reader["MpProduction"] is DBNull ? false : Convert.ToBoolean(reader["MpProduction"])
                        });
                    }
                }
            }

            return itens;
        }

        public void Atualizar(MeioPagamento meio)
        {
            if (meio.Ativo)
            {
                string updateAtivoQuery = "UPDATE CorteCor_MeioPagamento SET Ativo = 0 WHERE IdSalao = @IdSalao AND IdMeioPagamento <> @IdMeioPagamento AND Ativo = 1;";
                using (var connection = _dbHandler.GetConnection())
                using (var command = connection.CreateCommand(updateAtivoQuery))
                {
                    command.AddWithValue("@IdSalao", meio.IdSalao);
                    command.AddWithValue("@IdMeioPagamento", meio.IdMeioPagamento);
                    command.ExecuteNonQuery();
                }
            }

            string query = @"
        UPDATE CorteCor_MeioPagamento
        SET Nome = @Nome,
            Tipo = @Tipo,
            Gateway = @Gateway,
            PermiteParcelamento = @PermiteParcelamento,
            ParcelasMax = @ParcelasMax,
            TaxaPercentual = @TaxaPercentual,
            TaxaFixa = @TaxaFixa,
            PrazoRecebimentoDias = @PrazoRecebimentoDias,
            Ativo = @Ativo,
            IdSalao = @IdSalao,
            DataCadastro = @DataCadastro,
            MpAccessTokenProd = @MpAccessTokenProd,
            MpAccessTokenSandbox = @MpAccessTokenSandbox,
            MpPublicKeyProd = @MpPublicKeyProd,
            MpPublicKeySandbox = @MpPublicKeySandbox,
            MpProduction = @MpProduction
        WHERE IdMeioPagamento = @IdMeioPagamento AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", meio.Nome ?? "");
                command.AddWithValue("@Tipo", meio.Tipo ?? "");
                command.AddWithValue("@Gateway", meio.Gateway ?? "");

                command.AddWithValue("@PermiteParcelamento", meio.PermiteParcelamento);
                command.AddWithValue("@ParcelasMax", (object?)meio.ParcelasMax ?? DBNull.Value);

                command.AddWithValue("@TaxaPercentual", meio.TaxaPercentual);
                command.AddWithValue("@TaxaFixa", meio.TaxaFixa);
                command.AddWithValue("@PrazoRecebimentoDias", meio.PrazoRecebimentoDias);

                command.AddWithValue("@MpProduction", meio.MpProduction);

                command.AddWithValue("@Ativo", meio.Ativo);

                command.AddWithValue("@IdSalao", meio.IdSalao);
                command.AddWithValue("@DataCadastro", meio.DataCadastro == default ? DateTime.Now : meio.DataCadastro);

                command.AddWithValue("@MpAccessTokenProd", (object?)meio.MpAccessTokenProd ?? DBNull.Value);
                command.AddWithValue("@MpAccessTokenSandbox", (object?)meio.MpAccessTokenSandbox ?? DBNull.Value);
                command.AddWithValue("@MpPublicKeyProd", (object?)meio.MpPublicKeyProd ?? DBNull.Value);
                command.AddWithValue("@MpPublicKeySandbox", (object?)meio.MpPublicKeySandbox ?? DBNull.Value);

                command.AddWithValue("@IdMeioPagamento", meio.IdMeioPagamento);

                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            using (var connection = _dbHandler.GetConnection())
            {
                if (ativar)
                {
                    string updateAtivoQuery = @"
                        UPDATE CorteCor_MeioPagamento 
                        SET Ativo = 0 
                        WHERE IdSalao = (SELECT IdSalao FROM CorteCor_MeioPagamento WHERE IdMeioPagamento = @IdMeioPagamento)
                        AND IdMeioPagamento <> @IdMeioPagamento AND Ativo = 1;";
                    using (var cmd = connection.CreateCommand(updateAtivoQuery))
                    {
                        cmd.AddWithValue("@IdMeioPagamento", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                string query = "UPDATE CorteCor_MeioPagamento SET Ativo = @Ativo WHERE IdMeioPagamento = @IdMeioPagamento;";
                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@Ativo", ativar);
                    command.AddWithValue("@IdMeioPagamento", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public override void Excluir(int id)
        {
            string query = "DELETE FROM CorteCor_MeioPagamento WHERE IdMeioPagamento = @IdMeioPagamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdMeioPagamento", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idMeioPagamento, int idSalao)
        {
            string query = "DELETE FROM CorteCor_MeioPagamento WHERE IdMeioPagamento = @IdMeioPagamento AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdMeioPagamento", idMeioPagamento);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void Cadastrar(MeioPagamento entity)
        {
            throw new NotImplementedException();
        }
    }

    public class PagamentoHandler : EntityHandler<Pagamento>
    {
        public PagamentoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler)
        {
        }

        public virtual void CadastrarPagamento(Pagamento pagamento)
        {
            NormalizarEValidarPagamento(pagamento);

            // Desativa pagamentos anteriores somente quando existe agendamento vinculado.
            string deactivateQuery = "UPDATE CorteCor_Pagamento SET Ativo = 0 WHERE IdAgendamento = @IdAgendamento AND Ativo = 1;";

            string insertQuery = @"
        INSERT INTO CorteCor_Pagamento
            (IdPagamento, IdSalao, IdAgendamento, IdPedido, IdVendaProduto, OrigemPagamento,
             IdMeioPagamento, Ativo, Status, Data, Valor, Moeda, Descricao,
             Contos, Campos, MercadoPagoPreferenceId, MercadoPagoPaymentId, CheckoutUrl, MpStatus,
             MpStatusDetail, Tipo, CriadoEm, PagoEm, AtualizadoEm)
        VALUES
            (@IdPagamento, @IdSalao, @IdAgendamento, @IdPedido, @IdVendaProduto, @OrigemPagamento,
             @IdMeioPagamento, @Ativo, @Status, @Data, @Valor, @Moeda, @Descricao,
             @Contos, @Campos, @MercadoPagoPreferenceId, @MercadoPagoPaymentId, @CheckoutUrl, @MpStatus,
             @MpStatusDetail, @Tipo, @CriadoEm, @PagoEm, @AtualizadoEm);";

            using (var connection = _dbHandler.GetConnection())
            {
                if (pagamento.IdPagamento == Guid.Empty)
                {
                    pagamento.IdPagamento = Guid.NewGuid();
                }

                if (pagamento.IdAgendamento.HasValue)
                {
                    using (var commandDeactivate = connection.CreateCommand())
                    {
                        commandDeactivate.CommandText = deactivateQuery;
                        commandDeactivate.AddWithValue("@IdAgendamento", pagamento.IdAgendamento.Value);
                        commandDeactivate.ExecuteNonQuery();
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertQuery;
                    command.AddWithValue("@IdPagamento", pagamento.IdPagamento);
                    command.AddWithValue("@IdSalao", pagamento.IdSalao.HasValue && pagamento.IdSalao.Value > 0 ? pagamento.IdSalao.Value : (object)DBNull.Value);
                    command.AddWithValue("@IdAgendamento", pagamento.IdAgendamento.HasValue ? pagamento.IdAgendamento.Value : (object)DBNull.Value);
                    command.AddWithValue("@IdPedido", pagamento.IdPedido.HasValue ? pagamento.IdPedido.Value : (object)DBNull.Value);
                    command.AddWithValue("@IdVendaProduto", pagamento.IdVendaProduto.HasValue ? pagamento.IdVendaProduto.Value : (object)DBNull.Value);
                    command.AddWithValue("@OrigemPagamento", pagamento.OrigemPagamento);
                    command.AddWithValue("@IdMeioPagamento", pagamento.IdMeioPagamento > 0 ? pagamento.IdMeioPagamento : (object)DBNull.Value);
                    command.AddWithValue("@Ativo", pagamento.Ativo);
                    command.AddWithValue("@Status", pagamento.Status ?? "Pendente");
                    command.AddWithValue("@Data", pagamento.Data == default ? DateTime.Now : pagamento.Data);
                    command.AddWithValue("@Valor", pagamento.Valor);
                    command.AddWithValue("@Moeda", pagamento.Moeda ?? "BRL");
                    command.AddWithValue("@Descricao", (object?)(pagamento.Descricao ?? pagamento.Contos) ?? DBNull.Value);
                    command.AddWithValue("@Contos", (object?)pagamento.Contos ?? DBNull.Value);
                    command.AddWithValue("@Campos", (object?)pagamento.Campos ?? DBNull.Value);
                    command.AddWithValue("@MercadoPagoPreferenceId", (object?)pagamento.MercadoPagoPreferenceId ?? DBNull.Value);
                    command.AddWithValue("@MercadoPagoPaymentId", (object?)pagamento.MercadoPagoPaymentId ?? DBNull.Value);
                    command.AddWithValue("@CheckoutUrl", (object?)pagamento.CheckoutUrl ?? DBNull.Value);
                    command.AddWithValue("@MpStatus", (object?)pagamento.MpStatus ?? DBNull.Value);
                    command.AddWithValue("@MpStatusDetail", (object?)pagamento.MpStatusDetail ?? DBNull.Value);
                    command.AddWithValue("@Tipo", (object?)pagamento.Tipo ?? DBNull.Value);
                    command.AddWithValue("@CriadoEm", pagamento.CriadoEm == default ? DateTime.UtcNow : pagamento.CriadoEm);
                    command.AddWithValue("@PagoEm", (object?)pagamento.PagoEm ?? DBNull.Value);
                    command.AddWithValue("@AtualizadoEm", (object?)pagamento.AtualizadoEm ?? DBNull.Value);

                    command.ExecuteNonQuery();
                }
            }
        }

        public Pagamento ObterPorIdAgendamento(int idAgendamento)
        {
            string query = "SELECT * FROM CorteCor_Pagamento WHERE IdAgendamento = @IdAgendamento AND Ativo = 1;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@IdAgendamento", idAgendamento);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return Map(reader);
                }
            }
        }

        public Pagamento ObterPorPreferenceId(string preferenceId)
        {
            string query = "SELECT * FROM CorteCor_Pagamento WHERE MercadoPagoPreferenceId = @PreferenceId AND Ativo = 1;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@PreferenceId", preferenceId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return Map(reader);
                }
            }
        }

        public Pagamento ObterPorPaymentId(string paymentId)
        {
            string query = "SELECT * FROM CorteCor_Pagamento WHERE MercadoPagoPaymentId = @PaymentId;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@PaymentId", paymentId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return Map(reader);
                }
            }
        }

        public void AtualizarPagamento(Pagamento p, int idSalao)
        {
            p.IdSalao = idSalao > 0 ? idSalao : p.IdSalao;
            NormalizarEValidarPagamento(p);

            string query = @"
        UPDATE CorteCor_Pagamento P
        LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento
        LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
        LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido
        LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto
        SET P.IdSalao = @IdSalaoPagamento,
            P.IdAgendamento = @IdAgendamento,
            P.IdPedido = @IdPedido,
            P.IdVendaProduto = @IdVendaProduto,
            P.OrigemPagamento = @OrigemPagamento,
            P.IdMeioPagamento = @IdMeioPagamento,
            P.Status = @Status,
            P.Tipo = @Tipo,
            P.Valor = @Valor,
            P.Data = @Data,
            P.Contos = @Contos,
            P.Campos = @Campos,
            P.Descricao = @Descricao,
            P.MercadoPagoPaymentId = @MercadoPagoPaymentId,
            P.MpStatus = @MpStatus,
            P.MpStatusDetail = @MpStatusDetail,
            P.AtualizadoEm = NOW(),
            P.PagoEm = @PagoEm,
            P.Ativo = @Ativo
        WHERE P.IdPagamento = @IdPagamento
          AND (@IdSalao <= 0 OR P.IdSalao = @IdSalao OR S.IdSalao = @IdSalao OR Ped.IdSalao = @IdSalao OR V.IdSalao = @IdSalao);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalaoPagamento", p.IdSalao.HasValue && p.IdSalao.Value > 0 ? p.IdSalao.Value : (object)DBNull.Value);
                command.AddWithValue("@IdAgendamento", p.IdAgendamento.HasValue ? p.IdAgendamento.Value : (object)DBNull.Value);
                command.AddWithValue("@IdPedido", p.IdPedido.HasValue ? p.IdPedido.Value : (object)DBNull.Value);
                command.AddWithValue("@IdVendaProduto", p.IdVendaProduto.HasValue ? p.IdVendaProduto.Value : (object)DBNull.Value);
                command.AddWithValue("@OrigemPagamento", p.OrigemPagamento);
                command.AddWithValue("@IdMeioPagamento", p.IdMeioPagamento > 0 ? p.IdMeioPagamento : (object)DBNull.Value);
                command.AddWithValue("@Status", p.Status);
                command.AddWithValue("@Tipo", (object?)p.Tipo ?? DBNull.Value);
                command.AddWithValue("@Valor", p.Valor);
                command.AddWithValue("@Data", p.Data == default ? DateTime.Now : p.Data);
                command.AddWithValue("@Contos", (object?)p.Contos ?? DBNull.Value);
                command.AddWithValue("@Campos", (object?)p.Campos ?? DBNull.Value);
                command.AddWithValue("@Descricao", (object?)(p.Descricao ?? p.Contos) ?? DBNull.Value);
                command.AddWithValue("@MercadoPagoPaymentId", (object?)p.MercadoPagoPaymentId ?? DBNull.Value);
                command.AddWithValue("@MpStatus", (object?)p.MpStatus ?? DBNull.Value);
                command.AddWithValue("@MpStatusDetail", (object?)p.MpStatusDetail ?? DBNull.Value);
                command.AddWithValue("@PagoEm", (object?)p.PagoEm ?? DBNull.Value);
                command.AddWithValue("@Ativo", p.Ativo);
                command.AddWithValue("@IdPagamento", p.IdPagamento);
                command.AddWithValue("@IdSalao", idSalao);

                command.ExecuteNonQuery();
            }
        }

        private Pagamento Map(IDataReader reader)
        {
            return new Pagamento
            {
                IdPagamento = ReadGuid(reader["IdPagamento"]),
                IdSalao = HasColumn(reader, "IdSalao") ? ReadNullableInt(reader["IdSalao"]) : null,
                IdAgendamento = ReadNullableInt(reader["IdAgendamento"]),
                IdPedido = HasColumn(reader, "IdPedido") ? ReadNullableInt(reader["IdPedido"]) : null,
                IdVendaProduto = HasColumn(reader, "IdVendaProduto") ? ReadNullableInt(reader["IdVendaProduto"]) : null,
                OrigemPagamento = HasColumn(reader, "OrigemPagamento") && reader["OrigemPagamento"] is not DBNull
                    ? reader["OrigemPagamento"]?.ToString() ?? OrigemPagamento.Avulso
                    : (ReadNullableInt(reader["IdAgendamento"]).HasValue ? OrigemPagamento.Agendamento : OrigemPagamento.Avulso),
                Ativo = Convert.ToBoolean(reader["Ativo"]),
                Status = reader["Status"].ToString(),
                Valor = Convert.ToDecimal(reader["Valor"]),
                Moeda = reader["Moeda"].ToString(),
                Descricao = reader["Descricao"]?.ToString(),
                MercadoPagoPreferenceId = reader["MercadoPagoPreferenceId"]?.ToString(),
                MercadoPagoPaymentId = reader["MercadoPagoPaymentId"]?.ToString(),
                CheckoutUrl = reader["CheckoutUrl"]?.ToString(),
                MpStatus = reader["MpStatus"]?.ToString(),
                MpStatusDetail = reader["MpStatusDetail"]?.ToString(),
                CriadoEm = Convert.ToDateTime(reader["CriadoEm"]),
                AtualizadoEm = reader["AtualizadoEm"] is DBNull ? null : Convert.ToDateTime(reader["AtualizadoEm"]),
                PagoEm = reader["PagoEm"] is DBNull ? null : Convert.ToDateTime(reader["PagoEm"]),

                // Legacy mapping safely - Explicit check to avoid any evaluation risk
                IdMeioPagamento = HasColumn(reader, "IdMeioPagamento") ? (reader["IdMeioPagamento"] is DBNull ? 0 : Convert.ToInt32(reader["IdMeioPagamento"])) : 0,
                Tipo = HasColumn(reader, "Tipo") ? reader["Tipo"]?.ToString() : null,
                Data = HasColumn(reader, "Data") ? (reader["Data"] is DBNull ? (HasColumn(reader, "CriadoEm") ? Convert.ToDateTime(reader["CriadoEm"]) : DateTime.MinValue) : Convert.ToDateTime(reader["Data"])) : (HasColumn(reader, "CriadoEm") ? Convert.ToDateTime(reader["CriadoEm"]) : DateTime.MinValue),
                Contos = HasColumn(reader, "Contos") ? reader["Contos"]?.ToString() : null,
                Campos = HasColumn(reader, "Campos") ? reader["Campos"]?.ToString() : null,
                NomeCliente = HasColumn(reader, "NomeCliente") ? reader["NomeCliente"]?.ToString() : null,
                NomeServico = HasColumn(reader, "NomeServico") ? reader["NomeServico"]?.ToString() : null,
                DataAgendamento = HasColumn(reader, "DataAgendamento") ? (reader["DataAgendamento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataAgendamento"])) : null
            };
        }

        private static Guid ReadGuid(object value)
        {
            return value is Guid guid ? guid : Guid.Parse(value.ToString() ?? Guid.Empty.ToString());
        }

        private static int? ReadNullableInt(object value)
        {
            return value is DBNull || value is null ? null : Convert.ToInt32(value);
        }

        private static void NormalizarEValidarPagamento(Pagamento pagamento)
        {
            if (pagamento == null)
            {
                throw new InvalidOperationException("Informe os dados do pagamento.");
            }

            pagamento.IdAgendamento = pagamento.IdAgendamento.HasValue && pagamento.IdAgendamento.Value > 0
                ? pagamento.IdAgendamento.Value
                : null;
            pagamento.IdPedido = pagamento.IdPedido.HasValue && pagamento.IdPedido.Value > 0
                ? pagamento.IdPedido.Value
                : null;
            pagamento.IdVendaProduto = pagamento.IdVendaProduto.HasValue && pagamento.IdVendaProduto.Value > 0
                ? pagamento.IdVendaProduto.Value
                : null;
            pagamento.IdSalao = pagamento.IdSalao.HasValue && pagamento.IdSalao.Value > 0
                ? pagamento.IdSalao.Value
                : null;

            pagamento.OrigemPagamento = NormalizarOrigemPagamento(pagamento);

            if (pagamento.Valor <= 0)
            {
                throw new InvalidOperationException("Informe um valor de pagamento maior que zero.");
            }

            if (pagamento.OrigemPagamento == OrigemPagamento.Agendamento && !pagamento.IdAgendamento.HasValue)
            {
                throw new InvalidOperationException("Pagamentos de agendamento exigem um agendamento válido.");
            }

            if (pagamento.OrigemPagamento == OrigemPagamento.Pedido && !pagamento.IdPedido.HasValue)
            {
                throw new InvalidOperationException("Pagamentos de pedido exigem um pedido válido.");
            }

            if (pagamento.OrigemPagamento == OrigemPagamento.Venda && !pagamento.IdVendaProduto.HasValue)
            {
                throw new InvalidOperationException("Pagamentos de venda exigem uma venda válida.");
            }

            if ((pagamento.OrigemPagamento == OrigemPagamento.Avulso
                    || pagamento.OrigemPagamento == OrigemPagamento.Pedido
                    || pagamento.OrigemPagamento == OrigemPagamento.Venda)
                && !pagamento.IdSalao.HasValue)
            {
                throw new InvalidOperationException("Não foi possível identificar a empresa do pagamento.");
            }
        }

        private static string NormalizarOrigemPagamento(Pagamento pagamento)
        {
            var origem = pagamento.OrigemPagamento?.Trim();
            if (string.IsNullOrWhiteSpace(origem)
                || (origem.Equals(OrigemPagamento.Avulso, StringComparison.OrdinalIgnoreCase)
                    && (pagamento.IdAgendamento.HasValue || pagamento.IdPedido.HasValue || pagamento.IdVendaProduto.HasValue)))
            {
                if (pagamento.IdAgendamento.HasValue) return OrigemPagamento.Agendamento;
                if (pagamento.IdPedido.HasValue) return OrigemPagamento.Pedido;
                if (pagamento.IdVendaProduto.HasValue) return OrigemPagamento.Venda;
                return OrigemPagamento.Avulso;
            }

            if (origem.Equals(OrigemPagamento.Agendamento, StringComparison.OrdinalIgnoreCase)) return OrigemPagamento.Agendamento;
            if (origem.Equals(OrigemPagamento.Pedido, StringComparison.OrdinalIgnoreCase)) return OrigemPagamento.Pedido;
            if (origem.Equals(OrigemPagamento.Venda, StringComparison.OrdinalIgnoreCase)) return OrigemPagamento.Venda;
            if (origem.Equals(OrigemPagamento.Avulso, StringComparison.OrdinalIgnoreCase)) return OrigemPagamento.Avulso;

            throw new InvalidOperationException("Origem de pagamento inválida.");
        }

        private bool HasColumn(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void Atualizar(Pagamento p, int idSalao)
        {
            p.IdSalao = idSalao > 0 ? idSalao : p.IdSalao;
            NormalizarEValidarPagamento(p);

            string query = @"
        UPDATE CorteCor_Pagamento P
        LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento
        LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
        LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido
        LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto
        SET P.IdSalao = @IdSalaoPagamento,
            P.IdAgendamento = @IdAgendamento,
            P.IdPedido = @IdPedido,
            P.IdVendaProduto = @IdVendaProduto,
            P.OrigemPagamento = @OrigemPagamento,
            P.IdMeioPagamento = @IdMeioPagamento,
            P.Tipo = @Tipo,
            P.Valor = @Valor,
            P.Data = @Data,
            P.Contos = @Contos,
            P.Campos = @Campos,
            P.AtualizadoEm = UTC_TIMESTAMP()
        WHERE P.IdPagamento = @IdPagamento 
          AND (@IdSalao <= 0 OR P.IdSalao = @IdSalao OR S.IdSalao = @IdSalao OR Ped.IdSalao = @IdSalao OR V.IdSalao = @IdSalao);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@IdSalaoPagamento", p.IdSalao.HasValue && p.IdSalao.Value > 0 ? p.IdSalao.Value : (object)DBNull.Value);
                command.AddWithValue("@IdAgendamento", p.IdAgendamento.HasValue ? p.IdAgendamento.Value : (object)DBNull.Value);
                command.AddWithValue("@IdPedido", p.IdPedido.HasValue ? p.IdPedido.Value : (object)DBNull.Value);
                command.AddWithValue("@IdVendaProduto", p.IdVendaProduto.HasValue ? p.IdVendaProduto.Value : (object)DBNull.Value);
                command.AddWithValue("@OrigemPagamento", p.OrigemPagamento);
                command.AddWithValue("@IdMeioPagamento", p.IdMeioPagamento == 0 ? (object)DBNull.Value : p.IdMeioPagamento);
                command.AddWithValue("@Tipo", p.Tipo ?? "");
                command.AddWithValue("@Valor", p.Valor);
                command.AddWithValue("@Data", p.Data == default ? DateTime.Now : p.Data);
                command.AddWithValue("@Contos", p.Contos ?? "");
                command.AddWithValue("@Campos", p.Campos ?? "");
                command.AddWithValue("@IdPagamento", p.IdPagamento);
                command.AddWithValue("@IdSalao", idSalao);

                command.ExecuteNonQuery();
            }
        }

        public override List<Pagamento> Listar()
        {
            return Listar(new PagamentoFiltroDTO { PageSize = int.MaxValue }).Items;
        }

        public PagedResult<Pagamento> Listar(PagamentoFiltroDTO filtro)
        {
            var result = new PagedResult<Pagamento>
            {
                PageIndex = filtro.PageIndex,
                PageSize = filtro.PageSize
            };

            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_Pagamento P ");
            sb.Append("LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento ");
            sb.Append("LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico ");
            sb.Append("LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PePed ON Ped.IdPessoa = PePed.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PeVenda ON V.IdPessoa = PeVenda.IdPessoa ");
            sb.Append("WHERE (P.Ativo = 1 OR P.Ativo IS NULL) ");

            if (filtro.DataInicio.HasValue) sb.Append("AND P.CriadoEm >= @DataInicio ");
            if (filtro.DataFim.HasValue) sb.Append("AND P.CriadoEm <= @DataFim ");
            if (!string.IsNullOrEmpty(filtro.Status)) sb.Append("AND P.Status = @Status ");
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) sb.Append("AND COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome, '') LIKE @NomeCliente ");
            if (filtro.DataAgendamento.HasValue) sb.Append("AND CAST(A.DataHora AS DATE) = CAST(@DataAgendamento AS DATE) ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                using (var countCmd = connection.CreateCommand("SELECT COUNT(*) " + baseQuery))
                {
                    AddFiltroParams(countCmd, filtro);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string dataQuery = "SELECT P.*, COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome) as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento " +
                                   baseQuery +
                                   "ORDER BY P.CriadoEm DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = connection.CreateCommand(dataQuery))
                {
                    AddFiltroParams(cmd, filtro);
                    cmd.AddWithValue("@Offset", (filtro.PageIndex - 1) * filtro.PageSize);
                    cmd.AddWithValue("@PageSize", filtro.PageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var p = Map(reader);
                            p.NomeCliente = reader["NomeCliente"] is DBNull ? "" : reader["NomeCliente"].ToString();
                            p.NomeServico = reader["NomeServico"] is DBNull ? "" : reader["NomeServico"].ToString();
                            p.DataAgendamento = reader["DataAgendamento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataAgendamento"]);
                            result.Items.Add(p);
                        }
                    }
                }
            }
            return result;
        }

        public PagedResult<Pagamento> ListarPorSalao(int idSalao, PagamentoFiltroDTO filtro)
        {
            var result = new PagedResult<Pagamento>
            {
                PageIndex = filtro.PageIndex,
                PageSize = filtro.PageSize
            };

            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_Pagamento P ");
            sb.Append("LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento ");
            sb.Append("LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico ");
            sb.Append("LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PePed ON Ped.IdPessoa = PePed.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PeVenda ON V.IdPessoa = PeVenda.IdPessoa ");
            sb.Append("WHERE (P.Ativo = 1 OR P.Ativo IS NULL) AND (P.IdSalao = @IdSalao OR S.IdSalao = @IdSalao OR Ped.IdSalao = @IdSalao OR V.IdSalao = @IdSalao) ");

            if (filtro.DataInicio.HasValue) sb.Append("AND P.CriadoEm >= @DataInicio ");
            if (filtro.DataFim.HasValue) sb.Append("AND P.CriadoEm <= @DataFim ");
            if (!string.IsNullOrEmpty(filtro.Status)) sb.Append("AND P.Status = @Status ");
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) sb.Append("AND COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome, '') LIKE @NomeCliente ");
            if (filtro.DataAgendamento.HasValue) sb.Append("AND CAST(A.DataHora AS DATE) = CAST(@DataAgendamento AS DATE) ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                using (var countCmd = connection.CreateCommand("SELECT COUNT(*) " + baseQuery))
                {
                    AddFiltroParams(countCmd, filtro);
                    countCmd.AddWithValue("@IdSalao", idSalao);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string dataQuery = "SELECT P.*, COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome) as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento " +
                                   baseQuery +
                                   "ORDER BY P.CriadoEm DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = connection.CreateCommand(dataQuery))
                {
                    AddFiltroParams(cmd, filtro);
                    cmd.AddWithValue("@IdSalao", idSalao);
                    cmd.AddWithValue("@Offset", (filtro.PageIndex - 1) * filtro.PageSize);
                    cmd.AddWithValue("@PageSize", filtro.PageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var p = Map(reader);
                            p.NomeCliente = reader["NomeCliente"] is DBNull ? "" : reader["NomeCliente"].ToString();
                            p.NomeServico = reader["NomeServico"] is DBNull ? "" : reader["NomeServico"].ToString();
                            p.DataAgendamento = reader["DataAgendamento"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataAgendamento"]);
                            result.Items.Add(p);
                        }
                    }
                }
            }
            return result;
        }

        private void AddFiltroParams(IDbCommand cmd, PagamentoFiltroDTO filtro)
        {
            if (filtro.DataInicio.HasValue) cmd.AddWithValue("@DataInicio", filtro.DataInicio.Value);
            if (filtro.DataFim.HasValue) cmd.AddWithValue("@DataFim", filtro.DataFim.Value);
            if (!string.IsNullOrEmpty(filtro.Status)) cmd.AddWithValue("@Status", filtro.Status);
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) cmd.AddWithValue("@NomeCliente", "%" + filtro.NomeCliente + "%");
            if (filtro.DataAgendamento.HasValue) cmd.AddWithValue("@DataAgendamento", filtro.DataAgendamento.Value);
        }

        public (decimal totalValor, int totalContagem) ObterResumo(int idSalao, PagamentoFiltroDTO filtro)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_Pagamento P ");
            sb.Append("LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento ");
            sb.Append("LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico ");
            sb.Append("LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PePed ON Ped.IdPessoa = PePed.IdPessoa ");
            sb.Append("LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto ");
            sb.Append("LEFT JOIN CorteCor_Pessoa PeVenda ON V.IdPessoa = PeVenda.IdPessoa ");
            sb.Append("WHERE (P.Ativo = 1 OR P.Ativo IS NULL) AND (P.IdSalao = @IdSalao OR S.IdSalao = @IdSalao OR Ped.IdSalao = @IdSalao OR V.IdSalao = @IdSalao) ");

            if (filtro.DataInicio.HasValue) sb.Append("AND P.CriadoEm >= @DataInicio ");
            if (filtro.DataFim.HasValue) sb.Append("AND P.CriadoEm <= @DataFim ");
            if (!string.IsNullOrEmpty(filtro.Status)) sb.Append("AND P.Status = @Status ");
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) sb.Append("AND COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome, '') LIKE @NomeCliente ");
            if (filtro.DataAgendamento.HasValue) sb.Append("AND CAST(A.DataHora AS DATE) = CAST(@DataAgendamento AS DATE) ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                using (var cmd = connection.CreateCommand("SELECT ISNULL(SUM(P.Valor), 0), COUNT(*) " + baseQuery))
                {
                    AddFiltroParams(cmd, filtro);
                    cmd.AddWithValue("@IdSalao", idSalao);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (Convert.ToDecimal(reader[0]), Convert.ToInt32(reader[1]));
                        }
                    }
                }
            }
            return (0, 0);
        }

        public virtual Pagamento ObterPorId(Guid idPagamento)
        {
            string query = @"
            SELECT P.*, COALESCE(Pe.Nome, PePed.Nome, PeVenda.Nome) as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento
            FROM CorteCor_Pagamento P
            LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento
            LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa
            LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
            LEFT JOIN CorteCor_Pedido Ped ON P.IdPedido = Ped.IdPedido
            LEFT JOIN CorteCor_Pessoa PePed ON Ped.IdPessoa = PePed.IdPessoa
            LEFT JOIN CorteCor_VendaProduto V ON P.IdVendaProduto = V.IdVendaProduto
            LEFT JOIN CorteCor_Pessoa PeVenda ON V.IdPessoa = PeVenda.IdPessoa
            WHERE P.IdPagamento = @Id;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@Id", idPagamento);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return Map(reader);
                }
            }
        }

        public override void Excluir(int id) => throw new NotSupportedException("CorteCor_Pagamento utiliza UNIQUEIDENTIFIER. Use Excluir(Guid id).");

        public void Excluir(Guid id)
        {
            string query = "DELETE FROM CorteCor_Pagamento WHERE IdPagamento = @Id;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar) => throw new NotSupportedException("CorteCor_Pagamento utiliza UNIQUEIDENTIFIER.");
        public async Task<bool> SincronizarPagamento(Guid idPagamento, MercadoPagoService mpService, int idSalao)
        {
            var pagamento = ObterPorId(idPagamento);
            if (pagamento == null || string.IsNullOrEmpty(pagamento.MercadoPagoPaymentId)) return false;

            var mpPayment = await mpService.GetPaymentDetailsAsync(pagamento.MercadoPagoPaymentId);
            if (mpPayment == null) return false;

            pagamento.MpStatus = mpPayment.Status;
            pagamento.MpStatusDetail = mpPayment.StatusDetail;

            // Lógica de atualização de status baseada na resposta da API
            if (mpPayment.Status == "approved")
            {
                pagamento.Status = "Pago";
                pagamento.PagoEm = mpPayment.DateApproved ?? DateTime.UtcNow;

                if (pagamento.IdAgendamento.HasValue)
                {
                    var agHandler = new AgendamentoHandler(_dbHandler);
                    agHandler.AtualizarStatus(pagamento.IdAgendamento.Value, "Pago", idSalao);
                }
            }
            else if (mpPayment.Status == "cancelled" || mpPayment.Status == "rejected")
            {
                // Se foi rejeitado ou cancelado, marcamos este pagamento como inativo/cancelado
                // mas NÃO cancelamos o agendamento, permitindo nova tentativa.
                pagamento.Status = "Cancelado";
                pagamento.Ativo = false;
            }

            AtualizarPagamento(pagamento, idSalao);
            return true;
        }

        public void AtualizarStatusWebhook(Guid idPagamento, string status, long? mercadoPagoPaymentId, string mpStatus, string mpStatusDetail, DateTime? pagoEm)
        {
            Console.WriteLine($"[Webhook] Atualizando pagamento {idPagamento} no banco. Status: {status}, MP_PaymentId: {mercadoPagoPaymentId}");

            using (var connection = _dbHandler.GetConnection())
            {
                // 1. Atualizar Tabela de Pagamento
                string queryPagamento = @"
            UPDATE CorteCor_Pagamento
            SET Status = @Status,
                MercadoPagoPaymentId = @MercadoPagoPaymentId,
                MpStatus = @MpStatus,
                MpStatusDetail = @MpStatusDetail,
                AtualizadoEm = GETUTCDATE(),
                PagoEm = @PagoEm 
            WHERE IdPagamento = @IdPagamento;";

                using (var command = connection.CreateCommand(queryPagamento))
                {
                    command.AddWithValue("@Status", status);
                    command.AddWithValue("@MercadoPagoPaymentId", mercadoPagoPaymentId.HasValue ? (object)mercadoPagoPaymentId.Value.ToString() : DBNull.Value);
                    command.AddWithValue("@MpStatus", (object?)mpStatus ?? DBNull.Value);
                    command.AddWithValue("@MpStatusDetail", (object?)mpStatusDetail ?? DBNull.Value);
                    command.AddWithValue("@PagoEm", (object?)pagoEm ?? DBNull.Value);
                    command.AddWithValue("@IdPagamento", idPagamento);

                    command.ExecuteNonQuery();
                }

                // 2. Cascata para Agendamento
                string novoStatusAgendamento = null;
                if (status.Equals("Pago", StringComparison.OrdinalIgnoreCase) || status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    novoStatusAgendamento = "Pago";
                    Console.WriteLine($"[Sync] Pagamento {idPagamento} aprovado. Agendamento vinculado atualizado para Pago.");
                }
                else if (status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase) || status.Equals("rejected", StringComparison.OrdinalIgnoreCase) || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    novoStatusAgendamento = "Agendado"; // Revert to Agendado or Pendente? User said "Pendente" in previous code, asking for Pago consistency implies keeping it simple.
                                                        // Looking at previous code: novoStatusAgendamento = "Pendente";
                                                        // If rejected, it should probably go back to Pendente (allowing retry) or Agendado. 
                                                        // Let's keep it Pendente as it was before, just to be safe.
                    novoStatusAgendamento = "Pendente";
                    Console.WriteLine($"[Sync] Pagamento {idPagamento} cancelado. Agendamento vinculado voltando para Pendente.");
                }

                if (!string.IsNullOrEmpty(novoStatusAgendamento))
                {
                    string queryAgendamento = @"
                UPDATE CorteCor_Agendamento A
                INNER JOIN CorteCor_Pagamento P ON A.IdAgendamento = P.IdAgendamento
                SET Status = @StatusAg
                WHERE P.IdPagamento = @IdPagamento;";

                    using (var commandAg = connection.CreateCommand(queryAgendamento))
                    {
                        commandAg.AddWithValue("@StatusAg", novoStatusAgendamento);
                        commandAg.AddWithValue("@IdPagamento", idPagamento);
                        commandAg.ExecuteNonQuery();
                    }
                }
            }
        }

        public override void Cadastrar(Pagamento entity) => throw new NotSupportedException("Use CadastrarPagamento(Pagamento pago).");
    }

    public class ModeloEmailHandler : EntityHandler<ModeloEmail>
    {
        public ModeloEmailHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }

        public override void Cadastrar(ModeloEmail entity)
        {
            if (ObterPorEventoIncluindoInativos(entity.IdSalao, entity.TipoEvento) != null)
            {
                throw new InvalidOperationException("Já existe um modelo de e-mail criado para este evento. Edite o modelo existente ou escolha outro evento.");
            }

            string query = @"
        INSERT INTO CorteCor_ModeloEmail (IdSalao, TipoEvento, Assunto, CorpoHTML, Ativo, DataAtualizacao)
        VALUES (@IdSalao, @TipoEvento, @Assunto, @CorpoHTML, @Ativo, GETDATE());";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", entity.IdSalao);
                command.AddWithValue("@TipoEvento", entity.TipoEvento);
                command.AddWithValue("@Assunto", entity.Assunto);
                command.AddWithValue("@CorpoHTML", entity.CorpoHTML);
                command.AddWithValue("@Ativo", entity.Ativo);
                command.ExecuteNonQuery();
            }
        }

        public virtual void Atualizar(ModeloEmail entity)
        {
            string query = @"
        UPDATE CorteCor_ModeloEmail
        SET TipoEvento = @TipoEvento,
            Assunto = @Assunto,
            CorpoHTML = @CorpoHTML,
            Ativo = @Ativo,
            DataAtualizacao = GETDATE()
        WHERE IdModelo = @IdModelo AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@TipoEvento", entity.TipoEvento);
                command.AddWithValue("@Assunto", entity.Assunto);
                command.AddWithValue("@CorpoHTML", entity.CorpoHTML);
                command.AddWithValue("@Ativo", entity.Ativo);
                command.AddWithValue("@IdModelo", entity.IdModelo);
                command.AddWithValue("@IdSalao", entity.IdSalao);
                command.ExecuteNonQuery();
            }
        }

        public override List<ModeloEmail> Listar() => throw new NotSupportedException("Use ListarPorSalao.");

        public virtual List<ModeloEmail> ListarPorSalao(int idSalao)
        {
            string query = "SELECT * FROM CorteCor_ModeloEmail WHERE IdSalao = @IdSalao ORDER BY TipoEvento";
            var lista = new List<ModeloEmail>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(MapReader(reader));
                    }
                }
            }
            return lista;
        }

        public virtual ModeloEmail ObterPorId(int idModelo, int idSalao)
        {
            string query = "SELECT * FROM CorteCor_ModeloEmail WHERE IdModelo = @IdModelo AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdModelo", idModelo);
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) return MapReader(reader);
                }
            }
            return null;
        }

        public virtual ModeloEmail ObterPorEventoIncluindoInativos(int idSalao, string tipoEvento, int? ignorarIdModelo = null)
        {
            string query = @"
        SELECT TOP 1 *
        FROM CorteCor_ModeloEmail
        WHERE IdSalao = @IdSalao
          AND TipoEvento = @TipoEvento
          AND (@IgnorarIdModelo IS NULL OR IdModelo <> @IgnorarIdModelo)
        ORDER BY Ativo DESC, DataAtualizacao DESC;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@TipoEvento", tipoEvento ?? string.Empty);
                command.AddWithValue("@IgnorarIdModelo", ignorarIdModelo.HasValue ? ignorarIdModelo.Value : (object)DBNull.Value);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) return MapReader(reader);
                }
            }
            return null;
        }

        public virtual ModeloEmail ObterPorEvento(int idSalao, string tipoEvento)
        {
            string query = "SELECT TOP 1 * FROM CorteCor_ModeloEmail WHERE IdSalao = @IdSalao AND TipoEvento = @TipoEvento AND Ativo = 1";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@TipoEvento", tipoEvento);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) return MapReader(reader);
                }
            }
            return null;
        }

        public static bool IsDuplicateKeyException(Exception ex)
        {
            for (var current = ex; current != null; current = current.InnerException)
            {
                if (current.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("UQ_ModeloEmail_Salao_Evento", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("1062", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Excluir(int id)
        {
            // Physical delete for templates? Or logical? 
            // Let's assume physical delete is fine for configuration, or just deactive.
            // Implementing physical delete for now as per "Excluir" name.
            string query = "DELETE FROM CorteCor_ModeloEmail WHERE IdModelo = @IdModelo";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdModelo", id);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string query = "UPDATE CorteCor_ModeloEmail SET Ativo = @Ativo WHERE IdModelo = @IdModelo";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Ativo", ativar);
                command.AddWithValue("@IdModelo", id);
                command.ExecuteNonQuery();
            }
        }

        private ModeloEmail MapReader(System.Data.IDataReader reader)
        {
            return new ModeloEmail
            {
                IdModelo = reader["IdModelo"] is DBNull ? 0 : Convert.ToInt32(reader["IdModelo"]),
                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                TipoEvento = reader["TipoEvento"] is DBNull ? string.Empty : reader["TipoEvento"].ToString() ?? string.Empty,
                Assunto = reader["Assunto"] is DBNull ? string.Empty : reader["Assunto"].ToString() ?? string.Empty,
                CorpoHTML = reader["CorpoHTML"] is DBNull ? string.Empty : reader["CorpoHTML"].ToString() ?? string.Empty,
                Ativo = !(reader["Ativo"] is DBNull) && Convert.ToBoolean(reader["Ativo"]),
                DataAtualizacao = reader["DataAtualizacao"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataAtualizacao"])
            };
        }
    }

    public interface ILembreteHandler
    {
        List<LembreteConfig> ListarConfig(int idSalao);
        void SalvarConfig(LembreteConfig config);
        void ExcluirConfig(int idConfig);
        void ExcluirLembretesPendentes(int idAgendamento);
        void GerarLembretes(int idAgendamento);
        void AplicarRegraRetroativa(int idConfig);
        bool VerificarLimiteEmail(int idSalao, out int enviados, out int limite);

        // Methods moved from LembreteService for better encapsulation - Optimized with Types
        List<LembreteAgendado> ObterLembretesPendentes();
        LembreteEnvioDTO ObterDadosEnvio(int idLembrete);
        void AtualizarStatusLembrete(int idLembrete, string status);
        void RegistrarLogEnvio(int idLembrete, int idAgendamento, string destinatario, string assunto, string status, string? mensagemErro, string tipoLembrete, string? telefone);
    }

    public class LembreteHandler : ILembreteHandler
    {
        private readonly IDatabaseHandler _dbHandler;

        public LembreteHandler(IDatabaseHandler dbHandler = null)
        {
            _dbHandler = dbHandler ?? new DatabaseHandler();
        }

        public List<LembreteAgendado> ObterLembretesPendentes()
        {
            var lista = new List<LembreteAgendado>();
            string query = @"SELECT IdLembrete, IdAgendamento FROM CorteCor_LembreteAgendado 
                             WHERE Status = 'Pendente' AND DataEnvioProgramada <= @Agora";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Agora", DateTime.Now);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new LembreteAgendado
                        {
                            IdLembrete = (int)reader["IdLembrete"],
                            IdAgendamento = (int)reader["IdAgendamento"]
                        });
                    }
                }
            }
            return lista;
        }

        public LembreteEnvioDTO ObterDadosEnvio(int idLembrete)
        {
            string query = @"
                SELECT 
                    P.Nome AS NomeCliente, P.Email AS EmailCliente, P.Telefone AS TelefoneCliente,
                    A.DataHora AS DataHoraAgendamento,
                    S.Nome AS NomeServico,
                    F.Nome AS NomeProfissional,
                    TS.Nome AS NomeSalao, TS.IdSalao,
                    M.Assunto AS AssuntoEmail, M.CorpoHTML AS CorpoEmail,
                    MS.Conteudo AS ConteudoSMS,
                    C.TipoLembrete
                FROM CorteCor_LembreteAgendado LA
                JOIN CorteCor_Agendamento A ON LA.IdAgendamento = A.IdAgendamento
                JOIN CorteCor_Pessoa P ON A.IdPessoa = P.IdPessoa
                JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
                JOIN CorteCor_Funcionario F ON A.IdFuncionario = F.IdFuncionario
                JOIN CorteCor_LembreteConfig C ON LA.IdConfig = C.IdConfig
                LEFT JOIN CorteCor_ModeloEmail M ON C.IdModeloEmail = M.IdModelo
                LEFT JOIN CorteCor_ModeloSMS MS ON C.IdModeloSMS = MS.IdModelo
                LEFT JOIN CorteCor_Salao TS ON S.IdSalao = TS.IdSalao
                WHERE LA.IdLembrete = @IdLembrete";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdLembrete", idLembrete);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var dto = new LembreteEnvioDTO
                        {
                            NomeCliente = reader["NomeCliente"].ToString(),
                            EmailCliente = reader["EmailCliente"].ToString(),
                            TelefoneCliente = reader["TelefoneCliente"] is DBNull ? "" : reader["TelefoneCliente"].ToString(),
                            DataHoraAgendamento = Convert.ToDateTime(reader["DataHoraAgendamento"]),
                            NomeServico = reader["NomeServico"].ToString(),
                            NomeProfissional = reader["NomeProfissional"].ToString(),
                            NomeSalao = reader["NomeSalao"] is DBNull ? "Salão" : reader["NomeSalao"].ToString(),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            TipoLembrete = reader["TipoLembrete"] is DBNull ? "Email" : reader["TipoLembrete"].ToString()
                        };

                        if (dto.TipoLembrete == "Email")
                        {
                            dto.AssuntoModelo = reader["AssuntoEmail"] is DBNull ? null : reader["AssuntoEmail"].ToString();
                            dto.CorpoModelo = reader["CorpoEmail"] is DBNull ? null : reader["CorpoEmail"].ToString();
                        }
                        else
                        {
                            dto.AssuntoModelo = "SMS"; // SMS doesn't use subject
                            dto.CorpoModelo = reader["ConteudoSMS"] is DBNull ? null : reader["ConteudoSMS"].ToString();
                        }

                        return dto;
                    }
                }
            }
            return null;
        }

        public void AtualizarStatusLembrete(int idLembrete, string status)
        {
            string query = @"UPDATE CorteCor_LembreteAgendado SET Status = @Status, DataEnvioReal = @Data WHERE IdLembrete = @Id";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", status);
                command.AddWithValue("@Data", DateTime.Now);
                command.AddWithValue("@Id", idLembrete);
                command.ExecuteNonQuery();
            }
        }

        public void RegistrarLogEnvio(int idLembrete, int idAgendamento, string destinatario, string assunto, string status, string? mensagemErro, string tipoLembrete, string? telefone)
        {
            string query = @"INSERT INTO CorteCor_LogEnvioEmail (IdLembrete, IdAgendamento, DataEnvio, Destinatario, Assunto, Status, MensagemErro, TipoLembrete, Telefone)
                             VALUES (@IdLembrete, @IdAgendamento, @DataEnvio, @Destinatario, @Assunto, @Status, @MensagemErro, @TipoLembrete, @Telefone)";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdLembrete", idLembrete);
                command.AddWithValue("@IdAgendamento", idAgendamento);
                command.AddWithValue("@DataEnvio", DateTime.Now);
                command.AddWithValue("@Destinatario", destinatario ?? "");
                command.AddWithValue("@Assunto", assunto ?? "");
                command.AddWithValue("@Status", status);
                command.AddWithValue("@MensagemErro", mensagemErro ?? (object)DBNull.Value);
                command.AddWithValue("@TipoLembrete", tipoLembrete);
                command.AddWithValue("@Telefone", telefone ?? (object)DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        public List<LembreteConfig> ListarConfig(int idSalao)
        {
            var lista = new List<LembreteConfig>();
            string query = @"
            SELECT C.*, M.Assunto AS AssuntoModeloEmail, S.Conteudo AS ConteudoModeloSMS
            FROM CorteCor_LembreteConfig C
            LEFT JOIN CorteCor_ModeloEmail M ON C.IdModeloEmail = M.IdModelo
            LEFT JOIN CorteCor_ModeloSMS S ON C.IdModeloSMS = S.IdModelo
            WHERE C.IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var config = new LembreteConfig
                        {
                            IdConfig = (reader["IdConfig"] == null || reader["IdConfig"] is DBNull) ? 0 : Convert.ToInt32(reader["IdConfig"]),
                            IdSalao = (reader["IdSalao"] == null || reader["IdSalao"] is DBNull) ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            AntecedenciaValor = (reader["AntecedenciaValor"] == null || reader["AntecedenciaValor"] is DBNull) ? 0 : Convert.ToInt32(reader["AntecedenciaValor"]),
                            AntecedenciaUnidade = reader["AntecedenciaUnidade"]?.ToString() ?? "Horas",
                            IdModeloEmail = (reader["IdModeloEmail"] == null || reader["IdModeloEmail"] is DBNull) ? (int?)null : Convert.ToInt32(reader["IdModeloEmail"]),
                            IdModeloSMS = (reader["IdModeloSMS"] == null || reader["IdModeloSMS"] is DBNull) ? (int?)null : Convert.ToInt32(reader["IdModeloSMS"]),
                            TipoLembrete = reader["TipoLembrete"]?.ToString() ?? "Email",
                            Ativo = (reader["Ativo"] == null || reader["Ativo"] is DBNull) ? false : Convert.ToBoolean(reader["Ativo"]),
                            DataCriacao = (reader["DataCriacao"] == null || reader["DataCriacao"] is DBNull) ? DateTime.MinValue : Convert.ToDateTime(reader["DataCriacao"]),
                            DataInicio = (reader["DataInicio"] == null || reader["DataInicio"] is DBNull) ? DateTime.MinValue : Convert.ToDateTime(reader["DataInicio"]),
                            DataFim = (reader["DataFim"] == null || reader["DataFim"] is DBNull) ? (DateTime?)null : Convert.ToDateTime(reader["DataFim"])
                        };

                        if (config.TipoLembrete == "Email")
                        {
                            var val = reader["AssuntoModeloEmail"];
                            config.AssuntoModelo = (val == null || val is DBNull) ? "Padrão" : val.ToString() ?? "Padrão";
                        }
                        else
                        {
                            var val = reader["ConteudoModeloSMS"];
                            config.AssuntoModelo = (val == null || val is DBNull) ? "Padrão" : "SMS Personalizado";
                        }

                        lista.Add(config);
                    }
                }
            }
            return lista;
        }

        public void SalvarConfig(LembreteConfig config)
        {
            string query;
            if (config.IdConfig > 0)
            {
                query = @"UPDATE CorteCor_LembreteConfig 
                      SET AntecedenciaValor = @AntecedenciaValor, 
                          AntecedenciaUnidade = @AntecedenciaUnidade, 
                          IdModeloEmail = @IdModeloEmail, 
                          IdModeloSMS = @IdModeloSMS,
                          TipoLembrete = @TipoLembrete,
                          Ativo = @Ativo,
                          DataInicio = @DataInicio,
                          DataFim = @DataFim
                      WHERE IdConfig = @IdConfig AND IdSalao = @IdSalao";
            }
            else
            {
                query = @"INSERT INTO CorteCor_LembreteConfig (IdSalao, AntecedenciaValor, AntecedenciaUnidade, IdModeloEmail, IdModeloSMS, TipoLembrete, Ativo, DataInicio, DataFim)
                      VALUES (@IdSalao, @AntecedenciaValor, @AntecedenciaUnidade, @IdModeloEmail, @IdModeloSMS, @TipoLembrete, @Ativo, @DataInicio, @DataFim);
                      SELECT SCOPE_IDENTITY();";
            }

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", config.IdSalao);
                if (config.IdConfig > 0)
                    command.AddWithValue("@IdConfig", config.IdConfig);

                command.AddWithValue("@AntecedenciaValor", config.AntecedenciaValor);
                command.AddWithValue("@AntecedenciaUnidade", config.AntecedenciaUnidade);
                command.AddWithValue("@IdModeloEmail", (object)config.IdModeloEmail ?? DBNull.Value);
                command.AddWithValue("@IdModeloSMS", (object)config.IdModeloSMS ?? DBNull.Value);
                command.AddWithValue("@TipoLembrete", config.TipoLembrete);
                command.AddWithValue("@Ativo", config.Ativo);
                command.AddWithValue("@DataInicio", config.DataInicio == DateTime.MinValue ? DateTime.Now : config.DataInicio);
                command.AddWithValue("@DataFim", (object)config.DataFim ?? DBNull.Value);

                if (config.IdConfig > 0)
                {
                    command.ExecuteNonQuery();
                }
                else
                {
                    object result = command.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                    {
                        config.IdConfig = id;
                    }
                }
            }

            // Aplicar retroativamente se a regra estiver ativa
            if (config.Ativo && config.IdConfig > 0)
            {
                AplicarRegraRetroativa(config.IdConfig);
            }
        }

        public void ExcluirConfig(int idConfig)
        {
            using (var connection = _dbHandler.GetConnection())
            {
                // 1. Excluir lembretes agendados associados a esta configuração
                string deleteLembretesQuery = "DELETE FROM CorteCor_LembreteAgendado WHERE IdConfig = @IdConfig AND Status = 'Pendente'";
                using (var command = connection.CreateCommand(deleteLembretesQuery))
                {
                    command.AddWithValue("@IdConfig", idConfig);
                    command.ExecuteNonQuery();
                }

                // 2. Excluir a configuração
                string deleteConfigQuery = "DELETE FROM CorteCor_LembreteConfig WHERE IdConfig = @IdConfig";
                using (var command = connection.CreateCommand(deleteConfigQuery))
                {
                    command.AddWithValue("@IdConfig", idConfig);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ExcluirLembretesPendentes(int idAgendamento)
        {
            string query = "DELETE FROM CorteCor_LembreteAgendado WHERE IdAgendamento = @IdAgendamento AND Status = 'Pendente'";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdAgendamento", idAgendamento);
                command.ExecuteNonQuery();
            }
        }

        public void GerarLembretes(int idAgendamento)
        {
            // 0. Limpar lembretes pendentes antigos para evitar duplicidade ou inconsistência
            ExcluirLembretesPendentes(idAgendamento);

            DateTime dataAgendamento;
            int idSalao;

            // 1. Get Agendamento Info (DataHora, IdSalao via join with Servico)
            string queryJump = @"
            SELECT A.DataHora, S.IdSalao 
            FROM CorteCor_Agendamento A
            JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
            WHERE A.IdAgendamento = @Id";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(queryJump))
            {
                command.AddWithValue("@Id", idAgendamento);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        Console.WriteLine($"[LembreteHandler] Agendamento {idAgendamento} não encontrado ou sem serviço vinculado.");
                        return;
                    }
                    dataAgendamento = Convert.ToDateTime(reader["DataHora"]);
                    idSalao = Convert.ToInt32(reader["IdSalao"]);
                }
            }

            // 2. Get Active Configs for Salon that are valid for this appointment date
            var configs = ListarConfig(idSalao).Where(c => c.Ativo &&
                                                          dataAgendamento >= c.DataInicio &&
                                                          (c.DataFim == null || dataAgendamento <= c.DataFim)).ToList();

            // 3. Insert Schedules
            string insertQuery = @"INSERT INTO CorteCor_LembreteAgendado (IdAgendamento, IdConfig, DataEnvioProgramada, Status)
                               VALUES (@IdAgendamento, @IdConfig, @DataEnvio, 'Pendente')";

            using (var connection = _dbHandler.GetConnection())
            {
                foreach (var config in configs)
                {
                    DateTime dataEnvio = dataAgendamento;
                    if (config.AntecedenciaUnidade == "Horas")
                        dataEnvio = dataEnvio.AddHours(-config.AntecedenciaValor);
                    else if (config.AntecedenciaUnidade == "Dias")
                        dataEnvio = dataEnvio.AddDays(-config.AntecedenciaValor);
                    else if (config.AntecedenciaUnidade == "Minutos")
                        dataEnvio = dataEnvio.AddMinutes(-config.AntecedenciaValor);

                    // Lógica de agendamento:
                    // 1. Se DataEnvio > Agora -> Agendar para DataEnvio (Normal)
                    // 2. Se DataEnvio <= Agora mas DataAgendamento > Agora -> Agendar para Agora (Lembrete imediato para agendamento de última hora)
                    // 3. Se DataAgendamento <= Agora -> Não agendar (Evento já ocorreu)

                    if (dataEnvio > DateTime.Now)
                    {
                        using (var command = connection.CreateCommand(insertQuery))
                        {
                            command.AddWithValue("@IdAgendamento", idAgendamento);
                            command.AddWithValue("@IdConfig", config.IdConfig);
                            command.AddWithValue("@DataEnvio", dataEnvio);
                            command.ExecuteNonQuery();
                        }
                        Console.WriteLine($"[LembreteHandler] Lembrete agendado com sucesso para {dataEnvio}.");
                    }
                    else if (dataAgendamento > DateTime.Now)
                    {
                        DateTime dataEnvioImediato = DateTime.Now;

                        using (var command = connection.CreateCommand(insertQuery))
                        {
                            command.AddWithValue("@IdAgendamento", idAgendamento);
                            command.AddWithValue("@IdConfig", config.IdConfig);
                            command.AddWithValue("@DataEnvio", dataEnvioImediato);
                            command.ExecuteNonQuery();
                        }
                        Console.WriteLine($"[LembreteHandler] Prazo de antecedência passou, mas evento é futuro. Agendado para envio IMEDIATO ({dataEnvioImediato}).");
                    }
                }
            }
        }

        public void AplicarRegraRetroativa(int idConfig)
        {
            // 1. Obter a configuração
            LembreteConfig config = null;
            string queryConfig = "SELECT * FROM CorteCor_LembreteConfig WHERE IdConfig = @Id";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(queryConfig))
            {
                command.AddWithValue("@Id", idConfig);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        config = new LembreteConfig
                        {
                            IdConfig = Convert.ToInt32(reader["IdConfig"]),
                            IdSalao = Convert.ToInt32(reader["IdSalao"]),
                            AntecedenciaValor = Convert.ToInt32(reader["AntecedenciaValor"]),
                            AntecedenciaUnidade = reader["AntecedenciaUnidade"].ToString(),
                            Ativo = Convert.ToBoolean(reader["Ativo"]),
                            DataInicio = Convert.ToDateTime(reader["DataInicio"]),
                            DataFim = reader["DataFim"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["DataFim"])
                        };
                    }
                }
            }

            if (config == null || !config.Ativo) return;

            // 2. Buscar agendamentos que atendem à regra:
            // - Ativos
            // - Do mesmo salão
            // - DataHora dentro do período da regra
            // - Sem esse lembrete já agendado (evita duplicação)
            string queryAgendamentos = @"
            SELECT A.IdAgendamento, A.DataHora 
            FROM CorteCor_Agendamento A
            JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
            WHERE S.IdSalao = @IdSalao
              AND A.DataHora >= @DataInicio
              AND (@DataFim IS NULL OR A.DataHora <= @DataFim)
              AND (A.Excluido = 0 OR A.Excluido IS NULL)
              AND A.DataHora > GETDATE() -- Apenas futuros
              AND NOT EXISTS (SELECT 1 FROM CorteCor_LembreteAgendado LA WHERE LA.IdAgendamento = A.IdAgendamento AND LA.IdConfig = @IdConfig)";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(queryAgendamentos))
            {
                command.AddWithValue("@IdSalao", config.IdSalao);
                command.AddWithValue("@IdConfig", config.IdConfig);
                command.AddWithValue("@DataInicio", config.DataInicio);
                command.AddWithValue("@DataFim", (object)config.DataFim ?? DBNull.Value);

                var agendamentosAction = new List<(int Id, DateTime Data)>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        agendamentosAction.Add((Convert.ToInt32(reader["IdAgendamento"]), Convert.ToDateTime(reader["DataHora"])));
                    }
                }

                // 3. Gerar os lembretes
                string insertQuery = @"INSERT INTO CorteCor_LembreteAgendado (IdAgendamento, IdConfig, DataEnvioProgramada, Status)
                                   VALUES (@IdAgendamento, @IdConfig, @DataEnvio, 'Pendente')";

                foreach (var ag in agendamentosAction)
                {
                    DateTime dataEnvio = ag.Data;
                    if (config.AntecedenciaUnidade == "Horas")
                        dataEnvio = dataEnvio.AddHours(-config.AntecedenciaValor);
                    else if (config.AntecedenciaUnidade == "Dias")
                        dataEnvio = dataEnvio.AddDays(-config.AntecedenciaValor);
                    else if (config.AntecedenciaUnidade == "Minutos")
                        dataEnvio = dataEnvio.AddMinutes(-config.AntecedenciaValor);

                    // Se o tempo de envio já passou, envia agora (desde que o agendamento seja futuro)
                    if (dataEnvio <= DateTime.Now)
                        dataEnvio = DateTime.Now;

                    using (var cmdInsert = connection.CreateCommand(insertQuery))
                    {
                        cmdInsert.AddWithValue("@IdAgendamento", ag.Id);
                        cmdInsert.AddWithValue("@IdConfig", config.IdConfig);
                        cmdInsert.AddWithValue("@DataEnvio", dataEnvio);
                        cmdInsert.ExecuteNonQuery();
                    }
                }
            }
        }



        public PagedResult<LogEnvioEmail> ListarLogsEnvio(int idSalao, DateTime? inicio, DateTime? fim, string destinatario, string assunto, string status, int page = 1, int pageSize = 10, string tipoLembrete = null)
        {
            var result = new PagedResult<LogEnvioEmail>
            {
                PageIndex = page,
                PageSize = pageSize
            };

            var sb = new System.Text.StringBuilder();
            sb.Append(@" FROM CorteCor_LogEnvioEmail L
                         JOIN CorteCor_Agendamento A ON L.IdAgendamento = A.IdAgendamento
                         JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
                         WHERE S.IdSalao = @IdSalao ");

            if (inicio.HasValue) sb.Append("AND L.DataEnvio >= @Inicio ");
            if (fim.HasValue) sb.Append("AND L.DataEnvio <= @Fim ");
            if (!string.IsNullOrEmpty(destinatario)) sb.Append("AND L.Destinatario LIKE @Destinatario ");
            if (!string.IsNullOrEmpty(assunto)) sb.Append("AND L.Assunto LIKE @Assunto ");
            if (!string.IsNullOrEmpty(status)) sb.Append("AND L.Status = @Status ");
            if (!string.IsNullOrEmpty(tipoLembrete)) sb.Append("AND L.TipoLembrete = @TipoLembrete ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                using (var countCmd = connection.CreateCommand("SELECT COUNT(*) " + baseQuery))
                {
                    countCmd.AddWithValue("@IdSalao", idSalao);
                    AddLogParams(countCmd, inicio, fim, destinatario, assunto, status, tipoLembrete);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string dataQuery = "SELECT L.* " +
                                   baseQuery +
                                   "ORDER BY L.DataEnvio DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = connection.CreateCommand(dataQuery))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    AddLogParams(command, inicio, fim, destinatario, assunto, status, tipoLembrete);
                    command.AddWithValue("@Offset", (page - 1) * pageSize);
                    command.AddWithValue("@PageSize", pageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var log = new LogEnvioEmail
                            {
                                IdLog = Convert.ToInt32(reader["IdLog"]),
                                IdLembrete = Convert.ToInt32(reader["IdLembrete"]),
                                IdAgendamento = Convert.ToInt32(reader["IdAgendamento"]),
                                DataEnvio = Convert.ToDateTime(reader["DataEnvio"]),
                                Destinatario = reader["Destinatario"].ToString(),
                                Assunto = reader["Assunto"].ToString(),
                                Status = reader["Status"].ToString(),
                                MensagemErro = reader["MensagemErro"] is DBNull ? null : reader["MensagemErro"].ToString()
                            };

                            // Check columns existence for backward compatibility or simple check
                            // Assuming migration script applied
                            try { log.TipoLembrete = reader["TipoLembrete"] is DBNull ? "Email" : reader["TipoLembrete"].ToString(); } catch { }
                            try { log.Telefone = reader["Telefone"] is DBNull ? null : reader["Telefone"].ToString(); } catch { }

                            result.Items.Add(log);
                        }
                    }
                }
            }
            return result;
        }

        private void AddLogParams(IDbCommand cmd, DateTime? inicio, DateTime? fim, string destinatario, string assunto, string status, string tipoLembrete)
        {
            if (inicio.HasValue) cmd.AddWithValue("@Inicio", inicio.Value);
            if (fim.HasValue) cmd.AddWithValue("@Fim", fim.Value);
            if (!string.IsNullOrEmpty(destinatario)) cmd.AddWithValue("@Destinatario", "%" + destinatario + "%");
            if (!string.IsNullOrEmpty(assunto)) cmd.AddWithValue("@Assunto", "%" + assunto + "%");
            if (!string.IsNullOrEmpty(status)) cmd.AddWithValue("@Status", status);
            if (!string.IsNullOrEmpty(tipoLembrete)) cmd.AddWithValue("@TipoLembrete", tipoLembrete);
        }

        public bool VerificarLimiteEmail(int idSalao, out int enviados, out int limite)
        {
            return VerificarLimite(idSalao, "Email", out enviados, out limite);
        }

        public bool VerificarLimiteSMS(int idSalao, out int enviados, out int limite)
        {
            return VerificarLimite(idSalao, "SMS", out enviados, out limite);
        }

        private bool VerificarLimite(int idSalao, string tipo, out int enviados, out int limite)
        {
            enviados = 0;
            limite = 0;

            using (var connection = _dbHandler.GetConnection())
            {
                // 1. Get Limit
                string columnName = tipo == "Email" ? "LimiteEnvioEmail" : "LimiteEnvioSMS";
                string queryLimit = $"SELECT {columnName} FROM CorteCor_Salao WHERE IdSalao = @IdSalao";
                using (var cmd = connection.CreateCommand(queryLimit))
                {
                    cmd.AddWithValue("@IdSalao", idSalao);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        limite = Convert.ToInt32(result);
                    }
                }

                // If limit is not set or 0, we still count against it (showing 0/0 reached)
                // but we might want a way to disable the check entirely (e.g. -1).
                // For now, if it's 0, it behaves like they reached it.

                // 2. Count Sent for CURRENT MONTH
                // We use ISNULL(L.TipoLembrete, 'Email') for backward compatibility
                string queryCount = @"
                    SELECT COUNT(*)
                    FROM CorteCor_LogEnvioEmail L
                    JOIN CorteCor_LembreteAgendado LA ON L.IdLembrete = LA.IdLembrete
                    JOIN CorteCor_Agendamento A ON LA.IdAgendamento = A.IdAgendamento
                    JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
                    WHERE S.IdSalao = @IdSalao
                      AND ISNULL(L.TipoLembrete, 'Email') = @Tipo
                      AND (L.Status = 'Sucesso' OR L.Status = 'Enviado')
                      AND MONTH(L.DataEnvio) = @Mes AND YEAR(L.DataEnvio) = @Ano";

                using (var cmd = connection.CreateCommand(queryCount))
                {
                    cmd.AddWithValue("@IdSalao", idSalao);
                    cmd.AddWithValue("@Tipo", tipo);
                    cmd.AddWithValue("@Mes", DateTime.Now.Month);
                    cmd.AddWithValue("@Ano", DateTime.Now.Year);
                    enviados = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            return enviados >= limite;
        }
    }

    public class CategoriaProdutoHandler : EntityHandler<CategoriaProduto>
    {
        public CategoriaProdutoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }

        public override void Cadastrar(CategoriaProduto entity)
        {
            throw new NotImplementedException();
        }

        public int CadastrarCategoria(CategoriaProduto categoria)
        {
            int novoId = 0;
            string query = @"
                INSERT INTO CorteCor_CategoriaProduto (IdSalao, Nome, Ativo, DataCadastro)
                VALUES (@IdSalao, @Nome, @Ativo, @DataCadastro);
                SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", categoria.IdSalao);
                command.AddWithValue("@Nome", categoria.Nome ?? "");
                command.AddWithValue("@Ativo", categoria.Ativo);
                command.AddWithValue("@DataCadastro", categoria.DataCadastro);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId;
        }

        public void Atualizar(CategoriaProduto categoria)
        {
            string query = @"
                UPDATE CorteCor_CategoriaProduto
                SET Nome = @Nome, Ativo = @Ativo
                WHERE IdCategoria = @IdCategoria AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", categoria.Nome ?? "");
                command.AddWithValue("@Ativo", categoria.Ativo);
                command.AddWithValue("@IdCategoria", categoria.IdCategoria);
                command.AddWithValue("@IdSalao", categoria.IdSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void Excluir(int id)
        {
            string query = "UPDATE CorteCor_CategoriaProduto SET Ativo = 0 WHERE IdCategoria = @IdCategoria";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdCategoria", id);
                command.ExecuteNonQuery();
            }
        }

        public void ExcluirPorSalao(int idCategoria, int idSalao)
        {
            string query = "UPDATE CorteCor_CategoriaProduto SET Ativo = 0 WHERE IdCategoria = @IdCategoria AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdCategoria", idCategoria);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string query = "UPDATE CorteCor_CategoriaProduto SET Ativo = @Ativo WHERE IdCategoria = @IdCategoria";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Ativo", ativar);
                command.AddWithValue("@IdCategoria", id);
                command.ExecuteNonQuery();
            }
        }

        public override List<CategoriaProduto> Listar()
        {
            string query = @"SELECT * FROM CorteCor_CategoriaProduto ORDER BY Nome";
            var lista = new List<CategoriaProduto>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    lista.Add(new CategoriaProduto
                    {
                        IdCategoria = reader["IdCategoria"] is DBNull ? 0 : Convert.ToInt32(reader["IdCategoria"]),
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                        Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),
                        DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                    });
                }
            }
            return lista;
        }

        public List<CategoriaProduto> ListarPorSalao(int idSalao)
        {
            string query = @"SELECT * FROM CorteCor_CategoriaProduto WHERE IdSalao = @IdSalao ORDER BY Nome";
            var lista = new List<CategoriaProduto>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new CategoriaProduto
                        {
                            IdCategoria = reader["IdCategoria"] is DBNull ? 0 : Convert.ToInt32(reader["IdCategoria"]),
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),
                            DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                        });
                    }
                }
            }
            return lista;
        }

        public CategoriaProduto? ObterPorIdESalao(int idCategoria, int idSalao)
        {
            string query = @"SELECT * FROM CorteCor_CategoriaProduto WHERE IdCategoria = @IdCategoria AND IdSalao = @IdSalao";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdCategoria", idCategoria);
                command.AddWithValue("@IdSalao", idSalao);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;

                    return new CategoriaProduto
                    {
                        IdCategoria = reader["IdCategoria"] is DBNull ? 0 : Convert.ToInt32(reader["IdCategoria"]),
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                        Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),
                        DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                    };
                }
            }
        }

        public bool ExisteNomePorSalao(string nome, int idSalao, int? ignorarIdCategoria = null)
        {
            string query = @"
                SELECT COUNT(*)
                FROM CorteCor_CategoriaProduto
                WHERE IdSalao = @IdSalao
                  AND Nome = @Nome
                  AND (@IgnorarIdCategoria IS NULL OR IdCategoria <> @IgnorarIdCategoria);";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);
                command.AddWithValue("@Nome", nome?.Trim() ?? "");
                command.AddWithValue("@IgnorarIdCategoria", (object?)ignorarIdCategoria ?? DBNull.Value);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public PagedResult<CategoriaProduto> ListarPaginadoPorSalao(int idSalao, string? pesquisa, bool incluirInativas, int pageIndex, int pageSize)
        {
            var result = new PagedResult<CategoriaProduto>
            {
                PageIndex = pageIndex < 1 ? 1 : pageIndex,
                PageSize = pageSize < 1 ? 10 : pageSize
            };

            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            using (var connection = _dbHandler.GetConnection())
            {
                string countQuery = @"
                SELECT COUNT(*)
                FROM CorteCor_CategoriaProduto
                WHERE IdSalao = @IdSalao
                  AND (@IncluirInativas = 1 OR Ativo = 1)
                  AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa);";

                using (var countCommand = connection.CreateCommand(countQuery))
                {
                    countCommand.AddWithValue("@IdSalao", idSalao);
                    countCommand.AddWithValue("@IncluirInativas", incluirInativas);
                    countCommand.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    result.TotalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                string query = @"
                SELECT *
                FROM CorteCor_CategoriaProduto
                WHERE IdSalao = @IdSalao
                  AND (@IncluirInativas = 1 OR Ativo = 1)
                  AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa)
                ORDER BY Nome
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@IncluirInativas", incluirInativas);
                    command.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    command.AddWithValue("@Offset", (result.PageIndex - 1) * result.PageSize);
                    command.AddWithValue("@PageSize", result.PageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(new CategoriaProduto
                            {
                                IdCategoria = reader["IdCategoria"] is DBNull ? 0 : Convert.ToInt32(reader["IdCategoria"]),
                                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                                Ativo = reader["Ativo"] is DBNull ? false : Convert.ToBoolean(reader["Ativo"]),
                                DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                            });
                        }
                    }
                }
            }

            return result;
        }
    }

    public class ProdutoHandler : EntityHandler<Produto>
    {
        public ProdutoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }

        public override void Cadastrar(Produto entity) { throw new NotImplementedException(); }

        public int CadastrarProduto(Produto produto)
        {
            int novoId = 0;
            string query = @"
                INSERT INTO CorteCor_Produto (
                    IdSalao, Nome, CodigoProprio, IdCategoria, Tags, TipoUso, Arquivado, Anotacoes,
                    PrecoCusto, PrecoVenda, MargemContribuicao, ControlarEstoque, EstoqueAtual, EstoqueMinimo,
                    Origem, ReferenciaEAN, PesoLiquido, PesoBruto, NCM, CEST, UnidadeComercial, ExcecaoIPI, CodBeneficioFiscalUF,
                    UnidadeTributadaDiferente, EANTributada, UnidadeTributada, QuantidadeTributada, IgnorarTribPrecoVenda,
                    AnotacoesFiscaisNFe, GrupoTributarioVinculado, DataCadastro, Excluido
                ) VALUES (
                    @IdSalao, @Nome, @CodigoProprio, @IdCategoria, @Tags, @TipoUso, @Arquivado, @Anotacoes,
                    @PrecoCusto, @PrecoVenda, @MargemContribuicao, @ControlarEstoque, @EstoqueAtual, @EstoqueMinimo,
                    @Origem, @ReferenciaEAN, @PesoLiquido, @PesoBruto, @NCM, @CEST, @UnidadeComercial, @ExcecaoIPI, @CodBeneficioFiscalUF,
                    @UnidadeTributadaDiferente, @EANTributada, @UnidadeTributada, @QuantidadeTributada, @IgnorarTribPrecoVenda,
                    @AnotacoesFiscaisNFe, @GrupoTributarioVinculado, @DataCadastro, @Excluido
                );
                SELECT SCOPE_IDENTITY();";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", produto.IdSalao);
                command.AddWithValue("@Nome", produto.Nome ?? "");
                command.AddWithValue("@CodigoProprio", (object)produto.CodigoProprio ?? DBNull.Value);
                command.AddWithValue("@IdCategoria", (object)produto.IdCategoria ?? DBNull.Value);
                command.AddWithValue("@Tags", (object)produto.Tags ?? DBNull.Value);
                command.AddWithValue("@TipoUso", (object)produto.TipoUso ?? DBNull.Value);
                command.AddWithValue("@Arquivado", produto.Arquivado);
                command.AddWithValue("@Anotacoes", (object)produto.Anotacoes ?? DBNull.Value);
                
                command.AddWithValue("@PrecoCusto", (object)produto.PrecoCusto ?? DBNull.Value);
                command.AddWithValue("@PrecoVenda", produto.PrecoVenda);
                command.AddWithValue("@MargemContribuicao", (object)produto.MargemContribuicao ?? DBNull.Value);
                
                command.AddWithValue("@ControlarEstoque", produto.ControlarEstoque);
                command.AddWithValue("@EstoqueAtual", (object)produto.EstoqueAtual ?? DBNull.Value);
                command.AddWithValue("@EstoqueMinimo", (object)produto.EstoqueMinimo ?? DBNull.Value);
                
                command.AddWithValue("@Origem", (object)produto.Origem ?? DBNull.Value);
                command.AddWithValue("@ReferenciaEAN", (object)produto.ReferenciaEAN ?? DBNull.Value);
                command.AddWithValue("@PesoLiquido", (object)produto.PesoLiquido ?? DBNull.Value);
                command.AddWithValue("@PesoBruto", (object)produto.PesoBruto ?? DBNull.Value);
                command.AddWithValue("@NCM", (object)produto.NCM ?? DBNull.Value);
                command.AddWithValue("@CEST", (object)produto.CEST ?? DBNull.Value);
                command.AddWithValue("@UnidadeComercial", (object)produto.UnidadeComercial ?? DBNull.Value);
                command.AddWithValue("@ExcecaoIPI", (object)produto.ExcecaoIPI ?? DBNull.Value);
                command.AddWithValue("@CodBeneficioFiscalUF", (object)produto.CodBeneficioFiscalUF ?? DBNull.Value);
                
                command.AddWithValue("@UnidadeTributadaDiferente", produto.UnidadeTributadaDiferente);
                command.AddWithValue("@EANTributada", (object)produto.EANTributada ?? DBNull.Value);
                command.AddWithValue("@UnidadeTributada", (object)produto.UnidadeTributada ?? DBNull.Value);
                command.AddWithValue("@QuantidadeTributada", (object)produto.QuantidadeTributada ?? DBNull.Value);
                command.AddWithValue("@IgnorarTribPrecoVenda", produto.IgnorarTribPrecoVenda);
                command.AddWithValue("@AnotacoesFiscaisNFe", (object)produto.AnotacoesFiscaisNFe ?? DBNull.Value);
                
                command.AddWithValue("@GrupoTributarioVinculado", (object)produto.GrupoTributarioVinculado ?? DBNull.Value);
                
                command.AddWithValue("@DataCadastro", produto.DataCadastro);
                command.AddWithValue("@Excluido", produto.Excluido);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    novoId = id;
                }
            }
            return novoId;
        }

        public void Atualizar(Produto produto)
        {
            string query = @"
                UPDATE CorteCor_Produto SET
                    Nome = @Nome, CodigoProprio = @CodigoProprio, IdCategoria = @IdCategoria, 
                    Tags = @Tags, TipoUso = @TipoUso, Arquivado = @Arquivado, Anotacoes = @Anotacoes,
                    PrecoCusto = @PrecoCusto, PrecoVenda = @PrecoVenda, MargemContribuicao = @MargemContribuicao, 
                    ControlarEstoque = @ControlarEstoque, EstoqueAtual = @EstoqueAtual, EstoqueMinimo = @EstoqueMinimo,
                    Origem = @Origem, ReferenciaEAN = @ReferenciaEAN, PesoLiquido = @PesoLiquido, 
                    PesoBruto = @PesoBruto, NCM = @NCM, CEST = @CEST, UnidadeComercial = @UnidadeComercial, 
                    ExcecaoIPI = @ExcecaoIPI, CodBeneficioFiscalUF = @CodBeneficioFiscalUF,
                    UnidadeTributadaDiferente = @UnidadeTributadaDiferente, EANTributada = @EANTributada, 
                    UnidadeTributada = @UnidadeTributada, QuantidadeTributada = @QuantidadeTributada, 
                    IgnorarTribPrecoVenda = @IgnorarTribPrecoVenda, AnotacoesFiscaisNFe = @AnotacoesFiscaisNFe, 
                    GrupoTributarioVinculado = @GrupoTributarioVinculado
                WHERE IdProduto = @IdProduto AND IdSalao = @IdSalao;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", produto.Nome ?? "");
                command.AddWithValue("@CodigoProprio", (object)produto.CodigoProprio ?? DBNull.Value);
                command.AddWithValue("@IdCategoria", (object)produto.IdCategoria ?? DBNull.Value);
                command.AddWithValue("@Tags", (object)produto.Tags ?? DBNull.Value);
                command.AddWithValue("@TipoUso", (object)produto.TipoUso ?? DBNull.Value);
                command.AddWithValue("@Arquivado", produto.Arquivado);
                command.AddWithValue("@Anotacoes", (object)produto.Anotacoes ?? DBNull.Value);
                command.AddWithValue("@PrecoCusto", (object)produto.PrecoCusto ?? DBNull.Value);
                command.AddWithValue("@PrecoVenda", produto.PrecoVenda);
                command.AddWithValue("@MargemContribuicao", (object)produto.MargemContribuicao ?? DBNull.Value);
                command.AddWithValue("@ControlarEstoque", produto.ControlarEstoque);
                command.AddWithValue("@EstoqueAtual", (object)produto.EstoqueAtual ?? DBNull.Value);
                command.AddWithValue("@EstoqueMinimo", (object)produto.EstoqueMinimo ?? DBNull.Value);
                command.AddWithValue("@Origem", (object)produto.Origem ?? DBNull.Value);
                command.AddWithValue("@ReferenciaEAN", (object)produto.ReferenciaEAN ?? DBNull.Value);
                command.AddWithValue("@PesoLiquido", (object)produto.PesoLiquido ?? DBNull.Value);
                command.AddWithValue("@PesoBruto", (object)produto.PesoBruto ?? DBNull.Value);
                command.AddWithValue("@NCM", (object)produto.NCM ?? DBNull.Value);
                command.AddWithValue("@CEST", (object)produto.CEST ?? DBNull.Value);
                command.AddWithValue("@UnidadeComercial", (object)produto.UnidadeComercial ?? DBNull.Value);
                command.AddWithValue("@ExcecaoIPI", (object)produto.ExcecaoIPI ?? DBNull.Value);
                command.AddWithValue("@CodBeneficioFiscalUF", (object)produto.CodBeneficioFiscalUF ?? DBNull.Value);
                command.AddWithValue("@UnidadeTributadaDiferente", produto.UnidadeTributadaDiferente);
                command.AddWithValue("@EANTributada", (object)produto.EANTributada ?? DBNull.Value);
                command.AddWithValue("@UnidadeTributada", (object)produto.UnidadeTributada ?? DBNull.Value);
                command.AddWithValue("@QuantidadeTributada", (object)produto.QuantidadeTributada ?? DBNull.Value);
                command.AddWithValue("@IgnorarTribPrecoVenda", produto.IgnorarTribPrecoVenda);
                command.AddWithValue("@AnotacoesFiscaisNFe", (object)produto.AnotacoesFiscaisNFe ?? DBNull.Value);
                command.AddWithValue("@GrupoTributarioVinculado", (object)produto.GrupoTributarioVinculado ?? DBNull.Value);
                command.AddWithValue("@IdProduto", produto.IdProduto);
                command.AddWithValue("@IdSalao", produto.IdSalao);

                command.ExecuteNonQuery();
            }
        }

        public override void Excluir(int id)
        {
            AtivarDesativar(id, false); // Define Excluido = 1
        }
        
        public void ExcluirPorSalao(int idProduto, int idSalao)
        {
            string query = "UPDATE CorteCor_Produto SET Excluido = 1 WHERE IdProduto = @IdProduto AND IdSalao = @IdSalao";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdProduto", idProduto);
                command.AddWithValue("@IdSalao", idSalao);
                command.ExecuteNonQuery();
            }
        }

        public override void AtivarDesativar(int id, bool ativar)
        {
            string query = "UPDATE CorteCor_Produto SET Excluido = @Excluido WHERE IdProduto = @IdProduto";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Excluido", !ativar);
                command.AddWithValue("@IdProduto", id);
                command.ExecuteNonQuery();
            }
        }

        public override List<Produto> Listar()
        {
            string query = @"SELECT * FROM CorteCor_Produto WHERE Excluido = 0 ORDER BY Nome";
            return LerListaProdutos(query);
        }

        public List<Produto> ListarPorSalao(int idSalao, int? idCategoria = null)
        {
            string query = @"SELECT * FROM CorteCor_Produto WHERE IdSalao = @IdSalao AND (Excluido = 0 OR Excluido IS NULL) AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria) ORDER BY Nome";
            var parametros = new Dictionary<string, object> { 
                { "@IdSalao", idSalao },
                { "@IdCategoria", (object?)idCategoria ?? DBNull.Value }
            };
            return LerListaProdutos(query, parametros);
        }

        public Produto ObterPorId(int idProduto)
        {
            string query = @"SELECT * FROM CorteCor_Produto WHERE IdProduto = @IdProduto AND Excluido = 0";
            var parametros = new Dictionary<string, object> { { "@IdProduto", idProduto } };
            var produtos = LerListaProdutos(query, parametros);
            return produtos.Count > 0 ? produtos[0] : null;
        }

        public Produto? ObterPorIdESalao(int idProduto, int idSalao)
        {
            string query = @"SELECT * FROM CorteCor_Produto WHERE IdProduto = @IdProduto AND IdSalao = @IdSalao AND (Excluido = 0 OR Excluido IS NULL)";
            var parametros = new Dictionary<string, object>
            {
                { "@IdProduto", idProduto },
                { "@IdSalao", idSalao }
            };
            var produtos = LerListaProdutos(query, parametros);
            return produtos.Count > 0 ? produtos[0] : null;
        }

        public PagedResult<Produto> ListarPaginadoPorSalao(int idSalao, int? idCategoria, string? pesquisa, bool incluirArquivados, int pageIndex, int pageSize)
        {
            var result = new PagedResult<Produto>
            {
                PageIndex = pageIndex < 1 ? 1 : pageIndex,
                PageSize = pageSize < 1 ? 10 : pageSize
            };

            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            using (var connection = _dbHandler.GetConnection())
            {
                string countQuery = @"
                SELECT COUNT(*)
                FROM CorteCor_Produto
                WHERE IdSalao = @IdSalao
                  AND (Excluido = 0 OR Excluido IS NULL)
                  AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
                  AND (@IncluirArquivados = 1 OR ISNULL(Arquivado, 0) = 0)
                  AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR CodigoProprio LIKE @Pesquisa OR Tags LIKE @Pesquisa);";

                using (var countCommand = connection.CreateCommand(countQuery))
                {
                    countCommand.AddWithValue("@IdSalao", idSalao);
                    countCommand.AddWithValue("@IdCategoria", (object?)idCategoria ?? DBNull.Value);
                    countCommand.AddWithValue("@IncluirArquivados", incluirArquivados);
                    countCommand.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    result.TotalCount = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                string query = @"
                SELECT *
                FROM CorteCor_Produto
                WHERE IdSalao = @IdSalao
                  AND (Excluido = 0 OR Excluido IS NULL)
                  AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
                  AND (@IncluirArquivados = 1 OR ISNULL(Arquivado, 0) = 0)
                  AND (@Pesquisa IS NULL OR Nome LIKE @Pesquisa OR CodigoProprio LIKE @Pesquisa OR Tags LIKE @Pesquisa)
                ORDER BY Nome
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = connection.CreateCommand(query))
                {
                    command.AddWithValue("@IdSalao", idSalao);
                    command.AddWithValue("@IdCategoria", (object?)idCategoria ?? DBNull.Value);
                    command.AddWithValue("@IncluirArquivados", incluirArquivados);
                    command.AddWithValue("@Pesquisa", (object?)filtro ?? DBNull.Value);
                    command.AddWithValue("@Offset", (result.PageIndex - 1) * result.PageSize);
                    command.AddWithValue("@PageSize", result.PageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Items.Add(MapProduto(reader));
                        }
                    }
                }
            }

            return result;
        }

        private List<Produto> LerListaProdutos(string query, Dictionary<string, object> parametros = null)
        {
            var lista = new List<Produto>();
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                if (parametros != null)
                {
                    foreach (var p in parametros)
                    {
                        command.AddWithValue(p.Key, p.Value);
                    }
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(MapProduto(reader));
                    }
                }
            }
            return lista;
        }

        private static Produto MapProduto(IDataReader reader)
        {
            return new Produto
            {
                IdProduto = reader["IdProduto"] is DBNull ? 0 : Convert.ToInt32(reader["IdProduto"]),
                IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                CodigoProprio = reader["CodigoProprio"] is DBNull ? null : reader["CodigoProprio"].ToString(),
                IdCategoria = reader["IdCategoria"] is DBNull ? (int?)null : Convert.ToInt32(reader["IdCategoria"]),
                Tags = reader["Tags"] is DBNull ? null : reader["Tags"].ToString(),
                TipoUso = reader["TipoUso"] is DBNull ? null : reader["TipoUso"].ToString(),
                Arquivado = reader["Arquivado"] is DBNull ? false : Convert.ToBoolean(reader["Arquivado"]),
                Anotacoes = reader["Anotacoes"] is DBNull ? null : reader["Anotacoes"].ToString(),
                PrecoCusto = reader["PrecoCusto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PrecoCusto"]),
                PrecoVenda = reader["PrecoVenda"] is DBNull ? 0m : Convert.ToDecimal(reader["PrecoVenda"]),
                MargemContribuicao = reader["MargemContribuicao"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["MargemContribuicao"]),
                ControlarEstoque = reader["ControlarEstoque"] is DBNull ? false : Convert.ToBoolean(reader["ControlarEstoque"]),
                EstoqueAtual = reader["EstoqueAtual"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["EstoqueAtual"]),
                EstoqueMinimo = reader["EstoqueMinimo"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["EstoqueMinimo"]),
                Origem = reader["Origem"] is DBNull ? (int?)null : Convert.ToInt32(reader["Origem"]),
                ReferenciaEAN = reader["ReferenciaEAN"] is DBNull ? null : reader["ReferenciaEAN"].ToString(),
                PesoLiquido = reader["PesoLiquido"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PesoLiquido"]),
                PesoBruto = reader["PesoBruto"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["PesoBruto"]),
                NCM = reader["NCM"] is DBNull ? null : reader["NCM"].ToString(),
                CEST = reader["CEST"] is DBNull ? null : reader["CEST"].ToString(),
                UnidadeComercial = reader["UnidadeComercial"] is DBNull ? null : reader["UnidadeComercial"].ToString(),
                ExcecaoIPI = reader["ExcecaoIPI"] is DBNull ? (int?)null : Convert.ToInt32(reader["ExcecaoIPI"]),
                CodBeneficioFiscalUF = reader["CodBeneficioFiscalUF"] is DBNull ? null : reader["CodBeneficioFiscalUF"].ToString(),
                UnidadeTributadaDiferente = reader["UnidadeTributadaDiferente"] is DBNull ? false : Convert.ToBoolean(reader["UnidadeTributadaDiferente"]),
                EANTributada = reader["EANTributada"] is DBNull ? null : reader["EANTributada"].ToString(),
                UnidadeTributada = reader["UnidadeTributada"] is DBNull ? null : reader["UnidadeTributada"].ToString(),
                QuantidadeTributada = reader["QuantidadeTributada"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["QuantidadeTributada"]),
                IgnorarTribPrecoVenda = reader["IgnorarTribPrecoVenda"] is DBNull ? false : Convert.ToBoolean(reader["IgnorarTribPrecoVenda"]),
                AnotacoesFiscaisNFe = reader["AnotacoesFiscaisNFe"] is DBNull ? null : reader["AnotacoesFiscaisNFe"].ToString(),
                GrupoTributarioVinculado = reader["GrupoTributarioVinculado"] is DBNull ? (int?)null : Convert.ToInt32(reader["GrupoTributarioVinculado"]),
                DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"]),
                Excluido = reader["Excluido"] is DBNull ? false : Convert.ToBoolean(reader["Excluido"])
            };
        }
    }

    public class ItemListaServicoHandler : EntityHandler<ItemListaServico>
    {
        private static readonly (string Codigo, string Descricao)[] ItensPadrao = Lc116ItensCatalogo.Itens;

        public ItemListaServicoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }

        public override void Cadastrar(ItemListaServico entity) => throw new NotImplementedException();
        public override void Excluir(int id) => throw new NotImplementedException();
        public override void AtivarDesativar(int id, bool status) => throw new NotImplementedException();
        
        public override List<ItemListaServico> Listar()
        {
            using (var connection = _dbHandler.GetConnection())
            {
                var lista = CarregarItens(connection);
                if (ItensPadrao.All(item => lista.Any(cadastrado =>
                        cadastrado.Codigo == item.Codigo && cadastrado.Descricao == item.Descricao)))
                {
                    return lista;
                }

                SemearItensPadrao(connection);
                return CarregarItens(connection);
            }
        }

        private static List<ItemListaServico> CarregarItens(IDbConnection connection)
        {
            var lista = new List<ItemListaServico>();

            using (var cmd = connection.CreateCommand("SELECT IdItemListaServico, Codigo, Descricao FROM CorteCor_ItemListaServico ORDER BY Codigo"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lista.Add(new ItemListaServico
                    {
                        IdItemListaServico = Convert.ToInt32(reader["IdItemListaServico"]),
                        Codigo = reader["Codigo"].ToString(),
                        Descricao = reader["Descricao"].ToString()
                    });
                }
            }

            return lista;
        }

        private static void SemearItensPadrao(IDbConnection connection)
        {
            foreach (var item in ItensPadrao)
            {
                using (var cmd = connection.CreateCommand(@"
                    INSERT INTO CorteCor_ItemListaServico (Codigo, Descricao)
                    VALUES (@Codigo, @Descricao)
                    ON DUPLICATE KEY UPDATE Descricao = VALUES(Descricao)"))
                {
                    cmd.AddWithValue("@Codigo", item.Codigo);
                    cmd.AddWithValue("@Descricao", item.Descricao);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}


