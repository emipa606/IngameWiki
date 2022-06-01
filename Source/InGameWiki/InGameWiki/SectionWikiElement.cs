using UnityEngine;
using Verse;

namespace InGameWiki;

public class SectionWikiElement : CompoundWikiElement
{
    public bool Hidden = true;

    private float lastHeight;
    public string Name = "Section Name";

    public override Vector2 Draw(Rect maxBounds)
    {
        var rect = new Rect(maxBounds.x, maxBounds.y + 40f, maxBounds.width, lastHeight);
        lastHeight = 0f;
        var zero = Vector2.zero;
        Verse.Text.Font = GameFont.Medium;
        string text = Hidden ? "Wiki.Show".Translate().CapitalizeFirst() : "Wiki.Hide".Translate().CapitalizeFirst();
        var num = Verse.Text.CalcSize(text).x + 16f;
        if (Widgets.ButtonText(new Rect(maxBounds.x, maxBounds.y, num, 32f), text))
        {
            Hidden = !Hidden;
        }

        if (Name != null)
        {
            Widgets.Label(new Rect(maxBounds.x + num + 5f, maxBounds.y, maxBounds.width - num - 5f, 40f), Name);
        }

        Verse.Text.Font = GameFont.Small;
        zero.y = 40f;
        var maxBounds2 = new Rect(rect.x, rect.y, rect.width, 69420f);
        if (!Hidden)
        {
            foreach (var element in Elements)
            {
                var vector = element.Draw(maxBounds2);
                zero.y += vector.y + 10f;
                maxBounds2.y += vector.y + 10f;
                lastHeight += vector.y + 10f;
            }
        }

        zero.x = rect.width;
        return zero;
    }
}