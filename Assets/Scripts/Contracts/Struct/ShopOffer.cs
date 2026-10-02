namespace MagicSchool.Contracts
{
    // What is in a slot of the shop.
    // e.g.     a hero today
    //          an item will be an offer too.
    public readonly struct ShopOffer
    {
        public readonly string Name;
        public readonly int Price;

        public bool IsEmpty => Name == null;

        public ShopOffer(string name, int price)
        {
            Name = name;
            Price = price;
        }
    }
}
