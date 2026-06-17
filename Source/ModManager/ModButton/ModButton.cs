// ModButton.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static ModManager.Constants;

namespace ModManager;

public abstract class ModButton
{
    internal static readonly Dictionary<string, string> _modNameTruncationCache = new();

    // The active list gives Installed rows a wider left gutter (load-order number + drag handle), so the
    // name area is narrower there than in the available list. The Truncate cache is keyed by name only, so
    // active rows need their own cache or a mod toggled active/inactive would show a stale-width truncation.
    internal static readonly Dictionary<string, string> _activeModNameTruncationCache = new();
    private ModButton _focus;

    private List<Dependency> _relevantIssues;

    private string _relevantIssuesString;
    private string _trimmedName;

    public virtual string TrimmedName
    {
        get
        {
            if (_trimmedName.NullOrEmpty())
            {
                _trimmedName = Utilities.TrimModName(Name);
            }

            return _trimmedName;
        }
    }

    public abstract string Name { get; }
    public abstract string Identifier { get; }
    public abstract ulong SteamWorkshopId { get; }
    public abstract bool Active { get; set; }
    public virtual bool IsCoreMod => false;
    public virtual bool IsExpansion => false;
    public virtual bool IsModManager => false;
    public virtual int LoadOrder => -1;
    public virtual int SortOrder => -1;
    public abstract IEnumerable<Dependency> Requirements { get; }
    protected virtual int SeverityThreshold => 2;

    private List<Dependency> RelevantIssues =>
        _relevantIssues ??= Requirements.Where(i => i.Severity >= SeverityThreshold).ToList();

    private string RelevantIssuesString => _relevantIssuesString ??= RelevantIssues.OrderBy(i => i.Severity)
        .Select(i => i.Tooltip.Colorize(i.Color))
        .StringJoin("\n");

    public abstract bool SamePackageId(string packageId);

    public virtual void DoModButton(Rect canvas, bool alternate = false, Action clickAction = null,
        Action doubleClickAction = null, bool deemphasizeFiltered = false, string filter = null)
    {
#if DEBUG
            clickAction += () => Debug.Log($"clicked: {Name}");
            doubleClickAction += () => Debug.Log($"doubleClicked: {Name}");
#endif

        // dark zebra base for every row
        Widgets.DrawBoxSolid(canvas, alternate ? Resources.DarkTheme.PanelAlt : Resources.DarkTheme.Panel);

        if (Page_BetterModConfig.Instance.Selected == this)
        {
            // solid selected fill (#1f2937) with an accent left edge
            Widgets.DrawBoxSolid(canvas, Resources.DarkTheme.Selected);
            Widgets.DrawBoxSolid(new Rect(canvas.xMin, canvas.yMin, 2f, canvas.height), Resources.DarkTheme.Accent);
            if (Page_BetterModConfig.Instance.SelectedHasFocus)
            {
                Widgets.DrawHighlightSelected(canvas);
            }
        }

        if (!DraggingManager.Dragging)
        {
            HandleInteractions(canvas, clickAction, doubleClickAction);
        }
    }

    public virtual int MatchesFilter(string filter)
    {
        if (filter.NullOrEmpty())
        {
            return 1;
        }

        if (ModManager.Settings.TrimTags && TrimmedName.ToLower().Contains(filter.ToLower()) ||
            !ModManager.Settings.TrimTags && Name.ToLower().Contains(filter.ToLower()))
        {
            return 1;
        }

        return 0;
    }

    internal abstract void DoModActionButtons(Rect canvas);
    internal abstract void DoModDetails(Rect canvas);

    protected virtual void HandleInteractions(Rect canvas, Action clickAction, Action doubleClickAction)
    {
        if (!Mouse.IsOver(canvas))
        {
            return;
        }

        Widgets.DrawHighlight(canvas);
        if (Event.current.type == EventType.MouseDown)
        {
            _focus = this;
            if (Event.current.clickCount == 2)
            {
                doubleClickAction?.Invoke();
            }
        }

        if (Event.current.type == EventType.MouseUp && _focus == this)
        {
            clickAction?.Invoke();
        }
    }

    public virtual void Notify_RecacheIssues()
    {
        _relevantIssues = null;
        _relevantIssuesString = null;
    }

    internal virtual void DoModIssuesIcon(Rect canvas)
    {
        if (!RelevantIssues.Any())
        {
            return;
        }

        var worst = Requirements.MaxBy(d => d.Severity);
        GUI.color = worst.Color;
        GUI.DrawTexture(canvas, Resources.Warning);
        GUI.color = Color.white;
        TooltipHandler.TipRegion(canvas, RelevantIssuesString);
    }

    internal virtual void DrawRequirements(ref Rect canvas)
    {
        var severityThreshold = ModManager.Settings.ShowSatisfiedRequirements ? 0 : 1;
        var relevantIssues = Requirements.Where(i => i.Severity >= severityThreshold);
        if (!relevantIssues.Any())
        {
            return;
        }

        Utilities.DoLabel(ref canvas, I18n.Dependencies);
        var outRect = new Rect(canvas) { height = (relevantIssues.Count() * LineHeight) + (SmallMargin * 2f) };
        Widgets.DrawBoxSolid(outRect, Resources.SlightlyDarkBackground);
        canvas.yMin += outRect.height + SmallMargin;
        outRect = outRect.ContractedBy(SmallMargin);
        var issueRect = new Rect(
            outRect.xMin,
            outRect.yMin,
            outRect.width,
            LineHeight);

        foreach (var issue in relevantIssues)
        {
            var iconRect = new Rect(issueRect.xMin, issueRect.yMin, SmallIconSize, SmallIconSize)
                .CenteredOnYIn(issueRect);
            var labelRect = new Rect(issueRect);
            labelRect.xMin += SmallIconSize + SmallMargin;

            // satisfied -> green check; otherwise the issue's own icon tinted by severity (red/yellow)
            Texture2D icon;
            Color iconColor;
            if (issue.IsSatisfied)
            {
                icon = Resources.Check;
                iconColor = Resources.DarkTheme.DotGreen;
            }
            else
            {
                icon = issue.StatusIcon;
                iconColor = issue.Severity >= SeverityThreshold
                    ? Resources.DarkTheme.DotRed
                    : Resources.DarkTheme.DotYellow;
            }

            GUI.color = iconColor;
            GUI.DrawTexture(iconRect, icon);
            GUI.color = Resources.DarkTheme.TextPrimary;
            Widgets.Label(labelRect, issue.Tooltip);
            if (issue.Resolvers.Any())
            {
                Utilities.ActionButton(issueRect, () => issue.OnClicked(null)); // todo: reference to window? Why?
            }

            issueRect.y += LineHeight;
        }

        GUI.color = Color.white;
    }

    public static void Notify_ModButtonSizeChanged()
    {
        _modNameTruncationCache.Clear();
        _activeModNameTruncationCache.Clear();
    }
}