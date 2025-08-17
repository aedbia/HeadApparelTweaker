using ABEasyLib;
using ABEasyLib.ABExtensions;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static HeadApparelTweaker.HATweakerSetting;

namespace HeadApparelTweaker
{
    public static class SettingsWindowContents
    {
        internal static ThingDef choose;
        private static Vector2 loc = Vector2.zero;
        private static Rot4 direction = Rot4.South;
        internal static Pawn pawn = null;
        internal static Apparel apparel = null;
        private static Vector2 position0 = Vector2.zero;
        private static float height0 = 0;
        private static HATweakerSetting.HATSettingData copyData = new HATweakerSetting.HATSettingData();
        private static bool copy = false;
        private static bool ReDrawPawnTexture = true;
        private static Color color = new ColorInt(38, 43, 43).ToColor;
        private static bool reFliter = true;
        private static QuickSearchWidget quickSearch = new QuickSearchWidget();
        private static int TabInt = 0;
        private static ThingStyleDef chooseStyle;
        internal static bool ShowPawnGraphic = false;
        private static List<ThingDef> thingDefCache = new List<ThingDef>();
        internal static bool showBodyApparel = true;
        internal static TranslationOfHATSetting allTranslations = null;
        private static Vector2 BaScrLoc = Vector2.zero;
        private const float unitH = 120f;
        private static List<ScrollViewContent> basicSettingUnits = new List<ScrollViewContent>();
        private static List<ScrollViewContent> fliterListCache = new List<ScrollViewContent>();
        private static List<TabRecord> tabs = new List<TabRecord>();
        private static void NotifyReFliter()
        {
            reFliter = true;
        }

        private static void SetTabInt(int i)
        {
            TabInt = i;
            Reset();
            if (i > tabs.Count - 1)
            {
                return;
            }
            for (int a = 0; a < tabs.Count; a++)
            {
                if (a == i)
                {
                    tabs[a].selected = true;
                }
                else
                {
                    tabs[a].selected = false;
                }
            }
        }
        public static void DoSettingsWindowContents(Rect inRect)
        {
            if (allTranslations == null)
            {
                allTranslations = new TranslationOfHATSetting();
            }
            List<ThingDef> list = HATweakerCache.HeadApparel;
            if (list.NullOrEmpty())
            {
                return;
            }

            if (choose == null)
            {
                choose = list.First();
                ReDrawPawnTexture = true;
            }
            float height = inRect.height * 0.04f;
            Rect searchRect = new Rect(inRect.x, inRect.y, inRect.width * 0.3f, height);
            Rect tabsRect = new Rect(inRect.x + inRect.width * 0.31f, inRect.y + height, inRect.width * 0.69f, height);
            quickSearch.OnGUI(searchRect, NotifyReFliter, NotifyReFliter);
            if (tabs == null)
            {
                tabs = new List<TabRecord>();
            }
            if (tabs.Empty())
            {
                tabs.Add(new TabRecord(allTranslations.BASetting, () =>
                {
                    SetTabInt(0);
                }, TabInt == 0));
                tabs.Add(new TabRecord(allTranslations.ADVSetting, () =>
                {
                    SetTabInt(1);
                }, TabInt == 1));
                tabs.Add(new TabRecord(allTranslations.GLOSetting, () =>
                {
                    SetTabInt(2);
                }, TabInt == 2));
                if (HATweakerMod.AlienIndex != -1)
                {
                    tabs.Add(new TabRecord(allTranslations.ALISetting, () =>
                    {
                        SetTabInt(3);
                    }, TabInt == 3));
                }
            }
            TabDrawer.DrawTabs(tabsRect, tabs);
            switch (TabInt)
            {
                case 0:
                    tabs[0].selected = true;
                    DrawBasicSettings(list, inRect);
                    break;
                case 1:
                    tabs[1].selected = true;
                    DrawAdvanceSettings(list, inRect);
                    break;
                case 2:
                    tabs[2].selected = true;
                    DrawGlobalSettings(list, inRect);
                    break;
                case 3 when HATweakerMod.AlienIndex != -1:
                    tabs[3].selected = true;
                    DrawPatchSettings(inRect);
                    break;

            }
        }
        private static void DrawBasicSettings(List<ThingDef> list, Rect inRect)
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
                    basicSettingUnits.Add(new BasicSettingUnit(rect0.width, unitH, def));
                }
            }
            if (reFliter)
            {
                fliterListCache = basicSettingUnits.Where(c => quickSearch.filter.Matches(c.displayName)).ToList();
            }
            if (fliterListCache.NullOrEmpty())
            {
                quickSearch.noResultsMatched = true;
            }
            else
            {
                quickSearch.noResultsMatched = false;
                ABWidgetsExtensions.DrawScrollPanel(rect0.ContractedBy(3f), fliterListCache, ref BaScrLoc);
            }

        }

        private static void DrawAdvanceSettings(List<ThingDef> origin, Rect inRect)
        {
            float LabelHeigh = 30f;
            float itemHeight = LabelHeigh + 5f;
            string useDefault = allTranslations.useDefault;
            string useDefault1 = allTranslations.useDefault1;
            bool adjustStyles = ModsConfig.IdeologyActive;
            Rect rect0 = inRect.BottomPart(0.95f);
            if (origin.NullOrEmpty())
            {
                return;
            }
            if (reFliter)
            {
                thingDefCache = origin.Where(c => quickSearch.filter.Matches(c.label)).ToList();
            }
            Rect outRect = rect0.LeftPart(0.3f);
            Widgets.DrawWindowBackground(outRect);
            GUIStyle labelStyle = HATweakerUtility.GetLabelStyle(TextAnchor.MiddleCenter);

            if (!thingDefCache.NullOrEmpty())
            {
                Rect viewRect = new Rect(-3f, -3f, outRect.width - 26f, itemHeight * thingDefCache.Count + 3f);
                Widgets.BeginScrollView(outRect, ref loc, viewRect, true);

                // 计算可见范围
                int startIndex = Mathf.FloorToInt(loc.y / itemHeight);
                int visibleCount = Mathf.CeilToInt(outRect.height / itemHeight) + 1;
                int endIndex = Mathf.Min(startIndex + visibleCount, thingDefCache.Count);

                for (int idx = startIndex; idx < endIndex; idx++)
                {
                    ThingDef c = thingDefCache[idx];
                    float yPos = idx * itemHeight;
                    Rect rect1 = new Rect(LabelHeigh + 5f, yPos, outRect.width - 60f, LabelHeigh);
                    Rect rect2 = new Rect(0f, yPos, LabelHeigh, LabelHeigh);

                    Widgets.DrawHighlightIfMouseover(rect1);
                    if (Widgets.RadioButtonLabeled(rect1, c.label, choose == c))
                    {
                        choose = c;
                        chooseStyle = null;
                        if (apparel != null && apparel.def == c)
                        {
                            apparel.SetStyleDef(null);
                        }
                        ReDrawPawnTexture = true;
                    }
                    Widgets.DrawBox(rect2);
                    if (c.uiIcon != null)
                        GUI.DrawTexture(rect2, c.uiIcon);
                }
                Widgets.EndScrollView();
                quickSearch.noResultsMatched = false;
            }
            else
            {
                quickSearch.noResultsMatched = true;
            }
            if (choose == null) return;
            ThingDef def = choose;
            HATweakerSetting.SingleInit(def);
            bool isHeadApparel = HATweakerUtility.IsHeadApparel(def);
            HATweakerSetting.HATSettingData data = HATweakerSetting.SettingData[choose.defName];
            Rect main = rect0.RightPart(0.69f);
            Widgets.DrawWindowBackground(main);
            List<ThingStyleDef> styles = null;
            if (ModsConfig.IdeologyActive && adjustStyles && def.CanBeStyled() && !data.ChildrenData.NullOrEmpty())
            {
                styles = HATweakerUtility.GetStyles(def);
            }
            Widgets.BeginScrollView(main.LeftHalf(), ref position0, new Rect(0, 0, main.width / 2 - 18f, height0));
            Rect main1 = new Rect(5f, 5f, main.width / 2 - 28f, LabelHeigh);
            if (!styles.NullOrEmpty())
            {
                Widgets.DrawBoxSolid(main1, color);
                GUI.Label(main1, allTranslations.style, labelStyle);
                main1.y += LabelHeigh;
                bool useD = data.UseDefault;
                Widgets.CheckboxLabeled(main1, useDefault, ref data.UseDefault);
                if (useD != data.UseDefault)
                {
                    ReDrawPawnTexture = true;
                }
                main1.y += LabelHeigh + 10f;
                Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);

                Widgets.DrawHighlightIfMouseover(main1);
                if (chooseStyle == null)
                {
                    Widgets.DrawHighlightSelected(main1);
                }
                labelStyle.alignment = TextAnchor.MiddleRight;
                Rect iconL = new Rect(main1.x, main1.y, main1.height, main1.height);
                Rect labelR = new Rect(main1.x + main1.height, main1.y, main1.width - main1.height, main1.height);
                GUI.DrawTexture(iconL, def.uiIcon);
                GUI.Label(labelR, allTranslations.defaultStr, labelStyle);
                if (Widgets.ButtonInvisible(main1))
                {
                    chooseStyle = null;
                    apparel.SetStyleDef(null);
                    ReDrawPawnTexture = true;
                }
                main1.y += LabelHeigh;
                iconL.y += LabelHeigh;
                labelR.y += LabelHeigh;

                for (int i = 0; i < styles.Count; i++)
                {
                    ThingStyleDef styleDef = styles[i];
                    Widgets.DrawHighlightIfMouseover(main1);
                    if (chooseStyle == styleDef)
                    {
                        Widgets.DrawHighlightSelected(main1);
                    }
                    GUI.DrawTexture(iconL, styleDef.UIIcon);
                    string a = styleDef.label.NullOrEmpty() ? styleDef.defName : styleDef.label;
                    GUI.Label(labelR, a, labelStyle);
                    if (Widgets.ButtonInvisible(main1))
                    {
                        chooseStyle = styleDef;
                        if (apparel != null && apparel.def == choose)
                        {
                            apparel.SetStyleDef(styleDef);
                        }
                        ReDrawPawnTexture = true;
                    }
                    main1.y += LabelHeigh;
                    iconL.y += LabelHeigh;
                    labelR.y += LabelHeigh;
                }
                if (chooseStyle != null && data.ChildrenData.TryGetValue(chooseStyle.defName, out HATweakerSetting.HATSettingData data1))
                {
                    data = data1;
                }
                labelStyle.alignment = TextAnchor.MiddleCenter;
                main1.y += 10f;
                Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
            }
            Widgets.DrawBoxSolid(main1, color);
            GUI.Label(main1, allTranslations.BASetting, labelStyle);
            main1.y += LabelHeigh;
            if (adjustStyles && chooseStyle != null)
            {
                bool useD = data.UseDefault;
                Widgets.CheckboxLabeled(main1, useDefault1, ref data.UseDefault);
                if (useD != data.UseDefault)
                {
                    ReDrawPawnTexture = true;
                }
                main1.y += LabelHeigh + 10f;
                Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
            }
            LabelHeigh -= 3f;
            main1.height -= 3f;
            if (isHeadApparel)
            {
                Widgets.DrawHighlightIfMouseover(main1);
                if (Widgets.RadioButtonLabeled(main1, allTranslations.noGraphic, data.NoGraphic))
                {
                    data.NoGraphic = !data.NoGraphic;
                    ReDrawPawnTexture = true;
                }
            }
            if (!data.NoGraphic)
            {
                if (isHeadApparel)
                {
                    main1.y += LabelHeigh;
                }
                Widgets.DrawHighlightIfMouseover(main1);
                if (Widgets.RadioButtonLabeled(main1, allTranslations.noHair, data.NoHair))
                {
                    data.NoHair = !data.NoHair;
                    ReDrawPawnTexture = true;
                }
                main1.y += LabelHeigh;
                Widgets.DrawHighlightIfMouseover(main1);
                if (Widgets.RadioButtonLabeled(main1, allTranslations.noBeard, data.NoBeard))
                {
                    data.NoBeard = !data.NoBeard;
                    ReDrawPawnTexture = true;
                }
                if (isHeadApparel)
                {
                    main1.y += LabelHeigh;
                    Widgets.DrawHighlightIfMouseover(main1);

                    if (Widgets.RadioButtonLabeled(main1, allTranslations.hideDoor, data.HideInDoor))
                    {
                        data.HideInDoor = !data.HideInDoor;
                        ReDrawPawnTexture = true;
                    }
                    main1.y += LabelHeigh;
                    Widgets.DrawHighlightIfMouseover(main1);
                    if (Widgets.RadioButtonLabeled(main1, allTranslations.hideFight, data.HideNoFight))
                    {
                        data.HideNoFight = !data.HideNoFight;
                        ReDrawPawnTexture = true;
                    }
                    main1.y += LabelHeigh;
                    Widgets.DrawHighlightIfMouseover(main1);
                    if (Widgets.RadioButtonLabeled(main1, allTranslations.hideBed, data.HideInBed))
                    {
                        data.HideInBed = !data.HideInBed;
                        ReDrawPawnTexture = true;
                    }
                    if (Widgets.RadioButtonLabeled(main1, allTranslations.hideBed, data.HideInBed))
                    {
                        data.HideInBed = !data.HideInBed;
                        ReDrawPawnTexture = true;
                    }
                    main1.y += LabelHeigh;
                    if (ModsConfig.OdysseyActive)
                    {
                        Widgets.DrawHighlightIfMouseover(main1);
                        if (Widgets.RadioButtonLabeled(main1, allTranslations.hideNonVacuum, data.HideNonVacuum))
                        {
                            data.HideNonVacuum = !data.HideNonVacuum;
                            ReDrawPawnTexture = true;
                        }
                        main1.y += (LabelHeigh + 10f);
                    }
                    else
                    {
                        main1.y += 10f;
                    }
                    Widgets.DrawLineHorizontal(main1.x, main1.y - 5f, main1.width);
                    LabelHeigh += 3f;
                    main1.height += 3f;
                    Widgets.DrawBoxSolid(main1, color);
                    bool adv = data.AdvanceMode;
                    Widgets.CheckboxLabeled(main1, allTranslations.ADVMode, ref data.AdvanceMode);
                    if (adv != data.AdvanceMode)
                    {
                        ReDrawPawnTexture = true;
                    }
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
                            ReDrawPawnTexture = true;
                        }
                        ;
                        Rect rectAdj = new Rect(main1.x, main1.y + main1.height + 3f, main1.width, (main1.height + 10) * 3);
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.size + data.size.ToString("F2"), ref data.size.x, ref data.size.y, 0.5f, 2, 0.01f, () => data.size = Vector2.one))
                        {
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;

                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.south + data.SouthOffset.ToString("F2"), ref data.SouthOffset.x, ref data.SouthOffset.y, -1, 1, 0.01f, () => data.SouthOffset = Vector2.zero))
                        {
                            data.SetOffset(Rot4.South);
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.north + data.NorthOffset.ToString("F2"), ref data.NorthOffset.x, ref data.NorthOffset.y, -1, 1, 0.01f, () => data.NorthOffset = Vector2.zero))
                        {
                            data.SetOffset(Rot4.North);
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.west + data.WestOffset.ToString("F2"), ref data.WestOffset.x, ref data.WestOffset.y, -1, 1, 0.01f, () => data.WestOffset = Vector2.zero))
                        {
                            data.SetOffset(Rot4.West);
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.east + data.EastOffset.ToString("F2"), ref data.EastOffset.x, ref data.EastOffset.y, -1, 1, 0.01f, () => data.EastOffset = Vector2.zero))
                        {
                            data.SetOffset(Rot4.East);
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.rotate + allTranslations.south + "[" + data.SouthRotation.ToString("0") + "]" + allTranslations.north + "[" + data.NorthRotation.ToString("0") + "]", ref data.SouthRotation, ref data.NorthRotation, -180, 180, 1, () =>
                        {
                            data.SouthRotation = 0f;
                            data.NorthRotation = 0f;
                            ReDrawPawnTexture = true;
                        }))
                        {
                            data.SetRotation(Rot4.South);
                            data.SetRotation(Rot4.North);
                            ReDrawPawnTexture = true;
                        }
                        rectAdj.y += rectAdj.height + 2f;
                        if (ABWidgetsExtensions.DrawAdjust(rectAdj, allTranslations.rotate + allTranslations.east + "[" + data.EastRotation.ToString("0") + "]" + allTranslations.west + "[" + data.WestRotation.ToString("0") + "]", ref data.EastRotation, ref data.WestRotation, -180, 180, 1, () =>
                        {
                            data.EastRotation = 0f;
                            data.WestRotation = 0f;
                        }))
                        {
                            data.SetRotation(Rot4.East);
                            data.SetRotation(Rot4.West);
                            ReDrawPawnTexture = true;
                        }
                        main1.height = rectAdj.height * 0.4f;
                        main1.y = rectAdj.y + rectAdj.height + 2f;
                        Widgets.Label(main1.LeftPart(0.7f), allTranslations.layerOffset + data.LayerOffset.ToString("f4"));
                        bool work = false;
                        if (Widgets.ButtonText(main1.RightPart(0.3f).TopHalf(), allTranslations.reset))
                        {
                            data.LayerOffset = 0f;
                            work = true;
                        }
                        main1.height = rectAdj.height * 0.3f;
                        main1.y += (main1.height + 2f);
                        if (ABWidgetsExtensions.HorizontalSlider(main1, ref data.LayerOffset, -0.03f, 0.03f, 0.0001f))
                        {
                            work = true;
                        }
                        if (work)
                        {
                            data.GetDrawData(true);
                            ReDrawPawnTexture = true;
                        }
                    }
                }
            }
            Widgets.EndScrollView();
            LabelHeigh = 30f;
            height0 = main1.y + main1.height + 5f;
            Rect main2 = main.RightHalf();
            Rect main3 = new Rect(main2.x, main2.y, main2.width, main2.height - LabelHeigh - 5f);
            Rect two = new Rect(main2.x, main2.y + main2.height - LabelHeigh, main2.width, LabelHeigh);
            Widgets.DrawWindowBackground(main3);
            List<Pawn> Colonists = new List<Pawn>();
            if (Current.Game != null && Current.Game.CurrentMap != null && Current.Game.CurrentMap.mapPawns != null)
            {
                Colonists = Current.Game.CurrentMap.mapPawns.FreeColonists;
            }
            if (!Colonists.NullOrEmpty())
            {
                if (pawn == null)
                {
                    pawn = Colonists.FirstOrDefault();
                }
                if (pawn != null)
                {
                    if (Widgets.ButtonText(main3.TopPart(0.05f), pawn.Name.ToStringFull))
                    {
                        List<FloatMenuOption> Options = new List<FloatMenuOption>();
                        for (int i = 0; i < Colonists.Count; i++)
                        {
                            Pawn pa = Colonists[i];
                            string now = "";
                            if (pa == pawn)
                            {
                                now = "(Now)".Translate();
                            }
                            Options.Add(new FloatMenuOption(pa.Name.ToStringShort + now, () => { pawn = pa; ReDrawPawnTexture = true; }));
                        }
                        Find.WindowStack.Add(new FloatMenu(Options));
                    }
                    if (choose != null)
                    {
                        if (apparel == null || apparel.def != choose)
                        {
                            apparel = HATweakerUtility.NewApparel(choose);
                            apparel.SetStyleDef(chooseStyle);
                        }
                        if (apparel != null && ReDrawPawnTexture)
                        {
                            try
                            {
                                ShowPawnGraphic = true;
                                HATweakerUtility.DrawPawnCacheWithApparel(pawn, apparel, new Vector2(main2.width, main2.height), direction, out HATweakerCache.texture);
                                ShowPawnGraphic = false;
                            }
                            catch (Exception e)
                            {
                                Log.Error(e.Message);
                            }
                            finally
                            {
                                if (HATweakerCache.texture != null)
                                {
                                    ReDrawPawnTexture = false;
                                }
                            }
                        }
                        Rect main4 = two;
                        float cha = main3.height * 0.05f;
                        if (HATweakerCache.texture != null
                            && Widgets.ButtonImage(new Rect(main3.x + main3.width - cha - 1f, main3.y + cha + 1f, cha, cha), HATweakerCache.modUI, tooltip: "Show_Body_Apparel".Translate()))
                        {
                            ReDrawPawnTexture = true;
                            showBodyApparel = !showBodyApparel;
                        }
                        if (HATweakerCache.texture != null)
                        {
                            GUI.DrawTexture(main3.BottomPart(0.95f), HATweakerCache.texture);
                        }
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
                            ReDrawPawnTexture = true;
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
                            ReDrawPawnTexture = true;
                        }
                        two = main4.RightPart(0.66f).LeftHalf();
                    }
                }
            }
            else
            {
                pawn = null;
                HATweakerCache.texture = null;
                GUI.Label(main3, "Into_Game".Translate(), labelStyle);
            }
            if (Mouse.IsOver(two))
            {
                TooltipHandler.TipRegion(two, "Reset_Change_Tooltip".Translate());
            }
            if (Widgets.ButtonText(two, allTranslations.resetChange))
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
                ReDrawPawnTexture = true;
            }

        }

        private static void DrawGlobalSettings(List<ThingDef> list, Rect inRect)
        {
            Rect rect0 = inRect.BottomPart(0.95f);
            Rect one = rect0.TopPart(0.04f);
            one.width /= 3f;
            if (Widgets.ButtonText(one, allTranslations.quickNG))
            {
                QuickSetting(list, 0, true);
            }
            one.x += one.width;
            if (Widgets.ButtonText(one, allTranslations.quickHH))
            {
                QuickSetting(list, 1, true);
            }
            one.x += one.width;
            if (Widgets.ButtonText(one, allTranslations.quickSH))
            {
                QuickSetting(list, 1, false);
            }
            one.x -= 2f * one.width;
            one.width *= 3f;
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHB))
            {
                QuickSetting(list, 2, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSB))
            {
                QuickSetting(list, 2, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHD))
            {
                QuickSetting(list, 3, true);
            }

            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSD))
            {
                QuickSetting(list, 3, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHF))
            {
                QuickSetting(list, 4, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSF))
            {
                QuickSetting(list, 4, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHBed))
            {
                QuickSetting(list, 5, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSBed))
            {
                QuickSetting(list, 5, false);
            }
            if (ModsConfig.OdysseyActive)
            {
                one.y += one.height + 5f;
                if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHV))
                {
                    QuickSetting(list, 6, true);
                }
                if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSV))
                {
                    QuickSetting(list, 6, false);
                }
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one, allTranslations.resetAll))
            {
                HATweakerSetting.SettingData.Clear();
                HATweakerSetting.InitSetting();
            }
            one.y += one.height + 5f;
            Widgets.CheckboxLabeled(one, allTranslations.onColonist, ref HATweakerSetting.WorkOnColonist);
            one.y += one.height + 5f;
            Widgets.CheckboxLabeled(one, allTranslations.COLCache, ref HATweakerSetting.useIsColonistCache);
            one.y += one.height + 5f;
            Widgets.Label(one, allTranslations.GLOLayer + ":" + HATweakerSetting.BaseLayerOffset.ToString("f4"));
            one.y += one.height + 5f;
            if (ABWidgetsExtensions.HorizontalSlider(one, ref HATweakerSetting.BaseLayerOffset, -0.03f, 0.03f, 0.0001f))
            {
                try
                {
                    foreach (var data in HATweakerSetting.SettingData.Values)
                    {
                        if (data == null)
                        {
                            continue;
                        }
                        data.GetDrawData(true);
                        if (data.ChildrenData.NullOrEmpty())
                        {
                            continue;
                        }
                        foreach (var child in data.ChildrenData.Values)
                        {
                            if (child != null)
                            {
                                child.GetDrawData(true);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }

            }
        }

        private static void QuickSetting(List<ThingDef> list, int a, bool on)
        {
            for (int i = 0; i < list.Count; i++)
            {
                ThingDef def = list[i];
                bool isHeadApparel = HATweakerUtility.IsHeadApparel(def);
                if (HATweakerSetting.SettingData.TryGetValue(def.defName, out HATSettingData data))
                {
                    QuickSetSingleData(ref data, a, on, isHeadApparel);
                    if (!data.ChildrenData.NullOrEmpty())
                    {
                        var datas = data.ChildrenData.Values.GetEnumerator();
                        while (datas.MoveNext())
                        {
                            HATSettingData data1 = datas.Current;
                            QuickSetSingleData(ref data1, a, on, isHeadApparel);
                        }
                    }
                }
            }
            ResolveAllApparelGraphics();
        }

        private static void QuickSetSingleData(ref HATSettingData data, int a, bool on, bool isHeadApparel)
        {
            if (a == 0)
            {
                if (isHeadApparel)
                {
                    data.NoGraphic = on;
                }
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
                    case 6:
                        data.HideNonVacuum = on;
                        break;
                }
            }
        }

        private static void DrawPatchSettings(Rect inRect)
        {
            float LabelHeigh = 30f;
            Rect rect0 = inRect.BottomPart(0.95f);
            Dictionary<int, string> dict = HATweakerCache.alienCompatible.AllAddons;
            var textures = HATweakerCache.alienCompatible.textures;
            bool hasTextures = !textures.NullOrEmpty();
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
            if (dict != null && dict.Count != 0)
            {
                Rect outRect = new Rect(rect0.x + 5f, rect0.y + LabelHeigh + 10f, rect0.width - 10f, rect0.height - LabelHeigh - 20f);
                Rect viewRect = new Rect(0, 0, outRect.width - 26f, (LabelHeigh + 5f) * dict.Count + 3f);
                Rect rect1 = new Rect(0, 0f, viewRect.width / 4, LabelHeigh);
                Rect iconLoc = new Rect(0, 0, rect1.width * 3, LabelHeigh);
                Widgets.BeginScrollView(outRect, ref loc, viewRect, true);
                int index = 0;
                foreach (int dictKey in dict.Keys)
                {
                    string disc = dict[dictKey];
                    if (quickSearch.filter.Matches(disc))
                    {
                        index++;
                        if (hasTextures && textures.TryGetValue(dictKey, out var tex) && !tex.NullOrEmpty())
                        {
                            if (Mouse.IsOver(iconLoc))
                            {
                                Vector2 v;
                                int co = tex.Count();
                                if (co > 2)
                                {
                                    v = new Vector2(200, 200);
                                }
                                else if (co == 1)
                                {
                                    v = new Vector2(100, 100);
                                }
                                else
                                {
                                    v = new Vector2(200, 100);
                                }
                                ABWidgetsExtensions.DrawTooltipsGrid(tex.ToArray(), v, 2);
                            }
                        }
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
                        rect1.x = 0;
                        rect1.y += (LabelHeigh + 5f);
                        iconLoc.y += (LabelHeigh + 5f);
                    }
                }
                if (index == 0)
                {
                    quickSearch.noResultsMatched = true;
                }
                else
                {
                    quickSearch.noResultsMatched = false;
                }
            }
            Widgets.EndScrollView();
        }
        private static void Reset()
        {
            pawn = null;
            apparel = null;
            HATweakerCache.texture = null;
            ReDrawPawnTexture = true;
            reFliter = true;
        }
        public static void WriteSettings()
        {
            Reset();
            ResolveAllApparelGraphics();
            allTranslations = new TranslationOfHATSetting();
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

    public class BasicSettingUnit : ScrollViewContent
    {
        private float height = 135f;
        public override float Height => height;

        ThingDef def;
        float unitHeight = 30f;
        List<ThingStyleDef> styles;
        bool hasStyles = false;
        bool isHeadApparel = true;
        int baseUnitCount = 3;
        static readonly Color color0 = new ColorInt(40, 48, 48).ToColor;
        static readonly Color color1 = new ColorInt(32, 32, 32).ToColor;
        static readonly Color color2 = new ColorInt(16, 16, 16).ToColor;
        static readonly GUIStyle TextMidCenter = ABEasyUtility.GetTextStyle(TextAnchor.MiddleCenter);
        static TranslationOfHATSetting tran = null;
        public BasicSettingUnit(float width, float height, ThingDef def) : base(width, height, def.defName, def.label)
        {
            this.def = def;
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }
            if (ModsConfig.OdysseyActive)
            {
                baseUnitCount = 4;
            }
            height = (unitHeight + 5) * baseUnitCount - 5f;
            if (def.CanBeStyled() && !def.RelevantStyleCategories.NullOrEmpty())
            {
                this.styles = HATweakerUtility.GetStyles(def);
            }
            if (ModsConfig.IdeologyActive && !styles.NullOrEmpty())
            {
                this.hasStyles = true;
                this.height = (unitHeight * baseUnitCount + 3f) * (styles.Count + 1) + 7f;
            }

            isHeadApparel = HATweakerUtility.IsHeadApparel(def);
            if (tran == null)
            {
                tran = SettingsWindowContents.allTranslations;
            }
        }
        public override void DrawContect(Rect inRect)
        {
            Widgets.DrawBox(inRect);
            Rect rect = inRect.ContractedBy(5f);
            HATweakerSetting.SingleInit(def);
            float of = rect.width * 0.1f;
            Rect cr = new Rect(rect.x + of, rect.y, rect.width - of, 4 * unitHeight);
            Rect tr = new Rect(rect.x, rect.y, of, rect.height);
            Widgets.DrawBoxSolid(tr, color2);
            GUI.Label(tr, def.label, TextMidCenter);
            if (HATweakerSetting.SettingData.TryGetValue(def.defName, out var data))
            {
                //GUI.Label(cr, def.label, TextMidCenter);
                DrawBasicDataSettings(ref data, cr, tran.defaultStr, def.uiIcon);
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
                            bool disable = data.UseDefault || styleData.UseDefault;
                            if (disable)
                            {
                                TooltipHandler.TipRegion(cr, tran.pgta);
                            }
                            DrawBasicDataSettings(ref styleData, cr, style.label.NullOrEmpty() ? style.defName : style.label, style.UIIcon, disable);
                            cr.y += add;
                        }
                    }
                }
            }
        }

        private void DrawBasicDataSettings(ref HATweakerSetting.HATSettingData data, Rect rt, string label = "Default", Texture icon = null, bool disable = false)
        {
            Widgets.DrawBoxSolidWithOutline(rt, color1, color0);
            float uh = rt.height / baseUnitCount;
            bool od = baseUnitCount == 4;
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
            for (int i = 2; i < baseUnitCount; i++)
            {
                Widgets.DrawLineHorizontal(rt.x, rt.y + uh * i, rt.width, color0);
            }
            Rect vl = new Rect(rt.x + optionWidth, optionRect.y, 1f, (baseUnitCount - 1) * uh);
            Widgets.DrawBoxSolid(vl, color0);
            vl.x += optionWidth;
            Widgets.DrawBoxSolid(vl, color0);
            if (isHeadApparel)
            {
                Widgets.DrawHighlightIfMouseover(optionRect);
                if (Widgets.RadioButtonLabeled(optionRect, tran.noGraphic, data.NoGraphic, disable))
                {
                    data.NoGraphic = !data.NoGraphic;
                }
            }

            if (!data.NoGraphic)
            {
                if (isHeadApparel)
                {
                    optionRect.x += optionWidth;
                }
                Widgets.DrawHighlightIfMouseover(optionRect);
                if (Widgets.RadioButtonLabeled(optionRect, tran.noHair, data.NoHair, disable))
                {
                    data.NoHair = !data.NoHair;
                }
                if (isHeadApparel)
                {
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, tran.hideFight, data.HideNoFight, disable))
                    {
                        data.HideNoFight = !data.HideNoFight;
                    }
                    optionRect.x = rt.x + 10f;
                }
                optionRect.y += optionRect.height;
                Widgets.DrawHighlightIfMouseover(optionRect);
                if (Widgets.RadioButtonLabeled(optionRect, tran.noBeard, data.NoBeard, disable))
                {
                    data.NoBeard = !data.NoBeard;
                }
                if (isHeadApparel)
                {
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, tran.hideDoor, data.HideInDoor, disable))
                    {
                        data.HideInDoor = !data.HideInDoor;
                    }
                    optionRect.x += optionWidth;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, tran.hideBed, data.HideInBed, disable))
                    {
                        data.HideInBed = !data.HideInBed;
                    }
                    optionRect.x = rt.x + 10f;
                    optionRect.y += optionRect.height;
                    Widgets.DrawHighlightIfMouseover(optionRect);
                    if (Widgets.RadioButtonLabeled(optionRect, tran.hideNonVacuum, data.HideNonVacuum, disable))
                    {
                        data.HideNonVacuum = !data.HideNonVacuum;
                    }
                }
            }
        }
    }
}
