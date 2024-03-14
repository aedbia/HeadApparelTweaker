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
        private static int HATweakerModIndex = -1;
        private string choose = "";
        private string search = "";
        private bool BarChange = false;
        private int ChangeBarInt = 0;
        private int HatStartIndex = 0;
        private float IndexCount = 0;
        private Vector2 loc = Vector2.zero;
        private bool InGame = false;
        private static Rot4 direction = Rot4.South;
        internal static string PawnName = "";
        internal static Pawn pawn = null;
        internal static Apparel apparel = null;
        internal static ApparelGraphicRecord? apparelGraphicRecord = null;

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
                            bool flag0 = !data.renderSkipFlagDefs.NullOrEmpty() && data.renderSkipFlagDefs.Contains(RenderSkipFlagDefOf.Beard);
                            if (Widgets.RadioButtonLabeled(rect2.BottomHalf(), "No_Beard".Translate(), flag0))
                            {
                                if (flag0)
                                {
                                    data.renderSkipFlagDefs.Remove(RenderSkipFlagDefOf.Beard);
                                }
                                else
                                {
                                    if (data.renderSkipFlagDefs == null)
                                    {
                                        data.renderSkipFlagDefs = new List<RenderSkipFlagDef>();
                                    }
                                    data.renderSkipFlagDefs.Add(RenderSkipFlagDefOf.Beard);
                                }
                            }
                            rect2.x += (rt3.width - 0.85f * rt3.height) / 5;
                            if (Mouse.IsOver(rect2.TopHalf()))
                            {
                                Widgets.DrawHighlight(rect2.TopHalf());
                            }
                            bool flag1 = !data.renderSkipFlagDefs.NullOrEmpty() && data.renderSkipFlagDefs.Contains(RenderSkipFlagDefOf.Hair);
                            if (Widgets.RadioButtonLabeled(rect2.TopHalf(), "No_Hair".Translate(), flag1))
                            {
                                if (flag1)
                                {
                                    data.renderSkipFlagDefs.Remove(RenderSkipFlagDefOf.Hair);
                                }
                                else
                                {
                                    if (data.renderSkipFlagDefs == null)
                                    {
                                        data.renderSkipFlagDefs = new List<RenderSkipFlagDef>();
                                    }
                                    data.renderSkipFlagDefs.Add(RenderSkipFlagDefOf.Hair);
                                }
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
                            if (Widgets.RadioButtonLabeled(rect2.BottomHalf(), "Hide_No_Fight".Translate(), data.HideInBed))
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
                HATweakerSetting.SingleInit(def);
                Rect main = rect0.RightPart(0.69f);
                Widgets.DrawWindowBackground(main);
                HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[choose];
                Rect main1 = new Rect(main.x + 5f, main.y + 5f, main.width / 2 - 10f, LabelHeigh);
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
                    bool flag0 = !data.renderSkipFlagDefs.NullOrEmpty() && data.renderSkipFlagDefs.Contains(RenderSkipFlagDefOf.Hair);
                    if (Widgets.RadioButtonLabeled(main1, "No_Hair".Translate(), flag0))
                    {
                        if (flag0)
                        {
                            data.renderSkipFlagDefs.Remove(RenderSkipFlagDefOf.Hair);
                        }
                        else
                        {
                            if (data.renderSkipFlagDefs == null)
                            {
                                data.renderSkipFlagDefs = new List<RenderSkipFlagDef>();
                            }
                            data.renderSkipFlagDefs.Add(RenderSkipFlagDefOf.Hair);
                        }
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    bool flag1 = !data.renderSkipFlagDefs.NullOrEmpty() && data.renderSkipFlagDefs.Contains(RenderSkipFlagDefOf.Beard);
                    if (Widgets.RadioButtonLabeled(main1, "No_Beard".Translate(), flag1))
                    {
                        if (flag1)
                        {
                            data.renderSkipFlagDefs.Remove(RenderSkipFlagDefOf.Beard);
                        }
                        else
                        {
                            if (data.renderSkipFlagDefs == null)
                            {
                                data.renderSkipFlagDefs = new List<RenderSkipFlagDef>();
                            }
                            data.renderSkipFlagDefs.Add(RenderSkipFlagDefOf.Beard);
                        }
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_Indoor".Translate(), data.HideInDoor))
                    {
                        data.NoGraphic = !data.NoGraphic;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_No_Fight".Translate(), data.HideNoFight))
                    {
                        data.NoGraphic = !data.NoGraphic;
                    }
                    main1.y += LabelHeigh;
                    if (Mouse.IsOver(main1))
                    {
                        Widgets.DrawHighlight(main1);
                    }
                    if (Widgets.RadioButtonLabeled(main1, "Hide_In_Bed".Translate(), data.HideInBed))
                    {
                        data.NoGraphic = !data.NoGraphic;
                    }

                    main1.y += (LabelHeigh + 10f);
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
                    LabelHeigh += 3f;
                    Widgets.CheckboxLabeled(main1, "Advance_Mode".Translate(), ref data.AdvanceMode);
                    if (data.AdvanceMode)
                    {
                        main1.y += LabelHeigh;
                        Widgets.Label(main1.LeftPart(0.7f), "Size".Translate() + ":" + data.size.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.size = Vector2.one;
                        }
                        main1.y += (LabelHeigh);
                        data.size.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.size.x, 0.5f, 2f);
                        data.size.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.size.y, 0.5f, 2f);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "South".Translate() + ":" + data.SouthOffset.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.SouthOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh);
                        data.SouthOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.SouthOffset.x, -1, 1);
                        data.SouthOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.SouthOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "North".Translate() + ":" + data.NorthOffset.ToString("F2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.NorthOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh);
                        data.NorthOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.NorthOffset.x, -1, 1);
                        data.NorthOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.NorthOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "West".Translate() + ":" + data.WestOffset.ToString("f2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.WestOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh);
                        data.WestOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.WestOffset.x, -1, 1);
                        data.WestOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.WestOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "East".Translate() + ":" + data.EastOffset.ToString("f2"));
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.EastOffset = Vector2.zero;
                        }
                        main1.y += (LabelHeigh);
                        data.EastOffset.x =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.EastOffset.x, -1, 1);
                        data.EastOffset.y =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.EastOffset.y, -1, 1);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "Rotate".Translate() + ":" + "South".Translate() + "[" + data.SouthRotation.ToString("0") + "]" + "North".Translate() + "[" + data.NorthRotation.ToString("0") + "]");
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.SouthRotation = 0f;
                            data.NorthRotation = 0f;
                        }
                        main1.y += (LabelHeigh);
                        data.SouthRotation =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.SouthRotation, -180, 180);
                        data.NorthRotation =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.NorthRotation, -180, 180);
                        main1.y += (LabelHeigh - 5f);
                        Widgets.Label(main1.LeftPart(0.7f), "Rotate".Translate() + ":" + "East".Translate() + "[" + data.EastRotation.ToString("0") + "]" + "West".Translate() + "[" + data.WestRotation.ToString("0") + "]");
                        if (Widgets.ButtonText(main1.RightPart(0.3f), "Reset".Translate()))
                        {
                            data.EastRotation = 0f;
                            data.WestRotation = 0f;
                        }
                        main1.y += (LabelHeigh);
                        data.EastRotation =
                        Widgets.HorizontalSlider(main1.LeftHalf(), data.EastRotation, -180, 180);
                        data.WestRotation =
                        Widgets.HorizontalSlider(main1.RightHalf(), data.WestRotation, -180, 180);
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
                if (!choose.NullOrEmpty() && pawn != null && InGame)
                {
                    if (apparel != null)
                    {
                        if (apparel.def.defName != choose)
                        {
                            apparel = HATweakerUtility.NewApparel(choose);
                        }
                        if (apparelGraphicRecord == null || ((ApparelGraphicRecord)apparelGraphicRecord).sourceApparel.def.defName != choose)
                        {
                            ApparelGraphicRecordGetter.TryGetGraphicApparel(apparel, pawn.story.bodyType, out ApparelGraphicRecord a);
                            apparelGraphicRecord = a;
                        }
                    }
                    else
                    {
                        apparel = HATweakerUtility.NewApparel(choose);
                    }
                }
                if (pawn != null && InGame)
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
                if (data.AdvanceMode)
                {
                    Rect two = main4.RightPart(0.66f).LeftHalf();
                    if (Mouse.IsOver(two))
                    {
                        TooltipHandler.TipRegion(two, "Reset_Change_Tooltip".Translate());
                    }
                    if (Widgets.ButtonText(two, "Reset_Change".Translate()))
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
                        if (!HATweakerSetting.SettingData.NullOrEmpty() && HATweakerSetting.SettingData.ContainsKey(def.defName))
                        {
                            HATweakerSetting.SettingData.Remove(def.defName);
                        }
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
            /*else
            if (ChangeBar == 2)
            {
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
            }
            */
            if (BarChange)
            {
                pawn = null;
                PawnName = null;
                HATweakerCache.texture = null;
                BarChange = false;
            }
        }
        public override string SettingsCategory()
        {
            return base.Content.Name.Translate();
        }
    }

    public class HATweakerSetting : ModSettings
    {
        public static Dictionary<string, HATSettingData> SettingData = new Dictionary<string, HATSettingData>();
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref SettingData, "SettingData", valueLookMode: LookMode.Deep);
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

            if (SettingData.TryGetValue(def.defName, out HATSettingData data))
            {
                if (data == null)
                {
                    SettingData.SetOrAdd(def.defName, new HATSettingData()
                    {
                        renderSkipFlagDefs = def.apparel.renderSkipFlags
                    });
                }
            }
            else
            {
                SettingData.SetOrAdd(def.defName, new HATSettingData()
                {
                    renderSkipFlagDefs = def.apparel.renderSkipFlags
                });
            }
        }
        public class HATSettingData : IExposable
        {
            public List<RenderSkipFlagDef> renderSkipFlagDefs = new List<RenderSkipFlagDef>();
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
            public bool HideInDoor = false;
            public bool HideNoFight = false;
            public bool HideInBed = false;
            public bool AdvanceMode = false;

            public void ExposeData()
            {
                Scribe_Values.Look(ref NoGraphic, "NoGraphic");
                Scribe_Collections.Look(ref renderSkipFlagDefs, "renderSkipFlagDefs", LookMode.Def);
                if (renderSkipFlagDefs == null)
                {
                    renderSkipFlagDefs = new List<RenderSkipFlagDef>();
                }
                Scribe_Values.Look(ref AdvanceMode, "AdvanceMode");
                Look(ref SouthOffset, "SouthOffset", 2);
                Look(ref NorthOffset, "NorthOffset", 2);
                Look(ref EastOffset, "EastOffset", 2);
                Look(ref WestOffset, "WestOffset", 2);
                Look(ref SouthRotation, "SouthRotation", 0);
                Look(ref NorthRotation, "NorthRotation", 0);
                Look(ref EastRotation, "EastRotation", 0);
                Look(ref WestRotation, "WestRotation", 0);
                Look(ref LayerOffset, "LayerOffset", 2);
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
                return NorthOffset != Vector2.zero || SouthOffset != Vector2.zero || EastOffset != Vector2.zero
                    || WestOffset != Vector2.zero || NorthRotation != 0 || SouthRotation != 0 || EastRotation != 0 || WestRotation != 0;
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
                return new Vector3(offset.x, 0, offset.y);
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
                RenderTexture rt = PortraitsCache.Get(pawn, size, direction, renderClothes: false);
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
            MethodInfo draw = AccessTools.Method(renderTree, "ProcessApparel");
            if (draw != null)
            {
                harmony.Patch(draw, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranProcessApparel)));
            }
            MethodInfo getMat = AccessTools.Method(renderTree, nameof(PawnRenderTree.TryGetMatrix));
            if (getMat != null)
            {
                harmony.Patch(getMat, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranTryGetMatrix)));
            }

        }

        private static IEnumerable<CodeInstruction> TranTryGetMatrix(IEnumerable<CodeInstruction> codes)
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
            if (parms.pawn.IsColonist &&
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
            if (pawn.IsColonist && HATweakerSetting.SettingData.TryGetValue(cloth.def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (data.AdvanceMode)
                {
                    properties.drawSize.x *= data.size.x;
                    properties.drawSize.y *= data.size.y;
                }
            }
            return properties;
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