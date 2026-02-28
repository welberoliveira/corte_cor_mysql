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
    [Authorize(Policy = "AdminPolicy")]
    public class SalaoListaModel : PageModel
    {
        public List<Salao> Saloes { get; set; } = new();
        public string Mensagem { get; set; }
        public string StatusFilter { get; set; } = "Ativo";

        public void OnGet(string statusFilter = "Ativo")
        {
            StatusFilter = statusFilter;
            var handler = new SalaoHandler();
            // Garante que, mesmo se Listar() retornar null, allSaloes seja uma lista vazia
            var allSaloes = handler.Listar() ?? new List<Salao>();

            Saloes = statusFilter == "Ativo"
                ? allSaloes.Where(p => p.Status == "Ativo").ToList()
                : allSaloes;
        }



        public void OnPost()
        {
            var handler = new SalaoHandler();
            int id = int.Parse(Request.Form["id"]);
            string action = Request.Form["action"];

            if (action == "ativar")
            {
                handler.AtivarDesativar(id, true);
                Mensagem = "Salao ativada com sucesso.";
            }
            else if (action == "desativar")
            {
                handler.AtivarDesativar(id, false);
                Mensagem = "Salao desativada com sucesso.";
            }
            else if (action == "excluir")
            {
                try
                {
                    handler.Excluir(id);
                    Mensagem = "Salao excluída com sucesso.";
                }
                catch (Exception)
                {
                    Mensagem = "Não foi possível excluir esta Salao porque ela está associada a outros registros.";
                }
            }
            else if (action == "alterar")
            {
                Response.Redirect(HttpContext.Request.PathBase + $"/SalaoCadastro?id={id}");
                OnGet(StatusFilter);
            }

            OnGet(StatusFilter);
        }
    }
}

