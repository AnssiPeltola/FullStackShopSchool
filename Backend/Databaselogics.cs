using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

public record Tilaukset(int id, int asiakas_id, string tilauspaiva, string toimitusosoite, double tilauksen_hinta, string tilauksen_tila, string lisatiedot);
public record Product(int id, string name, string productCtgory, string productCtgory2, double price, int amount, string img, string description);
public record Purchase(int Id, int AsiakasId, string Tilauspaiva, string Toimitusosoite, double TilauksenHinta, string TilauksenTila, string Lisatiedot, string AsiakkaanNimi);
public record Asiakas(int id, string name, string email, string address, string phonenumber);
public record AddReview(int Id, int ProductId, int CustomerId, string Review,int NumReview);
public record Tilasrivi(int id, int tilaus_id, int tuote_id, int maara, double hinta);

    internal class Databaselogics
    {
        private static string _connectionString = "Data Source = verkkokauppa.db";
        public Databaselogics()
        {
            //
        }

        #region CreateTables
        public void CreateTables()
        {  
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Creates a table for 'Tuotteet' ('kuva' could be BLOB-type, but we are using TEXT for now)
            var crProductTable = connection.CreateCommand();
            crProductTable.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Tuotteet (
                id INTEGER PRIMARY KEY,
                nimi TEXT NOT NULL UNIQUE,
                kategoria TEXT,
                kategoria_kaksi TEXT,
                hinta REAL,
                kappalemaara INTEGER,
                kuva TEXT, 
                kuvaus TEXT
                )";
            crProductTable.ExecuteNonQuery();
            
            // Luodaan taulu Asiakkaat
            var crtTblAsiakkaat = connection.CreateCommand();
            crtTblAsiakkaat.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Asiakkaat (
                id INTEGER PRIMARY KEY,
                nimi TEXT NOT NULL,
                email TEXT NOT NULL UNIQUE,
                osoite TEXT NOT NULL,
                puhelinnumero TEXT
                )";
            crtTblAsiakkaat.ExecuteNonQuery();
          
            // Creates table Arvostelut
            var createReviewCmd = connection.CreateCommand();
            createReviewCmd.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Arvostelut(
                id INTEGER PRIMARY KEY, 
                tuote_id INTEGER NOT NULL, 
                asiakas_id INTEGER NOT NULL, 
                arvostelu TEXT, 
                numeerinen_arvio INTEGER,
                FOREIGN KEY (tuote_id) REFERENCES Tuotteet(id),
                FOREIGN KEY (asiakas_id) REFERENCES Asiakkaat(id)
                )";
            createReviewCmd.ExecuteNonQuery();

            // Create table Tilaukset
            var createTilaukset = connection.CreateCommand();
            createTilaukset.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Tilaukset(
                id INTEGER PRIMARY KEY,
                asiakas_id INTEGER NOT NULL,
                tilauspaiva TEXT NOT NULL,
                toimitusosoite TEXT NOT NULL,
                tilauksen_hinta REAL NOT NULL,
                tilauksen_tila TEXT NOT NULL,
                lisatiedot TEXT,
                FOREIGN KEY (asiakas_id) REFERENCES Asiakkaat(id)
                )";
            createTilaukset.ExecuteNonQuery();

            // Create table Tilausrivit
            var crtTilausrivit = connection.CreateCommand();
            crtTilausrivit.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Tilausrivit(
                id INTEGER PRIMARY KEY,
                tilaus_id INTEGER NOT NULL,
                tuote_id INTEGER NOT NULL,
                maara INTEGER NOT NULL,
                hinta REAL NOT NULL,
                FOREIGN KEY (tilaus_id) REFERENCES Tilaukset(id),
                FOREIGN KEY (tuote_id) REFERENCES Tuotteet(id)
                )";
            crtTilausrivit.ExecuteNonQuery();

            // Creates table Maksut
            var createPaymentsCmd = connection.CreateCommand();
            createPaymentsCmd.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Maksut(
                id INTEGER PRIMARY KEY, 
                tilaus_id INTEGER NOT NULL, 
                maksutapa TEXT, 
                summa REAL,
                FOREIGN KEY (tilaus_id) REFERENCES Tilaukset(id)
                )";
            createPaymentsCmd.ExecuteNonQuery();

            // Create a table for Logins
            var crLoginTable = connection.CreateCommand();
            crLoginTable.CommandText = 
                @"CREATE TABLE IF NOT EXISTS Kirjautumistiedot (
                id INTEGER PRIMARY KEY,
                asiakas_id INTEGER NOT NULL,
                salasana_hash TEXT NOT NULL,
                salasana_salt TEXT NOT NULL,
                FOREIGN KEY (asiakas_id) REFERENCES Asiakkaat(id)
                )";
            crLoginTable.ExecuteNonQuery();

            connection.Close();
        }
        #endregion

        #region Reviews
        // Inserts new product review
        public void AddReview(int productId, int customerId, string review,int numReview)
        { 
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"INSERT INTO Arvostelut (tuote_id, asiakas_id, arvostelu, numeerinen_arvio)
            VALUES ($tuote_id, $asiakas_id, $arvostelu, $numeerinen_arvio)";
            insertCmd.Parameters.AddWithValue("$tuote_id", productId);
            insertCmd.Parameters.AddWithValue("$asiakas_id", customerId);
            insertCmd.Parameters.AddWithValue("$arvostelu", review);
            insertCmd.Parameters.AddWithValue("$numeerinen_arvio", numReview);
            insertCmd.ExecuteNonQuery();

            connection.Close();
        }

        // Gets (select) product review (text) by product name
        public List<string> GetReview(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            List <string> returning = new List<string>();
            List<string> noReviews = new List<string>();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT arvostelu FROM Arvostelut
            JOIN Tuotteet ON Tuotteet.id = Arvostelut.tuote_id
            WHERE Tuotteet.nimi = $productName";
            selectCmd.Parameters.AddWithValue("$productName", productName);
            var result = selectCmd.ExecuteReader();

            while(result.Read())
            {
                returning.Add(result.GetString(0));
            }

            if(returning.Count==0)
            {
                noReviews.Add("");
                return noReviews;
            }

            connection.Close();
            return returning;
        }

        // Gets (select) numeric product review by product name
        public List<int> GetNumericReview(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            List<int> returning = new List<int>();
            List<int> noReviews = new List<int>();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT numeerinen_arvio FROM Arvostelut
            JOIN Tuotteet ON Tuotteet.id = Arvostelut.tuote_id
            WHERE Tuotteet.nimi = $productName";
            selectCmd.Parameters.AddWithValue("$productName", productName);
            var result = selectCmd.ExecuteReader();

            while(result.Read())
            {
                returning.Add(result.GetInt32(0));
            }

            if(returning.Count==0)
            {
                noReviews.Add(404);
                return noReviews;
            }

            connection.Close();
            return returning;
        }

        //Gets customer's id searched by review
        public List<int> GetCustomerIdFromReview(string review)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            List<int> customerId = new List<int>();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT asiakas_id FROM Arvostelut
            WHERE arvostelu = $review";
            selectCmd.Parameters.AddWithValue("$review", review);
            var result = selectCmd.ExecuteReader();

            while(result.Read())
            {
                customerId.Add(result.GetInt32(0));
            }

            connection.Close();
            return customerId;
        }

        // Deletes the review searched by string
        public void DeleteReview(string toBeDeleted)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var delCmd = connection.CreateCommand();
            delCmd.CommandText = @"DELETE FROM Arvostelut WHERE arvostelu = $arvostelu";
            delCmd.Parameters.AddWithValue("$arvostelu", toBeDeleted);
            delCmd.ExecuteNonQuery();

            connection.Close();
        }
        #endregion
      
        #region Customers
        // Lisää asiakas tauluun Asiakkaat
        public void AddCustomer(string name, string email, string address, string phonenumber)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
        
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = 
            @"INSERT INTO Asiakkaat (nimi, email, osoite, puhelinnumero)
            VALUES ($name, $email, $address, $phonenumber)";
            insertCmd.Parameters.AddWithValue("$name", name);
            insertCmd.Parameters.AddWithValue("$email", email);
            insertCmd.Parameters.AddWithValue("$address", address);
            insertCmd.Parameters.AddWithValue("$phonenumber", phonenumber);
            insertCmd.ExecuteNonQuery();
            connection.Close();
        }

        // UPDATE asiakkaan tietoja taulusta Asiakkaat. Valitse parametrillä email kenen tietoja päivitetään, colum mitä tietoja ja newInfo uusi tieto.
        public void UpdateCustomer(string column, string newInfo, string email)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            $"UPDATE Asiakkaat SET {column} = $newInfo WHERE email = $email";
            updateCmd.Parameters.AddWithValue("$newInfo", newInfo);
            updateCmd.Parameters.AddWithValue("$email", email);
            updateCmd.ExecuteNonQuery();

            connection.Close();
        }

        // Hakee tablesta Asiakkaat columnin tiedot siltä, missä email mätsää.
        public string GetCustomerInfo(string column, string email)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string returnResult = "";

            var getCmd = connection.CreateCommand();
            getCmd.CommandText = $"SELECT {column} FROM Asiakkaat WHERE email = $email";
            getCmd.Parameters.AddWithValue("$email", email);
            var result = getCmd.ExecuteReader();

            if (result.Read())
            {
                returnResult = result.GetString(0);
            }
            
            connection.Close();
            return returnResult;
        }

        // Poistaa tablesta Asiakkaat asiakkaan tiedot emailin perusteella
        public void DeleteCustomer(string email)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Asiakkaat SET nimi = '-', email = '-', osoite = '-', puhelinnumero = '-' WHERE email = $email";
                    cmd.Parameters.AddWithValue("$email", email);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Dictionary<string, object> GetCustomerByEmail(string email)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT * FROM Asiakkaat WHERE email = $email";
            selectCmd.Parameters.AddWithValue("$email", email);
            var reader = selectCmd.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            connection.Close();

            // Convert datatable to dictionary
            var result = dataTable.AsEnumerable().Select(row => dataTable.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName,column => row[column])).FirstOrDefault();

            return result;
        }

        // Printtaa consoleen tarjolla olevat sarakkeet haluamasta tablesta parametrillä tableName. En halunnut, että se näyttää "id" saraketta joten se skippaa ne!
        public void PrintColumnNames(SqliteConnection connection, string tableName)
        {
            Console.Write("Saatavilla olevat sarakkeet: ");
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName})";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // Get.Ordinal("name") Määrittää mitä tieto columnista haetaan. GetString(1)); tekisi saman asian!
                    string columnName = reader.GetString(reader.GetOrdinal("name"));

                    if (columnName != "id")
                    {
                        Console.Write(columnName + " ");
                    }
                }
            }
        }

        #endregion
      
        #region Products
        // Adds a product to the table
        public void AddProduct(string productName, string productCtgory, string productCtgory2, double productPrice, int productAmount, string productImg, string productDescription)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = 
            @"INSERT INTO Tuotteet (nimi, kategoria, kategoria_kaksi, hinta, kappalemaara, kuva, kuvaus) 
            VALUES ($nimi, $kategoria, $kategoria_kaksi, $hinta, $kappalemaara, $kuva, $kuvaus)";
            insertCmd.Parameters.AddWithValue("$nimi", productName);
            insertCmd.Parameters.AddWithValue("$kategoria", productCtgory);
            insertCmd.Parameters.AddWithValue("$kategoria_kaksi", productCtgory2);
            insertCmd.Parameters.AddWithValue("$hinta", productPrice);
            insertCmd.Parameters.AddWithValue("$kappalemaara", productAmount);
            insertCmd.Parameters.AddWithValue("$kuva", productImg);
            insertCmd.Parameters.AddWithValue("$kuvaus", productDescription);
            insertCmd.ExecuteNonQuery();

            connection.Close();
        }

        // Returns named product's info
        public Product GetProductInfo(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            Product product = null;
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText =
            @"SELECT *
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                // Create a new Product record and set its values from the database columns
                product = new Product(
                    result.GetInt32(0),      // id
                    result.GetString(1),     // name
                    result.GetString(2),     // productCtgory
                    result.GetString(3),     // productCtgory2
                    result.GetDouble(4),      // price
                    result.GetInt32(5),      // amount
                    result.GetString(6),     // img
                    result.GetString(7)      // description
                );
            }
            connection.Close();
            return product;
        }

        // Returns named product's id
        public int GetProductId(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            int id = 0;
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT id
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                id = result.GetInt32(0);
            }

            connection.Close();
            return id;
        }

        // Returns a list of all the product names in database
        public List<string> GetAllProductNames()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            List<string> names = new List<string>();
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT nimi
            FROM Tuotteet";
            var result = selectCmd.ExecuteReader();
            while (result.Read())
            {
                names.Add(result.GetString(0));
            }
            connection.Close();
            return names;
        }

        // Returns named product's category
        public string GetProductCategory(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string ctgory = "";
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT kategoria
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                ctgory = result.GetString(0);
            }
            connection.Close();
            return ctgory;
        }

        // Returns named product's second category
        public string GetProductCategory2(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string ctgory = "";
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT kategoria_kaksi
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                ctgory = result.GetString(0);
            }
            connection.Close();
            return ctgory;        
        }

        // Returns named product's price
        public double GetProductPrice(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            double price = 0;
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT hinta
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                price = result.GetDouble(0);
            }
            connection.Close();
            return price;
        }

        public double GetProductPriceById(int productId)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            double price = 0;
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT hinta
            FROM Tuotteet
            WHERE id = $id";
            selectCmd.Parameters.AddWithValue("$id", productId);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                price = result.GetDouble(0);
            }
            connection.Close();
            return price;
        }

        // Returns named product's amount
        public int GetProductAmount(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            int amount = 0;
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT kappalemaara
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                amount = result.GetInt32(0);
            }
            connection.Close();
            return amount;
        }

        // Returns named product's img
        public string GetProductImg(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string img = "";
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT kuva
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                img = result.GetString(0);
            }
            connection.Close();
            return img;
        }

        // Returns named product's description
        public string GetProductDescription(string productName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string description = "";
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = 
            @"SELECT kuvaus
            FROM Tuotteet
            WHERE nimi = $nimi";
            selectCmd.Parameters.AddWithValue("$nimi", productName);
            var result = selectCmd.ExecuteReader();
            if (result.Read())
            {
                description = result.GetString(0);
            }
            connection.Close();
            return description;
        }

        // Updates the product's name to the database
        public void UpdateProductName(string productName, string newName)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = 
            @"UPDATE Tuotteet
            SET nimi = $newName
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newName", newName);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's category to the database
        public void UpdateProductCategory(string productName, string newCategory)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kategoria = $newCategory
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newCategory", newCategory);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's second category to the database
        public void UpdateProductCategory2(string productName, string newCategory)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kategoria_kaksi = $newCategory
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newCategory", newCategory);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's price to the database
        public void UpdateProductPrice(string productName, double newPrice)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET hinta = $newPrice
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newPrice", newPrice);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the named product's amount to the database
        public void UpdateProductAmount(string productName, int newAmount)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kappalemaara = $newAmount
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newAmount", newAmount);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's amount by id to the database
        public void UpdateProductAmountById(int productId, int newAmount)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kappalemaara = $newAmount
            WHERE id = $productId";
            updateCmd.Parameters.AddWithValue("$newAmount", newAmount);
            updateCmd.Parameters.AddWithValue("$productId", productId);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's img to the database
        public void UpdateProductImg(string productName, string newImg)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kuva = $newImg
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newImg", newImg);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Updates the product's description to the database
        public void UpdateProductDescription(string productName, string newDescription)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText =
            @"UPDATE Tuotteet 
            SET kuvaus = $newDescription
            WHERE nimi = $productName";
            updateCmd.Parameters.AddWithValue("$newDescription", newDescription);
            updateCmd.Parameters.AddWithValue("$productName", productName);
            updateCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Method to update product details in the database
        public void UpdateProduct(string productName, Dictionary<string, JsonElement> updates)
        {
            // Establish a new connection to the database
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Create a new SQL command
            var updateCmd = connection.CreateCommand();

            // Construct the SET clause of the UPDATE statement from the updates dictionary
            string setClause = string.Join(", ", updates.Select(kv => $"{kv.Key} = @{kv.Key}"));
            // Set the command text to the UPDATE statement
            updateCmd.CommandText = $"UPDATE Tuotteet SET {setClause} WHERE nimi = @productName";

            // Loop through each update in the updates dictionary
            foreach (var update in updates)
            {
                var parameterName = update.Key;
                var parameterValue = update.Value;

                // Check the type of the JsonElement and convert it to the appropriate type
                if (parameterValue.ValueKind == JsonValueKind.Number && parameterValue.TryGetDecimal(out var decimalValue))
                {
                    // If the JsonElement is a number, add it as a decimal parameter to the command
                    updateCmd.Parameters.AddWithValue($"@{parameterName}", decimalValue);
                }
                else if (parameterValue.ValueKind == JsonValueKind.String)
                {
                    // If the JsonElement is a string, add it as a string parameter to the command
                    var stringValue = parameterValue.GetString();
                    updateCmd.Parameters.AddWithValue($"@{parameterName}", stringValue);
                }
                // Add more cases for other types as needed
            }

            // Add the product name as a parameter to the command
            updateCmd.Parameters.AddWithValue("@productName", productName);

            // Execute the command
            updateCmd.ExecuteNonQuery();

            // Close the connection to the database
            connection.Close();
        }

        // Deletes a product from the table
        public void DeleteProduct(string name)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var delCmd = connection.CreateCommand();
            delCmd.CommandText = 
            @"DELETE FROM Tuotteet
            WHERE nimi = $nimi";
            delCmd.Parameters.AddWithValue("$nimi", name);
            delCmd.ExecuteNonQuery();
            connection.Close();
        }

        // Prints all products to console (for testing purposes)
        public void PrintAllProductsFORTESTING()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            Console.WriteLine("Tuotteet:");
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Tuotteet";
            var product = selectCmd.ExecuteReader();
            
            while (product.Read())
            {
                Console.WriteLine($"{product["id"]} {product["nimi"]} {product["kategoria"]} {product["kategoria_kaksi"]} {product["hinta"]} {product["kappalemaara"]} {product["kuva"]} {product["kuvaus"]}");
            }
            connection.Close();
        }
        #endregion

        #region Purchase
        // Add purchase to TILAUKSET-table and returns the id of the new purchase
        public int AddPurchase(int asiakas_id, string toimitusosoite, double tilauksen_hinta, string lisatiedot)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string tilauspaiva = DateTime.Now.ToString("ddMMyyyy");
            string tilauksen_tila = "Tilaus vastaanotettu"; 

            var insertCmd2 = connection.CreateCommand();
            insertCmd2.CommandText = 
            @"INSERT INTO Tilaukset (asiakas_id, tilauspaiva, toimitusosoite, tilauksen_hinta, tilauksen_tila, lisatiedot) 
            VALUES ($asiakas_id, $tilauspaiva, $toimitusosoite, $tilauksen_hinta, $tilauksen_tila, $lisatiedot)
            RETURNING id";
            insertCmd2.Parameters.AddWithValue($"asiakas_id", asiakas_id);
            insertCmd2.Parameters.AddWithValue($"tilauspaiva", tilauspaiva);
            insertCmd2.Parameters.AddWithValue($"toimitusosoite", toimitusosoite);
            insertCmd2.Parameters.AddWithValue($"tilauksen_hinta", tilauksen_hinta);
            insertCmd2.Parameters.AddWithValue($"tilauksen_tila", tilauksen_tila);
            insertCmd2.Parameters.AddWithValue($"lisatiedot", lisatiedot);
            var newRecordId = Convert.ToInt32(insertCmd2.ExecuteScalar());

            connection.Close();
            return newRecordId;
        }

        // find purchase by purchase id
        public Purchase FindPurchaseId(int findOstos_id) 
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            // tilausrivit? // tilauksen hinta tulee toisesta taulusta, laske hinnat yhteen
            var selectPurchase = connection.CreateCommand();
            selectPurchase.CommandText = @"SELECT Tilaukset.id, Tilaukset.asiakas_id, Tilaukset.tilauspaiva, Tilaukset.toimitusosoite, Tilaukset.tilauksen_hinta, Tilaukset.tilauksen_tila, Tilaukset.lisatiedot, Asiakkaat.nimi FROM Tilaukset
            LEFT JOIN Asiakkaat ON Tilaukset.asiakas_id = Asiakkaat.id
            WHERE Tilaukset.id = $findOstos_id";
            selectPurchase.Parameters.AddWithValue("$findOstos_id", findOstos_id );
            
            using (var reader = selectPurchase.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new Purchase(
                        reader.GetInt32(0), //id
                        reader.GetInt32(1), //asiakasid
                        reader.GetString(2), //päivä
                        reader.GetString(3), //osoite
                        reader.GetDouble(4), //hinta
                        reader.GetString(5), // tila
                        reader.GetString(6), //lisatiedot
                        reader.GetString(7)  //nimi
                    );
                }
            }
            connection.Close();
            return null;
        }

        // Find purchase by customer id
        public Purchase FindPurchaseCustomerId(int findTilaus_tilaajanId)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            // tilauksen hinta tulee toisesta taulusta, SE PUUTTUU ??
            //joinaa siis myös tilausrivitaulujen yhteishinta
            var selectPurchase = connection.CreateCommand();
            selectPurchase.CommandText = @"SELECT Tilaukset.id, Tilaukset.asiakas_id, Tilaukset.tilauspaiva, Tilaukset.toimitusosoite, Tilaukset.tilauksen_hinta, Tilaukset.tilauksen_tila, Tilaukset.lisatiedot, Asiakkaat.nimi FROM Tilaukset
            LEFT JOIN Asiakkaat ON Tilaukset.asiakas_id = Asiakkaat.id
            WHERE Tilaukset.asiakas_id = $findTilaus_tilaajanId";
            selectPurchase.Parameters.AddWithValue("$findTilaus_tilaajanId", findTilaus_tilaajanId);
  
            using (var reader = selectPurchase.ExecuteReader())
            {
                if(reader.Read())
                {
                    return new Purchase(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetString(2),
                        reader.GetString(3),
                        reader.GetDouble(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        reader.GetString(7)
                    );
                }
        
            }
            connection.Close();
            return null;
        }

        // SELECT find purchase by customer name (antaako aina vain ensimmäisen?)
        public void FindPurchase_byCustomerName(string findTilaus_tilaajanNimi)
        {
            //tilauksen hinta laske tilausriveistä
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var selectPurchaseByName =connection.CreateCommand();
            selectPurchaseByName.CommandText = @"SELECT Tilaukset.id, Tilaukset.asiakas_id, Tilaukset.tilauspaiva, Tilaukset.toimitusosoite, Tilaukset.tilauksen_hinta, Tilaukset.tilauksen_tila, Tilaukset.lisatiedot, Asiakkaat.nimi FROM Tilaukset
            LEFT JOIN Asiakkaat ON Tilaukset.asiakas_id = Asiakkaat.id
            WHERE Asiakkaat.nimi = $findTilaus_tilaajanNimi";
            selectPurchaseByName.Parameters.AddWithValue("$findTilaus_tilaajanNimi", findTilaus_tilaajanNimi);

          //  var purchases = selectPurchaseByName
            connection.Close();
        } 
        // select find purchase by order date
        public void FindPurchase_bydate(string findPurchase_bydate)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            //laske tilauksen hinta tilausriveistä
            var selectPurchaseD = connection.CreateCommand();
            selectPurchaseD.CommandText = @"SELECT Tilaukset.id, Tilaukset.asiakas_id, Tilaukset.tilauspaiva, Tilaukset.toimitusosoite, Tilaukset.tilauksen_hinta, Tilaukset.tilauksen_tila, Tilaukset.lisatiedot, Asiakkaat.nimi FROM Tilaukset
            LEFT JOIN Asiakkaat ON Tilaukset.asiakas_id = Asiakkaat.id
            WHERE Tilaukset.tilauspaiva = $findPurchase_bydate";
            selectPurchaseD.Parameters.AddWithValue("$findPurchase_bydate", findPurchase_bydate);
            var purchaseD = selectPurchaseD.ExecuteReader();

            while(purchaseD.Read())
            {
                Console.WriteLine($"----------------------------\nTilausID: {purchaseD["id"]} | AsiakasID: {purchaseD["asiakas_id"]} | Asiakasnimi: {purchaseD["nimi"]} \nTilauspäivä: {purchaseD["tilauspaiva"]} | Toimitusosoite: {purchaseD["toimitusosoite"]} | Tilauksen hinta €: {purchaseD["tilauksen_hinta"]} | Tilauksen tila: {purchaseD["tilauksen_tila"]} | Lisätiedot: {purchaseD["lisatiedot"]}\n----------------------------");
            }

            connection.Open();
        }

        // DELETE purchase by purchase id
        public void DeletePurchaseById (int delTilaus)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var deletePurchaseCmd = connection.CreateCommand();
            deletePurchaseCmd.CommandText = "DELETE FROM Tilaukset WHERE Tilaukset.id = $delTilaus";
            deletePurchaseCmd.Parameters.AddWithValue("$delTilaus", delTilaus);
            deletePurchaseCmd.ExecuteNonQuery();

            connection.Close();
        }

        //print all purchases

        public void PrintAllPurchases()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var printPurchases = connection.CreateCommand();
            printPurchases.CommandText = "SELECT * FROM Tilaukset LEFT JOIN Asiakkaat ON Tilaukset.asiakas_id = Asiakkaat.id";
            var purchases = printPurchases.ExecuteReader();
            
            // Print purchases
            while (purchases.Read())
            {
                Console.WriteLine($"----------------------------\nTilausID: {purchases["id"]} | AsiakasID: {purchases["asiakas_id"]} | Asiakasnimi: {purchases["nimi"]} \nTilauspäivä: {purchases["tilauspaiva"]} | Toimitusosoite: {purchases["toimitusosoite"]} | Tilauksen hinta €: {purchases["tilauksen_hinta"]} | Tilauksen tila: {purchases["tilauksen_tila"]} | Lisätiedot: {purchases["lisatiedot"]}\n");
            }

            connection.Close();
        }

        #endregion

        #region Tilausrivit

        public void AddOrderLine(int tilaus_id, int tuote_id, int maara)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = 
                @"INSERT INTO Tilausrivit (tilaus_id, tuote_id, maara, hinta)
                VALUES ($tilaus_id, $tuote_id, $maara, $hinta)";
            insertCmd.Parameters.AddWithValue("$tilaus_id", tilaus_id);
            insertCmd.Parameters.AddWithValue("$tuote_id", tuote_id);
            insertCmd.Parameters.AddWithValue("$maara", maara);
            insertCmd.Parameters.AddWithValue("$hinta", GetProductPriceById(tuote_id) * maara);
            insertCmd.ExecuteNonQuery();

            connection.Close(); 
        }

        public void DeleteOrderLine(int deleteID)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = @"DELETE FROM Tilausrivit WHERE id = $deleteID";
            deleteCmd.Parameters.AddWithValue("$deleteID", deleteID);
            deleteCmd.ExecuteNonQuery();

            connection.Close();
        }

        public List<Dictionary<string, object>> GetOrderLine(int id)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT * FROM Tilausrivit WHERE id = $id";
            selectCmd.Parameters.AddWithValue("$id", id);
            var reader = selectCmd.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            connection.Close();

            // Convert datatable to list of dictionaries
            var result = dataTable.AsEnumerable().Select(row => dataTable.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName,column => row[column])).ToList();

            return result;
        }

        public List<Dictionary<string, object>> GetOrderLineByOrderId(int id)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT * FROM Tilausrivit WHERE tilaus_id = $id";
            selectCmd.Parameters.AddWithValue("$id", id);
            var reader = selectCmd.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            connection.Close();

            // Convert datatable to list of dictionaries
            var result = dataTable.AsEnumerable().Select(row => dataTable.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName,column => row[column])).ToList();

            return result;
        }

        public List<Dictionary<string, object>> GetOrderLineByProductId(int id)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT * FROM Tilausrivit WHERE tuote_id = $id";
            selectCmd.Parameters.AddWithValue("$id", id);
            var reader = selectCmd.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            connection.Close();

            // Convert datatable to list of dictionaries
            var result = dataTable.AsEnumerable().Select(row => dataTable.Columns.Cast<DataColumn>().ToDictionary(column => column.ColumnName,column => row[column])).ToList();

            return result;
        }

        public void UpdateOrderLine(int id, int tilaus_id, int tuote_id, int maara, double hinta)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"UPDATE Tilausrivit SET tilaus_id = $tilaus_id, tuote_id = $tuote_id, maara = $maara, hinta = $hinta WHERE id = $id";
            updateCmd.Parameters.AddWithValue("$id", id);
            updateCmd.Parameters.AddWithValue("$tilaus_id", tilaus_id);
            updateCmd.Parameters.AddWithValue("$tuote_id", tuote_id);
            updateCmd.Parameters.AddWithValue("$maara", maara);
            updateCmd.Parameters.AddWithValue("$hinta", hinta);
            updateCmd.ExecuteNonQuery();

            connection.Close(); 
        }

        #endregion
        
        #region Logins
        // Adds log in info to the table
        public void AddLogin(SqliteConnection connection, string customerEmail, string password)
        {
            // Check if the given email exists in the table for customers
            if(DoesEmailExist(customerEmail))
            {
                // Find the customer id
                int customerId = Convert.ToInt32(GetCustomerInfo("id", customerEmail));

                // Create salt by hashing current dateTime
                string hashedSalt = Convert.ToString(DateTime.Now.ToString().GetHashCode());
                // Add salt to password and hash them
                string hashedPassword = Convert.ToString((password + hashedSalt).GetHashCode());
                // Adds the log in info to table
                var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = 
                @"INSERT INTO Kirjautumistiedot (asiakas_id, salasana_hash, salasana_salt) 
                VALUES ($asiakas_id, $salasana_hash, $salasana_salt)";
                insertCmd.Parameters.AddWithValue("$asiakas_id", customerId);
                insertCmd.Parameters.AddWithValue("$salasana_hash", hashedPassword);
                insertCmd.Parameters.AddWithValue("$salasana_salt", hashedSalt);
                insertCmd.ExecuteNonQuery();
            }
        }

        // Checks if the given email and password match in the table
        public bool CheckPassword(SqliteConnection connection, string customerEmail, string password)
        {
            // Check if the given email exists in the table for customers
            if (DoesEmailExist(customerEmail))
            {
                // Finds the salt used by email
                string salt = "";
                var selectSaltCmd = connection.CreateCommand();
                selectSaltCmd.CommandText =
                @"SELECT Kirjautumistiedot.salasana_salt
                FROM Kirjautumistiedot JOIN Asiakkaat ON Kirjautumistiedot.asiakas_id = Asiakkaat.id
                WHERE Asiakkaat.email = $customer_email";
                selectSaltCmd.Parameters.AddWithValue("$customer_email", customerEmail);
                var result = selectSaltCmd.ExecuteReader();
                if (result.Read())
                {
                    salt = result.GetString(0);
                }
                // Add the correct salt to the given password and hash them
                string inputHashed = Convert.ToString((password + salt).GetHashCode());

                // Find the correct hashed password by email
                string customerPassword = "";
                var selectPasswordCmd = connection.CreateCommand();
                selectPasswordCmd.CommandText =
                @"SELECT Kirjautumistiedot.salasana_hash
                FROM Kirjautumistiedot JOIN Asiakkaat ON Kirjautumistiedot.asiakas_id = Asiakkaat.id
                WHERE Asiakkaat.email = $customer_email";
                selectPasswordCmd.Parameters.AddWithValue("$customer_email", customerEmail);
                var result2 = selectPasswordCmd.ExecuteReader();
                if (result2.Read())
                {
                    customerPassword = result2.GetString(0);
                }

                // Compare the hashed given password to the correct hashed password
                return inputHashed == customerPassword;
            }
            else
                return false;
        }

        // Delete Login from table
        public void DeleteLogin(SqliteConnection connection, int customerId)
        {
            var delCmd = connection.CreateCommand();
            delCmd.CommandText = 
            @"DELETE FROM Kirjautumistiedot
            WHERE asiakas_id = $id";
            delCmd.Parameters.AddWithValue("$id", customerId);
            delCmd.ExecuteNonQuery();
        }

        // Prints all Logins for testing
        public void PrintAllLogins(SqliteConnection connection)
        {
            Console.WriteLine("Loginit:");
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Kirjautumistiedot";
            var login = selectCmd.ExecuteReader();
            
            while (login.Read())
            {
                Console.WriteLine($"{login["id"]} {login["asiakas_id"]} {login["salasana_hash"]} {login["salasana_salt"]}");
            }
        }

        #endregion

        #region UsefulFunctions

        public bool DoesEmailExist(string email)
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            // Laskee monta emailia mätsää parametrin emailiin Tablesta 'Asiakkaat'
            cmd.CommandText = "SELECT COUNT(*) FROM Asiakkaat WHERE email = $email";
            cmd.Parameters.AddWithValue("$email", email);

            // ExecuteScalar palauttaa yksittäisen arvon kyselystä (määrän)

            // ExecuteScalar palauttaa kyselyn yksittäisen arvon (määrän) objektina
            // (long) varmistaa, että käsittelemme tulosta jonkinlaisena lukuna, normaalisti ExecuteScaler() palauttaa objektin
            var count = (long)cmd.ExecuteScalar();

            // Tarkista sähköpostin olemassaolon tarkistaminen, onko määrä suurempi kuin 0
            connection.Close();
            return count > 0;
        }

        // Palauttaa listan tablen columneista, jonka parametri annetaan tableName:ille
        public List<string> GetColumnNames(SqliteConnection connection, string tableName)
        {
            List <string> columns = new List<string>();

            var getCmd = connection.CreateCommand();
            getCmd.CommandText = $"PRAGMA table_info({tableName})";
            var result = getCmd.ExecuteReader();

            while (result.Read())
            {
                
                columns.Add(result.GetString(1)); // 1 koska, muuten se tulostaa vaan 1,2,3,4. Indexistä 1 tulostaa columnin nimen.
            }
            return columns;
        }

        #endregion

        #region Maksut
        public void AddPayment(SqliteConnection connection, int orderId, string paymentMethod, double sum)
        {
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"INSERT INTO Maksut (tilaus_id, maksutapa, summa)
            VALUES ($tilaus_id, $maksutapa, $summa)";
            insertCmd.Parameters.AddWithValue("$tilaus_id", orderId);
            insertCmd.Parameters.AddWithValue("$maksutapa", paymentMethod);
            insertCmd.Parameters.AddWithValue("$summa", sum);
            insertCmd.ExecuteNonQuery();
        }

        //For testing, get info
        /*public void TestPayment(SqliteConnection connection, int orderId)
        {
            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"SELECT maksutapa, summa FROM Maksut
            WHERE tilaus_id = $orderId";
            selectCmd.Parameters.AddWithValue("$orderId", orderId);
            var payments = selectCmd.ExecuteReader();

            while(payments.Read())
            {
                Console.WriteLine($"Maksutapa: {payments["maksutapa"]}, summa: {payments["summa"]}");
            }
        }*/

        #endregion

    }
