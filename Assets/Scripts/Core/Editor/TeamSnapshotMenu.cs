using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using MagicSchool.Combat.Align;
using MagicSchool.Combat.Heroes;
using MagicSchool.Combat.Placements;
using MagicSchool.Contracts;
using MagicSchool.Core;
using MagicSchool.DevTools;
using MagicSchool.Items;

namespace MagicSchool.Editor
{
    /// <summary>
    /// A screenshot of each selected team comp as it stands when combat starts: its milestone stage's enemies, the comp
    /// on the real board, and every one of its heroes' marks lit at once (in play only the hovered hero's show).
    /// Saved to BalanceLogs/teams/&lt;comp&gt;-game.png. A comp named "...-N" is shown at stage N, any other at stage 1.
    /// </summary>
    // Runs in Play mode: from Edit mode it enters Play, waits for Preparation, then takes the comps one by one on
    // EditorApplication.update (load stage, load team, light marks, render the main camera), and leaves Play at the end.
    // Wearing hexes are left out: nearly every mark hex is one, and their purple ring would hide the bonus colours.
    internal static class TeamSnapshotMenu
    {
        private const string PendingKey = "MagicSchool.TeamSnapshot.Pending";
        private const string OutFolder = "BalanceLogs/teams";
        private const string StageFolder = "Assets/Data/BattleSetups/Campaign";
        private const int Width = 1600, Height = 900;

        private static readonly Queue<string> Comps = new Queue<string>();
        private static int _step, _wait;
        private static BattlePlacementSO _comp;
        private static readonly List<string> Saved = new List<string>();

        [MenuItem("Magic School 4/Team Snapshot (selected comps)")]
        private static void Run()
        {
            string[] guids = Selection.objects.OfType<BattlePlacementSO>()
                .Select(c => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(c))).ToArray();
            if (guids.Length == 0)
            {
                Debug.LogWarning("[TeamSnapshot] select one or more team comps in the Project window first.");
                return;
            }

            SessionState.SetString(PendingKey, string.Join(";", guids));
            if (EditorApplication.isPlaying) Begin();
            else EditorApplication.EnterPlaymode();
        }

        [MenuItem("Magic School 4/Team Snapshot (selected comps)", true)]
        private static bool CanRun() => Selection.objects.OfType<BattlePlacementSO>().Any();

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(PendingKey, "").Length > 0) Begin();
            };
        }

        private static void Begin()
        {
            Comps.Clear();
            foreach (string guid in SessionState.GetString(PendingKey, "").Split(';').Where(g => g.Length > 0))
                Comps.Enqueue(guid);
            SessionState.EraseString(PendingKey);
            Saved.Clear();
            _step = 0;
            _wait = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }

            GameManager game = GameManager.Instance;
            StageEditor editor = StageEditor.Current;
            if (game == null || editor == null || game.Phase != GamePhaseEnum.Preparation) return;
            if (_wait-- > 0) return;

            switch (_step)
            {
                case 0:     // next comp: its milestone stage
                    if (Comps.Count == 0)
                    {
                        EditorApplication.update -= Tick;
                        Debug.Log($"[TeamSnapshot] saved {Saved.Count}: {string.Join(", ", Saved)}");
                        EditorApplication.ExitPlaymode();
                        return;
                    }
                    _comp = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>(AssetDatabase.GUIDToAssetPath(Comps.Dequeue()));
                    if (_comp == null) return;
                    Match milestone = Regex.Match(_comp.name, @"-(\d+)$");
                    int stage = milestone.Success ? int.Parse(milestone.Groups[1].Value) : 1;
                    var setup = AssetDatabase.LoadAssetAtPath<BattlePlacementSO>($"{StageFolder}/Stage {stage:00}.asset");
                    if (setup != null) editor.LoadStage(setup);
                    else Debug.LogWarning($"[TeamSnapshot] no Stage {stage:00} for {_comp.name}; kept the current stage.");
                    Next(3);
                    break;

                case 1:     // the comp on the board
                    editor.LoadPlayerTeam(_comp);
                    Next(3);
                    break;

                case 2:     // no loose item left over from the comps before: keep only what is worn or lies on a hex
                    RemoveLooseItems(Object.FindAnyObjectByType<BattleBoard>());
                    Next(1);
                    break;

                case 3:     // every hero's marks at once
                    LightAllMarks(Object.FindAnyObjectByType<BattleBoard>());
                    Next(2);
                    break;

                case 4:
                    Saved.Add(Capture(_comp.name, Object.FindAnyObjectByType<BattleBoard>()));
                    _step = 0;
                    break;
            }
        }

        private static void Next(int frames)
        {
            _step++;
            _wait = frames;
        }

        private static void LightAllMarks(BattleBoard board)
        {
            var onBoard = new Dictionary<Hex, MarkColours>();
            var paying = new HashSet<Hex>();
            var offBoard = new Dictionary<Vector2Int, (Vector3 position, MarkColours marks)>();

            foreach (Hero hero in board.HeroesOnBoard.OfType<Hero>().Where(h => h != null && h.Team == TeamEnum.Blue && h.CurrentHex != null))
            {
                foreach (AlignShape.ShownHex shown in AlignShape.ShownHexes(hero, hero.CurrentHex, board.Hexs.Values))
                {
                    if (shown.IsOffBoard)
                    {
                        var spot = new Vector2Int(Mathf.RoundToInt(shown.position.x * 4f), Mathf.RoundToInt(shown.position.y * 4f));
                        offBoard.TryGetValue(spot, out var ghost);
                        offBoard[spot] = (shown.position, With(ghost.marks, shown, true));
                        continue;
                    }

                    onBoard.TryGetValue(shown.hex, out MarkColours marks);
                    onBoard[shown.hex] = With(marks, shown, false);
                    if (shown.hex.IsInDeployZone && !shown.isPastEdge) paying.Add(shown.hex);
                }
            }

            foreach (Hex hex in onBoard.Keys.ToList())
            {
                MarkColours marks = onBoard[hex];
                marks.givesNothing = !paying.Contains(hex);
                onBoard[hex] = marks;
            }

            board.Colours.ShowAlignMarks(onBoard, offBoard.Values.ToList());
        }

        private static void RemoveLooseItems(BattleBoard board)
        {
            if (board == null) return;
            var keep = new HashSet<IEquipment>(board.Items.All().Select(entry => entry.item));
            foreach (Hero hero in board.HeroesOnBoard.OfType<Hero>().Where(h => h != null))
                keep.UnionWith(hero.WornItems);

            foreach (Item item in Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
            {
                if (!keep.Contains(item)) Object.Destroy(item.gameObject);
            }
        }

        private static MarkColours With(MarkColours marks, AlignShape.ShownHex shown, bool givesNothing)
        {
            if (shown.colour == AligningHex.Orange) marks.first = true;
            if (shown.colour == AligningHex.Green) marks.second = true;
            if (shown.colour == AligningHex.Special) marks.special = true;
            marks.givesNothing = givesNothing;
            return marks;
        }

        private static string Capture(string name, BattleBoard board)
        {
            Camera camera = Camera.main;
            var texture = new RenderTexture(Width, Height, 24);
            RenderTexture previousTarget = camera.targetTexture, previousActive = RenderTexture.active;

            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();

            // cut to the board: the bench and the loose-item column around it are not the team
            Rect crop = BoardRect(camera, board);
            if (crop.width > 0)
            {
                var cut = new Texture2D((int)crop.width, (int)crop.height, TextureFormat.RGB24, false);
                cut.SetPixels(image.GetPixels((int)crop.x, (int)crop.y, (int)crop.width, (int)crop.height));
                cut.Apply();
                Object.Destroy(image);
                image = cut;
            }

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.Destroy(texture);

            Directory.CreateDirectory(OutFolder);
            string path = Path.Combine(OutFolder, name + "-game.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
            return path;
        }

        // the board's hexes in the render's pixels, with a margin for the health bars above the top row
        private static Rect BoardRect(Camera camera, BattleBoard board)
        {
            if (board == null) return Rect.zero;
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (Hex hex in board.Hexs.Values)
            {
                foreach (Renderer r in hex.GetComponentsInChildren<Renderer>())
                {
                    foreach (Vector3 corner in new[] { r.bounds.min, r.bounds.max })
                    {
                        Vector3 p = camera.WorldToViewportPoint(corner);
                        xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
                        yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
                    }
                }
            }
            if (xMin > xMax) return Rect.zero;

            const int margin = 40;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(xMin * Width) - margin, 0, Width - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(yMin * Height) - margin, 0, Height - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(xMax * Width) + margin, x0 + 1, Width);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(yMax * Height) + margin, y0 + 1, Height);
            return new Rect(x0, y0, x1 - x0, y1 - y0);
        }
    }
}
