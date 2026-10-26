using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using MagicSchool.Contracts;

namespace MagicSchool.UI
{
    // Each row is one hero, each track one number (damage dealt, taken, healing given, taken), split into segments.
    // The longest track in the table gets the full width and the rest scale to it; a segment that does not count to
    // the total (overkill, overhealing) is drawn as a ghost tail inside the same 100%.
    // The table is a ScrollView, and a new round must start at the top: setting the scroll once is not enough, because
    // after the new rows are laid out the ScrollView re-applies its old position, so it is set again after that
    // layout.
    internal class ScoreboardController : PanelController, IScoreboardPanel
    {
        private static readonly Track Dealt = new Track("DAMAGE DEALT",
            new Segment("auto", "seg--auto", r => r.AutoAttackDamage),
            new Segment("skill", "seg--skill", r => r.SkillDamage),
            new Segment("overkill", "seg--ghost", r => r.Overkill, isGhost: true));

        private static readonly Track Taken = new Track("DAMAGE TAKEN",
            new Segment("taken", "seg--taken", r => r.DamageTaken),
            new Segment("blocked", "seg--ghost", r => r.DamageMitigated, isGhost: true));

        private static readonly Track HealingGiven = new Track("HEALING GIVEN",
            new Segment("healed", "seg--healed", r => r.HealingDone),
            new Segment("overheal", "seg--ghost", r => r.Overhealing, isGhost: true));

        private static readonly Track HealingTaken = new Track("HEALING TAKEN",
            new Segment("received", "seg--received", r => r.HealingReceived),
            new Segment("wound", "seg--ghost", r => r.HealingLostToWound, isGhost: true));

        private static readonly Chart[] Charts =
        {
            new Chart(Dealt, Taken, HealingGiven, HealingTaken),
        };

        private const float TooltipGap = 4f;
        private Label _tooltip;
        private VisualElement _tooltipOwner;

        private VisualElement _table;


        public void ShowScores(IReadOnlyList<ScoreRow> rows)
        {
            if (_table == null) return;

            int longest = LongestBar(rows);

            HideSegmentTooltip(_tooltipOwner);

            _table.Clear();
            _table.Add(BuildHeader());

            if (_table is ScrollView scroll)
            {
                scroll.scrollOffset = Vector2.zero;
                scroll.contentContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ => scroll.scrollOffset = Vector2.zero);
            }

            for (int i = 0; i < rows.Count; i++)
            {
                _table.Add(BuildRow(rows[i], longest, striped: i % 2 == 1));
            }

            SetShown(true);
        }

        protected override void OnMounted(VisualElement panel)
        {
            _table = panel.Q<VisualElement>("ScoreTable");
            _tooltip = NewTooltip(panel);

            SetShown(false);
        }

        private static int LongestBar(IReadOnlyList<ScoreRow> rows)
        {
            int longest = 1;

            foreach (Chart chart in Charts)
            {
                foreach (Track track in chart.Tracks)
                {
                    foreach (ScoreRow row in rows)
                    {
                        int total = 0;
                        foreach (Segment segment in track.Segments) total += ValueOf(row.Record, segment);

                        if (total > longest) longest = total;
                    }
                }
            }

            return longest;
        }

        private static VisualElement BuildHeader()
        {
            VisualElement header = NewRow();
            header.AddToClassList("row--header");
            header.Add(Cell("HERO", "cell--name", "cell--header"));

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

        private static VisualElement BuildTrackHead(Track track)
        {
            VisualElement head = new VisualElement();
            head.AddToClassList("track-head__block");
            head.pickingMode = PickingMode.Ignore;

            Label title = new Label(track.Header);
            title.AddToClassList("track-head__title");
            title.pickingMode = PickingMode.Ignore;
            head.Add(title);

            VisualElement legend = new VisualElement();
            legend.AddToClassList("track-head__legend");
            legend.pickingMode = PickingMode.Ignore;

            foreach (Segment segment in track.Segments) legend.Add(BuildLegend(segment));

            head.Add(legend);

            return head;
        }

        private static VisualElement BuildLegend(Segment segment)
        {
            VisualElement key = new VisualElement();
            key.AddToClassList("key");
            key.pickingMode = PickingMode.Ignore;

            VisualElement swatch = new VisualElement();
            swatch.AddToClassList("key__swatch");
            swatch.AddToClassList(segment.Class);
            swatch.pickingMode = PickingMode.Ignore;
            key.Add(swatch);

            Label name = new Label(segment.Name);
            name.AddToClassList("key__label");
            name.pickingMode = PickingMode.Ignore;
            key.Add(name);

            return key;
        }

        private VisualElement BuildRow(ScoreRow row, int longest, bool striped)
        {
            VisualElement line = NewRow();
            if (striped) line.AddToClassList("row--stripe");

            line.Add(HeroCell(row));

            for (int c = 0; c < Charts.Length; c++)
            {
                line.Add(BuildTracks(row.Record, Charts[c], longest));
            }

            return line;
        }

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

        private VisualElement BuildTrack(ICombatRecord record, Track track, int longest)
        {
            VisualElement line = new VisualElement();
            line.AddToClassList("track");
            line.pickingMode = PickingMode.Ignore;

            VisualElement rail = new VisualElement();
            rail.AddToClassList("track__rail");
            rail.pickingMode = PickingMode.Ignore;

            int total = 0;

            for (int i = 0; i < track.Segments.Length; i++)
            {
                Segment segment = track.Segments[i];
                int value = ValueOf(record, segment);

                if (!segment.IsGhost) total += value;
                if (value <= 0) continue;

                rail.Add(NewSegment(segment, value, longest));
            }

            line.Add(rail);
            line.Add(TrackValue(total));

            return line;
        }

        private VisualElement NewSegment(Segment segment, int value, int longest)
        {
            VisualElement track = new VisualElement();
            track.AddToClassList("seg");
            track.AddToClassList(segment.Class);

            track.style.width = Length.Percent(value * 100f / longest);

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

        private static VisualElement HeroCell(ScoreRow row)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList("cell");
            cell.AddToClassList("cell--name");
            cell.pickingMode = PickingMode.Ignore;

            VisualElement sprite = new VisualElement();
            sprite.AddToClassList("hero");
            sprite.pickingMode = PickingMode.Ignore;

            Label name = new Label(row.Name);
            name.AddToClassList("hero__name");
            name.pickingMode = PickingMode.Ignore;
            sprite.Add(name);

            sprite.AddToClassList(row.Team == TeamEnum.Blue ? "hero--blue" : "hero--red");
            if (!row.IsAlive) sprite.AddToClassList("hero--dead");

            cell.Add(sprite);

            return cell;
        }

        private static Label NewTooltip(VisualElement panel)
        {
            Label tooltip = new Label();
            tooltip.AddToClassList("seg-tooltip");
            tooltip.AddToClassList(HiddenClass);

            tooltip.pickingMode = PickingMode.Ignore;
            panel.Add(tooltip);

            return tooltip;
        }

        private void ShowSegmentTooltip(VisualElement track, Segment segment, int value)
        {
            if (_tooltip == null) return;

            _tooltipOwner = track;
            _tooltip.text = segment.Name + "  " + Format(value);

            Rect bounds = track.worldBound;
            Vector2 top = _tooltip.parent.WorldToLocal(new Vector2(bounds.center.x, bounds.yMin));
            _tooltip.style.left = top.x;
            _tooltip.style.top = top.y - TooltipGap;

            _tooltip.RemoveFromClassList(HiddenClass);
        }

        private void HideSegmentTooltip(VisualElement track)
        {
            if (_tooltip == null || _tooltipOwner != track) return;

            _tooltipOwner = null;
            _tooltip.AddToClassList(HiddenClass);
        }

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
            public readonly string Name;
            public readonly string Class;
            public readonly Func<ICombatRecord, int> Value;

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
