// Tab_Profiles.cs
// Saved mod lists ("profiles"): browse, load (replace or merge), compare against the active list, rename,
// recolour, export/import and subscribe to missing Workshop mods.

using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using RimWorld;
using UnityEngine;
using Verse;
using static SCPModManager.Constants;
using static SCPModManager.Resources;

namespace SCPModManager;

public sealed class Tab_Profiles : ManagerTab
{
    private const string SearchControl = "SCPModManager_ProfileSearch";
    private const float ListRowHeight = 40f;
    private const float CacheLifetime = 1f;

    private readonly List<Entry> _entries = [];
    private float _cachedAt = -1f;
    private ModList _cachedFor;
    private int _toActivate, _toDeactivate, _missing, _installed;
    private string _filter = string.Empty;
    private Vector2 _listScroll = Vector2.zero;
    private Vector2 _modsScroll = Vector2.zero;
    private ModList _selected;

    public override string Label => I18n.TabProfiles;

    public override string Badge => ModListManager.ModLists.Count > 0 ? ModListManager.ModLists.Count.ToString() : null;

    public override Color BadgeColor => DarkTheme.PanelAlt;

    public override void FocusSearch()
    {
        GUI.FocusControl(SearchControl);
    }

    public override void OnOpened()
    {
        _cachedFor = null;
    }

    public override void DoContents(Rect canvas)
    {
        if (_selected != null && !ModListManager.ModLists.Contains(_selected))
        {
            _selected = null;
        }

        _selected ??= ModListManager.ModLists.FirstOrDefault();

        var listWidth = Mathf.Floor(canvas.width * 0.34f);
        var listRect = new Rect(canvas.xMin, canvas.yMin, listWidth, canvas.height);
        var detailRect = new Rect(listRect.xMax + SmallMargin, canvas.yMin, canvas.width - listWidth - SmallMargin,
            canvas.height);

        DoProfileList(listRect);
        DoProfileDetails(detailRect);
    }

    private void DoProfileList(Rect canvas)
    {
        DarkWidgets.Panel(canvas);
        canvas = canvas.ContractedBy(SmallMargin);

        // create / import actions
        var x = canvas.xMin;
        if (DarkWidgets.ButtonAuto(ref x, canvas.yMin, I18n.ProfileSaveCurrent, true,
                ModButtonManager.ActiveButtons.Any(), I18n.ProfileSaveCurrentTip))
        {
            _ = new ModList(ModButtonManager.ActiveButtons);
        }

        DarkWidgets.Dropdown(ref x, canvas.yMin, I18n.ActionImport, () =>
        {
            var options = Utilities.NewOptionsList;
            options.Add(new FloatMenuOption(I18n.Import_FromString,
                () => Find.WindowStack.Add(new Dialog_Import_FromString())));
            options.Add(new FloatMenuOption(I18n.Import_FromSaveGame,
                () => Page_BetterModConfig.Instance?.DoImportFromSaveFloatMenu()));
            Utilities.FloatMenu(options);
        }, I18n.ActionImportTip);

        canvas.yMin += ButtonHeight + SmallMargin;

        var searchRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, ButtonHeight);
        DarkWidgets.SearchField(searchRect, ref _filter, SearchControl, I18n.ProfileSearchPlaceholder);
        canvas.yMin += ButtonHeight + SmallMargin;

        var lists = ModListManager.ModLists
            .Where(l => _filter.NullOrEmpty() || (l.Name?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0)
            .OrderBy(l => l.Name)
            .ToList();

        DarkWidgets.SectionHeader(ref canvas, I18n.ProfilesSaved, lists.Count.ToString());
        if (!lists.Any())
        {
            DarkWidgets.EmptyState(canvas, ModListManager.ModLists.Any() ? I18n.NoMatchingProfiles : I18n.NoProfiles);
            return;
        }

        var viewRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, lists.Count * ListRowHeight);
        if (viewRect.height > canvas.height)
        {
            viewRect.width -= 18f;
        }

        Widgets.BeginScrollView(canvas, ref _listScroll, viewRect);
        var rowRect = new Rect(viewRect.xMin, viewRect.yMin, viewRect.width, ListRowHeight);
        var alternate = false;
        foreach (var list in lists)
        {
            DoProfileRow(rowRect, list, alternate);
            rowRect.y += ListRowHeight;
            alternate = !alternate;
        }

        Widgets.EndScrollView();
    }

    private void DoProfileRow(Rect rect, ModList list, bool alternate)
    {
        Widgets.DrawBoxSolid(rect, alternate ? DarkTheme.PanelAlt : DarkTheme.Panel);
        if (list == _selected)
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.Selected);
            Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMin, 2f, rect.height), DarkTheme.Accent);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        var inner = rect.ContractedBy(SmallMargin);
        var swatch = new Rect(inner.xMin, inner.yMin + ((inner.height - DotSize) / 2f), DotSize, DotSize);
        DrawDot(swatch, list.Color);
        var textX = swatch.xMax + SmallMargin;
        DarkWidgets.LabelTruncated(new Rect(textX, inner.yMin, inner.xMax - textX, inner.height * 0.6f), list.Name,
            DarkTheme.TextPrimary);
        DarkWidgets.Label(new Rect(textX, inner.yMin + (inner.height * 0.55f), inner.xMax - textX, inner.height * 0.45f),
            I18n.ProfileModCount(list.Count), DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleLeft, false);

        if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            _selected = list;
            Utilities.FloatMenu(ProfileOptions(list));
            Event.current.Use();
            return;
        }

        if (Widgets.ButtonInvisible(rect))
        {
            _selected = list;
            _modsScroll = Vector2.zero;
        }
    }

    private List<FloatMenuOption> ProfileOptions(ModList list)
    {
        var options = Utilities.NewOptionsList;
        options.Add(new FloatMenuOption(I18n.ProfileLoad, () => Load(list, false)));
        options.Add(new FloatMenuOption(I18n.ProfileMerge, () => Load(list, true)));
        options.Add(new FloatMenuOption(I18n.RenameModList, () => Find.WindowStack.Add(new Dialog_Rename_ModList(list))));
        options.Add(new FloatMenuOption(I18n.ChangeListColour,
            () => Find.WindowStack.Add(new Dialog_ColourPicker(list.Color, color => list.Color = color))));
        options.Add(new FloatMenuOption(I18n.Export_ToString,
            () => Find.WindowStack.Add(new Dialog_Export_ToString(list))));
        options.Add(new FloatMenuOption(I18n.DeleteModList, () => ConfirmDelete(list)));
        return options;
    }

    private static void Load(ModList list, bool merge)
    {
        list.Import(merge);
        Messages.Message(I18n.ModListLoaded(list.Name), MessageTypeDefOf.TaskCompletion, false);
    }

    private static void ConfirmDelete(ModList list)
    {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(I18n.ConfirmDeleteProfile(list.Name),
            () => ModListManager.TryDelete(list), true));
    }

    private void Recache(ModList list)
    {
        if (_cachedFor == list && Time.realtimeSinceStartup - _cachedAt < CacheLifetime)
        {
            return;
        }

        _cachedFor = list;
        _cachedAt = Time.realtimeSinceStartup;
        _entries.Clear();
        _toActivate = _toDeactivate = _missing = _installed = 0;

        var profileIds = new HashSet<string>();
        foreach (var identifier in list.Mods)
        {
            var id = identifier.Id?.StripPostfixes() ?? string.Empty;
            var mod = id.NullOrEmpty() ? null : ModLister.GetModWithIdentifier(id, true);
            profileIds.Add(id.ToLowerInvariant());
            var entry = new Entry { Identifier = identifier, Mod = mod };
            if (mod == null)
            {
                _missing++;
            }
            else
            {
                _installed++;
                if (!mod.Active)
                {
                    _toActivate++;
                }
            }

            _entries.Add(entry);
        }

        _toDeactivate = ModsConfig.ActiveModsInLoadOrder.Count(m =>
            !profileIds.Contains(m.PackageId.StripPostfixes().ToLowerInvariant()));
    }

    private void DoProfileDetails(Rect canvas)
    {
        DarkWidgets.Panel(canvas);
        if (_selected == null)
        {
            DarkWidgets.EmptyState(canvas, I18n.NoProfiles);
            return;
        }

        var list = _selected;
        Recache(list);
        canvas = canvas.ContractedBy(SmallMargin);

        // title
        var titleRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, LineHeight + 12f);
        var swatch = new Rect(titleRect.xMin, titleRect.yMin + ((titleRect.height - 14f) / 2f), 14f, 14f);
        DrawDot(swatch, list.Color);
        DarkWidgets.LabelTruncated(new Rect(swatch.xMax + SmallMargin, titleRect.yMin,
            titleRect.width - swatch.width - SmallMargin, titleRect.height), list.Name, DarkTheme.TextPrimary,
            GameFont.Medium);
        canvas.yMin = titleRect.yMax + SmallMargin;

        // counts + diff against the currently active list
        var x = canvas.xMin;
        DarkWidgets.Chip(ref x, canvas.yMin, I18n.ProfileModCount(_entries.Count), DarkTheme.TextPrimary);
        DarkWidgets.Chip(ref x, canvas.yMin, I18n.ProfileInstalled(_installed), DarkTheme.DotGreen);
        if (_missing > 0)
        {
            DarkWidgets.Chip(ref x, canvas.yMin, I18n.ProfileMissing(_missing), DarkTheme.DotRed);
        }

        DarkWidgets.Chip(ref x, canvas.yMin, I18n.ProfileDiff(_toActivate, _toDeactivate), DarkTheme.TextMuted,
            I18n.ProfileDiffTip);
        canvas.yMin += ChipHeight + SmallMargin;

        // actions (wrap onto a second row in narrow windows)
        x = canvas.xMin;
        var y = canvas.yMin;
        var xMin = canvas.xMin;
        var xMax = canvas.xMax;
        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ProfileLoad, true, tip: I18n.ProfileLoadTip))
        {
            Load(list, false);
        }

        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ProfileMerge, tip: I18n.ProfileMergeTip))
        {
            Load(list, true);
        }

        var missingSteam = _entries
            .Where(e => e.Mod == null && ulong.TryParse(e.Identifier.SteamWorkshopId, out var id) && id != 0)
            .Select(e => e.Identifier.SteamWorkshopId)
            .ToList();
        if (missingSteam.Any() && WorkshopSearch.Available &&
            DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ProfileSubscribeMissing(missingSteam.Count)))
        {
            Workshop.Subscribe(missingSteam);
        }

        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ActionExport, tip: I18n.Export_ToString))
        {
            Find.WindowStack.Add(new Dialog_Export_ToString(list));
        }

        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ActionRename, tip: I18n.RenameModList))
        {
            Find.WindowStack.Add(new Dialog_Rename_ModList(list));
        }

        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ActionColour, tip: I18n.ChangeListColour))
        {
            Find.WindowStack.Add(new Dialog_ColourPicker(list.Color, color => list.Color = color));
        }

        if (DarkWidgets.ButtonFlow(ref x, ref y, xMin, xMax, I18n.ActionDelete, fg: DarkTheme.DotRed,
                tip: I18n.DeleteModList))
        {
            ConfirmDelete(list);
        }

        canvas.yMin = y + ButtonHeight + SmallMargin;

        // mods in the profile, in load order
        DarkWidgets.SectionHeader(ref canvas, I18n.ProfileMods);
        var viewRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, _entries.Count * TableRowHeight);
        if (viewRect.height > canvas.height)
        {
            viewRect.width -= 18f;
        }

        var first = Mathf.FloorToInt(_modsScroll.y / TableRowHeight) - 1;
        var last = Mathf.CeilToInt((_modsScroll.y + canvas.height) / TableRowHeight) + 1;
        Widgets.BeginScrollView(canvas, ref _modsScroll, viewRect);
        for (var i = Mathf.Max(first, 0); i < _entries.Count && i <= last; i++)
        {
            var rowRect = new Rect(viewRect.xMin, viewRect.yMin + (i * TableRowHeight), viewRect.width,
                TableRowHeight);
            DoModRow(rowRect, i, _entries[i]);
        }

        Widgets.EndScrollView();
    }

    private static void DoModRow(Rect rect, int index, Entry entry)
    {
        Widgets.DrawBoxSolid(rect, index % 2 == 1 ? DarkTheme.PanelAlt : DarkTheme.Panel);
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        var x = rect.xMin + SmallMargin;
        DarkWidgets.Label(new Rect(x, rect.yMin, 26f, rect.height), (index + 1).ToString(), DarkTheme.TextMuted,
            GameFont.Tiny, TextAnchor.MiddleRight, false);
        x += 26f + SmallMargin;

        Color dot;
        string state;
        if (entry.Mod == null)
        {
            dot = DarkTheme.DotRed;
            state = I18n.ProfileStateMissing;
        }
        else if (entry.Mod.Active)
        {
            dot = DarkTheme.DotGreen;
            state = I18n.ScopeActive;
        }
        else
        {
            dot = DarkTheme.TextDisabled;
            state = I18n.ProfileStateInactive;
        }

        var dotRect = new Rect(x, rect.yMin + ((rect.height - DotSize) / 2f), DotSize, DotSize);
        DrawDot(dotRect, dot);
        TooltipHandler.TipRegion(dotRect, state);
        x += DotSize + SmallMargin;

        const float actionWidth = 86f;
        var nameWidth = (rect.xMax - x - actionWidth - SmallMargin) * 0.6f;
        DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, nameWidth, rect.height),
            entry.Mod?.Name ?? entry.Identifier.Name, entry.Mod == null ? DarkTheme.TextMuted : DarkTheme.TextPrimary);
        x += nameWidth + SmallMargin;
        DarkWidgets.LabelTruncated(new Rect(x, rect.yMin, rect.xMax - x - actionWidth - SmallMargin, rect.height),
            entry.Identifier.Id ?? string.Empty, DarkTheme.TextMuted, GameFont.Tiny);

        var actionRect = new Rect(rect.xMax - actionWidth - SmallMargin, rect.yMin + 1f, actionWidth,
            rect.height - 2f);
        if (entry.Mod == null)
        {
            if (ulong.TryParse(entry.Identifier.SteamWorkshopId, out var id) && id != 0 && WorkshopSearch.Available)
            {
                if (DarkWidgets.PrimaryButton(actionRect, I18n.ActionSubscribe))
                {
                    Workshop.Subscribe(entry.Identifier.SteamWorkshopId);
                }
            }
            else if (DarkWidgets.Button(actionRect, I18n.ActionFind, tip: I18n.SearchSteamWorkshop(entry.Identifier.Name)))
            {
                Page_BetterModConfig.Instance?.OpenWorkshopSearch(entry.Identifier.Name);
            }
        }
        else if (DarkWidgets.Button(actionRect, I18n.ActionShowInList))
        {
            Page_BetterModConfig.Instance?.ShowMod(ModButton_Installed.For(entry.Mod));
        }
    }

    private sealed class Entry
    {
        public ModIdentifier Identifier;
        public ModMetaData Mod;
    }
}
