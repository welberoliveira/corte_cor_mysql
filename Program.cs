using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using CorteCor;
using CorteCor.Services;
using CorteCor.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Adiciona serviços ao container.
builder.Services.AddRazorPages();

// Configura autenticação baseada em cookies.
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Index"; // Redireciona para a página de login
        config.LogoutPath = "/Logout"; // Define a rota de logout
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
builder.Services.AddScoped<Salaoervice>();
builder.Services.AddScoped<ServicoHandler>();
builder.Services.AddScoped<PessoaHandler>();
builder.Services.AddScoped<AgendamentoHandler>();
builder.Services.AddScoped<FuncionarioHandler>();
builder.Services.AddScoped<FuncionarioServicoHandler>();
builder.Services.AddScoped<SalaoConfigFiscalHandler>();
builder.Services.AddScoped<NotaFiscalHandler>();
builder.Services.AddScoped<PagamentoHandler>();
builder.Services.AddScoped<MercadoPagoService>();
builder.Services.AddScoped<ModeloEmailHandler>();
builder.Services.AddScoped<ModeloSMSHandler>();
builder.Services.AddScoped<MeioPagamentoHandler>();
builder.Services.AddHttpClient<BrevoEmailService>();
//builder.Services.AddScoped<BrevoEmailService>(); // Removido pois AddHttpClient já registra
builder.Services.AddHttpClient<SMSMarketService>();
builder.Services.AddScoped<ILembreteHandler, LembreteHandler>();
builder.Services.AddScoped<LembreteService>();
builder.Services.AddScoped<FornecedoresHandler>();
builder.Services.AddHostedService<LembreteBackgroundService>();


// Serviços Fiscais
builder.Services.AddSingleton<ICriptografiaService, CriptografiaService>();
builder.Services.AddTransient<CertificadoFiscalFactory>();
builder.Services.AddTransient<NFCeEmissorService>();
builder.Services.AddTransient<NFSeEmissorService>();
builder.Services.AddTransient<FiscalActionService>();
builder.Services.AddTransient<FiscalPdfGenerator>();
builder.Services.AddScoped<NotaFiscalEventoHandler>();
builder.Services.AddScoped<NotaFiscalInutilizacaoHandler>();
builder.Services.AddHostedService<NFSeVerificadorRetornoJob>();

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

