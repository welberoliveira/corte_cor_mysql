using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using static CorteCor.Models;
using CorteCor;
using System.Data;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

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
    public UsuarioHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) {
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
            SELECT IdSalao, Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro
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
                DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
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
            (Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro, Observacao)
        VALUES 
            (@Nome, @Responsavel, @Email, @Telefone, @Endereco, @CNPJ, @Status, @DataCadastro, @Observacao);
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
        SELECT IdSalao, Nome, Responsavel, Email, Telefone, Endereco, CNPJ, Status, DataCadastro, Observacao
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
                        Observacao = reader["Observacao"] is DBNull ? null : reader["Observacao"].ToString()
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
            Observacao = @Observacao
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
            (Nome, Preco, Duracao, IdSalao)
        VALUES
            (@Nome, @Preco, @Duracao, @IdSalao);
        SELECT SCOPE_IDENTITY();
";

        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand(query))
        {
            command.AddWithValue("@Nome", servico.Nome ?? "");
            command.AddWithValue("@Preco", servico.Preco);
            command.AddWithValue("@Duracao", servico.Duracao);
            command.AddWithValue("@IdSalao", servico.IdSalao);

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
        SELECT IdServico, Nome, Preco, Duracao, IdSalao
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
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                };
            }
        }
    }

    public override List<Servico> Listar()
    {
        string query = @"
        SELECT IdServico, Nome, Preco, Duracao, IdSalao
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
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                });
            }
        }

        return servicos;
    }

    public virtual List<Servico> ListarPorSalao(int idSalao)
    {
        string query = @"
        SELECT IdServico, Nome, Preco, Duracao, IdSalao
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
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
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
            IdSalao = @IdSalao
        WHERE IdServico = @IdServico;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand(query))
        {
            command.AddWithValue("@Nome", servico.Nome ?? "");
            command.AddWithValue("@Preco", servico.Preco);
            command.AddWithValue("@Duracao", servico.Duracao);
            command.AddWithValue("@IdSalao", servico.IdSalao);
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
            (Nome, Telefone, Email, DataNascimento, IdSalao)
        VALUES
            (@Nome, @Telefone, @Email, @DataNascimento, @IdSalao);
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

            object result = command.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
                novoId = id;
        }

        return novoId;
    }

    public virtual Pessoa ObterPorId(int idPessoa)
    {
        string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao
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
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                };
            }
        }
    }

    public override List<Pessoa> Listar()
    {
        string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao
        FROM CorteCor_Pessoa
        ORDER BY Nome;";

        var pessoas = new List<Pessoa>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand(query))
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
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                });
            }
        }

        return pessoas;
    }

    public virtual List<Pessoa> ListarPorSalao(int idSalao)
    {
        string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
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
                        IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
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
            IdSalao = @IdSalao
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
            command.AddWithValue("@IdPessoa", pessoa.IdPessoa);

            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Pessoa WHERE IdPessoa = @IdPessoa";

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
                novoId = id;
        }

        return novoId;
    }

    public virtual Agendamento ObterPorId(int idAgendamento)
    {
        string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario
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
                    IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"])
                };
            }
        }
    }

    public override List<Agendamento> Listar()
    {
        string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario
        FROM CorteCor_Agendamento
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
                    IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"])
                });
            }
        }

        return agendamentos;
    }

    public virtual List<Agendamento> ListarPorIntervalo(int idSalao, DateTime inicio, DateTime fim)
    {
        string query = @"
        SELECT a.IdAgendamento, a.DataHora, a.Status, a.IdServico, a.IdPessoa, a.IdFuncionario
        FROM CorteCor_Agendamento a
        INNER JOIN CorteCor_Servico s ON s.IdServico = a.IdServico
        WHERE s.IdSalao = @IdSalao
          AND a.DataHora >= @Inicio
          AND a.DataHora < @Fim
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
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"])
                    });
                }
            }
        }

        return agendamentos;
    }


    public List<Agendamento> ListarPorFuncionario(int idFuncionario, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario
        FROM CorteCor_Agendamento
        WHERE IdFuncionario = @IdFuncionario
          AND (@DataInicio IS NULL OR DataHora >= @DataInicio)
          AND (@DataFim    IS NULL OR DataHora <  @DataFim)
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
                        IdFuncionario = reader["IdFuncionario"] is DBNull ? 0 : Convert.ToInt32(reader["IdFuncionario"])
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
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Agendamento WHERE IdAgendamento = @IdAgendamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand(query))
        {
            command.AddWithValue("@IdAgendamento", id);
            command.ExecuteNonQuery();
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
        }
    }

    public override void Cadastrar(Agendamento entity)
    {
        throw new NotImplementedException();
    }
}


public class MeioPagamentoHandler : EntityHandler<MeioPagamento>
{
    public MeioPagamentoHandler(IDatabaseHandler dbHandler = null) : base(dbHandler) { }
    public int CadastrarMeioPagamento(MeioPagamento meio)
    {
        int novoId = 0;

        string query = @"
        INSERT INTO CorteCor_MeioPagamento
            (Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
             TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo,
             IdSalao, DataCadastro)
        VALUES
            (@Nome, @Tipo, @Gateway, @PermiteParcelamento, @ParcelasMax,
             @TaxaPercentual, @TaxaFixa, @PrazoRecebimentoDias, @Ativo,
             @IdSalao, @DataCadastro);
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
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro
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
                    DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                };
            }
        }
    }

    public override List<MeioPagamento> Listar()
    {
        string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro
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
                    DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                });
            }
        }

        return itens;
    }

    public List<MeioPagamento> ListarPorSalao(int idSalao, bool? somenteAtivos = true)
    {
        string query = @"
        SELECT IdMeioPagamento, Nome, Tipo, Gateway, PermiteParcelamento, ParcelasMax,
               TaxaPercentual, TaxaFixa, PrazoRecebimentoDias, Ativo, IdSalao, DataCadastro
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
                        DataCadastro = reader["DataCadastro"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(reader["DataCadastro"])
                    });
                }
            }
        }

        return itens;
    }

    public void Atualizar(MeioPagamento meio)
    {
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
            DataCadastro = @DataCadastro
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

            command.AddWithValue("@Ativo", meio.Ativo);

            command.AddWithValue("@IdSalao", meio.IdSalao);
            command.AddWithValue("@DataCadastro", meio.DataCadastro == default ? DateTime.Now : meio.DataCadastro);

            command.AddWithValue("@IdMeioPagamento", meio.IdMeioPagamento);

            command.ExecuteNonQuery();
        }
    }

    public override void AtivarDesativar(int id, bool ativar)
    {
        string query = "UPDATE CorteCor_MeioPagamento SET Ativo = @Ativo WHERE IdMeioPagamento = @IdMeioPagamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand(query))
        {
            command.AddWithValue("@Ativo", ativar);
            command.AddWithValue("@IdMeioPagamento", id);
            command.ExecuteNonQuery();
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
    public PagamentoHandler(IDatabaseHandler dbHandler = null)
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
        return Listar(new PagamentoFiltroDTO());
    }

    public List<Pagamento> Listar(PagamentoFiltroDTO filtro)
    {
        // Base query with possible joins if Name filter is needed
        // If Filtering by Client Name (NomeCliente), we need to join with Agendamento and Pessoa
        
        var sb = new System.Text.StringBuilder();
        sb.Append("SELECT P.*, Pe.Nome as NomeCliente, S.Nome as NomeServico, A.DataHora as DataAgendamento ");
        sb.Append("FROM CorteCor_Pagamento P ");
        sb.Append("LEFT JOIN CorteCor_Agendamento A ON P.IdAgendamento = A.IdAgendamento ");
        sb.Append("LEFT JOIN CorteCor_Pessoa Pe ON A.IdPessoa = Pe.IdPessoa ");
        sb.Append("LEFT JOIN CorteCor_Servico S ON A.IdServico = S.IdServico ");
        
        sb.Append("WHERE 1=1 ");
        sb.Append("AND P.Ativo = 1 ");

        if (filtro.DataInicio.HasValue)
        {
            sb.Append("AND P.CriadoEm >= @DataInicio ");
        }

        if (filtro.DataFim.HasValue)
        {
            sb.Append("AND P.CriadoEm <= @DataFim ");
        }

        if (!string.IsNullOrEmpty(filtro.Status))
        {
            sb.Append("AND P.Status = @Status ");
        }

        if (!string.IsNullOrEmpty(filtro.NomeCliente))
        {
            sb.Append("AND Pe.Nome LIKE @NomeCliente ");
        }

        if (filtro.DataAgendamento.HasValue)
        {
            sb.Append("AND CAST(A.DataHora AS DATE) = CAST(@DataAgendamento AS DATE) ");
        }

        sb.Append("ORDER BY A.DataHora DESC, P.CriadoEm DESC;");

        var list = new List<Pagamento>();
        
        using (var connection = _dbHandler.GetConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sb.ToString();
            
            if (filtro.DataInicio.HasValue)
                command.AddWithValue("@DataInicio", filtro.DataInicio.Value);
                
            if (filtro.DataFim.HasValue)
                command.AddWithValue("@DataFim", filtro.DataFim.Value);
                
            if (!string.IsNullOrEmpty(filtro.Status))
                command.AddWithValue("@Status", filtro.Status);
                
            if (!string.IsNullOrEmpty(filtro.NomeCliente))
                command.AddWithValue("@NomeCliente", "%" + filtro.NomeCliente + "%");

            if (filtro.DataAgendamento.HasValue)
                command.AddWithValue("@DataAgendamento", filtro.DataAgendamento.Value);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
            }
        }
        return list;
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
            command.AddWithValue("@Id", id);
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
                novoStatusAgendamento = "Confirmado"; 
                Console.WriteLine($"[Sync] Pagamento {idPagamento} aprovado. Agendamento vinculado atualizado para Confirmado.");
            }
            else if (status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase) || status.Equals("rejected", StringComparison.OrdinalIgnoreCase) || status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            {
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




