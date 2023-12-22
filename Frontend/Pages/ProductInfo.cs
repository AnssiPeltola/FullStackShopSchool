namespace VerkkokauppaFrontti.Pages
{
    // Databasesta saadun tiedon käsittelyä varten
    public class ProductInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ProductCategory { get; set; }
            public string ProductCategory2 { get; set; }
            public decimal Price { get; set; }
            public int Amount { get; set; }
            public string Img { get; set; }
            public string Description { get; set; }
        }
}
