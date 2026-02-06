using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace CorteCor.Pages
{
    public class Logout2Model : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["HideMenu"] = "true";
            
            return RedirectToPage(HttpContext.Request.PathBase + "/Index"); 
        }
    }
}
