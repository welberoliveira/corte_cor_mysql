using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioServicoListaModel : PageModel
    {
        public class FuncionarioServicoListaItem
        {
            public int IdFuncionario { get; set; }
            public string FuncionarioNome { get; set; }

            public int IdServico { get; set; }
            public string ServicoNome { get; set; }
            public decimal ServicoPreco { get; set; }
            
        }

        public List<FuncionarioServicoListaItem> Itens { get; set; } = new();
        public string Mensagem { get; set; }

        public void OnGet()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            var funcionarioHandler = new FuncionarioHandler();
            var servicoHandler = new ServicoHandler();
            var fsHandler = new FuncionarioServicoHandler();

            var funcionariosSalao = (funcionarioHandler.ListarPorSalao(idSalao) ?? new List<Funcionario>())
                .Where(f => f.IdSalao == idSalao)
                .ToList();

            var servicosSalao = (servicoHandler.Listar() ?? new List<Servico>())
                .Where(s => s.IdSalao == idSalao)
                .ToList();

            // Rela??es N:N (todos) e filtra por funcion?rio/servi?o do sal?o
            var relacoes = fsHandler.Listar() ?? new List<FuncionarioServico>();

            var funcionariosDict = funcionariosSalao.ToDictionary(x => x.IdFuncionario, x => x);
            var servicosDict = servicosSalao.ToDictionary(x => x.IdServico, x => x);

            Itens = relacoes
                .Where(r => funcionariosDict.ContainsKey(r.IdFuncionario) && servicosDict.ContainsKey(r.IdServico))
                .Select(r =>
                {
                    var f = funcionariosDict[r.IdFuncionario];
                    var s = servicosDict[r.IdServico];

                    return new FuncionarioServicoListaItem
                    {
                        IdFuncionario = f.IdFuncionario,
                        FuncionarioNome = f.Nome,

                        IdServico = s.IdServico,
                        ServicoNome = s.Nome,
                        ServicoPreco = s.Preco,
                        
                    };
                })
                .OrderBy(x => x.FuncionarioNome)
                .ThenBy(x => x.ServicoNome)
                .ToList();
        }

        // Opcional (se voc? colocar bot?es na tela):
        // action=remover precisa mandar idFuncionario e idServico no form.
        public void OnPost()
        {
            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            string action = Request.Form["action"];

            if (action == "remover")
            {
                int idFuncionario = 0;
                int idServico = 0;
                int.TryParse(Request.Form["idFuncionario"], out idFuncionario);
                int.TryParse(Request.Form["idServico"], out idServico);

                try
                {
                    var fsHandler = new FuncionarioServicoHandler();

                    // remove a rela??o (IdFuncionario, IdServico)
                    fsHandler.Desvincular(idFuncionario, idServico);

                    Mensagem = "V?nculo removido com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "N?o foi poss?vel remover o v?nculo.";
                }
            }
            else if (action == "alterar")
            {
                int idFuncionario = 0;
                int.TryParse(Request.Form["idFuncionario"], out idFuncionario);

                Response.Redirect(HttpContext.Request.PathBase + $"/FuncionarioServicoCadastro?idFuncionario={idFuncionario}");
                return;
            }

            OnGet();
        }
    }
}

