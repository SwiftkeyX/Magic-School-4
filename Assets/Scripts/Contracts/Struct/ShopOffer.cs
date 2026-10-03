namespace MagicSchool.Contracts
{
    // What is in a slot of the shop 
    // e.g.     hero, item, (add here)...
    public readonly struct ShopOffer
    {
        public readonly string Name;
        public readonly int Price;
        public readonly ShopOfferKindEnum Kind;

        public bool IsEmpty => Name == null;

        public ShopOffer(string name, int price, ShopOfferKindEnum kind)
        {
            Name = name;
            Price = price;
            Kind = kind;
        }
    }
}
