using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.UI
{
    internal class ShopPanelController : PanelController, ISellZone, IShopPanel
    {
        // the shop tints itself while a held hero hovers it (see Shop.uss)
        private const string SellHintClass = "shop-panel--selling";

        // a slot the player can't pay for shows its price in red (see Shop.uss)
        private const string UnaffordableClass = "hero-slot--unaffordable";

        // ================= SerializeField ======================
        [SerializeField] private VisualTreeAsset _ghostAsset;
        [SerializeField] private List<HeroDataSO> _heroDataSOs;
        [SerializeField] private Bench _bench;
        [SerializeField] private int _refreshCost = 1;      

        // ================= VisualElement ======================
        private VisualElement _shopPanel;
        private VisualElement _ghost;
        private Dictionary<VisualElement, HeroDataSO> _heroSlotsDict = new Dictionary<VisualElement, HeroDataSO>();
        private List<VisualElement> _heroSlots;
        private Label _goldValue;
        private Label _refreshLabel;

        // ================== etc =======================
        private bool _isDragging = false;
        private Vector2 _ghostSize;
        private IWallet _wallet;        

        // ================= setter & getter ===================
        // ...

        #region Initialize Panel
        protected override void OnMounted(VisualElement panel)
        {
            // get the reference for later use
            _shopPanel = panel.Q<VisualElement>("ShopPanel");
            _goldValue = panel.Q<Label>("GoldValue");
            _refreshLabel = panel.Q<Label>("RefreshLabel");

            InitializeGhost();

            // Make hero slot draggable
            MakeShopUIDraggable();

            // Wire up the "Refresh" button to re-roll all hero slots
            MakeRefreshButtonWork();

            // init gold displayed
            ShowGold();
        }

        #endregion

        // FLAGGIGN: UI Draggable is generic too, it also deserved its own file in the future. 
        #region UI Draggable
        /// <summary>
        /// Main function for dragging
        /// A lot of comment since I never use those event before
        /// </summary>
        private void MakeShopUIDraggable()
        {
            // get all slots exist from the shop panel
            _heroSlots = _shopPanel.Query<VisualElement>("HeroSlot").ToList();

            // assign hero data to each slots, in order, for the initial roll
            for (int i = 0; i < _heroSlots.Count; i++)
            {
                if (i >= _heroDataSOs.Count) { Debug.LogError("HeroDataSO is not enough for all slot in shop panel"); return; }
                AssignHeroToSlot(_heroSlots[i], _heroDataSOs[i]);
            }

            // register event to every slot.
            foreach (var slot in _heroSlots)
            {
                // when click on heroslot, spawn ghost, move ghost to click point
                HeroSlotOnClick(slot);

                // when hold on heroslot, move ghost to hold point, create dragging logic visually
                HeroSlotOnMove(slot);

                // when your mouse release from holding, resolve buy/cancel based on the release point
                HeroSlotOnRelease(slot);
            }
        }

        // put a hero's data into a slot
        private void AssignHeroToSlot(VisualElement slot, HeroDataSO data)
        {
            _heroSlotsDict[slot] = data;

            slot.style.opacity = data == null ? 0.35f : 1f;

            Label nameLabel = slot.Q<Label>();
            if (nameLabel != null) nameLabel.text = data != null ? data.Name : string.Empty;

            Label priceLabel = slot.Q<Label>("Price");
            if (priceLabel != null) priceLabel.text = data != null ? $"{data.Price} g" : string.Empty;

            ShowAffordability(slot);
        }

        // ============================== Pointer Event ====================================
        private void HeroSlotOnClick(VisualElement heroSlot)
        {
            // PointerDownEvent = when your mouse hold inside heroslot bound
            // pointer = the point you start clicking
            heroSlot.RegisterCallback<PointerDownEvent>(pointer =>
            {
                // empty slot (already bought) has nothing to drag
                if (_heroSlotsDict[heroSlot] == null) return;

                _isDragging = true;

                // CapturePointer = All event from "heroslot" will continue working even though the "pointer" move out of bound
                heroSlot.CapturePointer(pointer.pointerId);

                // add ghost to main UI, this make ghost visible
                // ghost = the element that visually got drag together with your mouse e.g. hero sprite.
                // technically, ghost is element that copy your mouse pointer's position.
                ShowGhostAs(heroSlot);
                MainPanel.Add(_ghost);

                // move ghost to the point you just click
                MoveGhostTo(pointer.position);
            });
        }

        private void HeroSlotOnMove(VisualElement heroSlot)
        {
            // PointerMoveEvent = when you move your mouse inside heroslot bound
            // if your mouse exit heroslot bound, the event won't fired, BUT we use CapturePointer() so we can actually move out of bound
            // pointer = the point you holding your mouse, so this pointer can move
            heroSlot.RegisterCallback<PointerMoveEvent>(pointer =>
            {
                if (!_isDragging) return;

                // move ghost using pointer
                MoveGhostTo(pointer.position);
            });
        }

        private void HeroSlotOnRelease(VisualElement heroSlot)
        {
            // PointerUpEvent = when your mouse release from holding
            // pointer = the point you release your mouse
            heroSlot.RegisterCallback<PointerUpEvent>(pointer =>
            {
                _isDragging = false;

                // undo the CapturePointer
                heroSlot.ReleasePointer(pointer.pointerId);

                // release ghost from main UI, this make ghost go invisible
                _ghost?.RemoveFromHierarchy();

                // released back inside the shop bound = cancel, released outside = buy
                ResolveDrop(heroSlot, pointer.position);
            });
        }

        // ============================== Ghost ====================================
        // ghost = the same sprite that hero slot use in the shop.
        // ghost spawn when player drag one of the hero slot.
        private void InitializeGhost()
        {
            _ghost = PanelMounter.CloneTemplateRoot(_ghostAsset);
        }

        // show ghost as the same to the dragging hero slot.
        private void ShowGhostAs(VisualElement heroSlot)
        {
            Label slotLabel = heroSlot.Q<Label>();
            Label ghostLabel = _ghost.Q<Label>();
            if (slotLabel != null && ghostLabel != null) ghostLabel.text = slotLabel.text;

            _ghostSize = new Vector2(heroSlot.resolvedStyle.width, heroSlot.resolvedStyle.height);
            _ghost.style.width = _ghostSize.x;
            _ghost.style.height = _ghostSize.y;
        }

        // ghost copying the mouse position using "screen panel method"
        private void MoveGhostTo(Vector2 panelPosition)
        {
            _ghost.style.left = panelPosition.x - _ghostSize.x / 2f;
            _ghost.style.top = panelPosition.y - _ghostSize.y / 2f;
        }

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

            HeroDataSO data = _heroSlotsDict[slot];

            // can't buy hero because not enough money. 
            if (!CanAfford(data.Price))
            {
                Debug.Log($"Not enough gold for '{data.Name}' ({data.Price} g).");
                return;
            }

            // if spawn hero don't work, no buy, return
            bool bought = SpawnHero(data);
            if (!bought) return;

            // pay money
            Pay(data.Price);
            Debug.Log($"Bought hero from slot '{data}'.");

            // slot's hero is gone - clearing its data dims it and blocks re-buying (see AssignHeroToSlot)
            AssignHeroToSlot(slot, null);
        }
        #endregion

        #region etc
        private bool SpawnHero(HeroDataSO data)
        {
            return _bench.SpawnHeroOnBench(data);
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

            if (_refreshLabel != null) _refreshLabel.text = $"Refresh ({_refreshCost} g)";

            refresh.RegisterCallback<PointerUpEvent>(pointer => RerollShop());
        }

        // Re-roll every slot with a random hero, or item. Costs gold.
        private void RerollShop()
        {
            if (!CanAfford(_refreshCost))
            {
                Debug.Log($"Not enough gold to refresh the shop ({_refreshCost} g).");
                return;
            }

            Pay(_refreshCost);

            foreach (var slot in _heroSlots)
            {
                HeroDataSO randomHero = _heroDataSOs[Random.Range(0, _heroDataSOs.Count)];
                AssignHeroToSlot(slot, randomHero);
            }
        }
        #endregion

        // =========================== Gold ===============================
        // FIXLATER: the ShopPanelController is too big. Let divide into Shop and ShopPanel.
        // ShopPanel only displayed, Shop have the actual logic.
        #region Gold
        // === IShopPanel ===
        public void BindWallet(IWallet wallet)
        {
            if (_wallet != null) _wallet.OnGoldChanged -= OnGoldChanged;

            _wallet = wallet;

            if (_wallet != null) _wallet.OnGoldChanged += OnGoldChanged;

            ShowGold();
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnGoldChanged -= OnGoldChanged;
        }

        private void OnGoldChanged(int gold) => ShowGold();

        private bool CanAfford(int cost) => _wallet == null || _wallet.CanAfford(cost);
        private void Pay(int cost) => _wallet?.TrySpend(cost);

        // show the player's gold, and which slots it can still pay for
        private void ShowGold()
        {
            if (_goldValue != null) _goldValue.text = _wallet != null ? $"{_wallet.Gold} g" : string.Empty;

            if (_heroSlots == null) return;

            foreach (var slot in _heroSlots) ShowAffordability(slot);
        }

        private void ShowAffordability(VisualElement slot)
        {
            _heroSlotsDict.TryGetValue(slot, out HeroDataSO data);

            slot.EnableInClassList(UnaffordableClass, data != null && !CanAfford(data.Price));
        }
        #endregion

    }
}
