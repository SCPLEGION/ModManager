// DarkWidgets.cs
// Shared dark-theme building blocks for the tabbed pages. Everything draws through Verse.Widgets /
// UnityEngine.GUI and takes its colours from Resources.DarkTheme; global Text/GUI state is always restored.

using System;
using UnityEngine;
using Verse;
using static SCPModManager.Constants;
using static SCPModManager.Resources;

namespace SCPModManager;

public static class DarkWidgets
{
    /// <summary>Solid panel fill with a hairline border.</summary>
    public static void Panel(Rect rect, Color? fill = null)
    {
        Widgets.DrawBoxSolid(rect, fill ?? DarkTheme.Panel);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    public static void Label(Rect rect, string text, Color color, GameFont font = GameFont.Small,
        TextAnchor anchor = TextAnchor.MiddleLeft, bool wordWrap = true)
    {
        var oldFont = Text.Font;
        var oldAnchor = Text.Anchor;
        var oldWrap = Text.WordWrap;
        var oldColor = GUI.color;
        Text.Font = font;
        Text.Anchor = anchor;
        Text.WordWrap = wordWrap;
        GUI.color = color;
        Widgets.Label(rect, text);
        GUI.color = oldColor;
        Text.WordWrap = oldWrap;
        Text.Anchor = oldAnchor;
        Text.Font = oldFont;
    }

    /// <summary>Single-line label that is truncated to fit, with the full text as tooltip when cut off.</summary>
    public static void LabelTruncated(Rect rect, string text, Color color, GameFont font = GameFont.Small,
        TextAnchor anchor = TextAnchor.MiddleLeft)
    {
        if (text.NullOrEmpty())
        {
            return;
        }

        var oldFont = Text.Font;
        Text.Font = font;
        var truncated = text.Truncate(rect.width);
        Text.Font = oldFont;
        Label(rect, truncated, color, font, anchor, false);
        if (truncated != text)
        {
            TooltipHandler.TipRegion(rect, text);
        }
    }

    /// <summary>Uppercase, tiny, muted section heading. Consumes <see cref="LabelHeight" /> from the canvas.</summary>
    public static void SectionHeader(ref Rect canvas, string label, string suffix = null)
    {
        var rect = new Rect(canvas.xMin, canvas.yMin, canvas.width, LabelHeight);
        var text = label.ToUpperInvariant();
        if (!suffix.NullOrEmpty())
        {
            text += $"  ·  {suffix}";
        }

        Label(rect, text, DarkTheme.TextMuted, GameFont.Tiny);
        canvas.yMin += LabelHeight;
    }

    /// <summary>Flat dark-theme button: solid <paramref name="bg" /> (lightened on hover), hairline border,
    /// centred <paramref name="fg" /> label. Returns true when clicked.</summary>
    public static bool Button(Rect rect, string label, Color bg, Color fg, bool enabled = true, string tip = null)
    {
        var hover = enabled && Mouse.IsOver(rect);
        Widgets.DrawBoxSolid(rect, hover ? Lighten(bg) : bg);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;

        Label(rect, label, enabled ? fg : DarkTheme.TextDisabled, GameFont.Tiny, TextAnchor.MiddleCenter, false);
        if (!tip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tip);
        }

        return enabled && Widgets.ButtonInvisible(rect);
    }

    /// <summary>Neutral secondary button.</summary>
    public static bool Button(Rect rect, string label, bool enabled = true, string tip = null)
    {
        return Button(rect, label, DarkTheme.PanelAlt, DarkTheme.TextPrimary, enabled, tip);
    }

    /// <summary>Filled accent (primary) button.</summary>
    public static bool PrimaryButton(Rect rect, string label, bool enabled = true, string tip = null)
    {
        return Button(rect, label, DarkTheme.Accent, Color.white, enabled, tip);
    }

    /// <summary>Button sized to its label, laid out left-to-right from <paramref name="x" />.</summary>
    public static bool ButtonAuto(ref float x, float y, string label, bool primary = false, bool enabled = true,
        string tip = null, Color? fg = null)
    {
        var rect = new Rect(x, y, ButtonWidth(label), ButtonHeight);
        x += rect.width + SmallMargin;
        if (primary)
        {
            return PrimaryButton(rect, label, enabled, tip);
        }

        return Button(rect, label, DarkTheme.PanelAlt, fg ?? DarkTheme.TextPrimary, enabled, tip);
    }

    /// <summary>Button sized to its label in a left-to-right flow that wraps to a new row at
    /// <paramref name="xMax" />. Returns true when clicked.</summary>
    public static bool ButtonFlow(ref float x, ref float y, float xMin, float xMax, string label,
        bool primary = false, bool enabled = true, string tip = null, Color? fg = null)
    {
        if (x > xMin && x + ButtonWidth(label) > xMax)
        {
            x = xMin;
            y += ButtonHeight + (SmallMargin / 2f);
        }

        return ButtonAuto(ref x, y, label, primary, enabled, tip, fg);
    }

    /// <summary>Button sized to its label, laid out right-to-left ending at <paramref name="xMax" />.</summary>
    public static bool ButtonAutoRight(ref float xMax, float y, string label, bool primary = false,
        bool enabled = true, string tip = null, Color? fg = null)
    {
        var width = ButtonWidth(label);
        xMax -= width;
        var x = xMax;
        xMax -= SmallMargin;
        return ButtonAuto(ref x, y, label, primary, enabled, tip, fg);
    }

    public static float ButtonWidth(string label)
    {
        var oldFont = Text.Font;
        Text.Font = GameFont.Tiny;
        var width = Text.CalcSize(label).x + (SmallMargin * 4);
        Text.Font = oldFont;
        return Mathf.Max(width, 48f);
    }

    public static float ChipWidth(string text)
    {
        var oldFont = Text.Font;
        Text.Font = GameFont.Tiny;
        var width = Text.CalcSize(text).x + (SmallMargin * 2);
        Text.Font = oldFont;
        return width;
    }

    /// <summary>Static pill with tinted text. Advances <paramref name="x" />.</summary>
    public static Rect Chip(ref float x, float y, string text, Color tint, string tip = null, Color? fill = null)
    {
        var rect = new Rect(x, y, ChipWidth(text), ChipHeight);
        Widgets.DrawBoxSolid(rect, fill ?? DarkTheme.ChipBG);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
        Label(rect, text, tint, GameFont.Tiny, TextAnchor.MiddleCenter, false);
        if (!tip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tip);
        }

        x += rect.width + (SmallMargin / 2f);
        return rect;
    }

    /// <summary>Clickable toggle pill. Returns true when clicked (caller flips its own state).</summary>
    public static bool ToggleChip(ref float x, float y, string text, bool on, string tip = null)
    {
        var rect = new Rect(x, y, ChipWidth(text) + SmallMargin, ChipHeight + 2);
        x += rect.width + (SmallMargin / 2f);
        var hover = Mouse.IsOver(rect);
        Widgets.DrawBoxSolid(rect, on ? DarkTheme.AccentSoft : hover ? DarkTheme.RowHover : DarkTheme.ChipBG);
        GUI.color = on ? DarkTheme.Accent : DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
        Label(rect, text, on ? DarkTheme.TextPrimary : DarkTheme.TextMuted, GameFont.Tiny, TextAnchor.MiddleCenter,
            false);
        if (!tip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tip);
        }

        return Widgets.ButtonInvisible(rect);
    }

    /// <summary>Inset search/text field with a placeholder, a search icon and a clear button.
    /// Returns true when the text changed. The control count is constant regardless of state, so Unity
    /// keeps keyboard focus on the field while typing.</summary>
    public static bool SearchField(Rect rect, ref string text, string controlName, string placeholder,
        string tip = null)
    {
        Widgets.DrawBoxSolid(rect, DarkTheme.PanelAlt);
        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;

        var iconRect = new Rect(
            rect.xMax - SmallIconSize - SmallMargin,
            rect.yMin + ((rect.height - SmallIconSize) / 2f),
            SmallIconSize,
            SmallIconSize);
        var empty = text.NullOrEmpty();
        var changed = false;

        // the clear button has to consume its click before the textfield eats it
        if (Widgets.ButtonInvisible(empty ? Rect.zero : iconRect))
        {
            text = string.Empty;
            changed = true;
        }

        GUI.SetNextControlName(controlName);
        var fieldRect = new Rect(rect.xMin, rect.yMin, rect.width - SmallIconSize - SmallMargin, rect.height);
        var newText = Widgets.TextField(fieldRect, text ?? string.Empty);
        if (newText != (text ?? string.Empty))
        {
            text = newText;
            changed = true;
        }

        if (text.NullOrEmpty() && GUI.GetNameOfFocusedControl() != controlName && !placeholder.NullOrEmpty())
        {
            Label(new Rect(fieldRect.xMin + SmallMargin, fieldRect.yMin, fieldRect.width - SmallMargin,
                fieldRect.height), placeholder, DarkTheme.TextDisabled, GameFont.Small, TextAnchor.MiddleLeft, false);
        }

        GUI.color = Mouse.IsOver(iconRect) && !text.NullOrEmpty() ? GenUI.MouseoverColor : DarkTheme.TextMuted;
        GUI.DrawTexture(iconRect, text.NullOrEmpty() ? Search : Status_Cross);
        GUI.color = Color.white;

        if (!tip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tip);
        }

        return changed;
    }

    /// <summary>A dropdown-style button showing "label: value ▾" that runs <paramref name="onClick" />.</summary>
    public static void Dropdown(ref float x, float y, string label, Action onClick, string tip = null)
    {
        var text = $"{label} ▾";
        var rect = new Rect(x, y, ButtonWidth(text), ButtonHeight);
        x += rect.width + SmallMargin;
        if (Button(rect, text, DarkTheme.PanelAlt, DarkTheme.TextPrimary, true, tip))
        {
            onClick?.Invoke();
        }
    }

    /// <summary>Table column header; highlights when it is the sort column. Returns true when clicked.</summary>
    public static bool SortHeader(Rect rect, string label, bool active, bool descending,
        TextAnchor anchor = TextAnchor.MiddleLeft, string tip = null)
    {
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawBoxSolid(rect, DarkTheme.RowHover);
        }

        var text = active ? $"{label} {(descending ? "▼" : "▲")}" : label;
        Label(rect.ContractedBy(2f, 0f), text.ToUpperInvariant(), active ? DarkTheme.TextPrimary : DarkTheme.TextMuted,
            GameFont.Tiny, anchor, false);
        if (!tip.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect, tip);
        }

        return Widgets.ButtonInvisible(rect);
    }

    /// <summary>Centered, muted message for empty lists/pages.</summary>
    public static void EmptyState(Rect rect, string message)
    {
        Label(rect, message, DarkTheme.TextMuted, GameFont.Small, TextAnchor.MiddleCenter);
    }

    public static void HorizontalLine(float x, float y, float width)
    {
        GUI.color = DarkTheme.Border;
        Widgets.DrawLineHorizontal(x, y, width);
        GUI.color = Color.white;
    }

    /// <summary>Small rounded count badge (e.g. issues on a tab).</summary>
    public static void Badge(Rect anchorRect, string text, Color color)
    {
        if (text.NullOrEmpty())
        {
            return;
        }

        var width = Mathf.Max(ChipWidth(text), 16f);
        var rect = new Rect(anchorRect.xMax - width - 2f, anchorRect.yMin + 2f, width, 14f);
        Widgets.DrawBoxSolid(rect, color);
        Label(rect, text, Color.white, GameFont.Tiny, TextAnchor.MiddleCenter, false);
    }

    /// <summary>Draws a texture scaled to cover the rect (cropping the overflow), or a placeholder.</summary>
    public static void Thumbnail(Rect rect, Texture2D texture)
    {
        Widgets.DrawBoxSolid(rect, DarkTheme.ThumbPlaceholder);
        if (texture != null)
        {
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop);
        }

        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    public static Color Lighten(Color c)
    {
        return new Color(Mathf.Min(c.r + 0.08f, 1f), Mathf.Min(c.g + 0.08f, 1f), Mathf.Min(c.b + 0.08f, 1f), c.a);
    }
}
