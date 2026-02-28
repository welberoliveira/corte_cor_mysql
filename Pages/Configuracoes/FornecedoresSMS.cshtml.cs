using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;


namespace CorteCor.Pages.Configuracoes
{
    [Authorize(Policy = "AdminPolicy")]
    public class FornecedoresSMSModel : PageModel
    {
        private readonly FornecedoresHandler _handler;

        public FornecedoresSMSModel(FornecedoresHandler handler)
        {
            _handler = handler;
        }

        public IEnumerable<FornecedorSMS> Fornecedores { get; set; } = new List<FornecedorSMS>();

        public void OnGet()
        {
            Fornecedores = _handler.ObterSMS();
        }

        public IActionResult OnPost()
        {
            var model = new FornecedorSMS();
            model.IdFornecedor = int.Parse(Request.Form["IdFornecedor"]);
            model.Nome = Request.Form["Nome"];
            model.ApiKey = Request.Form["ApiKey"];
            model.ApiSecret = Request.Form["ApiSecret"];
            model.Endpoint = Request.Form["Endpoint"];
            model.Remetente = Request.Form["Remetente"];
            model.Ativo = Request.Form["Ativo"] == "true";

            _handler.SalvarSMS(model);
            return RedirectToPage();
        }

        public IActionResult OnPostAtivar(int id)
        {
            _handler.AtivarSMS(id);
            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            _handler.ExcluirSMS(id);
            return RedirectToPage();
        }
    }
}

