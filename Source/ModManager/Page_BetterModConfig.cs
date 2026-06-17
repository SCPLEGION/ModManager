// Window_ModSelection.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using static ModManager.Constants;
using static ModManager.Resources;

namespace ModManager;

public class Page_BetterModConfig : Page_ModsConfig
{
    private string _activeFilter;
    private bool _activeFilterVisible = true;
    private int _activeModsHash;
    private Vector2 _activeScrollPosition = Vector2.zero;
    private string _availableFilter;
    private bool _availableFilterVisible;
    private Vector2 _availableScrollPosition = Vector2.zero;

    private bool _draggingOverAvailable;

    private FocusArea _focusArea = FocusArea.Available;
    private int _focusElement = 1;

    private int _issueIndex;

    private int _lastControlID;
    private int _lastHoverIndex;
    private int _scrollViewHeight;
    private ModButton _selected;

    private float _width;

    public Page_BetterModConfig()
    {
        doWindowBackground = false;
        Instance = this;
        closeOnAccept = false;
        closeOnCancel = true;
        doCloseButton = false;
        doCloseX = true;
        absorbInputAroundWindow = false;
        resizeable = true;
        AccessTools.Field(typeof(Window), "resizer")
            .SetValue(this, new WindowResizer { minWindowSize = MinimumSize });
    }

    private bool FilterAvailable => !_availableFilterVisible && !_availableFilter.NullOrEmpty();
    private bool FilterActive => !_activeFilterVisible && !_activeFilter.NullOrEmpty();

    public static Page_BetterModConfig Instance { get; private set; }

    public override Vector2 InitialSize => StandardSize;

    private static Vector2 MinimumSize => StandardSize * 2 / 3f;

    public ModButton Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            CrossPromotionManager.Notify_UpdateRelevantMods();

            // cop-out if null
            if (value == null)
            {
                return;
            }

            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            // clear text field focus
            GUIUtility.keyboardControl = 0;

            // set correct focus
            if (!value.Active)
            {
                _focusArea = FocusArea.Available;
                if (!FilteredAvailableButtons.Contains(value))
                {
                    _availableFilter = null;
                }

                _focusElement = FilteredAvailableButtons.FirstIndexOf(mod => mod == value);
                EnsureVisible(ref _availableScrollPosition, _focusElement);
            }
            else
            {
                _focusArea = FocusArea.Active;
                if (!FilteredActiveButtons.Contains(value))
                {
                    _activeFilter = null;
                }

                _focusElement = FilteredActiveButtons.FirstIndexOf(mod => mod == value);
                EnsureVisible(ref _activeScrollPosition, _focusElement);
            }
        }
    }

    public bool SelectedHasFocus =>
        _focusArea == FocusArea.Active && ModButtonManager.ActiveButtons.Contains(Selected) ||
        _focusArea == FocusArea.Available && ModButtonManager.AvailableButtons.Contains(Selected);

    private List<ModButton> FilteredAvailableButtons
    {
        get
        {
            if (FilterAvailable)
            {
                return ModButtonManager.AvailableButtons
                    .Where(b => b.MatchesFilter(_availableFilter) > 0)
                    .OrderBy(b => b.MatchesFilter(_availableFilter))
                    .ToList();
            }

            return ModButtonManager.AvailableButtons;
        }
    }

    private List<ModButton> FilteredActiveButtons => ModButtonManager.ActiveButtons
        .Where(b => !FilterActive || b.MatchesFilter(_activeFilter) > 0)
        .ToList();

    public override void WindowOnGUI()
    {
        base.WindowOnGUI();
        DrawDarkWindowBackground(windowRect, ModManager.Settings.BackgroundColor);
    }

    public override void
        ExtraOnGUI()
    {
        base.ExtraOnGUI();
        DraggingManager.OnGUI();

#if DEBUG
            DebugOnGUI();
#endif
    }

    private static void DebugOnGUI()
    {
        var msg = "";
        var mods = ModsConfig.ActiveModsInLoadOrder.ToList();
        for (var i = 0; i < mods.Count; i++)
        {
            var mod = mods[i];
            msg += $"{i}: {mod?.Name ?? "NULL"}\t({mod?.PackageId ?? "NULL"})\n";
        }

        Widgets.Label(new Rect(0, 0, Screen.width, Screen.height), msg);
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        DraggingManager.Update();
        CrossPromotionManager.Update();
    }

    public override void DoWindowContents(Rect canvas)
    {
        CheckResized();
        HandleKeyboardNavigation();

        // dark header bar with tabs; everything below is laid out against `body`
        var headerRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, HeaderHeight);
        DoHeaderBar(headerRect);

        var body = new Rect(
            canvas.xMin,
            headerRect.yMax + SmallMargin,
            canvas.width,
            canvas.height - HeaderHeight - SmallMargin);

        var iconBarHeight = IconSize + SmallMargin;
        var colWidth = Mathf.FloorToInt(body.width / 5);

        var availableRect = new Rect(
            body.xMin,
            body.yMin,
            colWidth,
            body.height - SmallMargin - iconBarHeight);
        var moreModButtonsRect = new Rect(
            body.xMin,
            availableRect.yMax + SmallMargin,
            colWidth,
            iconBarHeight);
        var activeRect = new Rect(
            availableRect.xMax + SmallMargin,
            body.yMin,
            colWidth,
            body.height - SmallMargin - iconBarHeight);
        var modSetButtonsRect = new Rect(
            activeRect.xMin,
            activeRect.yMax + SmallMargin,
            colWidth,
            iconBarHeight);
        var detailRect = new Rect(
            activeRect.xMax + SmallMargin,
            body.yMin,
            body.width - ((colWidth + SmallMargin) * 2),
            body.height - SmallMargin - iconBarHeight);
        var modButtonsRect = new Rect(
            detailRect.xMin,
            detailRect.yMax + SmallMargin,
            detailRect.width,
            iconBarHeight);

        // if ( !DraggingManager.Dragging && !( Mouse.IsOver( availableRect ) || Mouse.IsOver( activeRect ) ) )
        //     GUI.DragWindow();
        DoAvailableMods(availableRect);
        DoActiveMods(activeRect);
        DoDetails(detailRect);

        DoAvailableModButtons(moreModButtonsRect);
        DoActiveModButtons(modSetButtonsRect);
        Selected?.DoModActionButtons(modButtonsRect);
    }

    private void DoHeaderBar(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, DarkTheme.HeaderBar);
        GUI.color = DarkTheme.Border;
        Widgets.DrawLineHorizontal(canvas.xMin, canvas.yMax, canvas.width);
        GUI.color = Color.white;

        var tabRect = new Rect(
            canvas.xMin + SmallMargin,
            canvas.yMin + ((HeaderHeight - TabHeight) / 2f),
            TabWidth,
            TabHeight);
        // Mods is the active view; Profiles opens the mod-list (profiles) menu; Updates lists mods with updates.
        DrawTab(ref tabRect, I18n.TabMods, true, null);
        DrawTab(ref tabRect, I18n.TabProfiles, false, DoModListFloatMenu);
        DrawTab(ref tabRect, I18n.TabUpdates, false, DoUpdatesFloatMenu);
    }

    private static void DrawTab(ref Rect rect, string label, bool active, Action onClick)
    {
        if (active)
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.Selected);
            GUI.color = DarkTheme.Accent;
            Widgets.DrawLineHorizontal(rect.xMin, rect.yMax - 2f, rect.width);
            GUI.color = Color.white;
        }
        else
        {
            Widgets.DrawHighlightIfMouseover(rect);
        }

        var oldAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = active ? DarkTheme.TextPrimary : DarkTheme.TextMuted;
        Widgets.Label(rect, label);
        GUI.color = Color.white;
        Text.Anchor = oldAnchor;

        if (Widgets.ButtonInvisible(rect))
        {
            onClick?.Invoke();
        }

        rect.x += rect.width + SmallMargin;
    }

    private void DoUpdatesFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        foreach (var button in ModButtonManager.ActiveButtons.OfType<ModButton_Installed>())
        {
            var updates = button.Requirements
                .Where(r => r is VersionCheck or SourceSync && r.IsApplicable && !r.IsSatisfied)
                .ToList();
            if (!updates.Any())
            {
                continue;
            }

            var target = button;
            var update = updates.First();
            options.Add(new FloatMenuOption($"{target.Name}: {update.Tooltip}", () =>
            {
                Selected = target;
                update.OnClicked(null);
            }));
        }

        if (!options.Any())
        {
            options.Add(new FloatMenuOption(I18n.NoUpdatesAvailable, null));
        }

        Utilities.FloatMenu(options);
    }

    private static void DrawColumnHeader(ref Rect canvas, string label)
    {
        // mirrors Utilities.DoLabel's canvas mutation exactly so downstream layout is unchanged
        var labelRect = new Rect(
            canvas.xMin + SmallIconSize,
            canvas.yMin,
            canvas.width - SmallIconSize,
            LabelHeight);
        canvas.yMin += LabelHeight - LabelOffset;
        var oldFont = Text.Font;
        Text.Font = GameFont.Tiny;
        GUI.color = DarkTheme.TextMuted;
        Widgets.Label(labelRect, label.ToUpperInvariant());
        GUI.color = Color.white;
        Text.Font = oldFont;
    }

    private void CheckResized()
    {
        if (windowRect.width == _width)
        {
            return;
        }

        ModButton.Notify_ModButtonSizeChanged();
        _width = windowRect.width;
    }

    private void DoAvailableModButtons(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
        canvas = canvas.ContractedBy(SmallMargin / 2f);

        var iconRect = new Rect(
            canvas.xMax - IconSize,
            canvas.yMin,
            IconSize,
            IconSize);
        if (Utilities.ButtonIcon(ref iconRect, Steam, I18n.GetMoreMods_SteamWorkshop, Status_Plus,
                Direction8Way.NorthWest))
        {
            SteamUtility.OpenSteamWorkshopPage();
        }

        if (Utilities.ButtonIcon(ref iconRect, Ludeon, I18n.GetMoreMods_LudeonForums, Status_Plus,
                Direction8Way.NorthWest))
        {
            Application.OpenURL("http://rimworldgame.com/getmods");
        }

        iconRect.x = canvas.xMin;
        if (ModButtonManager.AllMods.Any(m => m.Source == ContentSource.SteamWorkshop) &&
            Utilities.ButtonIcon(ref iconRect, Steam, I18n.MassUnSubscribe, Status_Cross, Direction8Way.NorthWest,
                Color.red, direction: UIDirection.RightThenDown))
        {
            Workshop.MassUnsubscribeFloatMenu();
        }

        if (ModButtonManager.AllMods.Any(m => m.Source == ContentSource.ModsFolder && !m.IsCoreMod) &&
            Utilities.ButtonIcon(ref iconRect, Folder, I18n.MassRemoveLocal, Status_Cross,
                mouseOverColor: Color.red, direction: UIDirection.RightThenDown))
        {
            IO.MassRemoveLocalFloatMenu();
        }
    }

    private void DoActiveModButtons(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
        canvas = canvas.ContractedBy(SmallMargin / 2f);

        var iconRect = new Rect(
            canvas.xMax - IconSize,
            canvas.yMin,
            IconSize,
            IconSize);
        if (Utilities.ButtonIcon(ref iconRect, Resources.File, I18n.ModListsTip))
        {
            DoModListFloatMenu();
        }

        if (ModButtonManager.ActiveMods.Any(mod => mod.Source == ContentSource.SteamWorkshop))
        {
            if (Utilities.ButtonIcon(ref iconRect, Folder, I18n.CreateLocalCopies, Status_Plus))
            {
                IO.CreateLocalCopies(ModButtonManager.ActiveMods);
            }
        }

        if (ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
            .Any(b => b.SteamWorkshopId.IsValidSteamWorkshopIdentifier()))
        {
            if (Utilities.ButtonIcon(ref iconRect, Steam, I18n.SubscribeAllMissing, Status_Plus,
                    Direction8Way.NorthWest))
            {
                Workshop.Subscribe(ModButtonManager.ActiveButtons.OfType<ModButton_Missing>()
                    .Where(b => b.SteamWorkshopId.IsValidSteamWorkshopIdentifier())
                    .Select(b => b.SteamWorkshopId.ToString()));
            }
        }

        iconRect.x = canvas.xMin + SmallMargin;
        var severityThreshold = 2;
        var relevantIssues =
            ModButtonManager.Issues.Where(i => i.Severity >= severityThreshold);
        if (relevantIssues.Any())
        {
            var groupedIssues = relevantIssues
                .GroupBy(i => i.parent)
                .OrderByDescending(bi => bi.Max(i => i.Severity))
                .ThenBy(bi => bi.Key.Mod.Name);
            foreach (var buttonIssues in groupedIssues)
            {
                var tip = $"<b>{buttonIssues.Key.Mod.Name}</b>\n";
                tip += buttonIssues.Select(i => i.Tooltip.Colorize(i.Color)).StringJoin("\n");
                TooltipHandler.TipRegion(iconRect, tip);
            }

            var worstIssue = relevantIssues.MaxBy(i => i.Severity);
            var color = worstIssue.Color;

            if (Widgets.ButtonImage(iconRect, worstIssue.StatusIcon, color))
            {
                _issueIndex = Utilities.Modulo(_issueIndex, groupedIssues.Count());
                Selected = ModButton_Installed.For(groupedIssues.ElementAt(_issueIndex++).Key.Mod);
            }

            iconRect.x += IconSize + SmallMargin;
        }

        if (ModButtonManager.ActiveButtons.Count > 0)
        {
            if (Utilities.ButtonIcon(ref iconRect, Spinner[0], I18n.ResetMods, mouseOverColor: Color.red,
                    direction: UIDirection.RightThenDown))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(I18n.ConfirmResetMods, I18n.Yes,
                    () => ModButtonManager.Reset(), I18n.Cancel,
                    buttonADestructive: true));
            }
        }

        if (ModButtonManager.ActiveButtons.Count <= 1 || !ModButtonManager.AnyIssue)
        {
            return;
        }

        if (Utilities.ButtonIcon(ref iconRect, Wand, I18n.SortMods))
        {
            ModButtonManager.Sort();
        }
    }

    private void DoModListFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.Add(new FloatMenuOption(I18n.Export, DoExportModListFloatMenu));
        options.Add(new FloatMenuOption(I18n.Import, DoImportModListFloatMenu));
        options.Add(new FloatMenuOption(I18n.Edit, DoEditModListFloatMenu));

        Utilities.FloatMenu(options);
    }

    private void DoExportModListFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.Add(new FloatMenuOption(I18n.Export_ToModList,
            () => { _ = new ModList(ModButtonManager.ActiveButtons); }));
        options.Add(new FloatMenuOption(I18n.Export_ToString, delegate
        {
            var modlist = new ModList(ModButtonManager.ActiveButtons, true);
            var dialog = new Dialog_Export_ToString(modlist);

            Find.WindowStack.Add(dialog);
        }));

        Utilities.FloatMenu(options);
    }

    private void DoImportModListFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.Add(new FloatMenuOption(I18n.Import_FromModList, DoImportFromModListFloatMenu));
        options.Add(new FloatMenuOption(I18n.Import_FromSaveGame, DoImportFromSaveFloatMenu));
        options.Add(
            new FloatMenuOption(I18n.Import_FromString, () => Find.WindowStack.Add(new Dialog_Import_FromString())));

        Utilities.FloatMenu(options);
    }

    private void DoImportFromModListFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.AddRange(ModListManager.ModLists.Select(ml =>
            new FloatMenuOption(ml.Name, () => ml.Import(Event.current.shift))));

        Utilities.FloatMenu(options);
    }

    private void DoEditModListFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.AddRange(ModListManager.ModLists.Select(ModListManager.GetEditSubMenuFor));

        Utilities.FloatMenu(options);
    }

    private void DoImportFromSaveFloatMenu()
    {
        var options = Utilities.NewOptionsList;
        options.AddRange(GenFilePaths.AllSavedGameFiles.Select(fi =>
        {
            var name = Path.GetFileNameWithoutExtension(fi.Name);
            return new FloatMenuOption(name,
                () => { ModList.FromSave(name, fi.FullName).Import(Event.current.shift); });
        }));
        Utilities.FloatMenu(options);
    }

    private void HandleKeyboardNavigation()
    {
        // note that this depends on pop-over windows setting absorbInputAroundWindow
        if (!Find.WindowStack.CurrentWindowGetsInput)
        {
            return;
        }

        // only use our custom logic if we are the top window
        if (Find.WindowStack.Windows.Last() != this)
        {
            return;
        }

        var id = GUIUtility.keyboardControl;
        if (_lastControlID != id)
        {
            _lastControlID = id;
            Debug.Log(
                $"Focus: {_focusArea}, {_focusElement}, {id}, {GUI.GetNameOfFocusedControl()}, {Event.current.type}");
        }

        // unity tries to select the next textField on Tab (which is _not_ documented!)
        if (_focusArea.ToString() != GUI.GetNameOfFocusedControl())
        {
            if (_focusArea is FocusArea.ActiveFilter or FocusArea.AvailableFilter)
            {
                // focus the control we want.
                GUI.FocusControl(_focusArea.ToString());
            }
            else
            {
                // clear focus.
                GUIUtility.keyboardControl = 0;
            }
        }


        // handle keyboard events
        var key = Event.current.keyCode;
        var shift = Event.current.shift;
        if (Event.current.type != EventType.KeyDown)
        {
            return;
        }

        // move controlled area on (shift) tab
        if (key == KeyCode.Tab)
        {
            // determine new focus
            var focusInt = Utilities.Modulo((int)_focusArea + (shift ? -1 : 1), 4);
            var focus = (FocusArea)focusInt;

            Debug.Log($"current focus: {_focusArea}, new focus: {focus}");

            // apply new focus
            _focusArea = focus;
            switch (focus)
            {
                case FocusArea.AvailableFilter:
                case FocusArea.ActiveFilter:
                    GUI.FocusControl(focus.ToString());
                    break;
                case FocusArea.Available:
                    SelectAt(FilteredAvailableButtons, _availableScrollPosition);
                    break;
                case FocusArea.Active:
                    SelectAt(FilteredActiveButtons, _activeScrollPosition);
                    break;
            }

            return;
        }

        if (_focusArea == FocusArea.Available)
        {
            switch (key)
            {
                case KeyCode.UpArrow:
                    SelectPrevious(FilteredAvailableButtons);
                    break;
                case KeyCode.DownArrow:
                    SelectNext(FilteredAvailableButtons);
                    break;
                case KeyCode.PageUp:
                    SelectFirst(FilteredAvailableButtons);
                    break;
                case KeyCode.PageDown:
                    SelectLast(FilteredAvailableButtons);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Selected.Active = true;
                    if (FilteredAvailableButtons.Any())
                    {
                        var index = Math.Min(_focusElement, FilteredAvailableButtons.Count - 1);
                        Selected = FilteredAvailableButtons.ElementAt(index);
                    }
                    else
                    {
                        Selected = Selected;
                    }

                    break;
                case KeyCode.RightArrow:
                    if (shift)
                    {
                        Selected.Active = true;
                        Selected = Selected; // sets _focusElement, _focusArea, plays sound.
                    }
                    else
                    {
                        SelectAt(FilteredActiveButtons, _availableScrollPosition, _activeScrollPosition);
                    }

                    break;
            }

            return;
        }


        if (_focusArea != FocusArea.Active)
        {
            return;
        }

        switch (key)
        {
            case KeyCode.UpArrow:
                if (Event.current.shift)
                {
                    MoveUp();
                }
                else
                {
                    SelectPrevious(FilteredActiveButtons);
                }

                break;
            case KeyCode.DownArrow:
                if (shift)
                {
                    MoveDown();
                }
                else
                {
                    SelectNext(FilteredActiveButtons);
                }

                break;
            case KeyCode.PageUp:
                if (shift)
                {
                    MoveTop();
                }
                else
                {
                    SelectFirst(FilteredActiveButtons);
                }

                break;
            case KeyCode.PageDown:
                if (shift)
                {
                    MoveBottom();
                }
                else
                {
                    SelectLast(FilteredActiveButtons);
                }

                break;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Delete:
                Selected.Active = false;
                if (FilteredActiveButtons.Any())
                {
                    var index = Math.Min(_focusElement, FilteredActiveButtons.Count - 1);
                    Selected = FilteredActiveButtons.ElementAt(index);
                }
                else
                {
                    Selected = Selected;
                }

                break;
            case KeyCode.LeftArrow:
                if (shift)
                {
                    Selected.Active = false;
                    Selected = Selected; // sets _focusArea, _focusElement, plays sound.
                }
                else
                {
                    SelectAt(FilteredAvailableButtons, _activeScrollPosition, _availableScrollPosition);
                }

                break;
        }
    }

    private void MoveUp()
    {
        if (_focusElement <= 0)
        {
            return;
        }

        // is only called from active.
        ModButtonManager.Insert(Selected,
            ModButtonManager.ActiveButtons.IndexOf(FilteredActiveButtons.ElementAt(_focusElement - 1)));
        Selected = Selected; // sets _focusElement, plays sound.
    }

    private void MoveTop()
    {
        ModButtonManager.Insert(Selected, 0);
        Selected = Selected;
    }

    private void MoveBottom()
    {
        ModButtonManager.Insert(Selected, ModButtonManager.ActiveButtons.Count);
        Selected = Selected;
    }

    private void MoveDown()
    {
        if (_focusElement >= FilteredActiveButtons.Count - 1)
        {
            return;
        }

        // is only called from active.
        ModButtonManager.Insert(Selected,
            ModButtonManager.ActiveButtons.IndexOf(FilteredActiveButtons.ElementAt(_focusElement + 1)) + 1);
        Selected = Selected; // sets _focusElement, plays sound.
    }

    private void SelectAt<T>(
        IEnumerable<T> targetMods,
        Vector2 sourceScrollposition,
        Vector2 targetScrollposition) where T : ModButton
    {
        var offset = (_focusElement * ModButtonHeight) - sourceScrollposition.y;
        SelectAt(targetMods, targetScrollposition, offset);
    }

    private void SelectAt<T>(IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f) where T : ModButton
    {
        SelectAt(mods, scrollposition.y + offset);
    }

    private void SelectAt<T>(IEnumerable<T> mods, float position) where T : ModButton
    {
        Selected = mods.Any() ? mods.ElementAt(IndexAt(mods, position)) : null;
    }

    private int IndexAt<T>(IEnumerable<T> mods, Vector2 scrollposition, float offset = 0f) where T : ModButton
    {
        return IndexAt(mods, scrollposition.y + offset);
    }

    private int IndexAt<T>(IEnumerable<T> mods, float position) where T : ModButton
    {
        if (!mods.Any())
        {
            return -1;
        }

        return Mathf.Clamp(Mathf.CeilToInt(position / ModButtonHeight), 0, mods.Count() - 1);
    }

    private void SelectNext<T>(IEnumerable<T> mods) where T : ModButton
    {
        if (!mods.Any())
        {
            return;
        }

        var index = Utilities.Modulo(_focusElement + 1, mods.Count());
        Selected = mods.ElementAt(index);
    }

    private void SelectFirst<T>(IEnumerable<T> mods) where T : ModButton
    {
        if (!mods.Any())
        {
            return;
        }

        Selected = mods.ElementAt(0);
    }

    private void SelectPrevious<T>(IEnumerable<T> mods) where T : ModButton
    {
        if (!mods.Any())
        {
            return;
        }

        var index = Utilities.Modulo(_focusElement - 1, mods.Count());
        Selected = mods.ElementAt(index);
    }

    private void SelectLast<T>(IEnumerable<T> mods) where T : ModButton
    {
        if (!mods.Any())
        {
            return;
        }

        var index = mods.Count() - 1;
        Selected = mods.ElementAt(index);
    }

    private void EnsureVisible(ref Vector2 scrollPosition, int index)
    {
        var min = index * ModButtonHeight;
        var max = (index + 1) * ModButtonHeight;

        if (min < scrollPosition.y)
        {
            scrollPosition.y = min;
        }

        if (max > scrollPosition.y + _scrollViewHeight)
        {
            scrollPosition.y = max - _scrollViewHeight + ModButtonHeight;
        }
    }

    private void DoAvailableMods(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, DarkTheme.Panel);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(canvas);
        GUI.color = Color.white;
        DrawColumnHeader(ref canvas, I18n.AvailableMods);

        var buttons = new List<ModButton>(FilteredAvailableButtons);
        var filterRect = new Rect(
            canvas.xMin,
            canvas.yMin,
            canvas.width,
            FilterHeight);
        var outRect = new Rect(
            canvas.xMin,
            filterRect.yMax + SmallMargin,
            canvas.width,
            canvas.height - FilterHeight - SmallMargin);
        var viewRect = new Rect(
            canvas.xMin,
            filterRect.yMax + SmallMargin,
            canvas.width,
            Mathf.Max(ModButtonHeight * buttons.Count, outRect.height));
        if (viewRect.height > outRect.height)
        {
            viewRect.width -= 18f;
        }

        var modRect = new Rect(
            viewRect.xMin,
            viewRect.yMin,
            viewRect.width,
            ModButtonHeight);
        _scrollViewHeight = (int)outRect.height;

        DoFilterField(filterRect, ref _availableFilter, ref _availableFilterVisible, FocusArea.AvailableFilter);

        var alternate = false;

        // only render rows inside the visible viewport (+1 row of buffer); with large mod lists this is
        // the difference between drawing ~25 rows and several hundred every frame.
        var firstVisible = Mathf.FloorToInt(_availableScrollPosition.y / ModButtonHeight) - 1;
        var lastVisible = Mathf.CeilToInt((_availableScrollPosition.y + outRect.height) / ModButtonHeight) + 1;

        Widgets.BeginScrollView(outRect, ref _availableScrollPosition, viewRect);
        for (var i = 0; i < buttons.Count; i++)
        {
            if (i >= firstVisible && i <= lastVisible)
            {
                var button = buttons[i];
                button.DoModButton(modRect, alternate, () => Selected = button, () => button.Active = true,
                    _availableFilterVisible, _availableFilter);
            }

            alternate = !alternate;
            modRect.y += ModButtonHeight;
        }

        // handle drag & drop
        var dropped = DraggingManager.ContainerUpdate(buttons, viewRect, out var hoverIndex);
        var draggingOverAvailable = hoverIndex >= 0;
        if (draggingOverAvailable != _draggingOverAvailable)
        {
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            _draggingOverAvailable = draggingOverAvailable;
        }

        if (dropped)
        {
            DraggingManager.Dragged.Active = false;
        }

        Widgets.EndScrollView();

        if (!draggingOverAvailable)
        {
            return;
        }

        GUI.color = DarkTheme.Accent;
        Widgets.DrawBox(outRect);
        GUI.color = Color.white;
    }

    private void DoActiveMods(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, DarkTheme.Panel);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(canvas);
        GUI.color = Color.white;
        DrawColumnHeader(ref canvas, I18n.ActiveMods);

        var buttons = FilteredActiveButtons;
        var filterRect = new Rect(
            canvas.xMin,
            canvas.yMin,
            canvas.width,
            FilterHeight);
        var outRect = new Rect(
            canvas.xMin,
            filterRect.yMax + SmallMargin,
            canvas.width,
            canvas.height - FilterHeight - SmallMargin);
        var viewRect = new Rect(
            canvas.xMin,
            filterRect.yMax + SmallMargin,
            canvas.width,
            Mathf.Max(ModButtonHeight * buttons.Count, outRect.height));
        if (viewRect.height > outRect.height)
        {
            viewRect.width -= 18f;
        }

        var modRect = new Rect(
            viewRect.xMin,
            viewRect.yMin,
            viewRect.width,
            ModButtonHeight);

        DoFilterField(filterRect, ref _activeFilter, ref _activeFilterVisible, FocusArea.ActiveFilter);

        var alternate = false;

        var firstVisible = Mathf.FloorToInt(_activeScrollPosition.y / ModButtonHeight) - 1;
        var lastVisible = Mathf.CeilToInt((_activeScrollPosition.y + outRect.height) / ModButtonHeight) + 1;

        Widgets.BeginScrollView(outRect, ref _activeScrollPosition, viewRect);
        if (DraggingManager.ContainerUpdate(buttons, viewRect, out var hoverIndex))
        {
            var dropIndex = hoverIndex;
            // if filtering the active list, figure out the desired index in the source list
            if (FilterActive && hoverIndex > 0)
            {
                var insertBefore = buttons.ElementAtOrDefault(hoverIndex);
                dropIndex = insertBefore == null
                    ? ModButtonManager.ActiveButtons.Count
                    : ModButtonManager.ActiveButtons.IndexOf(insertBefore);
            }

            ModButtonManager.Insert(DraggingManager.Dragged, dropIndex);
        }

        if (hoverIndex != _lastHoverIndex)
        {
            _lastHoverIndex = hoverIndex;
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
        }

        for (var i = 0; i < buttons.Count; i++)
        {
            if (i >= firstVisible && i <= lastVisible)
            {
                var mod = buttons.ElementAt(i);

                mod.DoModButton(modRect, alternate, () => Selected = mod, () => mod.Active = false,
                    _activeFilterVisible, _activeFilter);

                if (hoverIndex == i)
                {
                    GUI.color = DarkTheme.Accent;
                    Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
                    GUI.color = Color.white;
                }
            }

            alternate = !alternate;
            modRect.y += ModButtonHeight;
        }

        if (hoverIndex == buttons.Count)
        {
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(modRect.xMin, modRect.yMin, modRect.width);
            GUI.color = Color.white;
        }

        Widgets.EndScrollView();
    }

    private void DoDetails(Rect canvas)
    {
        if (Selected == null)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = DarkTheme.TextMuted;
            Widgets.Label(canvas, I18n.NoModSelected);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
        else
        {
            Selected.DoModDetails(canvas);
        }
    }

    private void DoFilterField(Rect canvas, ref string filter, ref bool visible, FocusArea focus)
    {
        var rect = canvas.ContractedBy(SmallMargin / 2f);

        // dark inset background for the search field
        Widgets.DrawBoxSolid(rect, DarkTheme.PanelAlt);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;

        var iconRect = new Rect(
            canvas.xMax - SmallIconSize - SmallMargin,
            canvas.yMin + ((canvas.height - SmallIconSize) / 2f),
            SmallIconSize,
            SmallIconSize
        );

        // intercept focus gain events
        if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp)
        {
            _focusArea = focus;
        }

        // handle button interactions before textfield, because textfield eats click events.
        if (!filter.NullOrEmpty())
        {
            if (Widgets.ButtonInvisible(iconRect))
            {
                filter = string.Empty;
            }

            iconRect.x -= SmallIconSize + SmallMargin;
            if (Widgets.ButtonInvisible(iconRect))
            {
                visible = !visible;
            }

            iconRect.x += SmallIconSize + SmallMargin;
        }
        else
        {
            // Unity gets confused if the number of controls changes, and the textfield will
            // lose focus. To avoid this, spawn some dummy controls.
            Widgets.ButtonInvisible(Rect.zero);
            Widgets.ButtonInvisible(Rect.zero);
        }

        // handle textfield
        GUI.SetNextControlName(focus.ToString());
        var newFilter = Widgets.TextField(rect, filter);
        if (newFilter != filter)
        {
            filter = newFilter;
            Notify_FilterChanged();
        }

        // draw buttons over textfield.
        // Note that these buttons _cannot_ be clicked, but the ButtonImage
        // code is a handy shortcut for mouseOver interactions.
        if (!filter.NullOrEmpty())
        {
            if (Widgets.ButtonImage(iconRect, Status_Cross))
            {
                filter = string.Empty;
                Notify_FilterChanged();
            }

            iconRect.x -= SmallIconSize + SmallMargin;
            if (!Widgets.ButtonImage(iconRect, visible ? EyeClosed : EyeOpen))
            {
                return;
            }

            visible = !visible;
            Notify_FilterChanged();
        }
        else
        {
            // search icon, plus more dummy controls (filter is called multiple times).
            GUI.DrawTexture(iconRect, Search);
            Widgets.ButtonInvisible(Rect.zero);
            Widgets.ButtonInvisible(Rect.zero);
        }
    }

    private void Notify_FilterChanged()
    {
        // filter was changed, which means Selected may now be invisible, and index may have changed.
        if (ModButtonManager.ActiveButtons.Contains(Selected))
        {
            if (!FilteredActiveButtons.Contains(Selected))
            {
                _selected = FilteredActiveButtons.FirstOrDefault();
                _focusElement = 0;
            }
            else
            {
                _focusElement = FilteredActiveButtons.FirstIndexOf(b => b == _selected);
            }
        }

        var available = Selected;
        if (available == null)
        {
            return;
        }

        if (!ModButtonManager.AvailableButtons.Contains(available))
        {
            return;
        }

        if (!FilteredAvailableButtons.Contains(available))
        {
            _selected = FilteredAvailableButtons.FirstOrDefault();
            _focusElement = 0;
        }
        else
        {
            _focusElement = FilteredAvailableButtons.FirstIndexOf(b => b == _selected);
        }
    }

    public override void PreOpen()
    {
        base.PreOpen();

        _activeModsHash = ModLister.InstalledModsListHash(true);
        ModButtonManager.InitializeModButtons();
        ModButtonManager.Notify_RecacheModMetaData();
        ModButtonManager.Notify_RecacheIssues();
        Selected = ModButtonManager.AvailableButtons.FirstOrDefault() ??
                   ModButtonManager.ActiveButtons.FirstOrDefault();
    }

    public override void OnAcceptKeyPressed()
    {
        // for some reason, even though closeOnAccept = false, the window still closes.
        // So, let's just override this with nothing.
    }

    public override void Close(bool doCloseSound = true)
    {
        if (ModButtonManager.AnyIssue)
        {
            ConfirmModIssues();
        }
        else
        {
            Find.WindowStack.TryRemove(this, doCloseSound);
        }
    }

    public override bool OnCloseRequest()
    {
        return true;
    }


    public override void PostClose()
    {
        ModsConfig.Save();
        CheckModListChanged();
    }

    private void ConfirmModIssues()
    {
        var issues = ModButtonManager.Issues.Where(i => i.Severity > 1);
        var issueList = "";
        foreach (var buttonIssues in issues.GroupBy(i => i.parent)
                     .OrderByDescending(bi => bi.Max(i => i.Severity))
                     .ThenBy(bi => bi.Key.Mod.Name))
        {
            issueList += $"{buttonIssues.Key.Mod.Name}\n";
            foreach (var issue in buttonIssues.Where(i => i.Severity > 1)
                         .OrderByDescending(i => i.Severity))
            {
                issueList += $"{issue.Tooltip.CapitalizeFirst().Colorize(issue.Color)}\n";
            }

            issueList += "\n";
        }

        var title = I18n.DialogConfirmIssuesTitle(issues.Count());
        var text =
            I18n.DialogConfirmIssues(issues.Any(i => i.Severity >= 3) ? I18n.DialogConfirmIssuesCritical : "",
                issueList);
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, close, true, title));
        return;

        void close()
        {
            Find.WindowStack.TryRemove(this);
        }
    }

    private void CheckModListChanged()
    {
        if (_activeModsHash == ModLister.InstalledModsListHash(true))
        {
            return;
        }

        Find.WindowStack.Add(
            new Dialog_MessageBox(
                I18n.ModsChanged,
                I18n.Later,
                null,
                I18n.OK,
                restart,
                null,
                true,
                restart,
                restart));
        return;

        static void restart()
        {
            GenCommandLine.Restart();
        }
    }

    private enum FocusArea
    {
        AvailableFilter,
        Available,
        ActiveFilter,
        Active
    }
}