using System.Data;
using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CorteCor;

using static CorteCor.Models;

namespace CorteCor.Pages
{
    [Authorize]
    public class AgendamentosModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IDatabaseHandler _dbHandler;

        public AgendamentosModel(IConfiguration config)
        {
            _config = config;
            _dbHandler = new DatabaseHandler();
        }

        private int GetIdSalao()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);
            return idSalao;
        }

        private static DateTime ParseDpDate(string s)
        {
            // DayPilot geralmente envia "yyyy-MM-ddTHH:mm:ss"
            // Vamos tratar como horário local (Brasil).
            return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
        }

        private static string ToIso(DateTime dt) =>
            dt.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        private static decimal? ParseMoney(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            // tenta pt-BR (1.234,56)
            if (decimal.TryParse(s, NumberStyles.Number, new CultureInfo("pt-BR"), out var br))
                return br;

            // tenta invariant (1234.56)
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv))
                return inv;

            return null;
        }

        // =========================
        // RESOURCES (Funcionários)
        // =========================
        public IActionResult OnGetResources()
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new JsonResult(Array.Empty<object>());

            var list = new List<object>();

            using var con = _dbHandler.GetConnection();

            // Ajuste os nomes das colunas conforme seu banco
            var sql = @"
    select 
        f.IdFuncionario as Id,
        f.Nome as Nome
    from CorteCor_Funcionario f
    where f.IdSalao = @IdSalao
    order by f.Nome;
    ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new
                {
                    id = rd["Id"].ToString(),
                    name = rd["Nome"]?.ToString() ?? ""
                });
            }

            return new JsonResult(list);
        }

        // =========================
        // CLIENTES
        // =========================
        public IActionResult OnGetClientes()
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new JsonResult(Array.Empty<object>());

            var list = new List<object>();

            using var con = _dbHandler.GetConnection();

            var sql = @"
    select 
        c.IdCliente as Id,
        c.Nome as Nome
    from CorteCor_Cliente c
    where c.IdSalao = @IdSalao
    order by c.Nome;
    ";
            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new
                {
                    id = rd["Id"].ToString(),
                    nome = rd["Nome"]?.ToString() ?? ""
                });
            }

            return new JsonResult(list);
        }

        // =========================
        // SERVIÇOS
        // =========================
        public IActionResult OnGetServicos()
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new JsonResult(Array.Empty<object>());

            var list = new List<object>();

            using var con = _dbHandler.GetConnection();

            // Ajuste: duração em minutos e valor padrão
            var sql = @"
    select 
        s.IdServico as Id,
        s.Nome as Nome,
        60 as DuracaoMin,
        s.Preco as ValorPadrao
    from CorteCor_Servico s
    where s.IdSalao = @IdSalao
    order by s.Nome;
    ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new
                {
                    id = rd["Id"].ToString(),
                    nome = rd["Nome"]?.ToString() ?? "",
                    duracaoMin = rd["DuracaoMin"] is DBNull ? 0 : Convert.ToInt32(rd["DuracaoMin"]),
                    valorPadrao = rd["ValorPadrao"] is DBNull ? "" : Convert.ToDecimal(rd["ValorPadrao"]).ToString("0.00", new CultureInfo("pt-BR"))
                });
            }

            return new JsonResult(list);
        }

        // =========================
        // EVENTS (Agendamentos)
        // =========================
        public IActionResult OnGetEvents(string start, string end, string? employeeId, string? status, string? q)
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new JsonResult(Array.Empty<object>());

            var dtStart = ParseDpDate(start);
            var dtEnd = ParseDpDate(end);

            employeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId.Trim();
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            var list = new List<object>();

            using var con = _dbHandler.GetConnection();

            // Retorna eventos que intersectam o range visível
            var sql = @"
    select
        a.IdAgendamento as Id,
        a.DataInicio as DataInicio,
        a.DataFim as DataFim,
        a.IdFuncionario as IdFuncionario,
        a.Status as Status,
        a.Observacao as Observacao,
        a.Valor as Valor,

        c.Nome as ClienteNome,
        s.Nome as ServicoNome
    from CorteCor_Agendamento a
    inner join CorteCor_Cliente c on c.IdCliente = a.IdCliente and c.IdSalao = @IdSalao
    inner join CorteCor_Servico s on s.IdServico = a.IdServico and s.IdSalao = @IdSalao
    where a.IdSalao = @IdSalao
      and a.DataInicio < @End
      and a.DataFim > @Start
    ";

            if (employeeId != null) sql += " and a.IdFuncionario = @IdFuncionario ";
            if (status != null) sql += " and a.Status = @Status ";
            if (q != null) sql += " and c.Nome like @Q ";

            sql += " order by a.DataInicio;";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@Start", dtStart);
            cmd.AddWithValue("@End", dtEnd);

            if (employeeId != null) cmd.AddWithValue("@IdFuncionario", employeeId);
            if (status != null) cmd.AddWithValue("@Status", status);
            if (q != null) cmd.AddWithValue("@Q", $"%{q}%");

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var id = rd["Id"].ToString()!;
                var ini = Convert.ToDateTime(rd["DataInicio"]);
                var fim = Convert.ToDateTime(rd["DataFim"]);
                var funcId = rd["IdFuncionario"].ToString()!;
                var st = rd["Status"]?.ToString() ?? "";
                var cliente = rd["ClienteNome"]?.ToString() ?? "";
                var servico = rd["ServicoNome"]?.ToString() ?? "";
                var valor = rd["Valor"] is DBNull ? "" : Convert.ToDecimal(rd["Valor"]).ToString("0.00", new CultureInfo("pt-BR"));

                // DayPilot: id, text, start, end, resource
                list.Add(new
                {
                    id,
                    text = $"{servico} - {cliente}",
                    start = ToIso(ini),
                    end = ToIso(fim),
                    resource = funcId,

                    status = st,
                    clientName = cliente,
                    serviceName = servico,
                    value = valor
                });
            }

            return new JsonResult(list);
        }

        // =========================
        // GET (Editar)
        // =========================
        public IActionResult OnGetGet(string id)
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new NotFoundResult();

            using var con = _dbHandler.GetConnection();

            var sql = @"
    select
        a.IdAgendamento as Id,
        a.DataInicio as DataInicio,
        a.DataFim as DataFim,
        a.IdFuncionario as IdFuncionario,
        a.IdCliente as IdCliente,
        a.IdServico as IdServico,
        a.Observacao as Observacao,
        a.Valor as Valor,
        a.Status as Status
    from CorteCor_Agendamento a
    where a.IdSalao = @IdSalao
      and a.IdAgendamento = @Id;
    ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@Id", id);

            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return new NotFoundResult();

            var ini = Convert.ToDateTime(rd["DataInicio"]);
            var fim = Convert.ToDateTime(rd["DataFim"]);

            return new JsonResult(new
            {
                id = rd["Id"].ToString(),
                start = ToIso(ini),
                end = ToIso(fim),
                employeeId = rd["IdFuncionario"].ToString(),
                clientId = rd["IdCliente"].ToString(),
                serviceId = rd["IdServico"].ToString(),
                obs = rd["Observacao"]?.ToString() ?? "",
                value = rd["Valor"] is DBNull ? "" : Convert.ToDecimal(rd["Valor"]).ToString("0.00", new CultureInfo("pt-BR")),
                status = rd["Status"]?.ToString() ?? "Pendente"
            });
        }

        // =========================
        // SAVE (Criar/Editar)
        // =========================
        public class SaveRequest
        {
            [JsonPropertyName("id")] public string? Id { get; set; }
            [JsonPropertyName("start")] public string Start { get; set; } = "";
            [JsonPropertyName("end")] public string End { get; set; } = ""; // ignorado (duração é pelo serviço)
            [JsonPropertyName("employeeId")] public string EmployeeId { get; set; } = "";
            [JsonPropertyName("clientId")] public string ClientId { get; set; } = "";
            [JsonPropertyName("serviceId")] public string ServiceId { get; set; } = "";
            [JsonPropertyName("obs")] public string? Obs { get; set; }
            [JsonPropertyName("value")] public string? Value { get; set; }
            [JsonPropertyName("status")] public string Status { get; set; } = "Pendente";
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostSave([FromBody] SaveRequest req)
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return BadRequest(new ErrorResponse { Message = "Salão inválido." });

            if (string.IsNullOrWhiteSpace(req.EmployeeId) ||
                string.IsNullOrWhiteSpace(req.ClientId) ||
                string.IsNullOrWhiteSpace(req.ServiceId) ||
                string.IsNullOrWhiteSpace(req.Status) ||
                string.IsNullOrWhiteSpace(req.Start))
            {
                return BadRequest(new ErrorResponse { Message = "Preencha Funcionário, Cliente, Serviço, Status e Início." });
            }

            var start = ParseDpDate(req.Start);

            using var con = _dbHandler.GetConnection();

            // 1) duração fixa pelo serviço
            var (duracaoMin, valorPadrao) = GetServicoInfo(con, idSalao, req.ServiceId);
            if (duracaoMin <= 0) return BadRequest(new ErrorResponse { Message = "Serviço sem duração configurada." });

            var end = start.AddMinutes(duracaoMin);

            // 2) (opcional) funcionário faz serviço
            var validateFuncionarioServico = true;
            if (validateFuncionarioServico)
            {
                try
                {
                    var ok = FuncionarioPodeExecutarServico(con, idSalao, req.EmployeeId, req.ServiceId);
                    if (!ok) return BadRequest(new ErrorResponse { Message = "Este funcionário não está vinculado a este serviço." });
                }
                catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
                {
                    // Se a tabela de vínculo não existir ainda, você pode:
                    // - criar a tabela, ou
                    // - desativar validateFuncionarioServico acima.
                    return BadRequest(new ErrorResponse { Message = "Tabela de vínculo Funcionário-Serviço não encontrada. Crie a tabela ou desative a validação no código." });
                }
            }

            // 3) valida horário de trabalho (se conseguir ler as janelas)
            var isWithin = IsWithinWorkHours(con, idSalao, req.EmployeeId, start, end);
            if (!isWithin)
                return BadRequest(new ErrorResponse { Message = "Horário fora da disponibilidade do funcionário." });

            // 4) valida sobreposição (bloquear 100%)
            var hasConflict = HasOverlap(con, idSalao, req.EmployeeId, start, end, req.Id);
            if (hasConflict)
                return BadRequest(new ErrorResponse { Message = "Conflito: já existe um agendamento nesse horário para este funcionário." });

            // 5) salvar (insert/update)
            var valor = ParseMoney(req.Value) ?? valorPadrao;

            if (string.IsNullOrWhiteSpace(req.Id))
            {
                // INSERT
                var sql = @"
    insert into CorteCor_Agendamento
    (
        IdSalao, IdFuncionario, IdCliente, IdServico,
        DataInicio, DataFim, Observacao, Valor, Status
    )
    values
    (
        @IdSalao, @IdFuncionario, @IdCliente, @IdServico,
        @DataInicio, @DataFim, @Observacao, @Valor, @Status
    );

    select cast(scope_identity() as int);
    ";
                using var cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.AddWithValue("@IdSalao", idSalao);
                cmd.AddWithValue("@IdFuncionario", req.EmployeeId);
                cmd.AddWithValue("@IdCliente", req.ClientId);
                cmd.AddWithValue("@IdServico", req.ServiceId);
                cmd.AddWithValue("@DataInicio", start);
                cmd.AddWithValue("@DataFim", end);
                cmd.AddWithValue("@Observacao", (object?)req.Obs ?? DBNull.Value);
                cmd.AddWithValue("@Valor", (object?)valor ?? DBNull.Value);
                cmd.AddWithValue("@Status", req.Status);

                var newId = Convert.ToInt32(cmd.ExecuteScalar());
                return new JsonResult(new { ok = true, id = newId });
            }
            else
            {
                // UPDATE
                var sql = @"
    update CorteCor_Agendamento
    set
        IdFuncionario = @IdFuncionario,
        IdCliente = @IdCliente,
        IdServico = @IdServico,
        DataInicio = @DataInicio,
        DataFim = @DataFim,
        Observacao = @Observacao,
        Valor = @Valor,
        Status = @Status
    where IdSalao = @IdSalao
      and IdAgendamento = @Id;
    ";
                using var cmd = con.CreateCommand();
                cmd.CommandText = sql;
                cmd.AddWithValue("@IdSalao", idSalao);
                cmd.AddWithValue("@Id", req.Id);
                cmd.AddWithValue("@IdFuncionario", req.EmployeeId);
                cmd.AddWithValue("@IdCliente", req.ClientId);
                cmd.AddWithValue("@IdServico", req.ServiceId);
                cmd.AddWithValue("@DataInicio", start);
                cmd.AddWithValue("@DataFim", end);
                cmd.AddWithValue("@Observacao", (object?)req.Obs ?? DBNull.Value);
                cmd.AddWithValue("@Valor", (object?)valor ?? DBNull.Value);
                cmd.AddWithValue("@Status", req.Status);

                var rows = cmd.ExecuteNonQuery();
                if (rows == 0) return BadRequest(new ErrorResponse { Message = "Agendamento não encontrado." });

                return new JsonResult(new { ok = true, id = req.Id });
            }
        }

        // =========================
        // DELETE (Excluir)
        // =========================
        public class DeleteRequest
        {
            [JsonPropertyName("id")] public string Id { get; set; } = "";
        }

        [ValidateAntiForgeryToken]
        public IActionResult OnPostDelete([FromBody] DeleteRequest req)
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return BadRequest(new ErrorResponse { Message = "Salão inválido." });
            if (string.IsNullOrWhiteSpace(req.Id)) return BadRequest(new ErrorResponse { Message = "Id inválido." });

            using var con = _dbHandler.GetConnection();

            // Hard delete. Se preferir soft delete, troque para update Status='Cancelado'.
            var sql = @"
    delete from CorteCor_Agendamento
    where IdSalao = @IdSalao
      and IdAgendamento = @Id;
    ";
            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@Id", req.Id);

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return BadRequest(new ErrorResponse { Message = "Agendamento não encontrado." });

            return new JsonResult(new { ok = true });
        }

        // =========================
        // TIMERANGES (bloqueios)
        // =========================
        // =========================
        // TIMERANGES (bloqueios)
        // =========================
        public IActionResult OnGetTimeRanges(string start, string end, string? employeeId)
        {
            var idSalao = GetIdSalao();
            if (idSalao <= 0) return new JsonResult(Array.Empty<object>());

            var dtStart = ParseDpDate(start).Date;
            var dtEnd = ParseDpDate(end).Date;

            employeeId = string.IsNullOrWhiteSpace(employeeId) ? null : employeeId.Trim();

            using var con = _dbHandler.GetConnection();

            // buscar funcionários (ou 1 específico)
            var funcs = GetFuncionariosComHorario(con, idSalao, employeeId);

            var ranges = new List<object>();

            // Para cada dia visível e funcionário, gera bloqueios fora da janela de trabalho
            for (var day = dtStart; day < dtEnd; day = day.AddDays(1))
            {
                foreach (var f in funcs)
                {
                    var ww = f.GetWorkWindow(day.DayOfWeek);
                    if (ww == null)
                    {
                        // dia OFF => bloqueia o dia inteiro
                        ranges.Add(new
                        {
                            start = ToIso(day.AddHours(0)),
                            end = ToIso(day.AddDays(1).AddHours(0)),
                            resource = f.IdFuncionario,
                            text = "Indisponível",
                            cssClass = "dp-off"
                        });
                        continue;
                    }

                    var (ini, fim) = ww.Value;

                    // bloqueio antes do início
                    if (ini > TimeSpan.Zero)
                    {
                        ranges.Add(new
                        {
                            start = ToIso(day.Add(TimeSpan.Zero)),
                            end = ToIso(day.Add(ini)),
                            resource = f.IdFuncionario,
                            text = "Indisponível",
                            cssClass = "dp-off"
                        });
                    }

                    // bloqueio após o fim
                    if (fim < TimeSpan.FromHours(24))
                    {
                        ranges.Add(new
                        {
                            start = ToIso(day.Add(fim)),
                            end = ToIso(day.AddDays(1).Add(TimeSpan.Zero)),
                            resource = f.IdFuncionario,
                            text = "Indisponível",
                            cssClass = "dp-off"
                        });
                    }
                }
            }

            return new JsonResult(ranges);
        }

        // =========================
        // Helpers: Conflito / Serviço / Horário / Vínculo
        // =========================

        private (int duracaoMin, decimal? valorPadrao) GetServicoInfo(IDbConnection con, int idSalao, string idServico)
        {
            var sql = @"
    select DuracaoMinutos, Valor
    from CorteCor_Servico
    where IdSalao = @IdSalao and IdServico = @IdServico;
    ";
            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@IdServico", idServico);

            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return (0, null);

            var dur = rd["DuracaoMinutos"] is DBNull ? 0 : Convert.ToInt32(rd["DuracaoMinutos"]);
            var val = rd["Valor"] is DBNull ? (decimal?)null : Convert.ToDecimal(rd["Valor"]);
            return (dur, val);
        }

        private bool FuncionarioPodeExecutarServico(IDbConnection con, int idSalao, string idFuncionario, string idServico)
        {
            // Ajuste o nome da tabela/colunas conforme seu padrão.
            var sql = @"
    select 1
    from CorteCor_FuncionarioServico fs
    where fs.IdSalao = @IdSalao
      and fs.IdFuncionario = @IdFuncionario
      and fs.IdServico = @IdServico;
    ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@IdFuncionario", idFuncionario);
            cmd.AddWithValue("@IdServico", idServico);

            var o = cmd.ExecuteScalar();
            return o != null;
        }

        private bool HasOverlap(IDbConnection con, int idSalao, string idFuncionario, DateTime start, DateTime end, string? ignoreId)
        {
            var sql = @"
    select count(1)
    from CorteCor_Agendamento a
    where a.IdSalao = @IdSalao
      and a.IdFuncionario = @IdFuncionario
      and a.Status <> 'Cancelado'
      and a.DataInicio < @End
      and a.DataFim > @Start
    ";
            if (!string.IsNullOrWhiteSpace(ignoreId))
                sql += " and a.IdAgendamento <> @IgnoreId ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            cmd.AddWithValue("@IdFuncionario", idFuncionario);
            cmd.AddWithValue("@Start", start);
            cmd.AddWithValue("@End", end);
            if (!string.IsNullOrWhiteSpace(ignoreId))
                cmd.AddWithValue("@IgnoreId", ignoreId);

            var c = Convert.ToInt32(cmd.ExecuteScalar());
            return c > 0;
        }

        private bool IsWithinWorkHours(IDbConnection con, int idSalao, string idFuncionario, DateTime start, DateTime end)
        {
            // Se não conseguir ler janela (por falta de colunas), vamos permitir.
            var f = (GetFuncionariosComHorario(con, idSalao, idFuncionario)).FirstOrDefault();
            if (f == null) return true;

            var ww = f.GetWorkWindow(start.DayOfWeek);
            if (ww == null) return false;

            var (ini, fim) = ww.Value;

            var day = start.Date;
            var winStart = day.Add(ini);
            var winEnd = day.Add(fim);

            return start >= winStart && end <= winEnd;
        }

        // =========================
        // Leitura do horário semanal do funcionário
        // (ajuste os nomes conforme seu DDL)
        // =========================
        private sealed class FuncHorario
        {
            public string IdFuncionario { get; set; } = "";
            public string Nome { get; set; } = "";

            // janelas por dia (null = folga)
            public (TimeSpan ini, TimeSpan fim)? Seg { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Ter { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Qua { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Qui { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Sex { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Sab { get; set; }
            public (TimeSpan ini, TimeSpan fim)? Dom { get; set; }

            public (TimeSpan ini, TimeSpan fim)? GetWorkWindow(DayOfWeek d) => d switch
            {
                DayOfWeek.Monday => Seg,
                DayOfWeek.Tuesday => Ter,
                DayOfWeek.Wednesday => Qua,
                DayOfWeek.Thursday => Qui,
                DayOfWeek.Friday => Sex,
                DayOfWeek.Saturday => Sab,
                DayOfWeek.Sunday => Dom,
                _ => null
            };
        }

        private List<FuncHorario> GetFuncionariosComHorario(IDbConnection con, int idSalao, string? idFuncionario)
        {
            var list = new List<FuncHorario>();

            // Ajuste aqui conforme seu DDL real do CorteCor_Funcionario.
            // Eu puxo "Nome" + campos de horário que podem ter nomes diferentes.
            var sql = @"
    select *
    from CorteCor_Funcionario f
    where f.IdSalao = @IdSalao
    ";
            if (!string.IsNullOrWhiteSpace(idFuncionario))
                sql += " and f.IdFuncionario = @IdFuncionario ";

            using var cmd = con.CreateCommand();
            cmd.CommandText = sql;
            cmd.AddWithValue("@IdSalao", idSalao);
            if (!string.IsNullOrWhiteSpace(idFuncionario))
                cmd.AddWithValue("@IdFuncionario", idFuncionario);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var fh = new FuncHorario
                {
                    IdFuncionario = SafeGet(rd, "IdFuncionario") ?? SafeGet(rd, "IDFuncionario") ?? "",
                    Nome = SafeGet(rd, "Nome") ?? ""
                };

                // tenta mapear janelas por múltiplos nomes possíveis
                fh.Seg = GetWorkWindowFromReader(rd, "Seg", "Segunda");
                fh.Ter = GetWorkWindowFromReader(rd, "Ter", "Terca", "Terça");
                fh.Qua = GetWorkWindowFromReader(rd, "Qua", "Quarta");
                fh.Qui = GetWorkWindowFromReader(rd, "Qui", "Quinta");
                fh.Sex = GetWorkWindowFromReader(rd, "Sex", "Sexta");
                fh.Sab = GetWorkWindowFromReader(rd, "Sab", "Sábado", "Sabado");
                fh.Dom = GetWorkWindowFromReader(rd, "Dom", "Domingo");

                list.Add(fh);
            }

            return list;
        }

        private static string? SafeGet(IDataReader rd, string name)
        {
            try
            {
                var v = rd[name];
                return v is DBNull ? null : v?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static TimeSpan? SafeGetTime(IDataReader rd, params string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var v = rd[n];
                    if (v is DBNull) continue;

                    if (v is TimeSpan ts) return ts;
                    if (TimeSpan.TryParse(v.ToString(), out var parsed)) return parsed;
                }
                catch { /* ignore */ }
            }
            return null;
        }

        private static bool? SafeGetBool(IDataReader rd, params string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var v = rd[n];
                    if (v is DBNull) continue;
                    if (v is bool b) return b;

                    if (int.TryParse(v.ToString(), out var i)) return i != 0;
                    if (bool.TryParse(v.ToString(), out var bb)) return bb;
                }
                catch { /* ignore */ }
            }
            return null;
        }

        private static (TimeSpan ini, TimeSpan fim)? GetWorkWindowFromReader(IDataReader rd, params string[] prefixes)
        {
            // procura padrão:
            // {Prefix}_Ativo / {Prefix}Ativo / Ativo{Prefix}
            // {Prefix}_Inicio / {Prefix}Inicio / Inicio{Prefix}
            // {Prefix}_Fim / {Prefix}Fim / Fim{Prefix}

            foreach (var p in prefixes)
            {
                var ativo = SafeGetBool(rd,
                    $"{p}_Ativo", $"{p}Ativo", $"Ativo{p}",
                    $"{p}_Trabalha", $"{p}Trabalha");

                var ini = SafeGetTime(rd,
                    $"{p}_Inicio", $"{p}Inicio", $"Inicio{p}",
                    $"{p}_HoraInicio", $"{p}HoraInicio");

                var fim = SafeGetTime(rd,
                    $"{p}_Fim", $"{p}Fim", $"Fim{p}",
                    $"{p}_HoraFim", $"{p}HoraFim");

                // Se existir flag e for false => folga
                if (ativo.HasValue && ativo.Value == false)
                    return null;

                // Se não tem ini/fim => não conseguimos mapear (não bloqueia)
                if (!ini.HasValue || !fim.HasValue)
                    continue;

                if (fim.Value <= ini.Value) return null;

                return (ini.Value, fim.Value);
            }

            // Não achou colunas compatíveis => não bloqueia (permite)
            return (TimeSpan.Zero, TimeSpan.FromHours(24));
        }
    }
}




