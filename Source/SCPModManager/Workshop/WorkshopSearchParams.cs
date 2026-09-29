// WorkshopSearchParams.cs
// One Workshop search: text, tags, sort, trend period and page. Also builds the equivalent website URL.

using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SCPModManager;

public class WorkshopSearchParams
{
    public bool MatchAnyTag;
    public uint Page = 1;
    public WorkshopSort Sort = WorkshopSort.Trending;
    public List<string> Tags = [];
    public string Text = string.Empty;
    public uint TrendDays = 7;

    public WorkshopSearchParams Clone()
    {
        return new WorkshopSearchParams
        {
            MatchAnyTag = MatchAnyTag,
            Page = Page,
            Sort = Sort,
            Tags = [..Tags],
            Text = Text,
            TrendDays = TrendDays
        };
    }

    /// <summary>The same search on the Steam Community website (used as a fallback without the Steam API).</summary>
    public string BrowserUrl
    {
        get
        {
            var url = "https://steamcommunity.com/workshop/browse/?appid=294100";
            if (!Text.NullOrEmpty())
            {
                url += $"&searchtext={Uri.EscapeDataString(Text)}";
            }

            url += Sort switch
            {
                WorkshopSort.Trending => $"&browsesort=trend&days={TrendDays}",
                WorkshopSort.MostSubscribed => "&browsesort=totaluniquesubscribers",
                WorkshopSort.TopRated => "&browsesort=toprated",
                WorkshopSort.MostRecent => "&browsesort=mostrecent",
                WorkshopSort.RecentlyUpdated => "&browsesort=lastupdated",
                _ => "&browsesort=textsearch"
            };
            url += Tags.Aggregate(string.Empty, (current, tag) => current + $"&requiredtags[]={Uri.EscapeDataString(tag)}");
            if (Page > 1)
            {
                url += $"&p={Page}";
            }

            return url;
        }
    }
}
