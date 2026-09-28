using System.Collections.Generic;
using UnityEngine;

namespace MagicSchool.Core
{
    /// Adjust how fast the game runs: x1, x2 or x3.
    internal class GameSpeed
    {
        public static readonly Dictionary<int, int> Levels = new Dictionary<int, int>();
        private const int DefaultLevel = 1;

        public GameSpeed()
        {
            Levels[1] = 1;
            Levels[2] = 2;
            Levels[3] = 3;
        }

        public int Multiplier { get; private set; } = DefaultLevel;

        public void Set(int multiplier)
        {
            if (!Levels.TryGetValue(multiplier, out int speed)) return;
            
            Multiplier = speed;
            Time.timeScale = Multiplier;
        }

        public void Reset() => Set(DefaultLevel);
    }
}
