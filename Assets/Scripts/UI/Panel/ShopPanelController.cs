using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // An item slot is dragged as a UI ghost: released outside the panel buys it, back inside cancels. A hero slot is
    // not: the shop hands the hero itself to the player, held in the world like a hero off the board (See
    // IShop.TryPickUp), so it has no ghost. The shop may be bound before the panel is mounted, so mounting draws
    // whatever is bound.
    internal class ShopPanelController : PanelController, ISellZone, IShopPanel
    {
        private const string SellHintClass = "shop-panel--selling";

        private const string UnaffordableClass = "hero-slot--unaffordable";

        private const string ItemClass = "hero-slot--item";

        private const string LockedClass = "refresh-slot--locked";

        [SerializeField] private VisualTreeAsset _ghostAsset;

        private VisualElement _shopPanel;
        private List<VisualElement> _heroSlots;
        private Label _goldValue;
        private Label _refreshLabel;
        private VisualElement _lock;
        private Label _lockLabel;

        private UIDrag _drag;
        private IShop _shop;
        private IInspectorPanel _inspector;    

        #region Initialize Panel
        protected override void OnMounted(VisualElement panel)
        {
            _shopPanel = panel.Q<VisualElement>("ShopPanel");
            _goldValue = panel.Q<Label>("GoldValue");
            _refreshLabel = panel.Q<Label>("RefreshLabel");
            _lock = panel.Q<VisualElement>("Lock");
            _lockLabel = panel.Q<Label>("LockLabel");

            MakeShopUIDraggable();

            MakeRefreshButtonWork();

            MakeLockButtonWork();

            MakeSlotsInspectable();

            ShowShop();
        }

        #endregion

        #region UI Draggable
        private void MakeShopUIDraggable()
        {
            _drag = new UIDrag(MainPanel, _ghostAsset);

            _heroSlots = _shopPanel.Query<VisualElement>("HeroSlot").ToList();

            foreach (var slot in _heroSlots)
            {
                _drag.MakeDraggable(
                    slot,
                    canPickUp: () => CanDragAsGhost(slot),

                    onDrop: position => ResolveDrop(slot, position));
            }
        }

        private bool CanDragAsGhost(VisualElement slot)
        {
            ShopOffer offer = OfferOf(slot);
            if (offer.IsEmpty) return false;

            if (offer.Kind == ShopOfferKindEnum.Hero)
            {
                _shop.TryPickUp(_heroSlots.IndexOf(slot));
                return false;
            }

            return true;
        }

        #region Inspect
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

        private void ResolveDrop(VisualElement slot, Vector2 releasePosition)
        {
            bool releasedInsideShop = _shopPanel.worldBound.Contains(releasePosition);
            if (releasedInsideShop)
            {
                Debug.Log("Buy cancelled - dropped back inside the shop.");
                return;
            }

            _shop?.TryBuy(_heroSlots.IndexOf(slot));
        }
        #endregion

        #region Sell
        public bool IsInSellBoundary(Vector2 screenPosition)
        {
            if (_shopPanel == null || _shopPanel.panel == null) return false;

            Vector2 flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(_shopPanel.panel, flipped);

            return _shopPanel.worldBound.Contains(panelPosition);
        }

        public void ShowSellHint(bool isOverZone)
        {
            if (_shopPanel == null) return;

            _shopPanel.EnableInClassList(SellHintClass, isOverZone);
        }
        #endregion

        #region Refresh
        private void MakeRefreshButtonWork()
        {
            VisualElement refresh = _shopPanel.Q<VisualElement>("Refresh");
            if (refresh == null) return;

            refresh.RegisterCallback<PointerUpEvent>(pointer => _shop?.TryRefresh());
        }
        #endregion

        #region Lock
        private void MakeLockButtonWork()
        {
            if (_lock == null) return;

            _lock.RegisterCallback<PointerUpEvent>(pointer => _shop?.ToggleLock());
        }
        #endregion

        #region Show
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

        private ShopOffer OfferOf(VisualElement slot)
        {
            int index = _heroSlots.IndexOf(slot);

            return _shop != null && index >= 0 && index < _shop.SlotCount ? _shop.OfferAt(index) : default;
        }

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
