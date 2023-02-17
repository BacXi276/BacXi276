using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ObservatoireDesTerritoires.Pages
{
    public class ImportModel : PageModel
    {
        public class DivColumn
        {
            public int Id { get; set; }
            public string Libelle { get; set; }
        }
        public List<DivColumn> ColumnList { get; set; }


        public string ErrorMessage { get; set; }
        public string nblentete { get; set; }
        public string newFile { get; set; }
        public int cols { get; set; }
        public void OnGet()
        {
            if (!string.IsNullOrEmpty(Request.Query["Lign"]))
            {



                string verifEncodedJwt = HttpContext.Request.Cookies["AuthToken"];
                string userMail = "";

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
                        userMail = claim.Value;
                    }
                }










                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string parentDirectory = Directory.GetParent(currentDirectory).FullName;
                string targetDirectory = Path.Combine(parentDirectory, "wwwroot", "uploads");
                string filePath = Path.Combine(targetDirectory, userMail + ".csv");
                string tempsFilePath = @"C:\Users\help.DESKTOP-QG16ICM\Desktop\Nouveau dossier (3)\Stage\ObservatoireDesTerritoires\wwwroot\uploads\admin@admin.fr.csv";

                var reader = new StreamReader(System.IO.File.OpenRead(tempsFilePath), Encoding.UTF8);

                List<string> lines = new List<string>();
                int lineCounter = 0;
                int enteteSkip = Int32.Parse(Request.Query["Lign"]);
                while (!reader.EndOfStream)
                {
                    if (lineCounter++ < enteteSkip) continue;
                    lines.Add(reader.ReadLine());
                }
                int rows = lines.Count;
                cols = lines[0].Split(';').Length;
                string[,] tableau2D = new string[rows, cols];
                int counter = 0;
                for (int i = 0; i < rows; i++)
                {
                    if (i < 2) continue;
                    string[] values = lines[i].Split(';');
                    if (lines[0] != null)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            tableau2D[counter, j] = values[j];
                        }
                    }

                    counter++;
                }
                ColumnList = new List<DivColumn>();

                for (int i = 0; i < cols; i++)
                {
                    int temppp = Int32.Parse(Request.Query["Lign"]);
                    temppp = temppp - 2;
                    ColumnList.Add(new DivColumn
                    {
                        Id = i,
                        Libelle = tableau2D[temppp, i]
                    });
                }

                string nbrColumns = (ColumnList.Count()).ToString();
                foreach (DivColumn column in ColumnList)
                {
                    Console.WriteLine(column.Libelle);
                }

                Console.WriteLine(cols.ToString() + " NOMBRE DE COLONNES");
            }
        }

        public IActionResult OnPostFileCollumn(IFormFile file)
        {
            Console.WriteLine("------------------------------");
            string verifEncodedJwt = HttpContext.Request.Cookies["AuthToken"];
            string userMail = "";
            string preview = "";

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
                    userMail = claim.Value;
                }
            }

            if (string.IsNullOrEmpty(Request.Query["Lign"]))
            {
                if (file == null || file.Length == 0)
                {
                    ErrorMessage = "Aucun fichier n'a été uploadé";
                    return Page();
                }

                if (!Path.GetExtension(file.FileName).EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Seuls les fichiers CSV sont acceptés";
                    return Page();
                }

                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", file.FileName);
                string oldFile = filePath;
                newFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", userMail + ".csv");

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }
                }
                catch (Exception ex)
                {
                    return Content("Une erreur s'est produite lors de l'enregistrement du fichier : " + ex.Message);
                }
                if (System.IO.File.Exists(newFile))
                {
                    System.IO.File.Delete(newFile);
                    System.IO.File.Move(filePath, newFile);
                }
                else
                {
                    System.IO.File.Move(filePath, newFile);
                }
                nblentete = Request.Form["nblentete"].ToString();


                var reader = new StreamReader(System.IO.File.OpenRead(@"C:\Users\help.DESKTOP-QG16ICM\Desktop\Nouveau dossier (3)\Stage\ObservatoireDesTerritoires\wwwroot\uploads\admin@admin.fr.csv"), Encoding.UTF8);

                List<string> lines = new List<string>();
                int lineCounter = 0;
                while (!reader.EndOfStream)
                {
                    if (lineCounter++ < 2) continue;
                    lines.Add(reader.ReadLine());
                }
                int rows = lines.Count;
                int cols = lines[0].Split(';').Length;
                string[,] tableau2D = new string[rows, cols];
                int counter = 0;
                for (int i = 0; i < rows; i++)
                {
                    if (i < 2) continue;
                    string[] values = lines[i].Split(';');
                    for (int j = 0; j < cols; j++)
                    {
                        tableau2D[counter, j] = values[j];
                    }
                    counter++;
                }

                List<string> list = new List<string>();
                //Console.Write(tableau2D);
                for (int i = 2; i < tableau2D.GetLength(1); i++)
                {
                    //Console.WriteLine(tableau2D[i, 1]);
                    for (int j = 0; j < tableau2D.GetLength(0); j++)
                    {
                        //Console.WriteLine(tableau2D[j, 1]);
                        //Console.WriteLine(tableau2D[j, i]);
                        // libelle = 1
                        //  value = 2
                        //    libelle ville = 3 
                        string request = "WITH ville AS (SELECT id_ville FROM Ville WHERE cog_ville = 'CogValue' LIMIT 1) INSERT INTO emploi_data(libelle_data, value_data, id_ville) SELECT 'FirstParam', 'SecondParam', id_ville FROM ville WHERE EXISTS (SELECT 1 FROM ville);";
                        string firstParamValue = tableau2D[0, i];
                        string cogValue = tableau2D[j, 0];
                        if (!String.IsNullOrEmpty(cogValue))
                        {
                            cogValue = cogValue.TrimStart('0');
                        }

                        string secondParamValue = tableau2D[j, i];
                        string thirdParamValue = tableau2D[j, 1];
                        if (!String.IsNullOrEmpty(cogValue) && !String.IsNullOrEmpty(firstParamValue) && !String.IsNullOrEmpty(secondParamValue) && !String.IsNullOrEmpty(thirdParamValue) && thirdParamValue != "Libellé")
                        {
                            Console.WriteLine("----------");
                            firstParamValue = firstParamValue.Replace("'", "''");
                            secondParamValue = secondParamValue.Replace("'", "''");
                            thirdParamValue = thirdParamValue.Replace("'", "''");
                            request = request.Replace("FirstParam", firstParamValue);
                            request = request.Replace("SecondParam", secondParamValue);
                            request = request.Replace("ThirdParam", thirdParamValue);
                            request = request.Replace("CogValue", cogValue);
                            //Console.WriteLine(cogValue);
                            //Console.WriteLine(request);



                            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                            IConfigurationRoot configuration = builder.Build();
                            string connectionString = configuration.GetConnectionString("Observatoire");
                            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                            connection.Open();
                            string strEmail = Request.Form["email"].ToString();
                            using (NpgsqlCommand command = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE mail_use = @email", connection))
                            {
                                using (NpgsqlCommand command2 = new NpgsqlCommand(request, connection))
                                {
                                    command2.ExecuteNonQuery();
                                    Console.WriteLine("execute");
                                }


                                connection.Close();
                            }





                            /*  using (var stream = new StreamWriter(@"C:\Users\help.DESKTOP-QG16ICM\Desktop\Nouveau dossier (3)\Stage\ObservatoireDesTerritoires\wwwroot\uploads\new_try.txt", true))
                              {
                                  stream.Write(request + "\n");
                              }*/
                        }
                        else
                        {
                            Console.WriteLine("value null or empty");
                        }


                    }
                }


                return Redirect("/Import?Lign=" + nblentete);











            }

            else
            {
                return null;
            }
        }
        public void OnPostSubmit()
        {
            /*
            string tempsFilePath = @"C:\Users\help.DESKTOP-QG16ICM\Desktop\Nouveau dossier (3)\Stage\ObservatoireDesTerritoires\wwwroot\uploads\admin@admin.fr.csv";
 
            var reader = new StreamReader(System.IO.File.OpenRead(tempsFilePath), Encoding.UTF8);
 
            List<string> lines = new List<string>();
            int lineCounter = 0;
            Console.WriteLine(Request.Query["option1"]);
            int enteteSkip = Int32.Parse(Request.Query["shadowValue"]);
            while (!reader.EndOfStream)
            {
                if (lineCounter++ < enteteSkip) continue;
                lines.Add(reader.ReadLine());
            }
            int rows = lines.Count;
            cols = lines[0].Split(';').Length;
 
 
 
            Console.WriteLine(cols + "-----------------------");
 
            for (int i = 1; i < cols + 1; i++)
            {
                string idwithIndex = "option" + i.ToString();
                Console.WriteLine(idwithIndex + "-----------------------");
            }
 
            */














        }
    }
}