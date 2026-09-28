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
    // e.g. draggable are items, and hero
    internal class Dragging
    {
        private readonly Camera _cam;
        private readonly BattleBoard _board;
        private readonly ISellZone _sellZone;
        private readonly TeamEnum _team;
        private readonly PlayerTeamSize _teamSize;

        private IDraggable _held;
        private Collider2D _heldHitbox;
        private bool _sellHintShown;

        // ============================= getter =============================
        public bool IsHolding => _held as Object != null;
        private bool IsPreparation => GameManager.Instance.Phase == GamePhaseEnum.Preparation;

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
            // guard - if not in preparation state, not allow to hold the hero.
            if (!IsPreparation && _held is Hero) Cancel();

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
            bool isSellable = _held is Hero;
            RefreshSellHint(isSellable && IsPointerOverSellZone());

            // if still holding, return
            bool stillHolding = PlayerInputSystem.IsPointerDown && !PlayerInputSystem.DragReleasedThisFrame;
            if (stillHolding) return;

            // if releasing, drop the hero 
            Drop(worldPos);
        }

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

            // if the item have wearer, take it off
            Hero wearer = item.GetComponentInParent<Hero>();
            if (wearer != null)
            {
                // if not in preparation state, not allow to strip item off hero.
                if (!IsPreparation) return null;

                wearer.TryTakeOff(item);
            }

            return item;
        }

        // There's some hero that player shouldn't allow to drag
        // e.g. enemy
        private bool CanDragThisHero(Hero hero)
        {
            return hero != null && hero.Team == _team;
        }

        private void Grab(IDraggable target)
        {
            _held = target;
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
                GameManager.Instance.SellHero(hero);
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

        // an item can be dropped on 1 thing so far:
        // 1) one of your heroes => that hero wears it, if it has a slot free
        private void DropItem(Item item, Vector3 worldPos)
        {
            Hero hero = Picker.At<Hero>(worldPos);

            Release();

            // wear the item to your hero
            if (hero == null || hero.Team != _team) return;

            // only in preparation state, allow to wear item to the hero
            if (IsPreparation) hero.TryWear(item);
        }

        // put back whatever is held, as far as it can be put back
        private void Cancel()
        {
            // only a hero has somewhere to be put back to; an item stays where it is
            if (_held is Hero hero)
            {
                IPlacement placement = hero.CurrentPlacement;

                // OnUnitPlaced re-seats the transform, so this is the snap back
                if (placement != null) placement.OnUnitPlaced(hero);
            }

            Release();
        }

        // when stop holding, reset var
        private void Release()
        {
            if (_heldHitbox != null) _heldHitbox.enabled = true;

            // nothing is held any more, so the shop must stop offering to take it
            RefreshSellHint(false);

            _heldHitbox = null;
            _held = null;
        }

        // check the placement before placing the hero
        private bool ValidatePlacement(IPlacement placement, Hero hero)
        {
            bool validate = false;

            // if placement is bench, allow the placement
            if (placement is BenchSlot)
            {
                validate = true;
            }

            // if placement is hex, check the which team hex belong to, and current hero limit
            // if correct, allow the placement
            // p.s. Hex could only belong to either Red or Blue team
            else if (placement is Hex targetHex)
            {
                bool correctTeam = targetHex.GetTeam() == _team;
                if (!correctTeam) return false;

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
