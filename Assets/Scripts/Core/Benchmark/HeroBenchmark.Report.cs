using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MagicSchool.Core.Benchmark
{
    // report 
    public partial class HeroBenchmark
    {
        // a hero's report numbers. use for comparison to other Peer
        private readonly struct Peer
        {
            public readonly string Name, Role;
            public readonly float Dps30, AoeDps30, TimeToDie;

            public Peer(string name, string role, float dps30, float aoeDps30, float timeToDie)
            {
                Name = name; Role = role; Dps30 = dps30; AoeDps30 = aoeDps30; TimeToDie = timeToDie;
            }
        }

        // the end of a whole test: 
        // compare everyone's numbers, write the CSV, say what was flagged. Returns the CSV's path.
        private static string Report(List<Result> results, bool isPartialMode, float realStart)
        {
            List<Peer> borrowed = BorrowPeers(isPartialMode, out string peersNote);
            CompareToPeers(results, borrowed);

            string path = Write(results, isPartialMode);
            Debug.Log($"[HeroBenchmark] done - {results.Count} heroes in {Time.realtimeSinceStartup - realStart:0}s real time.{peersNote} " +
                      $"{FlaggedSummary(results)}\n{path}");
            return path;
        }

        // a partial run borrows everyone else's numbers from the latest full report, a full run use its own full report
        private static List<Peer> BorrowPeers(bool isPartialMode, out string peersNote)
        {
            peersNote = "";
            if (!isPartialMode) return new List<Peer>();

            List<Peer> borrowed = LatestFullReport(out string source);
            peersNote = borrowed.Count > 0
                ? $" Peers from {source}."
                : " No full report yet - peers are only the heroes tested now.";
            return borrowed;
        }

        // this run's heroes, plus every borrowed hero this run did not retest
        private static void CompareToPeers(List<Result> results, List<Peer> borrowed)
        {
            var tested = new HashSet<string>(results.Select(r => r.Hero.Name));
            List<Peer> everyone = results
                .Select(r => new Peer(r.Hero.Name, r.Hero.Role.ToString(), r.Dps30, r.AoeDps30, r.TimeToDie))
                .Concat(borrowed.Where(p => !tested.Contains(p.Name)))
                .ToList();

            foreach (var role in results.GroupBy(r => r.Hero.Role.ToString()))
            {
                List<Peer> peers = everyone.Where(p => p.Role == role.Key).ToList();
                float dpsMedian = Median(peers.Select(p => p.Dps30));
                float aoeMedian = Median(peers.Select(p => p.AoeDps30));
                float ttdMedian = Median(peers.Select(p => p.TimeToDie));

                foreach (Result r in role)
                {
                    r.DpsVsRole = dpsMedian > 0f ? r.Dps30 / dpsMedian - 1f : 0f;
                    r.AoeVsRole = aoeMedian > 0f ? r.AoeDps30 / aoeMedian - 1f : 0f;
                    r.TtdVsRole = ttdMedian > 0f ? r.TimeToDie / ttdMedian - 1f : 0f;
                }
            }
        }

        private static float Median(IEnumerable<float> values)
        {
            List<float> sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0f;

            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2f;
        }

        private static string Flag(float vsMedian) => vsMedian > FlagBand ? "HIGH" : vsMedian < -FlagBand ? "LOW" : "";

        private static string FlaggedSummary(List<Result> results)
        {
            var lines = results
                .Where(r => Flag(r.DpsVsRole) != "" || Flag(r.AoeVsRole) != "" || Flag(r.TtdVsRole) != "")
                .Select(r => $"{r.Hero.Name} ({r.Hero.Role})" +
                             (Flag(r.DpsVsRole) != "" ? $" damage {Flag(r.DpsVsRole)} {r.DpsVsRole:+0%;-0%}" : "") +
                             (Flag(r.AoeVsRole) != "" ? $" aoe {Flag(r.AoeVsRole)} {r.AoeVsRole:+0%;-0%}" : "") +
                             (Flag(r.TtdVsRole) != "" ? $" tank {Flag(r.TtdVsRole)} {r.TtdVsRole:+0%;-0%}" : ""));
            string list = string.Join("; ", lines);
            return list.Length == 0 ? "Nothing outside +-20% of its role." : "Flagged: " + list;
        }

        private static string ReportFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BalanceLogs"));
        private const string FullPrefix = "benchmark-";
        private const string PartialPrefix = "benchmark-selected-";

        private static string Write(List<Result> results, bool partial)
        {
            string folder = ReportFolder;
            Directory.CreateDirectory(folder);
            string stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            string path = Path.Combine(folder, (partial ? PartialPrefix : FullPrefix) + stamp + ".csv");

            var text = new StringBuilder();
            text.AppendLine("hero,tier,role,price," +
                            "dps_15s,dps_30s,auto_share,healing_per_sec,dps_vs_role,dps_flag," +
                            "aoe_dps_15s,aoe_dps_30s,aoe_gain,aoe_vs_role,aoe_flag," +
                            "time_to_die,survived,hp_left,damage_taken,mitigated,healing_received,ttd_vs_role,tank_flag," +
                            "dummy_revives,error");

            foreach (Result r in results.OrderBy(r => r.Hero.Role).ThenByDescending(r => r.Dps30))
            {
                text.Append(Csv(r.Hero.Name)).Append(',')
                    .Append(r.Hero.Tier).Append(',')
                    .Append(r.Hero.Role).Append(',')
                    .Append(r.Hero.Price).Append(',')
                    .Append(F(r.Dps15)).Append(',')
                    .Append(F(r.Dps30)).Append(',')
                    .Append(Pct(r.AutoShare)).Append(',')
                    .Append(F(r.Hps30)).Append(',')
                    .Append(Pct(r.DpsVsRole)).Append(',')
                    .Append(Flag(r.DpsVsRole)).Append(',')
                    .Append(F(r.AoeDps15)).Append(',')
                    .Append(F(r.AoeDps30)).Append(',')
                    .Append("x").Append(F(r.AoeGain)).Append(',')
                    .Append(Pct(r.AoeVsRole)).Append(',')
                    .Append(Flag(r.AoeVsRole)).Append(',')
                    .Append(F(r.TimeToDie)).Append(',')
                    .Append(r.Survived ? "yes" : "no").Append(',')
                    .Append(Pct(r.HpLeft)).Append(',')
                    .Append(r.DamageTaken).Append(',')
                    .Append(r.Mitigated).Append(',')
                    .Append(r.HealingReceived).Append(',')
                    .Append(Pct(r.TtdVsRole)).Append(',')
                    .Append(Flag(r.TtdVsRole)).Append(',')
                    .Append(r.DummyRevives).Append(',')
                    .Append(Csv(r.Error))
                    .AppendLine();
            }

            File.WriteAllText(path, text.ToString(), Encoding.UTF8);
            return path;
        }

        // the newest full run's per-hero numbers. Only full reports - a partial one only knows a few heroes.
        // Reports from before the AoE test have no aoe column; those heroes then carry 0 there.
        private static List<Peer> LatestFullReport(out string source)
        {
            source = null;
            var peers = new List<Peer>();
            if (!Directory.Exists(ReportFolder)) return peers;

            FileInfo newest = new DirectoryInfo(ReportFolder).GetFiles(FullPrefix + "*.csv")
                .Where(f => !f.Name.StartsWith(PartialPrefix))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            if (newest == null) return peers;

            string[] lines = File.ReadAllLines(newest.FullName);
            if (lines.Length < 2) return peers;

            List<string> header = lines[0].TrimStart('\uFEFF').Split(',').ToList();
            int name = header.IndexOf("hero"), role = header.IndexOf("role");
            int dps = header.IndexOf("dps_30s"), aoe = header.IndexOf("aoe_dps_30s"), ttd = header.IndexOf("time_to_die");
            if (name < 0 || role < 0 || dps < 0 || ttd < 0) return peers;

            foreach (string line in lines.Skip(1))
            {
                string[] cells = line.Split(',');
                if (cells.Length < header.Count) continue;

                peers.Add(new Peer(cells[name].Trim('"'), cells[role],
                    Parse(cells[dps]), aoe >= 0 ? Parse(cells[aoe]) : 0f, Parse(cells[ttd])));
            }

            source = newest.Name;
            return peers;
        }

        private static float Parse(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;

        private static string F(float v) => v.ToString("0.0", CultureInfo.InvariantCulture);
        private static string Pct(float v) => (v * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        private static string Csv(string s) => string.IsNullOrEmpty(s) ? "" : s.Contains(",") ? "\"" + s + "\"" : s;
    }
}
