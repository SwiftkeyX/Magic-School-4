using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Core;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Items;
using MagicSchool.Input;

namespace MagicSchool.Player
{
    // Picking things up with the pointer, dragging around, and putting them down again.
    // e.g.     draggable are items, and hero
    // A hero or an item can be held two ways:
    //      drag  => press on it, move, release where it goes
    //      click => press and release on it without moving: it stays in hand, follows the pointer,
    //               and the next press puts it down where the pointer is. Esc puts it back where it came from.
    // Worth mentioning that there's also UIDrag.cs in this project.
    // That one was specifically for dragging the UI which is different from dragging the gameobject like this one.
    internal class Dragging
    {
        private readonly Camera _cam;
        private readonly BattleBoard _board;
        private readonly ISellZone _sellZone;
        private readonly TeamEnum _team;
        private readonly PlayerTeamSize _teamSize;

        private const float ClickSlop = 10f;    // pixels the pointer may move between press and release and still be a click

        private IDraggable _held;
        private Collider2D _heldHitbox;
        private bool _sellHintShown;
        private bool _isClickHold;              // picked up by a click: put down by the next press, not by a release
        private Vector2 _pressedAt;             // where the pointer was pressed to pick it up, in screen pixels
        private int _pressUsedFrame = -1;       // the frame a press put something down

        // where a held item came from, so Esc can put it back: worn by a hero, lying on a hex, or loose where it lay
        private Hero _itemWornBy;
        private Hex _itemLayOn;
        private Vector3 _itemLooseAt;

        // ============================= getter =============================
        public bool IsHolding => _held as Object != null;
        public Hero HeldHero => IsHolding ? _held as Hero : null;
        public Item HeldItem => IsHolding ? _held as Item : null;
        private bool IsPreparation => GameManager.Instance.Phase == GamePhaseEnum.Preparation;

        // this frame's press already put a click-held hero or item down - nothing else should act on it (See ZonePainting)
        public bool UsedPressThisFrame => _pressUsedFrame == Time.frameCount;
        private bool _isStageEditor => GameManager.Instance.StageEditor != null;

        public Dragging(Camera cam, BattleBoard board, ISellZone sellZone, TeamEnum team, PlayerTeamSize teamSize)
        {
            _cam = cam;
            _board = board;
            _sellZone = sellZone;
            _team = team;
            _teamSize = teamSize;
        }

        // =================================== life cycle ===================================
        public void Tick()
        {
            // guard - if not in preparation state, not allow to hold the hero,
            // nor an item taken off a hero or a hex (only Preparation could take it off, so put it back)
            if (!IsPreparation && (_held is Hero || IsItemFromBoard)) Cancel();

            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);

            // if not holding anything, polling until something was holded
            if (!IsHolding)
            {
                TryPickUp(worldPos);
                return;
            }

            // the holding object follow the pointer, create a visually object dragging
            Follow(worldPos);

            // if the object is sellable, drag it to shop, will turn on the shop hint
            // shop hint = make shop red to indicated it was focus
            bool isSellable = _held is ISellable;
            RefreshSellHint(isSellable && IsPointerOverSellZone());

            // held by a click: the next press puts it down, Esc puts it back
            if (_isClickHold)
            {
                if (PlayerInputSystem.CancelPressedThisFrame) { Cancel(); return; }
                if (!PlayerInputSystem.DragPressedThisFrame) return;

                _pressUsedFrame = Time.frameCount;
                Drop(worldPos);
                return;
            }

            // if still holding, return
            bool stillHolding = PlayerInputSystem.IsPointerDown && !PlayerInputSystem.DragReleasedThisFrame;
            if (stillHolding) return;

            // released without moving = a click: it stays in hand until the next press
            if (IsClick())
            {
                _isClickHold = true;
                return;
            }

            // if releasing, drop the hero
            Drop(worldPos);
        }

        private bool IsItemFromBoard => _held is Item && (_itemWornBy != null || _itemLayOn != null);

        // the pointer was released about where it was pressed
        private bool IsClick() => Vector2.Distance(PlayerInputSystem.PointerScreenPosition, _pressedAt) <= ClickSlop;

        // =================================== pick up ===================================
        // the pickup could be either hero, or item.
        private void TryPickUp(Vector3 worldPos)
        {
            if (!PlayerInputSystem.DragPressedThisFrame) return;

            // only allow to drag hero in preparation state
            if (IsPreparation)
            {
                IDraggable hero = TryPickHero(worldPos);
                if (hero != null)
                {
                    Grab(hero);
                    return;
                }
            }

            // always allow to drag item
            IDraggable item = TryPickItem(worldPos);
            if (item != null)
            {
                Grab(item);
                return;
            }
        }

        private IDraggable TryPickHero(Vector3 worldPos)
        {
            Hero hero = Picker.At<Hero>(worldPos);
            if (CanDragThisHero(hero))
            {
                return hero;
            }

            return null;
        }

        private IDraggable TryPickItem(Vector3 worldPos)
        {
            Item item = Picker.At<Item>(worldPos);
            if (item == null) { return null; }

            // remember where it came from, for Esc
            _itemWornBy = null;
            _itemLayOn = null;
            _itemLooseAt = item.transform.position;

            // if the item have wearer, take it off
            Hero wearer = item.GetComponentInParent<Hero>();
            if (wearer != null)
            {
                // if not in preparation state, not allow to strip item off hero.
                if (!IsPreparation) return null;

                GameManager.Instance.TakeItemOffHero(wearer, item);
                _itemWornBy = wearer;
            }

            // if the item lies on a hex, take it off the hex
            else if (_board.Items.HexOf(item) is Hex lyingOn)
            {
                // if not in preparation state, not allow to take an item off the board.
                if (!IsPreparation) return null;

                GameManager.Instance.TakeItemOffHex(item);
                _itemLayOn = lyingOn;
            }

            return item;
        }

        // There's some hero that player shouldn't allow to drag
        // e.g.     player allow to drag his own hero
        //          player don't allow to drag enemy
        //          player allow to drag enemy in the stage editor mode
        private bool CanDragThisHero(Hero hero)
        {
            if (hero == null) return false;

            bool isMyHero = hero.Team == _team;

            return isMyHero || _isStageEditor;
        }

        private void Grab(IDraggable target)
        {
            _held = target;
            _isClickHold = false;
            _pressedAt = PlayerInputSystem.PointerScreenPosition;
            _heldHitbox = target.transform.GetComponent<Collider2D>();
            if (_heldHitbox != null) _heldHitbox.enabled = false;
        }

        private void Follow(Vector3 worldPos)
        {
            Transform held = _held.transform;
            held.position = new Vector3(worldPos.x, worldPos.y, held.position.z);
        }

        // =================================== drop ===================================
        private void Drop(Vector3 worldPos)
        {
            if (_held is Hero hero)
            {
                DropHero(hero, worldPos);
                return;
            }

            if (_held is Item item)
            {
                DropItem(item, worldPos);
                return;
            }

            Release();
        }

        // hero could be drop on 2 thing:
        // 1) placement => put that hero on a placement
        // 2) shop => sell that hero
        private void DropHero(Hero hero, Vector3 worldPos)
        {
            IPlacement targetPlacement = Picker.At<IPlacement>(worldPos);

            // released on a placement = put a hero there
            if (targetPlacement != null && ValidatePlacement(targetPlacement, hero))
            {
                PlaceAndSwap(hero, targetPlacement);
                Release();
                return;
            }

            // released over the shop = sell a hero.
            if (targetPlacement == null && IsPointerOverSellZone())
            {
                GameManager.Instance.Sell(hero);
                Release();
                return;
            }

            // released on nothing = put hero back to its original placement
            Cancel();
        }

        // place the hero on the placement
        // if the target placement already have owner, swap the placement.
        private void PlaceAndSwap(Hero holded, IPlacement targetPlacement)
        {
            // get var for swapping
            Hero previousOwner = OccupantOf(targetPlacement);
            IPlacement myPreviousPlacement = holded.CurrentPlacement;

            // place the holded hero on the target placement
            GameManager.Instance.MoveHero(holded, targetPlacement);

            // nobody to swap with, return
            if (previousOwner == null || previousOwner == holded || myPreviousPlacement == null) return;

            // swap the previousOwner to myPreviousPlacement
            GameManager.Instance.MoveHero(previousOwner, myPreviousPlacement);
        }

        // an item can be dropped on 3 things:
        // 1) shop => sell that item
        // 2) one of your heroes => that hero wears it, if it isn't wearing one already
        // 3) a free hex of the deploy zone => it lies on that hex, for a hero whose wearing hex that is
        private void DropItem(Item item, Vector3 worldPos)
        {
            Release();

            // released over the shop = sell the item
            if (IsPointerOverSellZone())
            {
                GameManager.Instance.Sell(item);
                return;
            }

            // anything else is placing, which is only allowed in preparation
            if (!IsPreparation) return;

            // released over the hero = wear item to hero
            if (Picker.At<Hero>(worldPos) is Hero hero)
            {
                if (hero == null || hero.Team != _team) return;

                GameManager.Instance.WearItem(hero, item);
                return;
            }

            // released over a free hex of the deploy zone = lay the item on that hex
            if (Picker.At<Hex>(worldPos) is Hex hex && hex.IsInDeployZone && _board.WhoReservedThisHex(hex) == null)
            {
                GameManager.Instance.PutItemOnHex(item, hex);
            }
        }

        // put back whatever is held, back to its old placement
        private void Cancel()
        {
            if (_held is Hero hero)
            {
                IPlacement placement = hero.CurrentPlacement;

                // OnUnitPlaced re-seats the transform, so this is the snap back
                if (placement != null) placement.OnUnitPlaced(hero);
            }

            if (_held is Item item) PutItemBack(item);

            Release();
        }

        // an item goes back to the hero that wore it, the hex it lay on, or the spot it lay loose
        private void PutItemBack(Item item)
        {
            bool isBack = _itemWornBy != null ? GameManager.Instance.WearItem(_itemWornBy, item)
                        : _itemLayOn != null && GameManager.Instance.PutItemOnHex(item, _itemLayOn);

            if (!isBack) item.transform.position = _itemLooseAt;
        }

        // when stop holding, reset var
        private void Release()
        {
            if (_heldHitbox != null) _heldHitbox.enabled = true;

            // nothing is held any more, so the shop must stop offering to take it
            RefreshSellHint(false);

            _heldHitbox = null;
            _held = null;
            _isClickHold = false;
            _itemWornBy = null;
            _itemLayOn = null;
        }

        // check the placement before placing the hero
        private bool ValidatePlacement(IPlacement placement, Hero hero)
        {
            bool validate = false;

            // if placement is a bench slot and accept drop, allow the placement
            if (placement is BenchSlot slot && hero.Team == _team)
            {
                validate = slot.AcceptsDrop;
            }

            // if placement is hex, check the deploy zone, and current hero limit
            // if correct, allow the placement
            // p.s. a hex either belongs deploy zone or not, deploy zone is the zone player can place heroes in
            else if (placement is Hex targetHex)
            {
                // if it is my hero, the hex must be deploy zone
                // if it is enemy, the hex must not be deploy zone
                bool isMyHero = hero.Team == _team;
                if (isMyHero != targetHex.IsInDeployZone) return false;

                // an item lying on the hex takes the hex
                if (_board.Items.ItemOn(targetHex) != null) return false;

                // if hero is tracked, then this is just re-positioning, no new hero added
                bool isDropHeroTracked = _board.HeroesOnBoard.Any(tracked => (Hero)tracked == hero);
                if (isDropHeroTracked)
                {
                    return true;
                }

                // if hero is not tracked, but was swap, then the new hero is added, but no team size isn't increase
                bool isSwap = _board.IsReservedByOther(targetHex, hero);
                if (isSwap)
                {
                    return true;
                }

                // if none work, this hero is added new to the board, so increase team size too
                return _teamSize.IsAddingOK();
            }

            return validate;
        }

        // Who is standing here?
        private Hero OccupantOf(IPlacement placement)
        {
            if (placement is Hex hex) return _board.WhoReservedThisHex(hex) as Hero;

            if (placement is BenchSlot slot) return slot.Occupant as Hero;

            return null;
        }

        // =================================== sell zone ===================================
        // is player's pointer over shop boundary?
        private bool IsPointerOverSellZone()
        {
            return _sellZone != null && _sellZone.IsInSellBoundary(PlayerInputSystem.PointerScreenPosition);
        }

        private void RefreshSellHint(bool isOverZone)
        {
            if (isOverZone == _sellHintShown) return;

            _sellZone?.ShowSellHint(isOverZone);
            _sellHintShown = isOverZone;
        }
    }
}
