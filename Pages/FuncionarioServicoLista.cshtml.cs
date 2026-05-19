using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [TempData]
        public string Mensagem { get; set; }
        [TempData]
        public string MensagemTipo { get; set; } = "info";

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

            // Relações N:N (todos) e filtra por funcionário/serviço do salão
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

        // Opcional (se você colocar botões na tela):
        // action=remover precisa mandar idFuncionario e idServico no form.
        public IActionResult OnPost()
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

                    // remove a relação (IdFuncionario, IdServico)
                    fsHandler.Desvincular(idFuncionario, idServico);

                    Mensagem = "Vinculo removido com sucesso.";
                    MensagemTipo = "success";
                }
                catch (Exception)
                {
                    Mensagem = "Nao foi possivel remover o vinculo.";
                    MensagemTipo = "danger";
                }
            }
            else if (action == "alterar")
            {
                int idFuncionario = 0;
                int.TryParse(Request.Form["idFuncionario"], out idFuncionario);

                return RedirectToPage("/FuncionarioServicoCadastro", new { idFuncionario });
            }

            return RedirectToPage();
        }
    }
}

