using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // This is the dev tool to have a quick look at scoreboard panel.
    internal class ScoreboardPreview : MonoBehaviour
    {
        // pre-seeded the record. For fast quick look at the panel.
        [SerializeField]
        private List<Row> _rows = new List<Row>
        {
            new Row { Name = "Vharn",   Team = TeamEnum.Blue, Auto = 1210, Skill = 1270, Overkill = 145, Taken = 1890, Blocked = 620, Received = 340 },
            new Row { Name = "Sithra",  Team = TeamEnum.Red,  IsAlive = false, Auto = 640, Skill = 1335, Taken = 2310, Blocked = 410 },
            new Row { Name = "Solace",  Team = TeamEnum.Blue, Auto = 430, Taken = 760, Blocked = 240, Healed = 1180, Overheal = 295, Received = 120, Wound = 60 },
            new Row { Name = "Pip",     Team = TeamEnum.Blue, Taken = 90, Blocked = 12 },
            new Row { Name = "Bulwark", Team = TeamEnum.Red,  Auto = 180, Skill = 90, Overkill = 20, Taken = 3120, Blocked = 2400, Received = 900, Wound = 220 },
        };

        private IScoreboardPanel _scoreboard;
        private bool _shown;

        private void Start() => FindScoreboard();

        // find scoreboard 
        private IScoreboardPanel FindScoreboard()
        {
            if (_scoreboard as UnityEngine.Object != null) return _scoreboard;

            _scoreboard = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<IScoreboardPanel>()
                .FirstOrDefault();

            return _scoreboard;
        }

        // add context menu in inspector. For toggling the scoreboard.
        [ContextMenu("Toggle preview")]
        private void Toggle()
        {
            if (FindScoreboard() == null) return;

            _shown = !_shown;

            // ShowScores shows the panel itself, so there is only a hide to do on the way back
            if (_shown) _scoreboard.ShowScores(BuildRows());
            else _scoreboard.SetShown(false);
        }

        // =================================== helper ===================================
        private IReadOnlyList<ScoreRow> BuildRows()
        {
            return _rows
                .Where(row => row != null)
                .Select(row => new ScoreRow(row.Name, row.Team, row.IsAlive, new FakeRecord(row)))
                .ToList();
        }

        // in scoreboard preview, class for author a new row 
        [Serializable]
        private class Row
        {
            public string Name = "Hero";
            public TeamEnum Team = TeamEnum.Blue;
            public bool IsAlive = true;

            [Header("dealt")]
            public int Auto;
            public int Skill;
            public int Overkill;

            [Header("taken")]
            public int Taken;
            public int Blocked;

            [Header("healing done")]
            public int Healed;
            public int Overheal;

            [Header("healing taken")]
            public int Received;
            public int Wound;
        }

        // in scoreboard preview, class for author a new fake record for each hero
        private class FakeRecord : ICombatRecord
        {
            private readonly Row _row;

            public FakeRecord(Row row) => _row = row;

            public int DamageDealt => _row.Auto + _row.Skill;
            public int AutoAttackDamage => _row.Auto;
            public int SkillDamage => _row.Skill;
            public int Overkill => _row.Overkill;
            public int HealingDone => _row.Healed;
            public int Overhealing => _row.Overheal;

            public int DamageTaken => _row.Taken;
            public int DamageMitigated => _row.Blocked;
            public int HealingReceived => _row.Received;
            public int HealingLostToWound => _row.Wound;
        }
    }
}
