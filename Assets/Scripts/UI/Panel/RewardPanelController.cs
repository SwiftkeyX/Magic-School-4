using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;
using MagicSchool.Items;

namespace MagicSchool.UI
{
    internal class RewardPanelController : PanelController, IRewardPanel
    {
        // .uss for empty reward slot 
        private const string EmptySlotClass = "item-slot--empty";

        // ================= SerializeField ======================
        [Tooltip("Every item a stage win can offer. Three are drawn from it, without repeats.")]
        [SerializeField] private List<ItemDataSO> _itemPool = new List<ItemDataSO>();

        [Tooltip("Draw the card as soon as the scene starts. Off once a stage win is what opens it.")]
        [SerializeField] private bool _showOnStart = true;

        [Tooltip("Where a picked item lands in the world, for the player to drag onto a hero.")]
        [SerializeField] private Vector3 _spawnPosition = new Vector3(-5.6f, -2f, 0f);

        [Tooltip("How far along x each further pick lands, so they do not pile up on one spot.")]
        [SerializeField] private float _spawnSpacing = 0.6f;

        // ================= VisualElement ======================
        // the slot of the offered item
        private List<VisualElement> _itemSlots;

        // the items
        private readonly Dictionary<VisualElement, ItemDataSO> _items = new Dictionary<VisualElement, ItemDataSO>();

        // ================= other ======================
        // how many picks have been spawned, so the next spawned one lands beside the previous one
        private int _spawnedCount;

        // is the reward choosed yet.
        private bool _isChoosing;

        // ================= getter ======================
        public bool IsShown => Panel != null && !Panel.ClassListContains(HiddenClass);


        // ================================== interface ==================================
        public bool IsChoosing => _isChoosing;

        public void ShowReward()
        {
            // get random items from the pool, 
            // count = the number of slot in the panel
            List<ItemDataSO> items = RollOffer(_itemSlots != null ? _itemSlots.Count : 0);

            // put item in the slot
            ShowOffer(items);

            // if nothing is offered, return
            if (!HasAnyOffer()) return;

            // finally, show the reward panel
            _isChoosing = true;
            SetShown(true);
        }

        // ================================== override ==================================
        protected override void OnMounted(VisualElement panel)
        {
            _itemSlots = panel.Query<VisualElement>("ItemSlot").ToList();

            // register click event to each slot
            foreach (VisualElement slot in _itemSlots)
                slot.RegisterCallback<ClickEvent>(_ => Pick(slot));

            // register close event to the close button
            Button close = panel.Q<Button>("CloseButton");
            if (close != null) close.clicked += () => SetShown(false);

            // At start of the game, panel should be hidden
            SetShown(false);

            // FLAGGING: show on start make the result panel show at the start, this is for fast debugging.
            // rolls as well as shows, so an inspector-opened card is not blank
            if (_showOnStart) ShowReward();
        }

        // ================================== private ==================================
        // Put a set of items on the card. 
        private void ShowOffer(IReadOnlyList<ItemDataSO> offer)
        {
            if (_itemSlots == null) return;

            for (int slot = 0; slot < _itemSlots.Count; slot++)
            {
                ItemDataSO data = offer != null && slot < offer.Count ? offer[slot] : null;

                AssignItemToSlot(_itemSlots[slot], data);
            }
        }

        // Random an offer out of the pool, without repeating the same item.
        private List<ItemDataSO> RollOffer(int count)
        {
            List<ItemDataSO> candidates = new List<ItemDataSO>();

            // guard
            foreach (ItemDataSO item in _itemPool)
                if (item != null && item.ItemId != ItemIdEnum.None) candidates.Add(item);

            // random a item, and put it in the offer list
            List<ItemDataSO> offer = new List<ItemDataSO>();
            for (int drawn = 0; drawn < count && candidates.Count > 0; drawn++)
            {
                // random the item
                int pick = Random.Range(0, candidates.Count);

                // put in offer
                offer.Add(candidates[pick]);

                // remove from the candidate, to prevent the repeat item from being random again.
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

        // Put one item's data into one slot. 
        private void AssignItemToSlot(VisualElement slot, ItemDataSO data)
        {
            _items[slot] = data;

            // if the data is null, use empty slot style
            slot.EnableInClassList(EmptySlotClass, data == null);

            // put the item name in the ItemName container
            Label nameLabel = slot.Q<Label>("ItemName");
            if (nameLabel != null) nameLabel.text = data != null ? data.Name : string.Empty;

            // put the item description in the ItemDescription container
            Label descriptionLabel = slot.Q<Label>("ItemDescription");
            if (descriptionLabel != null) descriptionLabel.text = data != null ? data.Description : string.Empty;
        }

        // Clicking an offer, to spawns that item into the world and closes the card. 
        private void Pick(VisualElement slot)
        {
            if (!IsShown) return;

            // an empty slot has nothing to give
            if (!_items.TryGetValue(slot, out ItemDataSO data) || data == null) return;

            Vector3 where = _spawnPosition + new Vector3(_spawnedCount * _spawnSpacing, 0f, 0f);
            if (Item.Spawn(data, where) == null) return;

            _spawnedCount++;
            _isChoosing = false;
            SetShown(false);
        }
    }
}
