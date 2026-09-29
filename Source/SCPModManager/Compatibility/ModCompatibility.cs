// ModCompatibility.cs
// Per-mod, per-game-version compatibility data: what About.xml declares, which version folders / LoadFolders
// entries exist on disk, and what the Steam Workshop listing is tagged with (which runs ahead of the local
// copy when an update has been published but not downloaded yet).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Version = System.Version;

namespace SCPModManager;

public enum CompatStatus
{
    Outdated,
    UpdatePending,
    Unknown,
    NewerOnly,
    Compatible
}

public enum VersionSupport
{
    None,
    FolderOnly,
    WorkshopOnly,
    Declared
}

public sealed class ModCompatibility
{
    private static readonly Dictionary<ModMetaData, ModCompatibility> Cache = new();
    private static List<Version> _gameVersions;

    private readonly HashSet<string> _folderVersions = [];
    private readonly HashSet<string> _declaredVersions = [];

    private ModCompatibility(ModMetaData mod)
    {
        Mod = mod;
        foreach (var version in mod.SupportedVersionsReadOnly)
        {
            _declaredVersions.Add(Key(version));
        }

        LatestDeclared = mod.SupportedVersionsReadOnly.Count > 0 ? mod.SupportedVersionsReadOnly.Max() : null;

        try
        {
            var defined = mod.loadFolders?.DefinedVersions();
            if (defined != null)
            {
                foreach (var version in defined)
                {
                    _folderVersions.Add(version.TrimStart('v'));
                }
            }

            var root = mod.RootDir;
            if (root is { Exists: true })
            {
                foreach (var version in GameVersions)
                {
                    if (Directory.Exists(Path.Combine(root.FullName, Key(version))))
                    {
                        _folderVersions.Add(Key(version));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"ModCompatibility: could not inspect folders of {mod.Name}: {e.Message}");
        }
    }

    public ModMetaData Mod { get; }
    public Version LatestDeclared { get; }

    public WorkshopItemInfo Workshop => WorkshopDetailsCache.Get(Mod, false);

    public CompatStatus Status
    {
        get
        {
            if (Mod.VersionCompatible)
            {
                return CompatStatus.Compatible;
            }

            if (Workshop is { TaggedForCurrentVersion: true })
            {
                return CompatStatus.UpdatePending;
            }

            if (Mod.MadeForNewerVersion)
            {
                return CompatStatus.NewerOnly;
            }

            return LatestDeclared == null ? CompatStatus.Unknown : CompatStatus.Outdated;
        }
    }

    public Color StatusColor => Status switch
    {
        CompatStatus.Compatible => Resources.DarkTheme.DotGreen,
        CompatStatus.UpdatePending => Resources.DarkTheme.Accent,
        CompatStatus.NewerOnly => Resources.DarkTheme.DotYellow,
        CompatStatus.Unknown => Resources.DarkTheme.TextMuted,
        _ => Resources.DarkTheme.DotRed
    };

    public string StatusLabel => Status switch
    {
        CompatStatus.Compatible => I18n.CompatStatusCompatible,
        CompatStatus.UpdatePending => I18n.CompatStatusUpdatePending,
        CompatStatus.NewerOnly => I18n.CompatStatusNewer,
        CompatStatus.Unknown => I18n.Unknown,
        _ => I18n.CompatStatusOutdated(LatestDeclared?.ToString(2) ?? "?")
    };

    public string StatusTip => Status switch
    {
        CompatStatus.Compatible => I18n.CurrentVersion,
        CompatStatus.UpdatePending => I18n.CompatUpdatePendingTip,
        _ => I18n.DifferentVersion(Mod)
    };

    /// <summary>Game versions from 1.0 up to the running version, used as the compatibility grid columns.</summary>
    public static List<Version> GameVersions
    {
        get
        {
            if (_gameVersions != null)
            {
                return _gameVersions;
            }

            _gameVersions = [];
            var major = VersionControl.CurrentMajor;
            var minor = VersionControl.CurrentMinor;
            for (var m = 1; m <= major; m++)
            {
                var maxMinor = m == major ? minor : 9;
                for (var n = 0; n <= maxMinor; n++)
                {
                    _gameVersions.Add(new Version(m, n));
                }
            }

            return _gameVersions;
        }
    }

    public static bool IsCurrent(Version version)
    {
        return version.Major == VersionControl.CurrentMajor && version.Minor == VersionControl.CurrentMinor;
    }

    public static ModCompatibility For(ModMetaData mod)
    {
        if (mod == null)
        {
            return null;
        }

        if (Cache.TryGetValue(mod, out var compat))
        {
            return compat;
        }

        compat = new ModCompatibility(mod);
        Cache.Add(mod, compat);
        return compat;
    }

    public static void Notify_Refresh()
    {
        Cache.Clear();
    }

    public VersionSupport SupportFor(Version version)
    {
        var key = Key(version);
        if (_declaredVersions.Contains(key))
        {
            return VersionSupport.Declared;
        }

        if (Workshop != null && Workshop.TaggedFor(version))
        {
            return VersionSupport.WorkshopOnly;
        }

        return _folderVersions.Contains(key) ? VersionSupport.FolderOnly : VersionSupport.None;
    }

    public bool HasFolderFor(Version version)
    {
        return _folderVersions.Contains(Key(version));
    }

    public string SupportTip(Version version)
    {
        var key = Key(version);
        var lines = new List<string> { $"<b>{Mod.Name}</b> — RimWorld {key}" };
        lines.Add(_declaredVersions.Contains(key) ? I18n.CompatDeclared : I18n.CompatNotDeclared);
        if (_folderVersions.Contains(key))
        {
            lines.Add(I18n.CompatHasFolder(key));
        }

        var workshop = Workshop;
        if (workshop != null)
        {
            lines.Add(workshop.TaggedFor(version) ? I18n.CompatWorkshopTagged : I18n.CompatWorkshopNotTagged);
        }

        return lines.StringJoin("\n");
    }

    public static string Key(Version version)
    {
        return $"{version.Major}.{version.Minor}";
    }
}
