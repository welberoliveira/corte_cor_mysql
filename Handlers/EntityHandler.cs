using CorteCor.Logs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using CorteCor.Models;
using CorteCor;
using CorteCor.Services;
using System.Data;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

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
                         WHERE IdUsuario = @IdUsuario";
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

        public LoginManager(IDatabaseHandler dbHandler = null)
        {
            _dbHandler = dbHandler ?? new DatabaseHandler();
        }

        public virtual bool AutenticarAdministrador(string email, string senha)
        {
            string query = @"
            SELECT Senha 
            FROM CorteCor_Administrador 
            WHERE Email = @Email 
              AND Status = 'Ativo';";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Email", email);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string senhaHash = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString();
                        return VerificarSenha(senha, senhaHash);
                    }
                }
            }
            return false;
        }

        public bool AutenticarUsuario(string email, string senha)
        {
            string query = @"
            SELECT Senha 
            FROM CorteCor_Usuario 
            WHERE Email = @Email 
              AND Status = 'Ativo';";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Email", email);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string senhaHash = reader["Senha"] is DBNull ? "" : reader["Senha"].ToString();
                        return VerificarSenha(senha, senhaHash);
                    }
                }
            }
            return false;
        }

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
            // Implemente um hashing seguro (por exemplo, BCrypt ou SHA-256).
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(senha));
        }

        private bool VerificarSenha(string senha, string senhaHash)
        {
            return senhaHash == Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(senha));
        }

        public void EnviarEmail(string email, string from, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    client.Host = "smtp-relay.sendinblue.com";
                    client.Port = 587;
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential("welberoliveira3@gmail.com", "4DPStEUZIjh2gHLs");

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(from, "Tonni Corte & Cor");
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
        WHERE IdFuncionario = @IdFuncionario;";

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
            (Nome, Preco, Duracao, IdSalao, CodigoTributacaoMunicipio, Cnae, AliquotaISS)
        VALUES
            (@Nome, @Preco, @Duracao, @IdSalao, @CodigoTributacaoMunicipio, @Cnae, @AliquotaISS);
        SELECT SCOPE_IDENTITY();
";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", servico.Nome ?? "");
                command.AddWithValue("@Preco", servico.Preco);
                command.AddWithValue("@Duracao", servico.Duracao);
                command.AddWithValue("@IdSalao", servico.IdSalao);
                command.AddWithValue("@CodigoTributacaoMunicipio", string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) ? (object)DBNull.Value : servico.CodigoTributacaoMunicipio);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(servico.Cnae) ? (object)DBNull.Value : servico.Cnae);
                command.AddWithValue("@AliquotaISS", servico.AliquotaISS.HasValue ? (object)servico.AliquotaISS.Value : DBNull.Value);

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
        SELECT IdServico, Nome, Preco, Duracao, IdSalao, CodigoTributacaoMunicipio, Cnae, AliquotaISS
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
                        Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                        Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                        AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"])
                    };
                }
            }
        }

        public override List<Servico> Listar()
        {
            string query = @"
        SELECT IdServico, Nome, Preco, Duracao, IdSalao, CodigoTributacaoMunicipio, Cnae, AliquotaISS 
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
                        Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                        CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                        Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                        AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"])
                    });
                }
            }

            return servicos;
        }

        public virtual List<Servico> ListarPorSalao(int idSalao)
        {
            string query = @"
        SELECT IdServico, Nome, Preco, Duracao, IdSalao, CodigoTributacaoMunicipio, Cnae, AliquotaISS
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
        ORDER BY Nome;";

            var servicos = new List<Servico>();

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@IdSalao", idSalao);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        servicos.Add(new Servico
                        {
                            IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                            Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                            Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                            Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                            IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"]),
                            CodigoTributacaoMunicipio = reader["CodigoTributacaoMunicipio"] is DBNull ? "" : reader["CodigoTributacaoMunicipio"].ToString(),
                            Cnae = reader["Cnae"] is DBNull ? "" : reader["Cnae"].ToString(),
                            AliquotaISS = reader["AliquotaISS"] is DBNull ? (decimal?)null : Convert.ToDecimal(reader["AliquotaISS"])
                        });
                    }
                }
            }

            return servicos;
        }

        public void Atualizar(Servico servico)
        {
            string query = @"
        UPDATE CorteCor_Servico
        SET Nome = @Nome,
            Preco = @Preco,
            Duracao = @Duracao,
            IdSalao = @IdSalao,
            CodigoTributacaoMunicipio = @CodigoTributacaoMunicipio,
            Cnae = @Cnae,
            AliquotaISS = @AliquotaISS
        WHERE IdServico = @IdServico;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Nome", servico.Nome ?? "");
                command.AddWithValue("@Preco", servico.Preco);
                command.AddWithValue("@Duracao", servico.Duracao);
                command.AddWithValue("@IdSalao", servico.IdSalao);
                command.AddWithValue("@CodigoTributacaoMunicipio", string.IsNullOrWhiteSpace(servico.CodigoTributacaoMunicipio) ? (object)DBNull.Value : servico.CodigoTributacaoMunicipio);
                command.AddWithValue("@Cnae", string.IsNullOrWhiteSpace(servico.Cnae) ? (object)DBNull.Value : servico.Cnae);
                command.AddWithValue("@AliquotaISS", servico.AliquotaISS.HasValue ? (object)servico.AliquotaISS.Value : DBNull.Value);
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

        // Esta tabela nÃ£o tem Status. Mantive o mÃ©todo para bater com a base EntityHandler<T>.
        public override void AtivarDesativar(int id, bool ativar)
        {
            throw new NotSupportedException("CorteCor_Servico nÃ£o possui campo Status.");
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
        public virtual int CadastrarPessoa(Pessoa pessoa)
        {
            int novoId = 0;

            string query = @"
        INSERT INTO CorteCor_Pessoa
            (Nome, Telefone, Email, DataNascimento, IdSalao, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF)
        VALUES
            (@Nome, @Telefone, @Email, @DataNascimento, @IdSalao, @CpfCnpj, @InscricaoEstadual, @InscricaoMunicipal, @Cep, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @UF);
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
                command.AddWithValue("@Cep", string.IsNullOrWhiteSpace(pessoa.Cep) ? (object)DBNull.Value : pessoa.Cep);
                command.AddWithValue("@Logradouro", string.IsNullOrWhiteSpace(pessoa.Logradouro) ? (object)DBNull.Value : pessoa.Logradouro);
                command.AddWithValue("@Numero", string.IsNullOrWhiteSpace(pessoa.Numero) ? (object)DBNull.Value : pessoa.Numero);
                command.AddWithValue("@Complemento", string.IsNullOrWhiteSpace(pessoa.Complemento) ? (object)DBNull.Value : pessoa.Complemento);
                command.AddWithValue("@Bairro", string.IsNullOrWhiteSpace(pessoa.Bairro) ? (object)DBNull.Value : pessoa.Bairro);
                command.AddWithValue("@Cidade", string.IsNullOrWhiteSpace(pessoa.Cidade) ? (object)DBNull.Value : pessoa.Cidade);
                command.AddWithValue("@UF", string.IsNullOrWhiteSpace(pessoa.UF) ? (object)DBNull.Value : pessoa.UF);

                object result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    novoId = id;
            }

            return novoId;
        }

        public virtual Pessoa ObterPorId(int idPessoa)
        {
            string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF
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
                        UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString()
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
            SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF
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
                                UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString()
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
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF
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
                            UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString()
                        });
                    }
                }
            }

            return pessoas;
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
            UF = @UF
        WHERE IdPessoa = @IdPessoa;";

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
                command.AddWithValue("@Cep", string.IsNullOrWhiteSpace(pessoa.Cep) ? (object)DBNull.Value : pessoa.Cep);
                command.AddWithValue("@Logradouro", string.IsNullOrWhiteSpace(pessoa.Logradouro) ? (object)DBNull.Value : pessoa.Logradouro);
                command.AddWithValue("@Numero", string.IsNullOrWhiteSpace(pessoa.Numero) ? (object)DBNull.Value : pessoa.Numero);
                command.AddWithValue("@Complemento", string.IsNullOrWhiteSpace(pessoa.Complemento) ? (object)DBNull.Value : pessoa.Complemento);
                command.AddWithValue("@Bairro", string.IsNullOrWhiteSpace(pessoa.Bairro) ? (object)DBNull.Value : pessoa.Bairro);
                command.AddWithValue("@Cidade", string.IsNullOrWhiteSpace(pessoa.Cidade) ? (object)DBNull.Value : pessoa.Cidade);
                command.AddWithValue("@UF", string.IsNullOrWhiteSpace(pessoa.UF) ? (object)DBNull.Value : pessoa.UF);

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

        public List<Pessoa> ListarExcluidos(int idSalao)
        {
            string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao, Excluido, CpfCnpj, InscricaoEstadual, InscricaoMunicipal, Cep, Logradouro, Numero, Complemento, Bairro, Cidade, UF
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
                            UF = reader["UF"] is DBNull ? "" : reader["UF"].ToString()
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
        SELECT SCOPE_IDENTITY();";

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

        public virtual void Atualizar(Agendamento agendamento)
        {
            string query = @"
        UPDATE CorteCor_Agendamento
        SET DataHora = @DataHora,
            Status = @Status,
            IdServico = @IdServico,
            IdPessoa = @IdPessoa,
            IdFuncionario = @IdFuncionario
        WHERE IdAgendamento = @IdAgendamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@DataHora", agendamento.DataHora);
                command.AddWithValue("@Status", agendamento.Status ?? "Agendado");
                command.AddWithValue("@IdServico", agendamento.IdServico);
                command.AddWithValue("@IdPessoa", agendamento.IdPessoa);
                command.AddWithValue("@IdFuncionario", agendamento.IdFuncionario);
                command.AddWithValue("@IdAgendamento", agendamento.IdAgendamento);

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

                int count = (int)command.ExecuteScalar();
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

        public virtual void AtualizarStatus(int idAgendamento, string novoStatus)
        {
            string query = "UPDATE CorteCor_Agendamento SET Status = @Status WHERE IdAgendamento = @IdAgendamento;";
            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", novoStatus);
                command.AddWithValue("@IdAgendamento", idAgendamento);
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
        WHERE IdMeioPagamento = @IdMeioPagamento;";

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
            // Desativa pagamentos anteriores para evitar erro no Ã­ndice Ãºnico (UX_CorteCor_Pagamento_Agendamento_Ativo)
            string deactivateQuery = "UPDATE CorteCor_Pagamento SET Ativo = 0 WHERE IdAgendamento = @IdAgendamento AND Ativo = 1;";

            string insertQuery = @"
        INSERT INTO CorteCor_Pagamento
            (IdPagamento, IdAgendamento, Ativo, Status, Valor, Moeda, Descricao, 
             MercadoPagoPreferenceId, MercadoPagoPaymentId, CheckoutUrl, MpStatus, MpStatusDetail, Tipo)
        VALUES
            (@IdPagamento, @IdAgendamento, @Ativo, @Status, @Valor, @Moeda, @Descricao, 
             @MercadoPagoPreferenceId, @MercadoPagoPaymentId, @CheckoutUrl, @MpStatus, @MpStatusDetail, @Tipo);";

            using (var connection = _dbHandler.GetConnection())
            {
                using (var commandDeactivate = connection.CreateCommand())
                {
                    commandDeactivate.CommandText = deactivateQuery;
                    commandDeactivate.AddWithValue("@IdAgendamento", pagamento.IdAgendamento);
                    commandDeactivate.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = insertQuery;
                    command.AddWithValue("@IdPagamento", pagamento.IdPagamento == Guid.Empty ? Guid.NewGuid() : pagamento.IdPagamento);
                    command.AddWithValue("@IdAgendamento", pagamento.IdAgendamento);
                    command.AddWithValue("@Ativo", pagamento.Ativo);
                    command.AddWithValue("@Status", pagamento.Status ?? "Pendente");
                    command.AddWithValue("@Valor", pagamento.Valor);
                    command.AddWithValue("@Moeda", pagamento.Moeda ?? "BRL");
                    command.AddWithValue("@Descricao", (object?)pagamento.Descricao ?? DBNull.Value);
                    command.AddWithValue("@MercadoPagoPreferenceId", (object?)pagamento.MercadoPagoPreferenceId ?? DBNull.Value);
                    command.AddWithValue("@MercadoPagoPaymentId", (object?)pagamento.MercadoPagoPaymentId ?? DBNull.Value);
                    command.AddWithValue("@CheckoutUrl", (object?)pagamento.CheckoutUrl ?? DBNull.Value);
                    command.AddWithValue("@MpStatus", (object?)pagamento.MpStatus ?? DBNull.Value);
                    command.AddWithValue("@MpStatusDetail", (object?)pagamento.MpStatusDetail ?? DBNull.Value);
                    command.AddWithValue("@Tipo", (object?)pagamento.Tipo ?? DBNull.Value);

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

        public void AtualizarPagamento(Pagamento p)
        {
            string query = @"
        UPDATE CorteCor_Pagamento
        SET Status = @Status,
            MercadoPagoPaymentId = @MercadoPagoPaymentId,
            MpStatus = @MpStatus,
            MpStatusDetail = @MpStatusDetail,
            AtualizadoEm = GETUTCDATE(),
            PagoEm = @PagoEm,
            Ativo = @Ativo
        WHERE IdPagamento = @IdPagamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand(query))
            {
                command.AddWithValue("@Status", p.Status);
                command.AddWithValue("@MercadoPagoPaymentId", (object?)p.MercadoPagoPaymentId ?? DBNull.Value);
                command.AddWithValue("@MpStatus", (object?)p.MpStatus ?? DBNull.Value);
                command.AddWithValue("@MpStatusDetail", (object?)p.MpStatusDetail ?? DBNull.Value);
                command.AddWithValue("@PagoEm", (object?)p.PagoEm ?? DBNull.Value);
                command.AddWithValue("@Ativo", p.Ativo);
                command.AddWithValue("@IdPagamento", p.IdPagamento);

                command.ExecuteNonQuery();
            }
        }

        private Pagamento Map(IDataReader reader)
        {
            return new Pagamento
            {
                IdPagamento = (Guid)reader["IdPagamento"],
                IdAgendamento = (int)reader["IdAgendamento"],
                Ativo = (bool)reader["Ativo"],
                Status = reader["Status"].ToString(),
                Valor = (decimal)reader["Valor"],
                Moeda = reader["Moeda"].ToString(),
                Descricao = reader["Descricao"]?.ToString(),
                MercadoPagoPreferenceId = reader["MercadoPagoPreferenceId"]?.ToString(),
                MercadoPagoPaymentId = reader["MercadoPagoPaymentId"]?.ToString(),
                CheckoutUrl = reader["CheckoutUrl"]?.ToString(),
                MpStatus = reader["MpStatus"]?.ToString(),
                MpStatusDetail = reader["MpStatusDetail"]?.ToString(),
                CriadoEm = (DateTime)reader["CriadoEm"],
                AtualizadoEm = reader["AtualizadoEm"] is DBNull ? null : (DateTime?)reader["AtualizadoEm"],
                PagoEm = reader["PagoEm"] is DBNull ? null : (DateTime?)reader["PagoEm"],

                // Legacy mapping safely - Explicit check to avoid any evaluation risk
                IdMeioPagamento = HasColumn(reader, "IdMeioPagamento") ? (reader["IdMeioPagamento"] is DBNull ? 0 : (int)reader["IdMeioPagamento"]) : 0,
                Tipo = HasColumn(reader, "Tipo") ? reader["Tipo"]?.ToString() : null,
                Data = HasColumn(reader, "Data") ? (reader["Data"] is DBNull ? (HasColumn(reader, "CriadoEm") ? (DateTime)reader["CriadoEm"] : DateTime.MinValue) : (DateTime)reader["Data"]) : (HasColumn(reader, "CriadoEm") ? (DateTime)reader["CriadoEm"] : DateTime.MinValue),
                Contos = HasColumn(reader, "Contos") ? reader["Contos"]?.ToString() : null,
                Campos = HasColumn(reader, "Campos") ? reader["Campos"]?.ToString() : null,
                NomeCliente = HasColumn(reader, "NomeCliente") ? reader["NomeCliente"]?.ToString() : null,
                NomeServico = HasColumn(reader, "NomeServico") ? reader["NomeServico"]?.ToString() : null,
                DataAgendamento = HasColumn(reader, "DataAgendamento") ? (reader["DataAgendamento"] is DBNull ? (DateTime?)null : (DateTime)reader["DataAgendamento"]) : null
            };
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

        public void Atualizar(Pagamento p)
        {
            string query = @"
        UPDATE CorteCor_Pagamento
        SET IdAgendamento = @IdAgendamento,
            IdMeioPagamento = @IdMeioPagamento,
            Tipo = @Tipo,
            Valor = @Valor,
            Data = @Data,
            Contos = @Contos,
            Campos = @Campos,
            AtualizadoEm = GETUTCDATE()
        WHERE IdPagamento = @IdPagamento;";

            using (var connection = _dbHandler.GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.AddWithValue("@IdAgendamento", p.IdAgendamento);
                command.AddWithValue("@IdMeioPagamento", p.IdMeioPagamento == 0 ? (object)DBNull.Value : p.IdMeioPagamento);
                command.AddWithValue("@Tipo", p.Tipo ?? "");
                command.AddWithValue("@Valor", p.Valor);
                command.AddWithValue("@Data", p.Data == default ? DateTime.Now : p.Data);
                command.AddWithValue("@Contos", p.Contos ?? "");
                command.AddWithValue("@Campos", p.Campos ?? "");
                command.AddWithValue("@IdPagamento", p.IdPagamento);

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
            sb.Append("WHERE (P.Ativo = 1 OR P.Ativo IS NULL) ");

            if (filtro.DataInicio.HasValue) sb.Append("AND P.CriadoEm >= @DataInicio ");
            if (filtro.DataFim.HasValue) sb.Append("AND P.CriadoEm <= @DataFim ");
            if (!string.IsNullOrEmpty(filtro.Status)) sb.Append("AND P.Status = @Status ");
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) sb.Append("AND Pe.Nome LIKE @NomeCliente ");
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
                string dataQuery = "SELECT P.*, Pe.Nome as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento " +
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

        private void AddFiltroParams(IDbCommand cmd, PagamentoFiltroDTO filtro)
        {
            if (filtro.DataInicio.HasValue) cmd.AddWithValue("@DataInicio", filtro.DataInicio.Value);
            if (filtro.DataFim.HasValue) cmd.AddWithValue("@DataFim", filtro.DataFim.Value);
            if (!string.IsNullOrEmpty(filtro.Status)) cmd.AddWithValue("@Status", filtro.Status);
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) cmd.AddWithValue("@NomeCliente", "%" + filtro.NomeCliente + "%");
            if (filtro.DataAgendamento.HasValue) cmd.AddWithValue("@DataAgendamento", filtro.DataAgendamento.Value);
        }

        public (decimal totalValor, int totalContagem) ObterResumo(PagamentoFiltroDTO filtro)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_Pagamento P ");
            sb.Append("LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento ");
            sb.Append("LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa ");
            sb.Append("WHERE (P.Ativo = 1 OR P.Ativo IS NULL) ");

            if (filtro.DataInicio.HasValue) sb.Append("AND P.CriadoEm >= @DataInicio ");
            if (filtro.DataFim.HasValue) sb.Append("AND P.CriadoEm <= @DataFim ");
            if (!string.IsNullOrEmpty(filtro.Status)) sb.Append("AND P.Status = @Status ");
            if (!string.IsNullOrEmpty(filtro.NomeCliente)) sb.Append("AND Pe.Nome LIKE @NomeCliente ");
            if (filtro.DataAgendamento.HasValue) sb.Append("AND CAST(A.DataHora AS DATE) = CAST(@DataAgendamento AS DATE) ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                using (var cmd = connection.CreateCommand("SELECT ISNULL(SUM(P.Valor), 0), COUNT(*) " + baseQuery))
                {
                    AddFiltroParams(cmd, filtro);
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
            SELECT P.*, Pe.Nome as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento
            FROM CorteCor_Pagamento P
            LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento
            LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa
            LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico
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
        public async Task<bool> SincronizarPagamento(Guid idPagamento, MercadoPagoService mpService)
        {
            var pagamento = ObterPorId(idPagamento);
            if (pagamento == null || string.IsNullOrEmpty(pagamento.MercadoPagoPaymentId)) return false;

            var mpPayment = await mpService.GetPaymentDetailsAsync(pagamento.MercadoPagoPaymentId);
            if (mpPayment == null) return false;

            pagamento.MpStatus = mpPayment.Status;
            pagamento.MpStatusDetail = mpPayment.StatusDetail;

            // LÃ³gica de atualizaÃ§Ã£o de status baseada na resposta da API
            if (mpPayment.Status == "approved")
            {
                pagamento.Status = "Pago";
                pagamento.PagoEm = mpPayment.DateApproved ?? DateTime.UtcNow;

                // Atualiza tambÃ©m o agendamento
                var agHandler = new AgendamentoHandler();
                agHandler.AtualizarStatus(pagamento.IdAgendamento, "Pago");
            }
            else if (mpPayment.Status == "cancelled" || mpPayment.Status == "rejected")
            {
                // Se foi rejeitado ou cancelado, marcamos este pagamento como inativo/cancelado
                // mas NÃƒO cancelamos o agendamento, permitindo nova tentativa.
                pagamento.Status = "Cancelado";
                pagamento.Ativo = false;
            }

            AtualizarPagamento(pagamento);
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
                UPDATE A
                SET Status = @StatusAg
                FROM CorteCor_Agendamento A
                INNER JOIN CorteCor_Pagamento P ON A.IdAgendamento = P.IdAgendamento
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
                IdModelo = Convert.ToInt32(reader["IdModelo"]),
                IdSalao = Convert.ToInt32(reader["IdSalao"]),
                TipoEvento = reader["TipoEvento"].ToString(),
                Assunto = reader["Assunto"].ToString(),
                CorpoHTML = reader["CorpoHTML"].ToString(),
                Ativo = Convert.ToBoolean(reader["Ativo"]),
                DataAtualizacao = Convert.ToDateTime(reader["DataAtualizacao"])
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
                      WHERE IdConfig = @IdConfig";
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
                if (config.IdConfig > 0)
                    command.AddWithValue("@IdConfig", config.IdConfig);
                else
                    command.AddWithValue("@IdSalao", config.IdSalao);

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



        public PagedResult<LogEnvioEmail> ListarLogsEnvio(DateTime? inicio, DateTime? fim, string destinatario, string assunto, string status, int page = 1, int pageSize = 10, string tipoLembrete = null)
        {
            var result = new PagedResult<LogEnvioEmail>
            {
                PageIndex = page,
                PageSize = pageSize
            };

            var sb = new System.Text.StringBuilder();
            sb.Append("FROM CorteCor_LogEnvioEmail WHERE 1=1 ");

            if (inicio.HasValue) sb.Append("AND DataEnvio >= @Inicio ");
            if (fim.HasValue) sb.Append("AND DataEnvio <= @Fim ");
            if (!string.IsNullOrEmpty(destinatario)) sb.Append("AND Destinatario LIKE @Destinatario ");
            if (!string.IsNullOrEmpty(assunto)) sb.Append("AND Assunto LIKE @Assunto ");
            if (!string.IsNullOrEmpty(status)) sb.Append("AND Status = @Status ");
            if (!string.IsNullOrEmpty(tipoLembrete)) sb.Append("AND TipoLembrete = @TipoLembrete ");

            string baseQuery = sb.ToString();

            using (var connection = _dbHandler.GetConnection())
            {
                // Count
                using (var countCmd = connection.CreateCommand("SELECT COUNT(*) " + baseQuery))
                {
                    AddLogParams(countCmd, inicio, fim, destinatario, assunto, status, tipoLembrete);
                    result.TotalCount = Convert.ToInt32(countCmd.ExecuteScalar());
                }

                // Data
                string dataQuery = "SELECT * " +
                                   baseQuery +
                                   "ORDER BY DataEnvio DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = connection.CreateCommand(dataQuery))
                {
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




}

