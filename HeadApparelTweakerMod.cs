using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;
using UnityEngine;
using Verse;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        private static int HATweakerModIndex = -1;
        internal static string choose = "";
        private string search = "";
        private bool BarChange = false;
        private int ChangeBarInt = 0;
        private int HatStartIndex = 0;
        private float IndexCount = 0;
        private Vector2 loc = Vector2.zero;
        private static Rot4 direction = Rot4.South;
        internal static string PawnName = "";
        internal static Pawn pawn = null;
        internal static Apparel apparel = null;
        private Vector2 position0 = Vector2.zero;
        private float height0;
        internal static bool InGameSetting = false;

        private int ChangeBar
        {
            get { return ChangeBarInt; }
            set
            {
                if (value != ChangeBarInt)
                {
                    BarChange = true;
                    ChangeBarInt = value;
                };
            }
        }

        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            HATweakerModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            Harmony harmony = new Harmony(this.Content.PackageIdPlayerFacing);
            HarmonyPatchA5.PatchAllByHAT(harmony);
        }



        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<ThingDef> list = HATweakerCache.HeadApparel;
            if (choose.NullOrEmpty())
            {
                choose = list.First().defName;
            }
            //Search Tool;
            string[] ob = new string[] { "Basic_Settings".Translate(), "Advanced_Settings".Translate(), "Global_Settings".Translate() };
            search = Widgets.TextArea(inRect.TopPart(0.04f).LeftPart(0.3f), search);
            GUIContent[] guiB = new GUIContent[ob.Length];
            for (int i = 0; i < ob.Length; i++)
            {
                guiB[i] = new GUIContent(ob[i]);
            }
            GUIStyle guiA = new GUIStyle(GUI.skin.window);
            guiA.padding.bottom = -10;
            ChangeBar = GUI.SelectionGrid(inRect.TopPart(0.04f).RightPart(0.69f), ChangeBar, guiB, 3, guiA);
            //Initialized ScrollView Data;
            float LabelHeigh = 30f;
            Rect rect0 = inRect.BottomPart(0.95f);
            if (ChangeBar == 0)
            {
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
                        HATweakerSetting.SingleInit(def);
                        HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[def.defName];
                        Rect rect2 = new Rect(rt3.x + 0.9f * rt3.height, rt3.y, (rt3.width - 0.85f * rt3.height) / 5 - 0.1f * rt3.height, rt3.height);
                        if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "No_Graphic".Translate(), data.NoGraphic))
                        {
                            data.NoGraphic = !data.NoGraphic;
                        }
                        if (!data.NoGraphic)
                        {
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.BottomHalf(), "No_Beard".Translate(), data.NoBeard))
                            {
                                data.NoBeard = !data.NoBeard;
                            }
                            rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                            if (Mouse.IsOver(rect2.TopHalf()))
                            {
                                Widgets.DrawHighlight(rect2.TopHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "No_Hair".Translate(), data.NoHair))
                            {
                                data.NoHair = !data.NoHair;
                            }
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.BottomHalf(), "Hide_Indoor".Translate(), data.HideInDoor))
                            {
                                data.HideInDoor = !data.HideInDoor;
                            }
                            rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                            if (Mouse.IsOver(rect2.TopHalf()))
                            {
                                Widgets.DrawHighlight(rect2.TopHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "Hide_No_Fight".Translate(), data.HideNoFight))
                            {
                                data.HideNoFight = !data.HideNoFight;
                            }
                            if (Mouse.IsOver(rect2.BottomHalf()))
                            {
                                Widgets.DrawHighlight(rect2.BottomHalf());
                            }
                            if (Widgets.RadioButtonLabeled(rect2.BottomHalf(), "Hide_In_Bed".Translate(), data.HideInBed))
                            {
                                data.HideInBed = !data.HideInBed;
                            }

                        }
                        rt3.y += rt3.height;
                    }
                }
            }
            else
            if (ChangeBar == 1)
            {
                InGameSetting = Find.CurrentMap != null && Find.CurrentMap.mapPawns != null && Find.CurrentMap.mapPawns.ColonistsSpawnedCount > 0;
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
                //MainSetting
                ThingDef def = list.FirstOrDefault(x => x.defName == choose);
                HATweakerSetting.SingleInit(def);
                Rect main = rect0.RightPart(0.69f);
                Widgets.DrawWindowBackground(main);
                HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[choose];
                Widgets.BeginScrollView(main.LeftHalf(), ref position0, new Rect(0, 0, main.width / 2 - 18f, height0));
                Rect main1 = new Rect(5f, 5f, main.width / 2 - 28f, LabelHeigh);
                LabelHeigh -= 3f;
                if (Mouse.IsOver(main1))
                {
                    Widgets.DrawHighlight(main1);
                }
                if (Widgets.RadioButtonLabeled(main1, "No_Graphic".Translate(), data.NoGraphic))
                {
                    data.NoGraphic = !data.NoGraphic;
                }
                if (!data.NoGraphic)
                {
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "No_Hair".Translate(), data.NoHair))
                    {
                        data.NoHair = !data.NoHair;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "No_Beard".Translate(), data.NoBeard))
                    {
                        data.NoBeard = !data.NoBeard;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_Indoor".Translate(), data.HideInDoor))
                    {
                        data.HideInDoor = !data.HideInDoor;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_No_Fight".Translate(), data.HideNoFight))
                    {
                        data.HideNoFight = !data.HideNoFight;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_In_Bed".Translate(), data.HideInBed))
                    {
                        data.HideInBed = !data.HideInBed;
                    }

                    main1.y += (LabelHeigh + 10f);
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
                    LabelHeigh += 3f;
                    Widgets.CheckboxLabeled(main1, "Advance_Mode".Translate(), ref data.AdvanceMode);
                    if (data.AdvanceMode)
                    {

                        main1.y += LabelHeigh;
                        Widgets.Label(main1.LeftPart(0.7f), "Size".Translate() + ":" + data.size.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.size = Vector2.one;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.size.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.size.x, 0.5f, 2f);
                        data.size.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.size.y, 0.5f, 2f);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "South".Translate() + ":" + data.SouthOffset.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.SouthOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.SouthOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.SouthOffset.x, -1, 1);
                        data.SouthOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.SouthOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "North".Translate() + ":" + data.NorthOffset.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.NorthOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.NorthOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.NorthOffset.x, -1, 1);
                        data.NorthOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.NorthOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "West".Translate() + ":" + data.WestOffset.ToString("f2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.WestOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.WestOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.WestOffset.x, -1, 1);
                        data.WestOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.WestOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "East".Translate() + ":" + data.EastOffset.ToString("f2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.EastOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.EastOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.EastOffset.x, -1, 1);
                        data.EastOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.EastOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "Rotate".Translate() + ":" + "South".Translate() + "[" + data.SouthRotation.ToString("0") + "]" + "North".Translate() + "[" + data.NorthRotation.ToString("0") + "]");
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.SouthRotation = 0f;
                            data.NorthRotation = 0f;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.SouthRotation =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.SouthRotation, -180, 180);
                        data.NorthRotation =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.NorthRotation, -180, 180);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "Rotate".Translate() + ":" + "East".Translate() + "[" + data.EastRotation.ToString("0") + "]" + "West".Translate() + "[" + data.WestRotation.ToString("0") + "]");
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.EastRotation = 0f;
                            data.WestRotation = 0f;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.EastRotation =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.EastRotation, -180, 180);
                        data.WestRotation =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.WestRotation, -180, 180);
                        main1.y += (LabelHeigh - 6f);
                        Widgets.Label(main1.LeftPart(0.7f), "Layer_Offset".Translate() + ":" + data.LayerOffset.ToString("f5"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                        {
                            data.LayerOffset = 0f;
                        }
                        main1.y += (LabelHeigh - 6f);
                        data.LayerOffset =
                        Widgets.HorizontalSlider(main1, data.LayerOffset, -0.003f, +0.003f);
                    }
                }
                Widgets.EndScrollView();
                LabelHeigh = 30f;
                height0 = main1.y + LabelHeigh;
                Rect main2 = main.RightHalf();
                Rect main3 = new Rect(main2.x, main2.y, main2.width, main2.height - LabelHeigh - 5f);
                Widgets.DrawWindowBackground(main3);
                if (InGameSetting)
                {
                    List<Pawn> Colonists = Current.Game.CurrentMap.mapPawns.FreeColonists;
                    if (!Colonists.NullOrEmpty())
                    {
                        if (PawnName == null)
                        {
                            PawnName = Colonists.FirstOrDefault().Name.ToStringFull;
                        }
                        if (pawn == null)
                        {
                            pawn = Colonists.FirstOrDefault();
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
                        if (pawn != null && pawn.Name.ToStringFull != PawnName)
                        {
                            if (pawn.apparel != null)
                            {
                                pawn.apparel.Notify_ApparelChanged();
                            }
                            pawn = Colonists.FirstOrDefault(x => x.Name.ToStringFull == PawnName);
                            if (pawn == null)
                            {
                                PawnName = null;
                            }
                            else
                            {
                                PawnName = pawn.Name.ToStringFull;
                            }
                        }
                    }
                    else
                    {
                        PawnName = null;
                        pawn = null;
                        HATweakerCache.texture = null;
                    }
                }
                if (!choose.NullOrEmpty() && pawn != null && InGameSetting)
                {
                    if (apparel != null)
                    {
                        if (apparel.def.defName != choose)
                        {
                            apparel = HATweakerUtility.NewApparel(choose);
                        }
                    }
                    else
                    {
                        apparel = HATweakerUtility.NewApparel(choose);
                    }
                }
                if (pawn != null && InGameSetting)
                {
                    HATweakerUtility.DrawPawnCache(pawn, new Vector2(main2.width, main2.height), direction, out HATweakerCache.texture);
                    if (HATweakerCache.texture != null)
                    {
                        GUI.DrawTexture(main3.BottomPart(0.95f), HATweakerCache.texture);
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

                Rect two = main4.RightPart(0.66f).LeftHalf();
                if (Mouse.IsOver(two))
                {
                    TooltipHandler.TipRegion(two, "Reset_Change_Tooltip".Translate());
                }
                if (Widgets.ButtonText(two, "Reset_Change".Translate()))
                {
                    if (data.AdvanceMode)
                    {
                        data.size = Vector2.one;
                        data.SouthOffset = Vector2.zero;
                        data.NorthOffset = Vector2.zero;
                        data.EastOffset = Vector2.zero;
                        data.WestOffset = Vector2.zero;
                        data.SouthRotation = 0f;
                        data.NorthRotation = 0f;
                        data.EastRotation = 0f;
                        data.WestRotation = 0f;
                    }
                    if (!HATweakerSetting.SettingData.NullOrEmpty() && HATweakerSetting.SettingData.ContainsKey(def.defName))
                    {
                        HATweakerSetting.SettingData.Remove(def.defName);
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
                Rect one = rect0.TopPart(0.04f);
                one.width /= 3f;
                if (Widgets.ButtonText(one, "Quick_NoGraphic".Translate()))
                {
                    QuickSetting(0, true);
                }
                one.x += one.width;
                if (Widgets.ButtonText(one, "Quick_HideHair".Translate()))
                {
                    QuickSetting(1, true);
                }
                one.x += one.width;
                if (Widgets.ButtonText(one, "Quick_DisplayHair".Translate()))
                {
                    QuickSetting(1, false);
                }
                one.x -= 2f * one.width;
                one.width *= 3f;
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_HideBeard".Translate()))
                {
                    QuickSetting(2, true);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_DisplayBeard".Translate()))
                {
                    QuickSetting(2, false);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideInDoor".Translate()))
                {
                    QuickSetting(3, true);
                }

                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideInDoor".Translate()))
                {
                    QuickSetting(3,false);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideNoFight".Translate()))
                {
                    QuickSetting(4,true);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideNoFight".Translate()))
                {
                    QuickSetting(4, false);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideInBed".Translate()))
                {
                    QuickSetting(5,true);
                }
                if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideInBed".Translate()))
                {
                    QuickSetting(5,false);
                }
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one, "Reset_All_Setting".Translate()))
                {
                    HATweakerSetting.SettingData.Clear();
                    HATweakerSetting.InitSetting();
                }
                one.y += one.height + 5f;
                if (Mouse.IsOver(one))
                {
                    Widgets.DrawHighlight(one);
                }
                Widgets.CheckboxLabeled(one, "Only_Colonist".Translate(), ref HATweakerSetting.WorkOnColonist);
                /*one.y += one.height + 5f;
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
                }*/
            }
            /*else
            if (ChangeBar == 3)
            {
                if (SettingOpen)
                {
                    SettingOpen = false;
                }
                HarmonyPatchA5.HarmonyPatchAlienRace.DrawAlienRaceAboutSetting(rect0, search, ref scrollPosition);
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
            }*/
            if (BarChange)
            {
                pawn = null;
                apparel = null;
                PawnName = null;
                HATweakerCache.texture = null;
                BarChange = false;

            }
            if (ChangeBar != 1)
            {
                InGameSetting = false;
            }
            void QuickSetting(int a, bool on)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef def = list[i];
                    HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[def.defName];
                    if (a == 0)
                    {
                        data.NoGraphic = on;
                    }
                    else
                    {
                        data.NoGraphic = false;
                        if (a == 1)
                        {
                            data.NoHair = on;
                        }
                        else
                        if (a == 2)
                        {
                            data.NoBeard = on;
                        }
                        else
                        if (a == 3)
                        {
                            data.HideInDoor = on;
                        }
                        else
                        if (a == 4)
                        {

                        }
                    }
                }
            }
        }
        public override string SettingsCategory()
        {
            return base.Content.Name.Translate();
        }

        public override void WriteSettings()
        {
            pawn = null;
            apparel = null;
            PawnName = null;
            HATweakerCache.texture = null;
            BarChange = false;
            InGameSetting = false;
            ResolveAllApparelGraphics();
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

    public class HATweakerSetting : ModSettings
    {
        public static Dictionary<string, HATSettingData> SettingData = new Dictionary<string, HATSettingData>();
        public static bool WorkOnColonist = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref WorkOnColonist, "WorkOnColonist",true);
            List<string> names = SettingData.Keys.ToList();
            if (Scribe.EnterNode("HATData"))
            {
                try
                {
                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        if (names != null)
                        {
                            foreach (string name in names)
                            {
                                HATSettingData target = SettingData[name];
                                if (target.valueChange())
                                {
                                    Scribe_Deep.Look(ref target, name);
                                }
                            }
                            return;
                        }
                        Scribe.saver.WriteAttribute("IsNull", "True");
                    }
                    else if (Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        XmlNode curXmlParent = Scribe.loader.curXmlParent;
                        XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
                        if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            SettingData = null;
                        }
                        else
                        {

                            Dictionary<string, HATSettingData> list = new Dictionary<string, HATSettingData>(curXmlParent.ChildNodes.Count);
                            foreach (XmlNode childNode in curXmlParent.ChildNodes)
                            {

                                string name = childNode.Name;
                                HATSettingData a = ScribeExtractor.SaveableFromNode<HATSettingData>(childNode, null);
                                list.SetOrAdd(name, a);
                            }
                            SettingData = list;
                        }
                    }
                    return;
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
        }
        public static void InitSetting()
        {
            if (HATweakerCache.HeadApparel.NullOrEmpty())
            {
                return;
            }
            for (int i = 0; i < HATweakerCache.HeadApparel.Count; i++)
            {
                ThingDef def = HATweakerCache.HeadApparel[i];
                SingleInit(def);
            }
        }

        public static void SingleInit(ThingDef def)
        {
            if (!def.IsApparel)
            {
                return;
            }
            if (SettingData.NullOrEmpty())
            {
                SettingData = new Dictionary<string, HATSettingData>();
            }
            bool a;
            bool b;
            bool c = !def.apparel.renderSkipFlags.NullOrEmpty();
            if (c)
            {
                a = def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.Hair);
                b = def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.Beard);
            }
            else
            {
                b = def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead);
                a = b || def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead);
            }
            if (SettingData.ContainsKey(def.defName))
            {
                HATSettingData data = SettingData[def.defName];
                if (data == null)
                {

                    SettingData.SetOrAdd(def.defName, new HATSettingData()
                    {
                        NoHair = a,
                        NoBeard = b,
                    });
                }
            }
            else
            {
                SettingData.SetOrAdd(def.defName, new HATSettingData()
                {
                    NoHair = a || b,
                    NoBeard = b
                });
            }
            SettingData[def.defName].DefaultNoHair = a;
            SettingData[def.defName].DefaultNoBeard = b;
            SettingData[def.defName].DefaultNoEyes = c && b;
        }
        public class HATSettingData : IExposable
        {
            public Vector2 size = Vector2.one;
            public Vector2 NorthOffset = Vector2.zero;
            public Vector2 SouthOffset = Vector2.zero;
            public Vector2 EastOffset = Vector2.zero;
            public Vector2 WestOffset = Vector2.zero;
            public float NorthRotation = 0;
            public float SouthRotation = 0;
            public float EastRotation = 0;
            public float WestRotation = 0;
            public float LayerOffset = 0;
            public bool NoGraphic = false;
            public bool NoHair = false;
            public bool DefaultNoHair = false;
            public bool NoBeard = false;
            public bool DefaultNoBeard = false;
            public bool HideInDoor = false;
            public bool HideNoFight = false;
            public bool HideInBed = false;
            public bool AdvanceMode = false;
            public bool DefaultNoEyes = false;

            public void ExposeData()
            {
                Scribe_Values.Look(ref NoGraphic, "NoGraphic", false);
                Scribe_Values.Look(ref NoHair, "NoHair", DefaultNoHair);
                Scribe_Values.Look(ref NoBeard, "NoBeard", DefaultNoBeard);
                Scribe_Values.Look(ref AdvanceMode, "AdvanceMode", false);
                Scribe_Values.Look(ref HideNoFight, "HideNoFight", false);
                Scribe_Values.Look(ref HideInDoor, "HideInDoor", false);
                Scribe_Values.Look(ref HideInBed, "HideInBed", false);
                Look(ref size, "size", 2);
                Look(ref SouthOffset, "SouthOffset", 2);
                Look(ref NorthOffset, "NorthOffset", 2);
                Look(ref EastOffset, "EastOffset", 2);
                Look(ref WestOffset, "WestOffset", 2);
                Look(ref SouthRotation, "SouthRotation", 0);
                Look(ref NorthRotation, "NorthRotation", 0);
                Look(ref EastRotation, "EastRotation", 0);
                Look(ref WestRotation, "WestRotation", 0);
                Look(ref LayerOffset, "LayerOffset", 3);
                void Look<T>(ref T values, string label, int keepCount = 0, T defaultValue = default, bool forceSave = false)
                {
                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        if (!forceSave && (values != null || defaultValue == null) && (values == null || values.Equals(defaultValue)))
                        {
                            return;
                        }
                        if (values == null)
                        {
                            if (Scribe.EnterNode(label))
                            {
                                try
                                {
                                    Scribe.saver.WriteAttribute("IsNull", "True");
                                }
                                finally
                                {
                                    Scribe.ExitNode();
                                }
                            }
                        }
                        else
                        {
                            string keepCountStr = keepCount.ToString();
                            if (values is Vector2 vector2)
                            {
                                string format1 = "({0:F" + keepCountStr + "}, {1:F" + keepCountStr + "})";
                                string a = string.Format(format1, new object[2] { vector2.x, vector2.y });
                                Scribe.saver.WriteElement(label, a);
                            }
                            if (values is float float0)
                            {
                                string format1 = "{0:F" + keepCountStr + "}";
                                string a = string.Format(format1, new object[1] { float0 });
                                Scribe.saver.WriteElement(label, a);
                            }
                        }
                    }
                    else if (Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        values = ScribeExtractor.ValueFromNode(Scribe.loader.curXmlParent[label], defaultValue);
                    }
                }
            }

            public bool valueChange()
            {
                return AdvanceMode || size != Vector2.one || NorthOffset != Vector2.zero || SouthOffset != Vector2.zero || EastOffset != Vector2.zero
                    || WestOffset != Vector2.zero || NorthRotation != 0 || SouthRotation != 0 || EastRotation != 0 || WestRotation != 0
                    || LayerOffset != 0 || NoGraphic || HideInBed || HideInDoor || HideNoFight || NoHair != DefaultNoHair || NoBeard != DefaultNoBeard;
            }

            public Vector3 getOffset(Rot4 headFace)
            {
                Vector2 offset = Vector2.zero;
                if (headFace == Rot4.North)
                {
                    offset = NorthOffset;
                }
                else
                if (headFace == Rot4.South)
                {
                    offset = SouthOffset;
                }
                else
                if (headFace == Rot4.East)
                {
                    offset = EastOffset;
                }
                else
                if (headFace == Rot4.West)
                {
                    offset = WestOffset;
                }
                return new Vector3(offset.x, LayerOffset, offset.y);
            }
            public float getRotation(Rot4 headFace)
            {
                if (headFace == Rot4.North)
                {
                    return NorthRotation;
                }
                else
                if (headFace == Rot4.South)
                {
                    return SouthRotation;
                }
                else
                if (headFace == Rot4.East)
                {
                    return EastRotation;
                }
                else
                if (headFace == Rot4.West)
                {
                    return WestRotation;
                }
                else
                    return 0;
            }

        }
    }
    [StaticConstructorOnStartup]
    public static class HATweakerCache
    {
        public static List<ThingDef> HeadApparel = new List<ThingDef>();
        public static Dictionary<string, DrawData> drawDataCache = new Dictionary<string, DrawData>();
        internal static RenderTexture texture = null;

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
            HATweakerSetting.InitSetting();
            if (!HeadApparel.NullOrEmpty())
            {
                HATweakerSetting.InitSetting();
            }
        }
        public static List<ThingDef> GetAllOverHead()
        {
            return DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && x.apparel.LastLayer != null && Layers.Contains(x.apparel.LastLayer.defName)).ToList();
        }

    }
    public static class HATweakerUtility
    {
        internal static void DrawPawnCache(Pawn pawn, Vector2 size, Rot4 direction, out RenderTexture texture)
        {
            if (pawn != null)
            {
                if (pawn.apparel != null)
                {
                    pawn.apparel.Notify_ApparelChanged();
                }
                RenderTexture rt = PortraitsCache.Get(pawn, size, direction);
                texture = rt;
                //Log.Warning(texture.depth.ToStringSafe());
                return;
            }
            texture = null;
        }
        internal static Apparel NewApparel(string defName)
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
    }
    public static class HarmonyPatchA5
    {
        public static float rotate = 0;
        static Type This = typeof(HarmonyPatchA5);
        static Type renderTree = typeof(PawnRenderTree);
        internal static void PatchAllByHAT(Harmony harmony)
        {
            MethodInfo SetupApparelNodes = AccessTools.Method(renderTree, "SetupApparelNodes");
            if (SetupApparelNodes != null)
            {
                harmony.Patch(SetupApparelNodes, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranSetupApparelNodes)));
            }
            MethodInfo draw = AccessTools.Method(renderTree, "ProcessApparel");
            if (draw != null)
            {
                harmony.Patch(draw, prefix: new HarmonyMethod(This, nameof(HarmonyPatchA5.PreProcessApparel)), transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranProcessApparel)));
            }
            MethodInfo getMat = AccessTools.Method(renderTree, nameof(PawnRenderTree.TryGetMatrix));
            if (getMat != null)
            {
                harmony.Patch(getMat, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranTryGetMatrix)));
            }
            MethodInfo adjustParms = AccessTools.Method(renderTree, "AdjustParms");
            if (adjustParms != null)
            {
                harmony.Patch(adjustParms, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranAdjustParms)));
            }
            //Hide Not Drafted;
            MethodInfo setDraft = AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted));
            if (setDraft != null)
            {
                harmony.Patch(setDraft, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranSetDrafted)));
            }

            //Hide Under Roof;
            MethodInfo setPosition = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            if (setPosition != null)
            {
                harmony.Patch(setPosition, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(HarmonyPatchA5.TranSetPosition)));
            }

        }

        public static IEnumerable<CodeInstruction> TranSetupApparelNodes(IEnumerable<CodeInstruction> codes, ILGenerator generator)
        {
            Label a = generator.DefineLabel();
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i > 2 && code.opcode == OpCodes.Stloc_2 && list[i - 1].opcode == OpCodes.Ldloc_3)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(ShowChoose)));
                    yield return new CodeInstruction(OpCodes.Brfalse, a);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(GetApparel_0)));
                    //yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(HATweakerMod), nameof(HATweakerMod.apparel)));
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(renderTree, "ProcessApparel"));
                }
                else if (i > 3 && code.opcode == OpCodes.Ldarg_0 && list[i - 1].opcode == OpCodes.Stloc_2 && list[i - 2].opcode == OpCodes.Ldloc_3)
                {
                    if (code.labels.NullOrEmpty())
                    {
                        code.labels = new List<Label>()
                        {
                           a
                        };
                    }
                    else
                    {
                        code.labels.Add(a);
                    }
                    yield return code;
                }
                else
                {
                    yield return code;
                }
            }
        }
        private static Apparel GetApparel_0()
        {
            return HATweakerMod.apparel;
        }
        private static bool ShowChoose(PawnRenderTree tree)
        {
            bool a = false;
            if (HATweakerMod.pawn != null && HATweakerMod.pawn == tree.pawn)
            {
                bool b = HATweakerMod.InGameSetting;
                a = HATweakerMod.apparel != null && b;
                if (b)
                {
                    HATweakerMod.InGameSetting = false;
                }
            }
            return a;
        }
        public static bool PreProcessApparel(Apparel ap, PawnRenderNode headApparelNode, PawnRenderNode bodyApparelNode, PawnRenderTree __instance)
        {
            Pawn pawn = __instance.pawn;
            if (HATweakerMod.pawn != null && HATweakerMod.pawn == pawn && !HATweakerCache.HeadApparel.NullOrEmpty() && HATweakerCache.HeadApparel.Contains(ap.def) && ap != HATweakerMod.apparel)
            {
                return false;
            }
            if ((!HATweakerSetting.WorkOnColonist||pawn.IsColonist)&&HATweakerSetting.WorkOnColonist && HATweakerSetting.SettingData.TryGetValue(ap.def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (data.NoGraphic)
                {
                    return false;
                }
                else
                if (pawn.InBed() && data.HideInBed)
                {
                    return false;
                }
                else
                {
                    IntVec3 position = pawn.Position;
                    if (data.HideInDoor && pawn.Map != null && pawn.Position != null && !pawn.Position.UsesOutdoorTemperature(pawn.Map))
                    {
                        return false;
                    }
                    else
                    if (!pawn.Drafted && data.HideNoFight)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public static IEnumerable<CodeInstruction> TranAdjustParms(IEnumerable<CodeInstruction> codes)
        {
            FieldInfo info0 = typeof(Apparel).GetField("def");
            FieldInfo info1 = typeof(ThingDef).GetField("apparel");
            FieldInfo info2 = typeof(ApparelProperties).GetField("renderSkipFlags");
            MethodInfo method = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i > 6 && code.opcode == OpCodes.Ldfld && code.OperandIs(info2) && list[i - 1].opcode == OpCodes.Ldfld && list[i - 1].OperandIs(info1))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldfld, info0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderTree).GetField("pawn"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetDispalyFlags)));
                }
                else if (code.Is(OpCodes.Callvirt, method))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, typeof(PawnRenderTree).GetField("pawn"));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(This, nameof(GetApparel_1)));
                }
                else
                {
                    yield return code;
                }
            }
        }
        private static List<Apparel> GetApparel_1(List<Apparel> origin, Pawn pawn)
        {
            if (pawn != null && HATweakerMod.pawn != null && HATweakerMod.apparel != null)
            {
                if (pawn == HATweakerMod.pawn)
                {
                    List<Apparel> a;
                    if (!origin.NullOrEmpty())
                    {
                        a = new List<Apparel>(origin);
                    }
                    else
                    {
                        a = new List<Apparel>();
                    }
                    a.RemoveAll(x => HATweakerCache.HeadApparel.Contains(x.def));
                    a.Add(HATweakerMod.apparel);
                    return a;
                }
            }
            return origin;
        }

        public static List<RenderSkipFlagDef> SetDispalyFlags(List<RenderSkipFlagDef> origin, ThingDef def, Pawn pawn)
        {
            if ((!HATweakerSetting.WorkOnColonist || pawn.IsColonist) &&
               HATweakerSetting.SettingData.TryGetValue(def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (data.DefaultNoHair == data.NoHair && data.DefaultNoBeard == data.NoBeard)
                {
                    return origin;
                }
                else
                {
                    List<RenderSkipFlagDef> list;
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
            }
            else
            {
                return origin;
            }
        }

        public static IEnumerable<CodeInstruction> TranTryGetMatrix(IEnumerable<CodeInstruction> codes)
        {

            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (code.opcode == OpCodes.Callvirt && code.OperandIs(AccessTools.Method(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 5);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetRotateAndLoc)));
                }
                else
                {
                    yield return code;
                }
            }
        }
        public static void SetRotateAndLoc(PawnRenderNode node, PawnDrawParms parms, ref Vector3 vec, ref Quaternion quat)
        {
            if ((!HATweakerSetting.WorkOnColonist || parms.pawn.IsColonist) &&
                (node.Props.workerClass == typeof(PawnRenderNodeWorker_Apparel_Head) && node.children.NullOrEmpty()
            && HATweakerSetting.SettingData.TryGetValue(node.apparel != null ? node.apparel.def.defName : node.Props.debugLabel, out HATweakerSetting.HATSettingData data)))
            {
                if (data.AdvanceMode)
                {
                    Quaternion a = new Quaternion()
                    {
                        eulerAngles = quat.eulerAngles,
                        x = quat.x,
                        y = quat.y,
                        z = quat.z,
                        w = quat.w
                    };
                    Vector3 b = new Vector3(a.eulerAngles.x, a.eulerAngles.y + data.getRotation(parms.facing), a.eulerAngles.z);
                    a.eulerAngles = b;
                    quat = a;
                    vec += data.getOffset(parms.facing);
                }
            }

        }

        public static IEnumerable<CodeInstruction> TranProcessApparel(IEnumerable<CodeInstruction> codes)
        {
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i > 2 && i < list.Count - 2 && code.opcode == OpCodes.Stloc_S && list[i - 1].opcode == OpCodes.Stfld && list[i + 1].opcode == OpCodes.Br)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(renderTree, nameof(PawnRenderTree.pawn)));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(This, nameof(SetHeadClothesProps)));
                    yield return code;
                }
                else
                {
                    yield return code;
                }
            }
        }

        public static PawnRenderNodeProperties SetHeadClothesProps(PawnRenderNodeProperties properties, Thing cloth, Pawn pawn)
        {
            if ((!HATweakerSetting.WorkOnColonist || pawn.IsColonist) && HATweakerSetting.SettingData.TryGetValue(cloth.def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (data.AdvanceMode)
                {
                    properties.drawSize.x *= data.size.x;
                    properties.drawSize.y *= data.size.y;
                }
            }
            return properties;
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
            if (HATweakerSetting.WorkOnColonist && !pawn.IsColonist)
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
                if (HATweakerSetting.WorkOnColonist && !pawn.IsColonist)
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

        }

        public class HarmonyPatchAlienRace
        {

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