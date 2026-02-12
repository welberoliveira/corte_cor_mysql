using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using static CorteCor.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

public abstract class EntityHandler<T>
{
    protected readonly DatabaseHandler _dbHandler = new DatabaseHandler();

    public abstract void Cadastrar(T entity);
    public abstract void AtivarDesativar(int id, bool ativar);
    public abstract List<T> Listar();
    public abstract void Excluir(int id);
}

public class UsuarioHandler : EntityHandler<Usuario>
{
    public UsuarioHandler()
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", Usuario.Nome);
            command.Parameters.AddWithValue("@Sobrenome", Usuario.Sobrenome);
            command.Parameters.AddWithValue("@CPF", Usuario.CPF);
            command.Parameters.AddWithValue("@Email", Usuario.Email);
            command.Parameters.AddWithValue("@Telefone", Usuario.Telefone);
            // Se DataEntrada for nullable ou MinValue, tratar:
            if (Usuario.DataEntrada == DateTime.MinValue)
                command.Parameters.AddWithValue("@DataEntrada", DBNull.Value);
            else
                command.Parameters.AddWithValue("@DataEntrada", Usuario.DataEntrada);
            command.Parameters.AddWithValue("@Status", Usuario.Status);
            command.Parameters.AddWithValue("@Senha", Usuario.Senha);
            command.Parameters.AddWithValue("@IdSalao", Usuario.IdSalao);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@IdUsuario", id);
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
        using (var command = new SqlCommand(query, connection))
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdUsuario", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", Usuario.Nome);
            command.Parameters.AddWithValue("@Sobrenome", Usuario.Sobrenome);
            command.Parameters.AddWithValue("@CPF", Usuario.CPF);
            command.Parameters.AddWithValue("@Email", Usuario.Email);
            command.Parameters.AddWithValue("@Telefone", Usuario.Telefone);
            command.Parameters.AddWithValue("@DataEntrada", Usuario.DataEntrada);
            command.Parameters.AddWithValue("@IdUsuario", Usuario.IdUsuario);
            command.Parameters.AddWithValue("@IdSalao", Usuario.IdSalao);
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
    private readonly DatabaseHandler _dbHandler = new DatabaseHandler();

    public LoginManager()
    {
    }

    public bool AutenticarAdministrador(string email, string senha)
    {
        string query = @"
            SELECT Senha 
            FROM CorteCor_Administrador 
            WHERE Email = @Email 
              AND Status = 'Ativo';";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Email", email);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Email", email);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", nome);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Senha", senhaHash);
            command.Parameters.AddWithValue("@Perfil", perfil);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Senha", senhaHash);
            command.Parameters.AddWithValue("@Email", email);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdUsuario", idUsuario);
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
    private readonly DatabaseHandler _dbHandler;

    public Salaoervice(IMemoryCache cache)
    {
        _cache = cache;
        _dbHandler = new DatabaseHandler();
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
        using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@IdSalao", IdSalao);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", Salao.Nome);
            command.Parameters.AddWithValue("@Responsavel", Salao.Responsavel);
            command.Parameters.AddWithValue("@Email", Salao.Email);
            command.Parameters.AddWithValue("@Telefone", Salao.Telefone);
            command.Parameters.AddWithValue("@Endereco", Salao.Endereco);
            command.Parameters.AddWithValue("@CNPJ", Salao.CNPJ);
            command.Parameters.AddWithValue("@Status", Salao.Status);
            command.Parameters.AddWithValue("@DataCadastro", Salao.DataCadastro.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Observacao", (object)Salao.Observacao ?? DBNull.Value);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@IdSalao", id);
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
        using (var command = new SqlCommand(query, connection))
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", Salao.Nome);
            command.Parameters.AddWithValue("@Responsavel", Salao.Responsavel);
            command.Parameters.AddWithValue("@Email", Salao.Email);
            command.Parameters.AddWithValue("@Telefone", Salao.Telefone);
            command.Parameters.AddWithValue("@Endereco", Salao.Endereco);
            command.Parameters.AddWithValue("@CNPJ", Salao.CNPJ);
            command.Parameters.AddWithValue("@Status", Salao.Status);
            command.Parameters.AddWithValue("@DataCadastro", Salao.DataCadastro);
            command.Parameters.AddWithValue("@Observacao", (object)Salao.Observacao ?? DBNull.Value);
            command.Parameters.AddWithValue("@IdSalao", Salao.IdSalao);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", admin.Nome);
            command.Parameters.AddWithValue("@Email", admin.Email);
            command.Parameters.AddWithValue("@Senha", admin.Senha);
            command.Parameters.AddWithValue("@Perfil", admin.Perfil);
            command.Parameters.AddWithValue("@Status", admin.Status);
            command.Parameters.AddWithValue("@DataCriacao", admin.DataCriacao);
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
        using (var command = new SqlCommand(query, connection))
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@IdUsuario", id);
            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Administrador WHERE IdUsuario = @IdUsuario";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdUsuario", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", admin.Nome);
            command.Parameters.AddWithValue("@Email", admin.Email);
            command.Parameters.AddWithValue("@Senha", admin.Senha);
            command.Parameters.AddWithValue("@Perfil", admin.Perfil);
            command.Parameters.AddWithValue("@Status", admin.Status);
            command.Parameters.AddWithValue("@DataCriacao", admin.DataCriacao);
            command.Parameters.AddWithValue("@IdUsuario", admin.IdUsuario);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@FichaID", pessoa.FichaID);
            command.Parameters.AddWithValue("@Nome", (object)pessoa.Nome ?? DBNull.Value);
            command.Parameters.AddWithValue("@Filiacao", (object)pessoa.Filiacao ?? DBNull.Value);
            command.Parameters.AddWithValue("@RG", (object)pessoa.RG ?? DBNull.Value);
            command.Parameters.AddWithValue("@CPF", (object)pessoa.CPF ?? DBNull.Value);
            command.Parameters.AddWithValue("@DataNascimento", (object)pessoa.DataNascimento ?? DBNull.Value);
            command.Parameters.AddWithValue("@Nacionalidade", (object)pessoa.Nacionalidade ?? DBNull.Value);
            command.Parameters.AddWithValue("@NIS", (object)pessoa.NIS ?? DBNull.Value);
            command.Parameters.AddWithValue("@EstadoCivil", (object)pessoa.EstadoCivil ?? DBNull.Value);
            command.Parameters.AddWithValue("@RegimeCasamento", (object)pessoa.RegimeCasamento ?? DBNull.Value);
            command.Parameters.AddWithValue("@SituacaoProfissional", (object)pessoa.SituacaoProfissional ?? DBNull.Value);
            command.Parameters.AddWithValue("@Profissao", (object)pessoa.Profissao ?? DBNull.Value);
            command.Parameters.AddWithValue("@GrauInstrucao", (object)pessoa.GrauInstrucao ?? DBNull.Value);
            command.Parameters.AddWithValue("@Iletrado", (object)pessoa.Iletrado ?? DBNull.Value);
            command.Parameters.AddWithValue("@Empresa", (object)pessoa.Empresa ?? DBNull.Value);
            command.Parameters.AddWithValue("@CarteiraAssinada", (object)pessoa.CarteiraAssinada ?? DBNull.Value);
            command.Parameters.AddWithValue("@RendaMensal", (object)pessoa.RendaMensal ?? DBNull.Value);
            command.Parameters.AddWithValue("@Endereco", (object)pessoa.Endereco ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quadra", (object)pessoa.Quadra ?? DBNull.Value);
            command.Parameters.AddWithValue("@PontoReferencia", (object)pessoa.PontoReferencia ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bairro", (object)pessoa.Bairro ?? DBNull.Value);
            command.Parameters.AddWithValue("@Lote", (object)pessoa.Lote ?? DBNull.Value);
            command.Parameters.AddWithValue("@MunicipioResidencia", pessoa.MunicipioResidencia);
            command.Parameters.AddWithValue("@Telefone", (object)pessoa.Telefone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Celular", (object)pessoa.Celular ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeNome", (object)pessoa.ConjugeNome ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeFiliacao", (object)pessoa.ConjugeFiliacao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeRG", (object)pessoa.ConjugeRG ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeCPF", (object)pessoa.ConjugeCPF ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeIdade", (object)pessoa.ConjugeIdade ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeNacionalidade", (object)pessoa.ConjugeNacionalidade ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeSituacaoProfissional", (object)pessoa.ConjugeSituacaoProfissional ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeProfissao", (object)pessoa.ConjugeProfissao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeGrauInstrucao", (object)pessoa.ConjugeGrauInstrucao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeIletrado", (object)pessoa.ConjugeIletrado ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeEmpresa", (object)pessoa.ConjugeEmpresa ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeCarteiraAssinada", (object)pessoa.ConjugeCarteiraAssinada ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeRendaMensal", (object)pessoa.ConjugeRendaMensal ?? DBNull.Value);
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
        using (var command = new SqlCommand(query, connection))
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PessoaID", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", (object)pessoa.Nome ?? DBNull.Value);
            command.Parameters.AddWithValue("@Filiacao", (object)pessoa.Filiacao ?? DBNull.Value);
            command.Parameters.AddWithValue("@RG", (object)pessoa.RG ?? DBNull.Value);
            command.Parameters.AddWithValue("@CPF", (object)pessoa.CPF ?? DBNull.Value);
            command.Parameters.AddWithValue("@DataNascimento", (object)pessoa.DataNascimento ?? DBNull.Value);
            command.Parameters.AddWithValue("@Nacionalidade", (object)pessoa.Nacionalidade ?? DBNull.Value);
            command.Parameters.AddWithValue("@NIS", (object)pessoa.NIS ?? DBNull.Value);
            command.Parameters.AddWithValue("@EstadoCivil", (object)pessoa.EstadoCivil ?? DBNull.Value);
            command.Parameters.AddWithValue("@RegimeCasamento", (object)pessoa.RegimeCasamento ?? DBNull.Value);
            command.Parameters.AddWithValue("@SituacaoProfissional", (object)pessoa.SituacaoProfissional ?? DBNull.Value);
            command.Parameters.AddWithValue("@Profissao", (object)pessoa.Profissao ?? DBNull.Value);
            command.Parameters.AddWithValue("@GrauInstrucao", (object)pessoa.GrauInstrucao ?? DBNull.Value);
            command.Parameters.AddWithValue("@Iletrado", (object)pessoa.Iletrado ?? DBNull.Value);
            command.Parameters.AddWithValue("@Empresa", (object)pessoa.Empresa ?? DBNull.Value);
            command.Parameters.AddWithValue("@CarteiraAssinada", (object)pessoa.CarteiraAssinada ?? DBNull.Value);
            command.Parameters.AddWithValue("@RendaMensal", (object)pessoa.RendaMensal ?? DBNull.Value);
            command.Parameters.AddWithValue("@Endereco", (object)pessoa.Endereco ?? DBNull.Value);
            command.Parameters.AddWithValue("@Quadra", (object)pessoa.Quadra ?? DBNull.Value);
            command.Parameters.AddWithValue("@PontoReferencia", (object)pessoa.PontoReferencia ?? DBNull.Value);
            command.Parameters.AddWithValue("@Bairro", (object)pessoa.Bairro ?? DBNull.Value);
            command.Parameters.AddWithValue("@Lote", (object)pessoa.Lote ?? DBNull.Value);
            command.Parameters.AddWithValue("@MunicipioResidencia", (object)pessoa.MunicipioResidencia ?? DBNull.Value);
            command.Parameters.AddWithValue("@Telefone", (object)pessoa.Telefone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Celular", (object)pessoa.Celular ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeNome", (object)pessoa.ConjugeNome ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeFiliacao", (object)pessoa.ConjugeFiliacao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeRG", (object)pessoa.ConjugeRG ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeCPF", (object)pessoa.ConjugeCPF ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeIdade", (object)pessoa.ConjugeIdade ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeNacionalidade", (object)pessoa.ConjugeNacionalidade ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeSituacaoProfissional", (object)pessoa.ConjugeSituacaoProfissional ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeProfissao", (object)pessoa.ConjugeProfissao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeGrauInstrucao", (object)pessoa.ConjugeGrauInstrucao ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeIletrado", (object)pessoa.ConjugeIletrado ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeEmpresa", (object)pessoa.ConjugeEmpresa ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeCarteiraAssinada", (object)pessoa.ConjugeCarteiraAssinada ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConjugeRendaMensal", (object)pessoa.ConjugeRendaMensal ?? DBNull.Value);
            command.Parameters.AddWithValue("@FichaID", pessoa.FichaID);
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
    public int CadastrarFuncionario(Funcionario funcionario)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", funcionario.Nome ?? "");

            command.Parameters.AddWithValue("@seg", funcionario.seg);
            command.Parameters.AddWithValue("@seg_ini", (object?)funcionario.seg_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@seg_fim", (object?)funcionario.seg_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@ter", funcionario.ter);
            command.Parameters.AddWithValue("@ter_ini", (object?)funcionario.ter_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@ter_fim", (object?)funcionario.ter_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@qua", funcionario.qua);
            command.Parameters.AddWithValue("@qua_ini", (object?)funcionario.qua_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@qua_fim", (object?)funcionario.qua_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@qui", funcionario.qui);
            command.Parameters.AddWithValue("@qui_ini", (object?)funcionario.qui_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@qui_fim", (object?)funcionario.qui_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@sex", funcionario.sex);
            command.Parameters.AddWithValue("@sex_ini", (object?)funcionario.sex_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@sex_fim", (object?)funcionario.sex_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@sab", funcionario.sab);
            command.Parameters.AddWithValue("@sab_ini", (object?)funcionario.sab_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@sab_fim", (object?)funcionario.sab_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@dom", funcionario.dom);
            command.Parameters.AddWithValue("@dom_ini", (object?)funcionario.dom_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@dom_fim", (object?)funcionario.dom_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@IdSalao", funcionario.IdSalao);

            object result = command.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
                novoId = id;
        }

        return novoId;
    }

    public Funcionario ObterPorId(int idFuncionario)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);

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

    public List<Funcionario> ListarPorSalao(int idSalao)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", idSalao);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", funcionario.Nome ?? "");

            command.Parameters.AddWithValue("@seg", funcionario.seg);
            command.Parameters.AddWithValue("@seg_ini", (object?)funcionario.seg_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@seg_fim", (object?)funcionario.seg_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@ter", funcionario.ter);
            command.Parameters.AddWithValue("@ter_ini", (object?)funcionario.ter_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@ter_fim", (object?)funcionario.ter_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@qua", funcionario.qua);
            command.Parameters.AddWithValue("@qua_ini", (object?)funcionario.qua_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@qua_fim", (object?)funcionario.qua_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@qui", funcionario.qui);
            command.Parameters.AddWithValue("@qui_ini", (object?)funcionario.qui_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@qui_fim", (object?)funcionario.qui_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@sex", funcionario.sex);
            command.Parameters.AddWithValue("@sex_ini", (object?)funcionario.sex_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@sex_fim", (object?)funcionario.sex_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@sab", funcionario.sab);
            command.Parameters.AddWithValue("@sab_ini", (object?)funcionario.sab_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@sab_fim", (object?)funcionario.sab_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@dom", funcionario.dom);
            command.Parameters.AddWithValue("@dom_ini", (object?)funcionario.dom_ini ?? DBNull.Value);
            command.Parameters.AddWithValue("@dom_fim", (object?)funcionario.dom_fim ?? DBNull.Value);

            command.Parameters.AddWithValue("@IdSalao", funcionario.IdSalao);
            command.Parameters.AddWithValue("@IdFuncionario", funcionario.IdFuncionario);

            command.ExecuteNonQuery();
        }
    }

    public override List<Funcionario> Listar() => ListarPorSalao(0);

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Funcionario WHERE IdFuncionario = @IdFuncionario";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", id);
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
    public int CadastrarServico(Servico servico)
    {
        int novoId = 0;

        string query = @"
        INSERT INTO CorteCor_Servico
            (Nome, Preco, Duracao, Cor, IdSalao)
        VALUES
            (@Nome, @Preco, @Duracao, @Cor, @IdSalao);
        SELECT SCOPE_IDENTITY();
";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", servico.Nome ?? "");
            command.Parameters.AddWithValue("@Preco", servico.Preco);
            command.Parameters.AddWithValue("@Cor", servico.Cor ?? "");
            command.Parameters.AddWithValue("@Duracao", servico.Duracao);
            command.Parameters.AddWithValue("@IdSalao", servico.IdSalao);

            object result = command.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
            {
                novoId = id;
            }
        }

        return novoId;
    }

    public Servico ObterPorId(int idServico)
    {
        string query = @"
        SELECT IdServico, Nome, Preco, Duracao, Cor, IdSalao
        FROM CorteCor_Servico
        WHERE IdServico = @IdServico;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdServico", idServico);

            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read()) return null;

                return new Servico
                {
                    IdServico = reader["IdServico"] is DBNull ? 0 : Convert.ToInt32(reader["IdServico"]),
                    Nome = reader["Nome"] is DBNull ? "" : reader["Nome"].ToString(),
                    Preco = reader["Preco"] is DBNull ? 0m : Convert.ToDecimal(reader["Preco"]),
                    Duracao = reader["Duracao"] is DBNull ? TimeSpan.Zero : (TimeSpan)reader["Duracao"],
                    Cor = reader["Cor"] is DBNull ? "" : reader["Cor"].ToString(),
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                };
            }
        }
    }

    public override List<Servico> Listar()
    {
        string query = @"
        SELECT IdServico, Nome, Preco, Duracao, Cor, IdSalao
        FROM CorteCor_Servico
        ORDER BY Nome;";

        var servicos = new List<Servico>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
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
                    Cor = reader["Cor"] is DBNull ? "" : reader["Cor"].ToString(),
                    IdSalao = reader["IdSalao"] is DBNull ? 0 : Convert.ToInt32(reader["IdSalao"])
                });
            }
        }

        return servicos;
    }

    public List<Servico> ListarPorSalao(int idSalao)
    {
        string query = @"
        SELECT IdServico, Nome, Preco, Duracao, Cor, IdSalao
        FROM CorteCor_Servico
        WHERE IdSalao = @IdSalao
        ORDER BY Nome;";

        var servicos = new List<Servico>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", idSalao);

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
                        Cor = reader["Cor"] is DBNull ? "" : reader["Cor"].ToString(),
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
            Cor = @Cor,
            IdSalao = @IdSalao
        WHERE IdServico = @IdServico;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", servico.Nome ?? "");
            command.Parameters.AddWithValue("@Preco", servico.Preco);
            command.Parameters.AddWithValue("@Duracao", servico.Duracao);
            command.Parameters.AddWithValue("@Cor", servico.Cor ?? "");
            command.Parameters.AddWithValue("@IdSalao", servico.IdSalao);
            command.Parameters.AddWithValue("@IdServico", servico.IdServico);

            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Servico WHERE IdServico = @IdServico";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdServico", id);
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
    /// <summary>
    /// Vincula um serviço a um funcionário (N:N).
    /// </summary>
    public void Vincular(int idFuncionario, int idServico)
    {
        string query = @"
        INSERT INTO CorteCor_Funcionario_Servico (IdFuncionario, IdServico)
        VALUES (@IdFuncionario, @IdServico);";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
            command.Parameters.AddWithValue("@IdServico", idServico);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Remove o vínculo entre um funcionário e um serviço.
    /// </summary>
    public void Desvincular(int idFuncionario, int idServico)
    {
        string query = @"
        DELETE FROM CorteCor_Funcionario_Servico
        WHERE IdFuncionario = @IdFuncionario AND IdServico = @IdServico;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
            command.Parameters.AddWithValue("@IdServico", idServico);
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
        using (var command = new SqlCommand(query, connection))
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
    public List<int> ListarServicosDoFuncionario(int idFuncionario)
    {
        string query = @"
        SELECT IdServico
        FROM CorteCor_Funcionario_Servico
        WHERE IdFuncionario = @IdFuncionario
        ORDER BY IdServico;";

        var servicos = new List<int>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);

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
    public List<int> ListarFuncionariosDoServico(int idServico)
    {
        string query = @"
        SELECT IdFuncionario
        FROM CorteCor_Funcionario_Servico
        WHERE IdServico = @IdServico
        ORDER BY IdFuncionario;";

        var funcionarios = new List<int>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdServico", idServico);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdServico", idServico);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);

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
    //    using (var command = new SqlCommand(query, connection))
    //    {
    //        command.Parameters.AddWithValue("@IdFuncionario", entity.IdFuncionario);
    //        command.Parameters.AddWithValue("@IdServico", entity.IdServico);
    //        command.ExecuteNonQuery();
    //    }
    //}



}

public class PessoaHandler : EntityHandler<Pessoa>
{
    public int CadastrarPessoa(Pessoa pessoa)
    {
        int novoId = 0;

        string query = @"
        INSERT INTO CorteCor_Pessoa
            (Nome, Telefone, Email, DataNascimento, IdSalao)
        VALUES
            (@Nome, @Telefone, @Email, @DataNascimento, @IdSalao);
        SELECT SCOPE_IDENTITY();";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", pessoa.Nome ?? "");

            command.Parameters.AddWithValue("@Telefone", string.IsNullOrWhiteSpace(pessoa.Telefone)
                ? (object)DBNull.Value
                : pessoa.Telefone);

            command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(pessoa.Email)
                ? (object)DBNull.Value
                : pessoa.Email);

            command.Parameters.AddWithValue("@DataNascimento", pessoa.DataNascimento.HasValue
                ? (object)pessoa.DataNascimento.Value.Date
                : DBNull.Value);

            command.Parameters.AddWithValue("@IdSalao", pessoa.IdSalao);

            object result = command.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
                novoId = id;
        }

        return novoId;
    }

    public Pessoa ObterPorId(int idPessoa)
    {
        string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao
        FROM CorteCor_Pessoa
        WHERE IdPessoa = @IdPessoa;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdPessoa", idPessoa);

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
        using (var command = new SqlCommand(query, connection))
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

    public List<Pessoa> ListarPorSalao(int idSalao)
    {
        string query = @"
        SELECT IdPessoa, Nome, Telefone, Email, DataNascimento, IdSalao
        FROM CorteCor_Pessoa
        WHERE IdSalao = @IdSalao
        ORDER BY Nome;";

        var pessoas = new List<Pessoa>();

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", idSalao);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", pessoa.Nome ?? "");

            command.Parameters.AddWithValue("@Telefone", string.IsNullOrWhiteSpace(pessoa.Telefone)
                ? (object)DBNull.Value
                : pessoa.Telefone);

            command.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(pessoa.Email)
                ? (object)DBNull.Value
                : pessoa.Email);

            command.Parameters.AddWithValue("@DataNascimento", pessoa.DataNascimento.HasValue
                ? (object)pessoa.DataNascimento.Value.Date
                : DBNull.Value);

            command.Parameters.AddWithValue("@IdSalao", pessoa.IdSalao);
            command.Parameters.AddWithValue("@IdPessoa", pessoa.IdPessoa);

            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Pessoa WHERE IdPessoa = @IdPessoa";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdPessoa", id);
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
    public int CadastrarAgendamento(Agendamento agendamento)
    {
        int novoId = 0;

        string query = @"
        INSERT INTO CorteCor_Agendamento
            (DataHora, Status, IdServico, IdPessoa, IdFuncionario)
        VALUES
            (@DataHora, @Status, @IdServico, @IdPessoa, @IdFuncionario);
        SELECT SCOPE_IDENTITY();";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@DataHora", agendamento.DataHora);
            command.Parameters.AddWithValue("@Status", agendamento.Status ?? "Agendado");
            command.Parameters.AddWithValue("@IdServico", agendamento.IdServico);
            command.Parameters.AddWithValue("@IdPessoa", agendamento.IdPessoa);
            command.Parameters.AddWithValue("@IdFuncionario", agendamento.IdFuncionario);

            object result = command.ExecuteScalar();
            if (result != null && int.TryParse(result.ToString(), out int id))
                novoId = id;
        }

        return novoId;
    }

    public Agendamento ObterPorId(int idAgendamento)
    {
        string query = @"
        SELECT IdAgendamento, DataHora, Status, IdServico, IdPessoa, IdFuncionario
        FROM CorteCor_Agendamento
        WHERE IdAgendamento = @IdAgendamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdAgendamento", idAgendamento);

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
        using (var command = new SqlCommand(query, connection))
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

    public List<Agendamento> ListarPorIntervalo(int idSalao, DateTime inicio, DateTime fim)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", idSalao);
            command.Parameters.AddWithValue("@Inicio", inicio);
            command.Parameters.AddWithValue("@Fim", fim);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
            command.Parameters.AddWithValue("@DataInicio", (object?)dataInicio ?? DBNull.Value);
            command.Parameters.AddWithValue("@DataFim", (object?)dataFim ?? DBNull.Value);

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

    public void Atualizar(Agendamento agendamento)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@DataHora", agendamento.DataHora);
            command.Parameters.AddWithValue("@Status", agendamento.Status ?? "Agendado");
            command.Parameters.AddWithValue("@IdServico", agendamento.IdServico);
            command.Parameters.AddWithValue("@IdPessoa", agendamento.IdPessoa);
            command.Parameters.AddWithValue("@IdFuncionario", agendamento.IdFuncionario);
            command.Parameters.AddWithValue("@IdAgendamento", agendamento.IdAgendamento);

            command.ExecuteNonQuery();
        }
    }

    public bool VerificarDisponibilidade(int idFuncionario, DateTime inicio, DateTime fim, int? idAgendamentoIgnorar = null)
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdFuncionario", idFuncionario);
            command.Parameters.AddWithValue("@Inicio", inicio);
            command.Parameters.AddWithValue("@Fim", fim);
            command.Parameters.AddWithValue("@IdAgendamentoIgnorar", (object?)idAgendamentoIgnorar ?? DBNull.Value);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@IdAgendamento", id);
            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_Agendamento WHERE IdAgendamento = @IdAgendamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdAgendamento", id);
            command.ExecuteNonQuery();
        }
    }

    public void AtualizarStatus(int id, string status)
    {
        string query = "UPDATE CorteCor_Agendamento SET Status = @Status WHERE IdAgendamento = @IdAgendamento;";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@IdAgendamento", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", meio.Nome ?? "");
            command.Parameters.AddWithValue("@Tipo", meio.Tipo ?? "");
            command.Parameters.AddWithValue("@Gateway", meio.Gateway ?? "");

            command.Parameters.AddWithValue("@PermiteParcelamento", meio.PermiteParcelamento);
            command.Parameters.AddWithValue("@ParcelasMax", (object?)meio.ParcelasMax ?? DBNull.Value);

            command.Parameters.AddWithValue("@TaxaPercentual", meio.TaxaPercentual);
            command.Parameters.AddWithValue("@TaxaFixa", meio.TaxaFixa);
            command.Parameters.AddWithValue("@PrazoRecebimentoDias", meio.PrazoRecebimentoDias);

            command.Parameters.AddWithValue("@Ativo", meio.Ativo);

            command.Parameters.AddWithValue("@IdSalao", meio.IdSalao);
            command.Parameters.AddWithValue("@DataCadastro", meio.DataCadastro == default ? DateTime.Now : meio.DataCadastro);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdMeioPagamento", idMeioPagamento);

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
        using (var command = new SqlCommand(query, connection))
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdSalao", idSalao);
            command.Parameters.AddWithValue("@SomenteAtivos", (object?)somenteAtivos ?? DBNull.Value);

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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Nome", meio.Nome ?? "");
            command.Parameters.AddWithValue("@Tipo", meio.Tipo ?? "");
            command.Parameters.AddWithValue("@Gateway", meio.Gateway ?? "");

            command.Parameters.AddWithValue("@PermiteParcelamento", meio.PermiteParcelamento);
            command.Parameters.AddWithValue("@ParcelasMax", (object?)meio.ParcelasMax ?? DBNull.Value);

            command.Parameters.AddWithValue("@TaxaPercentual", meio.TaxaPercentual);
            command.Parameters.AddWithValue("@TaxaFixa", meio.TaxaFixa);
            command.Parameters.AddWithValue("@PrazoRecebimentoDias", meio.PrazoRecebimentoDias);

            command.Parameters.AddWithValue("@Ativo", meio.Ativo);

            command.Parameters.AddWithValue("@IdSalao", meio.IdSalao);
            command.Parameters.AddWithValue("@DataCadastro", meio.DataCadastro == default ? DateTime.Now : meio.DataCadastro);

            command.Parameters.AddWithValue("@IdMeioPagamento", meio.IdMeioPagamento);

            command.ExecuteNonQuery();
        }
    }

    public override void AtivarDesativar(int id, bool ativar)
    {
        string query = "UPDATE CorteCor_MeioPagamento SET Ativo = @Ativo WHERE IdMeioPagamento = @IdMeioPagamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Ativo", ativar);
            command.Parameters.AddWithValue("@IdMeioPagamento", id);
            command.ExecuteNonQuery();
        }
    }

    public override void Excluir(int id)
    {
        string query = "DELETE FROM CorteCor_MeioPagamento WHERE IdMeioPagamento = @IdMeioPagamento;";

        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdMeioPagamento", id);
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
    public void CadastrarPagamento(Pagamento pagamento)
    {
        // Desativa pagamentos anteriores para evitar erro no índice único (UX_CorteCor_Pagamento_Agendamento_Ativo)
        string deactivateQuery = "UPDATE CorteCor_Pagamento SET Ativo = 0 WHERE IdAgendamento = @IdAgendamento AND Ativo = 1;";
        
        string insertQuery = @"
        INSERT INTO CorteCor_Pagamento
            (IdPagamento, IdAgendamento, Ativo, Status, Valor, Moeda, Descricao, 
             MercadoPagoPreferenceId, MercadoPagoPaymentId, CheckoutUrl, MpStatus, MpStatusDetail)
        VALUES
            (@IdPagamento, @IdAgendamento, @Ativo, @Status, @Valor, @Moeda, @Descricao, 
             @MercadoPagoPreferenceId, @MercadoPagoPaymentId, @CheckoutUrl, @MpStatus, @MpStatusDetail);";

        using (var connection = _dbHandler.GetConnection())
        {
            using (var commandDeactivate = new SqlCommand(deactivateQuery, connection))
            {
                commandDeactivate.Parameters.AddWithValue("@IdAgendamento", pagamento.IdAgendamento);
                commandDeactivate.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@IdPagamento", pagamento.IdPagamento == Guid.Empty ? Guid.NewGuid() : pagamento.IdPagamento);
                command.Parameters.AddWithValue("@IdAgendamento", pagamento.IdAgendamento);
                command.Parameters.AddWithValue("@Ativo", pagamento.Ativo);
                command.Parameters.AddWithValue("@Status", pagamento.Status ?? "Pendente");
                command.Parameters.AddWithValue("@Valor", pagamento.Valor);
                command.Parameters.AddWithValue("@Moeda", pagamento.Moeda ?? "BRL");
                command.Parameters.AddWithValue("@Descricao", (object?)pagamento.Descricao ?? DBNull.Value);
                command.Parameters.AddWithValue("@MercadoPagoPreferenceId", (object?)pagamento.MercadoPagoPreferenceId ?? DBNull.Value);
                command.Parameters.AddWithValue("@MercadoPagoPaymentId", (object?)pagamento.MercadoPagoPaymentId ?? DBNull.Value);
                command.Parameters.AddWithValue("@CheckoutUrl", (object?)pagamento.CheckoutUrl ?? DBNull.Value);
                command.Parameters.AddWithValue("@MpStatus", (object?)pagamento.MpStatus ?? DBNull.Value);
                command.Parameters.AddWithValue("@MpStatusDetail", (object?)pagamento.MpStatusDetail ?? DBNull.Value);

                command.ExecuteNonQuery();
            }
        }
    }

    public Pagamento ObterPorIdAgendamento(int idAgendamento)
    {
        string query = "SELECT * FROM CorteCor_Pagamento WHERE IdAgendamento = @IdAgendamento AND Ativo = 1;";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdAgendamento", idAgendamento);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PreferenceId", preferenceId);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@PaymentId", paymentId);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Status", p.Status);
            command.Parameters.AddWithValue("@MercadoPagoPaymentId", (object?)p.MercadoPagoPaymentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@MpStatus", (object?)p.MpStatus ?? DBNull.Value);
            command.Parameters.AddWithValue("@MpStatusDetail", (object?)p.MpStatusDetail ?? DBNull.Value);
            command.Parameters.AddWithValue("@PagoEm", (object?)p.PagoEm ?? DBNull.Value);
            command.Parameters.AddWithValue("@Ativo", p.Ativo);
            command.Parameters.AddWithValue("@IdPagamento", p.IdPagamento);

            command.ExecuteNonQuery();
        }
    }

    private Pagamento Map(SqlDataReader reader)
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
            
            // Legacy mapping
            IdMeioPagamento = reader["IdMeioPagamento"] is DBNull ? 0 : (int)reader["IdMeioPagamento"],
            Tipo = reader["Tipo"]?.ToString(),
            Data = reader["Data"] is DBNull ? (DateTime)reader["CriadoEm"] : (DateTime)reader["Data"],
            Contos = reader["Contos"]?.ToString(),
            Campos = reader["Campos"]?.ToString()
        };
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@IdAgendamento", p.IdAgendamento);
            command.Parameters.AddWithValue("@IdMeioPagamento", p.IdMeioPagamento == 0 ? (object)DBNull.Value : p.IdMeioPagamento);
            command.Parameters.AddWithValue("@Tipo", p.Tipo ?? "");
            command.Parameters.AddWithValue("@Valor", p.Valor);
            command.Parameters.AddWithValue("@Data", p.Data == default ? DateTime.Now : p.Data);
            command.Parameters.AddWithValue("@Contos", p.Contos ?? "");
            command.Parameters.AddWithValue("@Campos", p.Campos ?? "");
            command.Parameters.AddWithValue("@IdPagamento", p.IdPagamento);

            command.ExecuteNonQuery();
        }
    }

    public override List<Pagamento> Listar()
    {
        string query = "SELECT * FROM CorteCor_Pagamento ORDER BY CriadoEm DESC;";
        var list = new List<Pagamento>();
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                list.Add(Map(reader));
            }
        }
        return list;
    }

    public Pagamento ObterPorId(Guid id)
    {
        string query = "SELECT * FROM CorteCor_Pagamento WHERE IdPagamento = @Id;";
        using (var connection = _dbHandler.GetConnection())
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", id);
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
        using (var command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }
    }

    public override void AtivarDesativar(int id, bool ativar) => throw new NotSupportedException("CorteCor_Pagamento utiliza UNIQUEIDENTIFIER.");
    public override void Cadastrar(Pagamento entity) => throw new NotSupportedException("Use CadastrarPagamento(Pagamento pago).");
}