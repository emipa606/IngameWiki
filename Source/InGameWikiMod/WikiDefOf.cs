using RimWorld;

namespace InGameWikiMod;

[DefOf]
public static class WikiDefOf
{
    public static MainButtonDef WikiButton;

    static WikiDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(WikiDefOf));
    }
}