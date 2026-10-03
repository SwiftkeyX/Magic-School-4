using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    internal class ShopPanelController : PanelController, ISellZone, IShopPanel
    {
        // the shop tints itself while a held hero hovers it (see Shop.uss)
        private const string SellHintClass = "shop-panel--selling";

        // a slot the player can't pay for shows its price in red (see Shop.uss)
        private const string UnaffordableClass = "hero-slot--unaffordable";

        // a slot selling an item rather than a hero looks different (see Shop.uss)
        private const string ItemClass = "hero-slot--item";

        // the Lock box lights up while the shop is locked (see Shop.uss)
        private const string LockedClass = "refresh-slot--locked";

        // ================= SerializeField ======================
        [SerializeField] private VisualTreeAsset _ghostAsset;

        // ================= VisualElement ======================
        private VisualElement _shopPanel;
        private List<VisualElement> _heroSlots;     // slot i shows the shop's offer i
        private Label _goldValue;
        private Label _refreshLabel;
        private VisualElement _lock;
        private Label _lockLabel;

        // ================== etc =======================
        private UIDrag _drag;
        private IShop _shop;
        private IInspectorPanel _inspector;    

        // ================= setter & getter ===================
        // ...

        #region Initialize Panel
        protected override void OnMounted(VisualElement panel)
        {
            // get the reference for later use
            _shopPanel = panel.Q<VisualElement>("ShopPanel");
            _goldValue = panel.Q<Label>("GoldValue");
            _refreshLabel = panel.Q<Label>("RefreshLabel");
            _lock = panel.Q<VisualElement>("Lock");
            _lockLabel = panel.Q<Label>("LockLabel");

            // Make hero slot draggable
            MakeShopUIDraggable();

            // Wire up the "Refresh" button to re-roll all hero slots
            MakeRefreshButtonWork();

            // Wire up the "Lock" button to prevent the shop from rerolling itself after each round
            MakeLockButtonWork();

            // Right-click a slot to see what it sells in the inspector
            MakeSlotsInspectable();

            // the shop may have been bound before the panel was mounted
            ShowShop();
        }

        #endregion

        #region UI Draggable
        // Every slot can be dragged out of the shop to buy it. The dragging itself is UIDrag's.
        private void MakeShopUIDraggable()
        {
            _drag = new UIDrag(MainPanel, _ghostAsset);

            // get all slots exist from the shop panel
            _heroSlots = _shopPanel.Query<VisualElement>("HeroSlot").ToList();

            // init draggable
            foreach (var slot in _heroSlots)
            {
                _drag.MakeDraggable(
                    slot,
                    // when pick up the slot:
                    //      all slot can be picked up.
                    //      only slot that can't be pick up is a empty slot
                    canPickUp: () => !OfferOf(slot).IsEmpty,

                    // when drop:
                    //      released back inside the shop bound = cancel
                    //      released outside bound = buy
                    onDrop: position => ResolveDrop(slot, position));
            }
        }

        #region Inspect
        // right-click a slot = show what it sells in the inspector
        private void MakeSlotsInspectable()
        {
            foreach (VisualElement slot in _heroSlots)
            {
                slot.RegisterCallback<PointerDownEvent>(pointer =>
                {
                    if (pointer.button != 1 || _shop == null) return;

                    _inspector ??= FindInspector();
                    _inspector?.Inspect(_shop.InspectableAt(_heroSlots.IndexOf(slot)));
                });
            }
        }

        private static IInspectorPanel FindInspector()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (behaviour is IInspectorPanel inspector) return inspector;

            return null;
        }
        #endregion

        // =========================== Buy / cancel on release ===============================
        private void ResolveDrop(VisualElement slot, Vector2 releasePosition)
        {
            // if the drop was done outside the shop's bound, buy that hero
            bool releasedInsideShop = _shopPanel.worldBound.Contains(releasePosition);
            if (releasedInsideShop)
            {
                Debug.Log("Buy cancelled - dropped back inside the shop.");
                return;
            }

            _shop?.TryBuy(_heroSlots.IndexOf(slot));
        }
        #endregion

        // =========================== Sell ===============================
        #region Sell
        // if releasing point match shop boundary, return true
        public bool IsInSellBoundary(Vector2 screenPosition)
        {
            if (_shopPanel == null || _shopPanel.panel == null) return false;

            Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(_shopPanel.panel, flipped);

            return _shopPanel.worldBound.Contains(panelPosition);
        }

        // change the shop UI's color to indicate that it was focus
        public void ShowSellHint(bool isOverZone)
        {
            if (_shopPanel == null) return;

            _shopPanel.EnableInClassList(SellHintClass, isOverZone);
        }
        #endregion

        #region Refresh
        // "Refresh" is one of the boxes on the left. It make the shop reroll the products. 
        private void MakeRefreshButtonWork()
        {
            VisualElement refresh = _shopPanel.Q<VisualElement>("Refresh");
            if (refresh == null) return;

            refresh.RegisterCallback<PointerUpEvent>(pointer => _shop?.TryRefresh());
        }
        #endregion

        #region Lock
        // If a shop is locked, prevent the re-rolled at the start of the next stage.
        private void MakeLockButtonWork()
        {
            if (_lock == null) return;

            _lock.RegisterCallback<PointerUpEvent>(pointer => _shop?.ToggleLock());
        }
        #endregion

        // =========================== Show the shop ===============================
        #region Show
        // === IShopPanel ===
        // ASKING: why is OnChanged also do -= ShowShop
        public void BindShop(IShop shop)
        {
            if (_shop != null) _shop.OnChanged -= ShowShop;

            _shop = shop;

            if (_shop != null) _shop.OnChanged += ShowShop;

            ShowShop();
        }

        private void OnDestroy()
        {
            if (_shop != null) _shop.OnChanged -= ShowShop;
        }

        // what the shop sells in this slot.
        private ShopOffer OfferOf(VisualElement slot)
        {
            int index = _heroSlots.IndexOf(slot);

            return _shop != null && index >= 0 && index < _shop.SlotCount ? _shop.OfferAt(index) : default;
        }

        // draw everything the shop displayed: 
        // e.g. the player's gold, the refresh cost, and every slot
        private void ShowShop()
        {
            if (_goldValue != null) _goldValue.text = _shop != null ? $"{_shop.Gold} g" : string.Empty;
            
            if (_refreshLabel != null) _refreshLabel.text = _shop != null ? $"Refresh ({_shop.RefreshCost} g)" : "Refresh";

            bool locked = _shop != null && _shop.IsLocked;
            if (_lockLabel != null) _lockLabel.text = locked ? "Locked" : "Lock";
            _lock?.EnableInClassList(LockedClass, locked);

            if (_heroSlots != null)
            {
                for (int i = 0; i < _heroSlots.Count; i++) ShowSlot(_heroSlots[i], i);
            }
        }

        private void ShowSlot(VisualElement slot, int index)
        {
            ShopOffer offer = OfferOf(slot);

            slot.style.opacity = offer.IsEmpty ? 0.35f : 1f;

            Label nameLabel = slot.Q<Label>();
            if (nameLabel != null) nameLabel.text = offer.IsEmpty ? string.Empty : offer.Name;

            Label priceLabel = slot.Q<Label>("Price");
            if (priceLabel != null) priceLabel.text = offer.IsEmpty ? string.Empty : $"{offer.Price} g";

            slot.EnableInClassList(UnaffordableClass, !offer.IsEmpty && !_shop.CanAfford(index));
            slot.EnableInClassList(ItemClass, !offer.IsEmpty && offer.Kind == ShopOfferKindEnum.Item);
        }
        #endregion

    }
}
