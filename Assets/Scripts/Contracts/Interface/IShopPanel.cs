namespace MagicSchool.Contracts
{
    // IShopPanel answers: Bind the Shop.cs to the shop panel.
    public interface IShopPanel : IPanel
    {
        void BindShop(IShop shop);
    }
}
