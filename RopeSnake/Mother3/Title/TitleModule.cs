using RopeSnake.Core;
using RopeSnake.Core.Validation;
using SharpFileSystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
8057040
8057080
80570E4
80571D0
8057248
8057284
80572E4
8057340
805737C
Default ASM Pointers of title

8126D98
813C60C
ASM Pointers to Disclaimer

8126DF0
813C664
ASM Pointers to Disclaimer's Palette

805AFC4
Default ASM Pointer of GBA Player Palettes

805AFB4
Default ASM Pointer of GBA Player Graphics

805AFBC
Default ASM Pointer of GBA Player Arrangement

805A61C
Default ASM Pointers of Health Screen
*/
namespace RopeSnake.Mother3.Title
{
    public class Frames
    {
       public int PauseFrames;
    }
    [Validate]
    public class TitleModule : Mother3Module
    {
        public static List<Byte[]> Final;
        public static List<Bitmap> Logo;
        public static List<Bitmap> Menu_Options;
        public static Bitmap Menu_Options_Palettes;
        public static Bitmap Numbers;
        public static List<Bitmap> Health;
        public static Bitmap Disclaimer;
        public static Bitmap GBAPlayer;
        private static TitleSettings Title_Settings;
        private static Frames Framesi;
        private static List<XY> OAMImport;
        private static List<byte[]> OAMRemains_Title;
        private static byte[] OAMRemains_Health;
        private static readonly string TitleKey = "Title";
        private static readonly string TitleFramesKey = "Title.Frames";
        private static readonly string DisclaimerGraphicsKey = "Disclaimer.Graphics";
        private static readonly string DisclaimerPaletteKey = "Disclaimer.Palette";
        private static readonly string GBAPlayerPaletteKey = "GBAPlayer.Palette";
        private static readonly string GBAPlayerGraphicsKey = "GBAPlayer.Graphics";
        private static readonly string GBAPlayerArrangementKey = "GBAPlayer.Arrangement";
        private static readonly string HealthKey = "Health";
        private static readonly string[] LogoPath = new string[] { "/title/Logo01.png", "/title/Logo02.png", "/title/Logo03.png" , "/title/Logo04.png"};
        public override string Name => "Title";
        public TitleModule(Mother3RomConfig romConfig, Mother3ProjectSettings projectSettings)
            : base(romConfig, projectSettings)
        {

        }
        public override void ReadFromFiles(IFileSystem fileSystem)
        {
            var jsonManager = new JsonFileManager(fileSystem);
            RegisterFileManagerProgress(jsonManager);
            Framesi = jsonManager.ReadJson<Frames>("/title/configFrames.json".ToPath());
            OAMImport = jsonManager.ReadJson<List<XY>>("/title/configOam.json".ToPath());
            if (fileSystem.Exists("/title/title_settings.json".ToPath()))
                Title_Settings = jsonManager.ReadJson<TitleSettings>("/title/title_settings.json".ToPath());
            else
                Title_Settings = TitleSettings.DefaultSettings(RomConfig.IsEnglish, RomConfig.Version == "en_v12");
            Logo = new List<Bitmap>();
            Menu_Options = new List<Bitmap>();
            Health = new List<Bitmap>();
            if (Title_Settings.Title_Screen)
            {
                for (int i = 0; i < 4; i++)
                    ReadImage(fileSystem, LogoPath[i], Logo);
                ReadImage(fileSystem, "/title/TitleStatic.png", Logo);
                for (int i = 0; i < 21; i++)
                {
                    string a;
                    if (i < 10)
                        a = "/title/TitleFrame0" + i + ".png";
                    else
                        a = "/title/TitleFrame" + i + ".png";
                    ReadImage(fileSystem, a, Logo);
                }
                for (int i = 0; i < 4; i++)
                {
                    string a;
                    if ((i) < 10)
                        a = "/title/Option0" + (i) + ".png";
                    else
                        a = "/title/Option" + (i) + ".png";
                    ReadImage(fileSystem, a, Menu_Options);
                }
                ReadImage(fileSystem, "/title/OptionPalette.png", ref Menu_Options_Palettes);
                ReadImage(fileSystem, "/title/Numbers.png", ref Numbers);
                OAMRemains_Title = new List<byte[]>();
                for (int i = 0; i < 2; i++)
                {
                    var j = fileSystem.OpenFile(("/title/OAM_Remains_Title_" + i.ToString() + ".bin").ToPath(), FileAccess.Read);
                    byte[] buffer = new byte[j.Length];
                    j.Read(buffer, 0, (int)j.Length);
                    j.Close();
                    OAMRemains_Title.Add(buffer);
                }
            }
            if (Title_Settings.Health)
            {
                ReadImage(fileSystem, "/title/HealthScreen.png", Health);
                ReadImage(fileSystem, "/title/HealthPress.png", Health);
                var HealthJ = fileSystem.OpenFile(("/title/OAM_Remains_Health_0.bin").ToPath(), FileAccess.Read);
                byte[] HealthBuffer = new byte[HealthJ.Length];
                HealthJ.Read(HealthBuffer, 0, (int)HealthJ.Length);
                HealthJ.Close();
                OAMRemains_Health = HealthBuffer;
            }
            if(Title_Settings.GBAPlayer)
                ReadImage(fileSystem, "/title/GBAPlayerLogo.png", ref GBAPlayer);
            if (RomConfig.IsEnglish && Title_Settings.Disclaimer)
                ReadImage(fileSystem, "/title/Disclaimer.png", ref Disclaimer);
        }

        public override void WriteToFiles(IFileSystem fileSystem, ISet<object> staleObjects)
        {
            List<Bitmap> newLogo = new List<Bitmap>();
            Menu_Options = new List<Bitmap>();
            fileSystem.CreateDirectory("title/".ToPath());
            for (int i = 0; i < 4; i++)
            {
                newLogo.Add(Logo[i]);
                SaveImage(fileSystem, Logo[i], LogoPath[i]);
            }
            newLogo.Add(Logo[4]);
            SaveImage(fileSystem, Logo[4], "/title/TitleStatic.png");
            for (int i=0; i < 21; i++)
            {
                string a;
                if (i < 10)
                    a = "/title/TitleFrame0" + i + ".png";
                else
                    a = "/title/TitleFrame" + i + ".png";
                newLogo.Add(Logo[5 + i]);
                SaveImage(fileSystem, Logo[5 + i], a);
            }
            for(int i=26; i<Logo.Count-2; i++)
            {
                string a;
                if ((i-26) < 10)
                    a = "/title/Option0" + (i-26) + ".png";
                else
                    a = "/title/Option" + (i-26) + ".png";
                Menu_Options.Add(Logo[i]);
                SaveImage(fileSystem, Logo[i], a);
            }
            Menu_Options_Palettes = Logo[Logo.Count - 2];
            SaveImage(fileSystem, Menu_Options_Palettes, "/title/OptionPalette.png");
            Numbers = Logo[Logo.Count - 1];
            SaveImage(fileSystem, Numbers, "/title/Numbers.png");
            SaveImage(fileSystem, Health[0], "/title/HealthScreen.png");
            SaveImage(fileSystem, Health[1], "/title/HealthPress.png");
            SaveImage(fileSystem, GBAPlayer, "/title/GBAPlayerLogo.png");
            Logo = newLogo;
            if (RomConfig.IsEnglish)
                SaveImage(fileSystem, Disclaimer, "/title/Disclaimer.png");
            OAMRemains_Title = new List<byte[]>();
            for (int i = 0; i < OAMTotal.OAMRemains_Title.Count; i++)
            {
                OAMRemains_Title.Add(OAMTotal.OAMRemains_Title[i].ToArray());
                var j = fileSystem.CreateFile(("/title/OAM_Remains_Title_" + i.ToString() + ".bin").ToPath());
                j.Write(OAMRemains_Title[i], 0, OAMRemains_Title[i].Length);
                j.Close();
            }
            OAMRemains_Health = OAMTotal.OAMRemains_Health[0].ToArray();
            var HealthJ = fileSystem.CreateFile(("/title/OAM_Remains_Health_0.bin").ToPath());
            HealthJ.Write(OAMRemains_Health, 0, OAMRemains_Health.Length);
            HealthJ.Close();
            Title_Settings = TitleSettings.DefaultSettings(RomConfig.IsEnglish, RomConfig.Version == "en_v12");
            var jsonManager = new JsonFileManager(fileSystem);
            RegisterFileManagerProgress(jsonManager);
            jsonManager.WriteJson("/title/configFrames.json".ToPath(), Framesi);
            var u = OAMTotal.d;
            OAMImport = u;
            jsonManager.WriteJson("/title/configOam.json".ToPath(), u);
            jsonManager.WriteJson("/title/title_settings.json".ToPath(), Title_Settings);

        }

        public override void ReadFromRom(Block romData)
        {
            int baseAddressPauseFrames = RomConfig.GetSingleReference(TitleFramesKey);
            Logo = new List<Bitmap>();
            Health = new List<Bitmap>();
            Framesi = new Frames();
            int offset = RomConfig.GetOffset(TitleKey, romData);
            int total = 4 + (RomConfig.IsEnglish ? 1 : 0);
            Progress?.Report(new ProgressPercent("Reading Title table", 100f/total));
            Logo = TitleExport.Begin(romData, offset);
            offset = RomConfig.GetOffset(HealthKey, romData);
            Progress?.Report(new ProgressPercent("Reading Health table", 200f / total));
            Health = TitleExport.Health(romData, offset);
            Progress?.Report(new ProgressPercent("Reading GBA Player table", 300f / total));
            GBAPlayer = TitleExport.GBAPlayerLogo(romData, RomConfig.GetOffset(GBAPlayerPaletteKey, romData), RomConfig.GetOffset(GBAPlayerGraphicsKey, romData), RomConfig.GetOffset(GBAPlayerArrangementKey, romData));
            if (RomConfig.IsEnglish)
            {
                offset= RomConfig.GetOffset(DisclaimerGraphicsKey, romData);
                Progress?.Report(new ProgressPercent("Reading GBA Player table", 400f / total));
                Disclaimer = TitleExport.Disclaimer(romData, offset, RomConfig.GetOffset(DisclaimerPaletteKey, romData));
            }
            offset = 0;
            Framesi.PauseFrames = TitleExport.Frames(romData, baseAddressPauseFrames);
        }

        public override void WriteToRom(Block romData, AllocatedBlockCollection allocatedBlocks)
        {
            //Initialize stuff
            int baseAddressTitle = RomConfig.GetOffset(TitleKey, romData);
            int baseAddressHealth = RomConfig.GetOffset(HealthKey, romData);
            int baseAddressPauseFrames = RomConfig.GetSingleReference(TitleFramesKey);
            int TitleEnd = 0;

            //Title graphics table
            if (Title_Settings.Title_Screen)
            {
                Final = new List<byte[]>();
                Progress?.Report(new ProgressPercent("Compiling logos", 100f / 3f));
                Final.AddRange(TitleImport.LogoImport(Logo));
                Progress?.Report(new ProgressPercent("Compiling menu", 200f / 3f));
                Final.AddRange(TitleImport.MenuImport(Menu_Options, OAMImport, Menu_Options_Palettes, Numbers, OAMRemains_Title));
                byte[] finalTitleTable = CreateTable(Final, 0);
                for (int i = 0; i < finalTitleTable.Length; i++)
                    romData.Data[baseAddressTitle + i] = finalTitleTable[i];
                TitleEnd = baseAddressTitle + finalTitleTable.Length;
            }

            //Health table
            if (Title_Settings.Health)
            {
                Progress?.Report(new ProgressPercent("Compiling health table", 100f / 2f));
                List<byte[]> Health_Final = TitleImport.HealthImport(Health, OAMImport[OAMImport.Count - 1], OAMRemains_Health);
                int offset = 0;
                if ((getTotalLength_Table(Health_Final) - getLength_PointerTable(Health_Final)) > getSpecialROMTableLength(romData.Data, baseAddressHealth))
                {
                    if (TitleEnd == 0)
                        throw new Exception("Health table is too big!");
                    offset = TitleEnd - baseAddressHealth;
                    TitleEnd += getTotalLength_Table(Health_Final) - getLength_PointerTable(Health_Final);
                }
                else
                    offset = getSpecialROMTableStart(romData.Data, baseAddressTitle) - getLength_PointerTable(Health_Final);
                byte[] finalHealthTable = CreateTable(Health_Final, offset);
                for (int i = 0; i < getLength_PointerTable(Health_Final); i++)
                    romData.Data[baseAddressHealth + i] = finalHealthTable[i];
                for (int i = getLength_PointerTable(Health_Final); i < finalHealthTable.Length; i++)
                    romData.Data[baseAddressHealth + i + offset] = finalHealthTable[i];
            }

            //GBA Player Logo data
            if (Title_Settings.GBAPlayer)
            {
                Progress?.Report(new ProgressPercent("Compiling gba player logo", 100f / 2f));
                List<byte[]> GBA_Final = TitleImport.GBAPlayerLogoImport(GBAPlayer);
                int baseGBAPaletteAddress = RomConfig.GetOffset(GBAPlayerPaletteKey, romData);
                int baseGBAArrangementAddress = RomConfig.GetOffset(GBAPlayerArrangementKey, romData);
                int baseGBAGraphicsAddress = TitleEnd;
                if (TitleEnd == 0)
                    baseGBAGraphicsAddress = RomConfig.GetOffset(GBAPlayerGraphicsKey, romData);
                else
                    TitleEnd += GBA_Final[1].Length;
                UpdateRomReferences(romData, GBAPlayerGraphicsKey, baseGBAGraphicsAddress);
                for (int i = 0; i < GBA_Final[0].Length; i++)
                    romData.Data[baseGBAPaletteAddress + i] = GBA_Final[0][i];
                for (int i = 0; i < GBA_Final[1].Length; i++)
                    romData.Data[baseGBAGraphicsAddress + i] = GBA_Final[1][i];
                for (int i = 0; i < GBA_Final[2].Length; i++)
                    romData.Data[baseGBAArrangementAddress + i] = GBA_Final[2][i];
            }

            //Disclaimer data
            if (RomConfig.IsEnglish && Title_Settings.Disclaimer)
            {
                Progress?.Report(new ProgressPercent("Compiling disclaimer", 100f / 2f));
                List<byte[]> Disclaimer_Final = TitleImport.DisclaimerImport(Disclaimer);
                int baseDisclaimerPaletteAddress = RomConfig.GetOffset(DisclaimerPaletteKey, romData);
                int baseDisclaimerGraphicsAddress = TitleEnd;
                if (TitleEnd == 0)
                    baseDisclaimerGraphicsAddress = RomConfig.GetOffset(DisclaimerGraphicsKey, romData);
                else
                    TitleEnd += Disclaimer_Final[1].Length;
                UpdateRomReferences(romData, DisclaimerGraphicsKey, baseDisclaimerGraphicsAddress);
                for (int i = 0; i < Disclaimer_Final[0].Length; i++)
                    romData.Data[baseDisclaimerPaletteAddress + i] = Disclaimer_Final[0][i];
                for (int i = 0; i < Disclaimer_Final[1].Length; i++)
                    romData.Data[baseDisclaimerGraphicsAddress + i] = Disclaimer_Final[1][i];
            }

            //Pause frames between the logo animation frames
            if ((Framesi.PauseFrames >= 8)||(Framesi.PauseFrames<0))
            {
                romData.Data[baseAddressPauseFrames] = 0;
                romData.Data[baseAddressPauseFrames + 3] = 0xD1;
            }
            else
            {
                romData.Data[baseAddressPauseFrames] = (byte)(Framesi.PauseFrames);
                romData.Data[baseAddressPauseFrames + 3] = 0xD9;
            }
        }

        static void SaveImage(IFileSystem fileSystem, Bitmap image, string chosenPath)
        {
            var j = fileSystem.CreateFile(chosenPath.ToPath());
            image.Save(j, ImageFormat.Png);
            j.Close();
        }

        static void ReadImage(IFileSystem fileSystem, string chosenPath, List<Bitmap> Collection)
        {
            var b = fileSystem.OpenFile(chosenPath.ToPath(), FileAccess.Read);
            Collection.Add(new Bitmap(b));
            b.Close();
        }

        static void ReadImage(IFileSystem fileSystem, string chosenPath, ref Bitmap BMP)
        {
            var b = fileSystem.OpenFile(chosenPath.ToPath(), FileAccess.Read);
            BMP = new Bitmap(b);
            b.Close();
        }

        static int getTotalLength_Table(List<byte[]> Entries)
        {
            int length = 0;
            for (int i = 0; i < Entries.Count; i++)
                length += Entries[i].Length;
            return length + getLength_PointerTable(Entries);
        }

        static int getLength_PointerTable(List<byte[]> Entries)
        {
            return ((Entries.Count + 2) * 4);
        }

        static byte[] CreateTable(List<byte[]> Entries, int offset)
        {
            int temp = Entries.Count;
            byte[] Start = new byte[getLength_PointerTable(Entries)];
            Start[0] = (byte)(temp & 0xFF);
            Start[1] = (byte)((temp >> 8) & 0xFF);
            Start[2] = (byte)((temp >> 16) & 0xFF);
            Start[3] = (byte)((temp >> 24) & 0xFF);
            int length = Start.Length;
            for (int i = 0; i < temp; i++)
            {
                Start[((i + 1) * 4)] = (byte)((length + offset) & 0xFF);
                Start[((i + 1) * 4) + 1] = (byte)(((length + offset) >> 8) & 0xFF);
                Start[((i + 1) * 4) + 2] = (byte)(((length + offset) >> 16) & 0xFF);
                Start[((i + 1) * 4) + 3] = (byte)(((length + offset) >> 24) & 0xFF);
                length += Entries[i].Length;
            }
            Start[((temp + 1) * 4)] = (byte)((length + offset) & 0xFF);
            Start[((temp + 1) * 4) + 1] = (byte)(((length + offset) >> 8) & 0xFF);
            Start[((temp + 1) * 4) + 2] = (byte)(((length + offset) >> 16) & 0xFF);
            Start[((temp + 1) * 4) + 3] = (byte)(((length + offset) >> 24) & 0xFF);
            List<byte> finalTable = new List<byte>();
            finalTable.AddRange(Start);
            for (int i = 0; i < Entries.Count; i++)
                finalTable.AddRange(Entries[i]);
            return finalTable.ToArray();
        }

        static int getSpecialROMTableLength(byte[] ROM, int tablePlace)
        {
            int count = ROM[tablePlace] + (ROM[tablePlace + 1] << 8) + (ROM[tablePlace + 2] << 16) + (ROM[tablePlace + 3] << 24);
            int start = getSpecialROMTableStart(ROM, tablePlace);
            return ROM[((count + 1) * 4) + tablePlace] + (ROM[((count + 1) * 4) + tablePlace + 1] << 8) + (ROM[((count + 1) * 4) + tablePlace + 2] << 16) + (ROM[((count + 1) * 4) + tablePlace + 3] << 24) - start;
        }

        static int getSpecialROMTableStart(byte[] ROM, int tablePlace)
        {
            return ROM[tablePlace + 4] + (ROM[tablePlace + 5] << 8) + (ROM[tablePlace + 6] << 16) + (ROM[tablePlace + 7] << 24);
        }

        public override ModuleSerializationResult Serialize()
        {
            var blocks = new LazyBlockCollection();

            return new ModuleSerializationResult(blocks, null);
        }
    }
}
