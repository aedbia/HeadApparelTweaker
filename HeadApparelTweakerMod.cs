using AlienRace;
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
        internal static string choose = "";
        internal static int IndexOfVEF = -1;
        internal static int IndexOfAR = -1;
        internal static Apparel Apparel = null;
        internal static ApparelGraphicRecord? ApparelGraphicRecord = null;
        internal Pawn Pawn = null;
        internal static string PawnName = null;
        private static bool InGame = false;
        internal static bool SettingOpen = false;
        private static Rot4 direction = Rot4.South;
        private static int changeBarInt = 0;
        private int HatStartIndex = 0;
        private static bool BarChange = false;
        private static int HATweakerModIndex = -1;
        private static Vector2 scrollPosition = Vector2.zero;
        private static int ChangeBar
        {
            get { return changeBarInt; }
            set
            {
                if (value != changeBarInt)
                {
                    BarChange = true;
                    changeBarInt = value;
                };
            }
        }

        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            int a = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "CETeam.CombatExtended");
            HATweakerModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            IndexOfAR = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces");
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
            else if (a < HATweakerModIndex)
            {
                MethodInfo info = AccessTools.Method(AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer+Harmony_PawnRenderer_DrawHeadHair"), "DrawHeadApparel");
                if (info != null)
                {
                    Harmony.Patch(info, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5.HarmonyPatchCE), nameof(HarmonyPatchA5.HarmonyPatchCE.TranCEDrawHeadHairDrawApparel)),
                        postfix: new HarmonyMethod(typeof(HarmonyPatchA5.HarmonyPatchCE), nameof(HarmonyPatchA5.HarmonyPatchCE.CanCEDrawHair)));
                }
            }
            Harmony.Patch(HarmonyPatchA5.methodInfoDrawApparel,
                transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranDrawHeadHairDrawApparel)));

            //Unpatch VEF Patch Of DrawHeadApparel;
            if (IndexOfVEF != -1 && HATweakerSetting.CloseVEFDraw)
            {
                Harmony.Unpatch(HarmonyPatchA5.methodInfoDrawApparel, HarmonyPatchType.All, "OskarPotocki.VFECore");
                Log.Warning(base.Content.PackageIdPlayerFacing + ":OskarPotocki.VFECore's Patch has removed");
            }
            if (IndexOfAR != -1 && HATweakerModIndex > IndexOfAR && HATweakerSetting.AlienRacePatch)
            {
                HarmonyPatchA5.HarmonyPatchAlienRace patchAlienRace = new HarmonyPatchA5.HarmonyPatchAlienRace(Harmony);
            }
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (!SettingOpen && ChangeBar == 1)
            {
                SettingOpen = true;
            }
            if (HATweakerSetting.HeadgearDisplayType.NullOrEmpty())
            {
                return;
            }
            List<ThingDef> list = HATweakerCache.HeadApparel;
            if (choose.NullOrEmpty())
            {
                choose = list.First().defName;
            }
            //Search Tool;
            string[] ob = new string[] { "Basic_Settings".Translate(), "Advanced_Settings".Translate(), "Global_Settings".Translate() };
            search = Widgets.TextArea(inRect.TopPart(0.04f).LeftPart(0.3f), search);
            if (IndexOfAR != -1 && HATweakerModIndex > IndexOfAR && HATweakerSetting.AlienRacePatch)
            {
                ob = new string[] { "Basic_Settings".Translate(), "Advanced_Settings".Translate(), "Global_Settings".Translate(), "Alien_Patch_Settings".Translate() };

            }
            else
            {
                ob = new string[] { "Basic_Settings".Translate(), "Advanced_Settings".Translate(), "Global_Settings".Translate() };
            }
            bool isAlienLoad = IndexOfAR != -1 && HATweakerModIndex > IndexOfAR && HATweakerSetting.AlienRacePatch;
            GUIContent[] guiB = new GUIContent[ob.Length];
            for (int i = 0; i < ob.Length; i++)
            {
                guiB[i] = new GUIContent(ob[i]);
            }
            GUIStyle guiA = new GUIStyle(GUI.skin.window);
            guiA.padding.bottom = -10;
            ChangeBar = GUI.SelectionGrid(inRect.TopPart(0.04f).RightPart(0.69f), ChangeBar, guiB, isAlienLoad ? 4 : 3, guiA);
            //Initialized ScrollView Data;
            float LabelHeigh = 30f;
            Rect rect0 = inRect.BottomPart(0.95f);
            if (ChangeBar == 0)
            {
                if (SettingOpen)
                {
                    SettingOpen = false;
                }
                int ShowCount = 5;
                List<ThingDef> list1 = list.Where(a1 => a1.label.IndexOf(search) != -1).ToList();
                if (Widgets.ButtonText(rect0.TopPart(0.052f), "↑"))
                {
                    if (HatStartIndex - ShowCount >= 0)
                    {
                        HatStartIndex -= ShowCount;
                    }
                }

                if (Widgets.ButtonText(rect0.BottomPart(0.052f), "↓"))
                {
                    if (list != null && HatStartIndex + ShowCount < list.Count)
                    {
                        HatStartIndex += ShowCount;
                    }
                }
                Rect r2 = new Rect(inRect.x, (inRect.y + inRect.height * 0.05f) + inRect.height * 0.95f * 0.052f, inRect.width, inRect.height * 0.95f * 0.896f);
                Widgets.DrawWindowBackground(r2);
                if (!list.NullOrEmpty())
                {
                    if (HatStartIndex >= list.Count)
                    {
                        HatStartIndex = 0;
                    }
                    Rect rt3 = new Rect(r2.x, r2.y, r2.width, r2.height / 5);
                    for (int i = HatStartIndex; i < HatStartIndex + 5 && i < list1.Count; i++)
                    {
                        ThingDef def = list1[i];
                        GUI.color = new ColorInt(97, 108, 122).ToColor;
                        GUI.DrawTexture(new Rect(rt3.x, rt3.y + rt3.height, rt3.width, 4f), BaseContent.WhiteTex);
                        Widgets.DrawLineHorizontal(rt3.x + 0.85f * rt3.height, rt3.y + rt3.height / 2, rt3.width - 0.85f * rt3.height);
                        Widgets.DrawLineVertical(rt3.x + 0.85f * rt3.height, rt3.y, rt3.height);
                        Widgets.DrawLineVertical(rt3.x + 0.85f * rt3.height + (rt3.width - 0.85f * rt3.height) / 5, rt3.y, rt3.height);
                        Widgets.DrawLineVertical(rt3.x + 0.85f * rt3.height + 2 * (rt3.width - 0.85f * rt3.height) / 5, rt3.y, rt3.height);
                        Widgets.DrawLineVertical(rt3.x + 0.85f * rt3.height + 3 * (rt3.width - 0.85f * rt3.height) / 5, rt3.y, rt3.height);
                        Widgets.DrawLineVertical(rt3.x + 0.85f * rt3.height + 4 * (rt3.width - 0.85f * rt3.height) / 5, rt3.y, rt3.height);

                        GUI.color = Color.white;
                        Rect rect = new Rect(rt3.x + 0.1f * rt3.height, rt3.y + 0.1f * rt3.height, rt3.height * 0.6f, rt3.height * 0.6f);
                        Widgets.DrawBox(rect);
                        GUI.DrawTexture(rect, def.uiIcon);
                        Rect rect1 = new Rect(rt3.x + 0.05f * rt3.height, rt3.y + rt3.height * 0.65f, rt3.height * 0.75f, rt3.height * 0.35f);
                        GUI.Label(rect1, def.label, new GUIStyle(Verse.Text.CurFontStyle)
                        {
                            alignment = TextAnchor.MiddleCenter
                        });
                        HATweakerSetting.InitialSingleSetting(def);
                        HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[def.defName];
                        Rect rect2 = new Rect(rt3.x + 0.9f * rt3.height, rt3.y, (rt3.width - 0.85f * rt3.height) / 5 - 0.1f * rt3.height, rt3.height);
                        if (Mouse.IsOver(rect2.TopHalf()))
                        {
                            Widgets.DrawHighlight(rect2.TopHalf());
                        }
                        if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "No_Graphic".Translate(), data.DisplayTypeInt == 0))
                        {
                            HATweakerSetting.HeadgearDisplayType[def.defName].DisplayTypeInt = 0;
                        }
                        if (data.DisplayTypeInt != 0)
                        {
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            Widgets.CheckboxLabeled(rect2.BottomHalf(), "Hide_Indoor".Translate(), ref HATweakerSetting.HeadgearDisplayType[def.defName].HideInDoor);
                        }
                        rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                        if (Mouse.IsOver(rect2.TopHalf()))
                        {
                            Widgets.DrawHighlight(rect2.TopHalf());
                        }
                        if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "No_Hair".Translate(), data.DisplayTypeInt == 1))
                        {
                            HATweakerSetting.HeadgearDisplayType[def.defName].DisplayTypeInt = 1;
                        }
                        if (data.DisplayTypeInt != 0)
                        {
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            Widgets.CheckboxLabeled(rect2.BottomHalf(), "Hide_No_Fight".Translate(), ref HATweakerSetting.HeadgearDisplayType[def.defName].HideNoFight);
                        }
                        rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                        if (Mouse.IsOver(rect2.TopHalf()))
                        {
                            Widgets.DrawHighlight(rect2.TopHalf());
                        }
                        if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "Show_Hair".Translate(), data.DisplayTypeInt >= 2))
                        {
                            HATweakerSetting.HeadgearDisplayType[def.defName].DisplayTypeInt = def.apparel.forceRenderUnderHair ? 2 : 3;
                        }
                        if (data.DisplayTypeInt != 0)
                        {
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            Widgets.CheckboxLabeled(rect2.BottomHalf(), "Hide_In_Bed".Translate(), ref HATweakerSetting.HeadgearDisplayType[def.defName].HideInBed);
                        }
                        if (data.DisplayTypeInt >= 2 && (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw))
                        {
                            rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                            if (Mouse.IsOver(rect2.TopHalf()))
                            {
                                Widgets.DrawHighlight(rect2.TopHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "Under_Hair".Translate(), data.DisplayTypeInt == 2))
                            {
                                HATweakerSetting.HeadgearDisplayType[def.defName].DisplayTypeInt = 2;
                            }
                            rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                            if (Mouse.IsOver(rect2.TopHalf()))
                            {
                                Widgets.DrawHighlight(rect2.TopHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "Above_Hair".Translate(), data.DisplayTypeInt == 3))
                            {
                                HATweakerSetting.HeadgearDisplayType[def.defName].DisplayTypeInt = 3;
                            }
                        }
                        HATweakerCache.SingleModCache(def.defName);
                        if (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
                        {
                            if (HATweakerCache.HeadApparelUnderHair.Contains(def.defName))
                            {
                                HATweakerUtility.UnderOrAboveHair(def.defName, true);
                            }
                            if (HATweakerCache.HeadApparelAboveHair.Contains(def.defName))
                            {
                                HATweakerUtility.UnderOrAboveHair(def.defName, false);
                            }
                        }

                        rt3.y += rt3.height;
                    }
                }
            }
            else
            if (ChangeBar == 1)
            {
                Rect outRect = rect0.LeftPart(0.3f);
                Widgets.DrawWindowBackground(outRect);
                Rect viewRect = new Rect(-3f, -3f, outRect.width - 26f, (LabelHeigh + 5f) * IndexCount + 3f);
                Rect rect1 = new Rect(LabelHeigh + 5f, 0f, outRect.width - 60f, LabelHeigh);
                Rect rect2 = new Rect(0f, 0f, LabelHeigh, LabelHeigh);
                Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
                int se = 0;

                //Draw ScrollView;
                foreach (ThingDef c in list)
                {
                    if (c.label.IndexOf(search) != -1)
                    {
                        se++;
                        if (Mouse.IsOver(rect1))
                        {
                            Widgets.DrawHighlight(rect1);
                        }
                        if (Widgets.RadioButtonLabeled(rect1, c.label, choose == c.defName))
                        {
                            choose = c.defName;
                        }
                        Widgets.DrawBox(rect2);
                        GUI.DrawTexture(rect2, c.uiIcon);
                        rect1.y += (LabelHeigh + 5f);
                        rect2.y += (LabelHeigh + 5f);
                    }
                }
                IndexCount = se;
                Widgets.EndScrollView();
                InGame = Current.Game != null && Current.Game.CurrentMap != null && Current.Game.CurrentMap.mapPawns != null && !Current.Game.CurrentMap.mapPawns.AllPawns.NullOrEmpty();
                //MainSetting
                ThingDef def = list.FirstOrDefault(x => x.defName == choose);
                HATweakerSetting.InitialSingleSetting(def);
                Rect main = rect0.RightPart(0.69f);
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
                main1.y += LabelHeigh;
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                if (Widgets.RadioButtonLabeled(main1, "No_Hair".Translate(), data.DisplayTypeInt == 1))
                {
                    HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 1;
                }
                main1.y += LabelHeigh;
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                if (Widgets.RadioButtonLabeled(main1, "Show_Hair".Translate(), data.DisplayTypeInt >= 2))
                {
                    HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = def.apparel.forceRenderUnderHair ? 2 : 3;
                }
                if (data.DisplayTypeInt >= 2 && (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw))
                {
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Under_Hair".Translate(), data.DisplayTypeInt == 2))
                    {
                        HATweakerSetting.HeadgearDisplayType[choose].DisplayTypeInt = 2;
                    }
                    main1.y += LabelHeigh;
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
                    if (data.DisplayTypeInt < 2 || (IndexOfVEF != -1 && !HATweakerSetting.CloseVEFDraw))
                    {
                        main1.y += 2 * LabelHeigh;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    Widgets.CheckboxLabeled(main1, "Hide_Indoor".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].HideInDoor);
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    Widgets.CheckboxLabeled(main1, "Hide_No_Fight".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].HideNoFight);
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    Widgets.CheckboxLabeled(main1, "Hide_In_Bed".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].HideInBed);

                    main1.y += (LabelHeigh + 10f);
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
                    if (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
                    {
                        Widgets.CheckboxLabeled(main1, "Advance_Mode".Translate(), ref HATweakerSetting.HeadgearDisplayType[choose].AdvanceMode);
                        if (data.AdvanceMode)
                        {
                            main1.y += LabelHeigh;
                            Widgets.Label(main1.LeftPart(0.7f), "Size".Translate() + ":" + data.size.ToString("F2"));
                            if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                            {
                                HATweakerSetting.HeadgearDisplayType[choose].size = Vector2.one;
                            }
                            main1.y += (LabelHeigh);
                            HATweakerSetting.HeadgearDisplayType[choose].size.x =
                            Widgets.HorizontalSlider_NewTemp(main1.LeftHalf(), data.size.x, 0.5f, 2f);
                            HATweakerSetting.HeadgearDisplayType[choose].size.y =
                            Widgets.HorizontalSlider_NewTemp(main1.RightHalf(), data.size.y, 0.5f, 2f);
                            main1.y += (LabelHeigh - 5f);
                            Widgets.Label(main1.LeftPart(0.7f), "North".Translate() + ":" + data.northOffset.ToString("F2"));
                            if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                            {
                                HATweakerSetting.HeadgearDisplayType[choose].northOffset = Vector2.zero;
                            }
                            main1.y += (LabelHeigh);
                            HATweakerSetting.HeadgearDisplayType[choose].northOffset.x =
                            Widgets.HorizontalSlider_NewTemp(main1.LeftHalf(), data.northOffset.x, -1, 1);
                            HATweakerSetting.HeadgearDisplayType[choose].northOffset.y =
                            Widgets.HorizontalSlider_NewTemp(main1.RightHalf(), data.northOffset.y, -1, 1);
                            main1.y += (LabelHeigh - 5f);
                            Widgets.Label(main1.LeftPart(0.7f), "South".Translate() + ":" + data.southOffset.ToString("F2"));
                            if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                            {
                                HATweakerSetting.HeadgearDisplayType[choose].southOffset = Vector2.zero;
                            }
                            main1.y += (LabelHeigh);
                            HATweakerSetting.HeadgearDisplayType[choose].southOffset.x =
                            Widgets.HorizontalSlider_NewTemp(main1.LeftHalf(), data.southOffset.x, -1, 1);
                            HATweakerSetting.HeadgearDisplayType[choose].southOffset.y =
                            Widgets.HorizontalSlider_NewTemp(main1.RightHalf(), data.southOffset.y, -1, 1);
                            main1.y += (LabelHeigh - 5f);
                            Widgets.Label(main1.LeftPart(0.7f), "East".Translate() + ":" + data.EastOffset.ToString("F2")
                                + " | " + "West".Translate() + data.GetOffset(Rot4.West).ToString("f2"));
                            if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                            {
                                HATweakerSetting.HeadgearDisplayType[choose].EastOffset = Vector2.zero;
                            }
                            main1.y += (LabelHeigh);
                            HATweakerSetting.HeadgearDisplayType[choose].EastOffset.x =
                            Widgets.HorizontalSlider_NewTemp(main1.LeftHalf(), data.EastOffset.x, -1, 1);
                            HATweakerSetting.HeadgearDisplayType[choose].EastOffset.y =
                            Widgets.HorizontalSlider_NewTemp(main1.RightHalf(), data.EastOffset.y, -1, 1);
                            main1.y += (LabelHeigh - 5f);
                            Widgets.Label(main1.LeftPart(0.7f), "Rotate".Translate() + ":" + "East".Translate() + "[" + data.EastRotation.ToString("0") + "]" + "South".Translate() + "[" + data.SouthRotation.ToString("0") + "]");
                            if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                            {
                                HATweakerSetting.HeadgearDisplayType[choose].EastRotation = 0f;
                                HATweakerSetting.HeadgearDisplayType[choose].SouthRotation = 0f;
                            }
                            main1.y += (LabelHeigh);
                            HATweakerSetting.HeadgearDisplayType[choose].EastRotation =
                            Widgets.HorizontalSlider_NewTemp(main1.LeftHalf(), data.EastRotation, -180, 180);
                            HATweakerSetting.HeadgearDisplayType[choose].SouthRotation =
                            Widgets.HorizontalSlider_NewTemp(main1.RightHalf(), data.SouthRotation, -180, 180);
                        }
                    }
                }
                Rect main2 = main.RightHalf();
                Rect main3 = new Rect(main2.x, main2.y, main2.width, main2.height - LabelHeigh - 5f);
                Widgets.DrawWindowBackground(main3);
                if (InGame)
                {
                    List<Pawn> Colonists = Current.Game.CurrentMap.mapPawns.FreeColonists;
                    if (!Colonists.NullOrEmpty())
                    {
                        if (PawnName == null)
                        {
                            PawnName = Colonists.FirstOrDefault().Name.ToStringFull;
                        }
                        if (Pawn == null)
                        {
                            Pawn = Colonists.FirstOrDefault();
                        }
                        if (PawnName != null && Widgets.ButtonText(main3.TopPart(0.05f), PawnName))
                        {
                            List<FloatMenuOption> Options = new List<FloatMenuOption>();
                            for (int i = 0; i < Colonists.Count; i++)
                            {
                                Pawn pa = Colonists[i];
                                string now = "";
                                if (pa.Name.ToStringFull == PawnName)
                                {
                                    now = "(Now)".Translate();
                                }
                                Options.Add(new FloatMenuOption(pa.Name.ToStringShort + now, () => PawnName = pa.Name.ToStringFull));
                            }
                            Find.WindowStack.Add(new FloatMenu(Options));
                        }

                        if (Pawn != null && Pawn.Name.ToStringFull != PawnName)
                        {
                            Pawn = Colonists.FirstOrDefault(x => x.Name.ToStringFull == PawnName);
                            if (Pawn == null)
                            {
                                PawnName = null;
                            }
                            else
                            {
                                PawnName = Pawn.Name.ToStringFull;
                            }
                        }
                    }
                    else
                    {
                        PawnName = null;
                        Pawn = null;
                        HATweakerCache.Texture = null;
                    }
                }
                if (!choose.NullOrEmpty() && Pawn != null && InGame)
                {
                    if (Apparel != null)
                    {
                        if (Apparel.def.defName != choose)
                        {
                            Apparel = HATweakerUtility.NewApparel(choose);
                        }
                        if (ApparelGraphicRecord == null || ((ApparelGraphicRecord)ApparelGraphicRecord).sourceApparel.def.defName != choose)
                        {
                            ApparelGraphicRecordGetter.TryGetGraphicApparel(Apparel, Pawn.story.bodyType, out ApparelGraphicRecord a);
                            ApparelGraphicRecord = a;
                        }
                    }
                    else
                    {
                        Apparel = HATweakerUtility.NewApparel(choose);
                    }
                }
                if (Pawn != null && InGame)
                {
                    HATweakerUtility.DrawPawnCache(Pawn, new Vector2(main2.width, main2.height), direction, out HATweakerCache.Texture);
                    if (HATweakerCache.Texture != null)
                    {
                        GUI.DrawTexture(main3.BottomPart(0.95f), HATweakerCache.Texture);
                    }
                }
                else
                {
                    GUI.Label(main3, "Into_Game".Translate());
                }
                Rect main4 = new Rect(main2.x, main2.y + main2.height - LabelHeigh, main2.width, LabelHeigh);
                if (Widgets.ButtonText(main4.LeftPart(0.32f), "←—"))
                {
                    if (direction == Rot4.South)
                    {
                        direction = Rot4.West;
                    }
                    else if (direction == Rot4.West)
                    {
                        direction = Rot4.North;
                    }
                    else if (direction == Rot4.North)
                    {
                        direction = Rot4.East;
                    }
                    else
                    {
                        direction = Rot4.South;
                    }
                }
                if (data.AdvanceMode)
                {
                    Rect two = main4.RightPart(0.66f).LeftHalf();
                    if (Mouse.IsOver(two))
                    {
                        TooltipHandler.TipRegion(two, "Reset_Change_Tooltip".Translate());
                    }
                    if (Widgets.ButtonText(two, "Reset_Change".Translate()))
                    {
                        HATweakerSetting.HeadgearDisplayType[choose].size = Vector2.one;
                        HATweakerSetting.HeadgearDisplayType[choose].southOffset = Vector2.zero;
                        HATweakerSetting.HeadgearDisplayType[choose].northOffset = Vector2.zero;
                        HATweakerSetting.HeadgearDisplayType[choose].EastOffset = Vector2.zero;
                        HATweakerSetting.HeadgearDisplayType[choose].SouthRotation = 0f;
                        HATweakerSetting.HeadgearDisplayType[choose].EastRotation = 0f;
                    }
                }
                if (Widgets.ButtonText(main4.RightPart(0.32f), "—→"))
                {
                    if (direction == Rot4.South)
                    {
                        direction = Rot4.East;
                    }
                    else if (direction == Rot4.East)
                    {
                        direction = Rot4.North;
                    }
                    else if (direction == Rot4.North)
                    {
                        direction = Rot4.West;
                    }
                    else
                    {
                        direction = Rot4.South;
                    }
                }
            }
            else
            if (ChangeBar == 2)
            {
                if (SettingOpen)
                {
                    SettingOpen = false;
                }
                Rect one = rect0.TopPart(0.04f);
                one.width /= 3f;
                if (Widgets.ButtonText(one, "Quick_NoGraphic".Translate()))
                {
                    QuickSetting(list, 0, null, 0);
                }
                one.x += one.width;
                if (Widgets.ButtonText(one, "Quick_HideHair".Translate()))
                {
                    QuickSetting(list, 0, null, 1);
                }
                one.x += one.width;
                if (Widgets.ButtonText(one, "Quick_DisplayHair".Translate()))
                {
                    QuickSetting(list, 0, null, 2);
                }
                one.x -= 2f * one.width;
                one.width *= 3f;
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideInDoor".Translate()))
                {
                    QuickSetting(list, 1, true, null);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideInDoor".Translate()))
                {
                    QuickSetting(list, 1, false, null);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideNoFight".Translate()))
                {
                    QuickSetting(list, 2, true, null);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideNoFight".Translate()))
                {
                    QuickSetting(list, 2, false, null);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideInBed".Translate()))
                {
                    QuickSetting(list, 3, true, null);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideInBed".Translate()))
                {
                    QuickSetting(list, 3, false, null);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one, "Reset_All_Setting".Translate()))
                {
                    HATweakerSetting.HeadgearDisplayType.Clear();
                    HATweakerSetting.HeadApparelDataInitialize();
                    HATweakerCache.MakeModCache();
                }
                one.y += one.height + 5f;
                if (Mouse.IsOver(one))
                {
                    Widgets.DrawHighlight(one);
                    TooltipHandler.TipRegion(one, "Only_Colonist_Tooltip".Translate());
                }
                Widgets.CheckboxLabeled(one, "Only_Colonist".Translate(), ref HATweakerSetting.OnlyWorkOnColonist);
                one.y += one.height + 5f;
                if (Mouse.IsOver(one))
                {
                    Widgets.DrawHighlight(one);
                    TooltipHandler.TipRegion(one, "Only_Colonist_1_Tooltip".Translate());
                }
                Widgets.CheckboxLabeled(one, "Only_Colonist_1".Translate(), ref HATweakerSetting.OnlyWorkOnColonist_1);
                one.y += one.height + 5f;
                if (Mouse.IsOver(one))
                {
                    Widgets.DrawHighlight(one);
                    TooltipHandler.TipRegion(one, "Only_Colonist_2_Tooltip".Translate());
                }
                Widgets.CheckboxLabeled(one, "Only_Colonist_2".Translate(), ref HATweakerSetting.OnlyWorkOnColonist_2);
                one.y += one.height + 5f;
                if (IndexOfVEF != -1)
                {
                    if (Mouse.IsOver(one))
                    {
                        Widgets.DrawHighlight(one);
                        TooltipHandler.TipRegion(one, "Restart_to_apply_settings".Translate());
                    }
                    Widgets.CheckboxLabeled(one, "Close_VEF_Draw_HeadApparel".Translate(), ref HATweakerSetting.CloseVEFDraw);
                    one.y += one.height + 5f;
                }
                if (IndexOfAR != -1)
                {
                    if (Mouse.IsOver(one))
                    {
                        Widgets.DrawHighlight(one);
                        TooltipHandler.TipRegion(one, "Restart_to_apply_settings".Translate());
                    }
                    Widgets.CheckboxLabeled(one, "AlienRace_Patch".Translate(), ref HATweakerSetting.AlienRacePatch);
                }
            }
            else
            if (ChangeBar == 3)
            {
                if (SettingOpen)
                {
                    SettingOpen = false;
                }
                Widgets.DrawWindowBackground(rect0);
                List<string> AllAlienBodyAddon = HATweakerCache.AllAlienBodyAddon;
                if (!AllAlienBodyAddon.NullOrEmpty())
                {
                    int count = AllAlienBodyAddon.Count(a => a.IndexOf(search) != -1);
                    Rect rect = new Rect(rect0.x + 5f, rect0.y + 5f, rect0.width - 10f, count * 30 + 10f);
                    Widgets.BeginScrollView(rect0, ref scrollPosition, rect);
                    Rect show = new Rect(rect0.x + 10f, rect0.y + 5f, (rect.width - 20) / 3, 30f);
                    foreach (string x in AllAlienBodyAddon)
                    {
                        if (x.IndexOf(search) != -1)
                        {
                            Widgets.Label(show, x);
                            show.x += rect.width / 3;
                            if (HATweakerSetting.AlienHeadBodyAddons == null)
                            {
                                HATweakerSetting.AlienHeadBodyAddons = new List<string>();
                            }
                            bool isHeadAddon = !HATweakerSetting.AlienHeadBodyAddons.NullOrEmpty() && HATweakerSetting.AlienHeadBodyAddons.Contains(x);
                            if (Widgets.RadioButtonLabeled(show, "Show_With_Hair".Translate(), isHeadAddon))
                            {
                                if (!isHeadAddon)
                                {
                                    HATweakerSetting.AlienHeadBodyAddons.Add(x);
                                }
                            }
                            show.x += rect.width / 3;
                            if (Widgets.RadioButtonLabeled(show, "No_Show_With_Hair".Translate(), !isHeadAddon))
                            {
                                if (isHeadAddon)
                                {
                                    HATweakerSetting.AlienHeadBodyAddons.Remove(x);
                                }
                            }
                            show.x = rect0.x + 10f;
                            show.y += 30;
                        }
                    }
                    Widgets.EndScrollView();
                }
            }
            //Log.Warning(Apparel.def.label);

            //The Switch Of VEF Patch And AlienRacePatch In Setting
            //Make SettingCache and Apply Setting;
            if (!choose.NullOrEmpty() && ChangeBar != 2)
            {
                if ((Pawn != null && InGame) && (ChangeBar == 1 || BarChange))
                {
                    Pawn.apparel.Notify_ApparelChanged();
                }
                HATweakerCache.SingleModCache(choose);
                if (IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
                {
                    if (HATweakerCache.HeadApparelUnderHair.Contains(choose))
                    {
                        HATweakerUtility.UnderOrAboveHair(choose, true);
                    }
                    if (HATweakerCache.HeadApparelAboveHair.Contains(choose))
                    {
                        HATweakerUtility.UnderOrAboveHair(choose, false);
                    }
                }
            }

            if (BarChange)
            {
                Pawn = null;
                PawnName = null;
                HATweakerCache.Texture = null;
                BarChange = false;
            }
        }
        private static void QuickSetting(List<ThingDef> defs, int mode, bool? targetBool, int? targetDisplayType)
        {
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                HATweakerSetting.InitialSingleSetting(def);
                HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[def.defName];
                if (mode == 0 && targetDisplayType != null)
                {
                    if (targetDisplayType == 2)
                    {
                        data.DisplayTypeInt = def.apparel.forceRenderUnderHair ? 2 : 3;
                    }
                    else
                    {
                        data.DisplayTypeInt = targetDisplayType;
                    }

                }
                else
                if (targetBool != null)
                {
                    bool a = (bool)targetBool;
                    if (mode == 1)
                    {
                        data.HideInDoor = a;
                    }
                    else
                    if (mode == 2)
                    {
                        data.HideNoFight = a;
                    }
                    else
                    if (mode == 3)
                    {
                        data.HideInBed = a;
                    }
                }
            }
        }
        public override void WriteSettings()
        {
            Pawn = null;
            PawnName = null;
            HATweakerCache.Texture = null;
            SettingOpen = false;
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
        public static bool AlienRacePatch = true;
        public static bool OnlyWorkOnColonist = true;
        public static bool OnlyWorkOnColonist_1 = true;
        public static bool OnlyWorkOnColonist_2 = true;
        public static List<string> AlienHeadBodyAddons = new List<string>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref CloseVEFDraw, "CloseVEFDraw", false);
            Scribe_Values.Look(ref AlienRacePatch, "AlienRacePatch", true);
            Scribe_Values.Look(ref OnlyWorkOnColonist, "OnlyWorkOnColonist", true);
            Scribe_Values.Look(ref OnlyWorkOnColonist_1, "OnlyWorkOnColonist_1", true);
            Scribe_Values.Look(ref OnlyWorkOnColonist_2, "OnlyWorkOnColonist_2", true);
            Scribe_Collections.Look(ref HeadgearDisplayType, "HeadgearDisplayType", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref AlienHeadBodyAddons, "AlienHeadBodyAddons");
        }
        public static void HeadApparelDataInitialize()
        {
            if (HATweakerCache.HeadApparel.NullOrEmpty())
            {
                return;
            }
            foreach (ThingDef x in HATweakerCache.HeadApparel)
            {
                InitialSingleSetting(x);
            }
        }

        internal static void InitialSingleSetting(ThingDef def)
        {
            if (HeadgearDisplayType == null)
            {
                HeadgearDisplayType = new Dictionary<string, HATSettingData>();
            }
            if (HeadgearDisplayType.ContainsKey(def.defName))
            {
                if (HeadgearDisplayType[def.defName] == null)
                {
                    int a;
                    if (!def.apparel.bodyPartGroups.NullOrEmpty() && def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
                    {
                        a = 1;
                    }
                    else
                    {
                        a = def.apparel.forceRenderUnderHair ? 2 : 3;
                    }
                    HeadgearDisplayType[def.defName] = new HATSettingData()
                    {
                        DisplayTypeInt = a
                    };
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                int a;
                if (!def.apparel.bodyPartGroups.NullOrEmpty() && def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
                {
                    a = 1;
                }
                else
                {
                    a = def.apparel.forceRenderUnderHair ? 2 : 3;
                }
                HeadgearDisplayType.Add(def.defName, new HATSettingData()
                {
                    DisplayTypeInt = a
                });
            }
        }

        public class HATSettingData : IExposable
        {
            public int? DisplayTypeInt;

            public bool HideNoFight = false;
            public bool HideInDoor = false;

            public bool AdvanceMode = false;
            public Vector2 size = Vector2.one;
            public Vector2 northOffset = Vector2.zero;
            public Vector2 southOffset = Vector2.zero;
            public Vector2 EastOffset = Vector2.zero;
            public float EastRotation = 0;
            public float SouthRotation = 0;

            public bool HideInBed = false;

            public void ExposeData()
            {
                Scribe_Values.Look(ref this.DisplayTypeInt, "HeadgearDisplayType", forceSave: true);
                if (DisplayTypeInt == null)
                {
                    DisplayTypeInt = 1;
                }
                Scribe_Values.Look(ref this.HideNoFight, "HideNoFight", false, true);
                Scribe_Values.Look(ref this.HideInDoor, "HideInDoor", false, true);
                Scribe_Values.Look(ref this.HideInBed, "HideInBed", false, true);
                Scribe_Values.Look(ref this.AdvanceMode, "AdvanceMode", false, true);
                Scribe_Values.Look(ref this.size, "size", Vector2.one, true);
                Scribe_Values.Look(ref this.northOffset, "northOffset", Vector2.zero, true);
                Scribe_Values.Look(ref this.southOffset, "southOffset", Vector2.zero, true);
                Scribe_Values.Look(ref this.EastOffset, "EastOffset", Vector2.zero, true);
                Scribe_Values.Look(ref this.EastRotation, "EastRotation", 0f, true);
                Scribe_Values.Look(ref this.SouthRotation, "SouthRotation", 0f, true);

            }

            public Vector2 GetOffset(Rot4 facing)
            {
                if (facing == Rot4.North)
                {
                    return northOffset;
                }
                else if (facing == Rot4.South)
                {
                    return southOffset;
                }
                else if (facing == Rot4.East)
                {
                    return EastOffset;
                }
                else
                {
                    return new Vector2(-EastOffset.x, EastOffset.y);
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

        public static List<string> HideNoFight = new List<string>();
        public static List<string> HideInDoor = new List<string>();
        public static List<string> HideInBed = new List<string>();
        public static List<string> AllAlienBodyAddon = new List<string>();

        internal static RenderTexture Texture = null;
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
            MakeModCache();
            if (HATweakerMod.IndexOfVEF == -1 || HATweakerSetting.CloseVEFDraw)
            {
                HATweakerUtility.MakeHeadApparelUnderOrAboveHair(true);
                HATweakerUtility.MakeHeadApparelUnderOrAboveHair(false);
            }
            if (HATweakerMod.IndexOfAR != -1 && HATweakerSetting.AlienRacePatch)
            {
                if (AllAlienBodyAddon == null)
                {
                    AllAlienBodyAddon = new List<string>();
                }
                new HarmonyPatchA5.HarmonyPatchAlienRace.AlienBodyAddonCollector();
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
                SingleModCache(list[x]);
            }
        }

        public static void SingleModCache(string defName)
        {
            HeadApparelNoGraphic.Remove(defName);
            HeadApparelNoHair.Remove(defName);
            HeadApparelUnderHair.Remove(defName);
            HeadApparelAboveHair.Remove(defName);
            HideInDoor.Remove(defName);
            HideNoFight.Remove(defName);
            HideInBed.Remove(defName);

            HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[defName];
            if (data.DisplayTypeInt == 0)
            {
                HeadApparelNoGraphic.Add(defName);
            }
            else
            if (data.DisplayTypeInt == 1)
            {
                HeadApparelNoHair.Add(defName);
            }
            else
            if (data.DisplayTypeInt == 2)
            {
                HeadApparelUnderHair.Add(defName);
            }
            else
            if (data.DisplayTypeInt == 3)
            {
                HeadApparelAboveHair.Add(defName);
            }
            if (data.DisplayTypeInt != 0)
            {
                if (data.HideInDoor)
                {
                    HideInDoor.Add(defName);
                }
                if (data.HideNoFight)
                {
                    HideNoFight.Add(defName);
                }
                if (data.HideInBed)
                {
                    HideInBed.Add(defName);
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
            HideInDoor.Clear();
        }
    }

    public static class HATweakerUtility
    {

        public static void DrawPawnCache(Pawn pawn, Vector2 size, Rot4 direction, out RenderTexture texture)
        {

            if (pawn != null)
            {
                RenderTexture rt = PortraitsCache.Get(pawn, size, direction, renderClothes: false);
                texture = rt;
                return;
            }
            texture = null;
        }
        public static Apparel NewApparel(string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
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

        public static void MakeHeadApparelUnderOrAboveHair(bool BeenUnder)
        {
            List<string> list = BeenUnder ? HATweakerCache.HeadApparelUnderHair : HATweakerCache.HeadApparelAboveHair;
            if (list.NullOrEmpty())
            {
                return;
            }
            foreach (string x in list)
            {
                UnderOrAboveHair(x, BeenUnder);
            }
        }
        public static void UnderOrAboveHair(string defName, bool BeenUnder)
        {
            ThingDef apparel = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (apparel != null && apparel.IsApparel)
            {
                bool a = apparel.apparel.forceRenderUnderHair;
                if (a != BeenUnder)
                {
                    DefDatabase<ThingDef>.GetNamedSilentFail(defName).apparel.forceRenderUnderHair = BeenUnder;
                }
            }
        }

        //internal static Texture2D ShowHair = ContentFinder<Texture2D>.Get("UI/SettingsUI/ShowHair");
        //internal static Texture2D ShowHead = ContentFinder<Texture2D>.Get("UI/SettingsUI/ShowHead");
    }


    public static class HarmonyPatchA5
    {
        internal static MethodInfo methodInfoDrawApparel = typeof(PawnRenderer).GetNestedTypes(AccessTools.all).
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
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
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
        public static bool CanHairDisplay(PawnRenderer renderer, Pawn pawn, bool origin)
        {
            if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist && !pawn.IsColonist)))
            {
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
            else
            {
                return origin;
            }
        }

        public static IEnumerable<CodeInstruction> TranDrawHeadHairDrawApparel(IEnumerable<CodeInstruction> codes, MethodBase __originalMethod)
        {
            Type type = __originalMethod.DeclaringType;
            FieldInfo field = type.GetFields().Where(c => c.FieldType == typeof(PawnRenderer)).FirstOrDefault();
            FieldInfo field1 = AccessTools.Field(typeof(PawnRenderer), "pawn");
            FieldInfo field2 = AccessTools.Field(type, "quat");
            int x = 0;
            List<CodeInstruction> list = codes.ToList();
            bool patch1 = true;
            bool patch2 = true;
            bool patch3 = true;
            bool patch4 = true;
            for (int a = 0; a < list.Count; a++)
            {
                CodeInstruction code = list[a];
                if (code.opcode == OpCodes.Stloc_0 && list[a + 1].opcode == OpCodes.Ldarg_1 && a < 20)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Ldfld, field1);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HarmonyPatchA5), nameof(SetMesh)));
                    yield return code;
                    patch1 = false;
                }
                else if (CodeInstructionExtensions.Is(code, OpCodes.Ldfld, AccessTools.Field(type, "onHeadLoc")) && x == 0)
                {

                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, "headFacing"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Ldfld, field1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetOnHeadLocOffset)));
                    x += 1;
                    patch2 = false;
                }
                else if (1 < a && a < list.Count - 3 && code.opcode == OpCodes.Ldloc_3 && list[a - 1].opcode == OpCodes.Ldloc_0 && list[a + 1].opcode == OpCodes.Ldarg_0)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, "onHeadLoc"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, "headFacing"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Ldfld, field1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetLocOffset)));
                    patch3 = false;
                }
                else if (code.opcode == OpCodes.Ldfld && code.OperandIs(field2))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(type, "headFacing"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field);
                    yield return new CodeInstruction(OpCodes.Ldfld, field1);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(SetRotation)));
                    patch4 = false;
                }
                else
                {
                    yield return code;
                }
            }
            if (patch1 && patch2 && patch3 && patch4)
            {
                Log.Warning("TranDrawHeadHairDrawApparel(1)-Fail");
            }
        }

        public static Quaternion SetRotation(Quaternion quat, Rot4 headFacing, ApparelGraphicRecord graphicRecord, Pawn pawn)
        {
            if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist_2 && !pawn.IsColonist)))
            {
                Quaternion a = new Quaternion()
                {
                    eulerAngles = quat.eulerAngles,
                    x = quat.x,
                    y = quat.y,
                    z = quat.z,
                    w = quat.w
                };
                if (HATweakerSetting.HeadgearDisplayType.ContainsKey(graphicRecord.sourceApparel.def.defName))
                {
                    HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[graphicRecord.sourceApparel.def.defName];
                    if (data.AdvanceMode)
                    {
                        float x;
                        if (headFacing == Rot4.West)
                        {
                            x = data.EastRotation;
                        }
                        else if (headFacing == Rot4.East)
                        {
                            x = -data.EastRotation;
                        }
                        else if (headFacing == Rot4.South)
                        {
                            x = data.SouthRotation;
                        }
                        else
                        {
                            x = -data.SouthRotation;
                        }
                        Vector3 b = new Vector3(a.eulerAngles.x, a.eulerAngles.y + x, a.eulerAngles.z);
                        a.eulerAngles = b;
                    }
                }
                return a;
            }
            else
            {
                return quat;
            }
        }

        public static Vector3 GetOnHeadLocOffset(Vector3 onHeadLoc, Rot4 headFacing, ApparelGraphicRecord graphicRecord, Pawn pawn)
        {
            Vector3 a = new Vector3(onHeadLoc.x, onHeadLoc.y, onHeadLoc.z);
            if (!graphicRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace && !HATweakerCache.HeadApparelNoGraphic.Contains(graphicRecord.sourceApparel.def.defName))
            {
                if (!graphicRecord.sourceApparel.def.apparel.forceRenderUnderHair)
                {
                    a.y += 0.00289575267f;
                }
            }
            if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist_2 && !pawn.IsColonist)))
            {
                if (HATweakerSetting.HeadgearDisplayType.ContainsKey(graphicRecord.sourceApparel.def.defName))
                {
                    HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[graphicRecord.sourceApparel.def.defName];
                    if (data.AdvanceMode)
                    {
                        Vector2 offset = data.GetOffset(headFacing);
                        a.x += offset.x;
                        a.z += offset.y;
                    }
                }
            }
            return a;
        }
        public static Vector3 GetLocOffset(Vector3 loc, Vector3 onHeadLoc, Rot4 headFacing, ApparelGraphicRecord graphicRecord, Pawn pawn)
        {
            Vector3 a = new Vector3(loc.x, loc.y, loc.z);

            if (graphicRecord.sourceApparel.def.apparel.hatRenderedFrontOfFace && graphicRecord.sourceApparel.def.apparel.forceRenderUnderHair)
            {
                a.y = onHeadLoc.y;
            }
            if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist_2 && !pawn.IsColonist)))
            {
                if (HATweakerSetting.HeadgearDisplayType.ContainsKey(graphicRecord.sourceApparel.def.defName))
                {
                    HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[graphicRecord.sourceApparel.def.defName];
                    if (data.AdvanceMode)
                    {
                        Vector2 offset = data.GetOffset(headFacing);
                        a.x += offset.x;
                        a.z += offset.y;
                    }
                }
            }
            return a;
        }

        public static Mesh SetMesh(Mesh mesh, ApparelGraphicRecord graphicRecord, Pawn pawn)
        {
            Mesh a = mesh;
            if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist_2 && !pawn.IsColonist)))
            {
                if (HATweakerSetting.HeadgearDisplayType.ContainsKey(graphicRecord.sourceApparel.def.defName))
                {
                    HATweakerSetting.HATSettingData data = HATweakerSetting.HeadgearDisplayType[graphicRecord.sourceApparel.def.defName];
                    if (data.AdvanceMode)
                    {
                        a = new Mesh()
                        {
                            name = mesh.name,
                            vertices = mesh.vertices,
                            triangles = mesh.triangles,
                            normals = mesh.normals,
                            uv = mesh.uv,
                            bounds = mesh.bounds
                        };
                        Vector2 size = data.size;
                        Vector3[] ve = mesh.vertices;
                        for (int k = 0; k < ve.Length; k++)
                        {
                            ve[k].x *= size.x;
                            ve[k].z *= size.y;
                        }
                        a.vertices = ve;
                    }
                }
            }
            return a;
        }




        public static void PreDrawHeadHair(ref PawnRenderer __instance, Pawn ___pawn)
        {
            if (__instance.graphics == null)
            {
                return;
            }
            if (HATweakerMod.SettingOpen && ___pawn.Name.ToStringFull == HATweakerMod.PawnName)
            {
                if (!__instance.graphics.apparelGraphics.NullOrEmpty())
                {
                    __instance.graphics.apparelGraphics.
                                        RemoveAll(x => x.sourceApparel.def.apparel.layers.Any(a => HeadLayerListDefOf.AllHeadLayerList.HeadLayerList.
                                        Contains(a.defName)));
                }
                else if (___pawn.RaceProps != null && ___pawn.RaceProps.Humanlike)
                {
                    __instance.graphics.apparelGraphics = new List<ApparelGraphicRecord>();
                }

                if (HATweakerMod.ApparelGraphicRecord != null)
                {
                    __instance.graphics.apparelGraphics.
                                        Add((ApparelGraphicRecord)HATweakerMod.ApparelGraphicRecord);
                }

            }
            if (__instance.graphics.apparelGraphics.NullOrEmpty())
            {
                return;
            }
            if (!(___pawn == null || (HATweakerSetting.OnlyWorkOnColonist_1 && !___pawn.IsColonist)))
            {
                __instance.graphics.apparelGraphics.
                RemoveAll(x => !HATweakerCache.HeadApparelNoGraphic.NullOrEmpty() && HATweakerCache.HeadApparelNoGraphic.
                Contains(x.sourceApparel.def.defName));
            }
            if (__instance.graphics.apparelGraphics.NullOrEmpty())
            {
                return;
            }
            if (!(___pawn == null || (HATweakerSetting.OnlyWorkOnColonist && !___pawn.IsColonist)))
            {
                IntVec3 position = ___pawn.Position;
                if (___pawn.Map != null && ___pawn.Position != null && !___pawn.Position.UsesOutdoorTemperature(___pawn.Map))
                {
                    __instance.graphics.apparelGraphics.
                        RemoveAll((ApparelGraphicRecord x) => !HATweakerCache.HideInDoor.NullOrEmpty<string>() && HATweakerCache.HideInDoor.
                        Contains(x.sourceApparel.def.defName));
                }
                if (__instance.graphics.apparelGraphics.NullOrEmpty())
                {
                    return;
                }
                if (!___pawn.Drafted)
                {
                    __instance.graphics.apparelGraphics.
                        RemoveAll((ApparelGraphicRecord x) => !HATweakerCache.HideNoFight.NullOrEmpty<string>() && HATweakerCache.HideNoFight.
                        Contains(x.sourceApparel.def.defName));
                }
                if (__instance.graphics.apparelGraphics.NullOrEmpty())
                {
                    return;
                }
                if (___pawn.InBed())
                {
                    __instance.graphics.apparelGraphics.
                        RemoveAll((ApparelGraphicRecord x) => !HATweakerCache.HideInBed.NullOrEmpty<string>() && HATweakerCache.HideInBed.
                        Contains(x.sourceApparel.def.defName));
                }
            }
        }





        public static IEnumerable<CodeInstruction> TranSetDrafted(IEnumerable<CodeInstruction> codes)
        {
            MethodInfo aaa = AccessTools.Method(typeof(PriorityWork), "ClearPrioritizedWorkAndJobQueue", null, null);
            List<CodeInstruction> list = codes.ToList();
            bool patch = true;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (code.opcode == OpCodes.Callvirt && code.OperandIs(aaa))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_DraftController), "pawn"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), "UpdateApparelData", null, null));
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
            if (!pawn.IsColonist && HATweakerSetting.OnlyWorkOnColonist)
            {
                return;
            }
            if (pawn.apparel != null && pawn.apparel.AnyApparel)
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
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Thing), "positionInt"));
                    yield return new CodeInstruction(OpCodes.Ldarg_1, null);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(IsPositionChange)));
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
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

            if (thing is Pawn)
            {
                Pawn pawn = thing as Pawn;
                if (!pawn.IsColonist && HATweakerSetting.OnlyWorkOnColonist)
                {
                    return;
                }
                if (pawn.Map != null && pawn.apparel != null && pawn.apparel.AnyApparel)
                {
                    if (ago.UsesOutdoorTemperature(pawn.Map) && !now.UsesOutdoorTemperature(pawn.Map))
                    {
                        pawn.apparel.Notify_ApparelChanged();
                    }
                    else

                        if (!ago.UsesOutdoorTemperature(pawn.Map) && now.UsesOutdoorTemperature(pawn.Map))
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
                int y = 0;
                List<CodeInstruction> list = codes.ToList();
                for (int a = 0; a < list.Count; a++)
                {
                    CodeInstruction code = list[a];
                    string operand = code.operand.ToStringSafe();
                    //Log.Warning(operand);
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
                        y++;
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
                        yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(GetOnHeadLocOffset)));
                        y++;
                    }
                    else
                    if (a < list.Count - 20 && code.opcode == OpCodes.Ldloc_S && operand == "UnityEngine.Mesh (6)" && list[a + 1].opcode == OpCodes.Ldloc_S
                        && (list[a + 1].operand.ToStringSafe() == "UnityEngine.Matrix4x4 (17)" || list[a + 1].operand.ToStringSafe() == "UnityEngine.Matrix4x4 (19)"))
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(SetMesh)));
                        y++;
                    }
                    else
                    if (CodeInstructionExtensions.Is(code, OpCodes.Ldarg_S, 6))
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldarg_S, 5);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(SetRotation)));
                        y++;
                    }
                    else
                    if (code.opcode == OpCodes.Stloc_S && operand == "System.Boolean (12)")
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchCE), nameof(AreadyHasGraphic)));
                        yield return code;
                        y++;
                    }
                    else
                    {
                        yield return code;
                    }
                }
                if (y != 7)
                {
                    Log.Warning("Head apparel tweaker: TranCEDrawHeadHairDrawApparel(4)-Fail");
                }
            }


            public static bool AreadyHasGraphic(ApparelGraphicRecord apparel)
            {
                return apparel.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.Overhead || apparel.sourceApparel.def.apparel.LastLayer == ApparelLayerDefOf.EyeCover;
            }


            public static void CanCEDrawHair(ref PawnRenderer renderer, ref Pawn pawn, ref bool shouldRenderHair)
            {
                if (!(pawn == null || (HATweakerSetting.OnlyWorkOnColonist_1 && !pawn.IsColonist)))
                {
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

        public class HarmonyPatchAlienRace
        {
            private static readonly Type type1 = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon");
            private static readonly BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
            private static readonly MethodInfo VisibleForPostureOf = type1.GetMethod("VisibleForPostureOf", Instance);
            private static readonly MethodInfo VisibleForBackstoryOf = type1.GetMethod("VisibleForBackstoryOf", Instance);
            private static readonly MethodInfo VisibleForRotStageOf = type1.GetMethod("VisibleForRotStageOf", Instance);
            private static readonly MethodInfo RequiredBodyPartExistsFor = type1.GetMethod("RequiredBodyPartExistsFor", Instance);
            private static readonly MethodInfo VisibleForGenderOf = type1.GetMethod("VisibleForGenderOf", Instance);
            private static readonly MethodInfo VisibleForBodyTypeOf = type1.GetMethod("VisibleForBodyTypeOf", Instance);
            private static readonly MethodInfo VisibleForDrafted = type1.GetMethod("VisibleForDrafted", Instance);
            public HarmonyPatchAlienRace(Harmony harmony)
            {
                if (HATweakerMod.IndexOfAR != -1)
                {
                    MethodInfo info = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon").GetMethods(AccessTools.all).
                        FirstOrDefault(x => x.Name == "CanDrawAddon" && x.GetParameters().Any(a => a.ParameterType == typeof(Pawn)));

                    if (info != null)
                    {
                        harmony.Patch(info, transpiler: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(TranCanDrawAddon))));
                    }
                }
            }
            public static IEnumerable<CodeInstruction> TranCanDrawAddon(IEnumerable<CodeInstruction> codes)
            {

                MethodInfo sort = AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(CanDrawAddon_1));
                List<CodeInstruction> list = codes.ToList();
                int a = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    CodeInstruction code = list[i];
                    if (code.opcode == OpCodes.Newobj)
                    {
                        continue;
                    }
                    else
                    if (code.opcode == OpCodes.Call && code.operand.ToStringSafe().IndexOf("CanDrawAddon") != -1)
                    {
                        yield return new CodeInstruction(OpCodes.Callvirt, sort);
                        a++;
                    }
                    else
                    {
                        yield return code;
                    }
                }
                if (a == 0 || a != 1)
                {
                    Log.Warning("Head apparel tweaker: Transpiler Alien Race-Failed");
                }
            }

            public static bool CanDrawAddon_1(object bodyaddon, Pawn pawn)
            {
                if (bodyaddon is AlienRace.AlienPartGenerator.BodyAddon)
                {
                    var addon = (AlienRace.AlienPartGenerator.BodyAddon)bodyaddon;
                    AlienRace.ExtendedGraphics.ExtendedGraphicsPawnWrapper alienPawn = new AlienRace.ExtendedGraphics.ExtendedGraphicsPawnWrapper(pawn);
                    bool toDraw = false;
                    List<string> hiddenUnderApparelTag = addon.hiddenUnderApparelTag;
                    List<BodyPartGroupDef> hiddenUnderApparelFor = addon.hiddenUnderApparelFor;
                    PawnRenderer renderer = pawn.Drawer.renderer;
                    if (IsHairBodyAddon(addon.Name))
                    {
                        if (!(renderer.graphics.apparelGraphics.
                        Any(x => !HATweakerCache.HeadApparelNoHair.NullOrEmpty() && HATweakerCache.HeadApparelNoHair.Contains(x.sourceApparel.def.defName))))
                        {
                            toDraw = true;
                        }
                    }
                    else
                    if (!alienPawn.HasApparelGraphics() || (hiddenUnderApparelTag.NullOrEmpty() && hiddenUnderApparelFor.NullOrEmpty()))
                    {
                        toDraw = true;
                    }
                    else
                    {
                        List<Apparel> apparels = renderer.graphics.apparelGraphics.Select(a => a.sourceApparel).ToList();
                        if (!apparels.Any((Apparel ap) => ap.def.apparel.bodyPartGroups.Any((BodyPartGroupDef bpgd) => hiddenUnderApparelFor.Contains(bpgd)) || ap.def.apparel.tags.Any((string s) => hiddenUnderApparelTag.Contains(s))))
                        {
                            toDraw = true;
                        }
                    }
                    if (toDraw)
                    {
                        if (VisibleForPostureOf != null
                            && VisibleForBackstoryOf != null
                            && VisibleForRotStageOf != null
                            && RequiredBodyPartExistsFor != null
                            && VisibleForGenderOf != null
                            && VisibleForBodyTypeOf != null
                            && VisibleForDrafted != null)
                        {
                            if ((bool)VisibleForPostureOf.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)VisibleForBackstoryOf.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)VisibleForRotStageOf.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)RequiredBodyPartExistsFor.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)VisibleForGenderOf.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)VisibleForBodyTypeOf.Invoke(bodyaddon, new object[] { alienPawn })
                                && (bool)VisibleForDrafted.Invoke(bodyaddon, new object[] { alienPawn })
                                && addon.VisibleForJob(alienPawn)
                                && addon.VisibleWithGene(alienPawn)
                                )
                            {
                                return addon.VisibleForRace(alienPawn);
                            };
                        }
                    }
                }
                return false;

            }



            private static bool IsHairBodyAddon(string name)
            {
                return !HATweakerSetting.AlienHeadBodyAddons.NullOrEmpty() && HATweakerSetting.AlienHeadBodyAddons.Contains(name);
            }
            public class AlienBodyAddonCollector
            {
                public AlienBodyAddonCollector()
                {
                    foreach (ThingDef_AlienRace def in DefDatabase<ThingDef_AlienRace>.AllDefs)
                    {
                        if (def.alienRace != null
                            && def.alienRace.generalSettings != null
                            && def.alienRace.generalSettings.alienPartGenerator != null
                            && !def.alienRace.generalSettings.alienPartGenerator.bodyAddons.NullOrEmpty())
                        {
                            var defBodyAddon = def.alienRace.generalSettings.alienPartGenerator.bodyAddons;
                            if (!defBodyAddon.NullOrEmpty())
                            {
                                foreach (var obj in defBodyAddon)
                                {
                                    //Log.Warning(obj.Name);
                                    HATweakerCache.AllAlienBodyAddon.Add(obj.Name);
                                }
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