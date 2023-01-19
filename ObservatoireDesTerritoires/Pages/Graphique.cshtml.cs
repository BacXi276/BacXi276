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

namespace ObservatoireDesTerritoires.Pages
{
    public class MyModel
    {
        public int Id { get; set; }
        public string Libelle { get; set; }
        public string value { get; set; }
        public string Ville { get; set; }
    }
    public class GraphiqueModel : PageModel
    {
        public List<MyModel> Model { get; set; }
        public int data { get; set; }

        private readonly ILogger<LogModel> _logger;

        public IActionResult? OnGet()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfigurationRoot configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("Observatoire");
            using NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            {

                connection.Open();
                if (Request.Query["Categorie"].ToString() != "")
                {
                    Model = new List<MyModel>();
                    // iterer sur cette commande : select id_ville from ville where id_epci = (select id_epci from epci where code_epci = '200029999');
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
                        string query2 = "SELECT * FROM " + Categorie + " INNER JOIN ville ON " + Categorie + ".id_ville = ville.id_ville WHERE ville.id_ville = " + ville_id + ";";
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
                                    Ville = reader2.GetString(5)
                                });
                            }
                        }
                        reader2.Close();
                    }
                    connection.Close();
                    
                    // pour execute cette commande : select * from emploi_data where id_ville = TON_ITERATION;

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
        public IActionResult OnPost(string logout)
        {
            //Supprimer le cookie
            Response.Cookies.Delete("AuthToken");
            //Rediriger vers la page de connexion
            return RedirectToPage("/Login");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "zonage_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "emploi_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "demographie_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "economie_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "revenus_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "diplome_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "mobilite_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "logement_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "equipement_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "environnement_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "citoyennete_data");
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
                return Redirect("/Graphique?epci=" + result + "&" + "Categorie=" + "finance_data");
            }
            else
            {
                Console.WriteLine("pas passé le test");
                return null;
            }
        }
    }
}
