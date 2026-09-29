// WorkshopItemInfo.cs
// Snapshot of one Steam Workshop item as returned by a UGC query (search results and the details cache).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using Steamworks;
using Verse;
using Version = System.Version;

namespace SCPModManager;

public class WorkshopItemInfo
{
    private static readonly Regex BBCodeTag = new(@"\[/?[a-z0-9*]+(=[^\]]*)?\]", RegexOptions.IgnoreCase);
    private static readonly Regex VersionTag = new(@"^\d+\.\d+$");

    private static readonly Dictionary<ulong, string> PersonaNames = new();

    private string _cleanDescription;
    private List<Version> _versions;

    public PublishedFileId_t FileId;
    public string Title;
    public string Description;
    public List<string> Tags = [];
    public ulong OwnerId;
    public DateTime Created;
    public DateTime Updated;
    public uint VotesUp;
    public uint VotesDown;
    public float Score;
    public ulong Subscribers;
    public ulong Favorites;
    public string PreviewUrl;
    public int FileSize;
    public bool Banned;

    public string CleanDescription => _cleanDescription ??= BBCodeTag.Replace(Description ?? string.Empty, "").Trim();

    /// <summary>Game versions the item is tagged with on the Workshop (tags such as "1.5", "1.6").</summary>
    public List<Version> TaggedVersions
    {
        get
        {
            if (_versions != null)
            {
                return _versions;
            }

            _versions = [];
            foreach (var tag in Tags)
            {
                if (VersionTag.IsMatch(tag) && Version.TryParse(tag, out var version))
                {
                    _versions.Add(version);
                }
            }

            _versions.Sort();
            return _versions;
        }
    }

    public bool TaggedForCurrentVersion => TaggedVersions.Any(v =>
        v.Major == VersionControl.CurrentMajor && v.Minor == VersionControl.CurrentMinor);

    public bool TaggedFor(Version version)
    {
        return TaggedVersions.Any(v => v.Major == version.Major && v.Minor == version.Minor);
    }

    public int RatingPercent => VotesUp + VotesDown == 0 ? -1 : (int)(VotesUp * 100f / (VotesUp + VotesDown));

    public string Url => SteamUtility.SteamWorkshopPageUrl(FileId);

    public EItemState State => SteamManagerReady ? (EItemState)SteamUGC.GetItemState(FileId) : EItemState.k_EItemStateNone;

    public bool Subscribed => (State & EItemState.k_EItemStateSubscribed) != 0;

    public bool Downloading => (State & (EItemState.k_EItemStateDownloading | EItemState.k_EItemStateDownloadPending)) != 0;

    public bool NeedsUpdate => (State & EItemState.k_EItemStateNeedsUpdate) != 0;

    public ModMetaData InstalledMod => WorkshopDetailsCache.InstalledMod(FileId);

    public string AuthorName
    {
        get
        {
            if (OwnerId == 0 || !SteamManagerReady)
            {
                return I18n.Unknown;
            }

            if (PersonaNames.TryGetValue(OwnerId, out var name))
            {
                return name;
            }

            var id = new CSteamID(OwnerId);
            // returns false once the name is available locally
            if (SteamFriends.RequestUserInformation(id, true))
            {
                return "…";
            }

            name = SteamFriends.GetFriendPersonaName(id);
            if (!name.NullOrEmpty() && name != "[unknown]")
            {
                PersonaNames[OwnerId] = name;
            }

            return name.NullOrEmpty() ? I18n.Unknown : name;
        }
    }

    private static bool SteamManagerReady => Verse.Steam.SteamManager.Initialized;

    public static WorkshopItemInfo FromQuery(UGCQueryHandle_t handle, uint index)
    {
        if (!SteamUGC.GetQueryUGCResult(handle, index, out var details) ||
            details.m_eResult != EResult.k_EResultOK)
        {
            return null;
        }

        var info = new WorkshopItemInfo
        {
            FileId = details.m_nPublishedFileId,
            Title = details.m_rgchTitle,
            Description = details.m_rgchDescription,
            Tags = (details.m_rgchTags ?? string.Empty)
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList(),
            OwnerId = details.m_ulSteamIDOwner,
            Created = FromUnix(details.m_rtimeCreated),
            Updated = FromUnix(details.m_rtimeUpdated),
            VotesUp = details.m_unVotesUp,
            VotesDown = details.m_unVotesDown,
            Score = details.m_flScore,
            FileSize = details.m_nFileSize,
            Banned = details.m_bBanned
        };

        if (SteamUGC.GetQueryUGCStatistic(handle, index, EItemStatistic.k_EItemStatistic_NumSubscriptions,
                out var subscribers))
        {
            info.Subscribers = subscribers;
        }

        if (SteamUGC.GetQueryUGCStatistic(handle, index, EItemStatistic.k_EItemStatistic_NumFavorites,
                out var favorites))
        {
            info.Favorites = favorites;
        }

        if (SteamUGC.GetQueryUGCPreviewURL(handle, index, out var url, 1024))
        {
            info.PreviewUrl = url;
        }

        return info;
    }

    private static DateTime FromUnix(uint seconds)
    {
        return seconds == 0 ? DateTime.MinValue : DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
    }

    public static string FormatCount(ulong count)
    {
        return count switch
        {
            >= 1_000_000 => $"{count / 1_000_000f:0.#}M",
            >= 10_000 => $"{count / 1_000f:0}k",
            >= 1_000 => $"{count / 1_000f:0.#}k",
            _ => count.ToString()
        };
    }

    public static string FormatDate(DateTime date)
    {
        return date == DateTime.MinValue ? "—" : date.ToString("yyyy-MM-dd");
    }
}
