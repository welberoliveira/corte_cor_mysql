using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text;

namespace CorteCor.Pages
{
    public class EstabelecimentosModel : PageModel
    {
            private readonly IConfiguration _config;

            public EstabelecimentosModel(IConfiguration config)
            {
                _config = config;
            }

            // =========================
            // Config de exportação
            // =========================
            public int ExportMaxRows => 200000; // limite seguro
            public bool ExportTruncado { get; set; }

            // =========================
            // Controle: só lista após pesquisar
            // =========================
            [BindProperty(SupportsGet = true)]
            public bool Pesquisar { get; set; } = false;

            public bool PesquisarExecutado => Pesquisar;

            // =========================
            // Dropdowns
            // =========================
            public List<OptionItem> Cnaes { get; set; } = new();
            public List<OptionItem> Municipios { get; set; } = new();
            public List<OptionItem> Paises { get; set; } = new();
            public List<OptionItem> Motivos { get; set; } = new();
            public List<OptionItem> Naturezas { get; set; } = new();
            public List<OptionItem> Qualificacoes { get; set; } = new();

            public List<string> Ufs { get; } = new()
        {
            "AC","AL","AP","AM","BA","CE","DF","ES","GO","MA","MT","MS","MG","PA","PB","PR","PE","PI","RJ","RN","RS","RO","RR","SC","SP","SE","TO"
        };

            public List<OptionItem> SituacoesCadastrais { get; } = new()
        {
            new OptionItem(1,  "Nula"),
            new OptionItem(2,  "Ativa"),
            new OptionItem(3,  "Suspensa"),
            new OptionItem(4,  "Inapta"),
            new OptionItem(8,  "Baixada")
        };

            public List<OptionItem> Portes { get; } = new()
        {
            new OptionItem(0, "Não informado"),
            new OptionItem(1, "Micro empresa"),
            new OptionItem(3, "Empresa de pequeno porte"),
            new OptionItem(5, "Demais")
        };

            // =========================
            // Filtros (GET)
            // =========================
            [BindProperty(SupportsGet = true)] public string? RazaoSocial { get; set; }
            [BindProperty(SupportsGet = true)] public string? NomeFantasia { get; set; }
            [BindProperty(SupportsGet = true)] public string? CnpjBasico { get; set; }
            [BindProperty(SupportsGet = true)] public string? CnpjCompleto { get; set; }

            [BindProperty(SupportsGet = true)] public string? Uf { get; set; }
            [BindProperty(SupportsGet = true)] public int? IdentificadorMatrizFilial { get; set; }
            [BindProperty(SupportsGet = true)] public int? SituacaoCadastral { get; set; }
            [BindProperty(SupportsGet = true)] public int? PorteCodigo { get; set; }

            [BindProperty(SupportsGet = true)] public int? CnaeFiscalPrincipal { get; set; }
            [BindProperty(SupportsGet = true)] public int? MunicipioCodigo { get; set; }
            [BindProperty(SupportsGet = true)] public int? PaisCodigo { get; set; }
            [BindProperty(SupportsGet = true)] public int? MotivoSituacaoCodigo { get; set; }
            [BindProperty(SupportsGet = true)] public int? NaturezaJuridicaCodigo { get; set; }
            [BindProperty(SupportsGet = true)] public int? QualificacaoRespCodigo { get; set; }

            [BindProperty(SupportsGet = true)] public string? Email { get; set; }
            [BindProperty(SupportsGet = true)] public string? Telefone { get; set; }
            [BindProperty(SupportsGet = true)] public bool SomenteComEmail { get; set; }
            [BindProperty(SupportsGet = true)] public bool SomenteComTelefone { get; set; }

            [BindProperty(SupportsGet = true)] public string? Cep { get; set; }
            [BindProperty(SupportsGet = true)] public string? Bairro { get; set; }
            [BindProperty(SupportsGet = true)] public string? Logradouro { get; set; }
            [BindProperty(SupportsGet = true)] public string? Numero { get; set; }
            [BindProperty(SupportsGet = true)] public string? Complemento { get; set; }
            [BindProperty(SupportsGet = true)] public string? TipoLogradouro { get; set; }
            [BindProperty(SupportsGet = true)] public string? NomeCidadeExterior { get; set; }
            [BindProperty(SupportsGet = true)] public string? CnaeSecundaria { get; set; }

            [BindProperty(SupportsGet = true)] public DateTime? DataInicioDe { get; set; }
            [BindProperty(SupportsGet = true)] public DateTime? DataInicioAte { get; set; }
            [BindProperty(SupportsGet = true)] public DateTime? DataSituacaoDe { get; set; }
            [BindProperty(SupportsGet = true)] public DateTime? DataSituacaoAte { get; set; }

            [BindProperty(SupportsGet = true)] public string? SituacaoEspecial { get; set; }
            [BindProperty(SupportsGet = true)] public DateTime? DataSitEspecialDe { get; set; }
            [BindProperty(SupportsGet = true)] public DateTime? DataSitEspecialAte { get; set; }

            // Paginação
            [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
            [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 50;

            // Resultados
            public List<EstabelecimentoRow> Itens { get; set; } = new();
            public int Total { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)Total / Math.Max(1, PageSize));

            // Date strings (inputs)
            public string DataInicioDeString => DataInicioDe?.ToString("yyyy-MM-dd") ?? "";
            public string DataInicioAteString => DataInicioAte?.ToString("yyyy-MM-dd") ?? "";
            public string DataSituacaoDeString => DataSituacaoDe?.ToString("yyyy-MM-dd") ?? "";
            public string DataSituacaoAteString => DataSituacaoAte?.ToString("yyyy-MM-dd") ?? "";
            public string DataSitEspecialDeString => DataSitEspecialDe?.ToString("yyyy-MM-dd") ?? "";
            public string DataSitEspecialAteString => DataSitEspecialAte?.ToString("yyyy-MM-dd") ?? "";

            // =========================
            // GET principal
            // =========================
            public void OnGet()
            {
                AjustarPaginacao();

                using var conn = GetConnection();
                LoadDropdowns(conn);

                if (!Pesquisar)
                {
                    // Não listar nada no primeiro carregamento
                    Total = 0;
                    Itens = new();
                    ExportTruncado = false;
                    return;
                }

                var (whereSql, pars) = BuildWhere();
                Total = ExecuteCount(conn, whereSql, pars);
                ExportTruncado = Total > ExportMaxRows;

                Itens = ExecuteList(conn, whereSql, pars);
            }

            // =========================
            // EXPORT CSV
            // =========================
            public IActionResult OnGetExportCsv()
            {
                // sempre carrega dropdowns? não precisa
                using var conn = GetConnection();
                var (whereSql, pars) = BuildWhere();

                var rows = ExecuteExport(conn, whereSql, pars, ExportMaxRows);
                var sb = new StringBuilder();

                // CSV com ; (bom pro Excel PT-BR)
                sb.AppendLine(string.Join(";", new[]
                {
                "PossuiEmail","PossuiTelefone","Cnpj","RazaoSocial","NomeFantasia","MatrizFilial",
                "SituacaoCadastral","DataSituacaoCadastral","Motivo",
                "DataInicioAtividade","CnaePrincipal","CnaeDescricao","CnaeSecundaria",
                "Municipio","UF","CEP","TipoLogradouro","Logradouro","Numero","Complemento","Bairro",
                "Ddd1","Telefone1","Ddd2","Telefone2","Email"
            }.Select(Csv)));

                foreach (var r in rows)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                    Csv(r.PossuiEmail ? "Sim" : "Nao"),
                    Csv(r.PossuiTelefone ? "Sim" : "Nao"),
                    Csv(r.Cnpj),
                    Csv(r.RazaoSocial),
                    Csv(r.NomeFantasia),
                    Csv(r.IdentificadorMatrizFilial?.ToString() ?? ""),
                    Csv(r.SituacaoCadastral?.ToString() ?? ""),
                    Csv(r.DataSituacaoCadastral?.ToString("yyyy-MM-dd") ?? ""),
                    Csv(r.MotivoSituacaoDescricao),
                    Csv(r.DataInicioAtividade?.ToString("yyyy-MM-dd") ?? ""),
                    Csv(r.CnaeFiscalPrincipal?.ToString() ?? ""),
                    Csv(r.CnaeDescricao),
                    Csv(r.CnaeFiscalSecundaria),
                    Csv(r.MunicipioDescricao),
                    Csv(r.Uf),
                    Csv(r.Cep),
                    Csv(r.TipoLogradouro),
                    Csv(r.Logradouro),
                    Csv(r.Numero),
                    Csv(r.Complemento),
                    Csv(r.Bairro),
                    Csv(r.Ddd1),
                    Csv(r.Telefone1),
                    Csv(r.Ddd2),
                    Csv(r.Telefone2),
                    Csv(r.CorreioEletronico)
                }));
                }

                var bytes = WithUtf8Bom(sb.ToString());
                var fileName = $"estabelecimentos_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(bytes, "text/csv; charset=utf-8", fileName);
            }

            // =========================
            // EXPORT EXCEL (HTML .xls)
            // =========================
            public IActionResult OnGetExportExcel()
            {
                using var conn = GetConnection();
                var (whereSql, pars) = BuildWhere();
                var rows = ExecuteExport(conn, whereSql, pars, ExportMaxRows);

                var html = new StringBuilder();
                html.AppendLine("<html><head><meta charset='utf-8'></head><body>");
                html.AppendLine("<table border='1' cellpadding='3' cellspacing='0'>");

                html.AppendLine("<tr>");
                foreach (var h in new[]
                {
                "PossuiEmail","PossuiTelefone","Cnpj","RazaoSocial","NomeFantasia","MatrizFilial",
                "SituacaoCadastral","DataSituacaoCadastral","Motivo",
                "DataInicioAtividade","CnaePrincipal","CnaeDescricao","CnaeSecundaria",
                "Municipio","UF","CEP","TipoLogradouro","Logradouro","Numero","Complemento","Bairro",
                "Ddd1","Telefone1","Ddd2","Telefone2","Email"
            })
                    html.Append("<th>").Append(Html(h)).AppendLine("</th>");
                html.AppendLine("</tr>");

                foreach (var r in rows)
                {
                    html.AppendLine("<tr>");
                    html.Append("<td>").Append(Html(r.PossuiEmail ? "Sim" : "Nao")).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.PossuiTelefone ? "Sim" : "Nao")).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Cnpj)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.RazaoSocial)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.NomeFantasia)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.IdentificadorMatrizFilial?.ToString() ?? "")).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.SituacaoCadastral?.ToString() ?? "")).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.DataSituacaoCadastral?.ToString("yyyy-MM-dd") ?? "")).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.MotivoSituacaoDescricao)).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.DataInicioAtividade?.ToString("yyyy-MM-dd") ?? "")).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.CnaeFiscalPrincipal?.ToString() ?? "")).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.CnaeDescricao)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.CnaeFiscalSecundaria)).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.MunicipioDescricao)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Uf)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Cep)).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.TipoLogradouro)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Logradouro)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Numero)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Complemento)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Bairro)).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.Ddd1)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Telefone1)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Ddd2)).AppendLine("</td>");
                    html.Append("<td>").Append(Html(r.Telefone2)).AppendLine("</td>");

                    html.Append("<td>").Append(Html(r.CorreioEletronico)).AppendLine("</td>");
                    html.AppendLine("</tr>");
                }

                html.AppendLine("</table></body></html>");

                var bytes = Encoding.UTF8.GetBytes(html.ToString());
                var fileName = $"estabelecimentos_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                return File(bytes, "application/vnd.ms-excel; charset=utf-8", fileName);
            }

            // =========================
            // Helpers
            // =========================
            private void AjustarPaginacao()
            {
                if (Page < 1) Page = 1;
                if (PageSize <= 0) PageSize = 50;
                if (PageSize > 200) PageSize = 200;
            }

            private SqlConnection GetConnection()
            {
                var cs = _config.GetConnectionString("TonniDb");
                var conn = new SqlConnection(cs);
                conn.Open();
                return conn;
            }

            private void LoadDropdowns(SqlConnection conn)
            {
                Cnaes = LoadOptionTable(conn, "dbo.CNPJ_Cnaes");
                Municipios = LoadOptionTable(conn, "dbo.CNPJ_Municipios");
                Paises = LoadOptionTable(conn, "dbo.CNPJ_Paises");
                Motivos = LoadOptionTable(conn, "dbo.CNPJ_MotivosSituacaoCadastral");
                Naturezas = LoadOptionTable(conn, "dbo.CNPJ_NaturezasJuridicas");
                Qualificacoes = LoadOptionTable(conn, "dbo.CNPJ_Qualificacoes");
            }

            private List<OptionItem> LoadOptionTable(SqlConnection conn, string table)
            {
                var list = new List<OptionItem>();

                var sql = $@"
SELECT Codigo, Descricao
FROM {table}
ORDER BY Descricao;";

                using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300; // 300s = 5 minutos

            using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var codigo = rd["Codigo"] is DBNull ? 0 : Convert.ToInt32(rd["Codigo"]);
                    var desc = rd["Descricao"] is DBNull ? "" : rd["Descricao"].ToString()!;
                    list.Add(new OptionItem(codigo, desc));
                }

                return list;
            }

            // =========================
            // WHERE builder
            // =========================
            private (string whereSql, List<SqlParameter> pars) BuildWhere()
            {
                var sb = new StringBuilder(" WHERE 1=1 ");
                var pars = new List<SqlParameter>();

                void AddLike(string col, string? val, string p)
                {
                    if (string.IsNullOrWhiteSpace(val)) return;
                    sb.Append($" AND {col} LIKE @{p} ");
                    pars.Add(new SqlParameter("@" + p, $"%{val.Trim()}%"));
                }

                void AddEqInt(string col, int? val, string p)
                {
                    if (!val.HasValue) return;
                    sb.Append($" AND {col} = @{p} ");
                    pars.Add(new SqlParameter("@" + p, val.Value));
                }

                void AddEqByte(string col, int? val, string p)
                {
                    if (!val.HasValue) return;
                    sb.Append($" AND {col} = @{p} ");
                    pars.Add(new SqlParameter("@" + p, Convert.ToByte(val.Value)));
                }

                void AddEqStr(string col, string? val, string p)
                {
                    if (string.IsNullOrWhiteSpace(val)) return;
                    sb.Append($" AND {col} = @{p} ");
                    pars.Add(new SqlParameter("@" + p, val.Trim()));
                }

                void AddDateRange(string col, DateTime? de, DateTime? ate, string pDe, string pAte)
                {
                    if (de.HasValue)
                    {
                        sb.Append($" AND {col} >= @{pDe} ");
                        pars.Add(new SqlParameter("@" + pDe, de.Value.Date));
                    }
                    if (ate.HasValue)
                    {
                        sb.Append($" AND {col} <= @{pAte} ");
                        pars.Add(new SqlParameter("@" + pAte, ate.Value.Date));
                    }
                }

                AddLike("emp.RazaoSocial", RazaoSocial, "RazaoSocial");
                AddLike("est.NomeFantasia", NomeFantasia, "NomeFantasia");

                var cnpjBasicoNorm = NormalizeDigitsFixed(CnpjBasico, 8);
                if (!string.IsNullOrWhiteSpace(cnpjBasicoNorm))
                    AddEqStr("est.CnpjBasico", cnpjBasicoNorm, "CnpjBasico");

                var cnpj14 = NormalizeDigitsFixed(CnpjCompleto, 14);
                if (!string.IsNullOrWhiteSpace(cnpj14) && cnpj14.Length == 14)
                {
                    AddEqStr("est.CnpjBasico", cnpj14.Substring(0, 8), "CnpjBasico14");
                    AddEqStr("est.CnpjOrdem", cnpj14.Substring(8, 4), "CnpjOrdem14");
                    AddEqStr("est.CnpjDv", cnpj14.Substring(12, 2), "CnpjDv14");
                }

                if (!string.IsNullOrWhiteSpace(Uf))
                    AddEqStr("est.Uf", Uf.Trim().ToUpper(), "Uf");

                AddEqByte("est.IdentificadorMatrizFilial", IdentificadorMatrizFilial, "MatrizFilial");
                AddEqInt("est.SituacaoCadastral", SituacaoCadastral, "SituacaoCadastral");
                AddEqByte("emp.PorteCodigo", PorteCodigo, "PorteCodigo");

                AddEqInt("est.CnaeFiscalPrincipal", CnaeFiscalPrincipal, "CnaeFiscalPrincipal");
                AddEqInt("est.MunicipioCodigo", MunicipioCodigo, "MunicipioCodigo");
                AddEqInt("est.PaisCodigo", PaisCodigo, "PaisCodigo");
                AddEqInt("est.MotivoSituacaoCodigo", MotivoSituacaoCodigo, "MotivoSituacaoCodigo");

                AddEqInt("emp.NaturezaJuridicaCodigo", NaturezaJuridicaCodigo, "NaturezaJuridicaCodigo");
                AddEqInt("emp.QualificacaoRespCodigo", QualificacaoRespCodigo, "QualificacaoRespCodigo");

                AddLike("est.CorreioEletronico", Email, "Email");

                if (!string.IsNullOrWhiteSpace(Telefone))
                {
                    sb.Append(" AND (est.Telefone1 LIKE @Telefone OR est.Telefone2 LIKE @Telefone OR est.Fax LIKE @Telefone) ");
                    pars.Add(new SqlParameter("@Telefone", $"%{Telefone.Trim()}%"));
                }

                if (SomenteComEmail)
                    sb.Append(" AND est.CorreioEletronico IS NOT NULL AND LTRIM(RTRIM(est.CorreioEletronico)) <> '' ");

                if (SomenteComTelefone)
                {
                    sb.Append(@"
 AND (
        (est.Telefone1 IS NOT NULL AND LTRIM(RTRIM(est.Telefone1)) <> '')
     OR (est.Telefone2 IS NOT NULL AND LTRIM(RTRIM(est.Telefone2)) <> '')
 ) ");
                }

                var cepNorm = NormalizeDigitsFixed(Cep, 8);
                if (!string.IsNullOrWhiteSpace(cepNorm))
                    AddEqStr("est.Cep", cepNorm, "Cep");

                AddLike("est.Bairro", Bairro, "Bairro");
                AddLike("est.Logradouro", Logradouro, "Logradouro");
                AddLike("est.Numero", Numero, "Numero");
                AddLike("est.Complemento", Complemento, "Complemento");
                AddLike("est.TipoLogradouro", TipoLogradouro, "TipoLogradouro");
                AddLike("est.NomeCidadeExterior", NomeCidadeExterior, "NomeCidadeExterior");
                AddLike("est.CnaeFiscalSecundaria", CnaeSecundaria, "CnaeSecundaria");

                AddDateRange("est.DataInicioAtividade", DataInicioDe, DataInicioAte, "DataInicioDe", "DataInicioAte");
                AddDateRange("est.DataSituacaoCadastral", DataSituacaoDe, DataSituacaoAte, "DataSituacaoDe", "DataSituacaoAte");

                AddLike("est.SituacaoEspecial", SituacaoEspecial, "SituacaoEspecial");
                AddDateRange("est.DataSituacaoEspecial", DataSitEspecialDe, DataSitEspecialAte, "DataSitEspecialDe", "DataSitEspecialAte");

                return (sb.ToString(), pars);
            }

            private int ExecuteCount(SqlConnection conn, string whereSql, List<SqlParameter> pars)
            {
                var sql = @"
SELECT COUNT(1)
FROM dbo.CNPJ_Estabelecimentos est
INNER JOIN dbo.CNPJ_Empresas emp ON emp.CnpjBasico = est.CnpjBasico
" + whereSql;

                using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300; // 300s = 5 minutos

            cmd.Parameters.AddRange(CloneParams(pars));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            private List<EstabelecimentoRow> ExecuteList(SqlConnection conn, string whereSql, List<SqlParameter> pars)
            {
                var offset = (Page - 1) * PageSize;

                var sql = BaseSelectSql() + whereSql + @"
ORDER BY est.CnpjBasico, est.CnpjOrdem, est.CnpjDv
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300; // 300s = 5 minutos

            cmd.Parameters.AddRange(CloneParams(pars));
                cmd.Parameters.Add(new SqlParameter("@Offset", offset));
                cmd.Parameters.Add(new SqlParameter("@PageSize", PageSize));

                return ReadRows(cmd);
            }

            // Exporta TOP(@MaxRows)
            private List<EstabelecimentoRow> ExecuteExport(SqlConnection conn, string whereSql, List<SqlParameter> pars, int maxRows)
            {
                var sql = BaseSelectSql(top: true) + whereSql + @"
ORDER BY est.CnpjBasico, est.CnpjOrdem, est.CnpjDv;";

                using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300; // 300s = 5 minutos

            cmd.Parameters.AddRange(CloneParams(pars));
                cmd.Parameters.Add(new SqlParameter("@MaxRows", maxRows));

                return ReadRows(cmd);
            }

            private string BaseSelectSql(bool top = false)
            {
                var topSql = top ? "TOP (@MaxRows)" : "";

                return $@"
SELECT {topSql}
    est.CnpjBasico, est.CnpjOrdem, est.CnpjDv,

    emp.RazaoSocial,
    emp.PorteCodigo,

    est.IdentificadorMatrizFilial,
    est.NomeFantasia,
    est.SituacaoCadastral,
    est.DataSituacaoCadastral,
    est.MotivoSituacaoCodigo,

    est.DataInicioAtividade,
    est.CnaeFiscalPrincipal,
    est.CnaeFiscalSecundaria,

    est.TipoLogradouro,
    est.Logradouro,
    est.Numero,
    est.Complemento,
    est.Bairro,
    est.Cep,
    est.Uf,

    est.Ddd1, est.Telefone1,
    est.Ddd2, est.Telefone2,
    est.CorreioEletronico,

    cnae.Descricao AS CnaeDescricao,
    mun.Descricao  AS MunicipioDescricao,
    mot.Descricao  AS MotivoSituacaoDescricao

FROM dbo.CNPJ_Estabelecimentos est
INNER JOIN dbo.CNPJ_Empresas emp ON emp.CnpjBasico = est.CnpjBasico
LEFT JOIN dbo.CNPJ_Cnaes cnae ON cnae.Codigo = est.CnaeFiscalPrincipal
LEFT JOIN dbo.CNPJ_Municipios mun ON mun.Codigo = est.MunicipioCodigo
LEFT JOIN dbo.CNPJ_MotivosSituacaoCadastral mot ON mot.Codigo = est.MotivoSituacaoCodigo
";
            }

            private List<EstabelecimentoRow> ReadRows(SqlCommand cmd)
            {
                var list = new List<EstabelecimentoRow>();

                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    var email = rd["CorreioEletronico"] as string ?? "";
                    var tel1 = rd["Telefone1"]?.ToString() ?? "";
                    var tel2 = rd["Telefone2"]?.ToString() ?? "";

                    list.Add(new EstabelecimentoRow
                    {
                        CnpjBasico = rd["CnpjBasico"]?.ToString() ?? "",
                        CnpjOrdem = rd["CnpjOrdem"]?.ToString() ?? "",
                        CnpjDv = rd["CnpjDv"]?.ToString() ?? "",

                        RazaoSocial = rd["RazaoSocial"] as string ?? "",
                        PorteCodigo = rd["PorteCodigo"] is DBNull ? (byte?)null : Convert.ToByte(rd["PorteCodigo"]),

                        IdentificadorMatrizFilial = rd["IdentificadorMatrizFilial"] is DBNull ? (byte?)null : Convert.ToByte(rd["IdentificadorMatrizFilial"]),
                        NomeFantasia = rd["NomeFantasia"] as string ?? "",

                        SituacaoCadastral = rd["SituacaoCadastral"] is DBNull ? (int?)null : Convert.ToInt32(rd["SituacaoCadastral"]),
                        DataSituacaoCadastral = rd["DataSituacaoCadastral"] is DBNull ? (DateTime?)null : Convert.ToDateTime(rd["DataSituacaoCadastral"]),
                        MotivoSituacaoDescricao = rd["MotivoSituacaoDescricao"] as string ?? "",

                        DataInicioAtividade = rd["DataInicioAtividade"] is DBNull ? (DateTime?)null : Convert.ToDateTime(rd["DataInicioAtividade"]),
                        CnaeFiscalPrincipal = rd["CnaeFiscalPrincipal"] is DBNull ? (int?)null : Convert.ToInt32(rd["CnaeFiscalPrincipal"]),
                        CnaeDescricao = rd["CnaeDescricao"] as string ?? "",
                        CnaeFiscalSecundaria = rd["CnaeFiscalSecundaria"] as string ?? "",

                        TipoLogradouro = rd["TipoLogradouro"] as string ?? "",
                        Logradouro = rd["Logradouro"] as string ?? "",
                        Numero = rd["Numero"] as string ?? "",
                        Complemento = rd["Complemento"] as string ?? "",
                        Bairro = rd["Bairro"] as string ?? "",
                        Cep = rd["Cep"]?.ToString() ?? "",
                        Uf = rd["Uf"]?.ToString() ?? "",

                        MunicipioDescricao = rd["MunicipioDescricao"] as string ?? "",

                        Ddd1 = rd["Ddd1"]?.ToString() ?? "",
                        Telefone1 = tel1,
                        Ddd2 = rd["Ddd2"]?.ToString() ?? "",
                        Telefone2 = tel2,

                        CorreioEletronico = email,

                        PossuiEmail = !string.IsNullOrWhiteSpace(email),
                        PossuiTelefone = !string.IsNullOrWhiteSpace(tel1) || !string.IsNullOrWhiteSpace(tel2)
                    });
                }

                return list;
            }

            // =========================
            // URL: paginação / export
            // =========================
            public string BuildPageUrl(int page)
            {
                return BuildUrl(extra: new Dictionary<string, string?> {
                { nameof(Page), page.ToString() }
            });
            }

            public string BuildExportUrl(string handler)
            {
                // mantém todos os filtros, apenas troca handler
                var baseUrl = BuildUrl(extra: null);
                if (baseUrl.Contains("?"))
                    return baseUrl.Replace(Request.Path + "?", Request.Path + $"?handler={handler}&");
                return Request.Path + $"?handler={handler}";
            }

            private string BuildUrl(Dictionary<string, string?>? extra)
            {
                var q = new Dictionary<string, string?>();

                void Put(string k, string? v) { if (!string.IsNullOrWhiteSpace(v)) q[k] = v; }
                void PutN(string k, int? v) { if (v.HasValue) q[k] = v.Value.ToString(); }
                void PutB(string k, bool v) { if (v) q[k] = "true"; }

                PutB(nameof(Pesquisar), Pesquisar);

                Put(nameof(RazaoSocial), RazaoSocial);
                Put(nameof(NomeFantasia), NomeFantasia);
                Put(nameof(CnpjBasico), CnpjBasico);
                Put(nameof(CnpjCompleto), CnpjCompleto);

                Put(nameof(Uf), Uf);
                PutN(nameof(IdentificadorMatrizFilial), IdentificadorMatrizFilial);
                PutN(nameof(SituacaoCadastral), SituacaoCadastral);
                PutN(nameof(PorteCodigo), PorteCodigo);

                PutN(nameof(CnaeFiscalPrincipal), CnaeFiscalPrincipal);
                PutN(nameof(MunicipioCodigo), MunicipioCodigo);
                PutN(nameof(PaisCodigo), PaisCodigo);
                PutN(nameof(MotivoSituacaoCodigo), MotivoSituacaoCodigo);
                PutN(nameof(NaturezaJuridicaCodigo), NaturezaJuridicaCodigo);
                PutN(nameof(QualificacaoRespCodigo), QualificacaoRespCodigo);

                Put(nameof(Email), Email);
                Put(nameof(Telefone), Telefone);
                PutB(nameof(SomenteComEmail), SomenteComEmail);
                PutB(nameof(SomenteComTelefone), SomenteComTelefone);

                Put(nameof(Cep), Cep);
                Put(nameof(Bairro), Bairro);
                Put(nameof(Logradouro), Logradouro);
                Put(nameof(Numero), Numero);
                Put(nameof(Complemento), Complemento);
                Put(nameof(TipoLogradouro), TipoLogradouro);
                Put(nameof(NomeCidadeExterior), NomeCidadeExterior);
                Put(nameof(CnaeSecundaria), CnaeSecundaria);

                Put(nameof(DataInicioDe), DataInicioDe?.ToString("yyyy-MM-dd"));
                Put(nameof(DataInicioAte), DataInicioAte?.ToString("yyyy-MM-dd"));
                Put(nameof(DataSituacaoDe), DataSituacaoDe?.ToString("yyyy-MM-dd"));
                Put(nameof(DataSituacaoAte), DataSituacaoAte?.ToString("yyyy-MM-dd"));

                Put(nameof(SituacaoEspecial), SituacaoEspecial);
                Put(nameof(DataSitEspecialDe), DataSitEspecialDe?.ToString("yyyy-MM-dd"));
                Put(nameof(DataSitEspecialAte), DataSitEspecialAte?.ToString("yyyy-MM-dd"));

                q[nameof(PageSize)] = PageSize.ToString();
                q[nameof(Page)] = Page.ToString();

                if (extra != null)
                {
                    foreach (var kv in extra)
                        q[kv.Key] = kv.Value;
                }

                var query = string.Join("&", q.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value ?? "")}"
                ));

                return $"{Request.Path}?{query}";
            }

            private static string? NormalizeDigitsFixed(string? v, int len)
            {
                if (string.IsNullOrWhiteSpace(v)) return null;
                var s = new string(v.Where(char.IsDigit).ToArray());
                if (s.Length == 0) return null;

                if (s.Length >= len) return s.Substring(s.Length - len, len);
                return s.PadLeft(len, '0');
            }

            private static byte[] WithUtf8Bom(string text)
            {
                var bom = Encoding.UTF8.GetPreamble();
                var data = Encoding.UTF8.GetBytes(text);
                var bytes = new byte[bom.Length + data.Length];
                Buffer.BlockCopy(bom, 0, bytes, 0, bom.Length);
                Buffer.BlockCopy(data, 0, bytes, bom.Length, data.Length);
                return bytes;
            }

            private static string Csv(string? v)
            {
                v ??= "";
                v = v.Replace("\r", " ").Replace("\n", " ");
                // sempre entre aspas e escapa aspas
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }

            private static string Html(string? v)
            {
                if (string.IsNullOrEmpty(v)) return "";
                return v.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;");
            }

            // =========================
            // Models internos
            // =========================
            public class OptionItem
            {
                public int Codigo { get; set; }
                public string Descricao { get; set; } = "";
                public OptionItem() { }
                public OptionItem(int codigo, string descricao) { Codigo = codigo; Descricao = descricao; }
            }

            public class EstabelecimentoRow
            {
                public string CnpjBasico { get; set; } = "";
                public string CnpjOrdem { get; set; } = "";
                public string CnpjDv { get; set; } = "";
                public string Cnpj => $"{CnpjBasico}{CnpjOrdem}{CnpjDv}";

                public string RazaoSocial { get; set; } = "";
                public byte? PorteCodigo { get; set; }

                public byte? IdentificadorMatrizFilial { get; set; }
                public string NomeFantasia { get; set; } = "";

                public int? SituacaoCadastral { get; set; }
                public DateTime? DataSituacaoCadastral { get; set; }
                public string MotivoSituacaoDescricao { get; set; } = "";

                public DateTime? DataInicioAtividade { get; set; }
                public int? CnaeFiscalPrincipal { get; set; }
                public string CnaeDescricao { get; set; } = "";
                public string CnaeFiscalSecundaria { get; set; } = "";

                public string TipoLogradouro { get; set; } = "";
                public string Logradouro { get; set; } = "";
                public string Numero { get; set; } = "";
                public string Complemento { get; set; } = "";
                public string Bairro { get; set; } = "";
                public string Cep { get; set; } = "";
                public string Uf { get; set; } = "";

                public string MunicipioDescricao { get; set; } = "";

                public string Ddd1 { get; set; } = "";
                public string Telefone1 { get; set; } = "";
                public string Ddd2 { get; set; } = "";
                public string Telefone2 { get; set; } = "";

                public string CorreioEletronico { get; set; } = "";

                public bool PossuiEmail { get; set; }
                public bool PossuiTelefone { get; set; }
            }

        private static SqlParameter[] CloneParams(IEnumerable<SqlParameter> pars)
        {
            return pars.Select(p =>
            {
                var c = new SqlParameter
                {
                    ParameterName = p.ParameterName,
                    SqlDbType = p.SqlDbType,
                    Direction = p.Direction,
                    Size = p.Size,
                    Precision = p.Precision,
                    Scale = p.Scale,
                    IsNullable = p.IsNullable,
                    Value = p.Value ?? DBNull.Value
                };

                if (!string.IsNullOrWhiteSpace(p.TypeName))
                    c.TypeName = p.TypeName;

                return c;
            }).ToArray();
        }

    }


}
