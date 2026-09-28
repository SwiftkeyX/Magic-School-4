using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using MagicSchool.Contracts;

namespace MagicSchool.CombatRecording
{
    // This is exporter for a log from CombatRecorder, this log will be use to balance the game.
    public class CombatCsvLog
    {
        private const string Header =
            "timestamp,stage,stage_count,winner,hero,team,alive," +
            "damage_dealt,auto_attack_damage,skill_damage,overkill," +
            "damage_taken,damage_mitigated," +
            "healing_done,overhealing,healing_received,healing_lost_to_wound";
        private readonly string _filePath;

        public CombatCsvLog(string folder)
        {
            // create directory
            Directory.CreateDirectory(folder);

            // get filepath
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            _filePath = Path.Combine(folder, "balance-" + stamp + ".csv");

            // write header
            File.WriteAllText(_filePath, Header + Environment.NewLine, Encoding.UTF8);
        }

        // One call per finished combat round.
        public void AppendRound(int stage, int stageCount, TeamEnum? winner, IReadOnlyList<ScoreRow> rows)
        {
            // 1 row = 1 hero data
            if (rows == null || rows.Count == 0) return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string won = winner == null ? "draw" : winner.ToString();

            // write each row using each hero data
            var text = new StringBuilder();
            foreach (ScoreRow row in rows)
            {
                ICombatRecord r = row.Record;
                if (r == null) continue;

                text.Append(timestamp).Append(',')
                    .Append(Num(stage)).Append(',')
                    .Append(Num(stageCount)).Append(',')
                    .Append(Escape(won)).Append(',')
                    .Append(Escape(row.Name)).Append(',')
                    .Append(Escape(row.Team.ToString())).Append(',')
                    .Append(row.IsAlive ? "alive" : "dead").Append(',')
                    .Append(Num(r.DamageDealt)).Append(',')
                    .Append(Num(r.AutoAttackDamage)).Append(',')
                    .Append(Num(r.SkillDamage)).Append(',')
                    .Append(Num(r.Overkill)).Append(',')
                    .Append(Num(r.DamageTaken)).Append(',')
                    .Append(Num(r.DamageMitigated)).Append(',')
                    .Append(Num(r.HealingDone)).Append(',')
                    .Append(Num(r.Overhealing)).Append(',')
                    .Append(Num(r.HealingReceived)).Append(',')
                    .Append(Num(r.HealingLostToWound))
                    .Append(Environment.NewLine);
            }

            File.AppendAllText(_filePath, text.ToString(), Encoding.UTF8);
        }

        // ======================================== helper ========================================
        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Escape(string field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;

            if (field.IndexOf(',') < 0 && field.IndexOf('"') < 0 && field.IndexOf('\n') < 0) return field;

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
