using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        private Vector2 loc = Vector2.zero;
        private static string search = "";
        private static int IndexCount = 0;
        internal static int IndexOfVEF = -1;
        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            int a = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "CETeam.CombatExtended");
            int b = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            IndexOfVEF = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "OskarPotocki.VanillaFactionsExpanded.Core");
            Harmony Harmony = new Harmony(base.Content.PackageIdPlayerFacing);


            Harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawHeadHair"), prefix: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.PreDrawHeadHair)));
            if (a == -1)
            {
                Harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawHeadHair"), transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranDrawHeadHair)));
            }
            else if (a < b)
            {
                MethodInfo info = AccessTools.Method(AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer+Harmony_PawnRenderer_DrawHeadHair"), "DrawHeadApparel");
                if (info != null)
                {
                    Harmony.Patch(info, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5.HarmonyPatchCE), nameof(HarmonyPatchA5.HarmonyPatchCE.TranCEDrawHeadHairDrawApparel)),
                        postfix: new HarmonyMethod(typeof(HarmonyPatchA5.HarmonyPatchCE), nameof(HarmonyPatchA5.HarmonyPatchCE.CanCEDrawHair)));
                }
            }
            Harmony.Patch(HarmonyPatchA5.methodInfo,
                transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranDrawHeadHairDrawApparel)));
            if (IndexOfVEF != -1 && HATweakerSetting.CloseVEFDraw)
            {
                Harmony.Unpatch(HarmonyPatchA5.methodInfo, HarmonyPatchType.All, "OskarPotocki.VFECore");
                Log.Warning(base.Content.PackageIdPlayerFacing + ":OskarPotocki.VFECore's Patch has removed");
            }
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            HATweakerSetting.HeadApparelDataInitialize();
            if (HATweakerSetting.HeadgearDisplayType.NullOrEmpty())
            {
                return;
            }
            List<string> list = HATweakerSetting.HeadgearDisplayType.Keys.ToList();
            Listing_Standard ls1 = new Listing_Standard();
            ls1.Begin(inRect.TopPart(0.05f));
            search = ls1.TextEntry(search);
            ls1.End();
            float LabelHeigh = 30f;
            Rect outRect = inRect.BottomPart(0.95f).TopPart(0.9f);
            Widgets.DrawWindowBackground(outRect);
            Rect viewRect = new Rect(-3f, -3f, outRect.width - 26f, (LabelHeigh + 5f) * IndexCount + 3f);
            Rect rect1 = new Rect(LabelHeigh + 5f, 0f, 150f, LabelHeigh);
            Rect rect2 = new Rect(0f, 0f, LabelHeigh, LabelHeigh);
            Rect rect3 = new Rect(rect1.x + rect1.width, 0f, 4 * LabelHeigh, LabelHeigh);
            Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
            int se = 0;
            TextAnchor anchor = Text.Anchor;
            foreach (string c in list)
            {
                ThingDef x = DefDatabase<ThingDef>.GetNamedSilentFail(c);
                if (x != null && x.label.IndexOf(search) != -1)
                {
                    se++;
                    rect1.y += 5f;
                    Widgets.Label(rect1, x.label);
                    Widgets.DrawLineHorizontal(rect2.x, rect2.y, rect2.width);
                    Widgets.DrawLineHorizontal(rect2.x, rect2.y + rect2.height, rect2.width);
                    Widgets.DrawLineVertical(rect2.x, rect2.y, rect2.height);
                    Widgets.DrawLineVertical(rect2.x + rect2.width, rect2.y, rect2.height);
                    GUI.DrawTexture(rect2, x.uiIcon);
                    Widgets.DrawLineVertical(rect3.x - 5f, rect3.y, rect3.height);
                    if (Mouse.IsOver(rect3))
                    {
                        Widgets.DrawHighlight(rect3);
                    }
                    Text.Anchor = TextAnchor.MiddleRight;
                    if (Widgets.RadioButtonLabeled(rect3, "No_Graphic".Translate(), HATweakerSetting.HeadgearDisplayType[c] == 0))
                    {
                        HATweakerSetting.HeadgearDisplayType[c] = 0;
                    }
                    Rect rect4 = rect3;
                    rect4.x += rect3.width + 10f;
                    if (Mouse.IsOver(rect4))
                    {
                        Widgets.DrawHighlight(rect4);
                    }
                    if (Widgets.RadioButtonLabeled(rect4, "No_Hair".Translate(), HATweakerSetting.HeadgearDisplayType[c] == 1))
                    {
                        HATweakerSetting.HeadgearDisplayType[c] = 1;
                    }
                    Rect rect5 = rect4;
                    rect5.x += rect4.width + 10f;
                    if (Mouse.IsOver(rect5))
                    {
                        Widgets.DrawHighlight(rect5);
                    }
                    Widgets.DrawLineVertical(rect5.x + rect5.width + 5f, rect5.y, rect5.height);
                    if (Widgets.RadioButtonLabeled(rect5, "Show_Hair".Translate(), HATweakerSetting.HeadgearDisplayType[c] >= 2))
                    {
                        int a = x.IsApparel && x.apparel.forceRenderUnderHair ? 2 : 3;
                        HATweakerSetting.HeadgearDisplayType[c] = a;
                    }
                    if (HATweakerSetting.HeadgearDisplayType[c] >= 2 && (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw))
                    {
                        Rect rect6 = rect5;
                        rect6.x += rect5.width + 10f;
                        if (Mouse.IsOver(rect6))
                        {
                            Widgets.DrawHighlight(rect6);
                        }
                        Widgets.DrawLineVertical(rect6.x - 5f, rect6.y, rect6.height);
                        if (Widgets.RadioButtonLabeled(rect6, "Under_Hair".Translate(), HATweakerSetting.HeadgearDisplayType[c] == 2))
                        {
                            HATweakerSetting.HeadgearDisplayType[c] = 2;
                        }
                        Rect rect7 = rect6;
                        rect7.x += rect6.width + 10f;
                        if (Mouse.IsOver(rect7))
                        {
                            Widgets.DrawHighlight(rect7);
                        }
                        Widgets.DrawLineVertical(rect7.x - 5f, rect7.y, rect7.height);
                        Widgets.DrawLineVertical(rect7.x + rect7.width + 5f, rect7.y, rect7.height);
                        if (Widgets.RadioButtonLabeled(rect7, "Above_Hair".Translate(), HATweakerSetting.HeadgearDisplayType[c] == 3))
                        {
                            HATweakerSetting.HeadgearDisplayType[c] = 3;
                        }
                    }
                    Text.Anchor = anchor;
                    rect1.y += LabelHeigh;
                    rect2.y += (LabelHeigh + 5f);
                    rect3.y += (LabelHeigh + 5f);
                }
            }
            IndexCount = se;
            Widgets.EndScrollView();
            Listing_Standard listing_Standard = new Listing_Standard();
            if (IndexOfVEF != -1)
            {
                listing_Standard.Begin(inRect.BottomPart(0.95f).BottomPart(0.1f));
                listing_Standard.GapLine(5f);
                listing_Standard.CheckboxLabeled("Close_VEF_Draw_HeadApparel".Translate(), ref HATweakerSetting.CloseVEFDraw, "Restart_to_apply_settings".Translate());
                listing_Standard.End();
            }
            HATweakerCache.ClearCache();
            HATweakerCache.MakeModCache();
            if (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
            {
                HATweakerUtility.MakeHeadApparelUnderHair();
                HATweakerUtility.MakeHeadApparelCoverHair();
            }
            ResolveAllApparelGraphics();
        }
        public static void ResolveAllApparelGraphics()
        {
            if (Find.Maps.NullOrEmpty())
            {
                return;
            }
            IEnumerable<Map> AllMap = Find.Maps.Where(x => x != null && x.mapPawns != null && !x.mapPawns.AllPawns.NullOrEmpty() && x.mapPawns.AllPawns.Any(c => c.Faction.IsPlayer));
            if (AllMap.EnumerableNullOrEmpty())
            {
                return;
            }
            foreach (Map map in AllMap)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawns)
                {
                    if (pawn.apparel != null)
                    {
                        pawn.apparel.Notify_ApparelChanged();
                    }
                }
            }
        }

        public override string SettingsCategory()
        {
            return base.Content.Name.Translate(0);
        }
    }

    public class HATweakerSetting : ModSettings
    {
        public static Dictionary<string, int?> HeadgearDisplayType = new Dictionary<string, int?>();
        public static bool CloseVEFDraw = false;
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref HeadgearDisplayType, "HeadgearDisplayType", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref CloseVEFDraw, "CloseVEFDraw", false);
        }
        public static void HeadApparelDataInitialize()
        {
            if (HATweakerCache.HeadApparel.NullOrEmpty())
            {
                return;
            }
            if (HeadgearDisplayType == null)
            {
                HeadgearDisplayType = new Dictionary<string, int?>();
            }
            foreach (ThingDef x in HATweakerCache.HeadApparel)
            {
                int a = 0;
                if (!x.apparel.bodyPartGroups.NullOrEmpty() && x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
                {
                    a = 1;
                }
                else
                {
                    a = x.apparel.forceRenderUnderHair ? 2 : 3;
                }
                if (HeadgearDisplayType.ContainsKey(x.defName))
                {
                    if (HeadgearDisplayType[x.defName] == null)
                    {
                        HeadgearDisplayType[x.defName] = a;
                    }
                }
                else
                {
                    HeadgearDisplayType.Add(x.defName, a);
                }
            }
        }
    }
    [StaticConstructorOnStartup]
    public static class HATweakerCache
    {
        public static List<ThingDef> HeadApparel = new List<ThingDef>();
        public static List<string> HeadApparelUnderHair = new List<string>();
        public static List<string> HeadApparelNoHair = new List<string>();
        public static List<string> HeadApparelNoGraphic = new List<string>();
        public static List<string> HeadApparelAboveHair = new List<string>();
        public static List<string> Layers
        {
            get
            {
                return HeadLayerListDefOf.AllHeadLayerList.HeadLayerList;
            }
        }
        static HATweakerCache()
        {
            HeadApparel = GetAllOverHead().NullOrEmpty() ? new List<ThingDef>() : GetAllOverHead();
            HATweakerSetting.HeadApparelDataInitialize();
            HATweakerMod.setting.Write();
            ClearCache();
            MakeModCache();
            if (HATweakerMod.IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
            {
                HATweakerUtility.MakeHeadApparelUnderHair();
                HATweakerUtility.MakeHeadApparelCoverHair();
            }
        }
        public static List<ThingDef> GetAllOverHead()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && x.apparel.LastLayer != null && Layers.Contains(x.apparel.LastLayer.defName)).ToList();
        }
        public static void MakeModCache()
        {
            if (HATweakerSetting.HeadgearDisplayType.NullOrEmpty())
            {
                return;
            }
            List<string> list = HATweakerSetting.HeadgearDisplayType.Keys.ToList();
            for (int x = 0; x < list.Count; x++)
            {
                string a = list[x];
                if (HATweakerSetting.HeadgearDisplayType[a] == 0)
                {
                    HeadApparelNoGraphic.Add(a);
                }
                else
                if (HATweakerSetting.HeadgearDisplayType[a] == 1)
                {
                    HeadApparelNoHair.Add(a);
                }
                else
                if (HATweakerSetting.HeadgearDisplayType[a] == 2)
                {
                    HeadApparelUnderHair.Add(a);
                }
                else
                if (HATweakerSetting.HeadgearDisplayType[a] == 3)
                {
                    HeadApparelAboveHair.Add(a);
                }
            }
        }
        public static void ClearCache()
        {
            HeadApparelUnderHair.Clear();
            HeadApparelAboveHair.Clear();
            HeadApparelNoGraphic.Clear();
            HeadApparelNoHair.Clear();
        }
    }

    public static class HATweakerUtility
    {
        public static void MakeHeadApparelUnderHair()
        {
            if (HATweakerCache.HeadApparelUnderHair.NullOrEmpty())
            {
                return;
            }
            foreach (string x in HATweakerCache.HeadApparelUnderHair)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(x)!=null&&DefDatabase<ThingDef>.GetNamedSilentFail(x).IsApparel)
                {
                    DefDatabase<ThingDef>.GetNamedSilentFail(x).apparel.forceRenderUnderHair = true;
                }
            }
        }
        public static void MakeHeadApparelCoverHair()
        {
            if (HATweakerCache.HeadApparelAboveHair.NullOrEmpty())
            {
                return;
            }
            foreach (string x in HATweakerCache.HeadApparelAboveHair)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(x) != null&&DefDatabase<ThingDef>.GetNamedSilentFail(x).IsApparel)
                {
                    DefDatabase<ThingDef>.GetNamedSilentFail(x).apparel.forceRenderUnderHair = false;
                }
            }
        }
    }







    public static class HarmonyPatchA5
    {
        internal static MethodInfo methodInfo = typeof(PawnRenderer).GetNestedTypes(AccessTools.all).
            SelectMany((Type type) => type.GetMethods(AccessTools.all)).FirstOrDefault((MethodInfo x) => x.Name == "<DrawHeadHair>g__DrawApparel|2");
        public static IEnumerable<CodeInstruction> TranDrawHeadHair(IEnumerable<CodeInstruction> codes)
        {
            int x = 0;
            List<CodeInstruction> list = codes.ToList();
            for (int a = 0; a < list.Count - 1; a++)
            {
                if (list[a].opcode == OpCodes.Ldloc_2 && list[a + 1].opcode == OpCodes.Brfalse_S && x == 0)
                {

                    yield return CodeInstructionExtensions.MoveLabelsFrom(new CodeInstruction(OpCodes.Ldarg_0), list[a]);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(typeof(PawnRenderer),"pawn"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(CanHairDisplay)));
                    x += 1;
                }
                else
                {
                    yield return list[a];
                }
            }
            yield return list.LastOrDefault();
        }
        public static bool CanHairDisplay(PawnRenderer renderer, Pawn pawn)
        {
            if (pawn==null)
            {
                return false;
            }
            if (pawn.Dead)
            {
                return false;
            }
            if (renderer.graphics.apparelGraphics.
                Any(x => !HATweakerCache.HeadApparelNoHair.NullOrEmpty() && HATweakerCache.HeadApparelNoHair.Contains(x.sourceApparel.def.defName)))
            {
                return false;
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> TranDrawHeadHairDrawApparel(IEnumerable<CodeInstruction> codes, MethodBase __originalMethod)
        {
            Type type = __originalMethod.DeclaringType;
            int x = 0;
            List<CodeInstruction> list = codes.ToList();
            for (int a = 0; a < list.Count; a++)
            {
                CodeInstruction code = list[a];
                if (CodeInstructionExtensions.Is(code, OpCodes.Ldfld, AccessTools.Field(type, "onHeadLoc")) && x == 0)
                {

                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetOnHeadLocOffset)));
                    x += 1;
                }
                else if (1 < a && a < list.Count - 3 && code.opcode == OpCodes.Ldloc_3 && list[a - 1].opcode == OpCodes.Ldloc_0 && list[a + 1].opcode == OpCodes.Ldarg_0)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, "onHeadLoc"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetLocOffset)));
                }
                else
                {
                    yield return code;
                }
            }
        }
        public static Vector3 GetOnHeadLocOffset(Vector3 onHeadLoc, ApparelGraphicRecord graphicRecord)
        {
            Vector3 a = new Vector3(onHeadLoc.x, onHeadLoc.y, onHeadLoc.z);
            if (!graphicRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace && !HATweakerCache.HeadApparelNoGraphic.Contains(graphicRecord.sourceApparel.def.defName))
            {
                if (!graphicRecord.sourceApparel.def.apparel.forceRenderUnderHair)
                {
                    a.y += 0.00289575267f;
                }
            }
            return a;
        }
        public static Vector3 GetLocOffset(Vector3 loc, Vector3 onHeadLoc, ApparelGraphicRecord graphicRecord)
        {
            Vector3 a = new Vector3(loc.x, loc.y, loc.z);

            if (graphicRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace && graphicRecord.sourceApparel.def.apparel.forceRenderUnderHair)
            {
                a.y = onHeadLoc.y;
            }
            return a;
        }
        public static void PreDrawHeadHair(ref PawnRenderer __instance)
        {
            if (__instance.graphics == null || __instance.graphics.apparelGraphics.NullOrEmpty())
            {
                return;
            }
            __instance.graphics.apparelGraphics.
            RemoveAll(x => !HATweakerCache.HeadApparelNoGraphic.NullOrEmpty() && HATweakerCache.HeadApparelNoGraphic.Contains(x.sourceApparel.def.defName));
        }



        public static class HarmonyPatchCE
        {
            public static IEnumerable<CodeInstruction> TranCEDrawHeadHairDrawApparel(IEnumerable<CodeInstruction> codes, ILGenerator generator)
            {
                Label l1 = generator.DefineLabel();
                int x = 0;
                List<CodeInstruction> list = codes.ToList();
                for (int a = 0; a < list.Count; a++)
                {
                    CodeInstruction code = list[a];
                    string operand = code.operand.ToStringSafe();
                    if (code.opcode == OpCodes.Stloc_S && operand == "UnityEngine.Vector3 (18)")
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloca_S, 11);
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelGraphicRecord), "sourceApparel"));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Apparel), "def"));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), "apparel"));
                        yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ApparelProperties), "forceRenderUnderHair"));
                        yield return new CodeInstruction(OpCodes.Brtrue, l1);
                        x = a;
                    }
                    else
                    if (x != 0 && a > x && code.opcode == OpCodes.Ldloca_S && operand == "UnityEngine.Matrix4x4 (17)")
                    {
                        code.labels.Add(l1);
                        yield return code;
                    }
                    else
                    if (a < list.Count - 2 && code.opcode == OpCodes.Ldloc_3 && list[a + 1].opcode == OpCodes.Ldarg_S && list[a + 1].OperandIs(6))
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetOnHeadLocOffset)));
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }

            public static void CanCEDrawHair(ref PawnRenderer renderer,ref Pawn pawn, ref bool shouldRenderHair)
            {
                if (pawn == null)
                {
                    shouldRenderHair = false;
                }else 
                if (pawn.Dead)
                {
                    shouldRenderHair = false;
                }
                else
                if (renderer.graphics.apparelGraphics.
                Any(x => !HATweakerCache.HeadApparelNoHair.NullOrEmpty() && HATweakerCache.HeadApparelNoHair.Contains(x.sourceApparel.def.defName)))
                {
                    shouldRenderHair = false;
                }
                else
                {
                    shouldRenderHair = true;
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