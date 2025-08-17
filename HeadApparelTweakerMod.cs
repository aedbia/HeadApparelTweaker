using ABEasyLib;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        public static int AlienIndex = -1;
        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            AlienIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces");
            if (AlienIndex == -1)
            {
                AlienIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces.dev");
            }
            Harmony harmony = new Harmony(this.Content.PackageIdPlayerFacing);
            HarmonyPatchHAT.PatchAllByHAT(harmony);
            if (AlienIndex != -1)
            {
                new HarmonyPatchHAT.HarmonyPatchAlienRace(harmony);
            }
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            SettingsWindowContents.DoSettingsWindowContents(inRect);
        }



        public override string SettingsCategory()
        {
            return "ABHATweaker".Translate();
        }

        public override void WriteSettings()
        {
            SettingsWindowContents.WriteSettings();
            base.WriteSettings();
        }

        public static void ResolveAllApparelGraphics()
        {
            if (Current.Game == null || Current.Game.CurrentMap == null)
            {
                return;
            }
            Map map = Current.Game.CurrentMap;
            if (map.mapPawns != null && !map.mapPawns.AllPawns.NullOrEmpty())
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn.apparel != null && pawn.apparel.AnyApparel)
                    {
                        pawn.apparel.Notify_ApparelChanged();
                    }
                }
            }

        }
    }

    [StaticConstructorOnStartup]
    public static class HATweakerCache
    {
        public static List<ThingDef> HeadApparel = new List<ThingDef>();
        internal static Texture texture = null;
        internal static Texture2D modUI = ContentFinder<Texture2D>.Get("UI/Buttons/body_apparel_ui");
        //public static Dictionary<string, DrawData> drawDataCache = new Dictionary<string, DrawData>();
        public static HATweakerUtility.AlienCompatible alienCompatible = null;
        public static List<string> Layers
        {
            get
            {
                return HeadLayerListDefOf.AllHeadLayerList.HeadLayerList;
            }
        }
        public static bool IsColonist(Pawn pawn)
        {
            if (HATweakerSetting.useIsColonistCache)
            {
                return ABEasyUtility.IsColonist(pawn);
            }
            return pawn.IsColonist;
        }
        static HATweakerCache()
        {
            HeadApparel = GetAllOverHead();
            HATweakerSetting.InitSetting();
            if (!HeadApparel.NullOrEmpty())
            {
                HATweakerMod.setting.ExposeData();
                HATweakerSetting.InitSetting();
                HATweakerMod.setting.Write();
            }
            if (HATweakerMod.AlienIndex != -1)
            {
                alienCompatible = new HATweakerUtility.AlienCompatible();
            }
        }


        public static List<ThingDef> GetAllOverHead()
        {
            //List<ThingDef> HeadApparel = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && x.apparel.LastLayer != null && Layers.Contains(x.apparel.LastLayer.defName)).ToList();
            List<ThingDef> BodyHeadApparel = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel &&
            (((!x.apparel.bodyPartGroups.NullOrEmpty()) && (x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) || x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead) || x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Eyes)))
            || ((!x.apparel.layers.NullOrEmpty()) && x.apparel.layers.Any(a => Layers.Contains(a.defName))))).ToList();
            return BodyHeadApparel;
        }

    }

    public static class HATweakerUtility
    {
        internal static void DrawPawnCacheWithApparel(Pawn pawn, Apparel apparel, Vector2 size, Rot4 direction, out Texture texture)
        {
            if (PawnTextureCache.GetPawnTextureCache(pawn, out var cache))
            {
                cache.GetPawnCacheWithApparel(apparel, size, direction, out texture);
                return;

            }
            texture = null;
        }

        internal static bool IsHeadApparel(ThingDef def)
        {
            return def.IsApparel && HATweakerCache.Layers.Contains(def.apparel.LastLayer.defName);
        }
        internal static T isRotationData<T>(object obj)
        {
            if (obj is T)
            {
                return (T)obj;
            }
            else
            {
                return default(T);
            }
        }

        internal static List<ThingStyleDef> GetStyles(ThingDef def)
        {
            if (def.RelevantStyleCategories.NullOrEmpty())
            {
                return new List<ThingStyleDef>();
            }
            return def.RelevantStyleCategories.SelectMany(a =>
            {
                return a.thingDefStyles.Where(b => b.ThingDef == def).Select(c => c.StyleDef);
            }).ToList();
        }
        internal static GUIStyle GetLabelStyle(TextAnchor anchor)
        {
            return new GUIStyle(Verse.Text.CurFontStyle)
            {
                alignment = anchor
            };
        }
        internal static Apparel NewApparel(ThingDef def)
        {
            if (def == null)
            {
                return null;
            }
            ThingDef stuff = null;
            if (def.MadeFromStuff)
            {
                stuff = GenStuff.DefaultStuffFor(def);
            }
            Apparel thing = (Apparel)ThingMaker.MakeThing(def, stuff);
            return thing;
            //(Apparel)ThingMaker.MakeThing(def, stuff);
        }
        public class AlienCompatible
        {
            public Dictionary<int, string> AllAddons = new Dictionary<int, string>();
            public Dictionary<int, List<Texture2D>> textures = new Dictionary<int, List<Texture2D>>();
            public AlienCompatible()
            {
                List<AlienRace.ThingDef_AlienRace> allRaces = DefDatabase<AlienRace.ThingDef_AlienRace>.AllDefs.ToList();
                if (allRaces.NullOrEmpty())
                {
                    return;
                }
                for (int k = 0; k < allRaces.Count; k++)
                {
                    string a = allRaces[k].defName;
                    for (int i = 0; i < allRaces[k].alienRace.generalSettings.alienPartGenerator.bodyAddons.Count; i++)
                    {
                        AlienRace.AlienPartGenerator.BodyAddon addon = allRaces[k].alienRace.generalSettings.alienPartGenerator.bodyAddons[i];
                        string b = a + "|" + (addon.Name.NullOrEmpty() ? "null" : addon.Name) + "|" + (addon.path.NullOrEmpty() ? "null" : addon.path);
                        int hash = addon.GetHashCode();
                        AllAddons.SetOrAdd(hash, b);
                        if (!addon.path.NullOrEmpty())
                        {
                            try
                            {
                                var texS = ContentFinder<Texture2D>.Get(addon.path + "_south", false);
                                var texN = ContentFinder<Texture2D>.Get(addon.path + "_north", false);
                                var texE = ContentFinder<Texture2D>.Get(addon.path + "_east", false);
                                var texW = ContentFinder<Texture2D>.Get(addon.path + "_west", false);
                                List<Texture2D> tex = new List<Texture2D>();
                                if (texS != null)
                                {
                                    tex.Add(texS);
                                }
                                if (texN != null)
                                {
                                    tex.Add(texN);
                                }
                                if (texE != null)
                                {
                                    tex.Add(texE);
                                }
                                if (texW != null)
                                {
                                    tex.Add(texW);
                                }
                                if (textures == null)
                                {
                                    textures = new Dictionary<int, List<Texture2D>>();
                                }
                                if (!tex.NullOrEmpty())
                                {
                                    textures.SetOrAdd(hash, tex);
                                }
                                /*if (graphics != null && graphics.Count() != 0)
                                {
                                    var gra = graphics.First();
                                    string path = gra.GetPath();
                                    if (!path.NullOrEmpty())
                                    {
                                        Log.Warning(path);
                                        var tex = ContentFinder<Texture2D>.Get(path,false);
                                        
                                    }
                                }*/
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }
                }
            }
        }
    }

    [DefOf]
    public static class HeadLayerListDefOf
    {
        public static HeadLayerListDef AllHeadLayerList;
    }

    public class HeadLayerListDef : Def
    {
        public List<string> HeadLayerList = new List<string>();
    }
}