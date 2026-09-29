// WebTextureCache.cs
// Loads Workshop preview images by URL without blocking the UI: requests are started lazily (only for rows
// that are actually drawn), a few at a time, and polled from the window's update loop. Textures are kept in a
// bounded cache and destroyed when evicted.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace SCPModManager;

public static class WebTextureCache
{
    private const int MaxConcurrent = 6;
    private const int MaxCached = 250;

    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static readonly LinkedList<string> Order = new();
    private static readonly HashSet<string> Failed = [];
    private static readonly Dictionary<string, UnityWebRequest> Running = new();
    private static readonly List<string> Pending = [];
    private static readonly List<string> Finished = [];

    /// <summary>Returns the texture if it is loaded; otherwise queues it and returns null.</summary>
    public static Texture2D Get(string url)
    {
        if (url.NullOrEmpty() || Failed.Contains(url))
        {
            return null;
        }

        if (Textures.TryGetValue(url, out var texture))
        {
            return texture;
        }

        if (!Running.ContainsKey(url) && !Pending.Contains(url))
        {
            // most recently drawn rows first
            Pending.Insert(0, url);
        }

        return null;
    }

    public static void Update()
    {
        Finished.Clear();
        foreach (var pair in Running)
        {
            var request = pair.Value;
            if (!request.isDone)
            {
                continue;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = null;
                try
                {
                    texture = DownloadHandlerTexture.GetContent(request);
                }
                catch (System.Exception e)
                {
                    Debug.Log($"WebTextureCache: could not decode {pair.Key}: {e.Message}");
                }

                if (texture != null)
                {
                    texture.name = "SCPModManager_WorkshopPreview";
                    Add(pair.Key, texture);
                }
                else
                {
                    Failed.Add(pair.Key);
                }
            }
            else
            {
                Failed.Add(pair.Key);
            }

            request.Dispose();
            Finished.Add(pair.Key);
        }

        foreach (var url in Finished)
        {
            Running.Remove(url);
        }

        while (Running.Count < MaxConcurrent && Pending.Count > 0)
        {
            var url = Pending[0];
            Pending.RemoveAt(0);
            var request = UnityWebRequestTexture.GetTexture(url, true);
            request.SendWebRequest();
            Running.Add(url, request);
        }

        // don't let a fast scroll build up a long backlog of rows that are no longer visible
        if (Pending.Count > MaxConcurrent * 4)
        {
            Pending.RemoveRange(MaxConcurrent * 4, Pending.Count - (MaxConcurrent * 4));
        }
    }

    /// <summary>Release every cached preview and abort pending downloads (called when the window closes).</summary>
    public static void Clear()
    {
        foreach (var request in Running.Values)
        {
            request.Abort();
            request.Dispose();
        }

        Running.Clear();
        Pending.Clear();
        foreach (var texture in Textures.Values)
        {
            Object.Destroy(texture);
        }

        Textures.Clear();
        Order.Clear();
        Failed.Clear();
    }

    private static void Add(string url, Texture2D texture)
    {
        Textures[url] = texture;
        Order.AddLast(url);
        while (Order.Count > MaxCached)
        {
            var oldest = Order.First.Value;
            Order.RemoveFirst();
            if (Textures.TryGetValue(oldest, out var old))
            {
                Object.Destroy(old);
                Textures.Remove(oldest);
            }
        }
    }
}
