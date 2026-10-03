using System;
using MagicSchool.Contracts;
using MagicSchool.Items;

namespace MagicSchool.Economy
{
    // Item is a inspectable. But item's UI version can't be inspected. 
    // ItemPreview.cs help the UI version to be able to be inspected.
    internal class ItemPreview : IInspectableItem
    {
        private readonly ItemDataSO _data;
        private readonly Func<bool> _isStillOffered;

        public ItemPreview(ItemDataSO data, Func<bool> isStillOffered)
        {
            _data = data;
            _isStillOffered = isStillOffered;
        }

        public string DisplayName => _data.Name;
        public string Description => _data.Description;
        public bool IsAlive => _isStillOffered == null || _isStillOffered();
    }
}
