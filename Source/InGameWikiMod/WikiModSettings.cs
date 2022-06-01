using UnityEngine;
using Verse;

namespace InGameWikiMod;

public class WikiModSettings : ModSettings
{
    public static bool TabButtonEnabled = true;

    public static bool InspectorButtonEnabled = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref TabButtonEnabled, "TabButtonEnabled", true);
        Scribe_Values.Look(ref InspectorButtonEnabled, "InspectorButtonEnabled", true);
    }

    public void Apply()
    {
        WikiDefOf.WikiButton.buttonVisible = TabButtonEnabled;
    }

    public void Draw(Rect rect)
    {
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(rect);
        var tabButtonEnabled = TabButtonEnabled;
        listing_Standard.CheckboxLabeled("Wiki.ShowMenuBarButton".Translate(), ref TabButtonEnabled,
            "Wiki.ShowMenuBarButtonDesc".Translate());
        if (tabButtonEnabled != TabButtonEnabled)
        {
            Apply();
        }

        listing_Standard.CheckboxLabeled("Wiki.ShowInspectorButton".Translate(), ref InspectorButtonEnabled,
            "Wiki.ShowInspectorButtonDesc".Translate());
        listing_Standard.End();
    }
}