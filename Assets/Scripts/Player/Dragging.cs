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
    // A held hero is dropped on a placement (put there, swapping with whoever stands on it), on the shop (sold,
    // Preparation only), or on nothing (back where it was). One from the shop, not bought yet: on the shop it goes
    // back unbought - the shop wins over a placement behind it, unlike selling, since a mistaken buy costs gold and a
    // put-back costs nothing; on a free placement it may stand on it is bought there (mid-fight a bench slot only);
    // anywhere else it is bought onto the bench.
    // A held item is dropped on the shop (sold, any phase), on one of your heroes (worn, if it wears nothing), or on a
    // free deploy-zone hex (lies there). Taking an item off a hero or a hex, and moving heroes, is Preparation only.
    // A placed hero counts against the team size only when it is new to the board: re-positioning or a swap adds
    // nobody.
    internal class Dragging
    {
        private readonly Camera _cam;
        private readonly BattleBoard _board;
        private readonly ISellZone _sellZone;
        private readonly TeamEnum _team;
        private readonly PlayerTeamSize _teamSize;

        private const float ClickSlop = 10f;

        private IDraggable _held;
        private Collider2D _heldHitbox;
        private bool _sellHintShown;
        private bool _isClickHold;
        private Vector2 _pressedAt;
        private int _pressUsedFrame = -1;

        private Hero _itemWornBy;
        private Hex _itemLayOn;
        private Vector3 _itemLooseAt;

        private int _shopSlot = -1;

        public bool IsHolding => _held as Object != null;
        public Hero HeldHero => IsHolding ? _held as Hero : null;
        public Item HeldItem => IsHolding ? _held as Item : null;
        private bool IsFromShop => _shopSlot >= 0 && _held is Hero;
        private bool IsPreparation => GameManager.Instance.Phase == GamePhaseEnum.Preparation;

        public bool UsedPressThisFrame => _pressUsedFrame == Time.frameCount;
        private bool _isStageEditor => GameManager.Instance.StageEditor != null;

        public Dragging(Camera cam, BattleBoard board, ISellZone sellZone, TeamEnum team, PlayerTeamSize teamSize)
        {
            _cam = cam;
            _board = board;
            _sellZone = sellZone;
            _team = team;
            _teamSize = teamSize;

            GameManager.Instance.Actions.OnShopHeroPickedUp += HoldFromShop;
        }

        public void Tick()
        {
            if (!IsPreparation && ((_held is Hero && !IsFromShop) || IsItemFromBoard)) Cancel();

            Vector3 worldPos = PlayerInputSystem.GetMouseWorldPosition(_cam);

            if (!IsHolding)
            {
                TryPickUp(worldPos);
                return;
            }

            Follow(worldPos);

            bool isSellable = _held is ISellable && !IsFromShop;
            RefreshSellHint(isSellable && IsPointerOverSellZone());

            if (_isClickHold)
            {
                if (PlayerInputSystem.CancelPressedThisFrame) { Cancel(); return; }
                if (!PlayerInputSystem.DragPressedThisFrame) return;

                _pressUsedFrame = Time.frameCount;
                Drop(worldPos);
                return;
            }

            bool stillHolding = PlayerInputSystem.IsPointerDown && !PlayerInputSystem.DragReleasedThisFrame;
            if (stillHolding) return;

            if (IsClick())
            {
                _isClickHold = true;
                return;
            }

            Drop(worldPos);
        }

        private bool IsItemFromBoard => _held is Item && (_itemWornBy != null || _itemLayOn != null);

        private bool IsClick() => Vector2.Distance(PlayerInputSystem.PointerScreenPosition, _pressedAt) <= ClickSlop;

        private void TryPickUp(Vector3 worldPos)
        {
            if (!PlayerInputSystem.DragPressedThisFrame) return;

            if (IsPreparation)
            {
                IDraggable hero = TryPickHero(worldPos);
                if (hero != null)
                {
                    Grab(hero);
                    return;
                }
            }

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

            _itemWornBy = null;
            _itemLayOn = null;
            _itemLooseAt = item.transform.position;

            Hero wearer = item.GetComponentInParent<Hero>();
            if (wearer != null)
            {
                if (!IsPreparation) return null;

                GameManager.Instance.Actions.TakeItemOffHero(wearer, item);
                _itemWornBy = wearer;
            }

            else if (_board.Items.HexOf(item) is Hex lyingOn)
            {
                if (!IsPreparation) return null;

                GameManager.Instance.Actions.TakeItemOffHex(item);
                _itemLayOn = lyingOn;
            }

            return item;
        }

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

        private void HoldFromShop(Hero hero, int slot)
        {
            if (IsHolding || UsedPressThisFrame)
            {
                GameManager.Instance.Actions.ReturnShopHero(hero);
                return;
            }

            Grab(hero);
            _shopSlot = slot;
            Follow(PlayerInputSystem.GetMouseWorldPosition(_cam));
        }

        private void Follow(Vector3 worldPos)
        {
            Transform held = _held.transform;
            held.position = new Vector3(worldPos.x, worldPos.y, held.position.z);
        }

        private void Drop(Vector3 worldPos)
        {
            if (IsFromShop)
            {
                DropShopHero((Hero)_held, worldPos);
                return;
            }

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

        private void DropHero(Hero hero, Vector3 worldPos)
        {
            IPlacement targetPlacement = Picker.At<IPlacement>(worldPos);

            if (targetPlacement != null && ValidatePlacement(targetPlacement, hero))
            {
                PlaceAndSwap(hero, targetPlacement);
                Release();
                return;
            }

            if (targetPlacement == null && IsPointerOverSellZone())
            {
                GameManager.Instance.Actions.Sell(hero);
                Release();
                return;
            }

            Cancel();
        }

        private void DropShopHero(Hero hero, Vector3 worldPos)
        {
            int slot = _shopSlot;
            IPlacement target = Picker.At<IPlacement>(worldPos);
            bool isOverShop = IsPointerOverSellZone();
            Release();

            if (isOverShop)
            {
                GameManager.Instance.Actions.ReturnShopHero(hero);
                return;
            }

            bool canStandThere = target != null && OccupantOf(target) == null
                              && (IsPreparation || target is BenchSlot)
                              && ValidatePlacement(target, hero);

            GameManager.Instance.Actions.BuyShopHero(hero, slot, canStandThere ? target : null);
        }

        private void PlaceAndSwap(Hero holded, IPlacement targetPlacement)
        {
            Hero previousOwner = OccupantOf(targetPlacement);
            IPlacement myPreviousPlacement = holded.CurrentPlacement;

            GameManager.Instance.Actions.MoveHero(holded, targetPlacement);

            if (previousOwner == null || previousOwner == holded || myPreviousPlacement == null) return;

            GameManager.Instance.Actions.MoveHero(previousOwner, myPreviousPlacement);
        }

        private void DropItem(Item item, Vector3 worldPos)
        {
            Release();

            if (IsPointerOverSellZone())
            {
                GameManager.Instance.Actions.Sell(item);
                return;
            }

            if (!IsPreparation) return;

            if (Picker.At<Hero>(worldPos) is Hero hero)
            {
                if (hero == null || hero.Team != _team) return;

                GameManager.Instance.Actions.WearItem(hero, item);
                return;
            }

            if (Picker.At<Hex>(worldPos) is Hex hex && hex.IsInDeployZone && _board.WhoReservedThisHex(hex) == null)
            {
                GameManager.Instance.Actions.PutItemOnHex(item, hex);
            }
        }

        private void Cancel()
        {
            if (IsFromShop)
            {
                GameManager.Instance.Actions.ReturnShopHero((Hero)_held);
                Release();
                return;
            }

            if (_held is Hero hero)
            {
                IPlacement placement = hero.CurrentPlacement;

                if (placement != null) placement.OnUnitPlaced(hero);
            }

            if (_held is Item item) PutItemBack(item);

            Release();
        }

        private void PutItemBack(Item item)
        {
            bool isBack = _itemWornBy != null ? GameManager.Instance.Actions.WearItem(_itemWornBy, item)
                        : _itemLayOn != null && GameManager.Instance.Actions.PutItemOnHex(item, _itemLayOn);

            if (!isBack) item.transform.position = _itemLooseAt;
        }

        private void Release()
        {
            if (_heldHitbox != null) _heldHitbox.enabled = true;

            RefreshSellHint(false);

            _heldHitbox = null;
            _held = null;
            _isClickHold = false;
            _itemWornBy = null;
            _itemLayOn = null;
            _shopSlot = -1;
        }

        private bool ValidatePlacement(IPlacement placement, Hero hero)
        {
            bool validate = false;

            if (placement is BenchSlot slot && hero.Team == _team)
            {
                validate = slot.AcceptsDrop;
            }

            else if (placement is Hex targetHex)
            {
                bool isMyHero = hero.Team == _team;
                if (isMyHero != targetHex.IsInDeployZone) return false;

                if (_board.Items.ItemOn(targetHex) != null) return false;

                bool isDropHeroTracked = _board.HeroesOnBoard.Any(tracked => (Hero)tracked == hero);
                if (isDropHeroTracked)
                {
                    return true;
                }

                bool isSwap = _board.IsReservedByOther(targetHex, hero);
                if (isSwap)
                {
                    return true;
                }

                return _teamSize.IsAddingOK();
            }

            return validate;
        }

        private Hero OccupantOf(IPlacement placement)
        {
            if (placement is Hex hex) return _board.WhoReservedThisHex(hex) as Hero;

            if (placement is BenchSlot slot) return slot.Occupant as Hero;

            return null;
        }

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
