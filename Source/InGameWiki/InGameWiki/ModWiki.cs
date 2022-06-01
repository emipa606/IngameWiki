using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using InGameWiki.Internal;
using Verse;

namespace InGameWiki;

public class ModWiki
{
    private static readonly List<ModWiki> allWikis = new List<ModWiki>();

    private static int wikiModInstallStatus;

    public List<WikiPage> Pages = new List<WikiPage>();

    public string WikiTitle = "Your Mod Name Here";

    private ModWiki()
    {
    }

    public static string APIVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public static bool IsWikiModInstalled
    {
        get
        {
            if (wikiModInstallStatus != 0)
            {
                return wikiModInstallStatus == 1;
            }

            wikiModInstallStatus = 2;
            if (ModLister.GetModWithIdentifier("Mlie.IngameWiki") != null)
            {
                wikiModInstallStatus = 1;
            }
            else
            {
                Log.Message("<color=cyan>Wiki mod is installed correctly, full API enabled.</color>");
            }
            //foreach (var modHandle in LoadedModManager.get)
            //{
            //    if (modHandle.Content.PackageId is not ("co.uk.epicguru.ingamewiki" or ))
            //    {
            //        continue;
            //    }

            //    wikiModInstallStatus = 1;
            //    Log.Message("<color=cyan>Wiki mod is installed correctly, full API enabled.</color>");
            //    break;
            //}

            return wikiModInstallStatus == 1;
        }
    }

    public static IReadOnlyList<ModWiki> AllWikis => allWikis;

    public Mod Mod { get; private set; }

    public bool NoSpoilerMode { get; set; } = true;


    public static void Patch(Harmony harmonyInstance, bool doInspectorButton)
    {
        if (harmonyInstance == null)
        {
            return;
        }

        var method = typeof(Dialog_InfoCard).GetMethod("DoWindowContents", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
        {
            Log.Error(
                "Failed to get method Dialog_InfoCard.DoWindowContents to patch. Did Rimworld update in a major way?");
            return;
        }

        var method2 =
            typeof(InspectorPatch).GetMethod("InGameWikiPostfix", BindingFlags.Static | BindingFlags.NonPublic);
        if (method2 == null)
        {
            Log.Error("Failed to get local patch method...");
            return;
        }

        var postfix = new HarmonyMethod(method2);
        var alreadyPatched = true;
        var patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo != null)
        {
            foreach (var postfix2 in patchInfo.Postfixes)
            {
                if (postfix2.PatchMethod.Name == "InGameWikiPostfix")
                {
                    alreadyPatched = false;
                    break;
                }

                Log.Warning("There is already a postfix on Dialog_InfoCard.DoWindowContents: " +
                            postfix2.PatchMethod.Name + " by " + postfix2.owner +
                            ". This could affect functionality of wiki patch.");
            }
        }

        if (!alreadyPatched)
        {
            return;
        }

        if (doInspectorButton)
        {
            harmonyInstance.Patch(method, null, postfix);
        }

        Log.Message("<color=cyan>Patched game for in-game wiki. Inspector button is " +
                    (doInspectorButton ? "enabled" : "<color=red>disabled</color>") + "</color>");
    }

    public static void OpenInspectWindow(Def def)
    {
        if (def != null)
        {
            Find.WindowStack.Add(new Dialog_InfoCard(def));
        }
    }

    public static ModWiki Create(Mod mod)
    {
        return Create(mod, new ModWiki());
    }

    public static ModWiki Create(Mod mod, ModWiki wiki)
    {
        if (mod == null)
        {
            Log.Error("Cannot pass in null mod to create wiki.");
            return null;
        }

        if (wiki == null)
        {
            Log.Error("Cannot pass in null ModWiki instance to create wiki.");
            return null;
        }

        try
        {
            wiki.Mod = mod;
            if (!IsWikiModInstalled)
            {
                Log.Warning("A wiki was registered for mod '" + mod.Content.Name +
                            "', but the InGameWiki mod is not installed. Dummy wiki has been created instead.");
                return wiki;
            }

            wiki.GenerateFromMod(mod);
            allWikis.Add(wiki);
            Log.Message("<color=cyan>A new wiki was registered for mod '" + mod.Content.Name + "'.</color>");
            return wiki;
        }
        catch (Exception arg)
        {
            Log.Error($"Exception creating wiki for {mod.Content.Name}: {arg}");
            return null;
        }
    }

    public static (ModWiki wiki, WikiPage page) GlobalFindPageFromDef(string defName)
    {
        if (defName == null)
        {
            return (null, null);
        }

        foreach (var allWiki in AllWikis)
        {
            var wikiPage = allWiki.FindPageFromDef(defName);
            if (wikiPage != null)
            {
                return (allWiki, wikiPage);
            }
        }

        return (null, null);
    }

    public static (ModWiki wiki, WikiPage page) GlobalFindPageFromID(string pageID)
    {
        if (pageID == null)
        {
            return (null, null);
        }

        foreach (var allWiki in AllWikis)
        {
            var wikiPage = allWiki.FindPageFromID(pageID);
            if (wikiPage != null)
            {
                return (allWiki, wikiPage);
            }
        }

        return (null, null);
    }

    public static void ShowPage(ModWiki wiki, WikiPage page)
    {
        if (wiki == null || page == null)
        {
            return;
        }

        if (WikiWindow.CurrentActive != null && WikiWindow.CurrentActive.Wiki == wiki)
        {
            WikiWindow.CurrentActive.CurrentPage = page;
        }
        else
        {
            WikiWindow.Open(wiki, page);
        }
    }

    public void Show()
    {
        WikiWindow.Open(this);
    }

    private void GenerateFromMod(Mod mod)
    {
        var excludedDefs = GetExcludedDefs(mod);
        foreach (var allDef in mod.Content.AllDefs)
        {
            if (!(allDef is ThingDef thingDef))
            {
                continue;
            }

            if (excludedDefs.Contains(allDef.defName))
            {
                excludedDefs.Remove(allDef.defName);
            }
            else if (AutogenPageFilter(thingDef))
            {
                WikiPage item = null;
                try
                {
                    item = WikiPage.CreateFromThingDef(this, thingDef);
                }
                catch (Exception arg)
                {
                    Log.Error(string.Format("Failed to generate wiki page for {0}'s ThingDef '{1}': {2}",
                        mod.Content?.Name ?? "<no-name-mod>", thingDef.LabelCap, arg));
                }

                Pages.Add(item);
            }
        }

        var dir = Path.Combine(mod.Content?.RootDir, "Wiki");
        PageParser.AddAllFromDirectory(this, dir);
        if (excludedDefs.Count == 0)
        {
            return;
        }

        Log.Error((mod.Content?.Name ?? "<no-name-mod>") +
                  "'s Exclude.txt file includes names of defs that do not exist:");
        foreach (var item2 in excludedDefs)
        {
            Log.Error("  -" + item2);
        }
    }

    private List<string> GetExcludedDefs(Mod mod)
    {
        var path = Path.Combine(mod.Content.RootDir, "Wiki", "Exclude.txt");
        if (!File.Exists(path))
        {
            return new List<string>();
        }

        var list = new List<string>();
        var array = File.ReadAllLines(path);
        foreach (var defs in array)
        {
            var text = defs.Trim();
            if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("//"))
            {
                list.Add(text);
            }
        }

        return list;
    }

    public virtual bool AutogenPageFilter(ThingDef def)
    {
        if (def == null)
        {
            return false;
        }

        if (def.IsBlueprint)
        {
            return false;
        }

        if (def.projectile != null)
        {
            return false;
        }

        if (def.entityDefToBuild != null)
        {
            return false;
        }

        var weaponTags = def.weaponTags;
        if (weaponTags != null && weaponTags.Contains("TurretGun"))
        {
            return false;
        }

        if (def.mote != null)
        {
            return false;
        }

        var category = def.category;
        if (category == ThingCategory.Ethereal || category == ThingCategory.Filth)
        {
            return false;
        }

        return true;
    }

    public WikiPage FindPageFromDef(string defName)
    {
        if (defName == null)
        {
            return null;
        }

        foreach (var page in Pages)
        {
            if (page != null && page.Def?.defName == defName)
            {
                return page;
            }
        }

        return null;
    }

    public WikiPage FindPageFromDef(Def def)
    {
        if (def == null)
        {
            return null;
        }

        foreach (var page in Pages)
        {
            if (page != null && page.Def == def)
            {
                return page;
            }
        }

        return null;
    }

    public WikiPage FindPageFromID(string pageID)
    {
        if (pageID == null)
        {
            return null;
        }

        foreach (var page in Pages)
        {
            if (page != null && page.ID == pageID)
            {
                return page;
            }
        }

        return null;
    }
}