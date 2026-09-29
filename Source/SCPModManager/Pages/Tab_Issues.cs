// Tab_Issues.cs
// One place for everything that needs attention in the active mod list: load-order and dependency problems
// (with their resolvers), available updates (manifest version checks, source syncs and pending Workshop
// downloads), active mods that don't support the running game version, and missing mods.

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using static SCPModManager.Constants;
using static SCPModManager.Resources;

namespace SCPModManager;

public sealed class Tab_Issues : ManagerTab
{
    private const float CacheLifetime = 1.5f;
    private const float SectionHeight = 30f;

    private readonly List<Line> _lines = [];
    private float _cachedAt = -1f;
    private List<Dependency> _cachedIssues;
    private int _missingSteam, _outdatedCount, _problemCount, _updateCount;
    private List<ModButton_Installed> _outdated = [];
    private Vector2 _scroll = Vector2.zero;

    private enum LineType
    {
        Section,
        Mod,
        Issue,
        Update,
        WorkshopUpdate,
        Outdated,
        Missing,
        Empty
    }

    public override string Label => I18n.TabIssues;

    public override string Badge
    {
        get
        {
            // same set as the Problems section (version-check updates are listed under Updates instead)
            var count = ModButtonManager.Issues.Count(i => i.Severity >= 2 && i is not VersionCheck);
            return count > 0 ? count.ToString() : null;
        }
    }

    public override void OnOpened()
    {
        _cachedAt = -1f;
        WorkshopDetailsCache.RequestAllInstalled();
    }

    public override void DoContents(Rect canvas)
    {
        Recache();

        var toolbar = new Rect(canvas.xMin, canvas.yMin, canvas.width, ButtonHeight);
        DoToolbar(toolbar);

        var body = new Rect(canvas.xMin, toolbar.yMax + SmallMargin, canvas.width,
            canvas.yMax - toolbar.yMax - SmallMargin);
        DarkWidgets.Panel(body);
        DoLines(body.ContractedBy(1f));
    }

    private void DoToolbar(Rect rect)
    {
        var x = rect.xMin;
        if (DarkWidgets.ButtonAuto(ref x, rect.yMin, I18n.SortMods, true,
                ModButtonManager.ActiveButtons.Count > 1, I18n.AutoSortTip))
        {
            ModButtonManager.Sort();
            _cachedAt = -1f;
        }

        if (_missingSteam > 0 && WorkshopSearch.Available &&
            DarkWidgets.ButtonAuto(ref x, rect.yMin, I18n.ProfileSubscribeMissing(_missingSteam)))
        {
            Workshop.Subscribe(ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                .Where(b => b.SteamWorkshopId.IsValidSteamWorkshopIdentifier())
                .Select(b => b.SteamWorkshopId.ToString()));
        }

        if (_outdated.Any() &&
            DarkWidgets.ButtonAuto(ref x, rect.yMin, I18n.DeactivateOutdated(_outdated.Count), fg: DarkTheme.DotRed))
        {
            ConfirmDeactivateOutdated();
        }

        var summary = new Rect(x, rect.yMin, rect.xMax - x, rect.height);
        DarkWidgets.Label(summary, I18n.IssuesSummary(_problemCount, _updateCount, _outdatedCount),
            DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleRight, false);
    }

    private void ConfirmDeactivateOutdated()
    {
        var targets = _outdated.ToList();
        var names = targets.Select(b => b.TrimmedName).ToLineList();
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(I18n.ConfirmDeactivateOutdated(targets.Count, names),
            () =>
            {
                foreach (var button in targets)
                {
                    button.Active = false;
                }

                _cachedAt = -1f;
            }, true));
    }

    private void Recache()
    {
        var issues = ModButtonManager.Issues;
        if (issues == _cachedIssues && Time.realtimeSinceStartup - _cachedAt < CacheLifetime)
        {
            return;
        }

        _cachedIssues = issues;
        _cachedAt = Time.realtimeSinceStartup;
        _lines.Clear();

        // problems (severity >= 2), grouped per mod, worst first
        var problems = issues
            .Where(i => i.Severity >= 2 && i is not VersionCheck)
            .GroupBy(i => i.parent)
            .OrderByDescending(g => g.Max(i => i.Severity))
            .ThenBy(g => g.Key.Mod.Name)
            .ToList();
        _problemCount = problems.Sum(g => g.Count());
        _lines.Add(new Line(LineType.Section, I18n.IssuesProblems(_problemCount)));
        if (!problems.Any())
        {
            _lines.Add(new Line(LineType.Empty, I18n.IssuesNoProblems));
        }

        foreach (var group in problems)
        {
            _lines.Add(new Line(LineType.Mod, group.Key.Mod.Name) { Button = group.Key.Button });
            foreach (var issue in group.OrderByDescending(i => i.Severity))
            {
                _lines.Add(new Line(LineType.Issue, issue.Tooltip) { Button = group.Key.Button, Dependency = issue });
            }
        }

        // updates: manifest version checks / source syncs, plus Workshop items Steam says need an update
        var updates = new List<Line>();
        foreach (var button in ModButtonManager.ActiveButtons.OfType<ModButton_Installed>())
        {
            foreach (var requirement in button.Requirements.Where(r =>
                         r.IsApplicable && !r.IsSatisfied &&
                         (r is SourceSync || r is VersionCheck && r.Severity >= 2)))
            {
                updates.Add(new Line(LineType.Update, requirement.Tooltip) { Button = button, Dependency = requirement });
            }

            var mod = button.Selected;
            if (mod.Source == ContentSource.SteamWorkshop && WorkshopSearch.Available &&
                WorkshopDetailsCache.NeedsUpdate(mod))
            {
                updates.Add(new Line(LineType.WorkshopUpdate, I18n.WorkshopUpdatePending) { Button = button });
            }
        }

        _updateCount = updates.Count;
        _lines.Add(new Line(LineType.Section, I18n.IssuesUpdates(_updateCount)));
        if (!updates.Any())
        {
            _lines.Add(new Line(LineType.Empty, I18n.NoUpdatesAvailable));
        }

        _lines.AddRange(updates);

        // active mods that were not made for this game version
        _outdated = ModButtonManager.ActiveButtons.OfType<ModButton_Installed>()
            .Where(b => !b.Selected.VersionCompatible && !b.IsCoreMod)
            .OrderBy(b => b.TrimmedName)
            .ToList();
        _outdatedCount = _outdated.Count;
        _lines.Add(new Line(LineType.Section, I18n.IssuesOutdated(_outdatedCount)));
        if (!_outdated.Any())
        {
            _lines.Add(new Line(LineType.Empty, I18n.IssuesNoOutdated));
        }

        foreach (var button in _outdated)
        {
            _lines.Add(new Line(LineType.Outdated, ModCompatibility.For(button.Selected).StatusLabel) { Button = button });
        }

        // missing mods (from an imported list)
        var missing = ModButtonManager.ActiveButtons.OfType<ModButton_Missing>().ToList();
        _missingSteam = missing.Count(b => b.SteamWorkshopId.IsValidSteamWorkshopIdentifier());
        if (missing.Any())
        {
            _lines.Add(new Line(LineType.Section, I18n.IssuesMissing(missing.Count)));
            foreach (var button in missing)
            {
                _lines.Add(new Line(LineType.Missing, button.Identifier) { Button = button });
            }
        }
    }

    private void DoLines(Rect outRect)
    {
        var height = _lines.Sum(LineHeightFor);
        var viewRect = new Rect(outRect.xMin, outRect.yMin, outRect.width, height);
        if (viewRect.height > outRect.height)
        {
            viewRect.width -= 18f;
        }

        Widgets.BeginScrollView(outRect, ref _scroll, viewRect);
        var y = viewRect.yMin;
        var alternate = false;
        foreach (var line in _lines)
        {
            var lineHeight = LineHeightFor(line);
            var rect = new Rect(viewRect.xMin, y, viewRect.width, lineHeight);
            y += lineHeight;

            // cull, but keep the zebra pattern stable
            if (line.Type == LineType.Section)
            {
                alternate = false;
            }
            else
            {
                alternate = !alternate;
            }

            if (rect.yMax < _scroll.y + outRect.yMin || rect.yMin > _scroll.y + outRect.yMax)
            {
                continue;
            }

            DoLine(rect, line, alternate);
        }

        Widgets.EndScrollView();
    }

    private static float LineHeightFor(Line line)
    {
        return line.Type == LineType.Section ? SectionHeight : TableRowHeight;
    }

    private void DoLine(Rect rect, Line line, bool alternate)
    {
        if (line.Type == LineType.Section)
        {
            var labelRect = new Rect(rect.xMin + SmallMargin, rect.yMax - LabelHeight, rect.width, LabelHeight);
            DarkWidgets.Label(labelRect, line.Text.ToUpperInvariant(), DarkTheme.TextMuted, GameFont.Tiny,
                TextAnchor.MiddleLeft, false);
            DarkWidgets.HorizontalLine(rect.xMin, rect.yMax - 1f, rect.width);
            return;
        }

        if (line.Type == LineType.Empty)
        {
            DarkWidgets.Label(new Rect(rect.xMin + (SmallMargin * 3), rect.yMin, rect.width, rect.height), line.Text,
                DarkTheme.TextDisabled, GameFont.Tiny, TextAnchor.MiddleLeft, false);
            return;
        }

        Widgets.DrawBoxSolid(rect, alternate ? DarkTheme.PanelAlt : DarkTheme.Panel);
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        const float actionWidth = 96f;
        var actionRect = new Rect(rect.xMax - actionWidth - SmallMargin, rect.yMin + 1f, actionWidth,
            rect.height - 2f);
        var x = rect.xMin + SmallMargin;

        switch (line.Type)
        {
            case LineType.Mod:
            {
                var nameRect = new Rect(x, rect.yMin, actionRect.xMin - x - SmallMargin, rect.height);
                DarkWidgets.LabelTruncated(nameRect, line.Text, DarkTheme.TextPrimary);
                if (DarkWidgets.Button(actionRect, I18n.ActionShowInList))
                {
                    Page_BetterModConfig.Instance?.ShowMod(line.Button);
                }

                break;
            }
            case LineType.Issue:
            case LineType.Update:
            {
                x += SmallMargin * 3;
                var dependency = line.Dependency;
                var iconRect = new Rect(x, rect.yMin + ((rect.height - SmallIconSize) / 2f), SmallIconSize,
                    SmallIconSize);
                GUI.color = dependency.Severity >= 3 ? DarkTheme.DotRed :
                    dependency.Severity >= 2 ? line.Type == LineType.Update ? DarkTheme.Accent : DarkTheme.DotRed :
                    DarkTheme.DotYellow;
                GUI.DrawTexture(iconRect, dependency.StatusIcon);
                GUI.color = Color.white;
                x = iconRect.xMax + SmallMargin;

                var text = line.Type == LineType.Update ? $"{line.Button.TrimmedName}: {line.Text}" : line.Text;
                DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, actionRect.xMin - x - SmallMargin, rect.height),
                    text.CapitalizeFirst(), DarkTheme.TextPrimary, GameFont.Tiny);

                if (!dependency.Resolvers.NullOrEmpty())
                {
                    if (DarkWidgets.PrimaryButton(actionRect,
                            line.Type == LineType.Update ? I18n.ActionUpdate : I18n.ActionFix))
                    {
                        dependency.OnClicked(null);
                        _cachedAt = -1f;
                    }
                }
                else if (DarkWidgets.Button(actionRect, I18n.ActionShowInList))
                {
                    Page_BetterModConfig.Instance?.ShowMod(line.Button);
                }

                break;
            }
            case LineType.WorkshopUpdate:
            {
                var iconRect = new Rect(x, rect.yMin + ((rect.height - SmallIconSize) / 2f), SmallIconSize,
                    SmallIconSize);
                GUI.color = DarkTheme.Accent;
                GUI.DrawTexture(iconRect, Steam);
                GUI.color = Color.white;
                x = iconRect.xMax + SmallMargin;
                DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, actionRect.xMin - x - SmallMargin, rect.height),
                    $"{line.Button.TrimmedName}: {line.Text}", DarkTheme.TextPrimary, GameFont.Tiny);
                if (DarkWidgets.Button(actionRect, I18n.ActionWorkshop) && line.Button is ModButton_Installed installed)
                {
                    SteamUtility.OpenWorkshopPage(installed.Selected.GetPublishedFileId());
                }

                break;
            }
            case LineType.Outdated:
            {
                var installed = (ModButton_Installed)line.Button;
                var compat = ModCompatibility.For(installed.Selected);
                var dotRect = new Rect(x, rect.yMin + ((rect.height - DotSize) / 2f), DotSize, DotSize);
                DrawDot(dotRect, compat.StatusColor);
                x = dotRect.xMax + SmallMargin;
                var nameWidth = (actionRect.xMin - x) * 0.55f;
                DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, nameWidth, rect.height), installed.TrimmedName,
                    DarkTheme.TextPrimary);
                x += nameWidth + SmallMargin;
                var statusRect = new Rect(x, rect.yMin, actionRect.xMin - x - SmallMargin, rect.height);
                DarkWidgets.LabelTruncated(statusRect, line.Text, compat.StatusColor, GameFont.Tiny);
                TooltipHandler.TipRegion(statusRect, compat.StatusTip);

                var deactivateRect = new Rect(actionRect.xMin - actionWidth - SmallMargin, actionRect.yMin,
                    actionWidth, actionRect.height);
                if (DarkWidgets.Button(deactivateRect, I18n.ActionDeactivate, DarkTheme.PanelAlt, DarkTheme.DotRed))
                {
                    installed.Active = false;
                    _cachedAt = -1f;
                }

                if (DarkWidgets.Button(actionRect, I18n.ActionShowInList))
                {
                    Page_BetterModConfig.Instance?.ShowMod(installed);
                }

                break;
            }
            case LineType.Missing:
            {
                var missing = (ModButton_Missing)line.Button;
                var dotRect = new Rect(x, rect.yMin + ((rect.height - DotSize) / 2f), DotSize, DotSize);
                DrawDot(dotRect, DarkTheme.DotRed);
                x = dotRect.xMax + SmallMargin;
                DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, actionRect.xMin - x - SmallMargin, rect.height),
                    $"{missing.Name}  ({missing.Identifier})", DarkTheme.TextPrimary, GameFont.Tiny);
                if (missing.SteamWorkshopId.IsValidSteamWorkshopIdentifier() && WorkshopSearch.Available)
                {
                    if (DarkWidgets.PrimaryButton(actionRect, I18n.ActionSubscribe))
                    {
                        Workshop.Subscribe(missing.SteamWorkshopId.ToString());
                    }
                }
                else if (DarkWidgets.Button(actionRect, I18n.ActionFind))
                {
                    Page_BetterModConfig.Instance?.OpenWorkshopSearch(missing.Name);
                }

                break;
            }
        }
    }

    private sealed class Line(LineType type, string text)
    {
        public readonly string Text = text;
        public readonly LineType Type = type;
        public ModButton Button;
        public Dependency Dependency;
    }
}
