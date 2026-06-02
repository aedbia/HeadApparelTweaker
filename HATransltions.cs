using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HeadApparelTweaker
{
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
        internal readonly string hideNonVacuum;
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
        internal readonly string quickHV;
        internal readonly string quickSV;
        internal readonly string quickHBed;
        internal readonly string quickSBed;
        internal readonly string resetAll;
        internal readonly string GLOLayer;
        internal readonly string onColonist;
        internal readonly string COLCache;
        internal readonly string pgta;
        internal readonly string EnableExperimentalTip;
        internal readonly string EnableExperimental;

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
            hideNonVacuum = "Hide_Non_Vacuum".Translate();
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
            quickHV = "Quick_Open_HideNonVacuum".Translate();
            quickSV = "Quick_Close_HideNonVacuum".Translate();
            resetAll = "Reset_All_Setting".Translate();
            GLOLayer = "Global_Layer_Offset".Translate();
            onColonist = "Only_Colonist".Translate();
            COLCache = "useIsColonistCache".Translate();
            pgta = "pgta".Translate();
            EnableExperimentalTip = "Enable_Experimental_Tip".Translate();
            EnableExperimental = "Enable_Experimental".Translate();
        }
    }
}
