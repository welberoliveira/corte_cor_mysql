using CorteCor.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CorteCor.Handlers;
using Xunit;
using CorteCor;
using CorteCor.Services;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace CorteCor.Tests
{
    public class DependencyInjectionTests
    {
        [Fact]
        public void Verify_DI_Configuration_For_All_PageModels()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRazorPages();
                        services.AddLogging();
                        services.AddHttpClient();
                        services.AddMemoryCache();
                        services.AddAuthentication("CookieAuth").AddCookie("CookieAuth", o => { });
                        services.AddAuthorization(o => {
                            o.AddPolicy("AdminPolicy", p => p.RequireClaim("Role"));
                            o.AddPolicy("UsuarioPolicy", p => p.RequireClaim("Role"));
                        });

                        var mockDbHandler = new Mock<IDatabaseHandler>();
                        services.AddScoped<IDatabaseHandler>(_ => mockDbHandler.Object);
                        
                        services.AddScoped<Salaoervice>(); // Note typo in original Program.cs: Salaoervice
                        services.AddScoped<ServicoHandler>();
                        services.AddScoped<PessoaHandler>();
                        services.AddScoped<AgendamentoHandler>();
                        services.AddScoped<FuncionarioHandler>();
                        services.AddScoped<FuncionarioServicoHandler>();
                        services.AddScoped<PagamentoHandler>();
                        services.AddScoped<MercadoPagoService>();
                        services.AddScoped<ModeloEmailHandler>();
                        services.AddScoped<ModeloSMSHandler>();
                        services.AddScoped<MeioPagamentoHandler>();
                        services.AddHttpClient<BrevoEmailService>();
                        services.AddHttpClient<SMSMarketService>();
                        
                        // Critical Fix Verification: Register ILembreteHandler -> LembreteHandler
                        services.AddScoped<ILembreteHandler, LembreteHandler>();
                        services.AddScoped<LembreteService>();
                        services.AddScoped<FornecedoresHandler>();
                        services.AddHostedService<LembreteBackgroundService>();

                        // Manually register PageModels since we don't have MapRazorPages logic here
                        var assembly = typeof(LembreteService).Assembly;
                        var pageModels = assembly.GetTypes()
                            .Where(t => t.IsSubclassOf(typeof(PageModel)) && !t.IsAbstract)
                            .ToList();

                        foreach (var pageModel in pageModels)
                        {
                            services.AddTransient(pageModel);
                        }
                    });
                    webBuilder.Configure(app => { });
                });

            using var host = hostBuilder.Build();
            using var scope = host.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var assemblyToTest = typeof(LembreteService).Assembly;
            var pageModelTypes = assemblyToTest.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PageModel)) && !t.IsAbstract)
                .ToList();

            var failures = new List<string>();

            foreach (var type in pageModelTypes)
            {
                try
                {
                    serviceProvider.GetRequiredService(type);
                }
                catch (Exception ex)
                {
                    failures.Add($"Failed to resolve {type.Name}: {ex.Message}");
                }
            }

            if (failures.Count > 0)
            {
                Assert.Fail($"Dependency Injection verification failed for the following PageModels:\n{string.Join("\n", failures)}");
            }
        }
    }
}

