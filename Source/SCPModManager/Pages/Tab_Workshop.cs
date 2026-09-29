// Tab_Workshop.cs
// In-game Steam Workshop browser: text search, type/version/custom tag filters, sort order and trend period,
// paged results with previews, and a details pane with subscribe / activate / open actions.

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;
using static SCPModManager.Constants;
using static SCPModManager.Resources;

namespace SCPModManager;

public sealed class Tab_Workshop : ManagerTab
{
    private const string SearchControl = "SCPModManager_WorkshopSearch";
    private const string TagControl = "SCPModManager_WorkshopTag";

    private static readonly string[] TypeTags = ["Mod", "Scenario", "Translation"];
    private static readonly uint[] TrendPeriods = [1, 7, 30, 90, 180, 365];

    private readonly List<string> _customTags = [];
    private readonly WorkshopSearchParams _params = new();
    private string _customTag = string.Empty;
    private Vector2 _descriptionScroll = Vector2.zero;
    private float _descriptionHeight;
    private WorkshopItemInfo _descriptionHeightFor;
    private float _descriptionHeightWidth;
    private bool _hideInstalled;
    private Vector2 _listScroll = Vector2.zero;
    private bool _searchedOnce;
    private int _seenGeneration = -1;
    private WorkshopItemInfo _selected;
    private string _text = string.Empty;
    private float _textChangedAt = -1f;

    public override string Label => I18n.TabWorkshop;

    public override string Tooltip => I18n.TabWorkshopTip;

    private static string CurrentVersionTag => $"{VersionControl.CurrentMajor}.{VersionControl.CurrentMinor}";

    public override void OnOpened()
    {
        if (_searchedOnce || !WorkshopSearch.Available)
        {
            return;
        }

        // sensible first page: trending mods for the running game version
        _params.Tags.Add("Mod");
        _params.Tags.Add(CurrentVersionTag);
        RunSearch();
    }

    public override void FocusSearch()
    {
        GUI.FocusControl(SearchControl);
    }

    /// <summary>Run a text search (e.g. for a missing mod), relevance-sorted and without tag filters.</summary>
    public void SearchFor(string text)
    {
        _text = text ?? string.Empty;
        _textChangedAt = -1f;
        _params.Text = _text.Trim();
        _params.Tags.Clear();
        _params.Sort = WorkshopSort.Relevance;
        _params.Page = 1;
        _searchedOnce = true;
        if (WorkshopSearch.Available)
        {
            RunSearch();
        }
    }

    public override void DoContents(Rect canvas)
    {
        HandleDebounce();
        HandleEnterKey();
        SyncSelection();

        var toolbar = new Rect(canvas.xMin, canvas.yMin, canvas.width, ButtonHeight);
        DoToolbar(toolbar);

        var filters = new Rect(canvas.xMin, toolbar.yMax + SmallMargin, canvas.width, canvas.height);
        var filtersHeight = DoFilterChips(filters);

        var footer = new Rect(canvas.xMin, canvas.yMax - ButtonHeight, canvas.width, ButtonHeight);
        var body = new Rect(canvas.xMin, filters.yMin + filtersHeight + SmallMargin, canvas.width,
            footer.yMin - SmallMargin - (filters.yMin + filtersHeight + SmallMargin));

        if (!WorkshopSearch.Available)
        {
            DoSteamUnavailable(body);
            return;
        }

        var listWidth = Mathf.Floor(body.width * 0.58f);
        var listRect = new Rect(body.xMin, body.yMin, listWidth, body.height);
        var detailRect = new Rect(listRect.xMax + SmallMargin, body.yMin, body.width - listWidth - SmallMargin,
            body.height);

        DoResultList(listRect);
        DoDetails(detailRect);
        DoFooter(footer);
    }

    private void HandleDebounce()
    {
        if (_textChangedAt < 0f || Time.realtimeSinceStartup - _textChangedAt < SearchDebounceMs / 1000f)
        {
            return;
        }

        _textChangedAt = -1f;
        _params.Text = _text.Trim();
        if (!_params.Text.NullOrEmpty() && _params.Sort == WorkshopSort.Trending)
        {
            // trending ignores relevance; text searches read better sorted by relevance
            _params.Sort = WorkshopSort.Relevance;
        }

        _params.Page = 1;
        RunSearch();
    }

    private void HandleEnterKey()
    {
        if (Event.current.type != EventType.KeyDown ||
            Event.current.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
        {
            return;
        }

        var focused = GUI.GetNameOfFocusedControl();
        if (focused == SearchControl)
        {
            _textChangedAt = 0.001f; // run on this frame's debounce check next frame
            Event.current.Use();
        }
        else if (focused == TagControl)
        {
            AddCustomTag();
            Event.current.Use();
        }
    }

    private void SyncSelection()
    {
        if (_seenGeneration == WorkshopSearch.Generation)
        {
            return;
        }

        _seenGeneration = WorkshopSearch.Generation;
        _listScroll = Vector2.zero;
        if (_selected == null || WorkshopSearch.Results.All(r => r.FileId != _selected.FileId))
        {
            _selected = VisibleResults().FirstOrDefault();
            _descriptionScroll = Vector2.zero;
        }
    }

    private void RunSearch()
    {
        _searchedOnce = true;
        WorkshopSearch.Search(_params);
    }

    private IEnumerable<WorkshopItemInfo> VisibleResults()
    {
        return _hideInstalled
            ? WorkshopSearch.Results.Where(r => r.InstalledMod == null)
            : WorkshopSearch.Results;
    }

    private void DoToolbar(Rect rect)
    {
        var x = rect.xMax;
        if (DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.WorkshopOpenInSteam, tip: _params.BrowserUrl))
        {
            SteamUtility.OpenUrl(_params.BrowserUrl);
        }

        var right = x;
        var dropdownX = right;
        // lay out the dropdowns right-to-left by measuring first
        var sortLabel = $"{I18n.WorkshopSortLabel}: {SortLabel(_params.Sort)}";
        var sortWidth = DarkWidgets.ButtonWidth($"{sortLabel} ▾");
        string periodLabel = null;
        var periodWidth = 0f;
        if (_params.Sort == WorkshopSort.Trending)
        {
            periodLabel = I18n.WorkshopTrendDays(_params.TrendDays);
            periodWidth = DarkWidgets.ButtonWidth($"{periodLabel} ▾") + SmallMargin;
        }

        dropdownX -= sortWidth + periodWidth;
        var searchButtonWidth = DarkWidgets.ButtonWidth(I18n.WorkshopSearch);
        var searchRect = new Rect(rect.xMin, rect.yMin, dropdownX - rect.xMin - searchButtonWidth - (SmallMargin * 2),
            rect.height);

        if (DarkWidgets.SearchField(searchRect, ref _text, SearchControl, I18n.WorkshopSearchPlaceholder))
        {
            _textChangedAt = Time.realtimeSinceStartup;
        }

        var bx = searchRect.xMax + SmallMargin;
        if (DarkWidgets.ButtonAuto(ref bx, rect.yMin, I18n.WorkshopSearch, true))
        {
            _textChangedAt = 0.001f;
        }

        var dx = dropdownX;
        DarkWidgets.Dropdown(ref dx, rect.yMin, sortLabel, DoSortMenu);
        if (periodLabel != null)
        {
            DarkWidgets.Dropdown(ref dx, rect.yMin, periodLabel, DoTrendMenu);
        }
    }

    private float DoFilterChips(Rect canvas)
    {
        var x = canvas.xMin;
        var y = canvas.yMin;
        var rowHeight = ChipHeight + 2 + (SmallMargin / 2f);

        void NewLineIfNeeded(float width)
        {
            if (x + width <= canvas.xMax || x <= canvas.xMin)
            {
                return;
            }

            x = canvas.xMin;
            y += rowHeight;
        }

        void Caption(string text)
        {
            var width = DarkWidgets.ChipWidth(text);
            NewLineIfNeeded(width + 60f);
            DarkWidgets.Label(new Rect(x, y, width, ChipHeight + 2), text, DarkTheme.TextMuted, GameFont.Tiny,
                TextAnchor.MiddleLeft, false);
            x += width;
        }

        void TagChip(string tag, string label = null, string tip = null)
        {
            label ??= tag;
            NewLineIfNeeded(DarkWidgets.ChipWidth(label) + SmallMargin);
            var on = _params.Tags.Contains(tag);
            if (!DarkWidgets.ToggleChip(ref x, y, label, on, tip))
            {
                return;
            }

            if (on)
            {
                _params.Tags.Remove(tag);
            }
            else
            {
                _params.Tags.Add(tag);
            }

            _params.Page = 1;
            RunSearch();
        }

        Caption(I18n.WorkshopTagsType.ToUpperInvariant());
        foreach (var tag in TypeTags)
        {
            TagChip(tag);
        }

        x += SmallMargin;
        Caption(I18n.WorkshopTagsVersion.ToUpperInvariant());
        foreach (var version in ModCompatibility.GameVersions.AsEnumerable().Reverse())
        {
            var tag = ModCompatibility.Key(version);
            TagChip(tag, tip: ModCompatibility.IsCurrent(version) ? I18n.CurrentVersion : null);
        }

        foreach (var tag in _customTags.ToList())
        {
            TagChip(tag, tip: I18n.WorkshopCustomTagTip);
        }

        // custom tag entry
        x += SmallMargin;
        const float tagFieldWidth = 110f;
        NewLineIfNeeded(tagFieldWidth + 40f);
        var tagRect = new Rect(x, y, tagFieldWidth, ChipHeight + 2);
        DarkWidgets.SearchField(tagRect, ref _customTag, TagControl, I18n.WorkshopAddTag, I18n.WorkshopAddTagTip);
        x = tagRect.xMax + (SmallMargin / 2f);
        var addRect = new Rect(x, y, 24f, ChipHeight + 2);
        if (DarkWidgets.Button(addRect, "+", !_customTag.NullOrEmpty()))
        {
            AddCustomTag();
        }

        x = addRect.xMax + SmallMargin;

        if (_params.Tags.Count > 1)
        {
            NewLineIfNeeded(DarkWidgets.ChipWidth(I18n.WorkshopMatchAny) + SmallMargin);
            if (DarkWidgets.ToggleChip(ref x, y, I18n.WorkshopMatchAny, _params.MatchAnyTag, I18n.WorkshopMatchAnyTip))
            {
                _params.MatchAnyTag = !_params.MatchAnyTag;
                _params.Page = 1;
                RunSearch();
            }
        }

        NewLineIfNeeded(DarkWidgets.ChipWidth(I18n.WorkshopHideInstalled) + SmallMargin);
        if (DarkWidgets.ToggleChip(ref x, y, I18n.WorkshopHideInstalled, _hideInstalled))
        {
            _hideInstalled = !_hideInstalled;
        }

        return y + rowHeight - canvas.yMin;
    }

    private void AddCustomTag()
    {
        var tag = _customTag?.Trim();
        _customTag = string.Empty;
        if (tag.NullOrEmpty() || _params.Tags.Contains(tag))
        {
            return;
        }

        if (!TypeTags.Contains(tag) && ModCompatibility.GameVersions.All(v => ModCompatibility.Key(v) != tag))
        {
            _customTags.TryAdd(tag);
        }

        _params.Tags.Add(tag);
        _params.Page = 1;
        RunSearch();
    }

    private void DoSortMenu()
    {
        var options = Utilities.NewOptionsList;
        foreach (WorkshopSort sort in System.Enum.GetValues(typeof(WorkshopSort)))
        {
            var target = sort;
            options.Add(new FloatMenuOption(SortLabel(sort), () =>
            {
                _params.Sort = target;
                _params.Page = 1;
                RunSearch();
            }));
        }

        Utilities.FloatMenu(options);
    }

    private void DoTrendMenu()
    {
        var options = Utilities.NewOptionsList;
        foreach (var days in TrendPeriods)
        {
            var target = days;
            options.Add(new FloatMenuOption(I18n.WorkshopTrendDays(days), () =>
            {
                _params.TrendDays = target;
                _params.Page = 1;
                RunSearch();
            }));
        }

        Utilities.FloatMenu(options);
    }

    private static string SortLabel(WorkshopSort sort)
    {
        return sort switch
        {
            WorkshopSort.Trending => I18n.WorkshopSortTrending,
            WorkshopSort.MostSubscribed => I18n.WorkshopSortMostSubscribed,
            WorkshopSort.TopRated => I18n.WorkshopSortTopRated,
            WorkshopSort.MostRecent => I18n.WorkshopSortMostRecent,
            WorkshopSort.RecentlyUpdated => I18n.WorkshopSortRecentlyUpdated,
            _ => I18n.WorkshopSortRelevance
        };
    }

    private void DoSteamUnavailable(Rect rect)
    {
        DarkWidgets.Panel(rect);
        var message = new Rect(rect.xMin, rect.center.y - 40f, rect.width, 40f);
        DarkWidgets.EmptyState(message, I18n.WorkshopSteamUnavailable);
        var label = I18n.WorkshopOpenInBrowser;
        var width = DarkWidgets.ButtonWidth(label);
        if (DarkWidgets.PrimaryButton(new Rect(rect.center.x - (width / 2f), message.yMax + SmallMargin, width,
                ButtonHeight), label))
        {
            Application.OpenURL(_params.BrowserUrl);
        }
    }

    private void DoResultList(Rect canvas)
    {
        DarkWidgets.Panel(canvas);
        var results = VisibleResults().ToList();

        if (!results.Any())
        {
            string message;
            if (WorkshopSearch.Loading)
            {
                message = I18n.WorkshopSearching;
            }
            else if (!WorkshopSearch.Error.NullOrEmpty())
            {
                message = WorkshopSearch.Error;
            }
            else
            {
                message = I18n.WorkshopNoResults;
            }

            DarkWidgets.EmptyState(canvas, message);
            return;
        }

        var outRect = canvas.ContractedBy(1f);
        var viewRect = new Rect(outRect.xMin, outRect.yMin, outRect.width, results.Count * WorkshopRowHeight);
        if (viewRect.height > outRect.height)
        {
            viewRect.width -= 18f;
        }

        var first = Mathf.FloorToInt(_listScroll.y / WorkshopRowHeight) - 1;
        var last = Mathf.CeilToInt((_listScroll.y + outRect.height) / WorkshopRowHeight) + 1;

        Widgets.BeginScrollView(outRect, ref _listScroll, viewRect);
        for (var i = Mathf.Max(0, first); i < results.Count && i <= last; i++)
        {
            var rowRect = new Rect(viewRect.xMin, viewRect.yMin + (i * WorkshopRowHeight), viewRect.width,
                WorkshopRowHeight);
            DoResultRow(rowRect, results[i], i % 2 == 1);
        }

        Widgets.EndScrollView();

        if (WorkshopSearch.Loading)
        {
            // dim stale results while the next page loads
            Widgets.DrawBoxSolid(outRect, DarkTheme.Scrim);
        }
    }

    private void DoResultRow(Rect rect, WorkshopItemInfo item, bool alternate)
    {
        Widgets.DrawBoxSolid(rect, alternate ? DarkTheme.PanelAlt : DarkTheme.Panel);
        var selected = _selected != null && _selected.FileId == item.FileId;
        if (selected)
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.Selected);
            Widgets.DrawBoxSolid(new Rect(rect.xMin, rect.yMin, 2f, rect.height), DarkTheme.Accent);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        var inner = rect.ContractedBy(SmallMargin);
        var thumbHeight = inner.height;
        var thumbRect = new Rect(inner.xMin, inner.yMin, thumbHeight * 16f / 9f, thumbHeight);
        DarkWidgets.Thumbnail(thumbRect, WebTextureCache.Get(item.PreviewUrl));

        const float actionWidth = 86f;
        var actionRect = new Rect(inner.xMax - actionWidth, inner.yMin + ((inner.height - ButtonHeight) / 2f),
            actionWidth, ButtonHeight);
        var textX = thumbRect.xMax + SmallMargin;
        var textWidth = actionRect.xMin - SmallMargin - textX;

        var titleRect = new Rect(textX, inner.yMin, textWidth, LineHeight + 2);
        DarkWidgets.LabelTruncated(titleRect, item.Title, DarkTheme.TextPrimary);

        var meta = $"{item.AuthorName}  ·  {I18n.WorkshopSubscribers(WorkshopItemInfo.FormatCount(item.Subscribers))}";
        if (item.RatingPercent >= 0)
        {
            meta += $"  ·  {item.RatingPercent}%";
        }

        meta += $"  ·  {WorkshopItemInfo.FormatDate(item.Updated)}";
        DarkWidgets.LabelTruncated(new Rect(textX, titleRect.yMax, textWidth, LineHeight - 2), meta,
            DarkTheme.TextMuted, GameFont.Tiny);

        var chipX = textX;
        var chipY = inner.yMax - ChipHeight;
        foreach (var version in item.TaggedVersions.AsEnumerable().Reverse())
        {
            var text = ModCompatibility.Key(version);
            if (chipX + DarkWidgets.ChipWidth(text) > textX + textWidth)
            {
                break;
            }

            DarkWidgets.Chip(ref chipX, chipY, text,
                ModCompatibility.IsCurrent(version) ? DarkTheme.DotGreen : DarkTheme.TextMuted);
        }

        StatusChip(ref chipX, chipY, item, textX + textWidth);

        if (DoPrimaryAction(actionRect, item))
        {
            return;
        }

        if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            Utilities.FloatMenu(ContextOptions(item));
            Event.current.Use();
            return;
        }

        if (Widgets.ButtonInvisible(rect) && !selected)
        {
            _selected = item;
            _descriptionScroll = Vector2.zero;
        }
    }

    private static void StatusChip(ref float x, float y, WorkshopItemInfo item, float xMax)
    {
        string text;
        Color color;
        var installed = item.InstalledMod;
        if (item.Downloading)
        {
            text = I18n.WorkshopStateDownloading;
            color = DarkTheme.Accent;
        }
        else if (installed != null && item.NeedsUpdate)
        {
            text = I18n.WorkshopStateNeedsUpdate;
            color = DarkTheme.DotYellow;
        }
        else if (installed is { Active: true })
        {
            text = I18n.WorkshopStateActive;
            color = DarkTheme.DotGreen;
        }
        else if (installed != null)
        {
            text = I18n.WorkshopStateInstalled;
            color = DarkTheme.DotGreen;
        }
        else if (item.Subscribed)
        {
            text = I18n.WorkshopStateSubscribed;
            color = DarkTheme.Accent;
        }
        else
        {
            return;
        }

        if (x + DarkWidgets.ChipWidth(text) > xMax)
        {
            return;
        }

        DarkWidgets.Chip(ref x, y, text, color);
    }

    /// <summary>Draws the row's main action; returns true if it was clicked.</summary>
    private static bool DoPrimaryAction(Rect rect, WorkshopItemInfo item)
    {
        var installed = item.InstalledMod;
        if (installed != null)
        {
            if (installed.Active)
            {
                return DarkWidgets.Button(rect, I18n.ActionShowInList) && ShowInModList(installed);
            }

            if (DarkWidgets.Button(rect, I18n.ActionActivate, DarkTheme.PanelAlt, DarkTheme.DotGreen))
            {
                ModButton_Installed.For(installed).Active = true;
                return true;
            }

            return false;
        }

        if (item.Subscribed || item.Downloading)
        {
            DarkWidgets.Button(rect, I18n.WorkshopStateDownloading, false);
            return false;
        }

        if (DarkWidgets.PrimaryButton(rect, I18n.ActionSubscribe))
        {
            Workshop.Subscribe(item.FileId);
            return true;
        }

        return false;
    }

    private static bool ShowInModList(ModMetaData mod)
    {
        Page_BetterModConfig.Instance?.ShowMod(ModButton_Installed.For(mod));
        return true;
    }

    private List<FloatMenuOption> ContextOptions(WorkshopItemInfo item)
    {
        var options = Utilities.NewOptionsList;
        var installed = item.InstalledMod;
        if (installed == null && !item.Subscribed)
        {
            options.Add(new FloatMenuOption(I18n.Subscribe(item.Title), () => Workshop.Subscribe(item.FileId)));
        }

        if (item.Subscribed || installed is { Source: ContentSource.SteamWorkshop })
        {
            options.Add(new FloatMenuOption(I18n.ActionUnsubscribe, () => Unsubscribe(item)));
        }

        if (installed != null)
        {
            options.Add(new FloatMenuOption(I18n.ActionShowInList, () => ShowInModList(installed)));
        }

        options.Add(new FloatMenuOption(I18n.WorkshopPage(item.Title), () => SteamUtility.OpenWorkshopPage(item.FileId)));
        options.Add(new FloatMenuOption(I18n.CopyLink, () => CopyLink(item)));
        options.Add(new FloatMenuOption(I18n.WorkshopMoreByAuthor(item.AuthorName), () => OpenAuthor(item)));
        return options;
    }

    private static void Unsubscribe(WorkshopItemInfo item)
    {
        var installed = item.InstalledMod;
        if (installed is { Source: ContentSource.SteamWorkshop })
        {
            Workshop.Unsubscribe(installed);
        }
        else
        {
            SteamUGC.UnsubscribeItem(item.FileId);
        }
    }

    private static void CopyLink(WorkshopItemInfo item)
    {
        GUIUtility.systemCopyBuffer = item.Url;
        Messages.Message(I18n.LinkCopied, MessageTypeDefOf.SilentInput, false);
    }

    private static void OpenAuthor(WorkshopItemInfo item)
    {
        SteamUtility.OpenUrl($"https://steamcommunity.com/profiles/{item.OwnerId}/myworkshopfiles/?appid=294100");
    }

    private void DoDetails(Rect canvas)
    {
        DarkWidgets.Panel(canvas);
        if (_selected == null)
        {
            DarkWidgets.EmptyState(canvas, I18n.WorkshopNothingSelected);
            return;
        }

        var item = _selected;
        canvas = canvas.ContractedBy(SmallMargin);

        // preview, 16:9 box
        var previewHeight = Mathf.Min(canvas.width * 9f / 16f, canvas.height * 0.38f);
        var previewRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, previewHeight);
        Widgets.DrawBoxSolid(previewRect, DarkTheme.ThumbPlaceholder);
        var texture = WebTextureCache.Get(item.PreviewUrl);
        if (texture != null)
        {
            GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit);
        }

        canvas.yMin = previewRect.yMax + SmallMargin;

        // title + author
        var titleRect = new Rect(canvas.xMin, canvas.yMin, canvas.width, LineHeight + 12f);
        DarkWidgets.LabelTruncated(titleRect, item.Title, DarkTheme.TextPrimary, GameFont.Medium);
        var authorRect = new Rect(canvas.xMin, titleRect.yMax, canvas.width, LineHeight);
        DarkWidgets.LabelTruncated(authorRect, I18n.WorkshopBy(item.AuthorName), DarkTheme.TextMuted, GameFont.Tiny);
        Utilities.ActionButton(authorRect, () => OpenAuthor(item));
        TooltipHandler.TipRegion(authorRect, I18n.WorkshopMoreByAuthor(item.AuthorName));
        canvas.yMin = authorRect.yMax + (SmallMargin / 2f);

        // stats
        var x = canvas.xMin;
        DarkWidgets.Chip(ref x, canvas.yMin, I18n.WorkshopSubscribers(WorkshopItemInfo.FormatCount(item.Subscribers)),
            DarkTheme.TextPrimary);
        if (item.RatingPercent >= 0)
        {
            DarkWidgets.Chip(ref x, canvas.yMin, I18n.WorkshopRating(item.RatingPercent),
                item.RatingPercent >= 80 ? DarkTheme.DotGreen :
                item.RatingPercent >= 50 ? DarkTheme.DotYellow : DarkTheme.DotRed,
                I18n.WorkshopVotes(item.VotesUp, item.VotesDown));
        }

        DarkWidgets.Chip(ref x, canvas.yMin, I18n.WorkshopUpdated(WorkshopItemInfo.FormatDate(item.Updated)),
            DarkTheme.TextMuted, I18n.WorkshopCreated(WorkshopItemInfo.FormatDate(item.Created)));
        if (item.FileSize > 0 && x + 60f < canvas.xMax)
        {
            DarkWidgets.Chip(ref x, canvas.yMin, ((long)item.FileSize).ToStringSize(), DarkTheme.TextMuted);
        }

        canvas.yMin += ChipHeight + SmallMargin;

        // per-version compatibility according to the Workshop tags (+ installed copy, if any)
        DarkWidgets.SectionHeader(ref canvas, I18n.Compatibility);
        x = canvas.xMin;
        var installed = item.InstalledMod;
        foreach (var version in ModCompatibility.GameVersions)
        {
            var key = ModCompatibility.Key(version);
            var tagged = item.TaggedFor(version);
            var local = installed != null && installed.SupportedVersionsReadOnly.Any(v =>
                v.Major == version.Major && v.Minor == version.Minor);
            var color = tagged || local
                ? ModCompatibility.IsCurrent(version) ? DarkTheme.DotGreen : DarkTheme.TextPrimary
                : DarkTheme.TextDisabled;
            var fill = tagged || local ? DarkTheme.CellGreen : (Color?)null;
            var tip = I18n.WorkshopVersionTip(key, tagged, installed != null ? local : null);
            DarkWidgets.Chip(ref x, canvas.yMin, key, color, tip, fill);
        }

        canvas.yMin += ChipHeight + SmallMargin;

        // other tags (clickable: filter by them)
        var otherTags = item.Tags.Where(t => !item.TaggedVersions.Any(v => ModCompatibility.Key(v) == t)).ToList();
        if (otherTags.Any())
        {
            x = canvas.xMin;
            foreach (var tag in otherTags)
            {
                if (x + DarkWidgets.ChipWidth(tag) + SmallMargin > canvas.xMax)
                {
                    break;
                }

                var on = _params.Tags.Contains(tag);
                if (DarkWidgets.ToggleChip(ref x, canvas.yMin, tag, on, I18n.WorkshopFilterByTag(tag)) && !on)
                {
                    _params.Tags.Add(tag);
                    if (!TypeTags.Contains(tag))
                    {
                        _customTags.TryAdd(tag);
                    }

                    _params.Page = 1;
                    RunSearch();
                }
            }

            canvas.yMin += ChipHeight + 2 + SmallMargin;
        }

        // actions (wrap onto a second row in narrow windows)
        var bx = canvas.xMin;
        var by = canvas.yMin;
        if (installed == null && !item.Subscribed)
        {
            if (DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.ActionSubscribe, true))
            {
                Workshop.Subscribe(item.FileId);
            }
        }
        else if (installed != null && !installed.Active)
        {
            if (DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.ActionActivate, true))
            {
                ModButton_Installed.For(installed).Active = true;
            }
        }

        if (installed != null &&
            DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.ActionShowInList))
        {
            ShowInModList(installed);
        }

        if (DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.ActionWorkshop))
        {
            SteamUtility.OpenWorkshopPage(item.FileId);
        }

        if (DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.CopyLink))
        {
            CopyLink(item);
        }

        if ((item.Subscribed || installed is { Source: ContentSource.SteamWorkshop }) &&
            DarkWidgets.ButtonFlow(ref bx, ref by, canvas.xMin, canvas.xMax, I18n.ActionUnsubscribe,
                fg: DarkTheme.DotRed, tip: I18n.UnSubscribe))
        {
            Unsubscribe(item);
        }

        canvas.yMin = by + ButtonHeight + SmallMargin;

        // description
        if (canvas.height <= LineHeight)
        {
            return;
        }

        Widgets.DrawBoxSolid(canvas, SlightlyDarkBackground);
        var outRect = canvas.ContractedBy(SmallMargin);
        var oldFont = Text.Font;
        Text.Font = GameFont.Small;
        // measuring a long description is expensive; only redo it when the item or width changes
        if (_descriptionHeightFor != item || !Mathf.Approximately(_descriptionHeightWidth, outRect.width))
        {
            _descriptionHeightFor = item;
            _descriptionHeightWidth = outRect.width;
            _descriptionHeight = Text.CalcHeight(item.CleanDescription, outRect.width - 18f);
        }

        var viewRect = new Rect(outRect.xMin, outRect.yMin, outRect.width - 18f, _descriptionHeight);
        Widgets.BeginScrollView(outRect, ref _descriptionScroll, viewRect);
        GUI.color = DarkTheme.TextPrimary;
        Widgets.Label(viewRect, item.CleanDescription);
        GUI.color = Color.white;
        Widgets.EndScrollView();
        Text.Font = oldFont;
    }

    private void DoFooter(Rect rect)
    {
        var page = WorkshopSearch.Current?.Page ?? _params.Page;
        var pages = WorkshopSearch.PageCount;
        var status = WorkshopSearch.Loading
            ? I18n.WorkshopSearching
            : I18n.WorkshopPageStatus(page, pages, WorkshopSearch.TotalResults);
        DarkWidgets.Label(rect, status, DarkTheme.TextMuted, GameFont.Tiny);

        var x = rect.xMax;
        if (DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.WorkshopNext, enabled: page < pages && !WorkshopSearch.Loading))
        {
            GoToPage(page + 1);
        }

        if (DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.WorkshopPrevious, enabled: page > 1 && !WorkshopSearch.Loading))
        {
            GoToPage(page - 1);
        }

        if (DarkWidgets.ButtonAutoRight(ref x, rect.yMin, I18n.WorkshopFirst, enabled: page > 1 && !WorkshopSearch.Loading))
        {
            GoToPage(1);
        }
    }

    private void GoToPage(uint page)
    {
        _params.Page = page;
        RunSearch();
    }
}
