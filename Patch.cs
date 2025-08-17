using ABEasyLib;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace HeadApparelTweaker
{
    public static class HarmonyPatchHAT
    {
        public static float rotate = 0;
        static Type This = typeof(HarmonyPatchHAT);
        static Type renderTree = typeof(PawnRenderTree);
        static Type renderNodeSetup = typeof(DynamicPawnRenderNodeSetup_Apparel);
        static FieldInfo nodeSetupNap = null;
        static FieldInfo nodeSetupNpawn = null;
        static FieldInfo nodeSetupBNode = null;
        internal static void PatchAllByHAT(Harmony harmony)
        {
            List<string> debug = new List<string>();
            var sub = renderNodeSetup?.GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic).Where(a => a.Name.IndexOf("ProcessApparel") != -1 && a.Name.IndexOf("d__5") != -1);
            MethodInfo processApparelN = null;
            if (sub != null && sub.Count() != 0)
            {
                //Log.Error("test");
                var t = sub.First();
                processApparelN = AccessTools.Method(t, "MoveNext");
                nodeSetupNpawn = AccessTools.Field(t, "pawn");
                nodeSetupNap = AccessTools.Field(t, "ap");
                nodeSetupBNode = AccessTools.Field(t, "bodyApparelNode");
            }
            MethodInfo processApparel = AccessTools.Method(renderNodeSetup, "ProcessApparel");
            bool fin = false;
            if (processApparel != null)
            {
                harmony.Patch(processApparel, prefix: new HarmonyMethod(This, nameof(PreProcessApparel)));
                fin = true;
            }
            if (processApparelN != null)
            {

                harmony.Patch(processApparelN, transpiler: new HarmonyMethod(This, nameof(TranProcessApparel)));
                if (fin)
                {
                    debug.Add("0 Patch ProcessApparel");
                }
            }
            MethodInfo adjustParms = AccessTools.Method(renderTree, "AdjustParms");
            if (adjustParms != null)
            {
                harmony.Patch(adjustParms, transpiler: new HarmonyMethod(This, nameof(TranAdjustParms)));
                debug.Add("1 Patch AdjustParms");
            }

            //Hide Not Drafted;
            MethodInfo setDraft = AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted));
            if (setDraft != null)
            {
                harmony.Patch(setDraft, transpiler: new HarmonyMethod(typeof(HarmonyPatchHAT), nameof(TranSetDrafted)));
                debug.Add("2 Patch DraftedSetter");
            }

            //Hide Under Roof;
            MethodInfo setPosition = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            if (setPosition != null)
            {
                harmony.Patch(setPosition, transpiler: new HarmonyMethod(typeof(HarmonyPatchHAT), nameof(TranSetPosition)));
                debug.Add("3 Patch PositionSetter");
            }

            if (debug.Count < 4)
            {
                Log.Warning(string.Join(" | ", debug));
            }
        }

        public static bool PreProcessApparel(Pawn pawn, PawnRenderTree tree, Apparel ap, PawnRenderNode headApparelNode, PawnRenderNode bodyApparelNode, Dictionary<PawnRenderNode, int> layerOffsets, ref IEnumerable<ValueTuple<PawnRenderNode, PawnRenderNode>> __result)
        {
            bool a = CanDrawApparel(pawn, ap);
            if (!a)
            {
                __result = new List<ValueTuple<PawnRenderNode, PawnRenderNode>>();
            }
            return a;
        }

        public static bool CanDrawApparel(Pawn pawn, Apparel ap)
        {
            if (ApplyGraphicData(pawn))
            {
                bool dis = false;
                if (SettingsWindowContents.ShowPawnGraphic && SettingsWindowContents.pawn == pawn)
                {
                    if (SettingsWindowContents.showBodyApparel)
                    {
                        dis = ap == SettingsWindowContents.apparel || (HATweakerCache.HeadApparel.NullOrEmpty() || !HATweakerCache.HeadApparel.Contains(ap.def));
                    }
                    else
                    {
                        dis = ap == SettingsWindowContents.apparel;
                    }
                }
                else
                {
                    dis = true;
                }
                if (dis)
                {
                    return CanDisplay(ap, pawn);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        private static bool ApplyGraphicData(Pawn pawn)
        {
            if (pawn != null && (!HATweakerSetting.WorkOnColonist || HATweakerCache.IsColonist(pawn)))
            {
                return true;
            }
            return false;
        }
        private static bool CanDisplay(Apparel ap, Pawn pawn)
        {
            if (HATweakerSetting.SettingData.TryGetValue(ap.def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (ModsConfig.IdeologyActive && !data.UseDefault && !data.ChildrenData.NullOrEmpty() && ap.StyleDef != null && data.ChildrenData.TryGetValue(ap.StyleDef.defName, out HATweakerSetting.HATSettingData data1) && !data1.UseDefault)
                {
                    return data1.CanDraw(pawn);
                }
                return data.CanDraw(pawn);
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> TranAdjustParms(IEnumerable<CodeInstruction> codes, ILGenerator generator)
        {
            Label a = generator.DefineLabel();
            FieldInfo info1 = typeof(ThingDef).GetField("apparel");
            FieldInfo info2 = typeof(ApparelProperties).GetField("renderSkipFlags");
            MethodInfo method = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            List<CodeInstruction> list = codes.ToList();
            List<string> debug = new List<string>();
            int success = 0;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i < 87 || i > 132)
                {
                    if (i > 6 && code.opcode == OpCodes.Ldfld && code.OperandIs(info2) && list[i - 1].opcode == OpCodes.Ldfld && list[i - 1].OperandIs(info1))
                    {

                        debug.Add($"\nAdjustParms - {code.opcode.ToStringSafe()} {code.operand.ToStringSafe()} [{success}]");
                        success++;
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderTree).GetField("pawn"));
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetDispalyFlags)));
                    }
                    else if (code.Is(OpCodes.Callvirt, method))
                    {
                        debug.Add($"\nAdjustParms - {code.opcode.ToStringSafe()} {code.operand.ToStringSafe()}");
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderTree).GetField("pawn"));
                        yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(This, nameof(GetApparel_1)));
                    }
                    else if (i == 57)
                    {
                        debug.Add($"\nAdjustParms - {code.opcode.ToStringSafe()} {code.operand.ToStringSafe()}");
                        yield return new CodeInstruction(OpCodes.Brfalse_S, a);
                    }
                    else if (i == 133)
                    {
                        debug.Add($"\nAdjustParms - {code.opcode.ToStringSafe()} {code.operand.ToStringSafe()}");
                        code.labels.Add(a);
                        yield return code;
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
            if (Prefs.DevMode && debug.Count > 0)
            {
                Log.Message("[HAT]:Transpiler of AdjustParms finished. Position:" + string.Join(",", debug));
            }

        }
        private static List<Apparel> GetApparel_1(List<Apparel> origin, Pawn pawn)
        {
            if (pawn == null)
            {
                return origin;
            }
            List<Apparel> or;
            try
            {
                if (PawnTextureCache.GetPawnTextureCache(pawn, out var cache))
                {
                    or = cache.GetApparelList(origin);
                }
                else
                {
                    or = origin;
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce(e.ToString(), pawn.GetHashCode());
                or = origin;
            }
            List<Apparel> a = or == null ? new List<Apparel>() : new List<Apparel>(or);
            a.RemoveAll(b => !CanDisplay(b, pawn));
            return a;
        }

        public static List<RenderSkipFlagDef> SetDispalyFlags(List<RenderSkipFlagDef> origin, Apparel apparel, Pawn pawn)
        {
            if (ApplyGraphicData(pawn) &&
               HATweakerSetting.SettingData.TryGetValue(apparel.def.defName, out HATweakerSetting.HATSettingData data0))
            {
                HATweakerSetting.HATSettingData data;
                if (ModsConfig.IdeologyActive && !data0.UseDefault && !data0.ChildrenData.NullOrEmpty()
                    && apparel.StyleDef != null
                    && data0.ChildrenData.TryGetValue(apparel.StyleDef.defName, out HATweakerSetting.HATSettingData data1)
                    && !data1.UseDefault)
                {
                    data = data1;
                }
                else
                {
                    data = data0;
                }
                List<RenderSkipFlagDef> list;
                if (SettingsWindowContents.ShowPawnGraphic && SettingsWindowContents.apparel != apparel)
                {
                    return new List<RenderSkipFlagDef>();
                }
                if (origin.NullOrEmpty())
                {
                    list = new List<RenderSkipFlagDef>();
                    if (data.DefaultNoEyes)
                    {
                        list.Add(RenderSkipFlagDefOf.Eyes);
                    }
                }
                else
                {
                    list = new List<RenderSkipFlagDef>(origin);

                }
                if (!data.NoHair)
                {
                    list.Remove(RenderSkipFlagDefOf.Hair);
                }
                else if (!list.Contains(RenderSkipFlagDefOf.Hair))
                {
                    list.Add(RenderSkipFlagDefOf.Hair);
                }
                if (!data.NoBeard)
                {
                    list.Remove(RenderSkipFlagDefOf.Beard);
                }
                else if (!list.Contains(RenderSkipFlagDefOf.Beard))
                {
                    list.Add(RenderSkipFlagDefOf.Beard);
                }
                return list;
            }

            else
            {
                return origin;
            }
        }

        public static IEnumerable<CodeInstruction> TranProcessApparel(IEnumerable<CodeInstruction> codes)
        {
            List<CodeInstruction> list = codes.ToList();
            FieldInfo field0 = AccessTools.Field(typeof(PawnRenderNodeProperties), nameof(PawnRenderNodeProperties.drawData));
            bool notNullField0 = field0 != null && nodeSetupNap != null && nodeSetupNpawn != null && nodeSetupBNode != null;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                /*if (i< list.Count - 10 && code.opcode == OpCodes.Stloc_S&& code.operand.ToStringSafe() == "Verse.PawnRenderNodeProperties (8)" && list[i+4].opcode == OpCodes.Callvirt&& list[i + 4].OperandIs(AccessTools.Method(renderTree, nameof(PawnRenderTree.ShouldAddNodeToTree))))
                {
                    yield return code;
                }else*/
                if (i > 10 && i < list.Count - 10 && notNullField0 && code.opcode == OpCodes.Stloc_2 && list[i + 1].opcode == OpCodes.Br & list[i + 2].opcode == OpCodes.Ldarg_0 && list[i + 3].Is(OpCodes.Ldfld, nodeSetupBNode) && list[i + 4].opcode == OpCodes.Brfalse)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloca, 2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, nodeSetupNpawn);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, nodeSetupNap);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetHeadClothesProps)));
                    if (Prefs.DevMode)
                    {
                        Log.Message("[HAT]: Transpiler of ProcessApparel finished.");
                    }
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static void SetHeadClothesProps(ref PawnRenderNodeProperties prop, Pawn pawn, Apparel cloth)
        {
            if (ApplyGraphicData(pawn) && cloth != null && HATweakerSetting.SettingData.TryGetValue(cloth.def.defName, out HATweakerSetting.HATSettingData data0))
            {
                HATweakerSetting.HATSettingData data;
                if (ModsConfig.IdeologyActive && !data0.UseDefault
                    && !data0.ChildrenData.NullOrEmpty()
                    && cloth.StyleDef != null
                    && data0.ChildrenData.TryGetValue(cloth.StyleDef.defName, out HATweakerSetting.HATSettingData data1)
                    && !data1.UseDefault)
                {
                    data = data1;
                }
                else
                {
                    data = data0;
                }
                if (data.AdvanceMode)
                {
                    var d = data.GetDrawData();
                    if (d != null)
                    {
                        prop.drawData = d;
                    }
                    prop.drawSize.x *= data.size.x;
                    prop.drawSize.y *= data.size.y;

                }
            }
        }
        public static IEnumerable<CodeInstruction> TranSetDrafted(IEnumerable<CodeInstruction> codes)
        {
            MethodInfo aaa = AccessTools.Method(typeof(PriorityWork), nameof(PriorityWork.ClearPrioritizedWorkAndJobQueue));
            List<CodeInstruction> list = codes.ToList();
            bool patch = true;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (code.opcode == OpCodes.Callvirt && code.OperandIs(aaa))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_DraftController), "pawn"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(UpdateApparelData)));
                    patch = false;
                }
                else
                {
                    yield return code;
                }
            }
            if (patch)
            {
                Log.Warning("TranSetDrafted(2)-Fail");
            }
        }
        public static void UpdateApparelData(Pawn pawn)
        {
            if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparel == null)
            {
                return;
            }
            if (ApplyGraphicData(pawn))
            {
                pawn.apparel.Notify_ApparelChanged();
            }
        }

        public static IEnumerable<CodeInstruction> TranSetPosition(IEnumerable<CodeInstruction> codes)
        {
            List<CodeInstruction> list = codes.ToList();
            bool patch = true;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i < list.Count - 5 && code.opcode == OpCodes.Ldarg_0 && list[i + 1].opcode == OpCodes.Ldarg_1
                    && list[i + 2].opcode == OpCodes.Stfld && list[i + 3].opcode == OpCodes.Ldarg_0 && list[i + 4].opcode == OpCodes.Call)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "positionInt"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchHAT), nameof(IsPositionChange)));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    patch = false;
                }
                else
                {
                    yield return code;
                }
            }
            if (patch)
            {
                Log.Warning("TranSetPosition(3)-Fail");
            }
        }

        public static void IsPositionChange(Thing thing, IntVec3 ago, IntVec3 now)
        {
            if (thing == null)
            {
                return;
            }
            if (thing is Pawn)
            {
                Pawn pawn = thing as Pawn;
                if (ago == now || pawn.apparel == null || pawn.apparel.WornApparel == null)
                {
                    return;
                }
                Map map = pawn.MapHeld;
                if (map == null || !ApplyGraphicData(pawn))
                {
                    //Log.Warning("HAT: Pawn " + pawn.Name.ToStringShort + " has no graphic data.");
                    return;
                }
                if (ago.InBounds(map) && now.InBounds(map))
                {
                    bool a = ago.UsesOutdoorTemperature(map);
                    bool b = now.UsesOutdoorTemperature(map);
                    bool work = ModsConfig.OdysseyActive;
                    if (a != b)
                    {
                        work = false;
                        pawn.apparel.Notify_ApparelChanged();
                    }
                    if (work)
                    {
                        bool a0 = ago.GetVacuum(map) == 0;
                        bool b0 = now.GetVacuum(map) == 0;
                        if (a0 != b0)
                        {
                            pawn.apparel.Notify_ApparelChanged();
                        }
                    }

                }
            }
        }

        public class HarmonyPatchAlienRace
        {
            //private static Dictionary<string, string> raceName = new Dictionary<string, string>();
            public HarmonyPatchAlienRace(Harmony harmony)
            {
                if (HATweakerMod.AlienIndex != -1)
                {
                    MethodInfo info = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon").GetMethods(AccessTools.all).
                        FirstOrDefault(x => x.Name == "CanDrawAddon" && x.GetParameters().Any(a => a.ParameterType == typeof(Pawn)));
                    string[] logs = new string[2];
                    if (info != null)
                    {
                        harmony.Patch(info, prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(PreCanDrawAddon))));
                        logs[0] = "CanDrawAddon";

                    }
                    MethodInfo info1 = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon").GetMethods(AccessTools.all).
                        FirstOrDefault(x => x.Name == "CanDrawAddonStatic" && x.GetParameters().Any(a => a.ParameterType == typeof(Pawn)));
                    if (info1 != null)
                    {
                        harmony.Patch(info1, prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(PreCanDrawAddon))));
                        logs[1] = "CanDrawAddonStatic";
                    }
                    if (Prefs.DevMode)
                    {
                        Log.Message($"[HAT]: Patch AlienRace {string.Join(",", logs)}");
                    }
                }
            }

            private static bool PreCanDrawAddon(Pawn pawn, object __instance, ref bool __result)
            {
                if (__instance is AlienRace.AlienPartGenerator.BodyAddon a)
                {
                    bool h = false;
                    if (IsHairBodyAddon(a.GetHashCode()))
                    {
                        if (a.useSkipFlags == null)
                        {
                            a.useSkipFlags = new List<RenderSkipFlagDef>();
                        }
                        a.useSkipFlags.Add(RenderSkipFlagDefOf.Hair);
                        h = true;
                    }
                    else
                    {
                        if ((!a.useSkipFlags.NullOrEmpty()) && a.useSkipFlags.Contains(RenderSkipFlagDefOf.Hair))
                        {
                            a.useSkipFlags.Remove(RenderSkipFlagDefOf.Hair);
                        }
                    }
                    bool b = false;
                    if (IsBeardBodyAddon(a.GetHashCode()))
                    {
                        if (a.useSkipFlags == null)
                        {
                            a.useSkipFlags = new List<RenderSkipFlagDef>();
                        }
                        a.useSkipFlags.Add(RenderSkipFlagDefOf.Beard);
                        b = true;
                    }
                    else
                    {
                        if ((!a.useSkipFlags.NullOrEmpty()) && a.useSkipFlags.Contains(RenderSkipFlagDefOf.Beard))
                        {
                            a.useSkipFlags.Remove(RenderSkipFlagDefOf.Beard);
                        }
                    }
                    if (h || b)
                    {
                        if (!a.conditions.NullOrEmpty())
                        {
                            foreach (AlienRace.ExtendedGraphics.Condition c in a.conditions)
                            {
                                if (!(c is AlienRace.ExtendedGraphics.ConditionApparel || c is AlienRace.ExtendedGraphics.ConditionApparelDef))
                                {
                                    if (!c.Satisfied(new AlienRace.ExtendedGraphics.ExtendedGraphicsPawnWrapper(pawn), ref a.resolveData))
                                    {
                                        __result = false;
                                        return false;
                                    }
                                }
                            }
                            ;
                        }
                        __result = true;
                        return false;
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }

            private static bool IsHairBodyAddon(int hashCode)
            {
                if (HATweakerCache.alienCompatible.AllAddons.ContainsKey(hashCode))
                {
                    string a = HATweakerCache.alienCompatible.AllAddons[hashCode];
                    return !HATweakerSetting.WithHair.NullOrEmpty() && HATweakerSetting.WithHair.Contains(a);
                }
                return false;
            }

            private static bool IsBeardBodyAddon(int hashCode)
            {
                if (HATweakerCache.alienCompatible.AllAddons.ContainsKey(hashCode))
                {
                    string a = HATweakerCache.alienCompatible.AllAddons[hashCode];
                    return !HATweakerSetting.WithBeard.NullOrEmpty() && HATweakerSetting.WithBeard.Contains(a);
                }
                return false;
            }
        }

    }
}
