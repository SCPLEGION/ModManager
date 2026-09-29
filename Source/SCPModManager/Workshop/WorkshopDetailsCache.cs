// WorkshopDetailsCache.cs
// Fetches (in batches of up to 50) and caches Steam Workshop details for installed mods: tags (and so the
// game versions the Workshop lists), last-updated time, subscribers and rating. Also maps published file ids
// back to installed mods.

using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace SCPModManager;

public static class WorkshopDetailsCache
{
    private const int BatchSize = 50; // kNumUGCResultsPerPage
    private const float InstalledLookupLifetime = 2f;

    private static readonly Dictionary<PublishedFileId_t, WorkshopItemInfo> Cache = new();
    private static readonly HashSet<PublishedFileId_t> Requested = [];
    private static readonly Queue<PublishedFileId_t> Queue = new();
    private static readonly Dictionary<PublishedFileId_t, ModMetaData> InstalledByFileId = new();

    private static CallResult<SteamUGCQueryCompleted_t> _callResult;
    private static bool _inFlight;
    private static float _installedLookupTime = -1f;

    public static bool Available => SteamManager.Initialized;
    public static bool Busy => _inFlight || Queue.Count > 0;
    public static int CachedCount => Cache.Count;
    public static int RequestedCount => Requested.Count;

    public static bool TryGet(PublishedFileId_t fileId, out WorkshopItemInfo info)
    {
        return Cache.TryGetValue(fileId, out info);
    }

    public static WorkshopItemInfo Get(ModMetaData mod, bool request = true)
    {
        if (mod == null)
        {
            return null;
        }

        var fileId = mod.GetPublishedFileId();
        if (fileId == PublishedFileId_t.Invalid)
        {
            return null;
        }

        if (Cache.TryGetValue(fileId, out var info))
        {
            return info;
        }

        if (request)
        {
            Request(fileId);
        }

        return null;
    }

    /// <summary>Queue details requests for every installed mod that has a Workshop id.</summary>
    public static void RequestAllInstalled()
    {
        foreach (var fileId in InstalledIds())
        {
            Request(fileId, false);
        }

        Pump();
    }

    public static void Request(PublishedFileId_t fileId, bool pump = true)
    {
        if (!Available || fileId == PublishedFileId_t.Invalid || !Requested.Add(fileId))
        {
            return;
        }

        Queue.Enqueue(fileId);
        if (pump)
        {
            Pump();
        }
    }

    /// <summary>Drop everything so the next request re-fetches fresh data from Steam.</summary>
    public static void Invalidate()
    {
        Cache.Clear();
        Requested.Clear();
        Queue.Clear();
        _installedLookupTime = -1f;
    }

    /// <summary>Record details that were fetched by another query (e.g. a Workshop search).</summary>
    public static void Store(WorkshopItemInfo info)
    {
        if (info == null)
        {
            return;
        }

        Cache[info.FileId] = info;
        Requested.Add(info.FileId);
    }

    public static ModMetaData InstalledMod(PublishedFileId_t fileId)
    {
        RefreshInstalledLookup();
        return InstalledByFileId.TryGetValue(fileId, out var mod) ? mod : null;
    }

    private static IEnumerable<PublishedFileId_t> InstalledIds()
    {
        RefreshInstalledLookup();
        return InstalledByFileId.Keys.ToList();
    }

    private static void RefreshInstalledLookup()
    {
        if (_installedLookupTime >= 0f && Time.realtimeSinceStartup - _installedLookupTime < InstalledLookupLifetime)
        {
            return;
        }

        _installedLookupTime = Time.realtimeSinceStartup;
        InstalledByFileId.Clear();
        foreach (var mod in ModLister.AllInstalledMods)
        {
            var fileId = mod.GetPublishedFileId();
            if (fileId == PublishedFileId_t.Invalid)
            {
                continue;
            }

            // prefer the Workshop copy over a local copy that carries the same PublishedFileId.txt
            if (!InstalledByFileId.ContainsKey(fileId) || mod.Source == ContentSource.SteamWorkshop)
            {
                InstalledByFileId[fileId] = mod;
            }
        }
    }

    private static void Pump()
    {
        if (_inFlight || Queue.Count == 0 || !Available)
        {
            return;
        }

        var batch = new List<PublishedFileId_t>(BatchSize);
        while (batch.Count < BatchSize && Queue.Count > 0)
        {
            batch.Add(Queue.Dequeue());
        }

        _callResult ??= CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
        var query = SteamUGC.CreateQueryUGCDetailsRequest(batch.ToArray(), (uint)batch.Count);
        SteamUGC.SetAllowCachedResponse(query, 300);
        var call = SteamUGC.SendQueryUGCRequest(query);
        _callResult.Set(call);
        _inFlight = true;
        Debug.Log($"WorkshopDetailsCache: requested details for {batch.Count} item(s)");
    }

    private static void OnQueryCompleted(SteamUGCQueryCompleted_t result, bool failure)
    {
        _inFlight = false;
        if (!failure && result.m_eResult == EResult.k_EResultOK)
        {
            for (uint i = 0; i < result.m_unNumResultsReturned; i++)
            {
                var info = WorkshopItemInfo.FromQuery(result.m_handle, i);
                if (info != null)
                {
                    Cache[info.FileId] = info;
                }
            }
        }
        else
        {
            Debug.Log($"WorkshopDetailsCache: query failed ({result.m_eResult}, io failure: {failure})");
        }

        SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
        Pump();
    }
}
