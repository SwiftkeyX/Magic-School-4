using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    /// The inspect panel on the right. Opens when the player right-clicks a inspectable
    /// e.g. hero, item.
    internal class InspectorController : PanelController, IInspectorPanel
    {
        private Label _heroName, _heroTeam;
        private Label _hpLabel, _manaLabel;
        private VisualElement _hpBar, _manaBar, _statGrid;
        private VisualElement _hpFill, _manaFill;
        private Label _statAtk, _statDef, _statMag, _statMr, _statAs, _statRange;
        private VisualElement _activeSection, _passiveSection, _itemSection;
        private Label _activeName, _activeBody, _passiveBody, _itemBody;
        private IInspectable _shown;

        // Some part of the inspector are always change dynamically 
        // e.g. update HP and Mana every frame
        private void Update()
        {
            if (_shown == null) return;

            // if the current inspectable dies, guard it 
            if (!_shown.IsAlive) { Inspect(null); return; }

            // If it was hero, the inspector update its hp and mana every frame
            if (_shown is IInspectableHero) Refresh();
        }


        // ============================== IInspectorPanel interface ==============================
        // update the panel according to whatever was just inspected
        public void Inspect(IInspectable unit)
        {
            // if the thing selected is already the inspected one, return.
            if (Panel != null && _shown != null && ReferenceEquals(_shown, unit)) return;

            _shown = unit;

            if (Panel == null) return;

            bool hasSomething = unit != null && unit.IsAlive;
            Panel.EnableInClassList("hero-panel--empty", !hasSomething);
            if (!hasSomething) return;

            // the one line both kinds share
            _heroName.text = unit.DisplayName;

            if (unit is IInspectableHero hero) ShowHero(hero);
            else if (unit is IInspectableItem item) ShowItem(item);
        }

        // =================================== override ===================================
        // OnMounted, get all element, and hide the inspector panel
        protected override void OnMounted(VisualElement panel)
        {
            CacheElements();

            // nothing is selected until the player right-clicks something
            Inspect(null);
        }

        // get reference to every text in this hero panel
        private void CacheElements()
        {
            _heroName = Panel.Q<Label>("HeroName");
            _heroTeam = Panel.Q<Label>("HeroTeam");

            _hpBar = Panel.Q<VisualElement>("HpBar");
            _manaBar = Panel.Q<VisualElement>("ManaBar");
            _statGrid = Panel.Q<VisualElement>("StatGrid");

            _hpFill = Panel.Q<VisualElement>("HpFill");
            _hpLabel = Panel.Q<Label>("HpLabel");
            _manaFill = Panel.Q<VisualElement>("ManaFill");
            _manaLabel = Panel.Q<Label>("ManaLabel");

            _statAtk = Panel.Q<Label>("StatAtk");
            _statDef = Panel.Q<Label>("StatDef");
            _statMag = Panel.Q<Label>("StatMag");
            _statMr = Panel.Q<Label>("StatMr");
            _statAs = Panel.Q<Label>("StatAs");
            _statRange = Panel.Q<Label>("StatRange");

            _activeSection = Panel.Q<VisualElement>("ActiveSection");
            _activeName = Panel.Q<Label>("ActiveName");
            _activeBody = Panel.Q<Label>("ActiveBody");
            _passiveSection = Panel.Q<VisualElement>("PassiveSection");
            _passiveBody = Panel.Q<Label>("PassiveBody");
            _itemSection = Panel.Q<VisualElement>("ItemSection");
            _itemBody = Panel.Q<Label>("ItemBody");
        }


        // ============================================= hero =============================================
        // the hero inspector: team, bars, stat grid, ability text
        private void ShowHero(IInspectableHero hero)
        {
            UnShowItem();

            SetShown(_heroTeam, true);
            SetShown(_hpBar, true);
            SetShown(_manaBar, true);
            SetShown(_statGrid, true);

            _heroTeam.text = hero.Team.ToString().ToUpperInvariant();
            _statAtk.text = hero.AttackDamage.ToString();
            _statDef.text = hero.Defence.ToString();
            _statMag.text = hero.Magic.ToString();
            _statMr.text = hero.MagicResist.ToString();
            _statAs.text = hero.AttackSpeed.ToString("0.00");
            _statRange.text = hero.Range.ToString();

            ShowAbility(hero);
            Refresh();
        }

        // resolve ability text
        private void ShowAbility(IInspectableHero unit)
        {
            bool hasActive = unit.HasSkill;
            SetShown(_activeSection, hasActive);
            if (hasActive)
            {
                _activeName.text = unit.SkillName;
                _activeBody.text = string.IsNullOrEmpty(unit.SkillDescription)
                    ? "No description written yet."
                    : unit.SkillDescription;
            }

            // only two heroes have one - an empty PASSIVE heading on the other fifteen reads as a bug
            bool hasPassive = unit.HasPassive;
            SetShown(_passiveSection, hasPassive);
            if (hasPassive)
            {
                _passiveBody.text = string.IsNullOrEmpty(unit.PassiveDescription)
                    ? "No description written yet."
                    : unit.PassiveDescription;
            }
        }

        private void UnShowHero()
        {
            SetShown(_heroTeam, false);
            SetShown(_hpBar, false);
            SetShown(_manaBar, false);
            SetShown(_statGrid, false);
            SetShown(_activeSection, false);
            SetShown(_passiveSection, false);
        }

        // ============================================= item =============================================
        // the item half: a name and what it does, with every hero-shaped row hidden
        private void ShowItem(IInspectableItem item)
        {
            UnShowHero();

            SetShown(_itemSection, true);
            _itemBody.text = string.IsNullOrEmpty(item.Description)
                ? "No description written yet."
                : item.Description;
        }

        private void UnShowItem()
        {
            SetShown(_itemSection, false);
        }


        // FLAGGING: those helper look so generic that it don't have to be in this class. Let leave it here for now.
        // ============================================= helper =============================================
        // update bar to hp and mana
        private void Refresh()
        {
            if (!(_shown is IInspectableHero hero)) return;

            SetBar(_hpFill, _hpLabel, hero.CurrentHP, hero.MaxHP);
            SetBar(_manaFill, _manaLabel, hero.CurrentMana, hero.MaxMana);
        }

        private static void SetBar(VisualElement fill, Label label, int current, int max)
        {
            float percent = max <= 0 ? 0f : Mathf.Clamp01((float)current / max) * 100f;
            fill.style.width = Length.Percent(percent);
            label.text = $"{current} / {max}";
        }

        // show/hide the text element
        private static void SetShown(VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
