using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VerkkokauppaFrontti.Pages
{
    public class Purchase     // Clässi Purchaselle, että saat käytettyä jsonin osia erikseen
    {
        public int id {get; set;} // tilausid
        public int asiakas_id {get; set;} // int asiakas_id 
        public string? tilauspaiva {get; set;} // TÄMÄ PITÄÄ MUOKATA KOSKA EI OO STRING // TimeSpan timeInBusiness = DateTime.Now - new DateTime(2018, 8, 14);
        public string? toimitusosoite {get; set;} // toimitusosoite
        public decimal tilauksen_hinta {get; set;} // tilauksen hinta TÄMÄ PITÄÄ LASKEA TILAUSRIVEISTÄ
        public string? tilauksen_tila {get; set;} // Tilauksen tila automaattisesti VASTAANOTETTU TMS 
        public string? lisatiedot {get; set;} // Lisätietoja 
        public int tilausrivit_id {get; set;} // Tilausrivit ID
    }

    public class PurchaseModel : PageModel
    {
         //esittele 
         [BindProperty]
        public Purchase NewPurchase { get; set; } = default!;
        public void OnGet()
        {
        }

        // Tänne metodit mitä tarvitaan, eli GET asiakkaan tiedot ja tilaustiedot ja loppusumma, tilauspäivän laittaminen
    }
}
