using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;


namespace ObservatoireDesTerritoires.Pages
{
    public class GraphiqueModel : PageModel
    {
        private readonly ILogger<LogModel> _logger;
        public IActionResult? OnGet()
        {
            // Vérifiez si le cookie de session existe
            if (!Request.Cookies.ContainsKey("AuthToken"))
            {
                // Redirigez vers la page Login
                return Redirect("/Login");
            } else
            {
                return null;
            }
        }

        [HttpPost]
        public IActionResult OnPost(string logout)
        {
            //Supprimer le cookie
            Response.Cookies.Delete("AuthToken");
            //Rediriger vers la page de connexion
            return RedirectToPage("/Login");
        }


    }
}
