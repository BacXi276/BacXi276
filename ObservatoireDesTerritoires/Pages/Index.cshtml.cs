using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Npgsql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ObservatoireDesTerritoires.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // Vérifiez si le cookie de session existe
            if (Request.Cookies.ContainsKey("user"))
            {
                // Récupérez la valeur du cookie
                string email = Request.Cookies["user"];
                string connectionString = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=Observatoire;";
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using NpgsqlCommand command = new NpgsqlCommand("SELECT code_epci FROM users INNER JOIN epci ON id_log = epci.id_epci WHERE mail_use = @Email", connection);
                var epci = "";
                command.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, email);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        epci = reader.GetInt32(0).ToString();
                    }
                }
                command.CommandText = "SELECT isadmin FROM users WHERE mail_use = @Email";
                int admin = 0;
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        admin = reader.GetInt32(0);
                    }
                }
                if (admin >= 1)
                {
                    return Redirect("/Admin");
                }
                else
                {
                    return Redirect("/Graphique?epci=" + epci);
                }
            }
            else
            {
                // Redirigez vers la page de connexion
                return Redirect("/Login");
            }
        }   
    }
}