namespace VerkkokauppaFrontti.Pages
{
    // CartItem is for handling the separate purchase in Cart.
    public class CartItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Amount { get; set; }
    

        public CartItem(int id, string name, double price, int amount)
        {
            this.Id = id;
            this.Name = name;

            // Rounds the price to two decimals, this is shown in the shoppingCart -page
            double roundedPrice = 0.00;
            // Converts the price first to string to set the decimals and then back to double
            if(double.TryParse(price.ToString("0.00"), out roundedPrice))
            {
                this.Price = roundedPrice;
            }

            this.Amount = amount;
        }

        // This is for the if(list.contains(item)) in index-page's OnPostAddToCart -method
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // Casting the obj to the same type than CartItem...
            CartItem otherItem = (CartItem)obj;
            // ...and comparing if the values are same.
            return Id == otherItem.Id &&
                Name == otherItem.Name;
        }
    }
}