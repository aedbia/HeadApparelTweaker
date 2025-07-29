using ABEasyLib;
using ABEasyLib.ABExtensions;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static HeadApparelTweaker.HATSettingContents;
using static HeadApparelTweaker.HATweakerSetting;
using static Verse.DrawData;

namespace HeadApparelTweaker
{
    public class HATweakerMod : Mod
    {
        public static HATweakerSetting setting;
        private static int HATweakerModIndex = -1;
        internal static int FWModIndex = -1;
        public static int AlienIndex = -1;
        internal static ThingDef choose;
        private Vector2 loc = Vector2.zero;
        private static Rot4 direction = Rot4.South;
        internal static Pawn pawn = null;
        internal static Apparel apparel = null;
        private Vector2 position0 = Vector2.zero;
        private float height0 = 0;
        private static HATweakerSetting.HATSettingData copyData = new HATweakerSetting.HATSettingData();
        private static bool copy = false;
        private static bool ReDrawPawnTexture = true;
        private static Color color;
        private bool reFliter = true;
        private static QuickSearchWidget quickSearch = new QuickSearchWidget();
        private static int TabInt = 0;
        private static ThingStyleDef chooseStyle;
        internal static bool ShowPawnGraphic = false;
        private List<ThingDef> thingDefCache = new List<ThingDef>();
        internal static bool showBodyApparel = true;
        internal static TranslationOfHATSetting allTranslations = null;
        private Vector2 BaScrLoc = Vector2.zero;
        private const float unitH = 120f;
        private List<ScrollViewContent> basicSettingUnits = new List<ScrollViewContent>();
        List<ScrollViewContent> fliterListCache = new List<ScrollViewContent>();

        public HATweakerMod(ModContentPack content) : base(content)
        {
            setting = GetSettings<HATweakerSetting>();
            color = new ColorInt(38, 43, 43).ToColor;
            HATweakerModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod == base.Content);
            FWModIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "aedbia.fashionwardrobe");
            AlienIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces");
            if (AlienIndex == -1)
            {
                AlienIndex = LoadedModManager.RunningModsListForReading.FindIndex(mod => mod.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces.dev");
            }
            Harmony harmony = new Harmony(this.Content.PackageIdPlayerFacing);
            HarmonyPatchA5.PatchAllByHAT(harmony);
            if (AlienIndex != -1)
            {
                new HarmonyPatchA5.HarmonyPatchAlienRace(harmony);
            }
        }

        private static List<TabRecord> tabs = new List<TabRecord>();

        private void NotifyReFliter()
        {
            reFliter = true;
        }

        private void SetTabInt(int i)
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

        public override void DoSettingsWindowContents(Rect inRect)
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
                if (AlienIndex != -1)
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
                case 3 when AlienIndex != -1:
                    tabs[3].selected = true;
                    DrawPatchSettings(inRect);
                    break;

            }
        }

        private void Reset()
        {
            pawn = null;
            apparel = null;
            HATweakerCache.texture = null;
            ReDrawPawnTexture = true;
            reFliter = true;
        }

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

        private void DrawAdvanceSettings(List<ThingDef> origin, Rect inRect)
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
                Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);

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
            if (adjustStyles && def.CanBeStyled() && !data.ChildrenData.NullOrEmpty())
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

                    main1.y += (LabelHeigh + 10f);
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

        private void DrawGlobalSettings(List<ThingDef> list, Rect inRect)
        {
            Rect rect0 = inRect.BottomPart(0.95f);
            Rect one = rect0.TopPart(0.04f);
            one.width /= 3f;
            if (Widgets.ButtonText(one, allTranslations.quickNG))
            {
                QuickSetting(0, true);
            }
            one.x += one.width;
            if (Widgets.ButtonText(one, allTranslations.quickHH))
            {
                QuickSetting(1, true);
            }
            one.x += one.width;
            if (Widgets.ButtonText(one, allTranslations.quickSH))
            {
                QuickSetting(1, false);
            }
            one.x -= 2f * one.width;
            one.width *= 3f;
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHB))
            {
                QuickSetting(2, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSB))
            {
                QuickSetting(2, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHD))
            {
                QuickSetting(3, true);
            }

            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSD))
            {
                QuickSetting(3, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHF))
            {
                QuickSetting(4, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSF))
            {
                QuickSetting(4, false);
            }
            one.y += one.height + 5f;
            if (Widgets.ButtonText(one.LeftHalf(), allTranslations.quickHBed))
            {
                QuickSetting(5, true);
            }
            if (Widgets.ButtonText(one.RightHalf(), allTranslations.quickSBed))
            {
                QuickSetting(5, false);
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
            void QuickSetting(int a, bool on)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ThingDef def = list[i];
                    bool isHeadApparel = HATweakerUtility.IsHeadApparel(def);
                    if (HATweakerSetting.SettingData.TryGetValue(def.defName, out HATSettingData data))
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
                            }
                        }
                        if (!data.ChildrenData.NullOrEmpty())
                        {
                            foreach (var data1 in data.ChildrenData.Values)
                            {
                                if (data1 != null)
                                {
                                    if (a == 0)
                                    {
                                        data1.NoGraphic = on;
                                    }
                                    else
                                    {
                                        data1.NoGraphic = false;
                                        switch (a)
                                        {
                                            case 1:
                                                data1.NoHair = on;
                                                break;
                                            case 2:
                                                data1.NoBeard = on;
                                                break;
                                            case 3:
                                                data1.HideInDoor = on;
                                                break;
                                            case 4:
                                                data1.HideNoFight = on;
                                                break;
                                            case 5:
                                                data1.HideInBed = on;
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                ResolveAllApparelGraphics();
            }
        }

        private void DrawPatchSettings(Rect inRect)
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
            if (dict.Count != 0)
            {
                Rect outRect = new Rect(rect0.x + 5f, rect0.y + LabelHeigh + 10f, rect0.width - 10f, rect0.height - LabelHeigh - 20f);
                Rect viewRect = new Rect(0, 0, outRect.width - 26f, (LabelHeigh + 5f) * dict.Count + 3f);
                Rect rect1 = new Rect(0, 0f, viewRect.width / 4, LabelHeigh);
                Rect iconLoc = new Rect(0, 0, rect1.width * 3, LabelHeigh);
                Widgets.BeginScrollView(outRect, ref this.loc, viewRect, true);
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

        public override string SettingsCategory()
        {
            return base.Content.Name.Translate();
        }

        public override void WriteSettings()
        {
            Reset();
            ResolveAllApparelGraphics();
            base.WriteSettings();
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
            bool isHeadApparel = true;
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
                if (def.CanBeStyled() && !def.RelevantStyleCategories.NullOrEmpty())
                {
                    this.styles = HATweakerUtility.GetStyles(def);
                }
                if (!styles.NullOrEmpty())
                {
                    this.hasStyles = true;
                    this.height = 93 * (styles.Count + 1) + 7f;
                }
                isHeadApparel = HATweakerUtility.IsHeadApparel(def);
                if (tran == null)
                {
                    tran = HATweakerMod.allTranslations;
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
                    }
                }
            }
        }

        public class TranslationOfHATSetting
        {
            internal readonly string BASetting;
            internal readonly string ADVSetting;
            internal readonly string GLOSetting;
            internal readonly string ALISetting;
            internal readonly string useDefault;
            internal readonly string useDefault1;
            internal readonly string style;
            internal readonly string resetChange;
            internal readonly string defaultStr;
            internal readonly string noGraphic;
            internal readonly string noHair;
            internal readonly string noBeard;
            internal readonly string hideDoor;
            internal readonly string hideFight;
            internal readonly string hideBed;
            internal readonly string ADVMode;
            internal readonly string size;
            internal readonly string south;
            internal readonly string north;
            internal readonly string west;
            internal readonly string east;
            internal readonly string rotate;
            internal readonly string layerOffset;
            internal readonly string reset;
            internal readonly string quickNG;
            internal readonly string quickHH;
            internal readonly string quickSH;
            internal readonly string quickHB;
            internal readonly string quickSB;
            internal readonly string quickHD;
            internal readonly string quickSD;
            internal readonly string quickHF;
            internal readonly string quickSF;
            internal readonly string quickHBed;
            internal readonly string quickSBed;
            internal readonly string resetAll;
            internal readonly string GLOLayer;
            internal readonly string onColonist;
            internal readonly string COLCache;
            internal readonly string pgta;

            public TranslationOfHATSetting()
            {
                BASetting = "Basic_Settings".Translate();
                ADVSetting = "Advanced_Settings".Translate();
                GLOSetting = "Global_Settings".Translate();
                ALISetting = "Alien_Patch_Settings".Translate();
                useDefault = "Use_Default0".Translate();
                useDefault1 = "Use_Default1".Translate();
                style = "Style".Translate();
                resetChange = "Reset_Change".Translate();
                defaultStr = "Default".Translate();
                noGraphic = "No_Graphic".Translate();
                noHair = "No_Hair".Translate();
                noBeard = "No_Beard".Translate();
                hideDoor = "Hide_Indoor".Translate();
                hideFight = "Hide_No_Fight".Translate();
                hideBed = "Hide_In_Bed".Translate();
                ADVMode = "Advance_Mode".Translate();
                size = "Size".Translate() + ":";
                south = "South".Translate() + ":";
                north = "North".Translate() + ":";
                west = "West".Translate() + ":";
                east = "East".Translate() + ":";
                rotate = "Rotate".Translate() + ":";
                layerOffset = "Layer_Offset".Translate() + ":";
                reset = "Reset".Translate();
                quickNG = "Quick_NoGraphic".Translate();
                quickHH = "Quick_HideHair".Translate();
                quickSH = "Quick_DisplayHair".Translate();
                quickHB = "Quick_HideBeard".Translate();
                quickSB = "Quick_DisplayBeard".Translate();
                quickHD = "Quick_Open_HideInDoor".Translate();
                quickSD = "Quick_Close_HideInDoor".Translate();
                quickHF = "Quick_Open_HideNoFight".Translate();
                quickSF = "Quick_Close_HideNoFight".Translate();
                quickHBed = "Quick_Open_HideInBed".Translate();
                quickSBed = "Quick_Close_HideInBed".Translate();
                resetAll = "Reset_All_Setting".Translate();
                GLOLayer = "Global_Layer_Offset".Translate();
                onColonist = "Only_Colonist".Translate();
                COLCache = "useIsColonistCache".Translate();
                pgta = "pgta".Translate();
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
        internal static float BaseLayerOffset = 0;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref WorkOnColonist, "WorkOnColonist", true);
            Scribe_Values.Look(ref useIsColonistCache, "useIsColonistCache", false);
            Scribe_Collections.Look(ref WithHair, "WithHair");
            Scribe_Collections.Look(ref WithBeard, "WithBeard");
            ABScribeExtensions.Look(ref BaseLayerOffset, "BaseLayerOffset", 4);
            ABScribeExtensions.LookDeep(ref SettingData, "HATData");
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
                ABScribeExtensions.Look(ref size, "size", 2, Vector2.one);
                ABScribeExtensions.Look(ref SouthOffset, "SouthOffset", 2);
                ABScribeExtensions.Look(ref NorthOffset, "NorthOffset", 2);
                ABScribeExtensions.Look(ref EastOffset, "EastOffset", 2);
                ABScribeExtensions.Look(ref WestOffset, "WestOffset", 2);
                ABScribeExtensions.Look(ref SouthRotation, "SouthRotation", 0);
                ABScribeExtensions.Look(ref NorthRotation, "NorthRotation", 0);
                ABScribeExtensions.Look(ref EastRotation, "EastRotation", 0);
                ABScribeExtensions.Look(ref WestRotation, "WestRotation", 0);
                ABScribeExtensions.Look(ref LayerOffset, "LayerOffset", 4);
            }

            public DrawData GetDrawData(bool reGet = false)
            {
                if (drawData == null || reGet)
                {
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
                        defaultD = HATweakerUtility.isRotationData<RotationalData>(drawDataDefault.GetValue(draw0));
                        north = HATweakerUtility.isRotationData<RotationalData?>(drawDataNorth.GetValue(draw0));
                        east = HATweakerUtility.isRotationData<RotationalData?>(drawDataEast.GetValue(draw0));
                        south = HATweakerUtility.isRotationData<RotationalData?>(drawDataSouth.GetValue(draw0));
                        west = HATweakerUtility.isRotationData<RotationalData?>(drawDataWest.GetValue(draw0));
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
                return new Vector3(offset.x, LayerOffset + BaseLayerOffset, offset.y);
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
                        va.rotationOffset = o + dataRotationValues[headFace.AsInt];
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

    public static class HarmonyPatchA5
    {
        public static float rotate = 0;
        static Type This = typeof(HarmonyPatchA5);
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
                harmony.Patch(setDraft, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(TranSetDrafted)));
                debug.Add("2 Patch DraftedSetter");
            }

            //Hide Under Roof;
            MethodInfo setPosition = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            if (setPosition != null)
            {
                harmony.Patch(setPosition, transpiler: new HarmonyMethod(typeof(HarmonyPatchA5), nameof(TranSetPosition)));
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
                if (HATweakerMod.ShowPawnGraphic && HATweakerMod.pawn == pawn)
                {
                    if (HATweakerMod.showBodyApparel)
                    {
                        dis = ap == HATweakerMod.apparel || (HATweakerCache.HeadApparel.NullOrEmpty() || !HATweakerCache.HeadApparel.Contains(ap.def));
                    }
                    else
                    {
                        dis = ap == HATweakerMod.apparel;
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
            if (Prefs.DevMode&&debug.Count>0)
            {
                Log.Message("[HAT]:Transpiler of AdjustParms finished. Position:"+ string.Join(",",debug));
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
                if (HATweakerMod.ShowPawnGraphic && HATweakerMod.apparel != apparel)
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatchA5), nameof(IsPositionChange)));
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
                Map map = pawn.Map ?? Current.Game?.CurrentMap;
                if (map == null || !ApplyGraphicData(pawn))
                {
                    return;
                }
                if (ago.InBounds(map) && now.InBounds(map))
                {
                    bool a = ago.UsesOutdoorTemperature(map);
                    bool b = now.UsesOutdoorTemperature(map);
                    if (a != b)
                    {
                        pawn.apparel.Notify_ApparelChanged();
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