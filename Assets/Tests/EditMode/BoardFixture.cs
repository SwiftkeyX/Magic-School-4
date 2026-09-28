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
    internal sealed class BoardFixture : IDisposable
    {
        private const float ColumnSpacing = 1.0f;
        private const float RowSpacing = 1.0f;
        private const float ColumnStagger = 0.5f;   // every other column slides half a row down

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Hero> _heroes = new List<Hero>();
        private readonly Dictionary<(TeamEnum side, int column, int row), Hex> _hexes
            = new Dictionary<(TeamEnum, int, int), Hex>();

        public BattleBoard Board { get; }

        // Blue side takes the left columns, red the right, laid out as one continuous grid the way
        // the real board is - so a distance measured across the middle means what it does in game.
        public BoardFixture(int columnsPerSide = 4, int rows = 7)
        {
            var boardObject = NewObject("BattleBoard");
            Board = boardObject.AddComponent<BattleBoard>();

            BuildSide(TeamEnum.Blue, "BlueSideHex", boardObject.transform, firstColumn: 0, columnsPerSide, rows);
            BuildSide(TeamEnum.Red, "RedSideHex", boardObject.transform, firstColumn: columnsPerSide, columnsPerSide, rows);

            // EditMode runs no player loop, so Awake never fires - kick off the hex discovery that
            // BattleBoard would normally do there, now that the hexes exist to be discovered.
            typeof(BattleBoard)
                .GetMethod("InitializeHex", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(Board, null);
        }

        // ============================== the board ==============================
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

        // ============================== the units ==============================
        /// <summary>
        /// A real Hero rather than a fake ICombatant: it is three lines to build once the board
        /// exists, and a fake would be forty members of guesswork that drifts from the real one.
        /// Nothing here needs the spawner or a prefab - Init is the whole ceremony.
        /// </summary>
        public Hero AddHero(TeamEnum team, Hex hex, int range = 1)
        {
            var data = ScriptableObject.CreateInstance<HeroDataSO>();
            if (range != 1) SetPrivateField(data, "_range", range);

            var heroObject = NewObject($"{team}Hero");
            var hero = heroObject.AddComponent<Hero>();
            hero.Init(data, Board, team);

            // the hex hands the hero its placement, its position and its reservation
            hex.OnUnitPlaced(hero);
            hero.TrackOnBoard();

            _heroes.Add(hero);
            return hero;
        }

        // A hero that exists but is not on the board - the bench case that targeting must ignore.
        public Hero AddBenchedHero(TeamEnum team)
        {
            var heroObject = NewObject($"{team}BenchedHero");
            var hero = heroObject.AddComponent<Hero>();
            hero.Init(ScriptableObject.CreateInstance<HeroDataSO>(), Board, team);
            Board.TrackThisHero(hero);      // tracked but placeless: IsInCombat is false

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

        /// <summary>
        /// FindEnemy caches who the enemies are and how far off they stand for the length of one
        /// frame, keyed on Time.frameCount. EditMode never runs a frame, so that cache would live
        /// for the whole test and answer every question with the board as it was at the first one.
        /// Anything that moves a unit has to clear it, the way the next frame would in a real game.
        /// </summary>
        private void ForgetCachedEnemies()
        {
            foreach (Hero hero in _heroes)
            {
                object finder = GetPrivateField(hero, "_findEnemy");
                if (finder == null) continue;

                SetPrivateField(finder, "_enemyBFSCacheFrame", -1);
                SetPrivateField(finder, "_enemyDistanceCacheFrame", -1);
            }
        }

        // ============================== plumbing ==============================
        private GameObject NewObject(string name)
        {
            var created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }

        private static object GetPrivateField(object target, string field)
            => target.GetType()
                     .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                     .GetValue(target);

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
