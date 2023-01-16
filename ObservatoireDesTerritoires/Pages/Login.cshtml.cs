using System;
using Npgsql;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Web;

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
            if (Request.Cookies.ContainsKey("authToken"))
            {

                string verifEncodedJwt = HttpContext.Request.Cookies["AuthToken"];

                if (string.IsNullOrEmpty(verifEncodedJwt))
                {
                    throw new Exception("Jeton manquant");
                }

                // Configuration de la clé de validation
                var builder2 = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration2 = builder2.Build();
                string SecretKey = configuration2.GetConnectionString("Key");
                var validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

                // Configuration des paramètres de validation de jeton
                var validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = validationKey,
                    ValidAudience = "Observatoire",
                    ValidIssuer = "Observatoire",
                    ValidateLifetime = true
                };

                SecurityToken validatedToken;

                try
                {
                    // Validation du jeton
                    var jwtHandler = new JwtSecurityTokenHandler();
                    jwtHandler.ValidateToken(verifEncodedJwt, validationParameters, out validatedToken);
                }
                catch (SecurityTokenExpiredException)
                {
                    // Jeton expiré
                    throw new Exception("Jeton expiré");
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    // Signature du jeton invalide
                    throw new Exception("Jeton non valide");
                }
                string decodedEmail = "";
                // Accès aux claims
                var jwt = (JwtSecurityToken)validatedToken;
                foreach (var claim in jwt.Claims)
                {
                    if (claim.Type == "Mail")
                    {
                        decodedEmail = claim.Value;
                    }
                }





                var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = builder.Build();
                string connectionString = configuration.GetConnectionString("Observatoire");
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                connection.Open();
                using NpgsqlCommand command = new NpgsqlCommand("select code_epci from epci inner join users on epci.id_epci = users.id_epci where mail_use = @Email;", connection);
                string epci = "";
                command.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, decodedEmail);
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        epci = reader.GetString(0);
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
                // Hash du mdp pour voir si les logs sont bonne

                var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = builder.Build();
                string connectionString = configuration.GetConnectionString("Observatoire");
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




                    // recuperer l'epci a partir du formulaire pour redirect sur la bonne page


                    var epci = "";
                    if (result > 0)
                    {
                        using NpgsqlCommand command2 = new NpgsqlCommand("select code_epci from epci inner join users on epci.id_epci = users.id_epci where mail_use = @Email;", connection);
                        Console.WriteLine($"result: {result}");
                        command2.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, email);
                        using (NpgsqlDataReader reader = command2.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                                epci = reader.GetString(0);
                            }
                        }



                        // recuperer le privilege grace a l'email du post


                        command.CommandText = "SELECT isadmin FROM users WHERE mail_use = @Email";
                        int admin = 0;
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                admin = reader.GetInt32(0);
                            }
                        }



                        //set le jwt token

                        var builder2 = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                        IConfigurationRoot configuration2 = builder2.Build();
                        string SecretKey = configuration2.GetConnectionString("Key");
                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


                        var claims = new[]
                        {
                            new Claim("Mail", email),
                            new Claim("Privilege", admin.ToString())
                        };

                        var token = new JwtSecurityToken(
                            issuer: "Observatoire",
                            audience: "Observatoire",
                            claims: claims,
                            expires: DateTime.Now.AddDays(1),
                            signingCredentials: creds
                            );
                        var encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);

                        HttpContext.Response.Cookies.Append("AuthToken", encodedJwt,
                                    new CookieOptions
                                    {
                                        Expires = DateTime.Now.AddDays(30)
                                    });



                        // verif du jeton




                        command.CommandText = "SELECT isadmin FROM users WHERE mail_use = @Email";
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