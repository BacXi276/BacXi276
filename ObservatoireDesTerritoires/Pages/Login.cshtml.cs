using System;
using Npgsql;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ObservatoireDesTerritoires.Pages
{
    public class LogModel : PageModel
    {
        private readonly ILogger<LogModel> _logger;
        public LogModel(ILogger<LogModel> logger)
        {
            _logger = logger;
        }
        [BindProperty]
        public string email { get; set; }
        [BindProperty]
        public string password { get; set; }
        public string ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
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
                return null;
            }
        }

        public IActionResult OnPost()
        {
            try
            {
                string connectionString = "Server=localhost;Port=5432;User Id=postgres;Password=root;Database=Observatoire;";
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using NpgsqlCommand command = new NpgsqlCommand("SELECT password_use FROM users WHERE mail_use = @Email", connection);
                command.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, email);
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
                Console.WriteLine(hashedPassword);
                string hashedPasswordBdd = (string)command.ExecuteScalar();
                if (BCrypt.Net.BCrypt.Verify(password, hashedPasswordBdd))
                {
                    command.CommandText = "SELECT COUNT(*) FROM users WHERE mail_use = @Email AND password_use = @Password; SELECT id_epci FROM users WHERE mail_use = @Email";
                    command.Parameters.AddWithValue("@Password", NpgsqlTypes.NpgsqlDbType.Text, hashedPasswordBdd);
                    long result = (long)command.ExecuteScalar();
                    var epci = "";
                    if (result > 0)
                    {
                        command.CommandText = "SELECT code_epci FROM users INNER JOIN epci ON id_log = epci.id_epci WHERE mail_use = @Email";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, email);
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                epci = reader.GetInt32(0).ToString();
                            }
                        }
                        // Successful login
                        // Redirect to graphique page with ville parameter
                        HttpContext.Response.Cookies.Append("user", email, new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(1)
                        });
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
                        ErrorMessage = "Email ou mot de passe incorrect";
                        return Page();
                    }
                }
                else
                {
                    ErrorMessage = "Email ou mot de passe incorrect";
                    return Page();
                }

            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "An error occurred while trying to connect to the database.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to process the request.");
                return BadRequest(ex.Message);
            }
        }
    }
}
