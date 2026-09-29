// ModSearchQuery.cs
// Parses and evaluates the mod-list search syntax:
//
//   plain words        match name, author or package id (all words must match)
//   "quoted phrase"    matched as a single term
//   -word              exclude matches
//   author:ludeon      field filters: name:, author: (a:), id: (package:), desc:, ver: (version:),
//                      list: (mod list name), tag: (Steam Workshop tag), is: (see IsFilters)
//   is:steam,local     comma-separated values are OR'ed
//
// Rank: 0 = no match, otherwise lower is better (name hit < author hit < id/other hit).

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Version = System.Version;

namespace SCPModManager;

public sealed class ModSearchQuery
{
    private const int MaxCached = 16;
    private static readonly Dictionary<string, ModSearchQuery> Cache = new();

    public static readonly string[] IsFilters =
    [
        "active", "inactive", "compatible", "outdated", "newer", "issues", "warnings", "steam", "local",
        "official", "dlc", "core", "settings", "localcopy", "duplicate", "missing", "update", "inlist"
    ];

    private readonly List<Term> _terms = [];

    private ModSearchQuery(string text)
    {
        Text = text ?? string.Empty;
        foreach (var token in Tokenize(Text))
        {
            var term = Term.Parse(token);
            if (term != null)
            {
                _terms.Add(term);
            }
        }
    }

    public string Text { get; }
    public bool IsEmpty => _terms.Count == 0;

    public static ModSearchQuery For(string text)
    {
        text ??= string.Empty;
        if (Cache.TryGetValue(text, out var query))
        {
            return query;
        }

        if (Cache.Count >= MaxCached)
        {
            Cache.Clear();
        }

        query = new ModSearchQuery(text);
        Cache.Add(text, query);
        return query;
    }

    public int Rank(ModButton button)
    {
        if (button == null)
        {
            return 0;
        }

        var rank = 1;
        foreach (var term in _terms)
        {
            var termRank = term.Evaluate(button);
            if (term.Negate)
            {
                if (termRank > 0)
                {
                    return 0;
                }

                continue;
            }

            if (termRank <= 0)
            {
                return 0;
            }

            rank = Math.Max(rank, termRank);
        }

        return rank;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var current = new System.Text.StringBuilder();
        var quoted = false;
        foreach (var c in text)
        {
            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(c) && !quoted)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }

                continue;
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static bool Contains(string haystack, string needle)
    {
        return !haystack.NullOrEmpty() && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private enum Field
    {
        Any,
        Name,
        Author,
        Id,
        Description,
        Version,
        List,
        Tag,
        Is
    }

    private sealed class Term
    {
        private Field _field;
        private string[] _values;
        private Version[] _versions;
        public bool Negate;

        public static Term Parse(string token)
        {
            var term = new Term();
            if (token.Length > 1 && token[0] == '-')
            {
                term.Negate = true;
                token = token.Substring(1);
            }

            var value = token;
            var colon = token.IndexOf(':');
            if (colon > 0 && colon < token.Length - 1)
            {
                var prefix = token.Substring(0, colon).ToLowerInvariant();
                var field = prefix switch
                {
                    "name" or "n" => Field.Name,
                    "author" or "a" or "by" => Field.Author,
                    "id" or "package" or "packageid" => Field.Id,
                    "desc" or "description" or "d" => Field.Description,
                    "ver" or "version" or "v" => Field.Version,
                    "list" or "profile" => Field.List,
                    "tag" or "t" => Field.Tag,
                    "is" or "source" or "has" => Field.Is,
                    _ => Field.Any
                };

                if (field != Field.Any)
                {
                    term._field = field;
                    value = token.Substring(colon + 1);
                }
            }

            if (value.NullOrEmpty())
            {
                return null;
            }

            term._values = value.Split([','], StringSplitOptions.RemoveEmptyEntries);
            if (term._field == Field.Version)
            {
                // parse each alternative on its own so "ver:1.4,1.5" means 1.4 or 1.5
                term._versions = term._values
                    .Select(v => Version.TryParse(v.Contains('.') ? v : $"{v}.0", out var parsed) ? parsed : null)
                    .ToArray();
            }

            return term._values.Length == 0 ? null : term;
        }

        public int Evaluate(ModButton button)
        {
            var best = 0;
            for (var i = 0; i < _values.Length; i++)
            {
                var rank = EvaluateValue(button, _values[i], _versions?[i]);
                if (rank > 0 && (best == 0 || rank < best))
                {
                    best = rank;
                }
            }

            return best;
        }

        private int EvaluateValue(ModButton button, string value, Version version)
        {
            var installed = button as ModButton_Installed;
            var mod = installed?.Selected;
            switch (_field)
            {
                case Field.Any:
                    if (MatchesName(button, value))
                    {
                        return 1;
                    }

                    if (mod != null && Contains(mod.AuthorsString, value))
                    {
                        return 2;
                    }

                    return Contains(button.Identifier, value) ? 3 : 0;
                case Field.Name:
                    return MatchesName(button, value) ? 1 : 0;
                case Field.Author:
                    return mod != null && Contains(mod.AuthorsString, value) ? 1 : 0;
                case Field.Id:
                    return Contains(button.Identifier, value) ||
                           button.SteamWorkshopId.ToString() == value
                        ? 1
                        : 0;
                case Field.Description:
                    return mod != null && Contains(mod.Description, value) ? 1 : 0;
                case Field.Version:
                    if (mod == null)
                    {
                        return 0;
                    }

                    if (version != null)
                    {
                        return mod.SupportedVersionsReadOnly.Any(v =>
                            v.Major == version.Major && v.Minor == version.Minor)
                            ? 1
                            : 0;
                    }

                    return Contains(mod.ModVersion, value) ? 1 : 0;
                case Field.List:
                    return installed != null && ModListManager.ListsFor(installed).Any(l => Contains(l.Name, value))
                        ? 1
                        : 0;
                case Field.Tag:
                    var info = WorkshopDetailsCache.Get(mod, false);
                    return info != null && info.Tags.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase))
                        ? 1
                        : 0;
                case Field.Is:
                    return MatchesIs(button, installed, mod, value.ToLowerInvariant()) ? 1 : 0;
                default:
                    return 0;
            }
        }

        private static bool MatchesName(ModButton button, string value)
        {
            // TrimmedName is the raw name when the TrimTags setting is off, matching what the list shows
            return Contains(button.TrimmedName, value);
        }

        private static bool MatchesIs(ModButton button, ModButton_Installed installed, ModMetaData mod, string value)
        {
            switch (value)
            {
                case "active" or "enabled" or "on":
                    return button.Active;
                case "inactive" or "available" or "disabled" or "off":
                    return !button.Active;
                case "missing":
                    return button is ModButton_Missing;
                case "downloading":
                    return button is ModButton_Downloading;
            }

            if (mod == null)
            {
                return false;
            }

            switch (value)
            {
                case "compatible" or "current":
                    return mod.VersionCompatible;
                case "outdated" or "old" or "incompatible":
                    return !mod.VersionCompatible && !mod.MadeForNewerVersion;
                case "newer":
                    return mod.MadeForNewerVersion;
                case "issues" or "issue" or "error" or "errors" or "problem" or "problems":
                    return button.Requirements.Any(r => r.Severity >= 2);
                case "warnings" or "warning":
                    return button.Requirements.Any(r => r.Severity >= 1);
                case "steam" or "workshop":
                    return mod.Source == ContentSource.SteamWorkshop;
                case "local" or "folder":
                    return mod.Source == ContentSource.ModsFolder && !mod.Official;
                case "official" or "ludeon":
                    return mod.Official;
                case "dlc" or "expansion":
                    return button.IsExpansion;
                case "core":
                    return button.IsCoreMod;
                case "settings" or "options":
                    return mod.HasSettings();
                case "localcopy" or "copy":
                    return mod.IsLocalCopy();
                case "duplicate" or "duplicates" or "multiple":
                    return installed.Versions.Count > 1;
                case "update" or "updates" or "outofdate":
                    return button.Requirements.Any(r => r is VersionCheck or SourceSync && r.IsApplicable &&
                                                        !r.IsSatisfied) ||
                           WorkshopDetailsCache.NeedsUpdate(mod);
                case "inlist" or "listed":
                    return ModListManager.ListsFor(installed).Any();
                case "unlisted":
                    return !ModListManager.ListsFor(installed).Any();
                case "colored" or "coloured":
                    return installed.Color != UnityEngine.Color.white;
                default:
                    return false;
            }
        }
    }
}
