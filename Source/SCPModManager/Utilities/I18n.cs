// I18n.cs
// Copyright Karel Kroeze, 2018-2018

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static SCPModManager.Utilities;
using Version = System.Version;

namespace SCPModManager;

public static class I18n
{
    private const string PREFIX = "SCPLegion.SCPModManager";

    public static readonly string YesAsTranslation = Key("YesAsTranslation").Translate();
    public static readonly string AvailableMods = Key("AvailableMods").Translate();
    public static readonly string ActiveMods = Key("ActiveMods").Translate();
    public static readonly string NoModSelected = Key("NoModSelected").Translate();

    public static readonly string Preview = Key("Preview").Translate();
    public static readonly string Title = "Title".Translate(); // core
    public static readonly string Author = "Author".Translate(); // core

    public static string Unknown = Key("Unknown").Translate();
    public static readonly string ModsChanged = "ModsChanged".Translate();
    public static readonly string Later = Key("Later").Translate();
    public static readonly string Dependencies = Key("Dependencies").Translate();
    public static readonly string Details = Key("Details").Translate();

    public static readonly string TabMods = Key("TabMods").Translate();
    public static readonly string TabProfiles = Key("TabProfiles").Translate();
    public static readonly string NoUpdatesAvailable = Key("NoUpdatesAvailable").Translate();

    // short verbs for the detail-panel action buttons (translated once, never per frame)
    public static readonly string ActionWorkshop = Key("ActionWorkshop").Translate();
    public static readonly string ActionLocalCopy = Key("ActionLocalCopy").Translate();
    public static readonly string ActionDeactivate = Key("ActionDeactivate").Translate();

    public static readonly string OK = "OK".Translate(); // core
    public static readonly string Yes = "Yes".Translate(); // core
    public static readonly string No = "No".Translate(); // core
    public static readonly string Cancel = Key("Cancel").Translate(); // you'd think this was in core...

    public static readonly string CurrentVersion = Key("CurrentVersion").Translate();

    public static string CoreNotFirst = Key("CoreNotFirst").Translate();

    public static readonly string GetMoreMods_SteamWorkshop = Key("GetMoreMods_SteamWorkshop").Translate();
    public static readonly string GetMoreMods_LudeonForums = Key("GetMoreMods_LudeonForums").Translate();
    public static readonly string UnSubscribe = Key("SteamWorkshop.UnSubscribe").Translate();
    public static readonly string ConfirmSteamWorkshopUpload = "ConfirmSteamWorkshopUpload".Translate(); // core
    public static readonly string ConfirmContentAuthor = "ConfirmContentAuthor".Translate(); // core
    public static string RebuildingModList_Key = Key("RebuildingModList");
    [Obsolete] public static string ExportModList = Key("ExportModList").Translate();
    [Obsolete] public static string ImportModListFromClipboard = Key("ImportModList").Translate();
    [Obsolete] public static string LoadModList = Key("LoadModList").Translate();
    [Obsolete] public static string AddModList = Key("AddModList").Translate();
    [Obsolete] public static string SaveModList = Key("SaveModList").Translate();
    public static readonly string DeleteModList = Key("DeleteModList").Translate();
    public static readonly string RenameModList = Key("RenameModList").Translate();

    // mass unsub
    public static readonly string MassUnSubscribe = Key("SteamWorkshop.MassUnSubscribe").Translate();
    public static readonly string MassUnSubscribeAll = Key("SteamWorkshop.MassUnSubscribeAll").Translate();
    public static readonly string MassUnSubscribeInactive = Key("SteamWorkshop.MassUnSubscribeInactive").Translate();
    public static readonly string MassUnSubscribeOutdated = Key("SteamWorkshop.MassUnSubscribeOutdated").Translate();

    // mass remove local
    public static readonly string MassRemoveLocal = Key("IO.MassRemoveLocal").Translate();
    public static readonly string MassRemoveLocalAll = Key("IO.MassRemoveLocalAll").Translate();
    public static readonly string MassRemoveLocalInactive = Key("IO.MassRemoveLocalInactive").Translate();
    public static readonly string MassRemoveLocalOutdated = Key("IO.MassRemoveLocalOutdated").Translate();

    public static readonly string CreateLocalCopies = Key("CreateLocalCopies").Translate();

    public static string NeedsWellFormattedTargetVersion = "MessageModNeedsWellFormattedTargetVersion".Translate(
        VersionControl.CurrentVersionString);

    // Modlists
    public static readonly string ModListsTip = Key("ModListsTip").Translate();

    public static readonly string Import = Key("Import").Translate();
    public static readonly string Export = Key("Export").Translate();
    public static readonly string Edit = Key("Edit").Translate();

    public static readonly string Import_FromModList = Key("Import.FromModList").Translate();
    public static readonly string Import_FromString = Key("Import.FromString").Translate();
    public static readonly string Import_FromSaveGame = Key("Import.FromSaveGame").Translate();
    public static readonly string Export_ToModList = Key("Export.ToModList").Translate();
    public static readonly string Export_ToString = Key("Export.ToString").Translate();
    public static readonly string CopyToClipboard = Key("CopyToClipboard").Translate();

    public static readonly string NameTooShort = Key("NameTooShort").Translate();
    public static string ImportModListFromSave = Key("LoadModListFromSave").Translate();
    public static string Problems = Key("Problems").Translate();

    public static readonly string AddToModList = Key("AddToModList").Translate();
    public static readonly string RemoveFromModList = Key("RemoveFromModList").Translate();


    // resolvers
    public static string MoveCoreToFirst = Key("MoveCoreToFirst").Translate();


    public static readonly string ChangeColour = Key("ChangeColour").Translate();

    public static readonly string ChangeListColour = Key("ChangeListColour").Translate();

    public static readonly string DialogConfirmIssuesCritical = Key("DialogConfirmIssuesCritical").Translate();

    // manifest
    public static string ManifestNotImplemented = Key("ManifestNotImplemented").Translate();
    public static string FetchingOnlineManifest = Key("FetchingOnlineManifest").Translate();

    public static readonly string LatestVersion = Key("LatestVersion").Translate();

    // workshop
    public static readonly string DownloadPending = Key("DownloadPending").Translate();

    public static readonly string SubscribeAllMissing = Key("SubscribeAllMissing").Translate();
    public static readonly string ResetMods = Key("ResetMods").Translate();
    public static readonly string ConfirmResetMods = Key("ConfirmResetMods").Translate();
    public static readonly string SortMods = Key("SortMods").Translate();

    // settings
    public static readonly string ModSettings = Key("ModSettings").Translate();
    public static readonly string ShowAllRequirements = Key("ShowAllRequirements").Translate();
    public static readonly string ShowAllRequirementsTip = Key("ShowAllRequirementsTip").Translate();
    public static readonly string AddSCPModManagerToNewModList = Key("AddSCPModManagerToNewModList").Translate();
    public static readonly string AddSCPModManagerToNewModListTip = Key("AddSCPModManagerToNewModListTip").Translate();
    public static readonly string AddHugsLibToNewModList = Key("AddHugsLibToNewModList").Translate();
    public static readonly string AddHugsLibToNewModListTip = Key("AddHugsLibToNewModListTip").Translate();
    public static readonly string AddExpansionsToNewModList = Key("AddExpansionsToNewModList").Translate();
    public static readonly string AddExpansionsToNewModListTip = Key("AddExpansionsToNewModListTip").Translate();
    public static readonly string ShowVersionChecksForSteamMods = Key("ShowVersionChecksForSteamMods").Translate();

    public static readonly string ShowVersionChecksForSteamModsTip =
        Key("ShowVersionChecksForSteamModsTip").Translate();

    public static readonly string UseTempFolderForCrossPromotionCache =
        Key("UseTempFolderForCrossPromotionCache").Translate();

    public static readonly string UseTempFolderForCrossPromotionCacheTip =
        Key("UseTempFolderForCrossPromotionCacheTip").Translate();

    public static readonly string DeleteCrossPromotionCache = Key("DeleteCrossPromotionCache").Translate();

    public static readonly string NoDownloadUri = Key("NoDownloadUri").Translate();


    // tabbed pages, search, Workshop browser, compatibility, profiles, issues
    public static readonly string TabWorkshop = Key("TabWorkshop").Translate();
    public static readonly string TabWorkshopTip = Key("TabWorkshopTip").Translate();
    public static readonly string TabCompatibility = Key("TabCompatibility").Translate();
    public static readonly string TabCompatibilityTip = Key("TabCompatibilityTip").Translate();
    public static readonly string TabIssues = Key("TabIssues").Translate();
    public static readonly string ActionActivate = Key("ActionActivate").Translate();
    public static readonly string ActionUnsubscribe = Key("ActionUnsubscribe").Translate();
    public static readonly string ActionImport = Key("ActionImport").Translate();
    public static readonly string ActionImportTip = Key("ActionImportTip").Translate();
    public static readonly string ActionExport = Key("ActionExport").Translate();
    public static readonly string ActionRename = Key("ActionRename").Translate();
    public static readonly string ActionColour = Key("ActionColour").Translate();
    public static readonly string ActionDelete = Key("ActionDelete").Translate();
    public static readonly string ActionFind = Key("ActionFind").Translate();
    public static readonly string ActionFix = Key("ActionFix").Translate();
    public static readonly string ActionShowInList = Key("ActionShowInList").Translate();
    public static readonly string ActionSubscribe = Key("ActionSubscribe").Translate();
    public static readonly string ActionUpdate = Key("ActionUpdate").Translate();
    public static readonly string CopyLink = Key("CopyLink").Translate();
    public static readonly string LinkCopied = Key("LinkCopied").Translate();
    public static readonly string Compatibility = Key("Compatibility").Translate();
    public static readonly string ScopeAll = Key("ScopeAll").Translate();
    public static readonly string ScopeActive = Key("ScopeActive").Translate();
    public static readonly string ScopeInactive = Key("ScopeInactive").Translate();
    public static readonly string NoMatchingMods = Key("NoMatchingMods").Translate();
    public static readonly string SearchPlaceholder = Key("SearchPlaceholder").Translate();
    public static readonly string SearchPlaceholderShort = Key("SearchPlaceholderShort").Translate();
    public static readonly string SearchHelpTitle = Key("SearchHelpTitle").Translate();
    public static readonly string SearchHelp = Key("SearchHelp").Translate();
    public static readonly string QuickFiltersTip = Key("QuickFiltersTip").Translate();
    public static readonly string QuickFilterOutdated = Key("QuickFilterOutdated").Translate();
    public static readonly string QuickFilterCompatible = Key("QuickFilterCompatible").Translate();
    public static readonly string QuickFilterIssues = Key("QuickFilterIssues").Translate();
    public static readonly string QuickFilterUpdates = Key("QuickFilterUpdates").Translate();
    public static readonly string QuickFilterSteam = Key("QuickFilterSteam").Translate();
    public static readonly string QuickFilterLocal = Key("QuickFilterLocal").Translate();
    public static readonly string QuickFilterOfficial = Key("QuickFilterOfficial").Translate();
    public static readonly string QuickFilterSettings = Key("QuickFilterSettings").Translate();
    public static readonly string QuickFilterDuplicates = Key("QuickFilterDuplicates").Translate();
    public static readonly string QuickFilterUnlisted = Key("QuickFilterUnlisted").Translate();
    public static readonly string SortTip = Key("SortTip").Translate();
    public static readonly string SortByDefault = Key("SortByDefault").Translate();
    public static readonly string SortByName = Key("SortByName").Translate();
    public static readonly string SortByAuthor = Key("SortByAuthor").Translate();
    public static readonly string SortBySource = Key("SortBySource").Translate();
    public static readonly string SortByUpdated = Key("SortByUpdated").Translate();
    public static readonly string WorkshopSearch = Key("WorkshopSearch").Translate();
    public static readonly string WorkshopSearchPlaceholder = Key("WorkshopSearchPlaceholder").Translate();
    public static readonly string WorkshopSearching = Key("WorkshopSearching").Translate();
    public static readonly string WorkshopNoResults = Key("WorkshopNoResults").Translate();
    public static readonly string WorkshopNothingSelected = Key("WorkshopNothingSelected").Translate();
    public static readonly string WorkshopSteamUnavailable = Key("WorkshopSteamUnavailable").Translate();
    public static readonly string WorkshopOpenInBrowser = Key("WorkshopOpenInBrowser").Translate();
    public static readonly string WorkshopOpenInSteam = Key("WorkshopOpenInSteam").Translate();
    public static readonly string WorkshopSortLabel = Key("WorkshopSortLabel").Translate();
    public static readonly string WorkshopSortTrending = Key("WorkshopSortTrending").Translate();
    public static readonly string WorkshopSortMostSubscribed = Key("WorkshopSortMostSubscribed").Translate();
    public static readonly string WorkshopSortTopRated = Key("WorkshopSortTopRated").Translate();
    public static readonly string WorkshopSortMostRecent = Key("WorkshopSortMostRecent").Translate();
    public static readonly string WorkshopSortRecentlyUpdated = Key("WorkshopSortRecentlyUpdated").Translate();
    public static readonly string WorkshopSortRelevance = Key("WorkshopSortRelevance").Translate();
    public static readonly string WorkshopTagsType = Key("WorkshopTagsType").Translate();
    public static readonly string WorkshopTagsVersion = Key("WorkshopTagsVersion").Translate();
    public static readonly string WorkshopTags = Key("WorkshopTags").Translate();
    public static readonly string WorkshopAddTag = Key("WorkshopAddTag").Translate();
    public static readonly string WorkshopAddTagTip = Key("WorkshopAddTagTip").Translate();
    public static readonly string WorkshopCustomTagTip = Key("WorkshopCustomTagTip").Translate();
    public static readonly string WorkshopMatchAny = Key("WorkshopMatchAny").Translate();
    public static readonly string WorkshopMatchAnyTip = Key("WorkshopMatchAnyTip").Translate();
    public static readonly string WorkshopHideInstalled = Key("WorkshopHideInstalled").Translate();
    public static readonly string WorkshopStateActive = Key("WorkshopStateActive").Translate();
    public static readonly string WorkshopStateInstalled = Key("WorkshopStateInstalled").Translate();
    public static readonly string WorkshopStateSubscribed = Key("WorkshopStateSubscribed").Translate();
    public static readonly string WorkshopStateDownloading = Key("WorkshopStateDownloading").Translate();
    public static readonly string WorkshopStateNeedsUpdate = Key("WorkshopStateNeedsUpdate").Translate();
    public static readonly string WorkshopUpdatePending = Key("WorkshopUpdatePending").Translate();
    public static readonly string WorkshopFirst = Key("WorkshopFirst").Translate();
    public static readonly string WorkshopPrevious = Key("WorkshopPrevious").Translate();
    public static readonly string WorkshopNext = Key("WorkshopNext").Translate();
    public static readonly string CompatColumnSource = Key("CompatColumnSource").Translate();
    public static readonly string CompatColumnModVersion = Key("CompatColumnModVersion").Translate();
    public static readonly string CompatColumnUpdated = Key("CompatColumnUpdated").Translate();
    public static readonly string CompatColumnIssues = Key("CompatColumnIssues").Translate();
    public static readonly string CompatColumnStatus = Key("CompatColumnStatus").Translate();
    public static readonly string CompatStatusCompatible = Key("CompatStatusCompatible").Translate();
    public static readonly string CompatStatusUpdatePending = Key("CompatStatusUpdatePending").Translate();
    public static readonly string CompatStatusNewer = Key("CompatStatusNewer").Translate();
    public static readonly string CompatUpdatePendingTip = Key("CompatUpdatePendingTip").Translate();
    public static readonly string CompatDeclared = Key("CompatDeclared").Translate();
    public static readonly string CompatNotDeclared = Key("CompatNotDeclared").Translate();
    public static readonly string CompatWorkshopTagged = Key("CompatWorkshopTagged").Translate();
    public static readonly string CompatWorkshopNotTagged = Key("CompatWorkshopNotTagged").Translate();
    public static readonly string CompatExport = Key("CompatExport").Translate();
    public static readonly string CompatExportTip = Key("CompatExportTip").Translate();
    public static readonly string CompatRefreshWorkshop = Key("CompatRefreshWorkshop").Translate();
    public static readonly string CompatRefreshWorkshopTip = Key("CompatRefreshWorkshopTip").Translate();
    public static readonly string ProfilesSaved = Key("ProfilesSaved").Translate();
    public static readonly string ProfileSaveCurrent = Key("ProfileSaveCurrent").Translate();
    public static readonly string ProfileSaveCurrentTip = Key("ProfileSaveCurrentTip").Translate();
    public static readonly string ProfileSearchPlaceholder = Key("ProfileSearchPlaceholder").Translate();
    public static readonly string NoProfiles = Key("NoProfiles").Translate();
    public static readonly string NoMatchingProfiles = Key("NoMatchingProfiles").Translate();
    public static readonly string ProfileLoad = Key("ProfileLoad").Translate();
    public static readonly string ProfileLoadTip = Key("ProfileLoadTip").Translate();
    public static readonly string ProfileMerge = Key("ProfileMerge").Translate();
    public static readonly string ProfileMergeTip = Key("ProfileMergeTip").Translate();
    public static readonly string ProfileMods = Key("ProfileMods").Translate();
    public static readonly string ProfileDiffTip = Key("ProfileDiffTip").Translate();
    public static readonly string ProfileStateInactive = Key("ProfileStateInactive").Translate();
    public static readonly string ProfileStateMissing = Key("ProfileStateMissing").Translate();
    public static readonly string IssuesNoProblems = Key("IssuesNoProblems").Translate();
    public static readonly string IssuesNoOutdated = Key("IssuesNoOutdated").Translate();
    public static readonly string AutoSortTip = Key("AutoSortTip").Translate();

    // options
    public static string SettingsCategory => Key("SettingsCategory").Translate();
    public static string ShowPromotions => Key("ShowPromotions").Translate();
    public static string ShowPromotionsTip => Key("ShowPromotionsTip").Translate();
    public static string ShowPromotions_NotSubscribed => Key("ShowPromotions_NotSubscribed").Translate();
    public static string ShowPromotions_NotActive => Key("ShowPromotions_NotActive").Translate();
    public static string TrimTags => Key("TrimTags").Translate();
    public static string TrimTagsTip => Key("TrimTagsTip").Translate();
    public static string TrimVersionStrings => Key("TrimVersionStrings").Translate();
    public static string TrimVersionStringsTip => Key("TrimVersionStringsTip").Translate();

    private static string Key(string key)
    {
        return $"{PREFIX}.{key}";
    }

    private static string Key(params string[] keys)
    {
        return $"{PREFIX}.{string.Join(".", keys)}";
    }

    public static string TargetVersions(string versions)
    {
        return Key("TargetVersions").Translate(versions);
    }

    public static string InvalidVersion(List<Version> versions)
    {
        return Key("InvalidVersion").Translate(versions.VersionList());
    }

    public static string DifferentVersion(ModMetaData mod)
    {
        return Key("DifferentVersion").Translate(mod.Name, mod.SupportedVersionsReadOnly.VersionList(),
            $"{VersionControl.CurrentMajor}.{VersionControl.CurrentMinor}");
    }

    public static string UpdateAvailable(Version current, Version latest)
    {
        return Key("UpdateAvailable").Translate(current.ToString(), latest.ToString());
    }

    public static string MissingMod(string name, string id)
    {
        return Key("MissingMod").Translate(name, id);
    }

    public static string DependencyNotFound(string name)
    {
        return Key("DependencyNotFound").Translate(name);
    }

    public static string DependencyUnknownVersion(ModMetaData tgt)
    {
        return Key("DependencyUnknownVersion").Translate(tgt.Name);
    }

    public static string DependencyWrongVersion(ModMetaData tgt, VersionedDependency depend)
    {
        return Key("DependencyWrongVersion")
            .Translate(tgt.Name, depend.Range.ToString(), tgt.GetManifest().Version.ToString());
    }

    public static string DependencyNotActive(ModMetaData tgt)
    {
        return Key("DependencyNotActive").Translate(tgt.Name);
    }

    public static string DependencyMet(ModMetaData tgt)
    {
        return Key("DependencyMet").Translate(tgt.Name, tgt.GetManifest()?.Version.ToString() ?? "[?]");
    }

    public static string IncompatibleMod(string name)
    {
        return Key("IncompatibleMod").Translate(name);
    }

    public static string LoadedBefore(string name)
    {
        return Key("LoadedBefore").Translate(name);
    }

    public static string ShouldBeLoadedBefore(string identifier)
    {
        return Key("ShouldBeLoadedBefore").Translate(identifier);
    }

    public static string LoadedAfter(string name)
    {
        return Key("LoadedAfter").Translate(name);
    }

    public static string ShouldBeLoadedAfter(string identifier)
    {
        return Key("ShouldBeLoadedAfter").Translate(identifier);
    }

    public static string MassUnSubscribeConfirm(int count, string list)
    {
        return Key("SteamWorkshop.MassUnSubscribeConfirm").Translate(count, list);
    }

    public static string MassRemoveLocalConfirm(int count, string list)
    {
        return Key("IO.MassRemoveLocalConfirm").Translate(count, list);
    }

    public static string CreateLocalCopiesConfirmation(int count)
    {
        return Key("CreateLocalCopiesConfirmation").Translate(count);
    }

    public static string CreateLocalCopy(string name)
    {
        return Key("CreateLocalCopy").Translate(name);
    }

    public static string CreateLocalSucceeded(string name)
    {
        return Key("CreateLocalSucceeded").Translate(name);
    }

    public static string CreateLocalFailed(string name)
    {
        return Key("CreateLocalFailed").Translate(name);
    }

    public static string CreatingLocal(string name)
    {
        return Key("CreatingLocal").Translate(name);
    }

    public static string RemovingLocal(string name)
    {
        return Key("RemovingLocal").Translate(name);
    }

    public static string DeleteLocalCopy(string name)
    {
        return Key("DeleteLocalCopy").Translate(name);
    }

    public static string ConfirmRemoveLocal(string name)
    {
        return Key("ConfirmRemoveLocal").Translate(name);
    }

    public static string RemoveLocalSucceeded(string name)
    {
        return Key("RemoveLocalSucceeded").Translate(name);
    }

    public static string RemoveLocalFailed(string name)
    {
        return Key("RemoveLocalFailed").Translate(name);
    }

    public static string ConfirmUnsubscribe(string name)
    {
        return "ConfirmUnsubscribe".Translate(name);
    } // core 

    public static string InvalidName(string name, string invalidChars)
    {
        return Key("InvalidName").Translate(name, invalidChars);
    }

    public static string ConfirmOverwriteModList(string name)
    {
        return Key("ConfirmOverwriteModList").Translate(name);
    }

    public static string ModListRenamed(string oldName, string newName)
    {
        return Key("ModListRenamed").Translate(oldName, newName);
    }

    public static string ModListCreated(string name)
    {
        return Key("ModListCreated").Translate(name);
    }

    public static string ModListLoaded(string name)
    {
        return Key("ModListLoaded").Translate(name);
    }

    public static string ModListDeleted(string name)
    {
        return Key("ModListDeleted").Translate(name);
    }

    public static string ModListCopiedToClipboard(string name)
    {
        return Key("ModListCopiedToClipboard").Translate(name);
    }

    public static string ModListCreatedFromClipboard(string name)
    {
        return Key("ModListCreatedFromClipboard").Translate(name);
    }

    public static string FailedToCreateModListFromClipboard(string reason)
    {
        return Key("FailedToCreateModListFromClipboard").Translate(reason);
    }

    public static string AddToModListX(string name)
    {
        return Key("AddToModListX").Translate(name);
    }

    public static string RemoveFromModListX(string name)
    {
        return Key("RemoveFromModListX").Translate(name);
    }

    public static string SearchSteamWorkshop(string name)
    {
        return Key("SearchSteamWorkshop").Translate(name);
    }

    public static string SearchForum(string name)
    {
        return Key("SearchForum").Translate(name);
    }

    public static string ActivateMod(ModMetaData mod)
    {
        return Key("ActivateMod").Translate(mod.Name, Manifest.For(mod)?.Version.ToString(),
            Key("ContentSource", mod.Source.ToString()).Translate());
    }

    //        public static string NoMatchingModInstalled(string name, Version desired, Dependency.EqualityOperator op )
    //        {
    //            return Key( "NoMatchingModInstalled_Version" )
    //                .Translate( name, VersionControl.CurrentMajor + "." + VersionControl.CurrentMinor, desired,
    //                    Dependency.OperatorToString( op ) );
    //        }
    public static string NoMatchingModInstalled(string name)
    {
        return Key("NoMatchingModInstalled")
            .Translate(name, $"{VersionControl.CurrentMajor}.{VersionControl.CurrentMinor}");
    }

    public static string DeactivateMod(ModButton_Installed mod)
    {
        return Key("DeactivateMod").Translate(TrimModName(mod.Name));
    }

    public static string MoveBefore(ModButton_Installed from, ModButton_Installed to)
    {
        return Key("MoveBefore").Translate(TrimModName(from.Name), TrimModName(to.Name));
    }

    public static string MoveAfter(ModButton_Installed from, ModButton_Installed to)
    {
        return Key("MoveAfter").Translate(TrimModName(from.Name), TrimModName(to.Name));
    }

    public static string ChangeModColour(string name)
    {
        return Key("ChangeModColour").Translate(name);
    }

    public static string ChangeButtonColour(string name)
    {
        return Key("ChangeButtonColour").Translate(name);
    }

    public static string ModHomePage(string url)
    {
        return Key("ModHomePage").Translate(url);
    }

    public static string WorkshopPage(string subject)
    {
        return Key("WorkshopPage").Translate(subject);
    }

    public static string DialogConfirmIssues(string warning, string description)
    {
        return Key("DialogConfirmIssues").Translate(warning, description).Resolve();
    }

    public static string DialogConfirmIssuesTitle(int count)
    {
        return Key("DialogConfirmIssuesTitle").Translate(count);
    }

    public static string FetchingOnlineManifestFailed(string error)
    {
        return Key("FetchingOnlineManifestFailed").Translate(error);
    }

    public static string NewVersionAvailable(Version current, Version latest)
    {
        return Key("NewVersionAvailable").Translate(current.ToString(), latest.ToString());
    }

    public static string ModInstalled(string name)
    {
        return Key("ModInstalled").Translate(name);
    }

    public static string Subscribe(string name)
    {
        return Key("Subscribe").Translate(name);
    }

    public static string SortFailed_Cyclic(string a, string b)
    {
        return Key("SortFailed.CyclicDependency").Translate(a, b);
    }

    // promotions
    public static string PromotionsFor(string author)
    {
        return Key("PromotionsFor").Translate(author);
    }

    public static string CrossPromotionCacheFolderSize(long size)
    {
        return Key("CrossPromotionCacheFolderSize").Translate(size.ToStringSize());
    }

    public static string ConfirmDeletingCrossPromotionCache(string path, int count, long size)
    {
        return Key("ConfirmDeletingCrossPromotionCache").Translate(path, count, size.ToStringSize());
    }

    public static string OpenDownloadUri(string downloadUri)
    {
        return Key("OpenDownloadUri").Translate(downloadUri).Resolve();
    }

    public static string XIsUpToDate(ModMetaData mod)
    {
        return Key("XIsUpToDate").Translate(mod.Name);
    }

    public static string YHasUpdated(ModMetaData target)
    {
        return Key("YHasUpdated").Translate(target.Name);
    }

    public static string UpdateLocalCopy(ModMetaData mod)
    {
        return Key("UpdateLocalCopy").Translate(mod.Name);
    }

    public static string XModsImportedFromString(int count)
    {
        return Key("XModsImportedFromString").Translate(count);
    }

    public static string XModsExportedToString(int count)
    {
        return Key("XModsExportedToString").Translate(count);
    }

    public static string TabShortcut(int index)
    {
        return Key("TabShortcut").Translate(index);
    }

    public static string HeaderSummary(int active, int installed, string version)
    {
        return Key("HeaderSummary").Translate(active, installed, version);
    }

    public static string QuickFilterVersion(string version)
    {
        return Key("QuickFilterVersion").Translate(version);
    }

    public static string WorkshopQueryFailed(string reason)
    {
        return Key("WorkshopQueryFailed").Translate(reason);
    }

    public static string WorkshopTrendDays(uint days)
    {
        return Key("WorkshopTrendDays").Translate((int)days);
    }

    public static string WorkshopSubscribers(string count)
    {
        return Key("WorkshopSubscribers").Translate(count);
    }

    public static string WorkshopRating(int percent)
    {
        return Key("WorkshopRating").Translate(percent);
    }

    public static string WorkshopVotes(uint up, uint down)
    {
        return Key("WorkshopVotes").Translate((int)up, (int)down);
    }

    public static string WorkshopUpdated(string date)
    {
        return Key("WorkshopUpdated").Translate(date);
    }

    public static string WorkshopCreated(string date)
    {
        return Key("WorkshopCreated").Translate(date);
    }

    public static string WorkshopBy(string author)
    {
        return Key("WorkshopBy").Translate(author);
    }

    public static string WorkshopMoreByAuthor(string author)
    {
        return Key("WorkshopMoreByAuthor").Translate(author);
    }

    public static string WorkshopFilterByTag(string tag)
    {
        return Key("WorkshopFilterByTag").Translate(tag);
    }

    public static string WorkshopPageStatus(uint page, uint pages, uint total)
    {
        return Key("WorkshopPageStatus").Translate((int)page, (int)pages, (int)total);
    }

    public static string CompatStatusOutdated(string latest)
    {
        return Key("CompatStatusOutdated").Translate(latest);
    }

    public static string CompatHasFolder(string version)
    {
        return Key("CompatHasFolder").Translate(version);
    }

    public static string CompatGameVersion(string version)
    {
        return Key("CompatGameVersion").Translate(version);
    }

    public static string CompatCountCompatible(int count)
    {
        return Key("CompatCountCompatible").Translate(count);
    }

    public static string CompatCountOutdated(int count)
    {
        return Key("CompatCountOutdated").Translate(count);
    }

    public static string CompatCountPending(int count)
    {
        return Key("CompatCountPending").Translate(count);
    }

    public static string CompatCountNewer(int count)
    {
        return Key("CompatCountNewer").Translate(count);
    }

    public static string CompatFilterBy(string filter)
    {
        return Key("CompatFilterBy").Translate(filter);
    }

    public static string CompatExported(int count)
    {
        return Key("CompatExported").Translate(count);
    }

    public static string CompatWorkshopLoaded(int count)
    {
        return Key("CompatWorkshopLoaded").Translate(count);
    }

    public static string CompatWorkshopLoading(int loaded, int requested)
    {
        return Key("CompatWorkshopLoading").Translate(loaded, requested);
    }

    public static string ProfileModCount(int count)
    {
        return Key("ProfileModCount").Translate(count);
    }

    public static string ProfileInstalled(int count)
    {
        return Key("ProfileInstalled").Translate(count);
    }

    public static string ProfileMissing(int count)
    {
        return Key("ProfileMissing").Translate(count);
    }

    public static string ProfileDiff(int activate, int deactivate)
    {
        return Key("ProfileDiff").Translate(activate, deactivate);
    }

    public static string ProfileSubscribeMissing(int count)
    {
        return Key("ProfileSubscribeMissing").Translate(count);
    }

    public static string ConfirmDeleteProfile(string name)
    {
        return Key("ConfirmDeleteProfile").Translate(name);
    }

    public static string IssuesProblems(int count)
    {
        return Key("IssuesProblems").Translate(count);
    }

    public static string IssuesUpdates(int count)
    {
        return Key("IssuesUpdates").Translate(count);
    }

    public static string IssuesOutdated(int count)
    {
        return Key("IssuesOutdated").Translate(count);
    }

    public static string IssuesMissing(int count)
    {
        return Key("IssuesMissing").Translate(count);
    }

    public static string IssuesSummary(int problems, int updates, int outdated)
    {
        return Key("IssuesSummary").Translate(problems, updates, outdated);
    }

    public static string DeactivateOutdated(int count)
    {
        return Key("DeactivateOutdated").Translate(count);
    }

    public static string ConfirmDeactivateOutdated(int count, string names)
    {
        return Key("ConfirmDeactivateOutdated").Translate(count, names);
    }

    public static string WorkshopVersionTipTagged(string version)
    {
        return Key("WorkshopVersionTipTagged").Translate(version);
    }

    public static string WorkshopVersionTipNotTagged(string version)
    {
        return Key("WorkshopVersionTipNotTagged").Translate(version);
    }

    public static string WorkshopVersionTipLocal(string version)
    {
        return Key("WorkshopVersionTipLocal").Translate(version);
    }

    public static string WorkshopVersionTipNotLocal(string version)
    {
        return Key("WorkshopVersionTipNotLocal").Translate(version);
    }

    public static string WorkshopVersionTip(string version, bool tagged, bool? installedSupports)
    {
        var tip = tagged ? WorkshopVersionTipTagged(version) : WorkshopVersionTipNotTagged(version);
        if (installedSupports.HasValue)
        {
            tip += "\n" + (installedSupports.Value ? WorkshopVersionTipLocal(version) : WorkshopVersionTipNotLocal(version));
        }

        return tip;
    }
}