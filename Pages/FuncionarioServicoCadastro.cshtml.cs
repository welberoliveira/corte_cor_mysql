using CorteCor.Models;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;


namespace CorteCor.Pages
{
    [Authorize(Policy = "UsuarioPolicy")]
    public class FuncionarioServicoCadastroModel : PageModel
    {
        public List<Funcionario> Funcionarios { get; set; } = new();
        public List<Servico> Servicos { get; set; } = new();

        public bool IsLockedEmployee { get; set; }

        // Para manter selecionado após post/edição
        public int? IdFuncionarioSelecionado { get; set; }
        public List<int> ServicosSelecionadosIds { get; set; } = new List<int>();

        public string Mensagem { get; set; } = string.Empty;
        public string MensagemTipo { get; set; } = "success";

        public void OnGet(int? idFuncionario)
        {
            var funcionarioHandler = new FuncionarioHandler();
            var servicoHandler = new ServicoHandler();

            int idSalao = 0;
            int.TryParse(User.FindFirst("IdSalao")?.Value, out idSalao);

            Funcionarios = funcionarioHandler.ListarPorSalao(idSalao) ?? new List<Funcionario>();
            Servicos = servicoHandler.ListarPorSalao(idSalao) ?? new List<Servico>();

            if (idFuncionario.HasValue)
            {
                IdFuncionarioSelecionado = idFuncionario.Value;
                IsLockedEmployee = true;

                // Carrega os serviços já relacionados (tabela N:N)
                var fsHandler = new FuncionarioServicoHandler();
                var relacoes = fsHandler.ListarPorFuncionario(idFuncionario.Value) ?? new List<FuncionarioServico>();

                ServicosSelecionadosIds = relacoes.Select(r => r.IdServico).ToList();
            }
        }

        public IActionResult OnPost(bool? locked)
        {
            if (locked == true) IsLockedEmployee = true;

            int idFuncionario = 0;
            int.TryParse(Request.Form["idFuncionario"], out idFuncionario);
            if (idFuncionario <= 0)
            {
                Mensagem = "Selecione um funcionario para vincular os servicos.";
                MensagemTipo = "warning";
                OnGet(null);
                return Page();
            }

            try
            {
            // checkbox: mesma key repetida => pega todos os values
            var idsSelecionados = new List<int>();
            var values = Request.Form["servicosSelecionados"].ToArray();

            foreach (var v in values)
            {
                if (int.TryParse(v, out var idServico))
                    idsSelecionados.Add(idServico);
            }

            var fsHandler = new FuncionarioServicoHandler();

            // Estratégia simples: limpa e regrava (ok, porque você quer salvar "o conjunto")
            fsHandler.ExcluirPorFuncionario(idFuncionario);

            foreach (var idServico in idsSelecionados.Distinct())
            {
                fsHandler.Cadastrar(new FuncionarioServico
                {
                    IdFuncionario = idFuncionario,
                    IdServico = idServico
                });
            }

            TempData["Mensagem"] = "Servicos vinculados ao funcionario com sucesso.";
            TempData["MensagemTipo"] = "success";
            return RedirectToPage("/FuncionarioServicoLista");
            }
            catch (Exception)
            {
                Mensagem = "Nao foi possivel salvar os vinculos do funcionario aos servicos.";
                MensagemTipo = "danger";
                OnGet(idFuncionario);
                return Page();
            }
        }
    }
}

