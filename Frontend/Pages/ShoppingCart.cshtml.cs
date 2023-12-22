using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static VerkkokauppaFrontti.Pages.IndexModel;
using static VerkkokauppaFrontti.Pages.CartItem;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel.Design.Serialization;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace VerkkokauppaFrontti.Pages
{
    public class ShoppingCartModel : PageModel
    {
        public List<CartItem> currentShoppingCartList { get; set; }

        // ShoppingCartModel.theCart.currentShoppingCartList <-- list of items in the cart
        // This is a unique static object that's used for everything. Usable from other files. Doesn't reset. Call this.
        public static ShoppingCartModel theCart;

        private string _baseUrl;
        private IConfiguration config;

        // Sum of the cart items
        public double sum;

        // Has the customer added their info?
        public bool customerStatus;

        // Object for comparing the order's items with database's saldo
        public ProductInfo dbProduct = new ProductInfo();

        // These are for storing the name and current amount of the product which has not enough amount in the database
        // so they can be used and printed via other methods
        public string notAvailableProduct = "";
        public int availableAmount;


        static ShoppingCartModel()
        {
            theCart = new ShoppingCartModel();
            theCart.currentShoppingCartList = new List<CartItem>();

            // Manually building IConfiguration because static constructor doesn't take parameters
            // Used for getting the _baseUrl from appsettings.json
            var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
            theCart.config = configurationBuilder.Build();

            theCart._baseUrl = theCart.config.GetValue<string>("ApiSettings:BaseUrl");
        }

        // Removes one item from the cart (by index)
        public IActionResult OnPostRemoveFromTheCart(){
            int cartIndex = Convert.ToInt32(Request.Form["CartIndex"]);
            ShoppingCartModel.theCart.currentShoppingCartList.RemoveAt(cartIndex);
            CountTheSum();
            return Page();
        }

        // Clears the whole cart
        public IActionResult OnPostEmptyTheCart(){
            ShoppingCartModel.theCart.currentShoppingCartList.Clear();
            CountTheSum();
            return Page();
        }

        // Checks if order is possible to deliver
        public async Task<bool> CheckProductAvailability()
        {
            bool isAvailable = false;

            for(int i = 0; i < theCart.currentShoppingCartList.Count; i++)
            {
                // Gets the product-info from database
                string url = $"{theCart._baseUrl}/getproductinfo/{theCart.currentShoppingCartList[i].Name}";

                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();

                    // Sets the data to the ProductInfo-object
                    this.dbProduct = JsonConvert.DeserializeObject<ProductInfo>(data);

                        // Check if there is enough amount in database
                        if(dbProduct.Amount >= theCart.currentShoppingCartList[i].Amount)
                        {
                            isAvailable = true;
                        }
                        else
                        {
                            // If there is not enough amount in database, sets that product's name and current amount in variables and breaks
                            notAvailableProduct = theCart.currentShoppingCartList[i].Name;
                            availableAmount = dbProduct.Amount;
                            isAvailable = false;
                            return isAvailable;
                        }
                }
                else
                {
                    Console.WriteLine("Error fetching product -data. Status code: " + response.StatusCode);
                }
            }
            return isAvailable;
        }

        public IActionResult OnPostSetCustomerStatus(){
            string custStatus = Request.Form["customerStatus"];
            if(custStatus == "Kyllä")
            {
                ShoppingCartModel.theCart.customerStatus = true;
            }
            else if(custStatus == "Ei")
            {
                ShoppingCartModel.theCart.customerStatus = false;
            }
            return Page();
        }

        // This is for AddCustomer button
        public IActionResult OnPostAddCustomer(string name, string email, string address, string phonenumber)
        {
            using (var client = new HttpClient())
            {
                var data = new { name, email, address, phonenumber };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var apiUrl = $"{theCart._baseUrl}/addcustomer";
                var response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Handle success
                    theCart.customerStatus = true;
                    return RedirectToPage("/ShoppingCart");
                }
                else
                {
                    // Handle failure
                    return Page();
                }
            }
        }

        public async Task<int> GetCustomerId(string email)
        {
            int customerId = -1;
            using (var client = new HttpClient())
            {
                var apiUrl = $"{theCart._baseUrl}/getcustomerinfo/id/{email}";
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var id = await response.Content.ReadAsStringAsync();
                    customerId = Convert.ToInt32(id); 
                }
                else
                {
                    // Log that the request failed
                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
                }
            }
            return customerId;
        }

        public async Task<int> AddPurchase(int asiakas_id, string toimitusosoite, string lisatiedot)
        {
            int purchaseId = -1;
            using (var client = new HttpClient())
            {
                double tilauksen_hinta = CountTheSum();
                var data = new { asiakas_id, toimitusosoite, tilauksen_hinta, lisatiedot };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var apiUrl = $"{theCart._baseUrl}/addpurchase";
                var response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    // Handle success
                    Console.WriteLine("Tilauksen teko onnistui!");
                    var id = await response.Content.ReadAsStringAsync();
                    purchaseId = Convert.ToInt32(id); 
                }
                else
                {
                    // Handle failure
                    Console.WriteLine("Tilaus ei onnistunut: " + response.StatusCode);
                }
                return purchaseId;
            }
        }

        public async void AddOrderLine(int tilaus_id, int tuote_id, int maara)
        {
            using (var client = new HttpClient())
            {
                var data = new { tilaus_id, tuote_id, maara };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var apiUrl = $"{theCart._baseUrl}/addorderline";
                var response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    // Handle success
                    // Debug:
                    Console.WriteLine("Tilausrivi lisätty! tilausId: " + tilaus_id + " tuoteId: " + tuote_id + " määrä: " + maara);
                }
                else
                {
                    // Handle failure
                    // Debug:
                    Console.WriteLine("Tilausrivin lisäys epäonnistui " + response.StatusCode);
                }
            }
        }

        public async Task<bool> CustomerExist(string email)
        {
            bool exists = false;
            using (var client = new HttpClient())
            {
                var apiUrl = $"{theCart._baseUrl}/customerexist/{email}";
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var existsString = await response.Content.ReadAsStringAsync();
                    exists = Convert.ToBoolean(existsString); 
                }
                else
                {
                    // Log that the request failed
                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
                }
            }
            return exists;
        }

        public async Task<IActionResult> OnPostPrePurchase(string email, string address, string additionalInfo)
        {
            if(await CustomerExist(email))
            {
                if(await CheckProductAvailability())
                {
                    // Get the customer_id from the email
                    int customerId = await GetCustomerId(email);
                    // Adds the purchase and returns the id
                    int purchaseId = await AddPurchase(customerId, address, additionalInfo);

                    // Debug:
                    //Console.WriteLine("Tilauksen id: " + purchaseId);
                    
                    foreach(var item in theCart.currentShoppingCartList)
                    {
                        AddOrderLine(purchaseId, item.Id, item.Amount);
                        await UpdateProductAmount(item);
                    }
                    // Sends the succesfull order -message
                    TempData["SuccessMessage"] = "Kiitos tilauksestasi! Tervetuloa uudestaan!";

                    return OnPostEmptyTheCart();
                }
                else
                {
                    // Debug:
                    Console.WriteLine("Ei varastossa");
                    // Send the order failed -message
                    TempData["ErrorMessage"] = "Tilausta ei voida toimittaa.\nTuotetta " + notAvailableProduct + " on vain " + availableAmount + "kpl varastossa.";
                    return Page();
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Asiakasta ei löytynyt";
                return Page();
            }
        }
        public double CountTheSum()
        {
            sum = 0;

            foreach(var item in theCart.currentShoppingCartList)
            {
                sum += item.Amount * item.Price;
            }

            // This part below rounds the double to two decimals
            double roundedSum = 0.00;

            // Converts the sum first to string to set the two decimals, then converts the string back to double
            if(double.TryParse(sum.ToString("0.00"), out roundedSum))
            {
                return roundedSum;
            }
            return sum;
        }

        // Updates the product's amount in database according the order
        public async Task UpdateProductAmount(CartItem item)
        {
            // Gets the product-info from database to get the current amount
            string url = $"{theCart._baseUrl}/getproductinfo/{item.Name}";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();

                    // Sets the data to the ProductInfo-object
                    this.dbProduct = JsonConvert.DeserializeObject<ProductInfo>(data);
                }
                else
                {
                    Console.WriteLine("Error fetching product data in UpdateProductAmount -method " + response.StatusCode);
                }
            
            // Put new amount to the database
            using(var client2 = new HttpClient())
            {
                var productId = dbProduct.Id;
                var newAmount = dbProduct.Amount - item.Amount;
                // Calls the API
                var apiUrl = $"{theCart._baseUrl}/updateproductamountbyid/{productId}/{newAmount}";
                var response2 = await client.PutAsync(apiUrl, null);

                // Check if the request was successful
                if (response2.IsSuccessStatusCode)
                {
                    Console.WriteLine("Product's amount updated.");
                }
                else
                {
                    Console.WriteLine("Request failed in UpdateProductAmount(CartItem item) -method: " + response2.StatusCode);
                }
            }
        }
    }
}
