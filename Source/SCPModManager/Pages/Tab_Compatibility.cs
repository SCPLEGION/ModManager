// Tab_Compatibility.cs
// Compatibility matrix for every installed mod: declared game versions (About.xml), version folders on disk,
// Workshop tags (can run ahead of the local copy), mod version, last Workshop update and active issues.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using static SCPModManager.Constants;
using static SCPModManager.Resources;
using Version = System.Version;

namespace SCPModManager;

public sealed class Tab_Compatibility : ManagerTab
{
    private const string SearchControl = "SCPModManager_CompatSearch";
    private const float VersionColumnWidth = 34f;
    private const float SourceColumnWidth = 26f;
    private const float ModVersionColumnWidth = 76f;
    private const float UpdatedColumnWidth = 82f;
    private const float StatusColumnWidth = 118f;
    private const float IssuesColumnWidth = 44f;

    private readonly List<ModButton_Installed> _rows = [];
    private int _allButtonsCount = -1;
    private int _workshopCount = -1;
    private int _compatible, _outdated, _newer, _pending;
    private bool _descending;
    private string _filter = string.Empty;
    private bool _dirty = true;
    private Scope _scope = Scope.All;
    private Vector2 _scroll = Vector2.zero;
    private SortColumn _sort = SortColumn.Status;

    private enum Scope
    {
        All,
        Active,
        Inactive
    }

    private enum SortColumn
    {
        Name,
        Source,
        Status,
        Updated,
        Issues
    }

    public override string Label => I18n.TabCompatibility;

    public override string Tooltip => I18n.TabCompatibilityTip;

    public override void OnOpened()
    {
        WorkshopDetailsCache.RequestAllInstalled();
        _dirty = true;
    }

    public override void FocusSearch()
    {
        GUI.FocusControl(SearchControl);
    }

    public override void DoContents(Rect canvas)
    {
        // re-evaluate when mods are added/removed or more Workshop data (which can change a status) arrived
        if (_allButtonsCount != ModButtonManager.AllButtons.Count ||
            _workshopCount != WorkshopDetailsCache.CachedCount)
        {
            _dirty = true;
        }

        if (_dirty)
        {
            Rebuild();
        }

        var toolbar = new Rect(canvas.xMin, canvas.yMin, canvas.width, ButtonHeight);
        DoToolbar(toolbar);

        var summary = new Rect(canvas.xMin, toolbar.yMax + SmallMargin, canvas.width, LineHeight);
        DoSummary(summary);

        var table = new Rect(canvas.xMin, summary.yMax + SmallMargin, canvas.width,
            canvas.yMax - summary.yMax - SmallMargin);
        DoTable(table);
    }

    private void DoToolbar(Rect rect)
    {
        var x = rect.xMax;
        if (DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.CompatExport, tip: I18n.CompatExportTip))
        {
            GUIUtility.systemCopyBuffer = ExportCsv();
            Messages.Message(I18n.CompatExported(_rows.Count), MessageTypeDefOf.SilentInput, false);
        }

        if (WorkshopDetailsCache.Available &&
            DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.CompatRefreshWorkshop, enabled: !WorkshopDetailsCache.Busy,
                tip: I18n.CompatRefreshWorkshopTip))
        {
            WorkshopDetailsCache.Invalidate();
            ModCompatibility.Notify_Refresh();
            WorkshopDetailsCache.RequestAllInstalled();
            _dirty = true;
        }

        // scope toggles, laid out right-to-left before the search field
        var scopes = new[] { (Scope.Inactive, I18n.ScopeInactive), (Scope.Active, I18n.ScopeActive), (Scope.All, I18n.ScopeAll) };
        foreach (var (scope, label) in scopes)
        {
            var width = DarkWidgets.ChipWidth(label) + SmallMargin;
            x -= width;
            var chipX = x;
            if (DarkWidgets.ToggleChip(ref chipX, rect.yMin + 2f, label, _scope == scope))
            {
                _scope = scope;
                _dirty = true;
            }

            x -= SmallMargin / 2f;
        }

        var searchRect = new Rect(rect.xMin, rect.yMin, x - rect.xMin - SmallMargin, rect.height);
        if (DarkWidgets.SearchField(searchRect, ref _filter, SearchControl, I18n.SearchPlaceholder, I18n.SearchHelp))
        {
            _dirty = true;
        }
    }

    private void DoSummary(Rect rect)
    {
        var x = rect.xMin;
        var y = rect.yMin + ((rect.height - ChipHeight) / 2f);
        DarkWidgets.Chip(ref x, y, I18n.CompatGameVersion(VersionControl.CurrentVersionStringWithoutBuild),
            DarkTheme.TextPrimary);
        if (SummaryChip(ref x, y, I18n.CompatCountCompatible(_compatible), DarkTheme.DotGreen, "is:compatible"))
        {
            SetFilter("is:compatible");
        }

        if (SummaryChip(ref x, y, I18n.CompatCountOutdated(_outdated), DarkTheme.DotRed, "is:outdated"))
        {
            SetFilter("is:outdated");
        }

        if (_pending > 0 && SummaryChip(ref x, y, I18n.CompatCountPending(_pending), DarkTheme.Accent, null))
        {
            _sort = SortColumn.Status;
            _descending = false;
            _dirty = true;
        }

        if (_newer > 0 && SummaryChip(ref x, y, I18n.CompatCountNewer(_newer), DarkTheme.DotYellow, "is:newer"))
        {
            SetFilter("is:newer");
        }

        if (WorkshopDetailsCache.Available)
        {
            var text = WorkshopDetailsCache.Busy
                ? I18n.CompatWorkshopLoading(WorkshopDetailsCache.CachedCount, WorkshopDetailsCache.RequestedCount)
                : I18n.CompatWorkshopLoaded(WorkshopDetailsCache.CachedCount);
            DarkWidgets.Label(new Rect(x + SmallMargin, rect.yMin, rect.xMax - x - SmallMargin, rect.height), text,
                DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleRight, false);
        }
    }

    private static bool SummaryChip(ref float x, float y, string text, Color color, string filter)
    {
        var rect = DarkWidgets.Chip(ref x, y, text, color, filter == null ? null : I18n.CompatFilterBy(filter));
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        return Widgets.ButtonInvisible(rect);
    }

    private void SetFilter(string filter)
    {
        _filter = _filter == filter ? string.Empty : filter;
        _dirty = true;
    }

    private void Rebuild()
    {
        _dirty = false;
        _allButtonsCount = ModButtonManager.AllButtons.Count;
        _workshopCount = WorkshopDetailsCache.CachedCount;

        var query = ModSearchQuery.For(_filter);
        IEnumerable<ModButton_Installed> buttons = ModButtonManager.AllButtons.OfType<ModButton_Installed>();
        buttons = _scope switch
        {
            Scope.Active => buttons.Where(b => b.Active),
            Scope.Inactive => buttons.Where(b => !b.Active),
            _ => buttons
        };

        var all = buttons.ToList();
        _compatible = _outdated = _newer = _pending = 0;
        foreach (var button in all)
        {
            switch (ModCompatibility.For(button.Selected).Status)
            {
                case CompatStatus.Compatible:
                    _compatible++;
                    break;
                case CompatStatus.UpdatePending:
                    _pending++;
                    break;
                case CompatStatus.NewerOnly:
                    _newer++;
                    break;
                default:
                    _outdated++;
                    break;
            }
        }

        _rows.Clear();
        _rows.AddRange(query.IsEmpty ? all : all.Where(b => query.Rank(b) > 0));
        Sort();
    }

    private void Sort()
    {
        Func<ModButton_Installed, IComparable> key = _sort switch
        {
            SortColumn.Source => b => (int)b.Selected.Source,
            SortColumn.Updated => b => ModCompatibility.For(b.Selected).Workshop?.Updated ?? DateTime.MinValue,
            SortColumn.Issues => b => b.Active ? b.Requirements.Count(r => r.Severity >= 2) : 0,
            SortColumn.Status => b => (int)ModCompatibility.For(b.Selected).Status,
            _ => b => b.TrimmedName
        };

        var ordered = _descending
            ? _rows.OrderByDescending(key).ThenBy(b => b.TrimmedName)
            : _rows.OrderBy(key).ThenBy(b => b.TrimmedName);
        var sorted = ordered.ToList();
        _rows.Clear();
        _rows.AddRange(sorted);
    }

    private void ToggleSort(SortColumn column)
    {
        if (_sort == column)
        {
            _descending = !_descending;
        }
        else
        {
            _sort = column;
            _descending = column is SortColumn.Updated or SortColumn.Issues;
        }

        Sort();
    }

    private void DoTable(Rect canvas)
    {
        DarkWidgets.Panel(canvas);
        var versions = ModCompatibility.GameVersions;

        // column layout: dot | name (flex) | source | mod version | versions... | updated | issues | status
        var fixedWidth = DotSize + SmallMargin + SourceColumnWidth + ModVersionColumnWidth +
                         (versions.Count * VersionColumnWidth) + UpdatedColumnWidth + IssuesColumnWidth +
                         StatusColumnWidth + 18f;
        var nameWidth = Mathf.Max(140f, canvas.width - fixedWidth - (SmallMargin * 2));

        var header = new Rect(canvas.xMin + SmallMargin, canvas.yMin, canvas.width - (SmallMargin * 2),
            TableRowHeight);
        var x = header.xMin + DotSize + SmallMargin;
        if (DarkWidgets.SortHeader(new Rect(x, header.yMin, nameWidth, header.height), I18n.Title,
                _sort == SortColumn.Name, _descending))
        {
            ToggleSort(SortColumn.Name);
        }

        x += nameWidth;
        if (DarkWidgets.SortHeader(new Rect(x, header.yMin, SourceColumnWidth, header.height), "",
                _sort == SortColumn.Source, _descending, tip: I18n.CompatColumnSource))
        {
            ToggleSort(SortColumn.Source);
        }

        x += SourceColumnWidth;
        DarkWidgets.Label(new Rect(x, header.yMin, ModVersionColumnWidth, header.height),
            I18n.CompatColumnModVersion.ToUpperInvariant(), DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleLeft,
            false);
        x += ModVersionColumnWidth;
        foreach (var version in versions)
        {
            var rect = new Rect(x, header.yMin, VersionColumnWidth, header.height);
            if (ModCompatibility.IsCurrent(version))
            {
                Widgets.DrawBoxSolid(rect, DarkTheme.AccentSoft);
            }

            DarkWidgets.Label(rect, ModCompatibility.Key(version),
                ModCompatibility.IsCurrent(version) ? DarkTheme.TextPrimary : DarkTheme.TextMuted, GameFont.Tiny,
                TextAnchor.MiddleCenter, false);
            x += VersionColumnWidth;
        }

        if (DarkWidgets.SortHeader(new Rect(x, header.yMin, UpdatedColumnWidth, header.height),
                I18n.CompatColumnUpdated, _sort == SortColumn.Updated, _descending, TextAnchor.MiddleCenter))
        {
            ToggleSort(SortColumn.Updated);
        }

        x += UpdatedColumnWidth;
        if (DarkWidgets.SortHeader(new Rect(x, header.yMin, IssuesColumnWidth, header.height), "!",
                _sort == SortColumn.Issues, _descending, TextAnchor.MiddleCenter, I18n.CompatColumnIssues))
        {
            ToggleSort(SortColumn.Issues);
        }

        x += IssuesColumnWidth;
        if (DarkWidgets.SortHeader(new Rect(x, header.yMin, StatusColumnWidth, header.height),
                I18n.CompatColumnStatus, _sort == SortColumn.Status, _descending))
        {
            ToggleSort(SortColumn.Status);
        }

        DarkWidgets.HorizontalLine(canvas.xMin, header.yMax, canvas.width);

        var outRect = new Rect(canvas.xMin, header.yMax + 1f, canvas.width, canvas.yMax - header.yMax - 2f);
        if (!_rows.Any())
        {
            DarkWidgets.EmptyState(outRect, I18n.NoMatchingMods);
            return;
        }

        var viewRect = new Rect(outRect.xMin, outRect.yMin, outRect.width - 18f, _rows.Count * TableRowHeight);
        var first = Mathf.FloorToInt(_scroll.y / TableRowHeight) - 1;
        var last = Mathf.CeilToInt((_scroll.y + outRect.height) / TableRowHeight) + 1;

        Widgets.BeginScrollView(outRect, ref _scroll, viewRect);
        for (var i = Mathf.Max(first, 0); i < _rows.Count && i <= last; i++)
        {
            var rowRect = new Rect(viewRect.xMin, viewRect.yMin + (i * TableRowHeight), viewRect.width,
                TableRowHeight);
            DoRow(rowRect, _rows[i], i % 2 == 1, nameWidth, versions);
        }

        Widgets.EndScrollView();
    }

    private static void DoRow(Rect rect, ModButton_Installed button, bool alternate, float nameWidth,
        List<Version> versions)
    {
        var mod = button.Selected;
        var compat = ModCompatibility.For(mod);
        var page = Page_BetterModConfig.Instance;

        Widgets.DrawBoxSolid(rect, alternate ? DarkTheme.PanelAlt : DarkTheme.Panel);
        if (page?.Selected == button)
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.Selected);
            Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMin, 2f, rect.height), DarkTheme.Accent);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        var x = rect.xMin + SmallMargin;
        var dotRect = new Rect(x, rect.yMin + ((rect.height - DotSize) / 2f), DotSize, DotSize);
        DrawDot(dotRect, button.Active ? DarkTheme.DotGreen : DarkTheme.TextDisabled);
        TooltipHandler.TipRegion(dotRect, button.Active ? I18n.ScopeActive : I18n.ScopeInactive);
        x += DotSize + SmallMargin;

        var nameRect = new Rect(x, rect.yMin, nameWidth, rect.height);
        DarkWidgets.LabelTruncated(nameRect.ContractedBy(2f, 0f), button.TrimmedName,
            button.Active ? DarkTheme.TextPrimary : DarkTheme.TextMuted);
        x += nameWidth;

        var iconRect = new Rect(x + ((SourceColumnWidth - SmallIconSize) / 2f),
            rect.yMin + ((rect.height - SmallIconSize) / 2f), SmallIconSize, SmallIconSize);
        GUI.color = DarkTheme.TextMuted;
        GUI.DrawTexture(iconRect, mod.Source.GetIcon());
        GUI.color = Color.white;
        TooltipHandler.TipRegion(iconRect, mod.Source.HumanLabel());
        x += SourceColumnWidth;

        var modVersion = button.Manifest is { HasVersion: true } manifest
            ? manifest.Version.ToString()
            : mod.ModVersion;
        DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, ModVersionColumnWidth - 4f, rect.height),
            modVersion.NullOrEmpty() ? "—" : modVersion, DarkTheme.TextMuted, GameFont.Tiny);
        x += ModVersionColumnWidth;

        foreach (var version in versions)
        {
            DoVersionCell(new Rect(x, rect.yMin, VersionColumnWidth, rect.height), compat, version);
            x += VersionColumnWidth;
        }

        var workshop = compat.Workshop;
        var updatedRect = new Rect(x, rect.yMin, UpdatedColumnWidth, rect.height);
        DarkWidgets.Label(updatedRect, workshop != null ? WorkshopItemInfo.FormatDate(workshop.Updated) : "—",
            DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleCenter, false);
        if (workshop != null)
        {
            TooltipHandler.TipRegion(updatedRect,
                $"{I18n.WorkshopSubscribers(WorkshopItemInfo.FormatCount(workshop.Subscribers))}\n" +
                $"{I18n.WorkshopTags}: {workshop.Tags.StringJoin(", ")}");
        }

        x += UpdatedColumnWidth;

        var issues = button.Active ? button.Requirements.Where(r => r.Severity >= 2).ToList() : null;
        if (issues is { Count: > 0 })
        {
            var issuesRect = new Rect(x, rect.yMin, IssuesColumnWidth, rect.height);
            DarkWidgets.Label(issuesRect, issues.Count.ToString(), DarkTheme.DotRed, GameFont.Tiny,
                TextAnchor.MiddleCenter, false);
            TooltipHandler.TipRegion(issuesRect, issues.Select(i => i.Tooltip).StringJoin("\n"));
        }

        x += IssuesColumnWidth;

        var statusRect = new Rect(x, rect.yMin, StatusColumnWidth, rect.height);
        DarkWidgets.LabelTruncated(statusRect, compat.StatusLabel, compat.StatusColor, GameFont.Tiny);
        TooltipHandler.TipRegion(statusRect, compat.StatusTip);

        // interactions: click selects, double-click opens it in the mod list, right-click shows the mod menu
        if (!Mouse.IsOver(rect))
        {
            return;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            if (Event.current.clickCount == 2)
            {
                page?.ShowMod(button);
            }
            else if (page != null)
            {
                page.Selected = button;
            }

            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            button.DoModActionFloatMenu();
            Event.current.Use();
        }
    }

    private static void DoVersionCell(Rect rect, ModCompatibility compat, Version version)
    {
        var cell = rect.ContractedBy(3f);
        var support = compat.SupportFor(version);
        switch (support)
        {
            case VersionSupport.Declared:
                Widgets.DrawBoxSolid(cell, DarkTheme.CellGreen);
                GUI.color = ModCompatibility.IsCurrent(version) ? DarkTheme.DotGreen : DarkTheme.TextPrimary;
                GUI.DrawTexture(new Rect(0, 0, SmallIconSize, SmallIconSize).CenteredOnXIn(cell).CenteredOnYIn(cell),
                    Check);
                break;
            case VersionSupport.WorkshopOnly:
                Widgets.DrawBoxSolid(cell, DarkTheme.CellBlue);
                GUI.color = DarkTheme.Accent;
                GUI.DrawTexture(new Rect(0, 0, SmallIconSize, SmallIconSize).CenteredOnXIn(cell).CenteredOnYIn(cell),
                    Status_Up);
                break;
            case VersionSupport.FolderOnly:
                Widgets.DrawBoxSolid(cell, DarkTheme.CellYellow);
                break;
            default:
                DrawDot(new Rect(0, 0, 4f, 4f).CenteredOnXIn(cell).CenteredOnYIn(cell), DarkTheme.TextDisabled);
                break;
        }

        // small marker for a version-specific folder / LoadFolders entry
        if (support != VersionSupport.None && compat.HasFolderFor(version))
        {
            Widgets.DrawBoxSolid(new Rect(cell.xMax - 4f, cell.yMax - 4f, 4f, 4f), DarkTheme.DotYellow);
        }

        GUI.color = Color.white;
        TooltipHandler.TipRegion(rect, () => compat.SupportTip(version), compat.GetHashCode() ^ version.GetHashCode());
    }

    private string ExportCsv()
    {
        var versions = ModCompatibility.GameVersions;
        var csv = new StringBuilder();
        csv.Append("name,packageId,source,active,modVersion,");
        csv.Append(versions.Select(ModCompatibility.Key).StringJoin(","));
        csv.AppendLine(",workshopTags,workshopUpdated,status");
        foreach (var button in _rows)
        {
            var mod = button.Selected;
            var compat = ModCompatibility.For(mod);
            var workshop = compat.Workshop;
            csv.Append($"{Quote(button.Name)},{mod.PackageId},{mod.Source},{button.Active},{Quote(mod.ModVersion)},");
            csv.Append(versions.Select(v => compat.SupportFor(v) switch
            {
                VersionSupport.Declared => "yes",
                VersionSupport.WorkshopOnly => "workshop",
                VersionSupport.FolderOnly => "folder",
                _ => ""
            }).StringJoin(","));
            csv.AppendLine(
                $",{Quote(workshop?.Tags.StringJoin(";") ?? "")},{(workshop != null ? WorkshopItemInfo.FormatDate(workshop.Updated) : "")},{compat.Status}");
        }

        return csv.ToString();
    }

    private static string Quote(string value)
    {
        value ??= string.Empty;
        return value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
