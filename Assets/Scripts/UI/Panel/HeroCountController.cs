using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    /// The "2 / 3 ON BOARD" chip, shown in its own box in the bottom row while the player is placing heroes.
    internal class HeroCountController : PanelController, IHeroCountPanel
    {
        // at the hero limit, warn by giving it red color
        private const string FullClass = "hero-count--full";

        private VisualElement _heroCount;
        private Label _heroCountValue;

        // =================================== IHeroCountPanel interface ===================================
        public void ShowHeroCount(int placed, int limit)
        {
            // hero limit text e.g. 3/3 
            if (_heroCountValue != null) _heroCountValue.text = $"{placed} / {limit}";

            // warn when the hero is at limit
            if (_heroCount != null) _heroCount.EnableInClassList(FullClass, placed >= limit);

            SetShown(true);
        }

        // =================================== Life cycle ===================================
        protected override void OnMounted(VisualElement panel)
        {
            _heroCount = panel.Q<VisualElement>("HeroCount");
            _heroCountValue = panel.Q<Label>("HeroCountValue");

            // nothing is placed before the player places it
            SetShown(false);
        }
    }
}
