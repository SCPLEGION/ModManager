//#define DEBUG_PROFILE

using System.Reflection;
using HarmonyLib;
using Mlie;
using UnityEngine;
using Verse;

namespace SCPModManager;

public class SCPModManager : Mod
{
    public static string CurrentVersion;

    public SCPModManager(ModContentPack content) : base(content)
    {
        CurrentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
        Instance = this;
        UserData = new UserData();
        Settings = GetSettings<SCPModManagerSettings>();

        var harmonyInstance = new Harmony("scplegion.scpmodmanager");

#if DEBUG
            Harmony.DEBUG = true;
#endif
        harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

#if DEBUG_PROFILE
            LongEventHandler.ExecuteWhenFinished( () => new Profiler( typeof( Page_BetterModConfig ).GetMethod(
                                                                          nameof( Page_BetterModConfig.DoWindowContents
                                                                          ) ) ) );
#endif
    }

    public static SCPModManager Instance { get; private set; }

    public static UserData UserData { get; private set; }
    public static SCPModManagerSettings Settings { get; private set; }

    public override string SettingsCategory()
    {
        return I18n.SettingsCategory;
    }


    public override void WriteSettings()
    {
        base.WriteSettings();
        CrossPromotionManager.Notify_UpdateRelevantMods();
        CrossPromotionManager.Notify_CrossPromotionPathChanged();
    }

    public override void DoSettingsWindowContents(Rect canvas)
    {
        base.DoSettingsWindowContents(canvas);
        Settings.DoWindowContents(canvas);
    }
}