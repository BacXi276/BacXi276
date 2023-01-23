using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
                string tempsFilePath = @"C:\Users\Bailly\Desktop\Stage\ObservatoireDesTerritoires\ObservatoireDesTerritoires\wwwroot\uploads\admin.csv";

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
                for(int i = 0; i < rows; i++)
                {
                    ColumnList.Add(new DivColumn
                    {
                        Id = i,
                        Libelle = tableau2D[i, Int32.Parse(Request.Query["Lign"]) - 1].ToString()
                    });
                }
                foreach(DivColumn column in ColumnList)
                {
                    Console.WriteLine(column.Libelle);
                }

                Console.WriteLine(cols.ToString() + " NOMBRE DE COLONNES");
            }
        }

        public IActionResult OnPostFileCollumn(IFormFile file)
        {
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
                string oldFile =  filePath;
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
                return Redirect("/Import?Lign=" + nblentete);
            }
            
            else
            {
                return null;
            }
        }
    }
}
