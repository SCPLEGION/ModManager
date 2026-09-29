// ManagerTab.cs
// One page of the mod manager window, selected from the header tab bar.

using UnityEngine;

namespace SCPModManager;

public abstract class ManagerTab
{
    public abstract string Label { get; }

    /// <summary>Optional count shown as a badge on the tab (e.g. number of problems).</summary>
    public virtual string Badge => null;

    public virtual Color BadgeColor => Resources.DarkTheme.DotRed;

    public virtual string Tooltip => null;

    public abstract void DoContents(Rect canvas);

    public virtual void OnOpened()
    {
    }

    public virtual void OnClosed()
    {
    }

    /// <summary>Ctrl+F: move keyboard focus to this page's search field, if it has one.</summary>
    public virtual void FocusSearch()
    {
    }

    public virtual void WindowUpdate()
    {
    }
}
