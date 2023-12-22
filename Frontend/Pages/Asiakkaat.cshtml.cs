using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Web;

namespace VerkkokauppaFrontti.Pages
{

    // Customer class for handling customer info
    public class Customer
    {
        public int id { get; set; }
        public string? nimi { get; set; }
        public string? email { get; set; }
        public string? osoite { get; set; }
        public string? puhelinnumero { get; set; }
    }

    

    public class AsiakkaatModel : PageModel
    {
        private readonly string _baseUrl;
        public string BaseUrl => _baseUrl;

        [BindProperty]
        public string? Email { get; set; }
        public IFormFile Img { get; set; }
        public Dictionary<string, object>? CustomerInfo { get; set; } // This is for the commented out handlers
        public Customer? Customer { get; set; } // This is for the handlers that return Customer object

        // Add a ProductInfo property
        public ProductInfo Product { get; set; }
        private readonly IConfiguration _configuration;

        public List<string> ProductNames { get; set; }

        // Or add a list of ProductInfo
        public List<ProductInfo> Products { get; set; }

        // This is needed for the _baseUrl that replaces http://localhost:5198
        // You can modify the _baseUrl in appsettings.json file
        public AsiakkaatModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl");
            // Customer = new Customer();
            // Product = new ProductInfo();
            ProductNames = new List<string>();
            // Products = new List<ProductInfo>();
        }

        public async Task OnGetAsync()
        {
            ProductNames = await GetAllProductNames();
        }

        private async Task<List<string>> GetAllProductNames()
        {
            string url = $"{_baseUrl}/getallproductnames";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                List<string> productNames = JsonConvert.DeserializeObject<List<string>>(data);
                
                // Sort the list in alphabetical order
                productNames.Sort();

                return productNames;
            }
            else
            {
                // handle error
                return new List<string>();
            }
        }

        #region Asiakkaat
       // This is for AddCustomer button
        public IActionResult OnPostAddCustomer(string name, string email, string address, string phonenumber)
        {
            // Example using HttpClient (you may need to inject HttpClient into your class)
            using (var client = new HttpClient())
            {
                var data = new { name, email, address, phonenumber };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var apiUrl = $"{_baseUrl}/addcustomer";
                var response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Handle success
                    return RedirectToPage("/asiakkaat");
                }
                else
                {
                    // Handle failure
                    return Page();
                }
            }
        }

        // This is for GetCustomer button
        // This handler gives customer name, email, address and phonenumber as a Customer object
        // In Asiakkaat.cshtml you can now access Customer properties like this:
        // <p>Asiakkaan nimi: @Model.Customer.nimi
        public async Task<IActionResult> OnPostGetCustomer()
        {
            // Create a new HttpClient object
            using var client = new HttpClient();
            // Call the API
            var apiUrl = $"{_baseUrl}/getcustomerbyemail/{Email}";
            // Get the response
            var response = await client.GetAsync(apiUrl);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                // Assuming your server returns JSON data
                var jsonString = await response.Content.ReadAsStringAsync();
                // Deserialize the JSON data to Customer object
                var customer = JsonConvert.DeserializeObject<Customer>(jsonString);

                if (customer == null)
                {
                    // Log if email not found
                    Console.WriteLine("Email not found");
                }
                Customer = customer;
            }
            else
            {
                // Log that the request failed
                Console.WriteLine("Request failed with status code: " + response.StatusCode);
            }

            // Return the same page with the updated Customer
            return Page();
        }

        // This is for UpdateCustomer button
        public async Task<IActionResult> OnPostUpdateCustomer(string column, string newInfo, string email)
        {
            // If the column is 'email', convert newInfo to lowercase
            if (column == "email")
            {
                newInfo = newInfo.ToLower();
            }

            using var client = new HttpClient();
            // Encode the parameters to URL format (this is needed if the parameters contain spaces or special characters)
            var encodedColumn = HttpUtility.UrlEncode(column).Replace("+", "%20");
            var encodedNewInfo = HttpUtility.UrlEncode(newInfo).Replace("+", "%20");
            var encodedEmail = HttpUtility.UrlEncode(email).Replace("+", "%20");
            // Call the API
            var apiUrl = $"{_baseUrl}/updatecustomer/{encodedColumn}/{encodedNewInfo}/{encodedEmail}";
            var response = await client.PutAsync(apiUrl, null);

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/asiakkaat");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // The requested resource was not found on the server.
                Console.WriteLine("Resource not found: " + apiUrl);
            }
            else
            {
                // The request failed. You might want to handle this case.
                Console.WriteLine("Request failed with status code: " + response.StatusCode);
            }

            return Page();
        }

        // This is for DeleteCustomer button
        public async Task<IActionResult> OnPostDeleteCustomer(string email)
        {
            using var client = new HttpClient();
            var encodedEmail = HttpUtility.UrlEncode(email).Replace("+", "%20");
            var apiUrl = $"{_baseUrl}/deletecustomer/{encodedEmail}";
            var response = await client.PutAsync(apiUrl, null);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("/asiakkaat");
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("Resource not found: " + apiUrl);
            }
            else
            {
                Console.WriteLine("Request failed with status code: " + response.StatusCode);
            }

            return Page();
        }
        #endregion

        #region Tuotteet

        public IActionResult OnPostAddProduct(string name, string productCtgory, string productCtgory2, double price, int amount, IFormFile img, string description)
        {
            // Save the uploaded file to the wwwroot/images directory
            var filePath = Path.Combine("wwwroot/images", img.FileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                img.CopyTo(stream);
            }

            // The image name that will be saved in the database
            var imgPathInDb = img.FileName;

            using (var client = new HttpClient())
            {
                // Include imgPathInDb in the data object instead of img
                var data = new { name, productCtgory, productCtgory2, price, amount, img = imgPathInDb, description };
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var apiUrl = $"{_baseUrl}/addproduct";
                var response = client.PostAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Handle success
                    return RedirectToPage("/asiakkaat");
                }
                else
                {
                    // Handle failure
                    return Page();
                }
            }
        }

        // This is for the UpdateProduct form
        public IActionResult OnPostUpdateProduct()
        {
            // Retrieve the product name from the form data
            var productName = Request.Form["productName"];
            var updates = new Dictionary<string, string>();

            // Check each form field to see if it's not empty. If it's not, add it to the updates dictionary
            if (!string.IsNullOrEmpty(Request.Form["nimi"]))
            {
                updates["nimi"] = Request.Form["nimi"];
            }
            if (!string.IsNullOrEmpty(Request.Form["kategoria"]))
            {
                updates["kategoria"] = Request.Form["kategoria"];
            }
            if (!string.IsNullOrEmpty(Request.Form["kategoria2"]))
            {
                updates["kategoria2"] = Request.Form["kategoria2"];
            }
            // Check the "hinta" field and validate that it's a valid double
            if (!string.IsNullOrEmpty(Request.Form["hinta"]))
            {
                if (double.TryParse(Request.Form["hinta"], out double hinta))
                {
                    updates["hinta"] = hinta.ToString();
                }
            }
            // Check the "maara" field and validate that it's a valid integer
            if (!string.IsNullOrEmpty(Request.Form["maara"]))
            {
                if (int.TryParse(Request.Form["maara"], out int maara))
                {
                    updates["maara"] = maara.ToString();
                }
            }
            if (!string.IsNullOrEmpty(Request.Form["kuva"]))
            {
                updates["kuva"] = Request.Form["kuva"];
            }
            if (!string.IsNullOrEmpty(Request.Form["kuvaus"]))
            {
                updates["kuvaus"] = Request.Form["kuvaus"];
            }

            // Create a new HttpClient to send a request to the API
            using (var client = new HttpClient())
            {
                // Convert the updates dictionary to a JSON string
                var content = new StringContent(JsonConvert.SerializeObject(updates), Encoding.UTF8, "application/json");
                var apiUrl = $"{_baseUrl}/updateproduct/{productName}";
                // Send a PUT request to the API with the updates
                var response = client.PutAsync(apiUrl, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    // If the request was successful, redirect to the "asiakkaat" page
                    return RedirectToPage("/asiakkaat");
                }
                else
                {
                    // Handle failure
                    return Page();
                }
            }
        }

        public async Task<IActionResult> OnPostUpdateProductImageAsync(string productName)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Save the uploaded image to the wwwroot/images/ folder
            var filePath = Path.Combine("wwwroot/images", Img.FileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                await Img.CopyToAsync(stream);
            }

            // Call the /updateproductimg/{productName}/{newImg} endpoint to update the image name in the database
            string url = $"{_baseUrl}/updateproductimg/{productName}/{Img.FileName}";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.PutAsync(url, null);

            if (!response.IsSuccessStatusCode)
            {
                // handle error
            }

            return RedirectToPage();
        }

        #endregion
    }
}
