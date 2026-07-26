// WindowResizer_DoResizeControl.cs
// Copyright Karel Kroeze, 2019-2019

using HarmonyLib;
using UnityEngine;
using Verse;

namespace SCPModManager;

[HarmonyPatch(typeof(WindowResizer), nameof(WindowResizer.DoResizeControl))]
public static class WindowResizer_DoResizeControl
{
    public static void Postfix(ref bool ___isResizing)
    {
        if (___isResizing && (Input.GetMouseButtonUp(0) || !Application.isFocused))
        {
            ___isResizing = false;
        }
    }
}