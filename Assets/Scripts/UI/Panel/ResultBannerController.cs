using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // The overlay panel said: [CLEARED, DEFEATED, DRAW] when the game end
    internal class ResultBannerController : PanelController, IBannerPanel
    {
        private const string BlueClass = "banner__text--blue";
        private const string RedClass = "banner__text--red";
        private const string DrawClass = "banner__text--draw";

        private Label _resultText;

        // =================================== IBannerPanel interface ===================================
        public void ShowResult(TeamEnum? winner, int stage, int stageCount, bool runCleared)
        {
            if (_resultText == null) return;

            _resultText.RemoveFromClassList(BlueClass);
            _resultText.RemoveFromClassList(RedClass);
            _resultText.RemoveFromClassList(DrawClass);

            if (winner == TeamEnum.Blue)
            {
                _resultText.text = runCleared ? "RUN CLEARED" : $"STAGE {stage} CLEARED";
                _resultText.AddToClassList(BlueClass);
            }
            else if (winner == TeamEnum.Red)
            {
                _resultText.text = "DEFEATED";
                _resultText.AddToClassList(RedClass);
            }
            else
            {
                // nobody left standing on either side
                _resultText.text = "DRAW";
                _resultText.AddToClassList(DrawClass);
            }

            SetShown(true);
        }

        // =================================== Life cycle ===================================
        protected override void OnMounted(VisualElement panel)
        {
            _resultText = panel.Q<Label>("ResultText");

            SetShown(false);
        }
    }
}
