// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using ColourPicker;
using HarmonyLib;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;
using static ModManager.Constants;
using static ModManager.Resources;
using static ModManager.Utilities;

namespace ModManager;

public sealed class ModButton_Installed : ModButton
{
    private Vector2 _descriptionScrollPosition = Vector2.zero;
    private Vector2 _previewScrollPosition = Vector2.zero;
    private ModMetaData _selected;

    // Cached status-dot color + tooltip. Both derive from Requirements (and the constant VersionCompatible),
    // so they're recomputed only when issues recache or the selected version changes — never per frame.
    private Color? _statusDotColor;
    private string _statusDotTip;

    private List<FloatMenuOption> _titleLinkOptions;

    private ModButton_Installed(ModMetaData mod)
    {
        if (mod == null)
        {
            throw new ArgumentNullException(nameof(mod));
        }

        Versions.Add(mod);
    }

    public ModButton_Installed(IEnumerable<ModMetaData> mods)
    {
        if (mods == null || !mods.Any())
        {
            throw new ArgumentNullException(nameof(mods));
        }

        Versions = mods.ToList();
    }

    public override int SortOrder => Selected.Compatibility();
    private List<ModList> Lists => ModListManager.ListsFor(this);

    public override int LoadOrder => Selected?.LoadOrder() ?? base.LoadOrder;

    private IEnumerable<ModMetaData> VersionsOrdered => Versions
        .OrderByDescending(mod => mod.Compatibility())
        .ThenBy(mod => mod.Source);

    public override string Name => Selected?.Name;
    public override string Identifier => Selected?.PackageId;
    public override ulong SteamWorkshopId => Selected?.GetPublishedFileId().m_PublishedFileId ?? 0;

    public override bool Active
    {
        get => Versions.Any(mod => mod.Active);
        set
        {
            Selected.Active = value;
            ModButtonManager.Notify_ActiveStatusChanged(this, value);
        }
    }

    public List<ModMetaData> Versions { get; } = [];
    public Manifest Manifest => Manifest.For(Selected);

    public ModMetaData Selected
    {
        get
        {
            _selected ??= Versions.FirstOrDefault(m => m.Active) ?? VersionsOrdered.FirstOrDefault();

            return _selected;
        }
        private set
        {
            if (value != null)
            {
                value.Active = Selected?.Active ?? false;
            }

            if (Selected != null)
            {
                Selected.Active = false;
            }

            _selected = value;
            _titleLinkOptions = null;
            ModButtonManager.Notify_ModListChanged();
        }
    }

    public Color Color
    {
        get
        {
            // use version colour if set
            if (ModManager.UserData[Selected].Color != Color.white)
            {
                return ModManager.UserData[Selected].Color;
            }

            // then button colour
            if (ModManager.UserData[this].Color != Color.white)
            {
                return ModManager.UserData[this].Color;
            }

            // if this mod is included in any lists, use that colour
            if (Lists.NullOrEmpty())
            {
                return Color.white;
            }

            var colours = Lists.Select(l => l.Color)
                .Where(c => c != Color.white);
            if (colours.Any())
            {
                return colours.Aggregate((a, b) => a + b) / colours.Count();
            }

            // if nothing stuck, use default
            return Color.white;
        }
        set => ModManager.UserData[this].Color = value;
    }

    public override bool IsCoreMod => Selected?.IsCoreMod ?? false;

    public override bool IsExpansion => !IsCoreMod && (Selected?.Official ?? false);

    public override bool IsModManager => Selected != null && Selected.SamePackageId("Mlie.ModManager");

    private List<FloatMenuOption> TitleLinkOptions
    {
        get
        {
            if (_titleLinkOptions != null)
            {
                return _titleLinkOptions;
            }

            _titleLinkOptions = NewOptionsList;
            if (!Selected?.Url.NullOrEmpty() ?? false)
            {
                _titleLinkOptions.Add(new FloatMenuOption(I18n.ModHomePage(Selected.Url),
                    () => Application.OpenURL(Selected.Url)));
            }

            PublishedFileId_t publishedFileId;
            if (Selected?.Source == ContentSource.SteamWorkshop)
            {
                publishedFileId = Selected.GetPublishedFileId();
                var id = publishedFileId;
                _titleLinkOptions.Add(
                    new FloatMenuOption(I18n.WorkshopPage(Selected.Name),
                        () => SteamUtility.OpenWorkshopPage(id)));
            }

            var source = Selected?.UserData()?.Source;
            if (Selected?.Source != ContentSource.ModsFolder || source == null)
            {
                return _titleLinkOptions;
            }

            publishedFileId = source.GetPublishedFileId();
            _titleLinkOptions.Add(
                new FloatMenuOption(I18n.WorkshopPage(source.Name),
                    () => SteamUtility.OpenWorkshopPage(publishedFileId)));

            return _titleLinkOptions;
        }
    }

    public override IEnumerable<Dependency> Requirements => Manifest?.Requirements ?? Manifest.EmptyRequirementList;

    public static ModButton_Installed For(ModMetaData mod)
    {
        var button = ModButtonManager.AllButtons.OfType<ModButton_Installed>()
            .FirstOrDefault(mb =>
                mb.Name == mod.Name || ModManager.Settings.TrimTags && mb.TrimmedName == TrimModName(mod.Name));
        if (button == null)
        {
            return new ModButton_Installed(mod);
        }

        if (!button.Versions.Contains(mod))
        {
            button.Versions.Add(mod);
        }

        return button;
    }

    public override int MatchesFilter(string filter)
    {
        if (base.MatchesFilter(filter) > 0)
        {
            return 1;
        }

        return Selected.AuthorsString.ToUpperInvariant().Contains(filter.ToUpperInvariant()) ? 2 : 0;
    }

    public override bool SamePackageId(string packageId)
    {
        return Selected?.SamePackageId(packageId) ?? false;
    }

    public override void DoModButton(
        Rect canvas,
        bool alternate = false,
        Action clickAction = null,
        Action doubleClickAction = null,
        bool deemphasizeFiltered = false,
        string filter = null)
    {
        base.DoModButton(canvas, alternate, clickAction, doubleClickAction, deemphasizeFiltered, filter);

        canvas = canvas.ContractedBy(SmallMargin / 2f).Rounded();

        var active = Active;

        // left gutter: (active only) load-order number + drag handle, then the status dot
        var x = canvas.xMin;
        if (active)
        {
            var lo = LoadOrder;
            if (lo >= 0)
            {
                var numRect = new Rect(x, canvas.yMin, 22f, canvas.height);
                var nf = Text.Font;
                var na = Text.Anchor;
                var nc = GUI.color;
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleRight;
                GUI.color = DarkTheme.TextMuted;
                Widgets.Label(numRect, (lo + 1).ToString());
                Text.Font = nf;
                Text.Anchor = na;
                GUI.color = nc;
            }

            x += 22f + (SmallMargin / 2f);
            DrawDragHandle(new Rect(x, canvas.yMin, DragHandleWidth, canvas.height));
            x += DragHandleWidth + (SmallMargin / 2f);
        }

        // status dot (green/yellow/red), vertically centered
        var dotRect = new Rect(x, canvas.yMin + ((canvas.height - DotSize) / 2f), DotSize, DotSize);
        DrawDot(dotRect, StatusDotColor());
        TooltipHandler.TipRegion(dotRect, StatusDotTip());
        x += DotSize + SmallMargin;

        var contentX = x;
        var contentW = canvas.xMax - contentX;

        var nameRect = new Rect(
            contentX,
            canvas.yMin,
            contentW - ((SmallIconSize + SmallMargin) * Versions.Count),
            canvas.height * 3 / 5f);
        var authorRect = new Rect(
            contentX,
            nameRect.yMax,
            contentW,
            canvas.height * 2 / 5f);
        var sourceIconsRect = new Rect(
            nameRect.xMax,
            canvas.yMin,
            (SmallIconSize + SmallMargin) * Versions.Count,
            nameRect.height);

        var deemphasized = deemphasizeFiltered && !filter.NullOrEmpty() && MatchesFilter(filter) <= 0;
        GUI.color = deemphasized || !Selected.enabled ? Color.Desaturate() : Color;

        Text.Anchor = TextAnchor.MiddleLeft;
        Text.Font = GameFont.Small;
        var nameCache = active ? _activeModNameTruncationCache : _modNameTruncationCache;
        Widgets.Label(nameRect, TrimmedName.Truncate(nameRect.width, nameCache));
        if (Mouse.IsOver(nameRect) && TrimmedName != TrimmedName.Truncate(nameRect.width, nameCache))
        {
            TooltipHandler.TipRegion(nameRect, TrimmedName);
        }

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Tiny;
        GUI.color = DarkTheme.TextMuted;
        Widgets.Label(authorRect, Selected.AuthorsString);
        GUI.color = Color.white;
        DoSourceButtons(sourceIconsRect);

        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        // floatmenu
        if (Event.current.type == EventType.MouseUp &&
            Event.current.button == 1 &&
            Mouse.IsOver(canvas) &&
            !Mouse.IsOver(sourceIconsRect))
        {
            DoModActionFloatMenu();
        }
    }

    private static void DrawDragHandle(Rect rect)
    {
        const float d = 2f;
        const float gapX = 3f;
        const float gapY = 3f;
        var cx = rect.center.x;
        var cy = rect.center.y;
        var xs = new[] { cx - (gapX / 2f) - d, cx + (gapX / 2f) };
        var ys = new[] { cy - (d + gapY) - (d / 2f), cy - (d / 2f), cy + (d + gapY) - (d / 2f) };
        foreach (var px in xs)
        {
            foreach (var py in ys)
            {
                Widgets.DrawBoxSolid(new Rect(px, py, d, d), DarkTheme.TextMuted);
            }
        }
    }

    private Color StatusDotColor()
    {
        if (_statusDotColor.HasValue)
        {
            return _statusDotColor.Value;
        }

        var worst = 0;
        foreach (var req in Requirements)
        {
            if (req.Severity > worst)
            {
                worst = req.Severity;
            }
        }

        Color color;
        if (worst >= SeverityThreshold)
        {
            color = DarkTheme.DotRed;
        }
        else if (worst >= 1 || !Selected.VersionCompatible)
        {
            color = DarkTheme.DotYellow;
        }
        else
        {
            color = DarkTheme.DotGreen;
        }

        _statusDotColor = color;
        return color;
    }

    private string StatusDotTip()
    {
        if (_statusDotTip != null)
        {
            return _statusDotTip;
        }

        var versionTip = Selected.VersionCompatible ? I18n.CurrentVersion : I18n.DifferentVersion(Selected);
        var issues = Requirements.Where(r => r.Severity >= 1).Select(r => r.Tooltip).ToList();
        _statusDotTip = issues.Any() ? $"{versionTip}\n{issues.StringJoin("\n")}" : versionTip;
        return _statusDotTip;
    }

    private string GetVersionTip(ModMetaData mod)
    {
        return mod.VersionCompatible ? I18n.CurrentVersion : I18n.DifferentVersion(mod);
    }

    private void DoSourceButtons(Rect canvas)
    {
        var iconRect = new Rect(
            canvas.xMax - SmallIconSize,
            canvas.yMin,
            SmallIconSize,
            SmallIconSize).CenteredOnYIn(canvas);

        var singleVersion = VersionsOrdered.Count() == 1;
        foreach (var mod in VersionsOrdered)
        {
            var icon = mod.Source.GetIcon();
            var color = mod.VersionCompatible ? Color.white : Color.red;
            GUI.color = color;
            if (singleVersion)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            else
            {
                if (Widgets.ButtonImage(iconRect, icon, mod == Selected ? color : color.Desaturate()))
                {
                    Selected = mod;
                }
            }

            TooltipHandler.TipRegion(iconRect, () => GetVersionTip(mod), mod.GetHashCode());
            iconRect.x -= SmallIconSize + SmallMargin;
        }
    }

    internal override void DoModActionButtons(Rect canvas)
    {
        Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
        canvas = canvas.ContractedBy(SmallMargin / 2f);

        if (IsCoreMod)
        {
            return;
        }

        var iconRect = new Rect(
            canvas.xMax - IconSize,
            canvas.yMin,
            IconSize,
            IconSize);

        if (ModListManager.ListsFor(this).Count < ModListManager.ModLists.Count)
        {
            if (ButtonIcon(ref iconRect, File, I18n.AddToModList, Status_Plus))
            {
                ModListManager.DoAddToModListFloatMenu(this);
            }
        }

        if (ModListManager.ListsFor(this).Any())
        {
            if (ButtonIcon(ref iconRect, File, I18n.RemoveFromModList, Status_Cross,
                    mouseOverColor: Color.red))
            {
                ModListManager.DoRemoveFromModListFloatMenu(this);
            }
        }

        if (Selected.Source == ContentSource.SteamWorkshop)
        {
            if (ButtonIcon(ref iconRect, Steam, I18n.UnSubscribe, Status_Cross, Direction8Way.NorthWest,
                    Color.red))
            {
                Workshop.Unsubscribe(Selected);
            }

            if (ButtonIcon(ref iconRect, Folder, I18n.CreateLocalCopy(Selected.Name), Status_Plus))
            {
                IO.CreateLocalCopy(Selected);
            }
        }

        if (Selected.Source == ContentSource.ModsFolder && !Selected.IsCoreMod)
        {
            if (ButtonIcon(ref iconRect, Folder, I18n.DeleteLocalCopy(Selected.Name),
                    Status_Cross, Direction8Way.NorthEast, Color.red))
            {
                IO.DeleteLocal(Selected);
            }
        }

        if (Prefs.DevMode && SteamManager.Initialized && Selected.CanToUploadToWorkshop())
        {
            if (ButtonIcon(ref iconRect, Steam, Verse.Steam.Workshop.UploadButtonLabel(Selected.GetPublishedFileId()),
                    Status_Up, Direction8Way.NorthWest))
            {
                Workshop.Upload(Selected);
            }
        }

        if (ButtonIcon(ref iconRect, Palette, I18n.ChangeColour))
        {
            var options = NewOptionsList;
            options.Add(new FloatMenuOption(I18n.ChangeModColour(Name), () => Find.WindowStack.Add(
                new Dialog_ColourPicker(Color, color =>
                    ModManager.UserData[Selected].Color = color
                ))));
            options.Add(new FloatMenuOption(I18n.ChangeButtonColour(Name), () => Find.WindowStack.Add(
                new Dialog_ColourPicker(Color, color =>
                    ModManager.UserData[this].Color = color
                ))));
            FloatMenu(options);
        }

        if (Selected.HasSettings() && ButtonIcon(ref iconRect, Gear, I18n.ModSettings))
        {
            OpenSettingsFor(Selected);
        }
    }

    private void DoModActionFloatMenu()
    {
        var options = NewOptionsList;
        if (ModListManager.ListsFor(this).Count < ModListManager.ModLists.Count)
        {
            options.Add(new FloatMenuOption(I18n.AddToModList,
                () => ModListManager.DoAddToModListFloatMenu(this)));
        }

        if (ModListManager.ListsFor(this).Any())
        {
            options.Add(new FloatMenuOption(I18n.RemoveFromModList,
                () => ModListManager.DoRemoveFromModListFloatMenu(this)));
        }

        if (Selected.Source == ContentSource.SteamWorkshop)
        {
            options.Add(new FloatMenuOption(I18n.UnSubscribe, () => Workshop.Unsubscribe(Selected)));
            options.Add(new FloatMenuOption(I18n.CreateLocalCopy(Selected.Name),
                () => IO.CreateLocalCopy(Selected)));
        }

        if (Selected.Source == ContentSource.ModsFolder && !Selected.IsCoreMod)
        {
            options.Add(new FloatMenuOption(I18n.DeleteLocalCopy(Selected.Name),
                () => IO.DeleteLocal(Selected)));
        }

        if (Prefs.DevMode && SteamManager.Initialized && Selected.CanToUploadToWorkshop())
        {
            options.Add(new FloatMenuOption(
                Verse.Steam.Workshop.UploadButtonLabel(Selected.GetPublishedFileId()),
                () => Workshop.Upload(Selected)));
        }

        options.Add(new FloatMenuOption(I18n.ChangeColour, () =>
        {
            var options2 = NewOptionsList;
            options2.Add(new FloatMenuOption(I18n.ChangeModColour(Name), () => Find.WindowStack.Add(
                new Dialog_ColourPicker(Color,
                    color =>
                        ModManager.UserData[Selected].Color = color
                ))));
            options2.Add(new FloatMenuOption(I18n.ChangeButtonColour(Name), () => Find.WindowStack.Add(
                new Dialog_ColourPicker(Color,
                    color => ModManager.UserData[this].Color = color
                ))));
            FloatMenu(options2);
        }));
        if (Selected.HasSettings())
        {
            options.Add(new FloatMenuOption(I18n.ModSettings, () => OpenSettingsFor(Selected)));
        }

        if (Prefs.DevMode)
        {
            options.Add(new FloatMenuOption("Open mod directory",
                () => Application.OpenURL(Selected.RootDir.FullName)));
        }

        FloatMenu(options);
    }

    internal override void DoModDetails(Rect canvas)
    {
        var mod = Selected;
        if (!mod.PreviewImage.NullOrBad())
        {
            DoLabel(ref canvas, I18n.Preview);
            var width = mod.PreviewImage.width;
            var height = mod.PreviewImage.height;
            var scale = canvas.width / width;
            var viewRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                width * scale,
                height * scale);
            var outRect = new Rect(
                canvas.xMin,
                canvas.yMin,
                canvas.width,
                Mathf.Min(viewRect.height, canvas.width / GoldenRatio, Page.StandardSize.x * 3 / 5f / GoldenRatio));
            if (viewRect.height > outRect.height)
            {
                viewRect.xMax -= 18f;
            }

            Widgets.BeginScrollView(outRect, ref _previewScrollPosition, viewRect);
            GUI.DrawTexture(viewRect, mod.PreviewImage);
            Widgets.EndScrollView();
            canvas.yMin = outRect.yMax + SmallMargin;
        }
        else
        {
            DoLabel(ref canvas, I18n.Details);
        }

        // hero: large mod name + small muted author
        const float heroNameH = LineHeight + 12f; // ~Medium line height
        var heroRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, heroNameH + LineHeight + (SmallMargin * 2));
        Widgets.DrawBoxSolid(heroRect, SlightlyDarkBackground);
        canvas.yMin = heroRect.yMax + SmallMargin;
        var heroInner = heroRect.ContractedBy(SmallMargin);

        var nameRect = new Rect(heroInner.xMin, heroInner.yMin, heroInner.width, heroNameH);
        var authorRect = new Rect(heroInner.xMin, nameRect.yMax, heroInner.width, LineHeight);

        var oldFont = Text.Font;
        var oldAnchor = Text.Anchor;
        var oldColor = GUI.color;

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = DarkTheme.TextPrimary;
        Widgets.Label(nameRect, mod.Name.Truncate(nameRect.width));
        if (TitleLinkOptions.Any())
        {
            ActionButton(nameRect, () => FloatMenu(TitleLinkOptions));
        }

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = DarkTheme.TextMuted;
        if (!mod.AuthorsString.NullOrEmpty())
        {
            Widgets.Label(authorRect, mod.AuthorsString.Truncate(authorRect.width));
            var steamMod = mod.Source switch
            {
                ContentSource.ModsFolder => mod.UserData()?.Source,
                ContentSource.SteamWorkshop => mod,
                _ => null
            };
            if (steamMod != null && SteamManager.Initialized)
            {
                var authorId = Traverse.Create(steamMod.GetWorkshopItemHook())
                    .Field("steamAuthor")
                    .GetValue<CSteamID>();
                ActionButton(authorRect,
                    () => SteamUtility.OpenUrl(
                        $"https://steamcommunity.com/profiles/{authorId.GetAccountID().m_AccountID}/myworkshopfiles/"));
            }
        }

        Text.Font = oldFont;
        Text.Anchor = oldAnchor;
        GUI.color = oldColor;

        // chips: version / target version(s) / compatibility
        DoDetailChips(ref canvas, mod);

        DrawRequirements(ref canvas);

        CrossPromotionManager.HandleCrossPromotions(ref canvas, Selected);

        // bottom action row (Workshop / Local copy / Deactivate), reserved before the description
        DoDetailActionButtons(ref canvas);

        // description (guard against a negative-height rect after the reservations above)
        if (canvas.height <= 0f)
        {
            return;
        }

        Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
        var descriptionOutRect = canvas.ContractedBy(SmallMargin).Rounded();

        var height2 = Text.CalcHeight(mod.Description, descriptionOutRect.width);
        var descriptionViewRect = new Rect(
            descriptionOutRect.xMin,
            descriptionOutRect.yMin,
            descriptionOutRect.width,
            height2);
        if (height2 > descriptionOutRect.height)
        {
            descriptionViewRect.xMax -= 18f;
        }

        Widgets.BeginScrollView(descriptionOutRect, ref _descriptionScrollPosition, descriptionViewRect);
        Widgets.Label(descriptionViewRect, mod.Description);
        Widgets.EndScrollView();
    }

    private void DoDetailChips(ref Rect canvas, ModMetaData mod)
    {
        var chipY = canvas.yMin;
        var chipX = canvas.xMin;
        var oldFont = Text.Font;
        var oldAnchor = Text.Anchor;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;

        void Chip(string text, Color tint, string tip)
        {
            if (text.NullOrEmpty())
            {
                return;
            }

            var w = Text.CalcSize(text).x + (SmallMargin * 2);
            var r = new Rect(chipX, chipY, w, ChipHeight);
            Widgets.DrawBoxSolid(r, DarkTheme.ChipBG);
            GUI.color = DarkTheme.Border;
            Widgets.DrawBox(r);
            GUI.color = tint;
            Widgets.Label(r, text);
            GUI.color = Color.white;
            if (!tip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(r, tip);
            }

            chipX += w + (SmallMargin / 2f);
        }

        if (Manifest.HasVersion)
        {
            Chip($"v{Manifest.Version}", DarkTheme.TextMuted, null);
        }

        var targetVersions = mod.SupportedVersionsReadOnly.VersionList();
        Chip(targetVersions, DarkTheme.TextMuted, I18n.TargetVersions(targetVersions));
        Chip(mod.VersionCompatible ? I18n.CurrentVersion : targetVersions, StatusDotColor(), GetVersionTip(mod));

        Text.Font = oldFont;
        Text.Anchor = oldAnchor;
        GUI.color = Color.white;
        canvas.yMin = chipY + ChipHeight + SmallMargin;
    }

    private void DoDetailActionButtons(ref Rect canvas)
    {
        // which actions apply (no per-frame allocation: no list/tuples/lambdas)
        var showWorkshop = TitleLinkOptions.Any();
        var showLocalCopy = Selected.Source == ContentSource.SteamWorkshop;
        var showDeactivate = Active;
        var count = (showWorkshop ? 1 : 0) + (showLocalCopy ? 1 : 0) + (showDeactivate ? 1 : 0);

        const float btnH = 28f;
        if (count == 0 || canvas.height < btnH + SmallMargin)
        {
            return;
        }

        var buttonRow = new Rect(canvas.xMin, canvas.yMax - btnH, canvas.width, btnH);
        canvas.yMax = buttonRow.yMin - SmallMargin;

        var cell = (buttonRow.width - (SmallMargin * (count - 1))) / count;
        var i = 0;

        Rect NextCell()
        {
            return new Rect(buttonRow.xMin + (i++ * (cell + SmallMargin)), buttonRow.yMin, cell, btnH);
        }

        // primary action is filled accent; the rest are flat panels with coloured labels
        if (showWorkshop && DrawActionButton(NextCell(), I18n.ActionWorkshop, DarkTheme.Accent, Color.white))
        {
            FloatMenu(TitleLinkOptions);
        }

        if (showLocalCopy &&
            DrawActionButton(NextCell(), I18n.ActionLocalCopy, DarkTheme.PanelAlt, DarkTheme.TextPrimary))
        {
            IO.CreateLocalCopy(Selected);
        }

        if (showDeactivate &&
            DrawActionButton(NextCell(), I18n.ActionDeactivate, DarkTheme.PanelAlt, DarkTheme.DotRed))
        {
            Active = false;
        }
    }

    /// <summary>Flat dark-theme button: solid <paramref name="bg" /> (lightened on hover), hairline border,
    /// centred <paramref name="fg" /> label. Returns true when clicked.</summary>
    private static bool DrawActionButton(Rect rect, string label, Color bg, Color fg)
    {
        var hover = Mouse.IsOver(rect);
        Widgets.DrawBoxSolid(rect, hover ? Lighten(bg) : bg);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);

        var oldFont = Text.Font;
        var oldAnchor = Text.Anchor;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = fg;
        Widgets.Label(rect, label);
        GUI.color = Color.white;
        Text.Font = oldFont;
        Text.Anchor = oldAnchor;

        return Widgets.ButtonInvisible(rect);
    }

    private static Color Lighten(Color c)
    {
        return new Color(Mathf.Min(c.r + 0.08f, 1f), Mathf.Min(c.g + 0.08f, 1f), Mathf.Min(c.b + 0.08f, 1f), c.a);
    }

    public override void Notify_RecacheIssues()
    {
        base.Notify_RecacheIssues();
        _statusDotColor = null;
        _statusDotTip = null;
    }

    public void Notify_ResetSelected()
    {
        _selected = null;
        _statusDotColor = null;
        _statusDotTip = null;
    }

    public void Notify_VersionAdded(ModMetaData version, bool active = false)
    {
        Versions.TryAdd(version);
        if (active && Selected.Active)
        {
            Selected.Active = false;
        }

        version.Active = active;
        Selected = version;
        if (active)
        {
            ModButtonManager.Notify_ModListChanged();
        }
    }

    public void Notify_VersionRemoved(ModMetaData version)
    {
        Versions.TryRemove(version);
        if (!Versions.Any())
        {
            ModButtonManager.TryRemove(this);
            if (Page_BetterModConfig.Instance.Selected == this)
            {
                Page_BetterModConfig.Instance.Selected = null;
            }

            return;
        }

        if (Selected == version)
        {
            _selected = null;
        }

        Selected.Active = version.Active;
    }
}