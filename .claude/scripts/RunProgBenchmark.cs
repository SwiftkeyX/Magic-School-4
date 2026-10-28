using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// For Coplay execute_script: selects the prog comps in Data/BattleSetups/Team Comps and starts the Team Comp Benchmark
// on them (Run Selected). A prog comp is one whose name ends in "-N" (Elf-Ettin-ShieldKnight-5, prog-Skeleton-5).
// To run only some, list them in Only (e.g. { "Elf-Ettin-ShieldKnight-5", "prog-Skeleton-5" }); empty = every prog comp.
// It enters Play mode itself; the report lands in BalanceLogs/teamcomp-*.csv and is imported when Play ends.
public static class RunProgBenchmark
{
    private static readonly string[] Only = { };

    public static string Execute()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return "In Play mode - stop it first.";

        var comps = AssetDatabase.FindAssets("t:BattlePlacementSO", new[] { "Assets/Data/BattleSetups/Team Comps" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(c => c != null && Regex.IsMatch(c.name, @"-\d+$") && (Only.Length == 0 || Only.Contains(c.name)))
            .OrderBy(c => c.name)
            .ToArray();
        if (comps.Length == 0) return "No prog comp found.";

        Selection.objects = comps.Cast<UnityEngine.Object>().ToArray();
        Type menu = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .First(t => t.Name == "TeamCompBenchmarkMenu");
        menu.GetMethod("RunSelected", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, null);
        return $"Started on {comps.Length} comps: {string.Join(", ", comps.Select(c => c.name))}";
    }
}
