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
        private static string choose = "";
        internal static int IndexOfVEF = -1;
        private static Dictionary<string, int> origin = new Dictionary<string, int>();


        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            int a = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "CETeam.CombatExtended");
            int b = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            IndexOfVEF = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "OskarPotocki.VanillaFactionsExpanded.Core");
            Harmony Harmony = new Harmony(base.Content.PackageIdPlayerFacing);

            Harmony.Patch(AccessTools.Method(typeof(PawnRenderer), "DrawHeadHair"), prefix: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.PreDrawHeadHair)));
            //Hide Not Drafted;
            MethodInfo original = AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted));
            Harmony.Patch(original, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranSetDrafted)));
            //Hide Under Roof;
            MethodInfo original2 = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            Harmony.Patch(original2, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranSetPosition)));

            //Head Hair Draw;
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

            //Unpatch VEF Patch Of DrawHeadApparel;
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
            if (choose.NullOrEmpty())
            {
                choose = list.First();
            }

            //Search Tool;
            Listing_Standard ls1 = new Listing_Standard();
            ls1.Begin(inRect.TopPart(0.05f));
            search = ls1.TextEntry(search);
            ls1.End();

            //Initialized ScrollView Data;
            float LabelHeigh = 30f;
            Rect outRect = inRect.BottomPart(0.95f).TopPart(0.9f).LeftPart(0.3f);
            Widgets.DrawWindowBackground(outRect);
            Rect viewRect = new Rect(-3f, -3f, outRect.width - 26f, (LabelHeigh + 5f) * IndexCount + 3f);
            Rect rect1 = new Rect(LabelHeigh + 5f, 0f, outRect.width - 60f, LabelHeigh);
            Rect rect2 = new Rect(0f, 0f, LabelHeigh, LabelHeigh);
            Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
            int se = 0;

            //Draw ScrollView;
            foreach (string c in list)
            {
                ThingDef x = DefDatabase<ThingDef>.GetNamedSilentFail(c);
                if (x != null && x.label.IndexOf(search) != -1)
                {
                    origin.SetOrAdd(c, x.IsApparel && x.apparel.forceRenderUnderHair ? 2 : 3);
                    se++;
                    if (Mouse.IsOver(rect1))
                    {
                        Widgets.DrawHighlight(rect1);
                    }
                    if (Widgets.RadioButtonLabeled(rect1, x.label, choose == c))
                    {
                        choose = c;
                    }
                    Widgets.DrawBox(rect2);
                    GUI.DrawTexture(rect2, x.uiIcon);
                    rect1.y += (LabelHeigh + 5f);
                    rect2.y += (LabelHeigh + 5f);
                }
            }
            IndexCount = se;
            Widgets.EndScrollView();
            Listing_Standard listing_Standard = new Listing_Standard();

            //MainSetting
            Rect main = inRect.BottomPart(0.95f).TopPart(0.9f).RightPart(0.69f);
            Widgets.DrawWindowBackground(main);
            HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[choose];
            Rect main1 = new Rect(main.x + 5f, main.y + 5f, main.width / 2 - 10f, LabelHeigh);

            if (Mouse.IsOver(main1))
            {
                Widgets.DrawHighlight(main1);
            }
            if (Widgets.RadioButtonLabeled(main1, "No_Graphic".Translate(), data.DisplayTypeInt == 0))
            {
                HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 0;
            }
            main1.y += (LabelHeigh + 5f);
            if (Mouse.IsOver(main1))
            {
                Widgets.DrawHighlight(main1);
            }
            if (Widgets.RadioButtonLabeled(main1, "No_Hair".Translate(), data.DisplayTypeInt == 1))
            {
                HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 1;
            }
            main1.y += (LabelHeigh + 5f);
            if (Mouse.IsOver(main1))
            {
                Widgets.DrawHighlight(main1);
            }
            if (Widgets.RadioButtonLabeled(main1, "Show_Hair".Translate(), data.DisplayTypeInt >= 2))
            {
                HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = origin[choose];
            }
            if (data.DisplayTypeInt >= 2 && (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw))
            {
                main1.y += (LabelHeigh + 5f);
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                if (Widgets.RadioButtonLabeled(main1, "Under_Hair".Translate(), data.DisplayTypeInt == 2))
                {
                    HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 2;
                }
                main1.y += (LabelHeigh + 5f);
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                if (Widgets.RadioButtonLabeled(main1, "Above_Hair".Translate(), data.DisplayTypeInt == 3))
                {
                    HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 3;
                }
            }
            if (data.DisplayTypeInt != 0)
            {
                if (data.DisplayTypeInt < 2)
                {
                    main1.y += 2 * (LabelHeigh + 5f);
                }
                main1.y += (LabelHeigh + 5f);
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                Widgets.CheckboxLabeled(main1, "Hide_Under_Roof".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].HideUnderRoof);
                main1.y += (LabelHeigh + 5f);
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                Widgets.CheckboxLabeled(main1, "Hide_No_Fight".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].HideNoFight);
            }

            Rect main2 = main.RightHalf();
            Rect main3 = new Rect(main2.x, main2.y, main2.width, main2.width);

            //The Switch Of VEF Patch In Setting
            if (IndexOfVEF != -1)
            {
                listing_Standard.Begin(inRect.BottomPart(0.95f).BottomPart(0.1f));
                listing_Standard.GapLine(5f);
                listing_Standard.CheckboxLabeled("Close_VEF_Draw_HeadApparel".Translate(), ref HATweakerSetting.CloseVEFDraw, "Restart_to_apply_settings".Translate());
                listing_Standard.End();
            }

            //Make SettingCache and Apply Setting;
            HATweakerCache.ClearCache();
            HATweakerCache.MakeModCache();
            if (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
            {
                HATweakerUtility.MakeHeadApparelUnderHair();
                HATweakerUtility.MakeHeadApparelCoverHair();
            }
        }


        public override void WriteSettings()
        {
            base.WriteSettings();
            ResolveAllApparelGraphics();
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

        public override string SettingsCategory()
        {
            return base.Content.Name.Translate(0);
        }
    }

    public class HATweakerSetting : ModSettings
    {
        public static Dictionary<string, HATSettingData> HeadgearDisplayType = new Dictionary<string, HATSettingData>();
        public static bool CloseVEFDraw = false;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref CloseVEFDraw, "CloseVEFDraw", false);
            Scribe_Collections.Look(ref HeadgearDisplayType, "HeadgearDisplayType", LookMode.Value, LookMode.Deep);
        }
        public static void HeadApparelDataInitialize()
        {
            if (HATweakerCache.HeadApparel.NullOrEmpty())
            {
                return;
            }
            if (HeadgearDisplayType == null)
            {
                HeadgearDisplayType = new Dictionary<string, HATSettingData>();
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
                        HeadgearDisplayType[x.defName] = new HATSettingData { DisplayTypeInt = a };
                    }
                }
                else
                {
                    HeadgearDisplayType.Add(x.defName, new HATSettingData { DisplayTypeInt = a });
                }
            }
        }
        public class HATSettingData : IExposable
        {
            public int? DisplayTypeInt;

            public bool HideNoFight = false;

            public bool HideUnderRoof = false;
            public void ExposeData()
            {
                Scribe_Values.Look<int?>(ref this.DisplayTypeInt, "HeadgearDisplayType", forceSave: true);
                if (DisplayTypeInt == null)
                {
                    DisplayTypeInt = 1;
                }
                Scribe_Values.Look<bool>(ref this.HideNoFight, "HideNoFight", false, true);
                Scribe_Values.Look<bool>(ref this.HideUnderRoof, "HideUnderRoof", false, true);
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

        public static List<string> HideNoFight = new List<string>();
        public static List<string> HideUnderRoof = new List<string>();
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
                HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[a];
                if (data.DisplayTypeInt == 0)
                {
                    HeadApparelNoGraphic.Add(a);
                }
                else
                if (data.DisplayTypeInt == 1)
                {
                    HeadApparelNoHair.Add(a);
                }
                else
                if (data.DisplayTypeInt == 2)
                {
                    HeadApparelUnderHair.Add(a);
                }
                else
                if (data.DisplayTypeInt == 3)
                {
                    HeadApparelAboveHair.Add(a);
                }
                if (data.DisplayTypeInt != 0)
                {
                    if (data.HideUnderRoof)
                    {
                        HideUnderRoof.Add(a);
                    }
                    if (data.HideNoFight)
                    {
                        HideNoFight.Add(a);
                    }
                }

            }
        }
        public static void ClearCache()
        {
            HeadApparelUnderHair.Clear();
            HeadApparelAboveHair.Clear();
            HeadApparelNoGraphic.Clear();
            HeadApparelNoHair.Clear();
            HideNoFight.Clear();
            HideUnderRoof.Clear();
        }
    }

    public static class HATweakerUtility
    {
        private static Texture2D RadioButOffTex = ContentFinder<Texture2D>.Get("UI/Widgets/RadioButOff");

        public static void MakeHeadApparelUnderHair()
        {
            if (HATweakerCache.HeadApparelUnderHair.NullOrEmpty())
            {
                return;
            }
            foreach (string x in HATweakerCache.HeadApparelUnderHair)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(x) != null && DefDatabase<ThingDef>.GetNamedSilentFail(x).IsApparel)
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
                if (DefDatabase<ThingDef>.GetNamedSilentFail(x) != null && DefDatabase<ThingDef>.GetNamedSilentFail(x).IsApparel)
                {
                    DefDatabase<ThingDef>.GetNamedSilentFail(x).apparel.forceRenderUnderHair = false;
                }
            }
        }
    }







    public static class HarmonyPatchA5
    {
        internal static MethodInfo methodInfo = typeof(PawnRenderer).GetNestedTypes(AccessTools.all).
            SelectMany((Type type) => type.GetMethods(AccessTools.all)).FirstOrDefault((MethodInfo x) => x.Name.Contains("DrawHeadHair") && x.Name.Contains("DrawApparel") && x.Name.Contains("2"));
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
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderer), "pawn"));
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
            if (pawn == null)
            {
                return false;
            }
            if (!pawn.health.hediffSet.HasHead || pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby || (pawn.Corpse != null && pawn.Corpse.IsDessicated()))
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
        public static void PreDrawHeadHair(ref PawnRenderer __instance, Pawn ___pawn)
        {
            if (__instance.graphics == null || __instance.graphics.apparelGraphics.NullOrEmpty())
            {
                return;
            }
            __instance.graphics.apparelGraphics.
            RemoveAll(x => !HATweakerCache.HeadApparelNoGraphic.NullOrEmpty() && HATweakerCache.HeadApparelNoGraphic.Contains(x.sourceApparel.def.defName));
            IntVec3 position = ___pawn.Position;
            if (___pawn.Map != null && ___pawn.Position != null && ___pawn.Position.Roofed(___pawn.Map))
            {
                __instance.graphics.apparelGraphics.RemoveAll((ApparelGraphicRecord x) => !HATweakerCache.HideUnderRoof.NullOrEmpty<string>() && HATweakerCache.HideUnderRoof.Contains(x.sourceApparel.def.defName));
            }
            if (!___pawn.Drafted)
            {
                __instance.graphics.apparelGraphics.RemoveAll((ApparelGraphicRecord x) => !HATweakerCache.HideNoFight.NullOrEmpty<string>() && HATweakerCache.HideNoFight.Contains(x.sourceApparel.def.defName));
            }
        }

        public static IEnumerable<CodeInstruction> TranSetDrafted(IEnumerable<CodeInstruction> codes)
        {
            MethodInfo aaa = AccessTools.Method(typeof(PriorityWork), "ClearPrioritizedWorkAndJobQueue", null, null);
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (code.opcode == OpCodes.Callvirt && code.OperandIs(aaa))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_DraftController), "pawn"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), "updateApparelData", null, null));
                }
                else
                {
                    yield return code;
                }
            }
        }
        public static void updateApparelData(Pawn pawn)
        {
            if (pawn.apparel != null && pawn.apparel.AnyApparel)
            {
                pawn.apparel.Notify_ApparelChanged();
            }
        }

        public static IEnumerable<CodeInstruction> TranSetPosition(IEnumerable<CodeInstruction> codes)
        {
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i < list.Count - 5 && code.opcode == OpCodes.Ldarg_0 && list[i + 1].opcode == OpCodes.Ldarg_1
                    && list[i + 2].opcode == OpCodes.Stfld && list[i + 3].opcode == OpCodes.Ldarg_0 && list[i + 4].opcode == OpCodes.Call)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "positionInt"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1, null);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(IsPositionChange)));
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static void IsPositionChange(Thing thing, IntVec3 ago, IntVec3 now)
        {

            if (thing is Pawn)
            {
                Pawn pawn = thing as Pawn;
                if (pawn.Map != null && pawn.apparel != null && pawn.apparel.AnyApparel)
                {
                    if (ago.Roofed(pawn.Map) && !now.Roofed(pawn.Map))
                    {
                        pawn.apparel.Notify_ApparelChanged();
                    }
                    else

                        if (!ago.Roofed(pawn.Map) && now.Roofed(pawn.Map))
                    {
                        pawn.apparel.Notify_ApparelChanged();
                    }

                }
            }
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

            public static void CanCEDrawHair(ref PawnRenderer renderer, ref Pawn pawn, ref bool shouldRenderHair)
            {
                if (pawn == null)
                {
                    shouldRenderHair = false;
                }
                else

                if (!pawn.health.hediffSet.HasHead || pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby || (pawn.Corpse != null && pawn.Corpse.IsDessicated()))
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