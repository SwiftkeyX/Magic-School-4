using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    /// The "2 / 3 ON BOARD" chip, shown in its own box in the bottom row while the player is placing heroes.
    internal class HeroCountController : PanelController, IHeroCountPanel
    {
        private const string FullClass = "hero-count--full";

        private VisualElement _heroCount;
        private Label _heroCountValue;

        public void ShowHeroCount(int placed, int limit)
        {
            if (_heroCountValue != null) _heroCountValue.text = $"{placed} / {limit}";

            if (_heroCount != null) _heroCount.EnableInClassList(FullClass, placed >= limit);

            SetShown(true);
        }

        protected override void OnMounted(VisualElement panel)
        {
            _heroCount = panel.Q<VisualElement>("HeroCount");
            _heroCountValue = panel.Q<Label>("HeroCountValue");

            SetShown(false);
        }
    }
}
