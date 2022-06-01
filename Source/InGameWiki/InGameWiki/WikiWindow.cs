using UnityEngine;
using Verse;

namespace InGameWiki;

public class WikiWindow : Window
{
    private float lastHeight;

    private Vector2 scroll;

    public int SearchHeight = 34;

    public string SearchText = "";

    public int SideWidth = 330;

    public int TopHeight = 38;
    public ModWiki Wiki;

    protected WikiWindow(ModWiki wiki)
    {
        Wiki = wiki;
        resizeable = true;
        doCloseButton = true;
        draggable = true;
        drawShadow = true;
        onlyOneOfTypeAllowed = true;
        absorbInputAroundWindow = false;
        preventCameraMotion = false;
    }

    public static WikiWindow CurrentActive { get; private set; }

    public WikiPage CurrentPage { get; set; }

    public override Vector2 InitialSize => new Vector2(1100f, 800f);

    public static WikiWindow Open(ModWiki wiki, WikiPage page = null)
    {
        if (wiki == null)
        {
            return null;
        }

        if (CurrentActive != null && CurrentActive.Wiki != wiki)
        {
            CurrentActive.Close();
        }

        var wikiWindow = new WikiWindow(wiki)
        {
            CurrentPage = page
        };
        CurrentActive = wikiWindow;
        Find.WindowStack?.Add(wikiWindow);
        return wikiWindow;
    }

    public override void DoWindowContents(Rect maxBounds)
    {
        var rect = new Rect(maxBounds.x, maxBounds.y, maxBounds.width, maxBounds.height - 50f);
        var rect2 = new Rect(rect.x, rect.y, rect.width, TopHeight);
        var rect3 = new Rect(rect.x, rect.y + TopHeight + 5f, SideWidth, SearchHeight);
        var rect4 = new Rect(rect.x, rect.y + TopHeight + 10f + SearchHeight, SideWidth,
            rect.height - 10f - TopHeight - SearchHeight);
        var rect5 = new Rect(rect.x + SideWidth + 5f, rect.y + TopHeight + 5f, rect.width - SideWidth - 5f,
            rect.height - TopHeight - 5f);
        Widgets.DrawBoxSolid(rect4, Color.white * 0.4f);
        Widgets.DrawBox(rect4);
        Widgets.DrawBox(rect2);
        Widgets.DrawBox(rect5);
        Text.Font = GameFont.Medium;
        var vector = Text.CalcSize(Wiki.WikiTitle);
        Widgets.Label(
            new Rect(rect2.x + ((rect2.width - vector.x) * 0.5f), rect2.y + ((rect2.height - vector.y) * 0.5f),
                vector.x, vector.y), Wiki.WikiTitle);
        SearchText = Widgets.TextField(rect3, SearchText);
        Widgets.BeginScrollView(rect4, ref scroll, new Rect(rect4.x, rect4.y, rect4.width - 32f, lastHeight));
        lastHeight = 0f;
        var value = SearchText?.Trim().ToLowerInvariant();
        var num = 0;
        foreach (var page in Wiki.Pages)
        {
            if (page == null || !string.IsNullOrEmpty(value) && !page.Title.Trim().ToLowerInvariant().Contains(value))
            {
                continue;
            }

            if (page.IsSpoiler)
            {
                num++;
                continue;
            }

            if (page.Icon != null)
            {
                GUI.color = page.IconColor;
                Widgets.DrawTextureFitted(new Rect(rect4.x + 4f, rect4.y + 4f + lastHeight + 5f, 24f, 24f), page.Icon,
                    1f);
                GUI.color = Color.white;
            }

            if (Widgets.ButtonText(new Rect(rect4.x + 28f, rect4.y + 4f + lastHeight, rect4.width - 32f, 40f),
                    page.Title))
            {
                CurrentPage = page;
            }

            lastHeight += 37f;
        }

        if (num > 0)
        {
            var label = $"<color=#FF6D71><i>{"Wiki.HiddenWarning".Translate(num)}</i></color>";
            Widgets.Label(new Rect(rect4.x + 4f, rect4.y + 4f + lastHeight, rect4.width - 4f, 50f), label);
            lastHeight += 47f;
        }

        Widgets.EndScrollView();
        CurrentPage?.Draw(rect5);
        var checkOn = Wiki.NoSpoilerMode;
        string text = "Wiki.HideSpoilerMode".Translate();
        var width = Text.CalcSize(text).x + 24f;
        Widgets.CheckboxLabeled(new Rect(maxBounds.x + 5f, maxBounds.yMax - 32f, width, 32f), text, ref checkOn);
        Wiki.NoSpoilerMode = checkOn;
    }

    public override void PreClose()
    {
        CurrentActive = null;
        base.PreClose();
    }

    public bool GoToPage(Def def, bool openInspectWindow = false)
    {
        if (def == null)
        {
            return false;
        }

        var wikiPage = Wiki.FindPageFromDef(def.defName);
        if (wikiPage == null)
        {
            if (!openInspectWindow)
            {
                return false;
            }

            ModWiki.OpenInspectWindow(def);
            return true;
        }

        CurrentPage = wikiPage;
        return true;
    }
}