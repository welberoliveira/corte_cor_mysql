using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    public class TermosUsoModel : PageModel
    {
        public void OnGet()
        {
            ViewData["HideMenu"] = "true";
        }
    }
}
