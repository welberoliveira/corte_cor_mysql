using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using static CorteCor.Models;

namespace CorteCor.Pages.Configuracoes
{
    [Authorize(Policy = "AdminPolicy")]
    public class FornecedoresEmailModel : PageModel
    {
        private readonly FornecedoresHandler _handler;

        public FornecedoresEmailModel(FornecedoresHandler handler)
        {
            _handler = handler;
        }

        public IEnumerable<FornecedorEmail> Fornecedores { get; set; }

        [BindProperty]
        public FornecedorEmail Input { get; set; }

        public void OnGet()
        {
            Fornecedores = _handler.ObterEmails();
        }

        public IActionResult OnPost()
        {
            // Bind manual properties since the modal form fields match the model properties
            var model = new FornecedorEmail();
            model.IdFornecedor = int.Parse(Request.Form["IdFornecedor"]);
            model.Nome = Request.Form["Nome"];
            model.ApiKey = Request.Form["ApiKey"];
            model.ApiSecret = Request.Form["ApiSecret"];
            model.Endpoint = Request.Form["Endpoint"];
            model.RemetenteNome = Request.Form["RemetenteNome"];
            model.RemetenteEmail = Request.Form["RemetenteEmail"];
            model.Ativo = Request.Form["Ativo"] == "true";

            _handler.SalvarEmail(model);
            return RedirectToPage();
        }

        public IActionResult OnPostAtivar(int id)
        {
            _handler.AtivarEmail(id);
            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            _handler.ExcluirEmail(id);
            return RedirectToPage();
        }
    }
}
