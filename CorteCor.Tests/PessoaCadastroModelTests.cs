using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using CorteCor;
using CorteCor.Handlers;
using CorteCor.Pages;
using CorteCor.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CorteCor.Tests
{
    public class PessoaCadastroModelTests
    {
        [Fact]
        public void OnPost_DeveBloquearTelefoneInvalido()
        {
            var consultaService = new ConsultaDocumentoService(new HttpClient());
            var mockPessoaHandler = new Mock<PessoaHandler>((IDatabaseHandler)null!);
            var pageModel = new PessoaCadastroModel(consultaService, mockPessoaHandler.Object);
            pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = CriarHttpContext(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    ["nome"] = "Maria",
                    ["telefone"] = "123",
                    ["email"] = "maria@test.com",
                    ["isCliente"] = "on"
                })
            };

            var result = pageModel.OnPost();

            Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
            Assert.Equal("Informe um telefone com DDD valido.", pageModel.Mensagem);
            Assert.Equal("warning", pageModel.MensagemTipo);
        }

        [Fact]
        public void OnPost_DeveExigirTipoDeContato()
        {
            var consultaService = new ConsultaDocumentoService(new HttpClient());
            var mockPessoaHandler = new Mock<PessoaHandler>((IDatabaseHandler)null!);
            var pageModel = new PessoaCadastroModel(consultaService, mockPessoaHandler.Object);
            pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = CriarHttpContext(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    ["nome"] = "Maria",
                    ["telefone"] = "(38) 99999-9999"
                })
            };

            var result = pageModel.OnPost();

            Assert.IsType<Microsoft.AspNetCore.Mvc.RazorPages.PageResult>(result);
            Assert.Equal("Selecione pelo menos um tipo de contato.", pageModel.Mensagem);
            Assert.Equal("warning", pageModel.MensagemTipo);
        }

        private static DefaultHttpContext CriarHttpContext(Dictionary<string, Microsoft.Extensions.Primitives.StringValues> formValues)
        {
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("IdSalao", "1"),
                new Claim("Role", "Usuario")
            }, "mock"));
            context.Request.Form = new FormCollection(formValues);
            return context;
        }
    }
}
