using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http;
using System.Runtime.Serialization.Formatters.Binary;

namespace VerkkokauppaFrontti.Pages
{
    public class IndexModel : PageModel
    {
        // Muuttujat:
        private readonly ILogger<IndexModel> _logger;
        // For handling error-messages
        public string errorMessage { get; set; }
        // For handling product-values from database
        public ProductInfo productInfo;
        // List for all products with all values
        public List<ProductInfo> allProductsList;
        private readonly string _baseUrl;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            errorMessage = "";
            this.productInfo = new ProductInfo();
            this.allProductsList = new List<ProductInfo>();
            _baseUrl = configuration.GetValue<string>("ApiSettings:BaseUrl");
        }
        // This is for the session to store variables and pass them to another files
        // public void ConfigureServices(IServiceCollection services)
        // {
        //     services.AddSession();
        // }
        // public void Configure(IApplicationBuilder app/*, IHostingEnvironment env*/)
        // {
        //     app.UseSession();
        //     //app.UseMvc();
        // }

        // Gets the list of names of all products
        public async Task OnGet()
        {
            try
            {
            string url = $"{_baseUrl}/getallproductnames";

            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string data = await response.Content.ReadAsStringAsync();

                    // Sets the productnames to the list
                    List<string> productList = JsonConvert.DeserializeObject<List<string>>(data);
                    // Debugging:
                    // productList.ForEach(Console.WriteLine);

                    // Await oli joku homma, että ilman sitä current metodi olis runnattu loppuun ja sit vasta kutsuttu tää
                    await GetAllProductInfo(productList);
                }
                else
                {
                    errorMessage = "Error fetching product names -data. Status code: " + response.StatusCode;
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Connection error while searching product names" + ex.Message;
            }
        }
        // Method to get all products and their infos from database
        public async Task GetAllProductInfo(List<string> productNames)
        {
            try
            {
                foreach (var product in productNames)
                {
                    string url = $"{_baseUrl}/getproductinfo/{product}";

                    HttpClient client = new HttpClient();
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string data = await response.Content.ReadAsStringAsync();

                        // Sets the data to the ProductInfo-object and adds it to the list
                        this.productInfo = JsonConvert.DeserializeObject<ProductInfo>(data);
                        this.allProductsList.Add(productInfo);
                        // HUOM! Tässä kohtaa productInfo -objektiin jää viimeisin tuote koska tässä ei luoda joka kerta uutta objektia
                        // Kaikki on kuitenkin allProductList -listassa, joten useampien olioiden säilöminen vielä erikseen ei liene tarpeellista?
                    }
                    else
                    {
                        errorMessage = "Error fetching product -data. Status code: " + response.StatusCode;
                    }
                }

                // Sort the allProductsList by Id (Näyttää tuotteet aina samassa järjestyksessä)
                this.allProductsList = this.allProductsList.OrderBy(p => p.Id).ToList();
            }
            catch (Exception ex)
            {
                errorMessage = "Connection error while searching products: " + ex.Message;
            }
        }

        public IActionResult OnPostAddToCart()
        {
            // Takes the product-information from the form and sets to variables
            int productId = Convert.ToInt32(Request.Form["ProductId"]);
            string productName = Request.Form["ProductName"];
            double productPrice = Convert.ToDouble(Request.Form["ProductPrice"]);
            int productAmount = Convert.ToInt32(Request.Form["ProductAmount"]);

            // Creates a new CartItem and adds it to the Cart
            CartItem newItem = new CartItem(productId, productName, productPrice, productAmount);

            // Check if the item is already in the cart-list:
            // FirstOrDefault-method is used to find the first element in the list that satisfies a specified condition, 'id' in this case.
            // If no such element is found, it returns null for reference types.
            CartItem existingItem = ShoppingCartModel.theCart.currentShoppingCartList.FirstOrDefault(item => item.Id == newItem.Id);

            // If the item is not in the cart-list, adds it
            if (existingItem == null)
            {
                ShoppingCartModel.theCart.currentShoppingCartList.Add(newItem);
            }
            // If the item is already in the cart-list, updates the amount
            else
            {
                existingItem.Amount += newItem.Amount;
            }
            
            // Debugging:
            // Console.WriteLine("Ostoskorissa: ");
            // foreach(var cartItem in ShoppingCartModel.theCart.currentShoppingCartList)
            // {
            //     Console.WriteLine(cartItem.Name);
            // }

            // Return to the product-page after adding the product to the cart
            return RedirectToPage("/index");
        }
    }
}