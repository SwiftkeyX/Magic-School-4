using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    /// The line along the bottom telling the player what to do next. Every phase has something to
    /// say, so this panel is never taken down - only its text changes.
    internal class PhaseHintController : PanelController, IHintPanel
    {
        private Label _phaseHint;

        public void ShowPreparation(int stage, int stageCount)
            => SetHint($"{Stage(stage, stageCount)} - drag heroes onto your hexes, then press SPACE to fight");

        public void ShowCombat(int stage, int stageCount)
            => SetHint($"{Stage(stage, stageCount)} - fighting. SPACE to pause, right-click a hero to inspect it");

        public void ShowPaused(int stage, int stageCount)
            => SetHint($"{Stage(stage, stageCount)} - paused. Press SPACE to go on");

        public void ShowScoreboard(TeamEnum? winner, int stage, int stageCount, bool runCleared)
        {
            if (winner == TeamEnum.Blue) SetHint("press SPACE for your reward");
            else ShowResult(winner, stage, stageCount, runCleared);
        }

        public void ShowResult(TeamEnum? winner, int stage, int stageCount, bool runCleared)
        {
            if (runCleared) SetHint("press SPACE to run it again from stage 1");
            else if (winner == TeamEnum.Blue) SetHint($"press SPACE for stage {stage + 1}");
            else SetHint($"press SPACE to retry {Stage(stage, stageCount).ToLowerInvariant()}");
        }

        protected override void OnMounted(VisualElement panel)
        {
            _phaseHint = panel.Q<Label>("PhaseHint");
        }

        private static string Stage(int stage, int stageCount)
        {
            return stageCount > 1 ? $"Stage {stage} of {stageCount}" : "Stage";
        }

        private void SetHint(string text)
        {
            if (_phaseHint != null) _phaseHint.text = text;
        }
    }
}
