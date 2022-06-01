using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace InGameWiki;

public static class PageParser
{
    public enum CurrentlyParsing
    {
        None,
        Text,
        Image,
        ThingDefLink,
        PageLink,
        Custom
    }

    private static readonly Dictionary<string, string> pageTags = new Dictionary<string, string>();

    private static readonly Dictionary<Type, (MethodInfo, bool)> classToParser =
        new Dictionary<Type, (MethodInfo, bool)>();

    public static string AddAllFromDirectory(ModWiki wiki, string dir)
    {
        if (wiki == null)
        {
            return null;
        }

        if (!Directory.Exists(dir))
        {
            return null;
        }

        var folderName = LanguageDatabase.activeLanguage.folderName;
        var defaultLangFolderName = LanguageDatabase.DefaultLangFolderName;
        if (!Directory.Exists(Path.Combine(dir, folderName)))
        {
            var text = wiki.Mod?.Content?.Name ?? "UKN";
            Log.Warning("Mod " + text + " has a wiki folder, but does not support language '" + folderName + "'. " +
                        (folderName == defaultLangFolderName
                            ? "Falling back to first found."
                            : "Falling back to '" + defaultLangFolderName + "', or first found.") + " ");
            if (Directory.Exists(Path.Combine(dir, defaultLangFolderName)))
            {
                dir = Path.Combine(dir, defaultLangFolderName);
                Log.Warning("Using " + defaultLangFolderName + ".");
            }
            else
            {
                var directories = Directory.GetDirectories(dir, "", SearchOption.TopDirectoryOnly);
                if (directories.Length == 0)
                {
                    Log.Warning("Mod " + text +
                                " has wiki folder, but no languages. The folder structure should be 'ModName/Wiki/LanguageName/'.");
                }
                else
                {
                    dir = directories[0];
                    Log.Warning("Failed to find wiki in '" + defaultLangFolderName + "', using first found: '" +
                                new DirectoryInfo(dir).Name + "'.");
                }
            }
        }
        else
        {
            dir = Path.Combine(dir, folderName);
        }

        var list = Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories).ToList();
        list.Sort(delegate(string a, string b)
        {
            var name2 = new FileInfo(a).Name;
            var name3 = new FileInfo(b).Name;
            return -string.Compare(name2, name3, StringComparison.InvariantCultureIgnoreCase);
        });
        foreach (var item in list)
        {
            var fileInfo = new FileInfo(item);
            var name = fileInfo.Name;
            name = name.Substring(0, name.Length - fileInfo.Extension.Length);
            if (name.StartsWith("All_"))
            {
                var text2 = name.Substring("All_".Length);
                var num = text2.LastIndexOf('.');
                if (num < 0)
                {
                    Log.Error("Wiki parse error: The All_ method path '" + text2 +
                              "' is invalid. Expected format: 'Namespace.Class.MethodName'");
                    continue;
                }

                var methodInfo = AccessTools.Method(text2.Substring(0, num) + ":" + text2.Substring(num + 1));
                if (methodInfo == null)
                {
                    Log.Error("Wiki parse error: The All_ method '" + text2 +
                              "' does not correspond to any found class/method. Check spelling!");
                    continue;
                }

                if (!methodInfo.IsStatic || methodInfo.IsGenericMethod || methodInfo.ReturnType != typeof(bool) ||
                    methodInfo.ContainsGenericParameters || methodInfo.GetParameters().Length != 1 ||
                    methodInfo.GetParameters()[0].ParameterType != typeof(ThingDef))
                {
                    Log.Error("Wiki parse error: The All_ method '" + text2 +
                              "' was found but is invalid: it must be a static method that returns a boolean and takes a single parameter of type ThingDef");
                    continue;
                }

                if (wiki?.Mod?.Content?.AllDefs == null)
                {
                    Log.Error(
                        "Unexpected wiki error when parsing All_ page: wiki does not belong to any mod, so cannot scan defs.");
                    continue;
                }

                var rawText = File.ReadAllText(item);
                foreach (var allDef in wiki.Mod.Content.AllDefs)
                {
                    if (!(allDef is ThingDef thingDef))
                    {
                        continue;
                    }

                    bool invokeResult;
                    try
                    {
                        invokeResult = (bool)methodInfo.Invoke(null, new object[] { thingDef });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        continue;
                    }

                    if (!invokeResult)
                    {
                        continue;
                    }

                    var wikiPage = wiki.FindPageFromDef(thingDef);
                    if (wikiPage != null)
                    {
                        Parse(wiki, rawText, wikiPage, name);
                    }
                }
            }
            else if (name.StartsWith("Thing_"))
            {
                var text3 = name.Substring(6);
                var wikiPage2 = wiki.FindPageFromDef(text3);
                if (wikiPage2 != null)
                {
                    Parse(wiki, File.ReadAllText(item), wikiPage2, name);
                }
                else
                {
                    Log.Error("Failed to find Thing wiki entry for wiki page: Thing_" + text3);
                }
            }
            else
            {
                var wikiPage3 = Parse(wiki, File.ReadAllText(item), null, name);
                if (wikiPage3 == null)
                {
                    Log.Error("Failed to load wiki page from " + item);
                }
                else
                {
                    wiki.Pages.Insert(0, wikiPage3);
                }
            }
        }

        return new DirectoryInfo(dir).Name;
    }

    private static string TryGetTag(string tag, string ifNotFound = null)
    {
        if (!pageTags.TryGetValue(tag, out var value))
        {
            return ifNotFound;
        }

        return value;
    }

    public static WikiPage Parse(ModWiki wiki, string rawText, WikiPage existing, string fileName)
    {
        var array = rawText.Split('\n');
        pageTags.Clear();
        var num = -1;
        for (var j = 0; j < array.Length; j++)
        {
            if (array[j].Trim() != "ENDTAGS")
            {
                continue;
            }

            num = j;
            break;
        }

        if (num == -1 && existing == null)
        {
            Log.Error("External wiki page '" + fileName +
                      "' does not have an ENDTAGS line.\nExternal pages need to have the following format:\n\nTAG:Data\nOTHERTAG:Other Data\nENDTAGS\n... Page data below ...\n\nRaw page:\n" +
                      rawText);
            return null;
        }

        if (num != -1)
        {
            for (var k = 0; k < num; k++)
            {
                var text = array[k];
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!text.Contains(':'))
                {
                    Log.Error(
                        $"External wiki page '{fileName}' tag error, line {k + 1}: incorrect format. Expected 'TAG:Data', got '{text.Trim()}'.\nRaw file:\n{rawText}");
                    continue;
                }

                var array2 = text.Split(':');
                var text2 = array2[0];
                if (string.IsNullOrWhiteSpace(text2))
                {
                    Log.Error(
                        $"External wiki page '{fileName}' tag error, line {k + 1}: blank tag.\nRaw file:\n{rawText}");
                }
                else if (pageTags.ContainsKey(text2))
                {
                    Log.Error(
                        $"External wiki page '{fileName}' tag error, line {k + 1}: duplicate tag '{text2}'.\nRaw file:\n{rawText}");
                }
                else
                {
                    pageTags.Add(text2, array2[1].Trim());
                }
            }
        }

        var p = existing ?? new WikiPage(wiki);
        if (existing == null)
        {
            var text3 = TryGetTag("ID", "INVALID_ID_ERROR");
            var text4 = TryGetTag("Title", "<No title specified>");
            var icon = ContentFinder<Texture2D>.Get(TryGetTag("Icon", ""), false);
            var background = ContentFinder<Texture2D>.Get(TryGetTag("Background", ""), false);
            var text5 = TryGetTag("RequiredResearch", "");
            var shortDescription = TryGetTag("Description", "");
            if (text3 == "INVALID_ID_ERROR")
            {
                Log.Warning("External wiki page '" + fileName + "' with title " + text4 +
                            " does not have an ID tag. It should specify 'ID: MyPageID'. It may break things.");
            }

            p.ID = text3;
            p.Title = text4;
            p.Icon = icon;
            p.ShortDescription = shortDescription;
            p.Background = background;
            if (text5 != string.Empty)
            {
                p.RequiresResearchRaw = text5;
            }

            if (TryGetTag("AlwaysSpoiler", "false") != "false")
            {
                p.IsAlwaysSpoiler = true;
            }
        }
        else
        {
            var background2 = ContentFinder<Texture2D>.Get(TryGetTag("Background", ""), false);
            var texture2D = ContentFinder<Texture2D>.Get(TryGetTag("Icon", "WIKI__ICON_NOT_SPECIFIED"), false);
            var text6 = TryGetTag("RequiredResearch", "");
            var text7 = TryGetTag("Description");
            var num2 = TryGetTag("AlwaysSpoiler", "false") != "false";
            p.Background = background2;
            if (text6 != string.Empty)
            {
                p.RequiresResearchRaw = text6;
            }

            if (num2)
            {
                p.IsAlwaysSpoiler = true;
            }

            if (texture2D != null)
            {
                p.Icon = texture2D;
            }

            if (text7 != null)
            {
                p.ShortDescription = text7;
            }
        }

        var str = new StringBuilder();
        var currentParsing2 = CurrentlyParsing.None;
        for (var l = num + 1; l < array.Length; l++)
        {
            var text8 = array[l] + "\n";
            var last2 = '\0';
            foreach (var c2 in text8)
            {
                var shouldAppend2 = 0;
                var text10 = CheckParseChar('|', CurrentlyParsing.Custom, last2, l, c2, ref currentParsing2,
                    ref shouldAppend2);
                if (text10 != null)
                {
                    AddCustom(text10);
                }

                text10 = CheckParseChar('#', CurrentlyParsing.Text, last2, l, c2, ref currentParsing2,
                    ref shouldAppend2);
                if (text10 != null)
                {
                    if (text10.StartsWith("!"))
                    {
                        AddText(text10.Substring(1), true);
                    }
                    else
                    {
                        AddText(text10, false);
                    }

                    continue;
                }

                text10 = CheckParseChar('$', CurrentlyParsing.Image, last2, l, c2, ref currentParsing2,
                    ref shouldAppend2);
                if (text10 != null)
                {
                    Vector2? vector = null;
                    if (text10.Contains(':'))
                    {
                        var array3 = text10.Split(':');
                        text10 = array3[0];
                        if (array3[1].Contains(","))
                        {
                            if (float.TryParse(array3[1].Split(',')[0], out var result) &&
                                float.TryParse(array3[1].Split(',')[1], out var result2))
                            {
                                vector = new Vector2(result, result2);
                            }
                            else
                            {
                                Log.Error("Error in wiki parse: failed to parse Vector2 for image size, '" + array3[1] +
                                          "', failed to parse " +
                                          (float.TryParse(array3[1].Split(',')[0], out _) ? "y" : "x") +
                                          " value as a float.");
                            }
                        }
                        else
                        {
                            Log.Error("Error in wiki parse: failed to parse Vector2 for image size, '" + array3[1] +
                                      "', expected format 'x, y'.");
                        }
                    }

                    var image = ContentFinder<Texture2D>.Get(text10, false);
                    p.Elements.Add(new WikiElement
                    {
                        Image = image,
                        AutoFitImage = !vector.HasValue,
                        ImageSize = vector ?? new Vector2(-1f, -1f)
                    });
                    continue;
                }

                text10 = CheckParseChar('@', CurrentlyParsing.ThingDefLink, last2, l, c2, ref currentParsing2,
                    ref shouldAppend2);
                if (text10 != null)
                {
                    string text11 = null;
                    if (text10.Contains(':'))
                    {
                        var array4 = text10.Split(':');
                        text10 = array4[0];
                        text11 = array4[1];
                    }

                    Def def = ThingDef.Named(text10);
                    if (def != null)
                    {
                        p.Elements.Add(new WikiElement
                        {
                            DefForIconAndLabel = def,
                            Text = text11
                        });
                    }
                    else
                    {
                        AddText("<i>MissingDefLink [" + text10 + "]</i>", false);
                    }

                    continue;
                }

                text10 = CheckParseChar('~', CurrentlyParsing.PageLink, last2, l, c2, ref currentParsing2,
                    ref shouldAppend2);
                if (text10 != null)
                {
                    p.Elements.Add(new WikiElement
                    {
                        PageLink = text10
                    });
                    continue;
                }

                if (shouldAppend2 > 0)
                {
                    str.Append(c2);
                }

                last2 = c2;
            }
        }

        str.Clear();
        return p;

        void AddCustom(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
            {
                Log.Warning("Empty custom tag found when parsing wiki.");
            }
            else
            {
                var num3 = txt.IndexOf(':');
                var text12 = num3 < 0 ? txt : txt.Substring(0, num3);
                var input = num3 < 0 ? null : txt.Substring(num3 + 1);
                var typeInAnyAssembly = GenTypes.GetTypeInAnyAssembly(text12);
                if (typeInAnyAssembly == null)
                {
                    Log.Error("Wiki: Failed to find class '" + text12 + "' for custom element parsing.");
                }
                else
                {
                    var parser = GetParser(typeInAnyAssembly, out var multi);
                    if (!(parser == null))
                    {
                        try
                        {
                            var customElementArgs = new CustomElementArgs(p, input);
                            if (multi)
                            {
                                if (parser.Invoke(null, new object[] { customElementArgs }) is not
                                    IEnumerable<WikiElement>
                                    enumerable)
                                {
                                    return;
                                }

                                foreach (var item2 in enumerable)
                                {
                                    if (item2 != null)
                                    {
                                        p.Elements.Add(item2);
                                    }
                                }

                                return;
                            }

                            if (parser.Invoke(null, new object[] { customElementArgs }) is WikiElement item)
                            {
                                p.Elements.Add(item);
                            }

                            return;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Wiki: Exception executing custom element parser " + typeInAnyAssembly.FullName +
                                      "." + parser.Name + "():");
                            Log.Error(ex.ToString());
                            return;
                        }
                    }

                    Log.Error("Wiki: Failed to find parser method in class '" + typeInAnyAssembly.FullName +
                              "'. There should be a static method in the class that has a single input parameter of type InGameWiki.CustomElementArgs and a return value of type InGameWiki.WikiElement or an enumerable of WikiElements.");
                }
            }
        }

        void AddText(string txt, bool large)
        {
            if (string.IsNullOrWhiteSpace(txt))
            {
                return;
            }

            if (large)
            {
                txt = "<color=cyan>" + txt + "</color>";
            }

            var wikiElement = WikiElement.Create(txt);
            wikiElement.FontSize = !large ? GameFont.Small : GameFont.Medium;
            p.Elements.Add(wikiElement);
        }

        string CheckParseChar(char tag, CurrentlyParsing newState, char last, int i, char c,
            ref CurrentlyParsing currentParsing, ref int shouldAppend)
        {
            if (c != tag)
            {
                if (currentParsing != 0)
                {
                    shouldAppend++;
                }

                return null;
            }

            if (last == '\\')
            {
                if (currentParsing == 0)
                {
                    return null;
                }

                str.Remove(str.Length - 1, 1);
                shouldAppend++;

                return null;
            }

            shouldAppend = -1000;
            if (currentParsing == CurrentlyParsing.None)
            {
                currentParsing = newState;
                str.Clear();
                return null;
            }

            if (currentParsing == newState)
            {
                currentParsing = CurrentlyParsing.None;
                var result3 = str.ToString();
                str.Clear();
                return result3;
            }

            Log.Error(
                $"Error parsing wiki '{fileName}' on line {i + 1}: got '{c}' which is invalid since {currentParsing} is currently active. Raw page:\n{rawText}");
            return null;
        }
    }

    private static MethodInfo GetParser(Type type, out bool multi)
    {
        multi = false;
        if (type == null)
        {
            return null;
        }

        if (classToParser.TryGetValue(type, out var value))
        {
            multi = value.Item2;
            return value.Item1;
        }

        foreach (var declaredMethod in AccessTools.GetDeclaredMethods(type))
        {
            if (!declaredMethod.IsStatic || declaredMethod.IsGenericMethod)
            {
                continue;
            }

            var parameters = declaredMethod.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CustomElementArgs))
            {
                continue;
            }

            var num = typeof(WikiElement).IsAssignableFrom(declaredMethod.ReturnType);
            if (!num && !typeof(IEnumerable<WikiElement>).IsAssignableFrom(declaredMethod.ReturnType))
            {
                continue;
            }

            classToParser.Add(type,
                (declaredMethod, typeof(IEnumerable<WikiElement>).IsAssignableFrom(declaredMethod.ReturnType)));
            multi = typeof(IEnumerable<WikiElement>).IsAssignableFrom(declaredMethod.ReturnType);
            return declaredMethod;
        }

        classToParser.Add(type, (null, false));
        return null;
    }
}