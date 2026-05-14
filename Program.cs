using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using CorteCor;
using CorteCor.Services;
using CorteCor.Handlers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

const long LargeUploadLimitBytes = 300L * 1024 * 1024;
const int LargeTextValueLimitBytes = 10 * 1024 * 1024;

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("CorteCor");

// Adiciona serviços ao container.
builder.Services.AddRazorPages();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = LargeUploadLimitBytes;
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = LargeUploadLimitBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = LargeUploadLimitBytes;
    options.ValueLengthLimit = LargeTextValueLimitBytes;
    options.MultipartHeadersLengthLimit = 64 * 1024;
    options.ValueCountLimit = 8192;
});

// Configura autenticação baseada em cookies.
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Index"; // Redireciona para a página de login
        config.LogoutPath = "/Logout"; // Define a rota de logout
        config.Cookie.HttpOnly = true;
        config.Cookie.SameSite = SameSiteMode.Lax;
        config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        config.SlidingExpiration = true;
        //config.AccessDeniedPath = "/AccessDenied"; // Página para acesso negado (opcional)
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
    policy.RequireClaim("Role", "Admin")
          .RequireClaim("IdSalao"));


    options.AddPolicy("UsuarioPolicy", policy =>
        policy.RequireClaim("Role", "Usuario")
            .RequireClaim("IdSalao"));
});

//inserido para o portalde cartas contempladas 
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("https://portalcontempladas.com.br")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDatabaseHandler, DatabaseHandler>();
builder.Services.AddScoped<SalaoHandler>();
builder.Services.AddScoped<ServicoHandler>();
builder.Services.AddScoped<PessoaHandler>();
builder.Services.AddScoped<ProdutoHandler>();
builder.Services.AddScoped<ImovelHandler>();
builder.Services.AddScoped<CategoriaProdutoHandler>();
builder.Services.AddScoped<ItemListaServicoHandler>();
builder.Services.AddScoped<AgendamentoHandler>();
builder.Services.AddScoped<FuncionarioHandler>();
builder.Services.AddScoped<FuncionarioServicoHandler>();
builder.Services.AddScoped<SalaoConfigFiscalHandler>();
builder.Services.AddScoped<NotaFiscalHandler>();
builder.Services.AddScoped<NotaFiscalLogHandler>();
builder.Services.AddScoped<PagamentoHandler>();
builder.Services.AddScoped<FinanceiroHandler>();
builder.Services.AddScoped<IFinanceiroModuloHandler, FinanceiroModuloHandler>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddScoped<RelatorioCentralService>();
builder.Services.AddScoped<IntegracaoHandler>();
builder.Services.AddScoped<MercadoPagoService>();
builder.Services.AddScoped<ModeloEmailHandler>();
builder.Services.AddScoped<ModeloSMSHandler>();
builder.Services.AddScoped<MeioPagamentoHandler>();
builder.Services.AddHttpClient<BrevoEmailService>();
//builder.Services.AddScoped<BrevoEmailService>(); // Removido pois AddHttpClient já registra
builder.Services.AddHttpClient<SMSMarketService>();
builder.Services.AddHttpClient<IWhatsappService, WhatsappService>();
builder.Services.AddScoped<ILembreteHandler, LembreteHandler>();
builder.Services.AddScoped<LembreteService>();
builder.Services.AddScoped<FornecedoresHandler>();
builder.Services.AddScoped<ICrmHandler, CrmHandler>();
builder.Services.AddScoped<CrmService>();
builder.Services.AddScoped<PedidoHandler>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<VendaEstoqueHandler>();
builder.Services.AddScoped<VendaFiscalPreparationService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddHttpClient<ConsultaDocumentoService>();
builder.Services.AddScoped<LogAcessoHandler>();


// Serviços Fiscais
builder.Services.AddSingleton<ICriptografiaService, CriptografiaService>();
builder.Services.AddTransient<CertificadoFiscalFactory>();
builder.Services.AddTransient<NFCeEmissorService>();
builder.Services.AddTransient<NFSeEmissorService>();
builder.Services.AddTransient<FiscalBuilderService>();
builder.Services.AddTransient<FiscalActionService>();
builder.Services.AddTransient<FiscalPdfGenerator>();
builder.Services.AddTransient<FiscalOrigemPreparationService>();
builder.Services.AddTransient<AgendamentoPreparationService>();
builder.Services.AddTransient<AgendamentoFiscalPreparationService>();
builder.Services.AddTransient<IValidaParametrosMunicipioService, ValidaParametrosMunicipioService>();
builder.Services.AddTransient<NotaFiscalAvulsaService>();
builder.Services.AddScoped<NotaFiscalEventoHandler>();
builder.Services.AddScoped<NotaFiscalInutilizacaoHandler>();

if (BancoConfigurado(builder.Configuration) &&
    (!builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("BackgroundJobs:Enabled")))
{
    builder.Services.AddHostedService<LembreteBackgroundService>();
    builder.Services.AddHostedService<NFSeVerificadorRetornoJob>();
}

// Configurar cultura para pt-BR
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var app = builder.Build();

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureInfo),
    SupportedCultures = new List<CultureInfo> { cultureInfo },
    SupportedUICultures = new List<CultureInfo> { cultureInfo }
};

app.UseRequestLocalization(localizationOptions);


var currentPath = AppDomain.CurrentDomain.BaseDirectory;

//>>> incluido para ter duas aplicações ao mesmo tempo Verifica se o PathBase precisa ser definido automaticamente
var pathBase = AppDomain.CurrentDomain.BaseDirectory.Contains("tonni") ? "/cortecor" : "";


if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// Certifica que cada aplicação usa seu próprio diretório de logs e dados temporários
var tempPath = Path.Combine(Path.GetTempPath(), pathBase.TrimStart('/'));
Directory.CreateDirectory(tempPath);
Environment.SetEnvironmentVariable("DOTNET_BUNDLE_EXTRACT_BASE_DIR", tempPath);
//<<< incluido para ter duas aplicações ao mesmo tempo 

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (DatabaseConnectionException)
    {
        if (context.Response.HasStarted)
        {
            throw;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;

        var wantsJson = context.Request.Headers.Accept.Any(header =>
            header?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);

        if (wantsJson)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Ocorreu um erro."
            }));
            return;
        }

        var retryForm = await BuildRetryFormAsync(context);

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync($$"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Ocorreu um erro</title>
                <style>
                    body { margin: 0; font-family: Arial, sans-serif; background: #f3f4f6; color: #111827; }
                    .db-error-page { min-height: 100vh; display: grid; place-items: center; padding: 24px; }
                    .db-error-box { width: min(460px, 100%); background: #fff; border: 1px solid #dbeafe; border-radius: 12px; box-shadow: 0 14px 35px rgba(15, 23, 42, .12); padding: 32px; text-align: center; }
                    h1 { margin: 0 0 22px; font-size: 1.55rem; color: #0f172a; }
                    .retry-button { display: inline-flex; align-items: center; justify-content: center; min-width: 180px; border: 0; border-radius: 8px; background: #0d6efd; color: #fff; font-size: 1rem; font-weight: 700; padding: 12px 18px; cursor: pointer; }
                    .retry-button:hover, .retry-button:focus { background: #0b5ed7; }
                </style>
            </head>
            <body>
                <main class="db-error-page">
                    <section class="db-error-box">
                        <h1>Ocorreu um erro.</h1>
                        {{retryForm}}
                    </section>
                </main>
            </body>
            </html>
            """);
    }
});

//inserido para o portalde cartas contempladas 
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].ToString();
    var path = context.Request.Path.ToString();

    // Verifica se a requisição é para "/axe/API/PortalContempladas"
    if (path.StartsWith("/CorteCor/API/PortalContempladas", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrEmpty(origin) || origin != "https://portalcontempladas.com.br")
        {
            context.Response.StatusCode = 403; // Proibido
            await context.Response.WriteAsync("? Acesso não autorizado.");
            return;
        }
    }

    await next(); // Permite a requisição continuar se não for bloqueada
});






//inserido para o portalde cartas contempladas 
app.UseCors(MyAllowSpecificOrigins);



// Configuração do pipeline de requisições.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}




////inserido para o portalde cartas contempladas 
//app.Use(async (context, next) =>
//{
//    context.Response.Headers.Remove("X-Frame-Options");
//    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN"); 
//    context.Response.Headers.Add("Content-Security-Policy", "frame-ancestors 'self' https://portalcontempladas.com.br;");

//    await next();
//});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

static bool BancoConfigurado(ConfigurationManager configuration)
{
    var configuredConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__TonniDb")
        ?? configuration.GetConnectionString("TonniDb");

    var connectionString = DatabaseHandler.ResolverConnectionString(configuredConnectionString);

    return !string.IsNullOrWhiteSpace(connectionString);
}

static async Task<string> BuildRetryFormAsync(HttpContext context)
{
    var path = $"{context.Request.PathBase}{context.Request.Path}";
    var fullPath = $"{path}{context.Request.QueryString}";
    var fields = new StringBuilder();
    var method = HttpMethods.IsPost(context.Request.Method) ? "post" : "get";
    var action = method == "post" ? fullPath : path;

    if (method == "post" && context.Request.HasFormContentType)
    {
        try
        {
            var form = await context.Request.ReadFormAsync();
            foreach (var item in form)
            {
                foreach (var value in item.Value)
                {
                    fields.AppendLine(BuildHiddenInput(item.Key, value ?? string.Empty));
                }
            }
        }
        catch
        {
            // Se o corpo do request nao puder ser relido, o botao ainda reenvia a rota atual.
        }
    }
    else
    {
        foreach (var item in context.Request.Query)
        {
            foreach (var value in item.Value)
            {
                fields.AppendLine(BuildHiddenInput(item.Key, value ?? string.Empty));
            }
        }
    }

    return $"""
        <form method="{method}" action="{Html(action)}">
            {fields}
            <button class="retry-button" type="submit">tentar novamente</button>
        </form>
        """;
}

static string BuildHiddenInput(string name, string value) =>
    $"""<input type="hidden" name="{Html(name)}" value="{Html(value)}">""";

static string Html(string value) => WebUtility.HtmlEncode(value);


