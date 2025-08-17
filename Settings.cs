using ABEasyLib.ABExtensions;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static Verse.DrawData;

namespace HeadApparelTweaker
{
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
            public bool HideNonVacuum = false;
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
                Scribe_Values.Look(ref HideNonVacuum, "HideNonVacuum", false);
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
                    var map = pawn.MapHeld;
                    IntVec3 pos = pawn.Position;
                    bool canWork = map != null && pos.InBounds(map);
                    if (ModsConfig.OdysseyActive && HideNonVacuum && canWork && pos.GetVacuum(map) >= 0.5f)
                    {
                        return true;
                    }
                    if (HideInDoor)
                    {
                        return canWork && pos.UsesOutdoorTemperature(map);
                    }
                    return !(HideInDoor || HideNoFight || HideNonVacuum);
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
}
