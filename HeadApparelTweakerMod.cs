using Fashion_Wardrobe;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Xml;
using UnityEngine;
using Verse;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        private static int HATweakerModIndex = -1;
        internal static int FWModIndex = -1;
        public static int AlienIndex = -1;
        internal static string choose = "";
        private string search = "";
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
        private int ListCount = 0;
        private int unitCount = 0;
        private const float unitH = 120f;
        private float Viewheight(List<ThingDef> list1)
        {
            if (list1.Count != ListCount)
            {
                unitCount = list1.SelectMany(a =>
                {
                    if (a.CanBeStyled() && !a.RelevantStyleCategories.NullOrEmpty())
                    {
                        return a.RelevantStyleCategories.SelectMany(s =>
                        {
                            return s.thingDefStyles.Where(b => b.ThingDef == a).Select(c => c.StyleDef);
                        });
                    }
                    return new List<ThingStyleDef>();
                }).Count() + list1.Count;
                ListCount = list1.Count;
            }
            return unitH * unitCount;
        }

        private void DrawBasicSettings(List<ThingDef> list, Rect inRect)
        {
            string useDefault1 = "Use_Default1".Translate();
            string tooltip = "pgta".Translate();
            Rect rect0 = inRect.BottomPart(0.95f);
            List<ThingDef> list1;
            if (string.IsNullOrEmpty(search))
            {
                list1 = list.ToList();
            }
            else
            {
                list1 = list.Where(a1 => a1.label?.Contains(search) == true).ToList();
            }
            if (list1.NullOrEmpty())
            {
                return;
            }
            Rect r2 = new Rect(0, 0, rect0.width - 17f, Viewheight(list1) + 10);
            Widgets.DrawWindowBackground(rect0);
            Widgets.BeginScrollView(rect0, ref BaScrLoc, r2);
            Rect rt3 = new Rect(5, 5, r2.width - 5, unitH);
            GUIStyle labelStyle = HATweakerUtility.GetLabelStyle(TextAnchor.MiddleCenter);

            for (int i = 0; i < list1.Count; i++)
            {
                ThingDef def = list1[i];
                if (rt3.y + rt3.height > BaScrLoc.y && rt3.y < BaScrLoc.y + rect0.height)
                {
                    // 初始化设置数据
                    HATweakerSetting.SingleInit(def);
                    HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[def.defName];
                    // 绘制背景色和纹理
                    GUI.color = new ColorInt(97, 108, 122).ToColor;
                    GUI.DrawTexture(new Rect(rt3.x, rt3.y + rt3.height, rt3.width, data.ChildrenData.NullOrEmpty() ? 4f : 1f), BaseContent.WhiteTex);
                    // 绘制分割线
                    Widgets.DrawLineHorizontal(rt3.x + rt3.height, rt3.y + rt3.height / 2 + labH / 2, rt3.width - rt3.height);
                    for (int j = 0; j < 3; j++)
                    {
                        Widgets.DrawLineVertical(rt3.x + rt3.height + j * (rt3.width - rt3.height) / 3, rt3.y, rt3.height);
                    }

                    // 绘制图标和标签
                    GUI.color = Color.white;
                    Rect iconRect = new Rect(rt3.x + 5f, rt3.y + 5f, rt3.height - 10f, rt3.height - 10f);
                    if (def.uiIcon != null)
                    {
                        GUI.DrawTexture(iconRect, def.uiIcon);
                    }
                    Rect labelRect = new Rect(rt3.x + rt3.height, rt3.y, rt3.width - rt3.height, labH);
                    Widgets.DrawBoxSolid(labelRect, Color.gray);
                    GUI.Label(labelRect, def.label, labelStyle);

                    // 绘制选项按钮
                    Rect rectO = new Rect(rt3.x + rt3.height, rt3.y + labH, rt3.width - rt3.height, rt3.height - labH);
                    DrawBasicDataSettings(ref data, rectO);
                }
                rt3.y += rt3.height;
                if (!def.CanBeStyled() || def.RelevantStyleCategories.NullOrEmpty())
                {
                    continue;
                }
                List<ThingStyleDef> styles = HATweakerUtility.GetStyles(def);
                if (styles.NullOrEmpty())
                {
                    continue;
                }

                for (int s = 0; s < styles.Count; s++)
                {
                    if (rt3.y + rt3.height > BaScrLoc.y && rt3.y < BaScrLoc.y + rect0.height)
                    {
                        ThingStyleDef style = styles[s];
                        GUI.color = new ColorInt(97, 108, 122).ToColor;
                        float len = s == styles.Count - 1 ? rt3.x : rt3.x + 0.1f * rt3.height;
                        GUI.DrawTexture(new Rect(len, rt3.y + rt3.height, rt3.width - 10f, s == styles.Count - 1 ? 4f : 1f), BaseContent.WhiteTex);


                        // 绘制图标和标签
                        GUI.color = Color.white;
                        Rect iconRect1 = new Rect(rt3.x + 0.3f * rt3.height, rt3.y + 0.1f * rt3.height, rt3.height * 0.5f, rt3.height * 0.5f);
                        Widgets.DrawBox(iconRect1);
                        GUI.DrawTexture(iconRect1, style.UIIcon);

                        Rect labelRect1 = new Rect(rt3.x + 0.15f * rt3.height, rt3.y + rt3.height * 0.6f, rt3.height * 0.8f, rt3.height * 0.4f);
                        string a;
                        if (style.label.NullOrEmpty())
                        {
                            a = style.defName;
                        }
                        else
                        {
                            a = style.label;
                        }
                        GUI.Label(labelRect1, a, labelStyle);
                        HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[def.defName];
                        if (!data.ChildrenData.NullOrEmpty() && data.ChildrenData.TryGetValue(style.defName, out HATweakerSetting.HATSettingData data1))
                        {
                            Rect rl = new Rect(rt3.x + rt3.height, rt3.y, rt3.width - rt3.height, rt3.height);
                            if (data.UseDefault)
                            {
                                GUI.Label(rl, tooltip, labelStyle);
                            }
                            else
                            {
                                GUI.color = new ColorInt(97, 108, 122).ToColor;
                                Rect labelRect = new Rect(rt3.x + rt3.height, rt3.y, rt3.width - rt3.height, labH);
                                Widgets.CheckboxLabeled(labelRect, useDefault1, ref data1.UseDefault);
                                Widgets.DrawLineHorizontal(labelRect.x, rt3.y + labelRect.height, labelRect.width);
                                // 绘制分割线

                                float lineOffset1 = 0.9f * rt3.height;
                                Widgets.DrawLineHorizontal(rt3.x + rt3.height, rt3.y + rt3.height / 2 + labH / 2, rt3.width - rt3.height);
                                for (int j = 0; j < 3; j++)
                                {
                                    Widgets.DrawLineVertical(rt3.x + rt3.height + j * (rt3.width - rt3.height) / 3, rt3.y + (j == 0 ? 0 : labH), rt3.height - (j == 0 ? 0 : labH));
                                }
                                GUI.color = Color.white;
                                Rect rectO = new Rect(rt3.x + rt3.height, rt3.y + labH, rt3.width - rt3.height, rt3.height - labH);
                                DrawBasicDataSettings(ref data1, rectO, data1.UseDefault);
                            }
                        }
                    }
                    rt3.y += rt3.height;
                }

            }
            Widgets.EndScrollView();
        }
        private float labH = unitH / 4f;
        private void DrawBasicDataSettings(ref HATweakerSetting.HATSettingData data, Rect rt, bool disable = false)
        {
            float optionWidth = rt.width / 3;
            Rect optionRect = new Rect(rt.x + 10f, rt.y, optionWidth - 20f, rt.height);
            if (Mouse.IsOver(optionRect.TopHalf()))
            {
                Widgets.DrawHighlight(optionRect.TopHalf());
            }
            if (Widgets.RadioButtonLabeled(optionRect.TopHalf(), "No_Graphic".Translate(), data.NoGraphic, disable))
            {
                data.NoGraphic = !data.NoGraphic;
            }

            if (!data.NoGraphic)
            {
                if (Mouse.IsOver(optionRect.BottomHalf()))
                {
                    Widgets.DrawHighlight(optionRect.BottomHalf());
                }
                if (Widgets.RadioButtonLabeled(optionRect.BottomHalf(), "No_Beard".Translate(), data.NoBeard, disable))
                {
                    data.NoBeard = !data.NoBeard;
                }
                optionRect.x += optionWidth;

                if (Mouse.IsOver(optionRect.TopHalf()))
                {
                    Widgets.DrawHighlight(optionRect.TopHalf());
                }
                if (Widgets.RadioButtonLabeled(optionRect.TopHalf(), "No_Hair".Translate(), data.NoHair, disable))
                {
                    data.NoHair = !data.NoHair;
                }

                if (Mouse.IsOver(optionRect.BottomHalf()))
                {
                    Widgets.DrawHighlight(optionRect.BottomHalf());
                }
                if (Widgets.RadioButtonLabeled(optionRect.BottomHalf(), "Hide_Indoor".Translate(), data.HideInDoor, disable))
                {
                    data.HideInDoor = !data.HideInDoor;
                }
                optionRect.x += optionWidth;

                if (Mouse.IsOver(optionRect.TopHalf()))
                {
                    Widgets.DrawHighlight(optionRect.TopHalf());
                }
                if (Widgets.RadioButtonLabeled(optionRect.TopHalf(), "Hide_No_Fight".Translate(), data.HideNoFight, disable))
                {
                    data.HideNoFight = !data.HideNoFight;
                }

                if (Mouse.IsOver(optionRect.BottomHalf()))
                {
                    Widgets.DrawHighlight(optionRect.BottomHalf());
                }
                if (Widgets.RadioButtonLabeled(optionRect.BottomHalf(), "Hide_In_Bed".Translate(), data.HideInBed, disable))
                {
                    data.HideInBed = !data.HideInBed;
                }
            }
        }
        private string chooseStyle = "";
        private void DrawAdvanceSettings(List<ThingDef> list, Rect inRect)
        {
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
            if (def.CanBeStyled() && !data.ChildrenData.NullOrEmpty())
            {
                List<ThingStyleDef> styles = HATweakerUtility.GetStyles(def);
                if (!styles.NullOrEmpty())
                {


                    Widgets.DrawBoxSolid(main1, Color.gray);
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
            Widgets.DrawBoxSolid(main1, Color.gray);
            GUI.Label(main1, "Basic_Settings".Translate(), labelStyle);
            main1.y += LabelHeigh;
            if (!chooseStyle.NullOrEmpty())
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
                Widgets.DrawBoxSolid(main1, Color.gray);
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
                    DrawAdjust(rectAdj, "South".Translate() + ":" + data.SouthOffset.ToString("F2"), ref data.SouthOffset.x, ref data.SouthOffset.y, -1, 1, 0.01f, () => data.SouthOffset = Vector2.zero);
                    rectAdj.y += rectAdj.height + 2f;
                    DrawAdjust(rectAdj, "North".Translate() + ":" + data.NorthOffset.ToString("F2"), ref data.NorthOffset.x, ref data.NorthOffset.y, -1, 1, 0.01f, () => data.NorthOffset = Vector2.zero);
                    rectAdj.y += rectAdj.height + 2f;
                    DrawAdjust(rectAdj, "West".Translate() + ":" + data.WestOffset.ToString("F2"), ref data.WestOffset.x, ref data.WestOffset.y, -1, 1, 0.01f, () => data.WestOffset = Vector2.zero);
                    rectAdj.y += rectAdj.height + 2f;
                    DrawAdjust(rectAdj, "East".Translate() + ":" + data.EastOffset.ToString("F2"), ref data.EastOffset.x, ref data.EastOffset.y, -1, 1, 0.01f, () => data.EastOffset = Vector2.zero);
                    rectAdj.y += rectAdj.height + 2f;
                    DrawAdjust(rectAdj, "Rotate".Translate() + ":" + "South".Translate() + "[" + data.SouthRotation.ToString("0") + "]" + "North".Translate() + "[" + data.NorthRotation.ToString("0") + "]", ref data.SouthRotation, ref data.NorthRotation, -180, 180, 1, () =>
                    {
                        data.SouthRotation = 0f;
                        data.NorthRotation = 0f;
                    });
                    rectAdj.y += rectAdj.height + 2f;
                    DrawAdjust(rectAdj, "Rotate".Translate() + ":" + "East".Translate() + "[" + data.EastRotation.ToString("0") + "]" + "West".Translate() + "[" + data.WestRotation.ToString("0") + "]", ref data.EastRotation, ref data.WestRotation, -180, 180, 1, () =>
                    {
                        data.EastRotation = 0f;
                        data.WestRotation = 0f;
                    });
                    main1.height = rectAdj.height * 0.4f;
                    main1.y = rectAdj.y + rectAdj.height + 2f;
                    Widgets.Label(main1.LeftPart(0.7f), "Layer_Offset".Translate() + ":" + data.LayerOffset.ToString("f5"));
                    if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                    {
                        data.LayerOffset = 0f;
                    }
                    main1.height = rectAdj.height * 0.3f;
                    main1.y += (main1.height + 2f);
                    main1.width = main1.height;
                    if (Widgets.ButtonImage(main1, TexButton.Minus))
                    {
                        data.LayerOffset = data.LayerOffset > -0.003f ? data.LayerOffset - 0.00001f : -0.003f;
                    }
                    ;
                    main1.x += main1.width;
                    main1.width = rectAdj.width - 2 * main1.height;
                    data.LayerOffset = Widgets.HorizontalSlider(main1, data.LayerOffset, -0.003f, +0.003f);
                    main1.x += main1.width;
                    main1.width = main1.height;
                    if (Widgets.ButtonImage(main1, TexButton.Plus))
                    {
                        data.LayerOffset = data.LayerOffset < 0.003f ? data.LayerOffset + 0.00001f : 0.003f;
                    }
                    ;
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
                HATweakerUtility.DrawPawnCache(pawn, new Vector2(main2.width, main2.height), direction, out HATweakerCache.texture);
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
            void DrawAdjust(Rect rectAd, string label, ref float x, ref float y, float min, float max, float interval, Action action)
            {
                Rect rectLa = rectAd.TopPart(0.4f);
                Widgets.Label(rectLa.LeftPart(0.7f), label);
                if (Widgets.ButtonText(rectLa.RightPart(0.3f).TopHalf(), "Reset".Translate()))
                {
                    if (action != null)
                    {
                        action();
                    }

                }
                Rect rectXYL = rectAd.BottomPart(0.6f).TopHalf();
                Rect minus = new Rect(rectXYL.x, rectXYL.y, rectXYL.height, rectXYL.height);
                rectXYL.x += rectXYL.height;
                rectXYL.width -= 2 * rectXYL.height;
                if (Widgets.ButtonImage(minus, TexButton.Minus))
                {
                    x = x > min ? x - interval : min;
                }
                ;
                x =
                Widgets.HorizontalSlider(rectXYL, x, min, max);
                minus.x += (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Plus))
                {
                    x = x < max ? x + interval : max;
                }
                ;
                rectXYL.y += rectXYL.height;
                minus.y += rectXYL.height;
                minus.x -= (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Minus))
                {
                    y = y > min ? y - interval : min;
                }
                ;
                y =
                Widgets.HorizontalSlider(rectXYL.BottomHalf(), y, min, max);
                minus.x += (rectXYL.width + rectXYL.x);
                if (Widgets.ButtonImage(minus, TexButton.Plus))
                {
                    y = y < max ? y + interval : max;
                }
                ;
                Widgets.DrawLineHorizontal(rectAd.x + 5f, rectAd.y + rectAd.height, rectAd.width - 5f, Color.gray);
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
            SettingData[def.defName].DefaultNoHair = a;
            SettingData[def.defName].DefaultNoBeard = b;
            SettingData[def.defName].DefaultNoEyes = c && b;
            if (def.CanBeStyled() && !def.RelevantStyleCategories.NullOrEmpty())
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
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoHair = a;
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoBeard = b;
                        SettingData[def.defName].ChildrenData[style.defName].DefaultNoEyes = c && b;
                    }
                }
            }
        }
        public class HATSettingData : IExposable
        {
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

            public bool CanDraw(Pawn pawn)
            {
                if (pawn == null)
                {
                    return false;
                }
                if (WorkOnColonist && !HATweakerCache.IsColonist(pawn))
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
                        return pawn.Map != null && pawn.Position != null && pawn.Position.UsesOutdoorTemperature(pawn.Map);
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
                return new Vector3(offset.x / 10, LayerOffset, offset.y / 10);
            }
            public float getRotation(Rot4 headFace)
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
                return a / 4;
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
        private static HATCacheSystem<bool> Colonists = new HATCacheSystem<bool>();
        private static bool working = false;
        public static List<string> Layers
        {
            get
            {
                return HeadLayerListDefOf.AllHeadLayerList.HeadLayerList;
            }
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
        public static bool IsColonist(Pawn pawn)
        {
            if (HATweakerSetting.useIsColonistCache)
            {
                try
                {
                    if (pawn == null)
                    {
                        return false;
                    }
                    string a = pawn.Name.ToStringFull;
                    if (Colonists == null)
                    {
                        Colonists = new HATCacheSystem<bool>();
                    }
                    if (Colonists.Count == 0 || !Colonists.TryGet(a, out bool ac))
                    {
                        bool isc = pawn.IsColonist;
                        Colonists.Set(a, isc);
                        return isc;
                    }
                    else
                    {
                        return Colonists.TryGet(a, out var isc) && isc;

                    }
                }
                catch
                {
                }
            }
            return pawn.IsColonist;
        }

        public static List<ThingDef> GetAllOverHead()
        {
            //List<ThingDef> HeadApparel = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && x.apparel.LastLayer != null && Layers.Contains(x.apparel.LastLayer.defName)).ToList();
            List<ThingDef> BodyHeadApparel = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel &&
            (((!x.apparel.bodyPartGroups.NullOrEmpty()) && (x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead) || x.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead)))
            || ((!x.apparel.layers.NullOrEmpty()) && x.apparel.layers.Any(a => Layers.Contains(a.defName))))).ToList();
            return BodyHeadApparel;
        }
        protected class HATCacheSystem<T> : IDisposable
        {
            private class CacheItem<S>
            {
                public T Value;
                public DateTime? AbsoluteExpiration;
                public TimeSpan? SlidingExpiration;
                public DateTime LastAccessTime;
                public LinkedListNode<string> ListNode;
            }
            private readonly ConcurrentDictionary<string, CacheItem<T>> _cache;
            private readonly LinkedList<string> _accessList = new LinkedList<string>();
            private readonly int _maxCapacity;
            private readonly Timer _cleanupTimer;
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            private long Hits;
            private long Misses;
            private long Evictions;
            private long Expired;

            public HATCacheSystem(int maxCapacity = 1000, TimeSpan? cleanupInterval = null)
            {
                _maxCapacity = maxCapacity;
                _cache = new ConcurrentDictionary<string, CacheItem<T>>();
                TimeSpan interval = cleanupInterval ?? TimeSpan.FromMinutes(5);
                _cleanupTimer = new Timer(CleanExpiredItemsCallback, null, interval, interval);
            }

            public void Set(string key, T value,
                TimeSpan? slidingExpiration = null,
                DateTime? absoluteExpiration = null)
            {
                var now = DateTime.UtcNow;
                var item = new CacheItem<T>
                {
                    Value = value,
                    AbsoluteExpiration = absoluteExpiration,
                    SlidingExpiration = slidingExpiration,
                    LastAccessTime = now
                };

                _lock.EnterWriteLock();
                try
                {
                    if (_cache.TryGetValue(key, out var existing))
                    {
                        if (existing.ListNode != null && existing.ListNode.List == _accessList)
                        {
                            _accessList.Remove(existing.ListNode);
                        }
                    }

                    _cache[key] = item;
                    var newNode = _accessList.AddFirst(key);
                    item.ListNode = newNode;

                    EvictIfNeeded();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public bool TryGet(string key, out T value)
            {
                value = default;
                bool found = false;
                bool expired = false;
                CacheItem<T> item = null;

                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (_cache.TryGetValue(key, out item))
                    {
                        found = true;
                        var now = DateTime.UtcNow;
                        expired = IsExpired(item, now);

                        if (expired)
                        {
                            Interlocked.Increment(ref Expired);
                            Interlocked.Increment(ref Misses);
                        }
                        else
                        {
                            if (item.SlidingExpiration.HasValue)
                            {
                                item.LastAccessTime = now;
                            }

                            _lock.EnterWriteLock();
                            try
                            {
                                if (item.ListNode != null && item.ListNode.List == _accessList)
                                {
                                    _accessList.Remove(item.ListNode);
                                    _accessList.AddFirst(item.ListNode);
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }

                            value = item.Value;
                            Interlocked.Increment(ref Hits);
                            return true;
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
                if (found && expired)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        if (_cache.TryGetValue(key, out item) && IsExpired(item, DateTime.UtcNow))
                        {
                            RemoveItem(key, item);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                if (!found)
                {
                    Interlocked.Increment(ref Misses);
                }
                return false;
            }

            private bool IsExpired(CacheItem<T> item, DateTime now)
            {
                if (item.AbsoluteExpiration.HasValue && item.AbsoluteExpiration.Value < now)
                    return true;

                if (item.SlidingExpiration.HasValue &&
                    item.LastAccessTime.Add(item.SlidingExpiration.Value) < now)
                    return true;

                return false;
            }

            public void Remove(string key)
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_cache.TryRemove(key, out var item))
                    {
                        RemoveItem(key, item);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            private void RemoveItem(string key, CacheItem<T> item)
            {
                if (item.ListNode != null && item.ListNode.List == _accessList)
                {
                    _accessList.Remove(item.ListNode);
                }
            }

            private void EvictIfNeeded()
            {
                if (_cache.Count <= _maxCapacity) return;

                _lock.EnterWriteLock();
                try
                {
                    while (_cache.Count > _maxCapacity && _accessList.Last != null)
                    {
                        var oldestKey = _accessList.Last.Value;
                        if (_cache.TryRemove(oldestKey, out var item))
                        {
                            _accessList.RemoveLast();
                            Interlocked.Increment(ref Evictions);
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            private void CleanExpiredItemsCallback(object state)
            {
                CleanExpiredItems();
            }

            private void CleanExpiredItems()
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                _lock.EnterReadLock();
                try
                {
                    foreach (var kvp in _cache)
                    {
                        if (IsExpired(kvp.Value, now))
                        {
                            expiredKeys.Add(kvp.Key);
                        }
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
                if (expiredKeys.Count > 0)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        foreach (var key in expiredKeys)
                        {
                            if (_cache.TryGetValue(key, out var item) &&
                                IsExpired(item, DateTime.UtcNow))
                            {
                                RemoveItem(key, item);
                                Interlocked.Increment(ref Expired);
                            }
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }

            public void Clear()
            {
                _lock.EnterWriteLock();
                try
                {
                    _cache.Clear();
                    _accessList.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public int Count => _cache.Count;

            public void Dispose()
            {
                _lock?.Dispose();
                _cleanupTimer?.Dispose();
            }
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
        static PatchFWMod patchFWMod = null;
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
            if (HATweakerMod.FWModIndex != -1)
            {
                patchFWMod = new PatchFWMod();
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
            return CanDisplay(ap, pawn);
        }

        private static bool CanDisplay(Apparel ap, Pawn pawn)
        {
            if (HATweakerSetting.SettingData.TryGetValue(ap.def.defName, out HATweakerSetting.HATSettingData data))
            {
                if (!data.ChildrenData.NullOrEmpty() && ap.StyleDef != null && data.ChildrenData.TryGetValue(ap.StyleDef.defName, out HATweakerSetting.HATSettingData data1))
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

            if (HATweakerMod.pawn != null && HATweakerMod.apparel != null && pawn == HATweakerMod.pawn)
            {

                a.RemoveAll(x => HATweakerCache.HeadApparel.Contains(x.def));
                a.Add(HATweakerMod.apparel);
            }

            a.RemoveAll(b => !CanDisplay(b, pawn));
            return a;
        }

        public static List<RenderSkipFlagDef> SetDispalyFlags(List<RenderSkipFlagDef> origin, Apparel apparel, Pawn pawn)
        {
            if ((!HATweakerSetting.WorkOnColonist || HATweakerCache.IsColonist(pawn)) &&
               HATweakerSetting.SettingData.TryGetValue(apparel.def.defName, out HATweakerSetting.HATSettingData data0))
            {
                HATweakerSetting.HATSettingData data;
                if (!data0.UseDefault && !data0.ChildrenData.NullOrEmpty()
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
            if ((!HATweakerSetting.WorkOnColonist || HATweakerCache.IsColonist(parms.pawn)) &&
                (node.Props.workerClass == typeof(PawnRenderNodeWorker_Apparel_Head) && node.children.NullOrEmpty()
            && HATweakerSetting.SettingData.TryGetValue(node.apparel != null ? node.apparel.def.defName : node.Props.debugLabel, out HATweakerSetting.HATSettingData data0)))
            {
                HATweakerSetting.HATSettingData data;
                if (!data0.UseDefault
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
            if ((!HATweakerSetting.WorkOnColonist || HATweakerCache.IsColonist(pawn)) && HATweakerSetting.SettingData.TryGetValue(cloth.def.defName, out HATweakerSetting.HATSettingData data0))
            {
                HATweakerSetting.HATSettingData data;
                if (!data0.UseDefault
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
            if (HATweakerSetting.WorkOnColonist && !HATweakerCache.IsColonist(pawn))
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
                if (HATweakerSetting.WorkOnColonist && !HATweakerCache.IsColonist(pawn))
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