using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
// JOS HERJAA NIIN RUNNAA: dotnet add package Microsoft.Data.Sqlite

var builder = WebApplication.CreateBuilder(args);

// Add Cross-Origin Resource Sharing (CORS) services to the application's service collection
builder.Services.AddCors(options =>
{
    // Define a new CORS policy
    options.AddPolicy("AllowLocalhost5020",
        builder =>
        {
            // Set the allowed origin for this policy to "http://localhost:5020"
            builder.WithOrigins("http://localhost:5020")
                   // Allow any header in the request
                   .AllowAnyHeader()
                   // Allow any HTTP method in the request
                   .AllowAnyMethod();
        });
});

var app = builder.Build();

// Apply the CORS policy named "AllowLocalhost5020" to the application. 
// This will allow the application to accept cross-origin requests from "http://localhost:5020".
app.UseCors("AllowLocalhost5020");

var database = new Databaselogics();
database.CreateTables();

app.MapGet("/", () => "Tervetuloa verkkokauppaan!");

// Nimeä polku SAMALLA NIMELLÄ KUIN METODI jota kutsutaan
// Tee POSTEIHIN recordi Databaselogicsiin
// Muuta Databaselogicsiin metodeihin connection niin, että ei ota sitä parametrina, vaan metodin sisällä luo ja open ja close

#region ProductMapping
    /* addProductin .json-malli: 
    {
        "name": "kukka",
        "productCtgory": "korvikset",
        "productCtgory2": "napit",
        "price": 10.75,
        "amount": 5,
        "img": "kuva.png",
        "description": "hienot"
    }
     */
    app.MapPost("/addproduct", (Product product) => database.AddProduct(product.name, product.productCtgory, product.productCtgory2, product.price, product.amount, product.img, product.description));
    app.MapGet("/getproductinfo/{productName}", (string productName) => database.GetProductInfo(productName));
    app.MapGet("/getproductid/{productName}", (string productName) => database.GetProductId(productName));
    app.MapGet("/getallproductnames", () => database.GetAllProductNames());
    app.MapPut("/updateproductimg/{productName}/{newImg}", (string productName, string newImg) => database.UpdateProductImg(productName, newImg));
    app.MapPut("/updateproductamountbyid/{productId}/{newAmount}", (int productId, int newAmount) => database.UpdateProductAmountById(productId, newAmount));
    app.MapPut("/updateproduct/{productName}", (string productName, JsonDocument updatesJson) =>
    {
        var updates = updatesJson.RootElement.EnumerateObject().ToDictionary(e => e.Name, e => e.Value);
        database.UpdateProduct(productName, updates);
    });
#endregion

#region CustomerMapping
// Add Customer - http://localhost:5198/addcustomer - Body JSON: {"name":"Anssi Peltola","email": "anssipeltola@hotmail.com", "address": "Itsenäisyydenkatu 18", "phonenumber": "0400244925"}
app.MapPost("/addcustomer", (Asiakas asiakas) => 
{
    database.AddCustomer(asiakas.name, asiakas.email, asiakas.address, asiakas.phonenumber);
    return Results.Ok(new { message = "Customer added successfully" });
});

// Get Customer Info from wanted column by email- http://localhost:5198/getcustomerinfo/nimi/anssipeltola@hotmail.com
app.MapGet("/getcustomerinfo/{column}/{email}", (string column, string email) => database.GetCustomerInfo(column, email)); 

// Update Customer Info - http://localhost:5198/updatecustomer/nimi/Anssi%20Peltola/anssipeltola%40hotmail.com %20 = välilyönti %40 = @
app.MapPut("/updatecustomer/{column}/{newInfo}/{email}", (string column, string newInfo, string email) => database.UpdateCustomer(column, newInfo, email));

// Delete Customer (Nulls all columns but leaves ID primary key as it were) - http://localhost:5198/deletecustomer/anssipeltola%40hotmail.com
app.MapPut("/deletecustomer/{email}", (string email) => database.DeleteCustomer(email));

// Get Customer By Email - http://localhost:5198/getcustomerbyemail/anssipeltola%40hotmail.com
app.MapGet("/getcustomerbyemail/{email}", (string email) => database.GetCustomerByEmail(email));

app.MapGet("/customerexist/{email}", (string email) => database.DoesEmailExist(email));
#endregion

#region TilausriviMapping
// Add orderline - http://localhost:5198/addorderline - Body JSON: {"tilaus_id": 1, "tuote_id": 1, "maara": 1}
app.MapPost("/addorderline", (Tilasrivi tilausrivi) => 
{
    database.AddOrderLine(tilausrivi.tilaus_id, tilausrivi.tuote_id, tilausrivi.maara);
    return Results.Ok();
});

// Delete orderline - http://localhost:5198/deleteorderline/1
app.MapDelete("/deleteorderline/{id}", (int id) => database.DeleteOrderLine(id));

// Get orderline - http://localhost:5198/getorderline/1 
app.MapGet("/getorderline/{id}", (int id) => database.GetOrderLine(id));

// Get orderline by tilaus id - http://localhost:5198/getorderlinebyorderid/1
app.MapGet("/getorderlinebyorderid/{id}", (int id) => database.GetOrderLineByOrderId(id));

// Get orderline by tuote id - http://localhost:5198/getorderlinebyproductid/1
app.MapGet("/getorderlinebyproductid/{id}", (int id) => database.GetOrderLineByProductId(id));

// UPDATE orderline - http://localhost:5198/updateorderline/1/1/1/1/1
app.MapPut("/updateorderline/{id}/{tilaus_id}/{tuote_id}/{maara}/{hinta}", (int id, int tilaus_id, int tuote_id, int maara, int hinta) => database.UpdateOrderLine(id, tilaus_id, tuote_id, maara, hinta));
#endregion

#region ReviewsMapping

//Pitäiskö tehdä vielä metodi, että string ja numeric review palautuis samassa molemmat
app.MapGet("/getreview/{name}", (string name) => database.GetReview(name));
app.MapGet("/getnumericreview/{name}", (string name) => database.GetNumericReview(name));
app.MapGet("/getcustomeridfromreview/{review}", (string review) => database.GetCustomerIdFromReview(review));

app.MapPost("/addreview", (AddReview review) => database.AddReview(review.ProductId, review.CustomerId, review.Review, review.NumReview));

app.MapDelete("/deletereview/{review}", (string review) => database.DeleteReview(review));

#endregion

#region PurchasesMapping

// Add Purchase - http://localhost:5198/addpurchase 
app.MapPost("/addpurchase", (Tilaukset tilaus) => database.AddPurchase(tilaus.asiakas_id, tilaus.toimitusosoite, tilaus.tilauksen_hinta,  tilaus.lisatiedot)); //tilaus.tilauspaiva, tilaus.tilauksen_tila,

// Find Purchase by Id - http://localhost:5198/findpurchaseid/id
app.MapGet("/findpurchaseid/{id}", (int id) => database.FindPurchaseId(id));

// Find Purchase by CustomerId - http://localhost:5198/findpurchasecustomerid/{customer_id}
//app.MapGet("/findpurchasecustomerid/{customer_id}", (int customer_id) => database.FindPurchaseCustomerId(customer_id));

// Find Purchase by date - http://localhost:5198/findpurchasebydate/{date} - example 03112023
//app.MapGet("/findpurchasebydate/{date}", (string date) => database.FindPurchase_bydate(date));

// Delete Purchase by Id - http://localhost:5198/deletePurchaseById/{id}
//app.MapDelete("/deletePurchaseById/{id}", (int id) => database.DeletePurchaseById(id));

// Print all purchases - http://localhost:5198/printallpurchases
//app.MapGet("/printallpurchases", () => database.PrintAllPurchases());

#endregion

app.Run();
