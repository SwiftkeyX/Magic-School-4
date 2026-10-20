using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MagicSchool.Core.Benchmark
{
    // report: the runs of one comp x stage x aligning averaged into one team row, and one row for each of its heroes
    public partial class TeamCompBenchmark
    {
        private const string Prefix = "teamcomp-";
        private static string ReportFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BalanceLogs"));

        private static string Report(List<Fight> fights, float realStart)
        {
            var groups = fights
                .GroupBy(f => (comp: f.Comp, stage: f.StageNumber, aligning: f.Aligning))
                .OrderBy(g => g.Key.comp.name).ThenBy(g => g.Key.stage).ThenByDescending(g => g.Key.aligning)
                .ToList();

            string path = Write(groups);
            Debug.Log($"[TeamCompBenchmark] done - {fights.Count(f => f.Error == null)} fights in " +
                      $"{Time.realtimeSinceStartup - realStart:0}s real time.\n{Summary(groups)}\n{path}");
            return path;
        }

        // per comp: how many stages it wins (at least half its runs) with aligning and without
        private static string Summary(List<IGrouping<(Combat.Placements.BattlePlacementSO comp, int stage, bool aligning), Fight>> groups)
        {
            var lines = new List<string>();
            foreach (var byComp in groups.GroupBy(g => g.Key.comp))
            {
                string Won(bool aligning)
                {
                    var stages = byComp.Where(g => g.Key.aligning == aligning && g.All(f => f.Error == null)).ToList();
                    int won = stages.Count(g => WinRate(g.ToList()) >= 0.5f);
                    string lost = string.Join(" ", stages.Where(g => WinRate(g.ToList()) < 0.5f).Select(g => g.Key.stage));
                    return $"{won}/{stages.Count}" + (lost.Length > 0 ? $" (loses stage {lost})" : "");
                }

                lines.Add($"{byComp.Key.name}: wins {Won(true)} stages aligned, {Won(false)} without aligning");
            }

            return string.Join("\n", lines);
        }

        private static float WinRate(List<Fight> runs)
            => runs.Count == 0 ? 0f : (float)runs.Count(f => f.Outcome == Outcome.Win) / runs.Count;

        private static string Write(List<IGrouping<(Combat.Placements.BattlePlacementSO comp, int stage, bool aligning), Fight>> groups)
        {
            Directory.CreateDirectory(ReportFolder);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string path = Path.Combine(ReportFolder, Prefix + stamp + ".csv");

            var text = new StringBuilder();
            text.AppendLine("row,comp,stage,stage_setup,aligning,runs,win_rate,wins,timeouts,avg_seconds,heroes_left,team_hp_left,team_dps," +
                            "hero,dps,damage_share,damage_taken,healing,survive_rate,died_at,error");

            foreach (var group in groups)
            {
                List<Fight> runs = group.ToList();
                Fight first = runs[0];
                string head = $"{Csv(first.Comp.name)},{first.StageNumber},{Csv(first.Stage != null ? first.Stage.name : "")},{(first.Aligning ? "on" : "off")}";

                if (first.Error != null)
                {
                    text.AppendLine($"team,{head},0,,,,,,,,,,,,,,,{Csv(first.Error)}");
                    continue;
                }

                float seconds = runs.Sum(f => f.Seconds);
                int teamDamage = runs.Sum(f => f.Heroes.Sum(h => h.Damage));
                float heroesLeft = (float)runs.Average(f => f.Heroes.Count(h => h.Survived));
                float hpLeft = (float)runs.Average(f => f.Heroes.Count > 0 ? f.Heroes.Average(h => h.HpLeft) : 0f);

                text.AppendLine($"team,{head},{runs.Count},{Pct(WinRate(runs))},{runs.Count(f => f.Outcome == Outcome.Win)}," +
                                $"{runs.Count(f => f.Outcome == Outcome.Timeout)},{F(seconds / runs.Count)},{F(heroesLeft)},{Pct(hpLeft)}," +
                                $"{F(seconds > 0 ? teamDamage / seconds : 0)},,,,,,,,");

                // a hero twice in one comp is one row, its numbers added up
                foreach (var hero in runs.SelectMany(f => f.Heroes).GroupBy(h => h.Name).OrderByDescending(g => g.Sum(h => h.Damage)))
                {
                    List<HeroLine> lines = hero.ToList();
                    int damage = lines.Sum(h => h.Damage);
                    text.AppendLine($"hero,{head},{runs.Count},,,,,,,," +
                                    $"{Csv(hero.Key)},{F(seconds > 0 ? damage / seconds : 0)},{Pct(teamDamage > 0 ? (float)damage / teamDamage : 0)}," +
                                    $"{F((float)lines.Sum(h => h.DamageTaken) / runs.Count)},{F((float)lines.Sum(h => h.Healing) / runs.Count)}," +
                                    $"{Pct((float)lines.Count(h => h.Survived) / lines.Count)}," +
                                    $"{(lines.Any(h => !h.Survived) ? F(lines.Where(h => !h.Survived).Average(h => h.DiedAt)) : "")},");
                }
            }

            File.WriteAllText(path, text.ToString(), Encoding.UTF8);
            return path;
        }

        private static string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        private static string Pct(float v) => (v * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        private static string Csv(string s) => string.IsNullOrEmpty(s) ? "" : s.Contains(",") ? "\"" + s + "\"" : s;
    }
}
