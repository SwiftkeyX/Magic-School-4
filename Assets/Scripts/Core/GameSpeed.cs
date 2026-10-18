using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Core
{
    /// Adjust how fast the game runs: x1, x2, x3 or x9 - or not at all while paused.
    /// A speed picked while paused is kept for when the game goes on.
    internal class GameSpeed
    {
        public static readonly Dictionary<int, int> Levels = new Dictionary<int, int>();
        private const int DefaultLevel = 1;

        public GameSpeed()
        {
            Levels[1] = 1;
            Levels[2] = 2;
            Levels[3] = 3;
            Levels[9] = 9;
        }

        public int Multiplier { get; private set; } = DefaultLevel;
        public bool IsPaused { get; private set; }

        public void Set(int multiplier)
        {
            if (!Levels.TryGetValue(multiplier, out int speed)) return;

            Multiplier = speed;
            Apply();
        }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
            Apply();
        }

        public void Reset()
        {
            IsPaused = false;
            Set(DefaultLevel);
        }

        private void Apply() => Time.timeScale = IsPaused ? 0f : Multiplier;
    }
}
