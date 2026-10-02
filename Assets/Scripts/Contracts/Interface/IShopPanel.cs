namespace MagicSchool.Contracts
{
    // IShopPanel answers: whose gold is spended when interact with the shop?
    public interface IShopPanel : IPanel
    {
        void BindWallet(IWallet wallet);
    }
}
