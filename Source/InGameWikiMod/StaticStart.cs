using Verse;

namespace InGameWikiMod;

[StaticConstructorOnStartup]
public static class StaticStart
{
    static StaticStart()
    {
        ModCore.Instance.GetSettings<WikiModSettings>().Apply();
    }
}