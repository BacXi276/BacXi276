using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ObservatoireDesTerritoires.Pages
{
    public class AdminModel : PageModel
    {
        public string email { get; set; }
        public string password { get; set; }
        public int code_epci { get; set; }
        public int type { get; set; }
        public string ErrorMessage { get; set; }
        public string ValidMessage { get; set; }

        public IActionResult OnGet()
        {
            string verifEncodedJwt = HttpContext.Request.Cookies["AuthToken"];

            if (string.IsNullOrEmpty(verifEncodedJwt))
            {
                throw new Exception("Jeton manquant");
            }

            // Configuration de la cl� de validation
            var builder2 = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration2 = builder2.Build();
            string SecretKey = configuration2.GetConnectionString("Key");
            var validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

            // Configuration des param�tres de validation de jeton
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
                // Jeton expir�
                throw new Exception("Jeton expir�");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                // Signature du jeton invalide
                throw new Exception("Jeton non valide");
            }

            // Acc�s aux claims
            var jwt = (JwtSecurityToken)validatedToken;
            foreach (var claim in jwt.Claims)
            {
                if (claim.Type == "Privilege")
                {
                    if (claim.Value != "1")
                    {
                        return Redirect("/Login");
                    }

                }
            }
            return Page();
        }


        public IActionResult OnPost()
        {

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration = builder.Build();
                string connectionString = configuration.GetConnectionString("Observatoire");
                using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                {
                    connection.Open();
                    string strEmail = Request.Form["email"].ToString();
                    // V�rifiez si l'email existe d�j� en utilisant une requ�te SELECT
                    using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE mail_use = @email", connection))
                    {
                        command.Parameters.AddWithValue("@email", NpgsqlTypes.NpgsqlDbType.Text, strEmail);
                        long count = (long)command.ExecuteScalar();
                        if (count > 0)
                        {
                            ErrorMessage = "L'email est d�j� dans la base de donn�e";
                        }
                        else
                        {
                            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(Request.Form["password"].ToString());
                            Console.WriteLine(hashedPassword);
                            using (NpgsqlCommand command2 = new NpgsqlCommand("INSERT INTO users (mail_use, password_use, id_epci, isadmin) VALUES (@email, @password, (Select id_epci from epci where code_epci = @code_epci), @type);", connection))
                            {
                                string strPassword = Request.Form["password"].ToString();
                                string strCodeEpci = Request.Form["code_epci"].ToString();
                                Int32 intValue = Int32.Parse(Request.Form["type"]);
                                command2.Parameters.AddWithValue("@email", NpgsqlTypes.NpgsqlDbType.Text, strEmail);
                                command2.Parameters.AddWithValue("@password", NpgsqlTypes.NpgsqlDbType.Text, hashedPassword);
                                command2.Parameters.AddWithValue("@code_epci", NpgsqlTypes.NpgsqlDbType.Text, strCodeEpci);
                                command2.Parameters.AddWithValue("@type", NpgsqlTypes.NpgsqlDbType.Integer, intValue);
                                command2.ExecuteNonQuery();
                                Console.WriteLine(strEmail + hashedPassword + strCodeEpci + intValue);
                                ValidMessage = "Vous avez ajouter : " + strEmail;
                            }

                            connection.Close();
                        }
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                //affiche l'erreur sur la console
                Console.WriteLine(ex.Message);
            }
            catch (FormatException ex)
            {
                return Page();
            }
            return Page();
        }

        public IActionResult OnPostToGraphique()
        {
            var epci = "";
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

                using NpgsqlCommand command = new NpgsqlCommand("select code_epci from epci inner join users on epci.id_epci = users.id_epci where mail_use = @Email;", connection);
                string verifEncodedJwt = HttpContext.Request.Cookies["AuthToken"];

                if (string.IsNullOrEmpty(verifEncodedJwt))
                {
                    throw new Exception("Jeton manquant");
                }

                // Configuration de la cl� de validation
                var builder2 = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                IConfigurationRoot configuration2 = builder2.Build();
                string SecretKey = configuration2.GetConnectionString("Key");
                var validationKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

                // Configuration des param�tres de validation de jeton
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
                    // Jeton expir�
                    throw new Exception("Jeton expir�");
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    // Signature du jeton invalide
                    throw new Exception("Jeton non valide");
                }

                // Acc�s aux claims
                var jwt = (JwtSecurityToken)validatedToken;
               
                foreach (var claim in jwt.Claims)
                {   
                    if (claim.Type == "Mail")
                    {
                        command.Parameters.AddWithValue("@Email", NpgsqlTypes.NpgsqlDbType.Text, claim.Value);
                        using (NpgsqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                epci = reader.GetString(0);
                            }
                        }
                        connection.Close();

                    }
                }
            }
            catch (NpgsqlException ex)
            {
                return BadRequest(ex.Message);
            }
            return Redirect("/Graphique?epci=" + epci);
        }
    }
}