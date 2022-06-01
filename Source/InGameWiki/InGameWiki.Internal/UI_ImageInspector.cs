using UnityEngine;
using Verse;

namespace InGameWiki.Internal;

public class UI_ImageInspector : Window
{
    public Texture2D Image;

    public UI_ImageInspector(Texture2D image)
    {
        Image = image;
        doCloseX = true;
        resizeable = true;
        draggable = true;
        closeOnClickedOutside = false;
    }

    public override Vector2 InitialSize
    {
        get
        {
            if (Image == null)
            {
                return new Vector2(256f, 256f);
            }

            var num = Mathf.Max(Image.width, Image.height) <= 128f ? 2f : 1f;
            return new Vector2(Image.width * num, Image.height * num);
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Image == null)
        {
            Close();
        }
        else
        {
            Widgets.DrawTextureFitted(inRect, Image, 1f);
        }
    }
}