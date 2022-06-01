using System;
using System.Collections.Generic;
using InGameWiki.Internal;
using UnityEngine;
using Verse;

namespace InGameWiki;

public class WikiPage
{
    public static bool DebugMode;

    public readonly ModWiki Wiki;

    public Texture2D Background;

    public Def Def;

    private Vector2 descScroll;

    public List<WikiElement> Elements = new List<WikiElement>();

    public Texture2D Icon;

    public bool IsAlwaysSpoiler;

    private float lastHeight;

    private ResearchProjectDef requiresResearch;

    public string RequiresResearchRaw;

    private Vector2 scroll;

    public string ShortDescription;

    public string Title;

    public WikiPage(ModWiki wiki)
    {
        Wiki = wiki;
    }

    public virtual bool IsSpoiler
    {
        get
        {
            if (!Wiki.NoSpoilerMode)
            {
                return false;
            }

            if (IsAlwaysSpoiler)
            {
                return true;
            }

            if (RequiresResearchRaw != null)
            {
                if (requiresResearch == null)
                {
                    requiresResearch = ResearchProjectDef.Named(RequiresResearchRaw);
                    if (requiresResearch == null)
                    {
                        Log.Error("Failed to find required research for page " + ID + " (" + Title + "): '" +
                                  RequiresResearchRaw + "'");
                        RequiresResearchRaw = null;
                    }
                }

                if (requiresResearch is { IsFinished: false })
                {
                    return true;
                }
            }

            if (!IsResearchFinished)
            {
                return true;
            }

            return false;
        }
    }

    public virtual bool IsResearchFinished
    {
        get
        {
            if (Def == null)
            {
                return true;
            }

            if (!(Def is ThingDef thingDef))
            {
                return true;
            }

            if (thingDef.researchPrerequisites != null)
            {
                return thingDef.IsResearchFinished;
            }

            if (thingDef.recipeMaker == null)
            {
                return true;
            }

            var prerequisiteIsFinished = true;
            if (thingDef.recipeMaker.researchPrerequisite != null)
            {
                prerequisiteIsFinished = thingDef.recipeMaker.researchPrerequisite.IsFinished;
            }

            if (!prerequisiteIsFinished)
            {
                return false;
            }

            if (thingDef.recipeMaker.researchPrerequisites == null)
            {
                return true;
            }

            foreach (var researchPrerequisite in thingDef.recipeMaker.researchPrerequisites)
            {
                if (!researchPrerequisite.IsFinished)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public virtual Color IconColor
    {
        get
        {
            if (!(Def is ThingDef thingDef))
            {
                return Color.white;
            }

            return thingDef.graphicData?.color ?? Color.white;
        }
    }

    public string ID { get; internal set; }

    public static WikiPage CreateFromThingDef(ModWiki wiki, ThingDef thing)
    {
        if (thing == null)
        {
            return null;
        }

        var wikiPage = new WikiPage(wiki);
        try
        {
            wikiPage.Title = thing.LabelCap;
            wikiPage.ShortDescription = thing.DescriptionDetailed;
            wikiPage.Icon = thing.uiIcon;
        }
        catch (Exception innerException)
        {
            throw new Exception("Exception setting page basics.", innerException);
        }

        try
        {
            if (thing.costList != null)
            {
                var sectionWikiElement = new SectionWikiElement
                {
                    Name = "Wiki.Cost".Translate()
                };
                foreach (var cost in thing.costList)
                {
                    sectionWikiElement.Elements.Add(new WikiElement
                    {
                        DefForIconAndLabel = cost.thingDef,
                        Text = cost.count <= 1 ? "" : $"x{cost.count}"
                    });
                }

                var num = thing.recipeMaker?.productCount ?? 1;
                sectionWikiElement.Elements.Add(WikiElement.Create("Wiki.OutputCount".Translate(num)));
                if (sectionWikiElement.Elements.Count > 0)
                {
                    wikiPage.Elements.Add(sectionWikiElement);
                }

                var sectionWikiElement2 = new SectionWikiElement
                {
                    Name = "Wiki.Creates".Translate()
                };
                foreach (var allRecipe in thing.AllRecipes)
                {
                    sectionWikiElement2.Elements.Add(WikiElement.Create($" • {allRecipe.LabelCap}"));
                }

                if (sectionWikiElement2.Elements.Count > 0)
                {
                    wikiPage.Elements.Add(sectionWikiElement2);
                }
            }
        }
        catch (Exception innerException2)
        {
            throw new Exception("Exception generating thing cost list.", innerException2);
        }

        try
        {
            if (thing.recipeMaker?.recipeUsers != null)
            {
                var sectionWikiElement3 = new SectionWikiElement
                {
                    Name = "Wiki.CraftedAt".Translate()
                };
                foreach (var recipeUser in thing.recipeMaker.recipeUsers)
                {
                    sectionWikiElement3.Elements.Add(new WikiElement
                    {
                        DefForIconAndLabel = recipeUser
                    });
                }

                if (sectionWikiElement3.Elements.Count > 0)
                {
                    wikiPage.Elements.Add(sectionWikiElement3);
                }
            }
        }
        catch (Exception innerException3)
        {
            throw new Exception("Exception generating thing crafting location list.", innerException3);
        }

        try
        {
            var sectionWikiElement4 = new SectionWikiElement
            {
                Name = "Wiki.ResearchToUnlock".Translate()
            };
            if (thing.researchPrerequisites is { Count: > 0 })
            {
                foreach (var researchPrerequisite2 in thing.researchPrerequisites)
                {
                    sectionWikiElement4.Elements.Add(new WikiElement
                    {
                        Text = $" • {researchPrerequisite2.LabelCap}"
                    });
                }
            }

            if (thing.recipeMaker?.researchPrerequisites != null)
            {
                foreach (var researchPrerequisite3 in thing.recipeMaker.researchPrerequisites)
                {
                    sectionWikiElement4.Elements.Add(new WikiElement
                    {
                        Text = $" • {researchPrerequisite3.LabelCap}"
                    });
                }
            }

            if (thing.recipeMaker?.researchPrerequisite != null)
            {
                var researchPrerequisite = thing.recipeMaker.researchPrerequisite;
                sectionWikiElement4.Elements.Add(new WikiElement
                {
                    Text = $" • {researchPrerequisite.LabelCap}"
                });
            }

            if (sectionWikiElement4.Elements.Count > 0)
            {
                wikiPage.Elements.Add(sectionWikiElement4);
            }

            if (DebugMode && thing.weaponTags != null)
            {
                foreach (var weaponTag in thing.weaponTags)
                {
                    wikiPage.Elements.Add(new WikiElement
                    {
                        Text = "WeaponTag: " + weaponTag
                    });
                }
            }
        }
        catch (Exception innerException4)
        {
            throw new Exception("Exception generating thing research requirements.", innerException4);
        }

        wikiPage.Def = thing;
        return wikiPage;
    }

    public virtual void Draw(Rect maxBounds)
    {
        var num = 133f;
        if (Background != null)
        {
            GUI.color = Color.white * 0.45f;
            var texCoords = CalculateUVCoords(maxBounds, new Rect(0f, 0f, Background.width, Background.height));
            GUI.DrawTextureWithTexCoords(maxBounds, Background, texCoords, true);
            GUI.color = Color.white;
        }

        if (Icon != null)
        {
            if (Widgets.ButtonImageFitted(new Rect(maxBounds.x + 5f, maxBounds.y + 5f, 128f, 128f), Icon, IconColor,
                    IconColor * 0.8f))
            {
                Find.WindowStack?.Add(new UI_ImageInspector(Icon));
            }

            GUI.color = Color.white;
        }

        if (Title != null)
        {
            Text.Font = GameFont.Medium;
            var x = !(Icon != null) ? maxBounds.x + 5f : maxBounds.x + 5f + 128f + 5f;
            var width = !(Icon != null) ? maxBounds.width - 10f : maxBounds.width - 10f - 128f;
            var text = Title;
            if (IsSpoiler)
            {
                text += $" <color=#FF6D71><i>[{"Wiki.Spoiler".Translate().CapitalizeFirst()}]</i></color>";
            }

            Widgets.Label(new Rect(x, maxBounds.y + 5f, width, 34f), text);
        }

        if (ShortDescription != null)
        {
            Text.Font = GameFont.Small;
            var x2 = !(Icon != null) ? maxBounds.x + 5f : maxBounds.x + 5f + 128f + 5f;
            var num2 = Title == null ? maxBounds.y + 5f : maxBounds.y + 10f + 34f;
            var width2 = !(Icon != null) ? maxBounds.width - 10f : maxBounds.width - 15f - 128f;
            var height = maxBounds.y + 5f + 128f - num2;
            Widgets.LabelScrollable(new Rect(x2, num2, width2, height), ShortDescription, ref descScroll, false, true,
                true);
        }

        if (Def != null)
        {
            var x3 = maxBounds.xMax - 24f - 5f;
            var y = maxBounds.y + 5f;
            Widgets.InfoCardButton(x3, y, Def);
        }

        Text.Font = GameFont.Small;
        maxBounds.y += num;
        maxBounds.height -= num;
        Widgets.DrawLineHorizontal(maxBounds.x, maxBounds.y, maxBounds.width);
        var inner = maxBounds.GetInner();
        Widgets.BeginScrollView(inner, ref scroll,
            new Rect(maxBounds.x + 5f, maxBounds.y + 5f, maxBounds.width - 25f - 10f, lastHeight));
        lastHeight = 0f;
        var maxBounds2 = inner;
        maxBounds2.width -= 18f;
        foreach (var element in Elements)
        {
            if (element == null)
            {
                continue;
            }

            try
            {
                var vector = element.Draw(maxBounds2);
                maxBounds2.y += vector.y + 10f;
                lastHeight += vector.y + 10f;
            }
            catch (Exception arg)
            {
                Log.Error($"In-game wiki exception when drawing element: {arg}");
            }
        }

        Widgets.EndScrollView();
    }

    private Rect CalculateUVCoords(Rect boundsToFill, Rect imageSize)
    {
        var rect = default(Rect);
        rect.size = imageSize.size;
        rect.center = boundsToFill.center;
        var vector = boundsToFill.min - rect.min;
        var vector2 = boundsToFill.max - rect.min;
        var vector3 = new Vector2(vector.x / imageSize.width, vector.y / imageSize.height);
        var vector4 = new Vector2(vector2.x / imageSize.width, vector2.y / imageSize.height);
        return new Rect(vector3, vector4 - vector3);
    }
}