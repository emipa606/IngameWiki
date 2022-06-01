using System;
using InGameWiki;
using RimWorld;
using UnityEngine;
using Verse;

namespace InGameWikiMod;

[StaticConstructorOnStartup]
public class MainButtonUI : MainTabWindow
{
    public override void DoWindowContents(Rect inRect)
    {
    }

    public override void PreOpen()
    {
        base.PreOpen();
        Close(false);
        WikiButtonClicked();
    }

    public override void PostOpen()
    {
        base.PostOpen();
        Close(false);
    }

    public void WikiButtonClicked()
    {
        var allWikis = ModWiki.AllWikis;
        if (allWikis.Count == 0)
        {
            Log.Warning("There are no wikis loaded.");
            return;
        }

        if (allWikis.Count == 1)
        {
            allWikis[0].Show();
            return;
        }

        string LabelGetter(ModWiki w)
        {
            return w.Mod.Content.Name;
        }

        Action ActionGetter(ModWiki w)
        {
            return w.Show;
        }

        FloatMenuUtility.MakeMenu(allWikis, LabelGetter, ActionGetter);
    }
}