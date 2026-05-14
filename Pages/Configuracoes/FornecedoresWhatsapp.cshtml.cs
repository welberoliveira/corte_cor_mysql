using CorteCor.Models;
using CorteCor.Handlers;
using CorteCor.Handlers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;


namespace CorteCor.Pages.Configuracoes
{
    [Authorize(Policy = "AdminPolicy")]
    public class FornecedoresWhatsappModel : PageModel
    {
        private readonly FornecedoresHandler _handler;

        public FornecedoresWhatsappModel(FornecedoresHandler handler)
        {
            _handler = handler;
        }

        public IEnumerable<FornecedorWhatsapp> Fornecedores { get; set; } = new List<FornecedorWhatsapp>();

        public void OnGet()
        {
            Fornecedores = _handler.ObterWhatsapp();
        }

        public IActionResult OnPost()
        {
            var model = new FornecedorWhatsapp();
            model.IdFornecedor = int.Parse(Request.Form["IdFornecedor"]);
            model.Nome = Request.Form["Nome"];
            model.ApiKey = Request.Form["ApiKey"];
            model.ApiSecret = Request.Form["ApiSecret"];
            model.Endpoint = Request.Form["Endpoint"];
            model.InstanceId = Request.Form["InstanceId"];
            model.Token = Request.Form["Token"];
            model.Ativo = Request.Form["Ativo"] == "true";

            _handler.SalvarWhatsapp(model);
            return RedirectToPage();
        }

        public IActionResult OnPostAtivar(int id)
        {
            _handler.AtivarWhatsapp(id);
            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            _handler.ExcluirWhatsapp(id);
            return RedirectToPage();
        }
    }
}

