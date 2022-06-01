using UnityEngine;
using Verse;

namespace InGameWiki;

public class WikiElement
{
    public bool AutoFitImage;

    public Def DefForIconAndLabel;

    public GameFont FontSize = GameFont.Small;

    public Texture2D Image;

    public float ImageScale = 1f;

    public Vector2 ImageSize = new Vector2(-1f, -1f);

    public string PageLink;

    public (ModWiki wiki, WikiPage page) PageLinkReal;
    public string Text;

    public bool IsLinkBroken { get; private set; }

    public bool HasText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return PageLink != null;
            }

            return true;
        }
    }

    public bool HasImage => Image != null;

    public static WikiElement Create(string text)
    {
        return Create(text, null);
    }

    public static WikiElement Create(Texture2D image, Vector2? imageSize = null)
    {
        return Create(null, image, imageSize);
    }

    public static WikiElement Create(string text, Texture2D image, Vector2? imageSize = null)
    {
        return new WikiElement
        {
            Text = text,
            Image = image,
            ImageSize = imageSize ?? new Vector2(-1f, -1f)
        };
    }

    public static WikiElement Create(Def def)
    {
        return new WikiElement
        {
            DefForIconAndLabel = def
        };
    }

    public virtual Vector2 Draw(Rect maxBounds)
    {
        var zero = Vector2.zero;
        var font = Verse.Text.Font;
        Verse.Text.Font = FontSize;
        var zero2 = Vector2.zero;
        if (HasImage)
        {
            if (!AutoFitImage)
            {
                var num = ImageSize.x < 1f ? Image.width * ImageScale : ImageSize.x;
                var num2 = ImageSize.y < 1f ? Image.height * ImageScale : ImageSize.y;
                Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, num, num2), Image, 1f);
                zero += new Vector2(num, num2);
                zero2.x = num;
            }
            else if (Image.width <= maxBounds.width)
            {
                float num3 = Image.width;
                float num4 = Image.height;
                Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, num3, num4), Image, 1f);
                zero += new Vector2(num3, num4);
                zero2.x = num3;
            }
            else
            {
                var width = maxBounds.width;
                var num5 = Image.height * (width / Image.width);
                Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, width, num5), Image, 1f);
                zero += new Vector2(width, num5);
                zero2.x = width;
            }
        }

        if (DefForIconAndLabel != null)
        {
            var rect = new Rect(maxBounds.x, maxBounds.y, 200f, 32f);
            var isSpoiler = false;
            (ModWiki, WikiPage) tuple = ModWiki.GlobalFindPageFromDef(DefForIconAndLabel.defName);
            if (tuple.Item2 != null)
            {
                isSpoiler = tuple.Item2.IsSpoiler;
            }

            var key = Input.GetKey(KeyCode.LeftControl);
            if (!isSpoiler || key)
            {
                Widgets.DefLabelWithIcon(rect, DefForIconAndLabel);
            }
            else
            {
                Widgets.DrawBoxSolid(rect, new Color(1f, 0.34901962f, 8f / 15f, 0.4f));
                string text = "<i>[" + "Wiki.Spoiler".Translate().CapitalizeFirst() + "]</i>";
                var vector = Verse.Text.CalcSize(text);
                Widgets.Label(
                    new Rect(rect.x + ((rect.width - vector.x) * 0.5f), rect.y + ((rect.height - vector.y) * 0.5f),
                        vector.x, vector.y), text);
            }

            if (Widgets.ButtonInvisible(rect) && (!isSpoiler || key))
            {
                if (tuple.Item2 != null)
                {
                    ModWiki.ShowPage(tuple.Item1, tuple.Item2);
                }
                else
                {
                    ModWiki.OpenInspectWindow(DefForIconAndLabel);
                }
            }

            zero.y += 32f;
            zero2.x = 200f;
            zero2.y = 6f;
        }

        if (HasText)
        {
            var num6 = PageLink != null;
            var num7 = maxBounds.x + zero2.x;
            var num8 = maxBounds.xMax - num7;
            var curY = maxBounds.y + zero2.y;
            var num9 = curY;
            if (num6 && PageLinkReal.page == null && !IsLinkBroken)
            {
                (ModWiki, WikiPage) pageLinkReal = ModWiki.GlobalFindPageFromID(PageLink);
                if (pageLinkReal.Item2 == null)
                {
                    IsLinkBroken = true;
                }
                else
                {
                    PageLinkReal = pageLinkReal;
                }
            }

            var text2 = IsLinkBroken
                ? string.Format("<color=#ff2b2b><b><i>{0}: [{1}]</i></b></color>", "Wiki.LinkBroken".Translate(),
                    PageLink)
                : string.Format("<color=#9c9c9c><b><i>{0}:</i></b></color>{1}", "Wiki.Link".Translate(),
                    PageLinkReal.page?.Title);
            var label = num6 ? text2 : Text;
            Widgets.LongLabel(num7, num8, label, ref curY);
            var num10 = curY - num9;
            if (num6)
            {
                var rect2 = new Rect(num7, num9, num8, num10);
                Widgets.DrawHighlightIfMouseover(rect2);
                if (!IsLinkBroken && Widgets.ButtonInvisible(rect2))
                {
                    ModWiki.ShowPage(PageLinkReal.wiki, PageLinkReal.page);
                }
            }

            zero += new Vector2(num8, 0f);
            if (zero.y < num10)
            {
                zero.y = num10;
            }
        }

        Verse.Text.Font = font;
        return zero;
    }
}