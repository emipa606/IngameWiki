using HarmonyLib;
using InGameWiki;
using UnityEngine;
using Verse;

namespace InGameWikiMod;

public class ModCore : Mod
{
    public static ModCore Instance;

    public ModCore(ModContentPack content)
        : base(content)
    {
        Instance = this;
        GetSettings<WikiModSettings>();
        ModWiki.Patch(new Harmony("co.uk.epicguru.ingamewiki"), WikiModSettings.InspectorButtonEnabled);
        try
        {
            Log.Message("<color=cyan>Finished loading in-game wiki mod: Version " + ModWiki.APIVersion + "</color>");
        }
        catch
        {
            // ignored
        }
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        GetSettings<WikiModSettings>().Draw(inRect);
    }

    public override string SettingsCategory()
    {
        return "In-Game Wiki";
    }
}