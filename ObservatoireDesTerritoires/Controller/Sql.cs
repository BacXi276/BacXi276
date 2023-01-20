
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ObservatoireDesTerritoires.Controller
{
    public class GetEpci
    {

        public string GetEpciByCookie(string cookie)
        {
            string epci = "";
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
                string verifEncodedJwt = cookie;

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

                // Accès aux claims
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
                                return epci;
                            }
                        }
                        connection.Close();


                    }
                }
            }
            catch (NpgsqlException ex)
            {
                return epci;
            }
            return epci;
        }
    }


}


