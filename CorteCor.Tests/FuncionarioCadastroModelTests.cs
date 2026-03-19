using System.Collections.Generic;
using System.Security.Claims;
using CorteCor;
using CorteCor.Handlers;
using CorteCor.Pages;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CorteCor.Tests
{
    public class FuncionarioCadastroModelTests
    {
        [Fact]
        public void OnPost_DeveExigirAoMenosUmDiaSelecionado()
        {
            var mockHandler = new Mock<FuncionarioHandler>((IDatabaseHandler)null!);
            var pageModel = new FuncionarioCadastroModel(mockHandler.Object);
            pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = CriarHttpContext(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    ["nome"] = "Joao"
                })
            };

            pageModel.OnPost();

            Assert.Equal("Selecione pelo menos um dia de atendimento.", pageModel.Mensagem);
            Assert.Equal("warning", pageModel.MensagemTipo);
        }

        [Fact]
        public void OnPost_DeveValidarHorarioInicialMenorQueFinal()
        {
            var mockHandler = new Mock<FuncionarioHandler>((IDatabaseHandler)null!);
            var pageModel = new FuncionarioCadastroModel(mockHandler.Object);
            pageModel.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext
            {
                HttpContext = CriarHttpContext(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
                {
                    ["nome"] = "Joao",
                    ["seg"] = "on",
                    ["seg_ini"] = "18:00",
                    ["seg_fim"] = "09:00"
                })
            };

            pageModel.OnPost();

            Assert.Equal("O horario de inicio deve ser menor que o horario de fim em segunda-feira.", pageModel.Mensagem);
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
