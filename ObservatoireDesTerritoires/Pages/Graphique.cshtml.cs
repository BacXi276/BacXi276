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
using System.Windows.Input;
using System.Web;

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
    public class GraphiqueModel : PageModel
    {
        public List<MyModel> Model { get; set; }
        public List<FilterValue> FilterValues { get; set; }
        public int data { get; set; }
        public string category { get; set; }

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

                connection.Open();
                if (Request.Query["Categorie"].ToString() != "")
                {
                    // REMPLISSAGE DU TABLEAU

                    Model = new List<MyModel>();
                    string query1 = "select * from ville where id_epci = (select id_epci from epci where code_epci = @code)";
                    NpgsqlCommand command1 = new NpgsqlCommand(query1, connection);
                    string epciParam = Request.Query["epci"].ToString();
                    command1.Parameters.AddWithValue("@code", NpgsqlTypes.NpgsqlDbType.Text, epciParam);
                    NpgsqlDataReader reader1 = command1.ExecuteReader();
                    List<string> ids_ville = new List<string>();
                    while (reader1.Read())
                    {
                        ids_ville.Add(reader1.GetInt32(0).ToString());
                    }                 
                    reader1.Close();
                    string Categorie = Request.Query["Categorie"].ToString();
                    foreach (string ville_id in ids_ville)
                    {
                        string query2 = "";
                        string filter = Request.Query["Filter"].ToString();
                        filter = filter.Replace("'", "''");
                        if (Request.Query["Filter"].ToString() == "") {
                            query2 = "SELECT * FROM " + Categorie + " WHERE id_ville = " + ville_id + ";";
                        }
                        else if (Request.Query["Filter"].ToString() != "")
                        {
                            query2 = "SELECT * FROM " + Categorie + " WHERE id_ville = " + ville_id + " and libelle_data = '"+ filter + "';";
                        }
                        else
                        {
                            query2 = "SELECT * FROM " + Categorie + " WHERE id_ville = " + ville_id + " and libelle_data = '" + filter + "';";
                        }
                        NpgsqlCommand command2 = new NpgsqlCommand(query2, connection);
                        NpgsqlDataReader reader2 = command2.ExecuteReader();

                        if (reader2.HasRows)
                        {
                            while (reader2.Read())
                            {
                                Model.Add(new MyModel
                                {
                                    Id = reader2.GetInt32(0),
                                    Libelle = reader2.GetString(1),
                                    value = reader2.GetString(2),
                                    Ville = reader2.GetString(4)
                                });
                            }
                        }
                        reader2.Close();
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
                Console.WriteLine(category + " DANS LE ONPOST");
                string cookie = HttpContext.Request.Cookies["AuthToken"];
                string result = _epciController.GetEpciByCookie(cookie);
                string selectedValue = Request.Form["thelist"].ToString();
                category = HttpContext.Request.Cookies["category"];
                Console.WriteLine(category);
                selectedValue = HttpUtility.UrlEncode(selectedValue);
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + category + "&" + "Filter=" + selectedValue);
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
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
