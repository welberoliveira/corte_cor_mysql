using System.Data;
using System.Text;
using Dapper;
using CorteCor.Models;

namespace CorteCor.Handlers
{
    public class CrmHandler : ICrmHandler
    {
        private readonly IDatabaseHandler _databaseHandler;

        public CrmHandler(IDatabaseHandler databaseHandler)
        {
            _databaseHandler = databaseHandler;
        }

        private IDbConnection GetConnection() => _databaseHandler.GetConnection();

        public void GarantirEtapasPadrao(int idSalao)
        {
            const string countSql = "SELECT COUNT(1) FROM CorteCor_CrmEtapaFunil WHERE IdSalao = @IdSalao";
            using var conn = GetConnection();
            var total = conn.ExecuteScalar<int>(countSql, new { IdSalao = idSalao });
            if (total > 0)
            {
                return;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmEtapaFunil (IdSalao, Nome, Ordem, Ganha, Perdida, Ativa)
VALUES
(@IdSalao, 'Novo Lead', 1, 0, 0, 1),
(@IdSalao, 'Contato Realizado', 2, 0, 0, 1),
(@IdSalao, 'Proposta Enviada', 3, 0, 0, 1),
(@IdSalao, 'Negociacao', 4, 0, 0, 1),
(@IdSalao, 'Ganhou', 5, 1, 0, 1),
(@IdSalao, 'Perdeu', 6, 0, 1, 1);";
            conn.Execute(insertSql, new { IdSalao = idSalao });
        }

        public CrmPessoaPerfil ObterOuCriarPerfil(int idSalao, int idPessoa)
        {
            const string selectSql = @"
SELECT TOP 1 *
FROM CorteCor_CrmPessoaPerfil
WHERE IdSalao = @IdSalao AND IdPessoa = @IdPessoa;";

            using var conn = GetConnection();
            var perfil = conn.QueryFirstOrDefault<CrmPessoaPerfil>(selectSql, new { IdSalao = idSalao, IdPessoa = idPessoa });
            if (perfil != null)
            {
                return perfil;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmPessoaPerfil
    (IdSalao, IdPessoa, StatusRelacionamento, OrigemLead, Temperatura, ScoreRelacionamento,
     PermiteEmail, PermiteSms, PermiteWhatsapp, NaoPerturbe, UltimoContatoEm, ProximaAcaoEm,
     ObservacoesInternas, DataAtualizacao)
VALUES
    (@IdSalao, @IdPessoa, @StatusRelacionamento, @OrigemLead, @Temperatura, @ScoreRelacionamento,
     @PermiteEmail, @PermiteSms, @PermiteWhatsapp, @NaoPerturbe, @UltimoContatoEm, @ProximaAcaoEm,
     @ObservacoesInternas, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            perfil = new CrmPessoaPerfil
            {
                IdSalao = idSalao,
                IdPessoa = idPessoa,
                StatusRelacionamento = "Cliente",
                Temperatura = "Morno",
                ScoreRelacionamento = 0,
                PermiteEmail = true,
                PermiteSms = true,
                PermiteWhatsapp = true,
                NaoPerturbe = false
            };

            perfil.IdPerfil = conn.ExecuteScalar<int>(insertSql, perfil);
            perfil.DataAtualizacao = DateTime.Now;
            return perfil;
        }

        public void SalvarPerfil(CrmPessoaPerfil perfil)
        {
            const string sql = @"
UPDATE CorteCor_CrmPessoaPerfil
SET StatusRelacionamento = @StatusRelacionamento,
    OrigemLead = @OrigemLead,
    Temperatura = @Temperatura,
    ScoreRelacionamento = @ScoreRelacionamento,
    PermiteEmail = @PermiteEmail,
    PermiteSms = @PermiteSms,
    PermiteWhatsapp = @PermiteWhatsapp,
    NaoPerturbe = @NaoPerturbe,
    UltimoContatoEm = @UltimoContatoEm,
    ProximaAcaoEm = @ProximaAcaoEm,
    ObservacoesInternas = @ObservacoesInternas,
    DataAtualizacao = GETDATE()
WHERE IdPerfil = @IdPerfil AND IdSalao = @IdSalao;";

            using var conn = GetConnection();
            conn.Execute(sql, perfil);
        }

        public PagedResult<CrmClienteResumo> ListarClientesResumo(int idSalao, string? pesquisa, int pageIndex, int pageSize)
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var offset = (pageIndex - 1) * pageSize;
            var filtro = string.IsNullOrWhiteSpace(pesquisa) ? null : $"%{pesquisa.Trim()}%";

            const string whereSql = @"
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT A.IdPessoa, MAX(A.DataHora) AS UltimoAgendamentoEm
    FROM CorteCor_Agendamento A
    WHERE COALESCE(A.Excluido, 0) = 0
    GROUP BY A.IdPessoa
) UltimoAgendamento ON UltimoAgendamento.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT A.IdPessoa, MAX(COALESCE(PG.PagoEm, PG.AtualizadoEm, PG.CriadoEm)) AS UltimoPagamentoEm
    FROM CorteCor_Pagamento PG
    INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = PG.IdAgendamento
    WHERE COALESCE(PG.Ativo, 0) = 1
    GROUP BY A.IdPessoa
) UltimoPagamento ON UltimoPagamento.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT T.IdSalao, T.IdPessoa, COUNT(1) AS TarefasAbertas
    FROM CorteCor_CrmTarefa T
    WHERE T.Status = 'Aberta'
    GROUP BY T.IdSalao, T.IdPessoa
) Tarefas ON Tarefas.IdSalao = P.IdSalao AND Tarefas.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT O.IdSalao, O.IdPessoa, COUNT(1) AS OportunidadesAbertas,
           COALESCE(SUM(O.ValorEstimado), 0) AS ValorOportunidadesAbertas
    FROM CorteCor_CrmOportunidade O
    WHERE O.Status = 'Aberta'
    GROUP BY O.IdSalao, O.IdPessoa
) Oportunidades ON Oportunidades.IdSalao = P.IdSalao AND Oportunidades.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND (
        @Pesquisa IS NULL
        OR P.Nome LIKE @Pesquisa
        OR COALESCE(P.Email, '') LIKE COALESCE(@Pesquisa, '')
        OR COALESCE(P.Telefone, '') LIKE COALESCE(@Pesquisa, '')
        OR COALESCE(P.Tags, '') LIKE COALESCE(@Pesquisa, '')
     )";

            var totalSql = $"SELECT COUNT(1) {whereSql};";
            var listSql = $@"
SELECT
    P.IdPessoa,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS StatusRelacionamento,
    COALESCE(CP.Temperatura, 'Morno') AS Temperatura,
    UltimoAgendamento.UltimoAgendamentoEm,
    UltimoPagamento.UltimoPagamentoEm,
    CP.UltimoContatoEm,
    CP.ProximaAcaoEm,
    COALESCE(Tarefas.TarefasAbertas, 0) AS TarefasAbertas,
    COALESCE(Oportunidades.OportunidadesAbertas, 0) AS OportunidadesAbertas,
    COALESCE(Oportunidades.ValorOportunidadesAbertas, 0) AS ValorOportunidadesAbertas
{whereSql}
ORDER BY
    CASE WHEN CP.ProximaAcaoEm IS NULL THEN 1 ELSE 0 END,
    CP.ProximaAcaoEm,
    P.Nome
LIMIT @PageSize OFFSET @Offset;";

            using var conn = GetConnection();
            var total = conn.ExecuteScalar<int>(totalSql, new { IdSalao = idSalao, Pesquisa = filtro });
            var items = conn.Query<CrmClienteResumo>(listSql, new
            {
                IdSalao = idSalao,
                Pesquisa = filtro,
                Offset = offset,
                PageSize = pageSize
            }).ToList();

            return new PagedResult<CrmClienteResumo>
            {
                Items = items,
                TotalCount = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public CrmClienteResumo ObterClienteResumo(int idSalao, int idPessoa)
        {
            const string sql = @"
SELECT
    P.IdPessoa,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS StatusRelacionamento,
    COALESCE(CP.Temperatura, 'Morno') AS Temperatura,
    UltimoAgendamento.UltimoAgendamentoEm,
    UltimoPagamento.UltimoPagamentoEm,
    CP.UltimoContatoEm,
    CP.ProximaAcaoEm,
    COALESCE(Tarefas.TarefasAbertas, 0) AS TarefasAbertas,
    COALESCE(Oportunidades.OportunidadesAbertas, 0) AS OportunidadesAbertas,
    COALESCE(Oportunidades.ValorOportunidadesAbertas, 0) AS ValorOportunidadesAbertas
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT A.IdPessoa, MAX(A.DataHora) AS UltimoAgendamentoEm
    FROM CorteCor_Agendamento A
    WHERE COALESCE(A.Excluido, 0) = 0
    GROUP BY A.IdPessoa
) UltimoAgendamento ON UltimoAgendamento.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT A.IdPessoa, MAX(COALESCE(PG.PagoEm, PG.AtualizadoEm, PG.CriadoEm)) AS UltimoPagamentoEm
    FROM CorteCor_Pagamento PG
    INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = PG.IdAgendamento
    WHERE COALESCE(PG.Ativo, 0) = 1
    GROUP BY A.IdPessoa
) UltimoPagamento ON UltimoPagamento.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT T.IdSalao, T.IdPessoa, COUNT(1) AS TarefasAbertas
    FROM CorteCor_CrmTarefa T
    WHERE T.Status = 'Aberta'
    GROUP BY T.IdSalao, T.IdPessoa
) Tarefas ON Tarefas.IdSalao = P.IdSalao AND Tarefas.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT O.IdSalao, O.IdPessoa, COUNT(1) AS OportunidadesAbertas,
           COALESCE(SUM(O.ValorEstimado), 0) AS ValorOportunidadesAbertas
    FROM CorteCor_CrmOportunidade O
    WHERE O.Status = 'Aberta'
    GROUP BY O.IdSalao, O.IdPessoa
) Oportunidades ON Oportunidades.IdSalao = P.IdSalao AND Oportunidades.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND P.IdPessoa = @IdPessoa
  AND COALESCE(P.Excluido, 0) = 0
LIMIT 1;";

            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<CrmClienteResumo>(sql, new { IdSalao = idSalao, IdPessoa = idPessoa }) ?? new CrmClienteResumo();
        }

        public List<CrmTimelineItem> ListarTimeline(int idSalao, int idPessoa, int limit = 100)
        {
            using var conn = GetConnection();
            var timeline = new List<CrmTimelineItem>();

            const string interacoesSql = @"
SELECT
    I.DataInteracao AS Data,
    'Interacao' AS Categoria,
    I.Assunto AS Titulo,
    COALESCE(I.Descricao, '') AS Descricao,
    CASE I.Canal
        WHEN 'Email' THEN 'bg-primary'
        WHEN 'SMS' THEN 'bg-warning text-dark'
        WHEN 'WhatsApp' THEN 'bg-success'
        ELSE 'bg-secondary'
    END AS BadgeClass
FROM CorteCor_CrmInteracao I
WHERE I.IdSalao = @IdSalao AND I.IdPessoa = @IdPessoa";

            const string tarefasSql = @"
SELECT
    CASE WHEN T.DataConclusao IS NOT NULL THEN T.DataConclusao ELSE T.DataCriacao END AS Data,
    'Tarefa' AS Categoria,
    T.Titulo AS Titulo,
    CONCAT('Status: ', T.Status, CASE WHEN T.Descricao IS NULL OR T.Descricao = '' THEN '' ELSE ' | ' + T.Descricao END) AS Descricao,
    CASE WHEN T.Status = 'Concluida' THEN 'bg-success' ELSE 'bg-info' END AS BadgeClass
FROM CorteCor_CrmTarefa T
WHERE T.IdSalao = @IdSalao AND T.IdPessoa = @IdPessoa";

            const string oportunidadesSql = @"
SELECT
    O.DataAtualizacao AS Data,
    'Oportunidade' AS Categoria,
    O.Titulo AS Titulo,
    CONCAT('Etapa: ', E.Nome, ' | Status: ', O.Status, ' | Valor: R$ ', FORMAT(O.ValorEstimado, 'N2', 'pt-BR')) AS Descricao,
    CASE WHEN O.Status = 'Ganha' THEN 'bg-success'
         WHEN O.Status = 'Perdida' THEN 'bg-danger'
         ELSE 'bg-dark' END AS BadgeClass
FROM CorteCor_CrmOportunidade O
INNER JOIN CorteCor_CrmEtapaFunil E ON E.IdEtapa = O.IdEtapa
WHERE O.IdSalao = @IdSalao AND O.IdPessoa = @IdPessoa";

            const string agendamentosSql = @"
SELECT
    A.DataHora AS Data,
    'Agendamento' AS Categoria,
    CONCAT('Agendamento - ', S.Nome) AS Titulo,
    CONCAT('Status: ', A.Status, ' | Profissional: ', F.Nome) AS Descricao,
    'bg-primary' AS BadgeClass
FROM CorteCor_Agendamento A
INNER JOIN CorteCor_Servico S ON S.IdServico = A.IdServico
INNER JOIN CorteCor_Funcionario F ON F.IdFuncionario = A.IdFuncionario
WHERE A.IdPessoa = @IdPessoa AND ISNULL(A.Excluido, 0) = 0";

            const string pagamentosSql = @"
SELECT
    COALESCE(P.PagoEm, P.AtualizadoEm, P.CriadoEm) AS Data,
    'Pagamento' AS Categoria,
    'Pagamento registrado' AS Titulo,
    CONCAT('Status: ', P.Status, ' | Valor: R$ ', FORMAT(P.Valor, 'N2', 'pt-BR')) AS Descricao,
    CASE WHEN P.Status = 'Pago' THEN 'bg-success' ELSE 'bg-warning text-dark' END AS BadgeClass
FROM CorteCor_Pagamento P
INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = P.IdAgendamento
WHERE A.IdPessoa = @IdPessoa AND ISNULL(P.Ativo, 0) = 1";

            const string notasSql = @"
SELECT
    N.DataAtualizacao AS Data,
    'Fiscal' AS Categoria,
    CONCAT(N.TipoNota, ' ', N.Numero, '/', N.Serie) AS Titulo,
    CONCAT('Status: ', N.Status, CASE WHEN N.JustificativaRejeicao IS NULL OR N.JustificativaRejeicao = '' THEN '' ELSE ' | ' + N.JustificativaRejeicao END) AS Descricao,
    CASE WHEN N.Status = 'Autorizada' THEN 'bg-success'
         WHEN N.Status = 'Cancelada' THEN 'bg-secondary'
         WHEN N.Status = 'Rejeitada' THEN 'bg-danger'
         ELSE 'bg-warning text-dark' END AS BadgeClass
FROM CorteCor_NotaFiscal N
INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = N.IdAgendamento
WHERE N.IdSalao = @IdSalao AND A.IdPessoa = @IdPessoa";

            const string lembretesSql = @"
SELECT
    L.DataEnvio AS Data,
    'Comunicacao' AS Categoria,
    CONCAT('Envio ', ISNULL(L.TipoLembrete, 'Email')) AS Titulo,
    CONCAT('Destino: ', ISNULL(NULLIF(L.Destinatario, ''), ISNULL(L.Telefone, '-')), ' | Status: ', L.Status) AS Descricao,
    CASE WHEN L.Status = 'Sucesso' THEN 'bg-success' ELSE 'bg-danger' END AS BadgeClass
FROM CorteCor_LogEnvioEmail L
INNER JOIN CorteCor_Agendamento A ON A.IdAgendamento = L.IdAgendamento
WHERE A.IdPessoa = @IdPessoa";

            const string campanhasSql = @"
SELECT
    D.DataEnvio AS Data,
    'Campanha' AS Categoria,
    CONCAT('Campanha - ', C.Nome) AS Titulo,
    CONCAT('Canal: ', D.Canal, ' | Status: ', D.Status, CASE WHEN D.MensagemErro IS NULL OR D.MensagemErro = '' THEN '' ELSE ' | ' + D.MensagemErro END) AS Descricao,
    CASE WHEN D.Status = 'Sucesso' THEN 'bg-success' ELSE 'bg-danger' END AS BadgeClass
FROM CorteCor_CrmCampanhaDestino D
INNER JOIN CorteCor_CrmCampanha C ON C.IdCampanha = D.IdCampanha
WHERE D.IdSalao = @IdSalao AND D.IdPessoa = @IdPessoa";

            var param = new { IdSalao = idSalao, IdPessoa = idPessoa };
            timeline.AddRange(conn.Query<CrmTimelineItem>(interacoesSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(tarefasSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(oportunidadesSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(agendamentosSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(pagamentosSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(notasSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(lembretesSql, param));
            timeline.AddRange(conn.Query<CrmTimelineItem>(campanhasSql, param));

            return timeline
                .Where(t => t.Data != default)
                .OrderByDescending(t => t.Data)
                .Take(limit)
                .ToList();
        }

        public void AdicionarInteracao(CrmInteracao interacao)
        {
            const string sql = @"
INSERT INTO CorteCor_CrmInteracao
    (IdSalao, IdPessoa, IdUsuario, Canal, Tipo, Assunto, Descricao, DataInteracao, Referencia, OrigemSistema)
VALUES
    (@IdSalao, @IdPessoa, @IdUsuario, @Canal, @Tipo, @Assunto, @Descricao, @DataInteracao, @Referencia, @OrigemSistema);";

            using var conn = GetConnection();
            conn.Execute(sql, interacao);
        }

        public List<CrmInteracao> ListarInteracoes(int idSalao, int idPessoa, int limit = 50)
        {
            const string sql = @"
SELECT TOP (@Limit) *
FROM CorteCor_CrmInteracao
WHERE IdSalao = @IdSalao
  AND IdPessoa = @IdPessoa
ORDER BY DataInteracao DESC, IdInteracao DESC;";

            using var conn = GetConnection();
            return conn.Query<CrmInteracao>(sql, new { IdSalao = idSalao, IdPessoa = idPessoa, Limit = limit }).ToList();
        }

        public int SalvarInteracao(CrmInteracao interacao)
        {
            using var conn = GetConnection();
            if (interacao.IdInteracao > 0)
            {
                const string updateSql = @"
UPDATE CorteCor_CrmInteracao
SET IdUsuario = @IdUsuario,
    Canal = @Canal,
    Tipo = @Tipo,
    Assunto = @Assunto,
    Descricao = @Descricao,
    DataInteracao = @DataInteracao,
    Referencia = @Referencia,
    OrigemSistema = @OrigemSistema
WHERE IdInteracao = @IdInteracao
  AND IdSalao = @IdSalao
  AND IdPessoa = @IdPessoa;";

                conn.Execute(updateSql, interacao);
                return interacao.IdInteracao;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmInteracao
    (IdSalao, IdPessoa, IdUsuario, Canal, Tipo, Assunto, Descricao, DataInteracao, Referencia, OrigemSistema)
VALUES
    (@IdSalao, @IdPessoa, @IdUsuario, @Canal, @Tipo, @Assunto, @Descricao, @DataInteracao, @Referencia, @OrigemSistema);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return conn.ExecuteScalar<int>(insertSql, interacao);
        }

        public List<Usuario> ListarResponsaveis(int idSalao)
        {
            const string sql = @"
SELECT
    IdUsuario,
    Nome,
    Sobrenome,
    Email,
    Status,
    IdSalao
FROM CorteCor_Usuario
WHERE IdSalao = @IdSalao
  AND COALESCE(Status, 'Ativo') <> 'Inativo'
ORDER BY Nome, Sobrenome, Email;";

            using var conn = GetConnection();
            return conn.Query<Usuario>(sql, new { IdSalao = idSalao }).ToList();
        }

        public PagedResult<CrmTarefa> ListarTarefas(int idSalao, int? idPessoa, string? status, int? idUsuarioResponsavel, int pageIndex, int pageSize, string? pesquisa = null, DateTime? dataVencimentoInicio = null, DateTime? dataVencimentoFim = null)
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var offset = (pageIndex - 1) * pageSize;

            const string whereSql = @"
FROM CorteCor_CrmTarefa T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_Usuario U ON U.IdUsuario = T.IdUsuarioResponsavel
WHERE T.IdSalao = @IdSalao
  AND (@IdPessoa IS NULL OR T.IdPessoa = @IdPessoa)
  AND (@Status IS NULL OR T.Status = @Status)
  AND (@IdUsuarioResponsavel IS NULL OR T.IdUsuarioResponsavel = @IdUsuarioResponsavel)
  AND (@Pesquisa IS NULL OR T.Titulo LIKE CONCAT('%', @Pesquisa, '%') OR COALESCE(T.Descricao, '') LIKE CONCAT('%', @Pesquisa, '%'))
  AND (@DataVencimentoInicio IS NULL OR DATE(T.DataVencimento) >= @DataVencimentoInicio)
  AND (@DataVencimentoFim IS NULL OR DATE(T.DataVencimento) <= @DataVencimentoFim)";

            var totalSql = $"SELECT COUNT(1) {whereSql};";
            var listSql = $@"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    U.Nome AS NomeResponsavel
{whereSql}
ORDER BY
    CASE WHEN T.Status = 'Aberta' THEN 0 ELSE 1 END,
    T.DataVencimento,
    T.DataCriacao DESC
LIMIT @PageSize OFFSET @Offset;";

            using var conn = GetConnection();
            var parameters = new
            {
                IdSalao = idSalao,
                IdPessoa = idPessoa,
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                IdUsuarioResponsavel = idUsuarioResponsavel,
                Pesquisa = string.IsNullOrWhiteSpace(pesquisa) ? null : pesquisa.Trim(),
                DataVencimentoInicio = dataVencimentoInicio?.Date,
                DataVencimentoFim = dataVencimentoFim?.Date,
                Offset = offset,
                PageSize = pageSize
            };

            return new PagedResult<CrmTarefa>
            {
                Items = conn.Query<CrmTarefa>(listSql, parameters).ToList(),
                TotalCount = conn.ExecuteScalar<int>(totalSql, parameters),
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public CrmTarefa? ObterTarefa(int idSalao, int idTarefa)
        {
            const string sql = @"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    U.Nome AS NomeResponsavel
FROM CorteCor_CrmTarefa T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_Usuario U ON U.IdUsuario = T.IdUsuarioResponsavel
WHERE T.IdSalao = @IdSalao
  AND T.IdTarefa = @IdTarefa;";

            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<CrmTarefa>(sql, new { IdSalao = idSalao, IdTarefa = idTarefa });
        }

        public int SalvarTarefa(CrmTarefa tarefa)
        {
            using var conn = GetConnection();
            if (tarefa.IdTarefa > 0)
            {
                const string updateSql = @"
UPDATE CorteCor_CrmTarefa
SET IdPessoa = @IdPessoa,
    IdUsuarioResponsavel = @IdUsuarioResponsavel,
    Titulo = @Titulo,
    Descricao = @Descricao,
    Prioridade = @Prioridade,
    Status = @Status,
    CanalSugerido = @CanalSugerido,
    DataVencimento = @DataVencimento,
    DataConclusao = @DataConclusao
WHERE IdTarefa = @IdTarefa AND IdSalao = @IdSalao;";
                conn.Execute(updateSql, tarefa);
                return tarefa.IdTarefa;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmTarefa
    (IdSalao, IdPessoa, IdUsuarioResponsavel, Titulo, Descricao, Prioridade, Status, CanalSugerido, DataVencimento, DataConclusao, DataCriacao)
VALUES
    (@IdSalao, @IdPessoa, @IdUsuarioResponsavel, @Titulo, @Descricao, @Prioridade, @Status, @CanalSugerido, @DataVencimento, @DataConclusao, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return conn.ExecuteScalar<int>(insertSql, tarefa);
        }

        public void AtualizarStatusTarefa(int idSalao, int idTarefa, string status, DateTime? dataConclusao)
        {
            const string sql = @"
UPDATE CorteCor_CrmTarefa
SET Status = @Status,
    DataConclusao = @DataConclusao
WHERE IdSalao = @IdSalao AND IdTarefa = @IdTarefa;";

            using var conn = GetConnection();
            conn.Execute(sql, new { IdSalao = idSalao, IdTarefa = idTarefa, Status = status, DataConclusao = dataConclusao });
        }

        public List<CrmEtapaFunil> ListarEtapas(int idSalao)
        {
            using var conn = GetConnection();
            return conn.Query<CrmEtapaFunil>(
                "SELECT * FROM CorteCor_CrmEtapaFunil WHERE IdSalao = @IdSalao AND Ativa = 1 ORDER BY Ordem, Nome",
                new { IdSalao = idSalao }).ToList();
        }

        public List<CrmOportunidade> ListarOportunidades(int idSalao, int? idPessoa, string? status)
        {
            const string sql = @"
SELECT
    O.*,
    P.Nome AS NomePessoa,
    E.Nome AS NomeEtapa
FROM CorteCor_CrmOportunidade O
INNER JOIN CorteCor_Pessoa P ON P.IdPessoa = O.IdPessoa
INNER JOIN CorteCor_CrmEtapaFunil E ON E.IdEtapa = O.IdEtapa
WHERE O.IdSalao = @IdSalao
  AND (@IdPessoa IS NULL OR O.IdPessoa = @IdPessoa)
  AND (@Status IS NULL OR O.Status = @Status)
ORDER BY E.Ordem, O.DataAtualizacao DESC;";

            using var conn = GetConnection();
            return conn.Query<CrmOportunidade>(sql, new
            {
                IdSalao = idSalao,
                IdPessoa = idPessoa,
                Status = string.IsNullOrWhiteSpace(status) ? null : status
            }).ToList();
        }

        public PagedResult<CrmOportunidade> ListarOportunidadesPaginadas(int idSalao, int? idPessoa, string? status, DateTime? dataInicio, DateTime? dataFim, int pageIndex, int pageSize)
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var offset = (pageIndex - 1) * pageSize;

            const string whereSql = @"
FROM CorteCor_CrmOportunidade O
INNER JOIN CorteCor_Pessoa P ON P.IdPessoa = O.IdPessoa
INNER JOIN CorteCor_CrmEtapaFunil E ON E.IdEtapa = O.IdEtapa
WHERE O.IdSalao = @IdSalao
  AND (@IdPessoa IS NULL OR O.IdPessoa = @IdPessoa)
  AND (@Status IS NULL OR O.Status = @Status)
  AND (@DataInicio IS NULL OR (O.PrevisaoFechamento IS NOT NULL AND CAST(O.PrevisaoFechamento AS DATE) >= @DataInicio))
  AND (@DataFim IS NULL OR (O.PrevisaoFechamento IS NOT NULL AND CAST(O.PrevisaoFechamento AS DATE) <= @DataFim))";

            var totalSql = $"SELECT COUNT(1) {whereSql};";
            var listSql = $@"
SELECT
    O.*,
    P.Nome AS NomePessoa,
    E.Nome AS NomeEtapa
{whereSql}
ORDER BY
    CASE WHEN O.Status = 'Aberta' THEN 0 WHEN O.Status = 'Ganha' THEN 1 ELSE 2 END,
    E.Ordem,
    COALESCE(O.PrevisaoFechamento, '9999-12-31'),
    O.DataAtualizacao DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var conn = GetConnection();
            var parameters = new
            {
                IdSalao = idSalao,
                IdPessoa = idPessoa,
                Status = string.IsNullOrWhiteSpace(status) ? null : status,
                DataInicio = dataInicio?.Date,
                DataFim = dataFim?.Date,
                Offset = offset,
                PageSize = pageSize
            };

            return new PagedResult<CrmOportunidade>
            {
                Items = conn.Query<CrmOportunidade>(listSql, parameters).ToList(),
                TotalCount = conn.ExecuteScalar<int>(totalSql, parameters),
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public int SalvarOportunidade(CrmOportunidade oportunidade)
        {
            using var conn = GetConnection();
            if (oportunidade.IdOportunidade > 0)
            {
                const string updateSql = @"
UPDATE CorteCor_CrmOportunidade
SET IdEtapa = @IdEtapa,
    Titulo = @Titulo,
    Descricao = @Descricao,
    ValorEstimado = @ValorEstimado,
    Probabilidade = @Probabilidade,
    Status = @Status,
    Origem = @Origem,
    PrevisaoFechamento = @PrevisaoFechamento,
    DataAtualizacao = GETDATE(),
    DataFechamento = @DataFechamento
WHERE IdOportunidade = @IdOportunidade AND IdSalao = @IdSalao;";
                conn.Execute(updateSql, oportunidade);
                return oportunidade.IdOportunidade;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmOportunidade
    (IdSalao, IdPessoa, IdEtapa, Titulo, Descricao, ValorEstimado, Probabilidade, Status, Origem, PrevisaoFechamento, DataCriacao, DataAtualizacao, DataFechamento)
VALUES
    (@IdSalao, @IdPessoa, @IdEtapa, @Titulo, @Descricao, @ValorEstimado, @Probabilidade, @Status, @Origem, @PrevisaoFechamento, GETDATE(), GETDATE(), @DataFechamento);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return conn.ExecuteScalar<int>(insertSql, oportunidade);
        }

        public void AtualizarEtapaOportunidade(int idSalao, int idOportunidade, int idEtapa, string status, DateTime? dataFechamento)
        {
            const string sql = @"
UPDATE CorteCor_CrmOportunidade
SET IdEtapa = @IdEtapa,
    Status = @Status,
    DataAtualizacao = GETDATE(),
    DataFechamento = @DataFechamento
WHERE IdSalao = @IdSalao AND IdOportunidade = @IdOportunidade;";

            using var conn = GetConnection();
            conn.Execute(sql, new
            {
                IdSalao = idSalao,
                IdOportunidade = idOportunidade,
                IdEtapa = idEtapa,
                Status = status,
                DataFechamento = dataFechamento
            });
        }

        public PagedResult<CrmCampanha> ListarCampanhas(int idSalao, int pageIndex, int pageSize, string? pesquisa = null, string? canal = null, string? segmento = null, string? status = null)
        {
            pageIndex = pageIndex <= 0 ? 1 : pageIndex;
            pageSize = pageSize <= 0 ? 10 : pageSize;
            var offset = (pageIndex - 1) * pageSize;
            var whereSql = new StringBuilder("WHERE IdSalao = @IdSalao");
            var parameters = new DynamicParameters();
            parameters.Add("IdSalao", idSalao);
            parameters.Add("Offset", offset);
            parameters.Add("PageSize", pageSize);

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                whereSql.Append(" AND (Nome LIKE @Pesquisa OR Assunto LIKE @Pesquisa)");
                parameters.Add("Pesquisa", $"%{pesquisa.Trim()}%");
            }

            if (!string.IsNullOrWhiteSpace(canal))
            {
                whereSql.Append(" AND Canal = @Canal");
                parameters.Add("Canal", canal.Trim());
            }

            if (!string.IsNullOrWhiteSpace(segmento))
            {
                whereSql.Append(" AND Segmento = @Segmento");
                parameters.Add("Segmento", segmento.Trim());
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                whereSql.Append(" AND Status = @Status");
                parameters.Add("Status", status.Trim());
            }

            var totalSql = $"SELECT COUNT(1) FROM CorteCor_CrmCampanha {whereSql}";
            var listSql = $@"
SELECT *
FROM CorteCor_CrmCampanha
{whereSql}
ORDER BY DataCriacao DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var conn = GetConnection();
            return new PagedResult<CrmCampanha>
            {
                Items = conn.Query<CrmCampanha>(listSql, parameters).ToList(),
                TotalCount = conn.ExecuteScalar<int>(totalSql, parameters),
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public int SalvarCampanha(CrmCampanha campanha)
        {
            using var conn = GetConnection();
            if (campanha.IdCampanha > 0)
            {
                const string updateSql = @"
UPDATE CorteCor_CrmCampanha
SET Nome = @Nome,
    Canal = @Canal,
    Segmento = @Segmento,
    FiltroTag = @FiltroTag,
    DiasInatividade = @DiasInatividade,
    IdPessoa = @IdPessoa,
    Assunto = @Assunto,
    Conteudo = @Conteudo,
    Status = @Status
WHERE IdCampanha = @IdCampanha AND IdSalao = @IdSalao;";
                conn.Execute(updateSql, campanha);
                return campanha.IdCampanha;
            }

            const string insertSql = @"
INSERT INTO CorteCor_CrmCampanha
    (IdSalao, Nome, Canal, Segmento, FiltroTag, DiasInatividade, IdPessoa, Assunto, Conteudo, Status, TotalDestinatarios, TotalSucesso, TotalFalha, UltimoEnvioEm, DataCriacao)
VALUES
    (@IdSalao, @Nome, @Canal, @Segmento, @FiltroTag, @DiasInatividade, @IdPessoa, @Assunto, @Conteudo, @Status, 0, 0, 0, NULL, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return conn.ExecuteScalar<int>(insertSql, campanha);
        }

        public CrmCampanha? ObterCampanha(int idSalao, int idCampanha)
        {
            using var conn = GetConnection();
            return conn.QueryFirstOrDefault<CrmCampanha>(
                "SELECT * FROM CorteCor_CrmCampanha WHERE IdSalao = @IdSalao AND IdCampanha = @IdCampanha",
                new { IdSalao = idSalao, IdCampanha = idCampanha });
        }

        public List<CrmContatoCampanha> ListarPublicoCampanha(int idSalao, string segmento, string? filtroTag, int? diasInatividade, int? idPessoa)
        {
            var sql = @"
SELECT
    P.IdPessoa,
    P.IdSalao,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    P.DataNascimento,
    UltimoAgendamento.UltimoAgendamentoEm,
    COALESCE(CP.PermiteEmail, 1) AS PermiteEmail,
    COALESCE(CP.PermiteSms, 1) AS PermiteSms,
    COALESCE(CP.PermiteWhatsapp, 1) AS PermiteWhatsapp,
    COALESCE(CP.NaoPerturbe, 0) AS NaoPerturbe
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT A.IdPessoa, MAX(A.DataHora) AS UltimoAgendamentoEm
    FROM CorteCor_Agendamento A
    WHERE COALESCE(A.Excluido, 0) = 0
    GROUP BY A.IdPessoa
) UltimoAgendamento ON UltimoAgendamento.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1";

            var parameters = new DynamicParameters();
            parameters.Add("IdSalao", idSalao);

            switch (segmento)
            {
                case CrmSegmentoCampanha.Inativos:
                    sql += @"
  AND TIMESTAMPDIFF(DAY, COALESCE(UltimoAgendamento.UltimoAgendamentoEm, CAST('1900-01-01' AS DATETIME)), NOW()) >= @DiasInatividade";
                    parameters.Add("DiasInatividade", diasInatividade.GetValueOrDefault(60));
                    break;
                case CrmSegmentoCampanha.AniversariantesDoMes:
                    sql += @"
  AND P.DataNascimento IS NOT NULL
  AND MONTH(P.DataNascimento) = MONTH(NOW())";
                    break;
                case CrmSegmentoCampanha.PorTag:
                    sql += @"
  AND COALESCE(P.Tags, '') LIKE @FiltroTag";
                    parameters.Add("FiltroTag", $"%{filtroTag?.Trim()}%");
                    break;
                case CrmSegmentoCampanha.ClienteEspecifico:
                    sql += @"
  AND P.IdPessoa = @IdPessoa";
                    parameters.Add("IdPessoa", idPessoa);
                    break;
            }

            sql += " ORDER BY P.Nome;";
            using var conn = GetConnection();
            return conn.Query<CrmContatoCampanha>(sql, parameters).ToList();
        }

        public void RegistrarDestinoCampanha(CrmCampanhaDestino destino)
        {
            const string sql = @"
INSERT INTO CorteCor_CrmCampanhaDestino
    (IdCampanha, IdSalao, IdPessoa, Canal, Destino, Status, MensagemErro, DataEnvio)
VALUES
    (@IdCampanha, @IdSalao, @IdPessoa, @Canal, @Destino, @Status, @MensagemErro, @DataEnvio);";

            using var conn = GetConnection();
            conn.Execute(sql, destino);
        }

        public void AtualizarResumoCampanha(int idSalao, int idCampanha, string status, int totalDestinatarios, int totalSucesso, int totalFalha, DateTime? ultimoEnvioEm)
        {
            const string sql = @"
UPDATE CorteCor_CrmCampanha
SET Status = @Status,
    TotalDestinatarios = @TotalDestinatarios,
    TotalSucesso = @TotalSucesso,
    TotalFalha = @TotalFalha,
    UltimoEnvioEm = @UltimoEnvioEm
WHERE IdSalao = @IdSalao AND IdCampanha = @IdCampanha;";

            using var conn = GetConnection();
            conn.Execute(sql, new
            {
                IdSalao = idSalao,
                IdCampanha = idCampanha,
                Status = status,
                TotalDestinatarios = totalDestinatarios,
                TotalSucesso = totalSucesso,
                TotalFalha = totalFalha,
                UltimoEnvioEm = ultimoEnvioEm
            });
        }

        public List<CrmCampanhaDestino> ListarDestinosCampanha(int idSalao, int idCampanha, int limit = 100)
        {
            const string sql = @"
SELECT TOP (@Limit)
    D.*,
    P.Nome AS NomePessoa
FROM CorteCor_CrmCampanhaDestino D
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = D.IdPessoa
WHERE D.IdSalao = @IdSalao AND D.IdCampanha = @IdCampanha
ORDER BY D.DataEnvio DESC;";

            using var conn = GetConnection();
            return conn.Query<CrmCampanhaDestino>(sql, new { IdSalao = idSalao, IdCampanha = idCampanha, Limit = limit }).ToList();
        }

        public CrmDashboardResumo ObterDashboard(int idSalao)
        {
            using var conn = GetConnection();
            var resumo = new CrmDashboardResumo
            {
                TotalClientes = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Pessoa
WHERE IdSalao = @IdSalao AND COALESCE(Excluido, 0) = 0 AND COALESCE(IsCliente, 0) = 1;", new { IdSalao = idSalao }),

                ClientesInativos = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Pessoa P
LEFT JOIN (
    SELECT
        A.IdPessoa,
        MAX(A.DataHora) AS UltimoAgendamentoEm
    FROM CorteCor_Agendamento A
    WHERE COALESCE(A.Excluido, 0) = 0
    GROUP BY A.IdPessoa
) UltimoAgendamento ON UltimoAgendamento.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND TIMESTAMPDIFF(DAY, COALESCE(UltimoAgendamento.UltimoAgendamentoEm, CAST('1900-01-01' AS DATETIME)), NOW()) >= 60;", new { IdSalao = idSalao }),

                AniversariantesMes = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Pessoa
WHERE IdSalao = @IdSalao
  AND COALESCE(Excluido, 0) = 0
  AND COALESCE(IsCliente, 0) = 1
  AND DataNascimento IS NOT NULL
  AND MONTH(DataNascimento) = MONTH(NOW());", new { IdSalao = idSalao }),

                TarefasAbertas = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmTarefa
WHERE IdSalao = @IdSalao AND Status = 'Aberta';", new { IdSalao = idSalao }),

                TarefasAtrasadas = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmTarefa
WHERE IdSalao = @IdSalao AND Status = 'Aberta' AND DataVencimento < NOW();", new { IdSalao = idSalao }),

                OportunidadesAbertas = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmOportunidade
WHERE IdSalao = @IdSalao AND Status = 'Aberta';", new { IdSalao = idSalao }),

                ValorOportunidadesAbertas = conn.ExecuteScalar<decimal>(@"
SELECT COALESCE(SUM(ValorEstimado), 0)
FROM CorteCor_CrmOportunidade
WHERE IdSalao = @IdSalao AND Status = 'Aberta';", new { IdSalao = idSalao }),

                CampanhasEnviadas30Dias = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmCampanha
WHERE IdSalao = @IdSalao
  AND UltimoEnvioEm >= DATE_SUB(NOW(), INTERVAL 30 DAY);", new { IdSalao = idSalao })
            };

            resumo.Funil = conn.Query<CrmEtapaResumo>(@"
SELECT
    E.Nome AS NomeEtapa,
    COUNT(O.IdOportunidade) AS Quantidade,
    ISNULL(SUM(O.ValorEstimado), 0) AS ValorTotal
FROM CorteCor_CrmEtapaFunil E
LEFT JOIN CorteCor_CrmOportunidade O ON O.IdEtapa = E.IdEtapa AND O.Status = 'Aberta'
WHERE E.IdSalao = @IdSalao AND E.Ativa = 1
GROUP BY E.Nome, E.Ordem
ORDER BY E.Ordem;", new { IdSalao = idSalao }).ToList();

            resumo.ProximasTarefas = conn.Query<CrmTarefa>(@"
SELECT
    T.*,
    P.Nome AS NomePessoa,
    U.Nome AS NomeResponsavel
FROM CorteCor_CrmTarefa T
LEFT JOIN CorteCor_Pessoa P ON P.IdPessoa = T.IdPessoa
LEFT JOIN CorteCor_Usuario U ON U.IdUsuario = T.IdUsuarioResponsavel
WHERE T.IdSalao = @IdSalao AND T.Status = 'Aberta'
ORDER BY T.DataVencimento, T.DataCriacao DESC
LIMIT 5;", new { IdSalao = idSalao }).ToList();

            resumo.ClientesEmRisco = conn.Query<CrmClienteResumo>(@"
SELECT
    P.IdPessoa,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS StatusRelacionamento,
    COALESCE(CP.Temperatura, 'Morno') AS Temperatura,
    UltimoAgendamento.UltimoAgendamentoEm,
    NULL AS UltimoPagamentoEm,
    CP.UltimoContatoEm,
    CP.ProximaAcaoEm,
    0 AS TarefasAbertas,
    0 AS OportunidadesAbertas,
    0 AS ValorOportunidadesAbertas
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT
        A.IdPessoa,
        MAX(A.DataHora) AS UltimoAgendamentoEm
    FROM CorteCor_Agendamento A
    WHERE COALESCE(A.Excluido, 0) = 0
    GROUP BY A.IdPessoa
) UltimoAgendamento ON UltimoAgendamento.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND TIMESTAMPDIFF(DAY, COALESCE(UltimoAgendamento.UltimoAgendamentoEm, CAST('1900-01-01' AS DATETIME)), NOW()) >= 60
ORDER BY UltimoAgendamento.UltimoAgendamentoEm, P.Nome
LIMIT 5;", new { IdSalao = idSalao }).ToList();

            return resumo;
        }

        public CrmRelatorioResumo ObterRelatorios(int idSalao, DateTime dataInicio, DateTime dataFim, int? idUsuarioResponsavel = null)
        {
            using var conn = GetConnection();
            var parametrosRelatorio = new
            {
                IdSalao = idSalao,
                DataInicio = dataInicio.Date,
                DataFim = dataFim.Date,
                IdUsuarioResponsavel = idUsuarioResponsavel
            };

            var resumo = new CrmRelatorioResumo
            {
                TotalClientes = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Pessoa P
WHERE P.IdSalao = @IdSalao
  AND ISNULL(P.Excluido, 0) = 0
  AND ISNULL(P.IsCliente, 0) = 1
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = P.IdSalao
            AND T.IdPessoa = P.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmInteracao I
          WHERE I.IdSalao = P.IdSalao
            AND I.IdPessoa = P.IdPessoa
            AND I.IdUsuario = @IdUsuarioResponsavel
      )
  );", parametrosRelatorio),

                ClientesSemContato30Dias = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND TIMESTAMPDIFF(DAY, COALESCE(CP.UltimoContatoEm, CAST('1900-01-01' AS DATETIME)), NOW()) >= 30
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = P.IdSalao
            AND T.IdPessoa = P.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmInteracao I
          WHERE I.IdSalao = P.IdSalao
            AND I.IdPessoa = P.IdPessoa
            AND I.IdUsuario = @IdUsuarioResponsavel
      )
  );", parametrosRelatorio),

                ClientesComTarefasAtrasadas = conn.ExecuteScalar<int>(@"
SELECT COUNT(DISTINCT IdPessoa)
FROM CorteCor_CrmTarefa
WHERE IdSalao = @IdSalao
  AND Status = 'Aberta'
  AND DataVencimento < NOW()
  AND IdPessoa IS NOT NULL
  AND (@IdUsuarioResponsavel IS NULL OR IdUsuarioResponsavel = @IdUsuarioResponsavel);", parametrosRelatorio),

                OportunidadesAbertas = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmOportunidade O
WHERE O.IdSalao = @IdSalao
  AND O.Status = 'Aberta'
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = O.IdSalao
            AND T.IdPessoa = O.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
  );", parametrosRelatorio),

                ValorPipelineAberto = conn.ExecuteScalar<decimal>(@"
SELECT COALESCE(SUM(ValorEstimado), 0)
FROM CorteCor_CrmOportunidade O
WHERE O.IdSalao = @IdSalao
  AND O.Status = 'Aberta'
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = O.IdSalao
            AND T.IdPessoa = O.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
  );", parametrosRelatorio),

                CampanhasEnviadasPeriodo = conn.ExecuteScalar<int>(@"
SELECT COUNT(1)
FROM CorteCor_CrmCampanha
WHERE IdSalao = @IdSalao
  AND UltimoEnvioEm >= @DataInicio
  AND UltimoEnvioEm < DATE_ADD(@DataFim, INTERVAL 1 DAY);", parametrosRelatorio)
            };

            resumo.ClientesPorStatus = conn.Query<CrmResumoFaixa>(@"
SELECT
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS Nome,
    COUNT(1) AS Quantidade,
    CAST(0 AS DECIMAL(18,2)) AS Valor
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = P.IdSalao
            AND T.IdPessoa = P.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmInteracao I
          WHERE I.IdSalao = P.IdSalao
            AND I.IdPessoa = P.IdPessoa
            AND I.IdUsuario = @IdUsuarioResponsavel
      )
  )
GROUP BY COALESCE(CP.StatusRelacionamento, 'Cliente')
ORDER BY COUNT(1) DESC;", parametrosRelatorio).ToList();

            resumo.ClientesPorTemperatura = conn.Query<CrmResumoFaixa>(@"
SELECT
    COALESCE(CP.Temperatura, 'Morno') AS Nome,
    COUNT(1) AS Quantidade,
    CAST(0 AS DECIMAL(18,2)) AS Valor
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = P.IdSalao
            AND T.IdPessoa = P.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmInteracao I
          WHERE I.IdSalao = P.IdSalao
            AND I.IdPessoa = P.IdPessoa
            AND I.IdUsuario = @IdUsuarioResponsavel
      )
  )
GROUP BY COALESCE(CP.Temperatura, 'Morno')
ORDER BY COUNT(1) DESC;", parametrosRelatorio).ToList();

            resumo.InteracoesPorCanal = conn.Query<CrmResumoFaixa>(@"
SELECT
    Canal AS Nome,
    COUNT(1) AS Quantidade,
    CAST(0 AS DECIMAL(18,2)) AS Valor
FROM CorteCor_CrmInteracao
WHERE IdSalao = @IdSalao
  AND DataInteracao >= @DataInicio
  AND DataInteracao < DATE_ADD(@DataFim, INTERVAL 1 DAY)
  AND (@IdUsuarioResponsavel IS NULL OR IdUsuario = @IdUsuarioResponsavel)
GROUP BY Canal
ORDER BY COUNT(1) DESC;", parametrosRelatorio).ToList();

            resumo.TarefasPorStatus = conn.Query<CrmResumoFaixa>(@"
SELECT
    Status AS Nome,
    COUNT(1) AS Quantidade,
    CAST(0 AS DECIMAL(18,2)) AS Valor
FROM CorteCor_CrmTarefa
WHERE IdSalao = @IdSalao
  AND (@IdUsuarioResponsavel IS NULL OR IdUsuarioResponsavel = @IdUsuarioResponsavel)
GROUP BY Status
ORDER BY COUNT(1) DESC;", parametrosRelatorio).ToList();

            resumo.OportunidadesPorEtapa = conn.Query<CrmResumoFaixa>(@"
SELECT
    E.Nome AS Nome,
    COUNT(O.IdOportunidade) AS Quantidade,
    COALESCE(SUM(O.ValorEstimado), 0) AS Valor
FROM CorteCor_CrmEtapaFunil E
LEFT JOIN CorteCor_CrmOportunidade O ON O.IdEtapa = E.IdEtapa
    AND O.IdSalao = E.IdSalao
    AND O.Status = 'Aberta'
    AND (
        @IdUsuarioResponsavel IS NULL
        OR EXISTS (
            SELECT 1
            FROM CorteCor_CrmTarefa T
            WHERE T.IdSalao = O.IdSalao
              AND T.IdPessoa = O.IdPessoa
              AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
        )
    )
WHERE E.IdSalao = @IdSalao
  AND E.Ativa = 1
GROUP BY E.Nome, E.Ordem
ORDER BY E.Ordem;", parametrosRelatorio).ToList();

            resumo.CampanhasPorCanal = conn.Query<CrmResumoFaixa>(@"
SELECT
    Canal AS Nome,
    COUNT(1) AS Quantidade,
    SUM(COALESCE(TotalSucesso, 0)) AS Valor
FROM CorteCor_CrmCampanha
WHERE IdSalao = @IdSalao
GROUP BY Canal
ORDER BY COUNT(1) DESC;", parametrosRelatorio).ToList();

            resumo.ClientesEmRisco = conn.Query<CrmClienteResumo>(@"
SELECT
    P.IdPessoa,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS StatusRelacionamento,
    COALESCE(CP.Temperatura, 'Morno') AS Temperatura,
    NULL AS UltimoAgendamentoEm,
    NULL AS UltimoPagamentoEm,
    CP.UltimoContatoEm,
    CP.ProximaAcaoEm,
    0 AS TarefasAbertas,
    0 AS OportunidadesAbertas,
    0 AS ValorOportunidadesAbertas
FROM CorteCor_Pessoa P
LEFT JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND TIMESTAMPDIFF(DAY, COALESCE(CP.UltimoContatoEm, CAST('1900-01-01' AS DATETIME)), NOW()) >= 30
  AND (
      @IdUsuarioResponsavel IS NULL
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmTarefa T
          WHERE T.IdSalao = P.IdSalao
            AND T.IdPessoa = P.IdPessoa
            AND T.IdUsuarioResponsavel = @IdUsuarioResponsavel
      )
      OR EXISTS (
          SELECT 1
          FROM CorteCor_CrmInteracao I
          WHERE I.IdSalao = P.IdSalao
            AND I.IdPessoa = P.IdPessoa
            AND I.IdUsuario = @IdUsuarioResponsavel
      )
  )
ORDER BY CP.UltimoContatoEm, P.Nome
LIMIT 10;", parametrosRelatorio).ToList();

            resumo.ProximasAcoes = conn.Query<CrmClienteResumo>(@"
SELECT
    P.IdPessoa,
    P.Nome,
    P.Email,
    P.Telefone,
    P.Tags,
    COALESCE(CP.StatusRelacionamento, 'Cliente') AS StatusRelacionamento,
    COALESCE(CP.Temperatura, 'Morno') AS Temperatura,
    NULL AS UltimoAgendamentoEm,
    NULL AS UltimoPagamentoEm,
    CP.UltimoContatoEm,
    CP.ProximaAcaoEm,
    COALESCE(TA.TotalAberta, 0) AS TarefasAbertas,
    0 AS OportunidadesAbertas,
    0 AS ValorOportunidadesAbertas
FROM CorteCor_Pessoa P
INNER JOIN CorteCor_CrmPessoaPerfil CP ON CP.IdSalao = P.IdSalao AND CP.IdPessoa = P.IdPessoa
LEFT JOIN (
    SELECT T.IdSalao, T.IdPessoa, COUNT(1) AS TotalAberta
    FROM CorteCor_CrmTarefa T
    WHERE T.Status = 'Aberta'
      AND (@IdUsuarioResponsavel IS NULL OR T.IdUsuarioResponsavel = @IdUsuarioResponsavel)
    GROUP BY T.IdSalao, T.IdPessoa
) TA ON TA.IdSalao = P.IdSalao AND TA.IdPessoa = P.IdPessoa
WHERE P.IdSalao = @IdSalao
  AND COALESCE(P.Excluido, 0) = 0
  AND COALESCE(P.IsCliente, 0) = 1
  AND CP.ProximaAcaoEm IS NOT NULL
  AND (@IdUsuarioResponsavel IS NULL OR COALESCE(TA.TotalAberta, 0) > 0)
ORDER BY CP.ProximaAcaoEm, P.Nome
LIMIT 10;", parametrosRelatorio).ToList();

            resumo.UltimasCampanhas = conn.Query<CrmCampanha>(@"
SELECT *
FROM CorteCor_CrmCampanha
WHERE IdSalao = @IdSalao
ORDER BY COALESCE(UltimoEnvioEm, DataCriacao) DESC
LIMIT 10;", parametrosRelatorio).ToList();

            return resumo;
        }
    }
}
