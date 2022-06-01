using UnityEngine;
using Verse;

namespace InGameWiki.Internal;

internal static class InspectorPatch
{
    internal static void InGameWikiPostfix(Rect inRect, Dialog_InfoCard __instance, Def ___def, Thing ___thing)
    {
        if (___def == null && ___thing == null)
        {
            return;
        }

        var def = ___def ?? ___thing.def;
        if (def == null)
        {
            return;
        }

        var (wiki, wikiPage) = ModWiki.GlobalFindPageFromDef(def.defName);
        if (wikiPage == null ||
            !Widgets.ButtonText(new Rect(inRect.x + (inRect.width * 0.5f) + 6f, inRect.y + 24f, 180f, 36f),
                "Wiki.OpenWikiPage".Translate()))
        {
            return;
        }

        __instance.Close();
        ModWiki.ShowPage(wiki, wikiPage);
    }
}