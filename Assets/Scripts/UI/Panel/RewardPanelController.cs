using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;
using MagicSchool.Items;

namespace MagicSchool.UI
{
    // An offer never repeats an item. _showOnStart rolls as well as shows, so a card opened from the Inspector is not
    // blank.
    internal class RewardPanelController : PanelController, IRewardPanel
    {
        private const string EmptySlotClass = "item-slot--empty";

        [Tooltip("Every item a stage win can offer. Three are drawn from it, without repeats.")]
        [SerializeField] private List<ItemDataSO> _itemPool = new List<ItemDataSO>();

        [Tooltip("Draw the card as soon as the scene starts. Off once a stage win is what opens it.")]
        [SerializeField] private bool _showOnStart = true;

        private List<VisualElement> _itemSlots;

        private readonly Dictionary<VisualElement, ItemDataSO> _items = new Dictionary<VisualElement, ItemDataSO>();

        private bool _isChoosing;

        public bool IsShown => Panel != null && !Panel.ClassListContains(HiddenClass);

        public bool IsChoosing => _isChoosing;

        public void ShowReward()
        {
            List<ItemDataSO> items = RollOffer(_itemSlots != null ? _itemSlots.Count : 0);

            ShowOffer(items);

            if (!HasAnyOffer()) return;

            _isChoosing = true;
            SetShown(true);
        }

        protected override void OnMounted(VisualElement panel)
        {
            _itemSlots = panel.Query<VisualElement>("ItemSlot").ToList();

            foreach (VisualElement slot in _itemSlots)
                slot.RegisterCallback<ClickEvent>(_ => Pick(slot));

            Button close = panel.Q<Button>("CloseButton");
            if (close != null) close.clicked += () => SetShown(false);

            SetShown(false);

            if (_showOnStart) ShowReward();
        }

        private void ShowOffer(IReadOnlyList<ItemDataSO> offer)
        {
            if (_itemSlots == null) return;

            for (int slot = 0; slot < _itemSlots.Count; slot++)
            {
                ItemDataSO data = offer != null && slot < offer.Count ? offer[slot] : null;

                AssignItemToSlot(_itemSlots[slot], data);
            }
        }

        private List<ItemDataSO> RollOffer(int count)
        {
            List<ItemDataSO> candidates = new List<ItemDataSO>();

            foreach (ItemDataSO item in _itemPool)
                if (item != null && item.ItemId != ItemIdEnum.None) candidates.Add(item);

            List<ItemDataSO> offer = new List<ItemDataSO>();
            for (int drawn = 0; drawn < count && candidates.Count > 0; drawn++)
            {
                int pick = Random.Range(0, candidates.Count);

                offer.Add(candidates[pick]);

                candidates.RemoveAt(pick);
            }

            return offer;
        }

        private bool HasAnyOffer()
        {
            foreach (ItemDataSO data in _items.Values)
                if (data != null) return true;

            return false;
        }

        private void AssignItemToSlot(VisualElement slot, ItemDataSO data)
        {
            _items[slot] = data;

            slot.EnableInClassList(EmptySlotClass, data == null);

            Label nameLabel = slot.Q<Label>("ItemName");
            if (nameLabel != null) nameLabel.text = data != null ? data.Name : string.Empty;

            Label descriptionLabel = slot.Q<Label>("ItemDescription");
            if (descriptionLabel != null) descriptionLabel.text = data != null ? data.Description : string.Empty;
        }

        private void Pick(VisualElement slot)
        {
            if (!IsShown) return;

            if (!_items.TryGetValue(slot, out ItemDataSO data) || data == null) return;

            if (ItemDrop.Spawn(data) == null) return;

            _isChoosing = false;
            SetShown(false);
        }
    }
}
