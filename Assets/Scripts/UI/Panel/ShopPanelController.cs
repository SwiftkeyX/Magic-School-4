using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.UI
{
    internal class ShopPanelController : PanelController, ISellZone
    {
        // the shop tints itself while a held hero hovers it (see Shop.uss)
        private const string SellHintClass = "shop-panel--selling";

        // ================= SerializeField ======================
        [SerializeField] private VisualTreeAsset _ghostAsset;
        [SerializeField] private List<HeroDataSO> _heroDataSOs;
        [SerializeField] private Bench _bench;

        // ================= VisualElement ======================
        private VisualElement _shopPanel;
        private VisualElement _ghost;
        private Dictionary<VisualElement, HeroDataSO> _heroSlotsDict = new Dictionary<VisualElement, HeroDataSO>();
        private List<VisualElement> _heroSlots;

        // ================== etc =======================
        private bool _isDragging = false;
        private Vector2 _ghostSize;

        // ================= setter & getter ===================
        // ...

        #region Initialize Panel
        protected override void OnMounted(VisualElement panel)
        {
            // get the reference for later use
            _shopPanel = panel.Q<VisualElement>("ShopPanel");

            InitializeGhost();

            // Make hero slot draggable
            MakeShopUIDraggable();

            // Wire up the "Refresh" button to re-roll all hero slots
            MakeRefreshButtonWork();
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

        // put a hero's data into a slot: what dragging/buying reads, and what's shown as its label.
        // A null data means "empty" (e.g. just bought). The slot stays in the layout at full size
        // either way (display is never toggled) so buying one doesn't reflow its siblings - it just
        // goes dim and unlabeled, which is derived here in one place rather than a separate flag.
        private void AssignHeroToSlot(VisualElement slot, HeroDataSO data)
        {
            _heroSlotsDict[slot] = data;

            slot.style.opacity = data == null ? 0.35f : 1f;

            Label nameLabel = slot.Q<Label>();
            if (nameLabel != null) nameLabel.text = data != null ? data.Name : string.Empty;
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
            // pointer.position and worldBound are both in the same panel coordinate space, no screen/world conversion needed
            bool releasedInsideShop = _shopPanel.worldBound.Contains(releasePosition);
            if (releasedInsideShop)
            {
                Debug.Log("Buy cancelled - dropped back inside the shop.");
                return;
            }

            // BLOCKED on: the gold/economy system. => Put spend gold logic here once it exists.
            bool bought = BuyHero(_heroSlotsDict[slot]);
            if (!bought) return;

            Debug.Log($"Bought hero from slot '{_heroSlotsDict[slot]}'.");

            // slot's hero is gone - clearing its data dims it and blocks re-buying (see AssignHeroToSlot)
            AssignHeroToSlot(slot, null);
        }
        #endregion

        #region etc
        private bool BuyHero(HeroDataSO data)
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
        private void MakeRefreshButtonWork()
        {
            // "Refresh" is one of the two refresh-slot boxes on the left (the other is "Lock", not wired up yet)
            VisualElement randomSlot = _shopPanel.Q<VisualElement>("RandomSlot");
            if (randomSlot == null) return;

            foreach (var child in randomSlot.Children())
            {
                Label label = child.Q<Label>();
                if (label == null || label.text != "Refresh") continue;

                child.RegisterCallback<PointerUpEvent>(pointer => RerollShop());
                break;
            }
        }

        // Re-roll every hero slot with a random hero from the full roster
        private void RerollShop()
        {
            foreach (var slot in _heroSlots)
            {
                HeroDataSO randomHero = _heroDataSOs[Random.Range(0, _heroDataSOs.Count)];
                AssignHeroToSlot(slot, randomHero);
            }
        }
        #endregion

    }
}
