using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    internal class ScoreboardController : PanelController, IScoreboardPanel
    {
        // ================================= Track var =================================
        // each track represent one of the hero performance 
        // e.g. damage dealt, damage taken, etc...

        // === damage dealt ===
        private static readonly Track Dealt = new Track("DAMAGE DEALT",
            new Segment("auto", "seg--auto", r => r.AutoAttackDamage),
            new Segment("skill", "seg--skill", r => r.SkillDamage),
            new Segment("overkill", "seg--ghost", r => r.Overkill, isGhost: true));

        // === damage taken ===
        private static readonly Track Taken = new Track("DAMAGE TAKEN",
            new Segment("taken", "seg--taken", r => r.DamageTaken),
            new Segment("blocked", "seg--ghost", r => r.DamageMitigated, isGhost: true));

        // === healing given ===
        private static readonly Track HealingGiven = new Track("HEALING GIVEN",
            new Segment("healed", "seg--healed", r => r.HealingDone),
            new Segment("overheal", "seg--ghost", r => r.Overhealing, isGhost: true));

        // === healing taken ===
        private static readonly Track HealingTaken = new Track("HEALING TAKEN",
            new Segment("received", "seg--received", r => r.HealingReceived),
            new Segment("wound", "seg--ghost", r => r.HealingLostToWound, isGhost: true));

        // ================================= Chart var =================================
        // Chart contain several track, 1 chart represent 1 hero
        // e.g. Vharn's Chart = {do 100 dmg, take 300 dmg, etc...}

        // charts contain all chart that'll be shown on the scoreboard.
        private static readonly Chart[] Charts =
        {
            new Chart(Dealt, Taken, HealingGiven, HealingTaken),
        };

        // ================================= tooltip var =================================
        private const float TooltipGap = 4f;
        private Label _tooltip;
        private VisualElement _tooltipOwner;

        // ================================= other var =================================
        // the entire table that represent this scoreboard
        private VisualElement _table;



        // =================================== IScoreboardPanel ===================================
        public void ShowScores(IReadOnlyList<ScoreRow> rows)
        {
            if (_table == null) return;

            int longest = LongestBar(rows);

            HideSegmentTooltip(_tooltipOwner);

            _table.Clear();
            _table.Add(BuildHeader());

            for (int i = 0; i < rows.Count; i++)
            {
                _table.Add(BuildRow(rows[i], longest, striped: i % 2 == 1));
            }

            SetShown(true);
        }

        // =================================== Life cycle ===================================
        protected override void OnMounted(VisualElement panel)
        {
            _table = panel.Q<VisualElement>("ScoreTable");
            _tooltip = NewTooltip(panel);

            // nothing to show until a round has actually been fought
            SetShown(false);
        }

        // =================================== track width scale ===================================
        // context: the longest track in the whole table gets the full width. Other shorter track will adjust accordingly to the longest one.
        // e.g. track's width = (value * 100f) / (longest track in the table)

        // Find across all charts, how much width is the longest
        private static int LongestBar(IReadOnlyList<ScoreRow> rows)
        {
            int longest = 1;

            foreach (Chart chart in Charts)
            {
                foreach (Track track in chart.Tracks)
                {
                    foreach (ScoreRow row in rows)
                    {
                        // the ghost tail is drawn too, so it has to fit inside the same 100%
                        int total = 0;
                        foreach (Segment segment in track.Segments) total += ValueOf(row.Record, segment);

                        if (total > longest) longest = total;
                    }
                }
            }

            return longest;
        }

        // =================================== building ===================================
        // ====== 1. header ======
        // each header contain title and legend 
        // e.g. header1 = DAMAGE DEALT | ■ auto  ■ skill  ■ overkill
        // e.g. header2 = DAMAGE TAKEN | ■ taken  ■ blocked

        // add header to the scoreboard
        private static VisualElement BuildHeader()
        {
            VisualElement header = NewRow();
            header.AddToClassList("row--header");
            header.Add(Cell("HERO", "cell--name", "cell--header"));

            foreach (Chart chart in Charts) header.Add(BuildColumnHead(chart));

            return header;
        }

        private static VisualElement BuildColumnHead(Chart column)
        {
            VisualElement head = new VisualElement();
            head.AddToClassList("cell");
            head.AddToClassList("column");
            head.AddToClassList("track-head");
            head.pickingMode = PickingMode.Ignore;

            foreach (Track track in column.Tracks) head.Add(BuildTrackHead(track));

            return head;
        }

        // add title and legend
        private static VisualElement BuildTrackHead(Track track)
        {
            VisualElement head = new VisualElement();
            head.AddToClassList("track-head__block");
            head.pickingMode = PickingMode.Ignore;

            // title = DAMAGE DEALT, DAMAGE TAKEN, etc...
            Label title = new Label(track.Header);
            title.AddToClassList("track-head__title");
            title.pickingMode = PickingMode.Ignore;
            head.Add(title);

            // legend = ■ auto  ■ skill  ■ overkill
            VisualElement legend = new VisualElement();
            legend.AddToClassList("track-head__legend");
            legend.pickingMode = PickingMode.Ignore;

            foreach (Segment segment in track.Segments) legend.Add(BuildLegend(segment));

            head.Add(legend);

            return head;
        }

        // build a legend to look like this => ■ auto
        private static VisualElement BuildLegend(Segment segment)
        {
            VisualElement key = new VisualElement();
            key.AddToClassList("key");
            key.pickingMode = PickingMode.Ignore;

            // add class for ■
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList("key__swatch");
            swatch.AddToClassList(segment.Class);
            swatch.pickingMode = PickingMode.Ignore;
            key.Add(swatch);

            // add name e.g. auto, skill, overkill, etc...
            Label name = new Label(segment.Name);
            name.AddToClassList("key__label");
            name.pickingMode = PickingMode.Ignore;
            key.Add(name);

            return key;
        }

        // ====== 2. row ======
        // each row consist of hero's sprite and its chart
        // e.g. sprite | [damage dealt | damage taken | etc...]
        private VisualElement BuildRow(ScoreRow row, int longest, bool striped)
        {
            VisualElement line = NewRow();
            if (striped) line.AddToClassList("row--stripe");

            // add hero sprite to this line
            line.Add(HeroCell(row));

            // add track to this line
            for (int c = 0; c < Charts.Length; c++)
            {
                line.Add(BuildTracks(row.Record, Charts[c], longest));
            }

            return line;
        }

        // add all the tracks
        private VisualElement BuildTracks(ICombatRecord record, Chart column, int longest)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList("cell");
            cell.AddToClassList("column");
            cell.pickingMode = PickingMode.Ignore;

            for (int t = 0; t < column.Tracks.Length; t++)
            {
                cell.Add(BuildTrack(record, column.Tracks[t], longest));
            }

            return cell;
        }

        // add one track consist of segments, and the total
        // e.g. dmg dealt = auto-attack segment | skill segment | total dmg
        private VisualElement BuildTrack(ICombatRecord record, Track track, int longest)
        {
            VisualElement line = new VisualElement();
            line.AddToClassList("track");
            line.pickingMode = PickingMode.Ignore;

            // add a visual element for track 
            VisualElement rail = new VisualElement();
            rail.AddToClassList("track__rail");
            rail.pickingMode = PickingMode.Ignore;

            int total = 0;

            // a actual track added here
            for (int i = 0; i < track.Segments.Length; i++)
            {
                Segment segment = track.Segments[i];
                int value = ValueOf(record, segment);

                if (!segment.IsGhost) total += value;
                if (value <= 0) continue;

                // add each segment additively - combine into 1 track
                rail.Add(NewSegment(segment, value, longest));
            }

            line.Add(rail);
            line.Add(TrackValue(total));

            return line;
        }

        // a actual chart added here
        // a whole track's width was scaling accordingly to the longest track in the table.
        private VisualElement NewSegment(Segment segment, int value, int longest)
        {
            VisualElement track = new VisualElement();
            track.AddToClassList("seg");
            track.AddToClassList(segment.Class);

            // the whole track is measured against the longest track in the table
            track.style.width = Length.Percent(value * 100f / longest);

            // add event: the segment could be hovering on
            track.pickingMode = PickingMode.Position;
            track.RegisterCallback<PointerEnterEvent>(_ =>
            {
                track.AddToClassList("seg--hover");
                ShowSegmentTooltip(track, segment, value);
            });
            track.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                track.RemoveFromClassList("seg--hover");
                HideSegmentTooltip(track);
            });

            return track;
        }

        private static Label TrackValue(int total)
        {
            Label value = new Label(Format(total));
            value.AddToClassList("track__value");
            value.pickingMode = PickingMode.Ignore;

            if (total == 0) value.AddToClassList("track__value--zero");

            return value;
        }

        // add visual element that contain hero sprite
        private static VisualElement HeroCell(ScoreRow row)
        {
            // create cell container
            VisualElement cell = new VisualElement();
            cell.AddToClassList("cell");
            cell.AddToClassList("cell--name");
            cell.pickingMode = PickingMode.Ignore;

            // create hero's sprite (now sprite is the placeholder)
            VisualElement sprite = new VisualElement();
            sprite.AddToClassList("hero");
            sprite.pickingMode = PickingMode.Ignore;

            // put hero name into the sprite (placeholder)
            Label name = new Label(row.Name);
            name.AddToClassList("hero__name");
            name.pickingMode = PickingMode.Ignore;
            sprite.Add(name);

            // separate team by colour
            sprite.AddToClassList(row.Team == TeamEnum.Blue ? "hero--blue" : "hero--red");
            if (!row.IsAlive) sprite.AddToClassList("hero--dead");

            // add hero's sprite to a cell container
            cell.Add(sprite);

            return cell;
        }

        // =================================== tooltip ===================================
        // this tooltip show how much this segment does in number

        // initialize a tooltip for segment
        // e.g. auto-attack = 250 damage, skill = 400 damage
        private static Label NewTooltip(VisualElement panel)
        {
            Label tooltip = new Label();
            tooltip.AddToClassList("seg-tooltip");
            tooltip.AddToClassList(HiddenClass);

            tooltip.pickingMode = PickingMode.Ignore;
            panel.Add(tooltip);

            return tooltip;
        }

        // show tooltip on top of the segment
        private void ShowSegmentTooltip(VisualElement track, Segment segment, int value)
        {
            if (_tooltip == null) return;

            _tooltipOwner = track;
            _tooltip.text = segment.Name + "  " + Format(value);

            // move tooltip to on top of segment
            Rect bounds = track.worldBound;
            Vector2 top = _tooltip.parent.WorldToLocal(new Vector2(bounds.center.x, bounds.yMin));
            _tooltip.style.left = top.x;
            _tooltip.style.top = top.y - TooltipGap;

            // show tooltip
            _tooltip.RemoveFromClassList(HiddenClass);
        }

        // hide tooltip
        private void HideSegmentTooltip(VisualElement track)
        {
            if (_tooltip == null || _tooltipOwner != track) return;

            _tooltipOwner = null;
            _tooltip.AddToClassList(HiddenClass);
        }

        // =================================== helper ===================================
        private static int ValueOf(ICombatRecord record, Segment segment)
        {
            return record == null ? 0 : segment.Value(record);
        }

        private static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private static VisualElement NewRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("row");
            row.pickingMode = PickingMode.Ignore;

            return row;
        }

        private static Label Cell(string text, params string[] extraClasses)
        {
            Label cell = new Label(text);
            cell.AddToClassList("cell");
            cell.pickingMode = PickingMode.Ignore;

            foreach (string extra in extraClasses) cell.AddToClassList(extra);

            return cell;
        }

        // One coloured piece inside a track, each segment represent different value of that track.
        // e.g. damage dealt track can have 2 segment: 1) auto-attack, 2) skill
        private readonly struct Segment
        {
            public readonly string Name;                     // what the legend and the tooltip call it
            public readonly string Class;                    // its colour, from Scoreboard.uss
            public readonly Func<ICombatRecord, int> Value;

            // the segment part that did not count to total 
            // e.g. dmg dealt => overkill doesn't count to total dmg.
            // e.g. heal given => overhealed doesn't count to total heal. 
            public readonly bool IsGhost;                    

            public Segment(string name, string cssClass, Func<ICombatRecord, int> value, bool isGhost = false)
            {
                Name = name;
                Class = cssClass;
                Value = value;
                IsGhost = isGhost;
            }
        }

        // Track represent a number visually using UI bar 
        // e.g. damage dealt bar, damage taken bar
        private readonly struct Track
        {
            public readonly string Header;
            public readonly Segment[] Segments;

            public Track(string header, params Segment[] segments)
            {
                Header = header;
                Segments = segments;
            }
        }

        // each hero have 1 Chart
        // Chart consist of all several track that represent hero performance
        private readonly struct Chart
        {
            public readonly Track[] Tracks;
            public Chart(params Track[] tracks) => Tracks = tracks;
        }
    }
}
