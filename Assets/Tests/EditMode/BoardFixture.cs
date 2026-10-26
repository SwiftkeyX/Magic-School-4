using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using MagicSchool.Contracts;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;

namespace MagicSchool.Combat.Tests
{
    /// <summary>
    /// A board, in code, for EditMode tests - no scene, no prefabs, no BattlePlacementSO, no play
    /// mode. Everything a targeting question needs and nothing it doesn't: hexes at the real
    /// spacing, heroes standing on them, and the board tracking both.
    ///
    /// Geometry copied off the live board (Scenes/Board): columns are 1.0 apart in x, rows 1.0
    /// apart in y, and neighbouring columns are staggered by 0.5 - which is what makes a
    /// same-column neighbour 1.0 away and a diagonal one 1.118. Hex works those neighbours out by
    /// distance, so getting this wrong would silently give every hex the wrong neighbours.
    /// </summary>
    // Blue takes the left columns and red the right, one continuous grid as on the real board, so a distance across
    // the middle means what it does in game. EditMode runs no player loop, so Awake never fires: the fixture calls
    // BattleBoard.InitializeHex itself once the hexes exist. Heroes are real Heroes rather than a fake ICombatant:
    // three lines to build once the board exists, where a fake would be forty members of guesswork drifting from the
    // real one; Init is the whole ceremony.
    // EnemyScan caches the enemies and their distances for one frame, keyed on Time.frameCount, and EditMode never
    // runs a frame, so anything that moves a unit clears the caches (ForgetCachedEnemies) the way the next frame
    // would.
    internal sealed class BoardFixture : IDisposable
    {
        private const float ColumnSpacing = 1.0f;
        private const float RowSpacing = 1.0f;
        private const float ColumnStagger = 0.5f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Hero> _heroes = new List<Hero>();
        private readonly Dictionary<(TeamEnum side, int column, int row), Hex> _hexes
            = new Dictionary<(TeamEnum, int, int), Hex>();

        public BattleBoard Board { get; }

        public BoardFixture(int columnsPerSide = 4, int rows = 7)
        {
            var boardObject = NewObject("BattleBoard");
            Board = boardObject.AddComponent<BattleBoard>();

            BuildSide(TeamEnum.Blue, "BlueSideHex", boardObject.transform, firstColumn: 0, columnsPerSide, rows);
            BuildSide(TeamEnum.Red, "RedSideHex", boardObject.transform, firstColumn: columnsPerSide, columnsPerSide, rows);

            Board.InitializeHex();
        }

        public Hex HexAt(TeamEnum side, int column, int row) => _hexes[(side, column, row)];

        private void BuildSide(TeamEnum side, string parentName, Transform boardTransform,
                               int firstColumn, int columns, int rows)
        {
            var sideObject = NewObject(parentName);
            sideObject.transform.SetParent(boardTransform, worldPositionStays: false);

            for (int column = 0; column < columns; column++)
            {
                int gridColumn = firstColumn + column;

                for (int row = 0; row < rows; row++)
                {
                    var hexObject = NewObject($"{side}-column{column}-row{row}");
                    hexObject.transform.SetParent(sideObject.transform, worldPositionStays: false);

                    float x = gridColumn * ColumnSpacing;
                    float y = -(row * RowSpacing) - (gridColumn % 2 == 0 ? ColumnStagger : 0f);
                    hexObject.transform.localPosition = new Vector3(x, y, 0f);

                    _hexes[(side, column, row)] = hexObject.AddComponent<Hex>();
                }
            }
        }

        public Hero AddHero(TeamEnum team, Hex hex, int range = 1)
        {
            var data = ScriptableObject.CreateInstance<HeroDataSO>();
            if (range != 1) SetPrivateField(data, "_range", range);

            var heroObject = NewObject($"{team}Hero");
            var hero = heroObject.AddComponent<Hero>();
            hero.Init(data, Board, team);

            hex.OnUnitPlaced(hero);
            hero.TrackOnBoard();

            _heroes.Add(hero);
            return hero;
        }

        public Hero AddBenchedHero(TeamEnum team)
        {
            var heroObject = NewObject($"{team}BenchedHero");
            var hero = heroObject.AddComponent<Hero>();
            hero.Init(ScriptableObject.CreateInstance<HeroDataSO>(), Board, team);
            Board.TrackThisHero(hero);

            _heroes.Add(hero);
            return hero;
        }

        public void MoveHero(Hero hero, Hex to)
        {
            IPlacement previous = hero.CurrentPlacement;
            if (previous != null) previous.OnUnitUnplaced(hero);
            to.OnUnitPlaced(hero);

            ForgetCachedEnemies();
        }

        private void ForgetCachedEnemies()
        {
            foreach (Hero hero in _heroes) hero.ForgetScanCaches();
        }

        private GameObject NewObject(string name)
        {
            var created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }

        private static void SetPrivateField(object target, string field, object value)
            => target.GetType()
                     .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                     .SetValue(target, value);

        public void Dispose()
        {
            foreach (GameObject spawned in _spawned)
                if (spawned != null) UnityEngine.Object.DestroyImmediate(spawned);

            _spawned.Clear();
            _heroes.Clear();
            _hexes.Clear();
        }
    }
}
