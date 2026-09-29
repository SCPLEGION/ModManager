// Tab_Mods.cs
// The classic three-column mod list (available / active / details). Its drawing, selection and keyboard
// navigation live in Page_BetterModConfig, which owns that state.

using UnityEngine;

namespace SCPModManager;

public sealed class Tab_Mods(Page_BetterModConfig page) : ManagerTab
{
    public override string Label => I18n.TabMods;

    public override void DoContents(Rect canvas)
    {
        page.DoModsPage(canvas);
    }
}
