using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace CorteCor.Pages
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["HideMenu"] = "true";
            await HttpContext.SignOutAsync();

            return RedirectToPage("/Adm");
        }
    }
}
