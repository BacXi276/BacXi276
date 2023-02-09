using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Npgsql;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Data.SqlClient;
using ObservatoireDesTerritoires.Controller;
using Newtonsoft.Json;
using System.Web;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace ObservatoireDesTerritoires.Pages
{
    public class MyModel
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public string value { get; set; }
        public string Ville { get; set; }
    }
    public class FilterValue
    {
        public string Libelle { get; set; }
    }
    public class FilterVille
    {
        public string ville { get; set; }
    }
    public class FilterEpci
    {
        public string epci { get; set; }
    }
    public class GraphiqueModel : PageModel
    {
        public List<MyModel> Model { get; set; }
        public List<FilterValue> FilterValues { get; set; }
        public List<FilterVille> FilterVilles { get; set; }
        public string SelectedVille1 { get; set; }
        public string SelectedVille2 { get; set; }
        public string SelectedFilter { get; set; }
        public int data { get; set; }
        public string category { get; set; }
        public int isAdmin { get; set; }

        private readonly ILogger<LogModel> _logger;

        public IActionResult? OnGet()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("Observatoire");
            category = Request.Query["Categorie"].ToString();
            var options = new CookieOptions();
            options.Expires = DateTime.Now.AddMinutes(30);
            HttpContext.Response.Cookies.Append("category", category, options);
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            {
                string epciParam = Request.Query["epci"].ToString();
                string Categorie = Request.Query["Categorie"].ToString();
                string decodedEmail = "";
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
                        //throw new Exception("Jeton expiré");
                        //Supprimer le cookie
                        Response.Cookies.Delete("AuthToken");
                        //Rediriger vers la page de connexion
                        return RedirectToPage("/Login");

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
                        if (claim.Type == "Privilege")
                        {
                            decodedEmail = claim.Value;
                        }
                    }
                }
                isAdmin = Int32.Parse(decodedEmail);
                connection.Open();
                string VilleQuery = "select libelle_ville from ville where id_epci = (select id_epci from epci where code_epci = '" + epciParam + "') order by libelle_ville;";
                FilterVilles = new List<FilterVille>();
                NpgsqlCommand command4 = new NpgsqlCommand(VilleQuery, connection);
                NpgsqlDataReader reader4 = command4.ExecuteReader();
                while (reader4.Read())
                {
                    // CREE DES OBJETS
                    FilterVilles.Add(new FilterVille
                    {
                        ville = reader4.GetString(0)
                    });
                }
                connection.Close();
                connection.Open();
                if (Request.Query["Categorie"].ToString() != "")
                {
                    // REMPLISSAGE DU TABLEAU

                    Model = new List<MyModel>();
                    string query = ""; 
                    string filter = Request.Query["Filter"].ToString();
                    SelectedFilter = Request.Query["Filter"].ToString();
                    filter = filter.Replace("'", "''");
                    string Ville_1 = Request.Query["Ville_1"].ToString();
                    SelectedVille1 = Request.Query["Ville_1"].ToString();
                    Ville_1 = Ville_1.Replace("'", "''");
                    string Ville_2 = Request.Query["Ville_2"].ToString();
                    SelectedVille2 = Request.Query["Ville_2"].ToString();
                    Ville_2 = Ville_2.Replace("'", "''");

                    if (Request.Query["Filter"].ToString() == "")
                    {
                        query = "SELECT * FROM " + Categorie + " where code_epci = @code";
                    }
                    else
                    {
                        if (Request.Query["Filter"].ToString() != "" && Request.Query["Ville_1"].ToString() == "" && Request.Query["Ville_2"].ToString() == "")
                        {
                            query = "SELECT * FROM " + Categorie + " where code_epci = @code and libelle_data = '" + filter + "';";
                        }
                        else
                        {
                            query = "SELECT * FROM " + Categorie + " where libelle_data = '" + filter + "' and (libelle_ville = '" + Ville_1 + "' or libelle_ville = '" + Ville_2 + "');";
                        }
                    }

                    NpgsqlCommand command = new NpgsqlCommand(query, connection);
                    command.Parameters.AddWithValue("@code", NpgsqlTypes.NpgsqlDbType.Text, epciParam);
                    NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Model.Add(new MyModel
                            {
                                Id = reader.GetInt32(0),
                                Libelle = reader.GetString(1),
                                value = reader.GetString(2),
                                Ville = reader.GetString(4)
                            });
                        }
                    }
                    connection.Close();


                    // REMPLISSAGE DU FILTRE

                    // RECUPERER LES LIBELLE DISTINCT
                    connection.Open();
                    FilterValues = new List<FilterValue>();
                    string filterQuery = "select distinct libelle_data from " + Categorie;
                    NpgsqlCommand command3 = new NpgsqlCommand(filterQuery, connection);
                    NpgsqlDataReader reader3 = command3.ExecuteReader();
                    while (reader3.Read())
                    {
                        // CREE DES OBJETS
                        FilterValues.Add(new FilterValue
                        {
                            Libelle = reader3.GetString(0)
                        });
                    }
                    connection.Close();
                }
            }
            // Vérifiez si le cookie de session existe
            if (!Request.Cookies.ContainsKey("AuthToken"))
            {
                // Redirigez vers la page Login
                return Redirect("/Login");
            }
            else
            {
                return Page();
            }
        }

        [HttpPost]
        public IActionResult OnPostLogout()
        {
            //Supprimer le cookie
            Response.Cookies.Delete("AuthToken");
            //Rediriger vers la page de connexion
            return RedirectToPage("/Login");
        }

        public IActionResult OnPostFilter()
        {

            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                string ville1 = WebUtility.UrlEncode(Request.Form["ville1"].ToString());
                string ville2 = WebUtility.UrlEncode(Request.Form["ville2"].ToString());
                string selectedValue = Request.Form["thelist"].ToString();
                category = HttpContext.Request.Cookies["category"];
                Console.WriteLine(ville1 + ville2 + selectedValue);
                selectedValue = HttpUtility.UrlEncode(selectedValue);
                if (string.IsNullOrEmpty(ville1) || string.IsNullOrEmpty(ville2))
                {
                    return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + category + "&" + "Filter=" + selectedValue);
                }
                else
                {
                    return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + category + "&" + "Filter=" + selectedValue + "&Ville_1=" + ville1 + "&Ville_2=" + ville2);
                }
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        [HttpPost]
        public IActionResult OnPostComparaison()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                string ville1 = WebUtility.UrlEncode(Request.Form["ville1"].ToString());
                string ville2 = WebUtility.UrlEncode(Request.Form["ville2"].ToString());
                string filter = WebUtility.UrlEncode(Request.Form["thelist"].ToString());
                Console.WriteLine(ville1 + ville2 + filter);
                category = HttpContext.Request.Cookies["category"];
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + category + "&" + "Filter=" + filter + "&Ville_1=" + ville1 + "&Ville_2=" + ville2);
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        [HttpPost]
        public JsonResult GetVilleSuggestions(string ville)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("Observatoire");
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open();
            var query = "SELECT libelle_ville FROM ville WHERE libelle_ville LIKE @ville + '%' LIMIT 10";
            using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@ville", ville);
            using var reader = command.ExecuteReader();
            var suggestions = new List<string>();
            while (reader.Read())
            {
                suggestions.Add(reader["libelle_ville"].ToString());
            }
            return new JsonResult(suggestions);
        }

        private GetEpci _epciController;

        public GraphiqueModel()
        {
            _epciController = new GetEpci();
        }

        public IActionResult OnPostZonage()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "zonage_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostEmploi()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "emploi_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostDemographie()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "demographie_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostEconomie()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "economie_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostRevenus()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "revenus_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostDiplome()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "diplome_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostMobilites()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "mobilite_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostLogement()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "logement_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostEquipements()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "equipement_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostEnvironnement()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "environnement_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostCitoyennete()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "citoyennete_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }

        public IActionResult OnPostFinances()
        {
            if (HttpContext.Request.Cookies.ContainsKey("AuthToken"))
            {
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "finance_view");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }
    }
}
