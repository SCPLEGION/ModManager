// WorkshopSearch.cs
// Paged Steam Workshop search for RimWorld items: free text, required/any tags, sort order and trend period.
// Only one query is in flight at a time; a newer request replaces any queued one, and results that arrive
// for a superseded request are discarded.

using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Verse;
using Verse.Steam;

namespace SCPModManager;

public static class WorkshopSearch
{
    public const int ResultsPerPage = 50;

    private static CallResult<SteamUGCQueryCompleted_t> _callResult;
    private static WorkshopSearchParams _inFlight;
    private static WorkshopSearchParams _queued;

    public static List<WorkshopItemInfo> Results { get; private set; } = [];
    public static WorkshopSearchParams Current { get; private set; }
    public static uint TotalResults { get; private set; }
    public static string Error { get; private set; }
    public static bool Loading => _inFlight != null || _queued != null;
    public static int Generation { get; private set; }

    public static uint PageCount => TotalResults == 0 ? 1 : (uint)Math.Ceiling(TotalResults / (float)ResultsPerPage);

    public static bool Available => SteamManager.Initialized;

    public static void Search(WorkshopSearchParams parameters)
    {
        if (!Available)
        {
            Error = I18n.WorkshopSteamUnavailable;
            return;
        }

        _queued = parameters.Clone();
        Dispatch();
    }

    private static void Dispatch()
    {
        if (_inFlight != null || _queued == null)
        {
            return;
        }

        var parameters = _queued;
        _queued = null;

        var appId = SteamUtils.GetAppID();
        var query = SteamUGC.CreateQueryAllUGCRequest(
            QueryType(parameters),
            EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse,
            appId, appId, Math.Max(1u, parameters.Page));
        if (query == UGCQueryHandle_t.Invalid)
        {
            // e.g. a page past the end; no callback would ever arrive, so fail now instead of loading forever
            Fail(parameters, "invalid query");
            return;
        }

        if (!parameters.Text.NullOrEmpty())
        {
            SteamUGC.SetSearchText(query, parameters.Text);
        }

        foreach (var tag in parameters.Tags)
        {
            SteamUGC.AddRequiredTag(query, tag);
        }

        if (parameters.Tags.Count > 1)
        {
            SteamUGC.SetMatchAnyTag(query, parameters.MatchAnyTag);
        }

        if (parameters.Sort == WorkshopSort.Trending)
        {
            SteamUGC.SetRankedByTrendDays(query, Math.Max(1u, parameters.TrendDays));
        }

        SteamUGC.SetReturnLongDescription(query, true);
        SteamUGC.SetAllowCachedResponse(query, 60);

        var call = SteamUGC.SendQueryUGCRequest(query);
        if (call == SteamAPICall_t.Invalid)
        {
            SteamUGC.ReleaseQueryUGCRequest(query);
            Fail(parameters, "request not sent");
            return;
        }

        _callResult ??= CallResult<SteamUGCQueryCompleted_t>.Create(OnQueryCompleted);
        _callResult.Set(call);
        _inFlight = parameters;
        Error = null;
    }

    private static void Fail(WorkshopSearchParams parameters, string reason)
    {
        Error = I18n.WorkshopQueryFailed(reason);
        Results = [];
        TotalResults = 0;
        Current = parameters;
        Generation++;
    }

    private static EUGCQuery QueryType(WorkshopSearchParams parameters)
    {
        return parameters.Sort switch
        {
            WorkshopSort.Trending => EUGCQuery.k_EUGCQuery_RankedByTrend,
            WorkshopSort.MostSubscribed => EUGCQuery.k_EUGCQuery_RankedByTotalUniqueSubscriptions,
            WorkshopSort.TopRated => EUGCQuery.k_EUGCQuery_RankedByVote,
            WorkshopSort.MostRecent => EUGCQuery.k_EUGCQuery_RankedByPublicationDate,
            WorkshopSort.RecentlyUpdated => EUGCQuery.k_EUGCQuery_RankedByLastUpdatedDate,
            _ => EUGCQuery.k_EUGCQuery_RankedByTextSearch
        };
    }

    private static void OnQueryCompleted(SteamUGCQueryCompleted_t result, bool failure)
    {
        var parameters = _inFlight;
        _inFlight = null;

        // a newer search was requested while this one ran; drop these results and run that one instead
        if (_queued == null)
        {
            if (failure || result.m_eResult != EResult.k_EResultOK)
            {
                Error = I18n.WorkshopQueryFailed(failure ? "IO failure" : result.m_eResult.ToString());
                Results = [];
                TotalResults = 0;
            }
            else
            {
                var results = new List<WorkshopItemInfo>((int)result.m_unNumResultsReturned);
                for (uint i = 0; i < result.m_unNumResultsReturned; i++)
                {
                    var info = WorkshopItemInfo.FromQuery(result.m_handle, i);
                    if (info == null)
                    {
                        continue;
                    }

                    results.Add(info);
                    WorkshopDetailsCache.Store(info);
                }

                Results = results;
                TotalResults = result.m_unTotalMatchingResults;
                Error = null;
            }

            Current = parameters;
            Generation++;
        }

        SteamUGC.ReleaseQueryUGCRequest(result.m_handle);
        Dispatch();
    }
}
