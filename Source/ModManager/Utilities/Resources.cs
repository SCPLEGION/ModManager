// Resources.cs
// Copyright Karel Kroeze, 2018-2018

using System.Linq;
using UnityEngine;
using Verse;

namespace ModManager;

[StaticConstructorOnStartup]
public class Resources
{
    public static Color SlightlyDarkBackground;

    public static Texture2D Close;

    public static readonly Texture2D EyeOpen;

    public static readonly Texture2D EyeClosed;

    public static readonly Texture2D Search;

    public static readonly Texture2D Steam;

    public static readonly Texture2D Ludeon;

    public static readonly Texture2D Folder;

    public static readonly Texture2D File;

    public static readonly Texture2D Warning;

    public static Texture2D Question;

    public static readonly Texture2D Palette;

    public static readonly Texture2D Gear;

    public static readonly Texture2D Status_Cross;

    public static Texture2D Status_Down;

    public static readonly Texture2D Status_Up;

    public static readonly Texture2D Status_Plus;

    public static readonly Texture2D Check;

    public static readonly Texture2D Wand;

    public static readonly Texture2D[] Spinner;

    public static readonly Color WindowBGBorderColor = new ColorInt(97, 108, 122).ToColor;

    // White anti-aliased circular alpha mask, tinted per-call via GUI.color (see DrawDot).
    // Built once on the main thread during the [StaticConstructorOnStartup] pass.
    public static readonly Texture2D DotMask;

    /// <summary>
    ///     Single home for the dark-theme palette. Every theme colour is defined here exactly once;
    ///     UI code references <c>DarkTheme.*</c> and never inlines <c>new Color(...)</c>.
    ///     Components are 0..1 floats (UnityEngine.Color), with the source hex noted alongside.
    /// </summary>
    public static class DarkTheme
    {
        public static readonly Color Background = new Color(0.067f, 0.067f, 0.067f, 1f); // #111111
        public static readonly Color Panel = new Color(0.102f, 0.102f, 0.102f, 1f); // #1a1a1a
        public static readonly Color PanelAlt = new Color(0.137f, 0.137f, 0.137f, 1f); // zebra row
        public static readonly Color Selected = new Color(0.122f, 0.161f, 0.216f, 1f); // #1f2937
        public static readonly Color HeaderBar = new Color(0.086f, 0.086f, 0.086f, 1f);

        public static readonly Color Accent = new Color(0.310f, 0.557f, 0.969f, 1f); // #4f8ef7
        public static readonly Color Border = new Color(1f, 1f, 1f, 0.08f); // subtle hairline over dark

        public static readonly Color TextPrimary = new Color(0.92f, 0.92f, 0.93f, 1f);
        public static readonly Color TextMuted = new Color(0.55f, 0.57f, 0.60f, 1f);

        public static readonly Color ChipBG = new Color(1f, 1f, 1f, 0.06f);

        public static readonly Color DotGreen = new Color(0.36f, 0.80f, 0.42f, 1f);
        public static readonly Color DotYellow = new Color(0.95f, 0.77f, 0.27f, 1f);
        public static readonly Color DotRed = new Color(0.91f, 0.34f, 0.34f, 1f);
    }

    static Resources()
    {
        // Dark theme: solid dark panel shade reused by every existing SlightlyDarkBackground call site.
        SlightlyDarkBackground = DarkTheme.Panel;
        DotMask = MakeCircleTex(32);
        Close = ContentFinder<Texture2D>.Get("UI/Icons/Close");
        EyeOpen = ContentFinder<Texture2D>.Get("UI/Icons/EyeOpen");
        EyeClosed = ContentFinder<Texture2D>.Get("UI/Icons/EyeClosed");
        Search = ContentFinder<Texture2D>.Get("UI/Icons/Search");
        Steam = ContentFinder<Texture2D>.Get("UI/Icons/ContentSources/SteamWorkshop");
        Ludeon = ContentFinder<Texture2D>.Get("UI/Icons/Ludeon");
        File = ContentFinder<Texture2D>.Get("UI/Icons/File");
        Folder = ContentFinder<Texture2D>.Get("UI/Icons/ContentSources/ModsFolder");
        Warning = ContentFinder<Texture2D>.Get("UI/Icons/Warning");
        Question = ContentFinder<Texture2D>.Get("UI/Icons/Question");
        // the joys of case-unaware file systems - I now don't know which version is out there...
        Palette = ContentFinder<Texture2D>.Get("UI/Icons/Palette", false);
        Palette ??= ContentFinder<Texture2D>.Get("UI/Icons/palette");
        Gear = ContentFinder<Texture2D>.Get("UI/Icons/Gear");
        Check = ContentFinder<Texture2D>.Get("UI/Widgets/CheckOn");
        Wand = ContentFinder<Texture2D>.Get("UI/Icons/Wand");

        Status_Cross = ContentFinder<Texture2D>.Get("UI/Icons/Status/Cross");
        Status_Down = ContentFinder<Texture2D>.Get("UI/Icons/Status/Down");
        Status_Up = ContentFinder<Texture2D>.Get("UI/Icons/Status/Up");
        Status_Plus = ContentFinder<Texture2D>.Get("UI/Icons/Status/Plus");

        Spinner = ContentFinder<Texture2D>.GetAllInFolder("UI/Icons/Spinner").ToArray();
        if (Spinner.Length == 0)
        {
            // Degrade gracefully instead of crashing on Spinner[0] if the textures fail to load
            // (e.g. an AssetBundle that won't load on this platform). BadTex is always available.
            Spinner = new[] { Warning ?? BaseContent.BadTex };
        }
    }

    public static void DrawWindowBackground(Rect rect, Color bgColor)
    {
        GUI.color = Widgets.WindowBGFillColor;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = bgColor;
        GUI.DrawTexture(rect, BaseContent.WhiteTex);
        GUI.color = WindowBGBorderColor;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    /// <summary>
    ///     Dark-theme window background: a flat <see cref="DarkTheme.Background" /> base (#111) with an optional
    ///     user tint overlaid (keeps the BackgroundColor setting meaningful) and a subtle border.
    /// </summary>
    public static void DrawDarkWindowBackground(Rect rect, Color overlay)
    {
        Widgets.DrawBoxSolid(rect, DarkTheme.Background);
        if (overlay.a > 0f)
        {
            Widgets.DrawBoxSolid(rect, overlay);
        }

        GUI.color = DarkTheme.Border;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    /// <summary>Fills a panel rect with a solid theme colour (Panel / PanelAlt / Selected).</summary>
    public static void DrawPanel(Rect rect, Color fill)
    {
        Widgets.DrawBoxSolid(rect, fill);
    }

    /// <summary>Draws the shared circular dot mask tinted to <paramref name="color" />. Pass a square rect.</summary>
    public static void DrawDot(Rect rect, Color color)
    {
        if (DotMask == null)
        {
            // mask failed to build — a solid square still communicates the status colour
            Widgets.DrawBoxSolid(rect, color);
            return;
        }

        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, DotMask);
        GUI.color = old;
    }

    private static Texture2D MakeCircleTex(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "ModManager_DotMask"
        };
        var pixels = new Color[size * size];
        var center = (size - 1) / 2f;
        var radius = (size / 2f) - 1f; // 1px margin so the AA edge isn't clipped
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                float dx = x - center, dy = y - center;
                var dist = (float)System.Math.Sqrt((dx * dx) + (dy * dy));
                var alpha = Mathf.Clamp(radius - dist + 0.5f, 0f, 1f); // ~1px feathered edge
                pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha); // white; tinted via GUI.color at draw
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false);
        return tex;
    }
}