using ABEasyLib;
using ABEasyLib.ABExtensions;
using Fashion_Wardrobe;
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
using static HeadApparelTweaker.HATSettingContents;
using static Verse.DrawData;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        private static int HATweakerModIndex = -1;
        internal static int FWModIndex = -1;
        public static int AlienIndex = -1;
        internal static string choose = "";
        internal static string search = "";
        private bool BarChange = false;
        private int ChangeBarInt = 0;
        private float IndexCount = 0;
        private Vector2 loc = Vector2.zero;
        private static Rot4 direction = Rot4.South;
        internal static string PawnName = "";
        internal static Pawn pawn = null;
        internal static Apparel apparel = null;
        private Vector2 position0 = Vector2.zero;
        private float height0 = 0;
        internal static bool InGameSetting = false;
        private static HATweakerSetting.HATSettingData copyData = new HATweakerSetting.HATSettingData();
        private static bool copy;
        private static Color color;
        //private static readonly Texture2D NULL = ContentFinder<Texture2D>.Get("UI/Overlays/QuestionMark", true);

        private int ChangeBar
        {
            get { return ChangeBarInt; }
            set
            {
                if (value != ChangeBarInt)
                {
                    BarChange = true;
                    ChangeBarInt = value;
                }
                ;
            }
        }

        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            color = new ColorInt(38, 43, 43).ToColor;
            HATweakerModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            FWModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "aedbia.fashionwardrobe");
            AlienIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces");
            Harmony harmony = new Harmony(this.Content.PackageIdPlayerFacing);
            HarmonyPatchA5.PatchAllByHAT(harmony);
            if (AlienIndex != -1)
            {
                new HarmonyPatchA5.HarmonyPatchAlienRace(harmony);
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            List<ThingDef> list = HATweakerCache.HeadApparel;
            if (list.NullOrEmpty())
            {
                return;
            }

            if (choose.NullOrEmpty())
            {
                choose = list.First().defName;
            }

            // Cache the Rect calculations
            Rect searchRect = inRect.TopPart(0.04f).LeftPart(0.3f);
            Rect selectionGridRect = inRect.TopPart(0.04f).RightPart(0.69f);

            // Search Tool
            List<string> ob = new List<string> { "Basic_Settings".Translate(), "Advanced_Settings".Translate(), "Global_Settings".Translate() };
            int obCount = 3;
            if (AlienIndex != -1)
            {
                ob.Add("Alien_Patch_Settings".Translate());
                obCount = 4;
            }

            // Cache GUIContent and GUIStyle
            if (guiB == null || guiB.Length != ob.Count)
            {
                guiB = new GUIContent[ob.Count];
                for (int i = 0; i < ob.Count; i++)
                {
                    guiB[i] = new GUIContent(ob[i]);
                }
            }

            if (guiA == null)
            {
                guiA = new GUIStyle(GUI.skin.window);
                guiA.padding.bottom = -10;
            }

            search = Widgets.TextArea(searchRect, search);
            ChangeBar = GUI.SelectionGrid(selectionGridRect, ChangeBar, guiB, obCount, guiA);

            // Initialized ScrollView Data
            switch (ChangeBar)
            {
                case 0:
                    DrawBasicSettings(list, inRect);
                    break;
                case 1:
                    DrawAdvanceSettings(list, inRect);
                    break;
                case 2:
                    DrawGlobalSettings(list, inRect);
                    break;
                case 3:
                    DrawPatchSettings(inRect);
                    break;
            }

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
        }

        private GUIContent[] guiB;
        private GUIStyle guiA;
        private Vector2 BaScrLoc = Vector2.zero;
        private const float unitH = 120f;
        private List<ScrollViewContent> basicSettingUnits = new List<ScrollViewContent>();
        private void DrawBasicSettings(List<ThingDef> list, Rect inRect)
        {
            if (list.NullOrEmpty())
            {
                return;
            }
            Rect rect0 = inRect.BottomPart(0.95f);
            Widgets.DrawWindowBackground(rect0);

            if (basicSettingUnits == null)
            {
                basicSettingUnits = new List<ScrollViewContent>();
            }
            if (basicSettingUnits.Count == 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef def = list[i];
                    basicSettingUnits.Add(new BasicSettingUnit(rect0.width, unitH, def.defName, def));
                }
            }
            ABWidgetsExtensions.DrawScrollPanel(rect0.ContractedBy(3f), basicSettingUnits, ref BaScrLoc);
        }

        private string chooseStyle = "";
        private void DrawAdvanceSettings(List<ThingDef> list, Rect inRect)
        {
            bool adjustStyles = ModsConfig.IdeologyActive;
            float LabelHeigh = 30f;
            string useDefault = "Use_Default0".Translate();
            string useDefault1 = "Use_Default1".Translate();
            Rect rect0 = inRect.BottomPart(0.95f);
            InGameSetting = Find.CurrentMap != null && Find.CurrentMap.mapPawns != null && Find.CurrentMap.mapPawns.ColonistsSpawnedCount > 0;
            Rect outRect = rect0.LeftPart(0.3f);
            Widgets.DrawWindowBackground(outRect);
            Rect viewRect = new Rect(-3f, -3f, outRect.width - 26f, (LabelHeigh + 5f) * IndexCount + 3f);
            Rect rect1 = new Rect(LabelHeigh + 5f, 0f, outRect.width - 60f, LabelHeigh);
            Rect rect2 = new Rect(0f, 0f, LabelHeigh, LabelHeigh);
            Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
            GUIStyle labelStyle = HATweakerUtility.GetLabelStyle(TextAnchor.MiddleCenter);
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
                        chooseStyle = "";
                        if (apparel != null && apparel.def == c)
                        {
                            apparel.SetStyleDef(null);
                        }
                    }
                    Widgets.DrawBox(rect2);
                    if (c.uiIcon != null)
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
            if (adjustStyles && def.CanBeStyled() && !data.ChildrenData.NullOrEmpty())
            {
                List<ThingStyleDef> styles = HATweakerUtility.GetStyles(def);
                if (!styles.NullOrEmpty())
                {


                    Widgets.DrawBoxSolid(main1, color);
                    GUI.Label(main1, "Style".Translate(), labelStyle);
                    main1.y += LabelHeigh;
                    Widgets.CheckboxLabeled(main1, useDefault, ref data.UseDefault);
                    main1.y += LabelHeigh + 10f;
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);

                    Widgets.DrawHighlightIfMouseover(main1);
                    if (chooseStyle.NullOrEmpty())
                    {
                        Widgets.DrawHighlightSelected(main1);
                    }
                    labelStyle.alignment = TextAnchor.MiddleRight;
                    Rect iconL = new Rect(main1.x, main1.y, main1.height, main1.height);
                    Rect labelR = new Rect(main1.x + main1.height, main1.y, main1.width - main1.height, main1.height);
                    GUI.DrawTexture(iconL, def.uiIcon);
                    GUI.Label(labelR, "Default".Translate(), labelStyle);
                    if (Widgets.ButtonInvisible(main1))
                    {
                        chooseStyle = "";
                        apparel.SetStyleDef(null);
                    }
                    main1.y += LabelHeigh;
                    iconL.y += LabelHeigh;
                    labelR.y += LabelHeigh;

                    for (int i = 0; i < styles.Count; i++)
                    {
                        ThingStyleDef styleDef = styles[i];
                        Widgets.DrawHighlightIfMouseover(main1);
                        if (chooseStyle == styleDef.defName)
                        {
                            Widgets.DrawHighlightSelected(main1);
                        }
                        GUI.DrawTexture(iconL, styleDef.UIIcon);
                        string a = styleDef.label.NullOrEmpty() ? styleDef.defName : styleDef.label;
                        GUI.Label(labelR, a, labelStyle);
                        if (Widgets.ButtonInvisible(main1))
                        {
                            chooseStyle = styleDef.defName;
                            if (apparel != null && apparel.def.defName == choose)
                            {
                                apparel.SetStyleDef(styleDef);
                            }
                        }
                        main1.y += LabelHeigh;
                        iconL.y += LabelHeigh;
                        labelR.y += LabelHeigh;
                    }
                    if (!chooseStyle.NullOrEmpty() && data.ChildrenData.TryGetValue(chooseStyle, out HATweakerSetting.HATSettingData data1))
                    {
                        data = data1;
                    }
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    main1.y += 10f;
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
                }
            }
            Widgets.DrawBoxSolid(main1, color);
            GUI.Label(main1, "Basic_Settings".Translate(), labelStyle);
            main1.y += LabelHeigh;
            if (adjustStyles && !chooseStyle.NullOrEmpty())
            {
                Widgets.CheckboxLabeled(main1, useDefault1, ref data.UseDefault);
                main1.y += LabelHeigh + 10f;
                Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
            }
            LabelHeigh -= 3f;
            main1.height -= 3f;
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
                main1.height += 3f;
                Widgets.DrawBoxSolid(main1, color);
                Widgets.CheckboxLabeled(main1, "Advance_Mode".Translate(), ref data.AdvanceMode);
                if (data.AdvanceMode)
                {
                    main1.y += (main1.height + 2f);
                    Rect copyLoc = new Rect(main1.x, main1.y, main1.height, main1.height);
                    if (Widgets.ButtonImage(copyLoc, TexButton.Copy))
                    {
                        if (copyData == null)
                        {
                            copyData = new HATweakerSetting.HATSettingData();
                        }
                        copyData.NorthOffset = data.NorthOffset;
                        copyData.EastOffset = data.EastOffset;
                        copyData.SouthOffset = data.SouthOffset;
                        copyData.WestOffset = data.WestOffset;
                        copyData.NorthRotation = data.NorthRotation;
                        copyData.EastRotation = data.EastRotation;
                        copyData.SouthRotation = data.SouthRotation;
                        copyData.WestRotation = data.WestRotation;
                        copyData.size = data.size;
                        copyData.LayerOffset = data.LayerOffset;
                        copy = true;
                    }
                    ;
                    copyLoc.x += copyLoc.width + 5f;
                    if (copy && Widgets.ButtonImage(copyLoc, TexButton.Paste))
                    {
                        data.NorthOffset = copyData.NorthOffset;
                        data.EastOffset = copyData.EastOffset;
                        data.SouthOffset = copyData.SouthOffset;
                        data.WestOffset = copyData.WestOffset;
                        data.NorthRotation = copyData.NorthRotation;
                        data.EastRotation = copyData.EastRotation;
                        data.SouthRotation = copyData.SouthRotation;
                        data.WestRotation = copyData.WestRotation;
                        data.size = copyData.size;
                        data.LayerOffset = copyData.LayerOffset;
                    }
                    ;
                    Rect rectAdj = new Rect(main1.x, main1.y + main1.height + 3f, main1.width, (main1.height + 10) * 3);
                    DrawAdjust(rectAdj, "Size".Translate() + ":" + data.size.ToString("F2"), ref data.size.x, ref data.size.y, 0.5f, 2, 0.01f, () => data.size = Vector2.one);
                    rectAdj.y += rectAdj.height + 2f;

                    if (DrawAdjust(rectAdj, "South".Translate() + ":" + data.SouthOffset.ToString("F2"), ref data.SouthOffset.x, ref data.SouthOffset.y, -1, 1, 0.01f, () => data.SouthOffset = Vector2.zero))
                    {
                        data.SetOffset(Rot4.South);
                    }
                    rectAdj.y += rectAdj.height + 2f;
                    if (DrawAdjust(rectAdj, "North".Translate() + ":" + data.NorthOffset.ToString("F2"), ref data.NorthOffset.x, ref data.NorthOffset.y, -1, 1, 0.01f, () => data.NorthOffset = Vector2.zero))
                    {
                        data.SetOffset(Rot4.North);
                    }
                    rectAdj.y += rectAdj.height + 2f;
                    if (DrawAdjust(rectAdj, "West".Translate() + ":" + data.WestOffset.ToString("F2"), ref data.WestOffset.x, ref data.WestOffset.y, -1, 1, 0.01f, () => data.WestOffset = Vector2.zero))
                    {
                        data.SetOffset(Rot4.West);
                    }
                    rectAdj.y += rectAdj.height + 2f;
                    if (DrawAdjust(rectAdj, "East".Translate() + ":" + data.EastOffset.ToString("F2"), ref data.EastOffset.x, ref data.EastOffset.y, -1, 1, 0.01f, () => data.EastOffset = Vector2.zero))
                    {
                        data.SetOffset(Rot4.East);
                    }
                    rectAdj.y += rectAdj.height + 2f;
                    if (DrawAdjust(rectAdj, "Rotate".Translate() + ":" + "South".Translate() + "[" + data.SouthRotation.ToString("0") + "]" + "North".Translate() + "[" + data.NorthRotation.ToString("0") + "]", ref data.SouthRotation, ref data.NorthRotation, -180, 180, 1, () =>
                    {
                        data.SouthRotation = 0f;
                        data.NorthRotation = 0f;
                    }))
                    {
                        data.SetRotation(Rot4.South);
                        data.SetRotation(Rot4.North);
                    }
                    rectAdj.y += rectAdj.height + 2f;
                    if(DrawAdjust(rectAdj, "Rotate".Translate() + ":" + "East".Translate() + "[" + data.EastRotation.ToString("0") + "]" + "West".Translate() + "[" + data.WestRotation.ToString("0") + "]", ref data.EastRotation, ref data.WestRotation, -180, 180, 1, () =>
                    {
                        data.EastRotation = 0f;
                        data.WestRotation = 0f;
                    }))
                    {
                        data.SetRotation(Rot4.East);
                        data.SetRotation(Rot4.West);
                    }
                    main1.height = rectAdj.height * 0.4f;
                    main1.y = rectAdj.y + rectAdj.height + 2f;
                    Widgets.Label(main1.LeftPart(0.7f), "Layer_Offset".Translate() + ":" + data.LayerOffset.ToString("f4"));
                    bool work = false;
                    if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                    {
                        data.LayerOffset = 0f;
                        work = true;
                    }
                    main1.height = rectAdj.height * 0.3f;
                    main1.y += (main1.height + 2f);
                    main1.width = main1.height;
                    if (Widgets.ButtonImage(main1, TexButton.Minus))
                    {
                        data.LayerOffset = data.LayerOffset > -0.03f ? data.LayerOffset - 0.0001f : -0.03f;
                        work = true;
                    }
                    ;
                    main1.x += main1.width;
                    main1.width = rectAdj.width - 2 * main1.height;
                    float layer = Widgets.HorizontalSlider(main1, data.LayerOffset, -0.03f, +0.03f);
                    if (layer != data.LayerOffset)
                    {
                        data.LayerOffset = layer;
                        work =true;
                       } 
                    main1.x += main1.width;
                    main1.width = main1.height;
                    if (Widgets.ButtonImage(main1, TexButton.Plus))
                    {
                        data.LayerOffset = data.LayerOffset < 0.03f ? data.LayerOffset + 0.0001f : 0.03f;
                        work = true;
                    }
                    if (work)
                    {
                        data.GetDrawData(true);
                    }
                }
            }
            Widgets.EndScrollView();
            LabelHeigh = 30f;
            height0 = main1.y + main1.height + 5f;
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
                if (apparel == null || apparel.def.defName != choose)
                {
                    apparel = HATweakerUtility.NewApparel(choose);
                }
            }
            if (pawn != null && InGameSetting)
            {
                if (apparel != null)
                {
                    pawn.apparel?.WornApparel?.Add(apparel);
                    HATweakerUtility.DrawPawnCache(pawn, new Vector2(main2.width, main2.height), direction, out HATweakerCache.texture);
                    pawn.apparel?.WornApparel?.Remove(apparel);
                }


                if (HATweakerCache.texture != null)
                {
                    GUI.DrawTexture(main3.BottomPart(0.95f), HATweakerCache.texture);
                }
            }
            else
            {
                GUI.Label(main3, "Into_Game".Translate(), labelStyle);
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
                data.GetDrawData(true);
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
            bool DrawAdjust(Rect rectAd, string label, ref float x, ref float y, float min, float max, float interval, Action action)
            {
                Rect rectLa = rectAd.TopPart(0.4f);
                Widgets.Label(rectLa.LeftPart(0.7f), label);
                bool work = false;
                if (Widgets.ButtonText(rectLa.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                {
                    if (action != null)
                    {
                        action();
                    }
                    work = true;

                }
                Rect rectXYL = rectAd.BottomPart(0.6f).TopHalf();
                Rect minus = new Rect(rectXYL.x, rectXYL.y, rectXYL.height, rectXYL.height);
                rectXYL.x += rectXYL.height;
                rectXYL.width -= 2 * rectXYL.height;
                if (Widgets.ButtonImage(minus, TexButton.Minus))
                {
                    x = x > min ? x - interval : min;
                    work = true;
                }
                ;
                float x0 = Widgets.HorizontalSlider(rectXYL, x, min, max);
                if (x != x0)
                {
                    x = x0;
                    work = true;
                }
                minus.x += (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Plus))
                {
                    x = x < max ? x + interval : max;
                    work = true;
                }
                ;
                rectXYL.y += rectXYL.height;
                minus.y += rectXYL.height;
                minus.x -= (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Minus))
                {
                    y = y > min ? y - interval : min;
                    work = true;
                }
                ;
                float y0 = Widgets.HorizontalSlider(rectXYL.BottomHalf(), y, min, max);
                if (y != y0)
                {
                    y = y0;
                    work = true;
                }
                minus.x += (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Plus))
                {
                    y = y < max ? y + interval : max;
                    work = true;
                }
                ;
                Widgets.DrawLineHorizontal(rectAd.x + 5f, rectAd.y + rectAd.height, rectAd.width - 5f, Color.gray);
                return work;
            }
        }

        private void DrawGlobalSettings(List<ThingDef> list, Rect inRect)
        {
            Rect rect0 = inRect.BottomPart(0.95f);
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
                QuickSetting(3, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideNoFight".Translate()))
            {
                QuickSetting(4, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideNoFight".Translate()))
            {
                QuickSetting(4, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), "Quick_Open_HideInBed".Translate()))
            {
                QuickSetting(5, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), "Quick_Close_HideInBed".Translate()))
            {
                QuickSetting(5, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one, "Reset_All_Setting".Translate()))
            {
                HATweakerSetting.SettingData.Clear();
                HATweakerSetting.InitSetting();
            }
            one.y += one.height + 5f;
            Widgets.CheckboxLabeled(one, "Only_Colonist".Translate(), ref HATweakerSetting.WorkOnColonist);
            one.y += one.height + 5f;
            Widgets.CheckboxLabeled(one, "useIsColonistCache".Translate(), ref HATweakerSetting.useIsColonistCache);
            void QuickSetting(int a, bool on)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef def = list[i];
                    if (HATweakerSetting.SettingData.TryGetValue(def.defName, out HATweakerSetting.HATSettingData data))
                    {
                        if (a == 0)
                        {
                            data.NoGraphic = on;
                        }
                        else
                        {
                            data.NoGraphic = false;
                            switch (a)
                            {
                                case 1:
                                    data.NoHair = on;
                                    break;
                                case 2:
                                    data.NoBeard = on;
                                    break;
                                case 3:
                                    data.HideInDoor = on;
                                    break;
                                case 4:
                                    data.HideNoFight = on;
                                    break;
                                case 5:
                                    data.HideInBed = on;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void DrawPatchSettings(Rect inRect)
        {
            float LabelHeigh = 30f;
            Rect rect0 = inRect.BottomPart(0.95f);
            Dictionary<int, string> dict = HATweakerCache.alienCompatible.AllAddons;
            Widgets.DrawWindowBackground(rect0);
            float wi = (rect0.width - 36f) / 4;
            Rect rect00 = new Rect(rect0.x + 5f, rect0.y + 5f, wi - 5f, LabelHeigh);
            Widgets.DrawLineHorizontal(rect0.x, rect00.y + LabelHeigh, rect0.width, Color.grey);
            Widgets.Label(rect00, "Race".Translate());
            Widgets.DrawLineVertical(rect00.x + rect00.width, rect0.y, LabelHeigh + 5);
            rect00.x += wi;
            Widgets.Label(rect00, "Name".Translate());
            Widgets.DrawLineVertical(rect00.x + rect00.width, rect0.y, LabelHeigh + 5);
            rect00.x += wi;
            Widgets.Label(rect00, "Path".Translate());
            Widgets.DrawLineVertical(rect00.x + rect00.width, rect0.y, LabelHeigh + 5);
            rect00.x += wi;
            rect00.width = wi / 2 - 5f;
            Widgets.Label(rect00, "With_Hair".Translate());
            Widgets.DrawLineVertical(rect00.x + rect00.width, rect0.y, LabelHeigh + 5);
            rect00.x += wi / 2;
            Widgets.Label(rect00, "With_Beard".Translate());
            Widgets.DrawLineVertical(rect00.x + rect00.width, rect0.y, LabelHeigh + 5);
            Rect outRect = new Rect(rect0.x + 5f, rect0.y + LabelHeigh + 10f, rect0.width - 10f, rect0.height - LabelHeigh - 20f);
            Rect viewRect = new Rect(0, 0, outRect.width - 26f, (LabelHeigh + 5f) * IndexCount + 3f);
            Rect rect1 = new Rect(0f, 0f, viewRect.width / 4, LabelHeigh);
            Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
            int se = 0;
            //Draw ScrollView;

            if (dict.Count != 0)
            {
                foreach (int dictKey in dict.Keys)
                {
                    string disc = dict[dictKey];
                    if (disc.IndexOf(search) != -1)
                    {
                        se++;
                        /*if (Mouse.IsOver(rect1))
                        {
                            Widgets.DrawHighlight(rect1);
                        }
                        if (Widgets.RadioButtonLabeled(rect1, c.label, choose == c.defName))
                        {
                            choose = c.defName;
                        }*/
                        string[] strings = disc.Split('|');
                        for (int i = 0; i < 3; i++)
                        {
                            if (strings.Length > i)
                            {
                                Widgets.Label(rect1, strings[i]);
                                rect1.x += rect1.width;
                            }
                        }
                        if (Widgets.RadioButton(rect1.x + rect1.width / 4 - 17f, rect1.y, (!HATweakerSetting.WithHair.NullOrEmpty()) && HATweakerSetting.WithHair.Contains(disc)))
                        {
                            if ((!HATweakerSetting.WithHair.NullOrEmpty()) && HATweakerSetting.WithHair.Contains(disc))
                            {
                                HATweakerSetting.WithHair.Remove(disc);
                            }
                            else
                            {
                                if (HATweakerSetting.WithHair == null)
                                {
                                    HATweakerSetting.WithHair = new List<string>();
                                }
                                HATweakerSetting.WithHair.Add(disc);
                            }
                        }
                        if (Widgets.RadioButton(rect1.x + rect1.width * 3 / 4 - 17f, rect1.y, (!HATweakerSetting.WithBeard.NullOrEmpty()) && HATweakerSetting.WithBeard.Contains(disc)))
                        {
                            if ((!HATweakerSetting.WithBeard.NullOrEmpty()) && HATweakerSetting.WithBeard.Contains(disc))
                            {
                                HATweakerSetting.WithBeard.Remove(disc);
                            }
                            else
                            {
                                if (HATweakerSetting.WithBeard == null)
                                {
                                    HATweakerSetting.WithBeard = new List<string>();
                                }
                                HATweakerSetting.WithBeard.Add(disc);
                            }
                        }
                        rect1.x = 0f;
                        rect1.y += (LabelHeigh + 5f);
                    }
                }
            }
            IndexCount = se;
            Widgets.EndScrollView();
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
    internal static class HATSettingContents
    {
        public class BasicSettingUnit : ScrollViewContent
        {
            private float height = 100f;
            public override float Height => height;

            ThingDef def;
            float unitHeight = 30f;
            List<ThingStyleDef> styles;
            bool hasStyles = false;
            readonly string name;
            static readonly Color color0 = new ColorInt(40, 48, 48).ToColor;
            static readonly Color color1 = new ColorInt(32, 32, 32).ToColor;
            static readonly Color color2 = new ColorInt(16, 16, 16).ToColor;
            static readonly GUIStyle TextMidCenter = ABEasyUtility.GetTextStyle(TextAnchor.MiddleCenter);
            public BasicSettingUnit(float width, float height, string id, ThingDef def) : base(width, height, id)
            {
                this.def = def;
                name = def.label.ToLower();
                if (!ModsConfig.IdeologyActive)
                {
                    return;
                }
                if (def.CanBeStyled() && !def.RelevantStyleCategories.NullOrEmpty())
                {
                    this.styles = HATweakerUtility.GetStyles(def);
                }
                if (!styles.NullOrEmpty())
                {
                    this.hasStyles = true;
                    this.height = 93 * (styles.Count + 1) + 7f;
                }
            }
            public override void DrawContect(Rect inRect)
            {
                Widgets.DrawBox(inRect);
                Rect rect = inRect.ContractedBy(5f);
                HATweakerSetting.SingleInit(def);
                float of = rect.width * 0.1f;
                Rect cr = new Rect(rect.x + of, rect.y, rect.width - of, 3 * unitHeight);
                Rect tr = new Rect(rect.x, rect.y, of, rect.height);
                Widgets.DrawBoxSolid(tr, color2);
                GUI.Label(tr, def.label, TextMidCenter);
                if (HATweakerSetting.SettingData.TryGetValue(def.defName, out var data))
                {
                    //GUI.Label(cr, def.label, TextMidCenter);
                    DrawBasicDataSettings(ref data, cr, "Default".Translate(), def.uiIcon);
                    float add = cr.height + 3f;
                    cr.y += add;
                    if (hasStyles)
                    {
                        var st = styles.GetEnumerator();
                        while (st.MoveNext())
                        {
                            var style = st.Current;
                            if (style != null && !data.ChildrenData.NullOrEmpty() && data.ChildrenData.TryGetValue(style.defName, out var styleData))
                            {
                                DrawBasicDataSettings(ref data, cr, style.label.NullOrEmpty() ? style.defName : style.label, style.UIIcon);
                                cr.y += add;
                            }
                        }
                    }
                }

            }
            public override bool CanDisplay()
            {
                string a = HATweakerMod.search;
                if (a.NullOrEmpty())
                {
                    return true;
                }
                else
                {
                    return name.IndexOf(a.ToLower()) != -1;
                }
            }
            private void DrawBasicDataSettings(ref HATweakerSetting.HATSettingData data, Rect rt, string label = "Default", Texture icon = null, bool disable = false)
            {
                Widgets.DrawBoxSolidWithOutline(rt, color1, color0);
                float uh = rt.height / 3f;

                Rect labelRect = new Rect(rt.x, rt.y, rt.width, uh);
                Widgets.DrawBoxSolid(labelRect, color0);
                if (icon != null)
                {
                    Rect iconRect = new Rect(rt.x, rt.y, uh, uh);
                    GUI.DrawTexture(iconRect, icon);
                    labelRect.x += uh;
                    labelRect.width -= uh;
                }
                GUI.Label(labelRect, label, TextMidCenter);
                float optionWidth = rt.width / 3;
                Rect optionRect = new Rect(rt.x + 10f, rt.y + uh, optionWidth - 20f, uh);
                Widgets.DrawLineHorizontal(rt.x, rt.y + uh * 2, rt.width, color0);
                Rect vl = new Rect(rt.x + optionWidth, optionRect.y, 1f, 2 * uh);
                Widgets.DrawBoxSolid(vl, color0);
                vl.x += optionWidth;
                Widgets.DrawBoxSolid(vl, color0);
                Widgets.DrawHighlightIfMouseover(optionRect);
                if (Widgets.RadioButtonLabeled(optionRect, "No_Graphic".Translate(), data.NoGraphic, disable))
                {
                    data.NoGraphic = !data.NoGraphic;
                }
                if (!data.NoGraphic)
                {
                    optionRect.x += optionWidth;

                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, "No_Hair".Translate(), data.NoHair, disable))
                    {
                        data.NoHair = !data.NoHair;
                    }
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, "Hide_No_Fight".Translate(), data.HideNoFight, disable))
                    {
                        data.HideNoFight = !data.HideNoFight;
                    }
                    optionRect.x = rt.x + 10f;
                    optionRect.y += optionRect.height;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, "No_Beard".Translate(), data.NoBeard, disable))
                    {
                        data.NoBeard = !data.NoBeard;
                    }
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, "Hide_Indoor".Translate(), data.HideInDoor, disable))
                    {
                        data.HideInDoor = !data.HideInDoor;
                    }
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, "Hide_In_Bed".Translate(), data.HideInBed, disable))
                    {
                        data.HideInBed = !data.HideInBed;
                    }
                }
            }
        }
    }

    public class HATweakerSetting : ModSettings
    {

        public static Dictionary<string, HATSettingData> SettingData = new Dictionary<string, HATSettingData>();
        public static bool WorkOnColonist = true;
        public static bool useIsColonistCache = false;
        public static List<string> WithHair = new List<string>();
        public static List<string> WithBeard = new List<string>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref WorkOnColonist, "WorkOnColonist", true);
            Scribe_Values.Look(ref useIsColonistCache, "useIsColonistCache", false);
            Scribe_Collections.Look(ref WithHair, "WithHair");
            Scribe_Collections.Look(ref WithBeard, "WithBeard");
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
                                Scribe_Deep.Look(ref target, name);
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
            if (!def.IsApparel && def.apparel == null)
            {
                return;
            }
            if (SettingData == null)
            {
                SettingData = new Dictionary<string, HATSettingData>();
            }
            bool a = false;
            bool b = false;
            bool c = !def.apparel.renderSkipFlags.NullOrEmpty();
            if (c)
            {
                a = def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.Hair);
                b = def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.Beard);
            }
            else
            {
                if (!def.apparel.bodyPartGroups.NullOrEmpty())
                {
                    b = def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead);
                    a = b || def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead);
                }
            }
            if (!SettingData.ContainsKey(def.defName) || SettingData[def.defName] == null)
            {
                SettingData.SetOrAdd(def.defName, new HATSettingData()
                {
                    NoHair = a,
                    NoBeard = b,
                });

            }
            SettingData[def.defName].def = def;
            SettingData[def.defName].DefaultNoHair = a;
            SettingData[def.defName].DefaultNoBeard = b;
            SettingData[def.defName].DefaultNoEyes = c && b;
            SettingData[def.defName].GetDrawData();
            if (ModsConfig.IdeologyActive && def.CanBeStyled() && !def.RelevantStyleCategories.NullOrEmpty())
            {
                if (SettingData[def.defName].ChildrenData == null)
                {
                    SettingData[def.defName].ChildrenData = new Dictionary<string, HATSettingData>();
                }
                List<ThingStyleDef> styles = HATweakerUtility.GetStyles(def);
                if (!styles.NullOrEmpty())
                {
                    foreach (ThingStyleDef style in styles)
                    {
                        if (!SettingData[def.defName].ChildrenData.ContainsKey(style.defName) || SettingData[def.defName].ChildrenData[style.defName] == null)
                        {
                            SettingData[def.defName].ChildrenData.SetOrAdd(style.defName, new HATSettingData()
                            {
                                NoHair = a,
                                NoBeard = b
                            });
                        }
                        SettingData[def.defName].ChildrenData[style.defName].def = def;
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoHair = a;
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoBeard = b;
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoEyes = c && b;
                        SettingData[def.defName].ChildrenData[style.defName].GetDrawData();
                    }
                }
            }
        }
        public class HATSettingData : IExposable
        {
            internal ThingDef def;
            public Dictionary<string, HATSettingData> ChildrenData = new Dictionary<string, HATSettingData>();
            public bool UseDefault = true;
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
            public DrawData drawData;
            private bool hasDataOffsetValue = false;
            private bool hasDataRotationValue = false;
            List<Vector3> dataOffsetValues = new List<Vector3>(4) { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
            List<float> dataRotationValues = new List<float>(4) { 0, 0, 0, 0 };
            private readonly static FieldInfo drawDataDefault = AccessTools.Field(typeof(DrawData), "defaultData");
            private readonly static FieldInfo drawDataNorth = AccessTools.Field(typeof(DrawData), "dataNorth");
            private readonly static FieldInfo drawDataEast = AccessTools.Field(typeof(DrawData), "dataEast");
            private readonly static FieldInfo drawDataSouth = AccessTools.Field(typeof(DrawData), "dataSouth");
            private readonly static FieldInfo drawDataWest = AccessTools.Field(typeof(DrawData), "dataWest");
            private readonly static FieldInfo typeScales = AccessTools.Field(typeof(DrawData), "bodyTypeScales");
            public HATSettingData()
            {

            }
            public void ExposeData()
            {
                if ((Scribe.mode == LoadSaveMode.Saving && !ChildrenData.NullOrEmpty()) || Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    Scribe_Collections.Look(ref ChildrenData, "ChildrenData", LookMode.Value, LookMode.Deep);
                }
                Scribe_Values.Look(ref NoGraphic, "NoGraphic", false);
                Scribe_Values.Look(ref UseDefault, "UseDefault", true);
                Scribe_Values.Look(ref NoHair, "NoHair", DefaultNoHair, true);
                Scribe_Values.Look(ref NoBeard, "NoBeard", DefaultNoBeard, true);
                Scribe_Values.Look(ref AdvanceMode, "AdvanceMode", false);
                Scribe_Values.Look(ref HideNoFight, "HideNoFight", false);
                Scribe_Values.Look(ref HideInDoor, "HideInDoor", false);
                Scribe_Values.Look(ref HideInBed, "HideInBed", false);
                Look(ref size, "size", 2, Vector2.one);
                Look(ref SouthOffset, "SouthOffset", 2);
                Look(ref NorthOffset, "NorthOffset", 2);
                Look(ref EastOffset, "EastOffset", 2);
                Look(ref WestOffset, "WestOffset", 2);
                Look(ref SouthRotation, "SouthRotation", 0);
                Look(ref NorthRotation, "NorthRotation", 0);
                Look(ref EastRotation, "EastRotation", 0);
                Look(ref WestRotation, "WestRotation", 0);
                Look(ref LayerOffset, "LayerOffset", 4);
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

            public DrawData GetDrawData(bool reGet = false)
            {
                if (drawData == null || reGet)
                {
                    if (dataOffsetValues == null)
                    {
                        new List<Vector3>(4) { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
                    }
                    CreateDrawData();

                }
                return drawData;
            }
            private void CreateDrawData()
            {
                DrawData draw0 = def?.apparel?.drawData;
                if (drawDataDefault != null && drawDataNorth != null && drawDataEast != null && drawDataSouth != null && drawDataWest != null && typeScales != null)
                {
                    drawData = new DrawData();
                    RotationalData defaultD;
                    RotationalData? north;
                    RotationalData? east;
                    RotationalData? south;
                    RotationalData? west;

                    if (draw0 != null)
                    {
                        var ts = typeScales.GetValue(draw0);
                        defaultD = (RotationalData)drawDataDefault.GetValue(draw0);
                        north = (RotationalData?)drawDataNorth.GetValue(draw0);
                        east = (RotationalData?)drawDataEast.GetValue(draw0);
                        south = (RotationalData?)drawDataSouth.GetValue(draw0);
                        west = (RotationalData?)drawDataWest.GetValue(draw0);
                        typeScales.SetValue(drawData, ts);
                        drawData.scale = draw0.scale;
                        drawData.childScale = draw0.childScale;
                        drawData.useBodyPartAnchor = draw0.useBodyPartAnchor;
                        drawData.scaleOffsetByBodySize = draw0.scaleOffsetByBodySize;
                    }
                    else
                    {
                        defaultD = new RotationalData();
                        north = new RotationalData()
                        {
                            rotation = Rot4.North
                        };
                        east = new RotationalData()
                        {
                            rotation = Rot4.East
                        };
                        south = new RotationalData()
                        {
                            rotation = Rot4.South
                        };
                        west = new RotationalData()
                        {
                            rotation = Rot4.West
                        };
                    }
                    drawDataDefault.SetValue(drawData, defaultD);
                    drawDataNorth.SetValue(drawData, SetRotationaldata(north.HasValue ? north.Value : defaultD, Rot4.North));
                    drawDataEast.SetValue(drawData, SetRotationaldata(east.HasValue ? east.Value : defaultD, Rot4.East));
                    drawDataSouth.SetValue(drawData, SetRotationaldata(south.HasValue ? south.Value : defaultD, Rot4.South));
                    drawDataWest.SetValue(drawData, SetRotationaldata(west.HasValue ? west.Value : defaultD, Rot4.West));
                }
                RotationalData? SetRotationaldata(RotationalData rData, Rot4? rot = null)
                {
                    //Log.Warning("00000");
                    var n = new RotationalData()
                    {
                        rotation = rot,
                        flip = rData.flip,
                        layer = rData.layer
                    };
                    Vector3 no;
                    if (rot != null && rot.HasValue)
                    {
                        no = getOffset(rot.Value);
                    }
                    else
                    {
                        no = Vector3.zero;
                    }
                    if (n.offset != null && n.offset.HasValue)
                    {
                        var o = n.offset.Value;
                        hasDataOffsetValue = true;
                        if (rot != null && rot.HasValue && dataOffsetValues.Count == 4)
                        {
                            dataOffsetValues[rot.Value.AsInt] = new Vector3(o.x, o.y, o.z);
                        }
                        n.offset = new Vector3(o.x + no.x, o.y + no.y, o.z + no.z);
                    }
                    else
                    {
                        n.offset = new Vector3(no.x, no.y, no.z);
                    }
                    float r;
                    if (rot != null && rot.HasValue)
                    {
                        r = GetRotation(rot.Value);
                    }
                    else
                    {
                        r = 0;
                    }
                    if (n.rotationOffset != null && n.rotationOffset.HasValue)
                    {
                        var f = n.rotationOffset.Value;
                        hasDataRotationValue = true;
                        if (rot != null && rot.HasValue && dataRotationValues.Count == 4)
                        {
                            dataRotationValues[rot.Value.AsInt] = f;
                        }
                        n.rotationOffset = f + r;
                    }
                    else
                    {
                        n.rotationOffset = r;
                    }
                    if (n.pivot.HasValue)
                    {
                        var o = n.pivot.Value;
                        n.pivot = new Vector2(o.x, o.y);
                    }
                    return n;
                }
            }

            public bool CanDraw(Pawn pawn)
            {
                if (pawn == null)
                {
                    return false;
                }
                if (WorkOnColonist && !ABEasyUtility.IsColonist(pawn))
                {
                    return true;
                }
                if (NoGraphic)
                {
                    return false;
                }
                if (HideInBed && pawn.InBed())
                {
                    return false;
                }
                else
                {
                    if (HideNoFight && pawn.Drafted)
                    {
                        return true;
                    }
                    else
                    if (HideInDoor)
                    {
                        return pawn.Map != null && pawn.Position.UsesOutdoorTemperature(pawn.Map);
                    }
                    else
                    {
                        return !HideNoFight;
                    }
                }
            }

            public Vector3 getOffset(Rot4 headFace)
            {
                Vector2 offset = Vector2.zero;
                switch (headFace.AsInt)
                {
                    case 0:
                        offset = NorthOffset;
                        break;
                    case 1:
                        offset = EastOffset;
                        break;
                    case 2:
                        offset = SouthOffset;
                        break;
                    case 3:
                        offset = WestOffset;
                        break;
                }
                return new Vector3(offset.x, LayerOffset, offset.y);
            }
            public void SetOffset(Rot4 headFace)
            {
                if (drawData != null)
                {
                    FieldInfo fieldInfo = null;
                    switch (headFace.AsInt)
                    {
                        case 0:
                            fieldInfo = drawDataNorth;
                            break;
                        case 1:
                            fieldInfo = drawDataEast;
                            break;
                        case 2:
                            fieldInfo = drawDataSouth;
                            break;
                        case 3:
                            fieldInfo = drawDataWest;
                            break;
                    }
                    if (fieldInfo != null)
                    {
                        var va0 = (RotationalData?)fieldInfo.GetValue(drawData);
                        RotationalData va;
                        if (va0 == null || !va0.HasValue)
                        {
                            va = new RotationalData()
                            {
                                rotation = headFace
                            };
                        }
                        else
                        {
                            va = va0.Value;
                        }
                        var o = getOffset(headFace);
                        if (hasDataOffsetValue)
                        {
                            va.offset = o + dataOffsetValues[headFace.AsInt];
                        }
                        else
                        {
                            va.offset = o;
                        }
                        fieldInfo.SetValue(drawData, va);
                    }
                }
                //var va = rotationalDatas[headFace.AsInt];



            }
            public float GetRotation(Rot4 headFace)
            {
                float a = 0;
                switch (headFace.AsInt)
                {
                    case 0:
                        a = NorthRotation;
                        break;
                    case 1:
                        a = EastRotation;
                        break;
                    case 2:
                        a = SouthRotation;
                        break;
                    case 3:
                        a = WestRotation;
                        break;
                }
                float b = a;
                return b;
            }
            public void SetRotation(Rot4 headFace)
            {
                FieldInfo fieldInfo = null;
                switch (headFace.AsInt)
                {
                    case 0:
                        fieldInfo = drawDataNorth;
                        break;
                    case 1:
                        fieldInfo = drawDataEast;
                        break;
                    case 2:
                        fieldInfo = drawDataSouth;
                        break;
                    case 3:
                        fieldInfo = drawDataWest;
                        break;
                }
                if (fieldInfo != null)
                {
                    var va0 = (RotationalData?)fieldInfo.GetValue(drawData);
                    RotationalData va;
                    if (va0 == null || !va0.HasValue)
                    {
                        va = new RotationalData()
                        {
                            rotation = headFace
                        };
                    }
                    else
                    {
                        va = va0.Value;
                    }
                    var o = GetRotation(headFace);
                    if (hasDataRotationValue)
                    {
                        va.rotationOffset = o+ dataRotationValues[headFace.AsInt];
                    }
                    else
                    {
                        va.rotationOffset = o;
                    }
                    fieldInfo.SetValue(drawData, va);
                }

            }
        }
    }
    [StaticConstructorOnStartup]
    public static class HATweakerCache
    {
        public static List<ThingDef> HeadApparel = new List<ThingDef>();
        public static Dictionary<string, DrawData> drawDataCache = new Dictionary<string, DrawData>();
        internal static RenderTexture texture = null;
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
            (((!x.apparel.bodyPartGroups.NullOrEmpty()) && (x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) || x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead)))
            || ((!x.apparel.layers.NullOrEmpty()) && x.apparel.layers.Any(a => Layers.Contains(a.defName))))).ToList();
            return BodyHeadApparel;
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
        public class AlienCompatible
        {
            public Dictionary<int, string> AllAddons = new Dictionary<int, string>();
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
                        AllAddons.SetOrAdd(addon.GetHashCode(), b);
                    }
                }
            }
        }
    }
    public static class HarmonyPatchA5
    {
        public static float rotate = 0;
        static Type This = typeof(HarmonyPatchA5);
        static Type renderTree = typeof(PawnRenderTree);
        static Type renderNodeSetup = typeof(DynamicPawnRenderNodeSetup_Apparel);
        static PatchFWMod patchFWMod = null;
        //static FieldInfo nodeSetupPwan = null;
        internal static void PatchAllByHAT(Harmony harmony)
        {
            List<string> debug = new List<string>();
            /*var flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = renderNodeSetup.GetNestedTypes(BindingFlags.NonPublic)?.Where(a => a.GetMethods(flag).Any(m => m.Name == "MoveNext") && a.Name.IndexOf("GetDynamicNodes") != -1 && a.Name.IndexOf("d__3") != -1).FirstOrDefault();
            MethodInfo getDynamicNodes = null;
            if (type != null)
            {
                getDynamicNodes = type.GetMethod("MoveNext", flag);
                nodeSetupPwan = type.GetField("pawn", flag);
            }
            //AccessTools.Method(renderNodeSetup, nameof(DynamicPawnRenderNodeSetup_Apparel.GetDynamicNodes));
            if (getDynamicNodes != null)
            {
                harmony.Patch(getDynamicNodes, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranGetDynamicNodes)));
                debug.Add("0");
            }*/
            MethodInfo processApparel = AccessTools.Method(renderNodeSetup, "ProcessApparel");
            if (processApparel != null)
            {
                harmony.Patch(processApparel, prefix: new HarmonyMethod(This, nameof(PreProcessApparel)), transpiler: new HarmonyMethod(This, nameof(TranProcessApparel)));
                debug.Add("0");
            }
            /*MethodInfo getMat = AccessTools.Method(renderTree, nameof(PawnRenderTree.TryGetMatrix));
            if (getMat != null)
            {
                harmony.Patch(getMat, transpiler: new HarmonyMethod(This, nameof(HarmonyPatchA5.TranTryGetMatrix)));
                debug.Add("1");
            }*/

            MethodInfo adjustParms = AccessTools.Method(renderTree, "AdjustParms");
            if (adjustParms != null)
            {
                harmony.Patch(adjustParms, transpiler: new HarmonyMethod(This, nameof(TranAdjustParms)));
                debug.Add("1");
            }

            //Hide Not Drafted;
            MethodInfo setDraft = AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted));
            if (setDraft != null)
            {
                harmony.Patch(setDraft, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(TranSetDrafted)));
                debug.Add("2");
            }

            //Hide Under Roof;
            MethodInfo setPosition = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            if (setPosition != null)
            {
                harmony.Patch(setPosition, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(TranSetPosition)));
                debug.Add("3");
            }
            MethodInfo test = AccessTools.Method(typeof(PawnRenderNodeWorker), nameof(PawnRenderNodeWorker.OffsetFor));
            /*if (test != null)
            {
                harmony.Patch(test, postfix: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(Test)));
                debug.Add("3");
            }*/
            if (HATweakerMod.FWModIndex != -1)
            {
                patchFWMod = new PatchFWMod();
            }
            if (debug.Count < 4)
            {
                Log.Warning(string.Join(" | ", debug));
            }
        }
        /*public static void Test(PawnRenderNode node, PawnDrawParms parms,ref Vector3 __result)
        {
            __result.x += 0.5f;
        }*/
        public static bool PreProcessApparel(Pawn pawn, PawnRenderTree tree, Apparel ap, PawnRenderNode headApparelNode, PawnRenderNode bodyApparelNode, Dictionary<PawnRenderNode, int> layerOffsets)
        {
            return ApplyGraphicData(pawn) && CanDrawApparel(pawn, ap);
        }

        /*public static IEnumerable<CodeInstruction> TranGetDynamicNodes(IEnumerable<CodeInstruction> codes, ILGenerator generator)
        {
            Label a = generator.DefineLabel();
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i < list.Count - 2 && nodeSetupPwan != null && code.Is(OpCodes.Call, AccessTools.Method(renderNodeSetup, "ShouldAddApparelNode")))
                {
                    yield return code;
                    yield return list[i + 1];
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, nodeSetupPwan);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(CanDrawApparel)));
                }
                else
                {
                    yield return code;
                }
            }
        }*/
        public static bool CanDrawApparel(Pawn pawn, Apparel ap)
        {
            if (HATweakerMod.pawn == pawn && !HATweakerCache.HeadApparel.NullOrEmpty() && HATweakerCache.HeadApparel.Contains(ap.def) && ap != HATweakerMod.apparel)
            {
                return false;
            }
            return CanDisplay(ap, pawn);
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
            FieldInfo info0 = typeof(Apparel).GetField("def");
            FieldInfo info1 = typeof(ThingDef).GetField("apparel");
            FieldInfo info2 = typeof(ApparelProperties).GetField("renderSkipFlags");
            MethodInfo method = AccessTools.PropertyGetter(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.WornApparel));
            List<CodeInstruction> list = codes.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i < 87 || i > 132)
                {
                    if (i > 6 && code.opcode == OpCodes.Ldfld && code.OperandIs(info2) && list[i - 1].opcode == OpCodes.Ldfld && list[i - 1].OperandIs(info1))
                    {
                        yield return code;
                        yield return new CodeInstruction(OpCodes.Ldloc_1);
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
                    else if (i == 57)
                    {
                        yield return new CodeInstruction(OpCodes.Brfalse_S, a);
                    }
                    else if (i == 133)
                    {
                        code.labels.Add(a);
                        yield return code;
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
        }
        private static List<Apparel> GetApparel_1(List<Apparel> origin, Pawn pawn)
        {
            List<Apparel> or;
            if (patchFWMod != null)
            {
                or = patchFWMod.GetFWApparel(pawn, origin);
            }
            else
            {
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

        /*public static IEnumerable<CodeInstruction> TranTryGetMatrix(IEnumerable<CodeInstruction> codes)
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
            if (ApplyGraphicData(parms.pawn) &&
                (node.Props.workerClass == typeof(PawnRenderNodeWorker_Apparel_Head) && node.children.NullOrEmpty()
            && HATweakerSetting.SettingData.TryGetValue(node.apparel != null ? node.apparel.def.defName : node.Props.debugLabel, out HATweakerSetting.HATSettingData data0)))
            {
                HATweakerSetting.HATSettingData data;
                if (ModsConfig.IdeologyActive && !data0.UseDefault
                    && !data0.ChildrenData.NullOrEmpty()
                    && node.apparel != null
                    && node.apparel.StyleDef != null
                    && data0.ChildrenData.TryGetValue(node.apparel.StyleDef.defName, out HATweakerSetting.HATSettingData data1)
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

        }*/

        public static IEnumerable<CodeInstruction> TranProcessApparel(IEnumerable<CodeInstruction> codes)
        {
            List<CodeInstruction> list = codes.ToList();
            FieldInfo field0 = AccessTools.Field(typeof(PawnRenderNodeProperties), nameof(PawnRenderNodeProperties.drawData));
            bool notNullField0 = field0 != null;
            CodeInstruction code0 = null;
            int index = 0;
            for (int i = 0; i < list.Count; i++)
            {
                CodeInstruction code = list[i];
                if (i > 10 && i < list.Count - 10 && notNullField0 && code.Is(OpCodes.Stfld, field0) && list[i - 1].opcode == OpCodes.Ldloc_2 && list[i + 1].opcode == OpCodes.Stloc_0 && list[i + 2].opcode == OpCodes.Br)
                {
                    code0 = list[i + 1];
                    index = i + 1;
                    //yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetHeadClothesProps)));
                    yield return code;
                    //Log.Warning(" 0101:"+code.operand.ToStringSafe()+"|"+field0.ToStringSafe());
                }
                else if (code == code0 && i == index)
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Ldloca, 0);
                    //yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(This, nameof(SetHeadClothesSize)));

                }
                else
                {
                    yield return code;
                }
            }
        }

        public static void SetHeadClothesSize(ref PawnRenderNodeProperties prop, Apparel cloth, Pawn pawn)
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
                    prop.drawSize.x *= data.size.x;
                    prop.drawSize.y *= data.size.y;
                }
            }
        }

        public static DrawData SetHeadClothesProps(DrawData drawData, Apparel cloth, Pawn pawn)
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

                        //Log.Warning("notNull");
                        return d;
                    }
                    //draw.drawSize.x *= data.size.x;
                    //properties.drawSize.y *= data.size.y;
                }
            }
            return drawData;
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
                    yield return new CodeInstruction(OpCodes.Ldarg_0, null);
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
            if (ApplyGraphicData(pawn) && pawn.apparel != null && pawn.apparel.AnyApparel)
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
                if (ApplyGraphicData(pawn) && pawn.Map != null && pawn.apparel != null && pawn.apparel.AnyApparel)
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

        public class HarmonyPatchAlienRace
        {
            private static Dictionary<string, string> raceName = new Dictionary<string, string>();
            public HarmonyPatchAlienRace(Harmony harmony)
            {
                if (HATweakerMod.AlienIndex != -1)
                {
                    MethodInfo info = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon").GetMethods(AccessTools.all).
                        FirstOrDefault(x => x.Name == "CanDrawAddon" && x.GetParameters().Any(a => a.ParameterType == typeof(Pawn)));
                    if (info != null)
                    {
                        //Log.Warning("aaa");
                        harmony.Patch(info, prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(PreCanDrawAddon))));
                    }
                    MethodInfo info1 = AccessTools.TypeByName("AlienRace.AlienPartGenerator+BodyAddon").GetMethods(AccessTools.all).
                        FirstOrDefault(x => x.Name == "CanDrawAddonStatic" && x.GetParameters().Any(a => a.ParameterType == typeof(Pawn)));
                    if (info1 != null)
                    {
                        harmony.Patch(info1, prefix: new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatchAlienRace), nameof(PreCanDrawAddon))));
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
        public class PatchFWMod
        {
            internal List<Apparel> GetFWApparel(Pawn pawn, List<Apparel> a)
            {
                if (HarmonyPatchA8.FWork(pawn))
                {
                    return pawn.GetComp<FashionOverrideComp>().GetApparel();
                }
                return a;
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