using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorteCor.Pages
{
    public class PrivacidadeModel : PageModel
    {
        public void OnGet()
        {
            ViewData["HideMenu"] = "true";
        }
    }
}
