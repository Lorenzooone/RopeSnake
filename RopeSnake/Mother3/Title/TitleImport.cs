using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//Frame related: 0x805792C Frames, 0x8057FFC Fading 0x8058128
namespace RopeSnake.Mother3.Title
{
    class TitleImport
    {
        static int ClosestSpace(List<int[]> Space, byte[] PAL, byte[]Temp, int num)
        {
            int Length = Temp.Length;
            for(int i=0; i<Temp.Length/2; i++)
            {
                for(int o=0; o<0x10; o++)
                    if((PAL[(o * 2) + (num * 0x20)] == Temp[i * 2])&& (PAL[((o * 2)+1) + (num * 0x20)] == Temp[1+(i * 2)]))
                    {
                        Length -= 2;
                        break;
                    }
            }
            int address = -1, min=0xFF;
            for(int i=0; i<Space.Count; i++)
            {
                if ((Space[i][1] < min) && (Space[i][1] >= Length))
                {
                    address = Space[i][0];
                    min = Space[i][1];
                }
            }
            return address;
        }
        static List<int[]> SpaceFF(byte[] PAL, int num)
        {
            List<int[]> A = new List<int[]>();
            int Last = -2;
            for(int i=num*0x10; i<(num+1)*0x10; i++)
                if ((PAL[i * 2] == 0xFF) && (PAL[(i * 2) + 1] == 0xFF))
                {
                    if (Last != (i - 1))
                    {
                        int[] t = { i*2, 2 };
                        A.Add(t);
                        Last = i;
                    }
                    else
                    {
                        A[A.Count - 1][1] += 2;
                        Last = i;
                    }
                }
            return A;
        }
        static byte[] PALGet(Bitmap menu)
        {
            int size = ((menu.Width / 4) * (menu.Height / 4) * 2);
            byte[] returnPAL = new byte[size + 0x20];
            for (int i = 0; i < menu.Width / 4; i++)
            {
                for (int j = 0; j < menu.Height / 4; j++)
                {
                    Color temp = menu.GetPixel(i * 4, j * 4);
                    byte[] a = new byte[2];
                    int B = ((temp.B >> 3) & 31) << 10;
                    int G = ((temp.G >> 3) & 31) << 5;
                    int R = ((temp.R >> 3) & 31);
                    a[0] = (byte)((B + G + R) & 0xFF);
                    a[1] = (byte)(((B + G + R) >> 8) & 0xFF);
                    returnPAL[(((i * (menu.Height / 4)) + j) * 2)] = a[0];
                    returnPAL[(((i * (menu.Height / 4)) + j) * 2) + 1] = a[1];
                }
            }
            for (int i = 0; i < 0x20; i++)
                returnPAL[size + i] = 0;
            return returnPAL;
        }
        static int PALGEN(Bitmap LogoN, int StartH, int StartW, byte[] PAL)
        {
            int A = -1;
            byte[] Temp = TilePal(LogoN, StartH, StartW);
            for (int i = 0; i < PAL.Length/0x20; i++)
            {
                int u = 0;
                for (int p = 0; p < Temp.Length / 2; p++)
                    for (int o = 0; o < 0x10; o++)
                    {
                        if ((PAL[(o * 2) + (i * 0x20)] == Temp[p * 2]) && (PAL[(o * 2) + 1 + (i * 0x20)] == Temp[(p * 2) + 1]))
                        {
                            u++;
                            break;
                        }
                    }
                if (u == Temp.Length / 2)
                {
                    A = i;
                    break;
                }
            }
            int k = 0;
            while (A == -1)
            {
                A = ClosestSpace(SpaceFF(PAL, k), PAL, Temp, k);
                if (A != -1)
                {
                    A = A / 0x20;
                    List<int> NotAdd = new List<int>();
                    for (int i = 0; i < Temp.Length / 2; i++)
                    {
                        for (int o = 0; o < 0x10; o++)
                            if ((PAL[(o * 2) + (k * 0x20)] == Temp[i * 2]) && (PAL[((o * 2) + 1) + (k * 0x20)] == Temp[1 + (i * 2)]))
                            {
                                NotAdd.Add(i);
                                break;
                            }
                    }
                    for (int i = 0; i < Temp.Length / 2; i++)
                    {
                        if (!NotAdd.Exists(x => x == i))
                        {
                            for (int o = (A * 0x10); o < (A + 1) * 0x10; o++)
                            {
                                if ((PAL[o * 2] == 0xFF) && (PAL[(o * 2) + 1] == 0xFF))
                                {
                                    PAL[o * 2] = Temp[i * 2];
                                    PAL[(o * 2) + 1] = Temp[(i * 2) + 1];
                                    break;
                                }
                            }
                        }
                    }
                }
                k++;
            }
            return A;
        }
        static int PALGEN8(Bitmap LogoN, int StartH, int StartW, byte[] PAL)
        {
            byte[] Temp = TilePal(LogoN, StartH, StartW);
            List<int> NotAdd = new List<int>();
                    for (int i = 0; i < Temp.Length / 2; i++)
                    {
                        for (int o = 0; o < 0x100; o++)
                            if ((PAL[(o * 2)] == Temp[i * 2]) && (PAL[((o * 2) + 1)] == Temp[1 + (i * 2)]))
                            {
                                NotAdd.Add(i);
                                break;
                            }
                    }
                    for (int i = 0; i < Temp.Length / 2; i++)
                    {
                        if (!NotAdd.Exists(x => x == i))
                        {
                            for (int o = 0; o < (0x100); o++)
                            {
                                if ((PAL[o * 2] == 0xFF) && (PAL[(o * 2) + 1] == 0xFF))
                                {
                                    PAL[o * 2] = Temp[i * 2];
                                    PAL[(o * 2) + 1] = Temp[(i * 2) + 1];
                                    break;
                                }
                            }
                        }
                    }
            return 0;
        }
        static byte[] TilePal(Bitmap LogoN, int StartH, int StartW)
        {
            List<byte> PAL = new List<byte>();
            for(int o=0; o<8; o++)
                for(int i=0; i<8; i++)
                {
                    Color temp = LogoN.GetPixel(StartW + i, StartH+o);
                    byte[] a = new byte[2];
                    int B = ((temp.B >> 3) & 31) << 10;
                    int G = ((temp.G >> 3) & 31) << 5;
                    int R = ((temp.R >> 3) & 31);
                    a[0] = (byte)((B + G + R) & 0xFF);
                    a[1] = (byte)(((B + G + R)>>8) & 0xFF);
                    int u = 0;
                    for (int p = 0; p < PAL.Count / 2; p++)
                    {
                        if ((PAL[p * 2] == a[0]) && (PAL[(p * 2) + 1] == a[1]))
                        {
                            u = 1;
                            break;
                        }
                    }
                    if (u == 0)
                        PAL.AddRange(a);
                }
            return PAL.ToArray();
        }
        static int readImage_OAM(Bitmap LogoN, int startPos, List<List<byte>> TotalEntries, byte[] Palette, int XLen, List<int>startingAddress, bool PALPreset)
        {
            int tileWidth = (LogoN.Width / 8) + (LogoN.Width % 8 == 0 ? 0 : 1);
            int tileHeight = (LogoN.Height / 8) + (LogoN.Height % 8 == 0 ? 0 : 1);
            int cycles = 0;
            int CorrespondingPAL;
            do
            {
                startingAddress.Add(startPos);
                for (int y = 0; y < tileHeight; y++)
                    for (int x = 0; x < XLen; x++)
                    {
                        CorrespondingPAL = 0;
                        if(!PALPreset && !(((x + (cycles * XLen) + 1) * 8) >= LogoN.Width))
                            CorrespondingPAL = (PALGEN(LogoN, y * 8, (x + (cycles * XLen)) * 8, Palette));
                        GraphicsGEN(LogoN, y * 8, (x + (cycles * XLen)) * 8, CorrespondingPAL, Palette, TotalEntries, ref startPos);
                    }
            } while ((cycles++) < (tileWidth -1) / XLen);
            return startPos;
        }
        static int GraphicsGEN(Bitmap LogoN, int StartH, int StartW, int ActualPAL, byte[] LogoPalettes, byte[] LogoGraphics, ref int lastaddress, bool saveAll)
        {
            byte[] Tile = new Byte[0x20];
            for (int o = 0; o < 8; o++)
                for (int i = 0; i < 4; i++)
                {
                    byte alpha = 0;
                    for (int y = 0; y < 2; y++)
                    {
                        if (StartW + (i * 2) + y < LogoN.Width && StartH + o < LogoN.Height)
                        {
                            Color temp = LogoN.GetPixel(StartW + (i * 2) + y, StartH + o);
                            byte[] a = new byte[2];
                            int B = ((temp.B >> 3) & 31) << 10;
                            int G = ((temp.G >> 3) & 31) << 5;
                            int R = ((temp.R >> 3) & 31);
                            a[0] = (byte)((B + G + R) & 0xFF);
                            a[1] = (byte)(((B + G + R) >> 8) & 0xFF);
                            for (int j = 0; j < 0x10; j++)
                            {
                                if ((LogoPalettes[(ActualPAL * 0x20) + (j * 2)] == a[0]) && (LogoPalettes[(ActualPAL * 0x20) + 1 + (j * 2)] == a[1]))
                                {
                                    alpha += (byte)(j << (4 * y));
                                    break;
                                }
                            }
                        }
                    }
                    Tile[(o * 4) + i] = alpha;
                }
            int p = 0;
            int e = 0;
            if(!saveAll)
                for (; e < lastaddress / 0x20; e++)
                {
                    for (p = 0; p < 0x20; p++)
                    {
                        if (LogoGraphics[(e * 0x20) + p] == Tile[p]) { }
                        else break;
                    }
                    if (p == 0x20) break;
                }
            if (p == 0x20)
                return e;
            else
            {
                for (int i = 0; i < 0x20; i++)
                {
                    LogoGraphics[lastaddress + i] = Tile[i];
                }
                lastaddress = lastaddress + 0x20;
                return ((lastaddress / 0x20) - 1);
            }
        }
        static int GraphicsGEN(Bitmap LogoN, int StartH, int StartW, int ActualPAL, byte[] LogoPalettes, List<List<byte>> LogoGraphics, ref int lastaddress)
        {
            byte[] Tile = new Byte[0x20];
            for (int o = 0; o < 8; o++)
                for (int i = 0; i < 4; i++)
                {
                    byte alpha = 0;
                    for (int y = 0; y < 2; y++)
                    {
                        if (StartW + (i * 2) + y < LogoN.Width && StartH + o < LogoN.Height)
                        {
                            Color temp = LogoN.GetPixel(StartW + (i * 2) + y, StartH + o);
                            byte[] a = new byte[2];
                            int B = ((temp.B >> 3) & 31) << 10;
                            int G = ((temp.G >> 3) & 31) << 5;
                            int R = ((temp.R >> 3) & 31);
                            a[0] = (byte)((B + G + R) & 0xFF);
                            a[1] = (byte)(((B + G + R) >> 8) & 0xFF);
                            for (int j = 0; j < 0x10; j++)
                            {
                                if ((LogoPalettes[(ActualPAL * 0x20) + (j * 2)] == a[0]) && (LogoPalettes[(ActualPAL * 0x20) + 1 + (j * 2)] == a[1]))
                                {
                                    alpha += (byte)(j << (4 * y));
                                    break;
                                }
                            }
                        }
                    }
                    Tile[(o * 4) + i] = alpha;
                }
            LogoGraphics.Add(Tile.ToList());
            lastaddress++;
            return lastaddress - 1;
        }
        static int GraphicsGEN8(Bitmap LogoN, int StartH, int StartW, int ActualPAL, byte[] LogoPalettes, ref int Last, byte[] LogoGraphics)
        {
            byte[] Tile = new Byte[0x40];
            for (int o = 0; o < 8; o++)
                for (int i = 0; i < 8; i++)
                {
                    /*if (StartH == 32 && StartW == 168 && o==1 && i==1)
                    {
                        System.Diagnostics.Debugger.Break();
                    }*/
                    byte alpha = 0;
                    if (StartW + i < LogoN.Width && StartH + o < LogoN.Height)
                    {
                        Color temp = LogoN.GetPixel(StartW + i, StartH + o);
                        byte[] a = new byte[2];
                        int B = ((temp.B >> 3) & 31) << 10;
                        int G = ((temp.G >> 3) & 31) << 5;
                        int R = ((temp.R >> 3) & 31);
                        a[0] = (byte)((B + G + R) & 0xFF);
                        a[1] = (byte)(((B + G + R) >> 8) & 0xFF);
                        for (int j = 0; j < 0x100; j++)
                        {
                            if ((LogoPalettes[(j * 2)] == a[0]) && (LogoPalettes[1 + (j * 2)] == a[1]))
                            {
                                alpha = (byte)(j);
                                break;
                            }
                        }
                    }
                    Tile[(o * 8) + i] = alpha;
                }
            int p = 0;
            int e = 0;
            for (; e < Last / 0x40; e++)
            {
                for (p = 0; p < 0x40; p++)
                {
                    if (LogoGraphics[(e * 0x40) + p] == Tile[p]) { }
                    else break;
                }
                if (p == 0x40) break;
            }
            if (p == 0x40)
                return e;
            else
            {
                for (int i = 0; i < 0x40; i++)
                {
                    LogoGraphics[Last+i]=Tile[i];
                }
                Last += 0x40;
                return ((Last/ 0x40) - 1);
            }
        }
        static int GraphicsGEN8(Bitmap LogoN, int StartH, int StartW, int ActualPAL, byte[] LogoPalettes, List<byte>LogoGraphics, bool saveAll)
        {
            byte[] Tile = new Byte[0x40];
            for (int o = 0; o < 8; o++)
                for (int i = 0; i < 8; i++)
                {
                    /*if (StartH == 32 && StartW == 168 && o==1 && i==1)
                    {
                        System.Diagnostics.Debugger.Break();
                    }*/
                    byte alpha = 0;
                    if (StartW + i < LogoN.Width && StartH + o < LogoN.Height)
                    {
                        Color temp = LogoN.GetPixel(StartW + i, StartH + o);
                        byte[] a = new byte[2];
                        int B = ((temp.B >> 3) & 31) << 10;
                        int G = ((temp.G >> 3) & 31) << 5;
                        int R = ((temp.R >> 3) & 31);
                        a[0] = (byte)((B + G + R) & 0xFF);
                        a[1] = (byte)(((B + G + R) >> 8) & 0xFF);
                        for (int j = 0; j < 0x100; j++)
                        {
                            if ((LogoPalettes[(j * 2)] == a[0]) && (LogoPalettes[1 + (j * 2)] == a[1]))
                            {
                                alpha = (byte)(j);
                                break;
                            }
                        }
                    }
                    Tile[(o * 8) + i] = alpha;
                }
            int p = 0;
            int e = 0;
            if (!saveAll)
                for (; e < LogoGraphics.Count / 0x40; e++)
                {
                    for (p = 0; p < 0x40; p++)
                        if (LogoGraphics[(e * 0x40) + p] != Tile[p])
                            break;
                    if (p == 0x40) break;
                }
            if (p == 0x40)
                return e;
            else
            {
                for (int i = 0; i < 0x40; i++)
                    LogoGraphics.Add(Tile[i]);
                return ((LogoGraphics.Count / 0x40) - 1);
            }
        }
        static byte[] LogoExec(Bitmap LogoN, ref int lastaddress, byte[] LogoGraphics, Byte[] LogoPalettes)
        {
            int CorrespondingPAL = 0, TileNum = 0;
            byte[] Arrangement = new byte[0x800];
            for (int o = 0; o < LogoN.Height / 8; o++)
                for (int i = 0; i < LogoN.Width / 8; i++)
                {
                    CorrespondingPAL = (PALGEN(LogoN, o * 8, i * 8, LogoPalettes));
                    TileNum = GraphicsGEN(LogoN, o * 8, i * 8, CorrespondingPAL, LogoPalettes, LogoGraphics, ref lastaddress, false);
                    if ((o * (LogoN.Width / 4)) + (i * 2) >= 0x800)
                        break;
                    Arrangement[(o * (LogoN.Width / 4)) + (2 * i)] = (byte)(TileNum & 0xFF);
                    Arrangement[(o * (LogoN.Width / 4)) + 1 + (2 * i)] = (byte)(((TileNum >> 8) & 0x3) + (CorrespondingPAL << 4));
                }
            return Arrangement;
        }
        static byte[] NumbersExec(Bitmap Numbers, Byte[] NumbersPalettes)
        {
            int CorrespondingPAL = 0;
            int lastaddress = 0;
            byte[] Graphics = new byte[0x400];
            for (int o = 0; o < Numbers.Height / 8; o++)
                for (int i = 0; i < Numbers.Width / 8; i++)
                {
                    CorrespondingPAL = (PALGEN(Numbers, o * 8, i * 8, NumbersPalettes));
                    GraphicsGEN(Numbers, o * 8, i * 8, CorrespondingPAL, NumbersPalettes, Graphics, ref lastaddress, true);
                }
            return Graphics;
        }
        static byte[] DisclaimerExec(Bitmap Disclaimer, Byte[] DisclaimerPal)
        {
            List<byte> DisclaimerGraphics = new List<byte>();

            int CorrespondingPAL = 0, TileNum = 0;
            for (int o = 0; o < Disclaimer.Height / 8; o++)
                for (int i = 0; i < Disclaimer.Width / 8; i++)
                {
                    CorrespondingPAL = (PALGEN8(Disclaimer, o * 8, i * 8, DisclaimerPal));
                    TileNum = GraphicsGEN8(Disclaimer, o * 8, i * 8, CorrespondingPAL, DisclaimerPal, DisclaimerGraphics, true);
                }
            return DisclaimerGraphics.ToArray();
        }
        static byte[] TitleExec(Bitmap TitleStatic, List<byte> TitleGraphics, Byte[] TitlePal, int ArrangementSize)
        {
            int CorrespondingPAL = 0, TileNum = 0;
            byte[] Arrangement = new byte[ArrangementSize];
            for (int o = 0; o < TitleStatic.Height / 8; o++)
                for (int i = 0; i < TitleStatic.Width / 8; i++)
                {
                    CorrespondingPAL = (PALGEN8(TitleStatic, o * 8, i * 8, TitlePal));
                    TileNum = GraphicsGEN8(TitleStatic, o * 8, i * 8, CorrespondingPAL, TitlePal, TitleGraphics, false);
                    if ((o * (TitleStatic.Width / 4)) + (i * 2) >= ArrangementSize)
                        break;
                    Arrangement[(o * (TitleStatic.Width / 4)) + (2 * i)] = (byte)(TileNum & 0xFF);
                    Arrangement[(o * (TitleStatic.Width / 4)) + 1 + (2 * i)] = (byte)(((TileNum >> 8) & 0x3) + (CorrespondingPAL << 4));
                }
            return Arrangement;
        }
        static byte[] TitleDynExec(Bitmap TitleStatic, Byte[] TitleGraphics, Byte[] TitlePal, ref int Last, int ArrangementSize)
        {
            int CorrespondingPAL = 0, TileNum = 0;
            byte[] Arrangement = new byte[ArrangementSize];
            for (int o = 0; o < TitleStatic.Height / 8; o++)
                for (int i = 0; i < TitleStatic.Width / 8; i++)
                {
                    CorrespondingPAL = (PALGEN8(TitleStatic, o * 8, i * 8, TitlePal));
                    TileNum = GraphicsGEN8(TitleStatic, o * 8, i * 8, CorrespondingPAL, TitlePal, ref Last, TitleGraphics);
                    if ((o * (TitleStatic.Width / 4)) + (i * 2) >= ArrangementSize)
                        break;
                    Arrangement[(o * (TitleStatic.Width / 4)) + (2 * i)] = (byte)(TileNum & 0xFF);
                    Arrangement[(o * (TitleStatic.Width / 4)) + 1 + (2 * i)] = (byte)(((TileNum >> 8) & 0x3) + (CorrespondingPAL << 4));
                }
            return Arrangement;
        }
        static int Generate_OAM_Graphics_Palette_Inner(Bitmap Entry, XY Position, List<int> startingAddress, List<byte> OAMEntry, int StartingPos, int XLen, byte[] Palettes, List<List<byte>> EntriesGraphics, bool PALPreset)
        {
            int X = Position.X_Coord;
            int Y = Position.Y_Coord;

            if (X <= 0x100)
                X += 0x100;
            else
                X -= 0x100;
            if (Y <= 0x80)
                Y += 0x80;
            else
                Y -= 0x80;

            int YLen = (Entry.Height / 8) + (Entry.Height % 8 == 0 ? 0 : 1);
            StartingPos = readImage_OAM(Entry, StartingPos, EntriesGraphics, Palettes, XLen, startingAddress, PALPreset);
            for (int j = 0; j < startingAddress.Count; j++)
            {
                Enemy_Graphics.Importing.OAMFor(ref OAMEntry, EntriesGraphics, startingAddress[j], YLen, XLen, X + (XLen * j * 8), Y, XLen, false);
            }
            return StartingPos;
        }
        static byte[] Generate_OAM_Graphics_Palette(List<Bitmap> Entries, List<XY> Positions, List<byte>[] OAMEntries, byte[] Palettes, bool PALPreset)
        {
            List<int>[] startingAddresses = new List<int>[Entries.Count];
            List<Bitmap> Tmp_Entries = new List<Bitmap>();
            List<List<byte>> EntriesGraphics = new List<List<byte>>();
            Tmp_Entries.AddRange(Entries);
            Tmp_Entries.Sort(widthCompare);
            int BaseXLen = (Tmp_Entries[0].Width / 8) + (Tmp_Entries[0].Width % 8 == 0 ? 0 : 1);
            int XLen = BaseXLen;
            if (XLen > 4)
                XLen = 8;
            else if (XLen > 2)
                XLen = 4;
            int startingPos = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                startingAddresses[i] = new List<int>();
                OAMEntries[i] = new List<byte>();
                startingPos = Generate_OAM_Graphics_Palette_Inner(Entries[i], Positions[i], startingAddresses[i], OAMEntries[i], startingPos, XLen, Palettes, EntriesGraphics, PALPreset);
            }
            return Enemy_Graphics.Importing.Finalize(EntriesGraphics, OAMEntries, XLen, 6);
        }
        static byte[] Generate_OAM_Graphics_Palette(Bitmap Entry, XY Position, ref List<byte> OAMEntry, byte[] Palettes, bool PALPreset)
        {
            List<int> startingAddress = new List<int>();
            List<List<byte>> EntryGraphics = new List<List<byte>>();
            int BaseXLen = (Entry.Width / 8) + (Entry.Width % 8 == 0 ? 0 : 1);
            int XLen = BaseXLen;
            if (XLen > 4)
                XLen = 8;
            else if (XLen > 2)
                XLen = 4;
            Generate_OAM_Graphics_Palette_Inner(Entry, Position, startingAddress, OAMEntry, 0, XLen, Palettes, EntryGraphics, PALPreset);

            return Enemy_Graphics.Importing.Finalize(EntryGraphics, ref OAMEntry, XLen, 6);
        }
        static void buildArray(byte[] firstOAM, List<byte> currentOAMEntries)
        {
            firstOAM[0] = 1;
            firstOAM[1] = 0;
            firstOAM[2] = 8;
            firstOAM[3] = 0;
            firstOAM[4] = (byte)(currentOAMEntries.Count + 8);
            firstOAM[5] = 0;
            firstOAM[6] = 0;
            firstOAM[7] = 0;
        }
        static byte[] buildOAM(List<byte>[] OAMEntries, List<byte[]> OAMRemains, bool insertMiddle, bool insertEnd)
        {
            List<byte> tmpOAMEntries = new List<byte>();
            tmpOAMEntries.Add((byte)(OAMEntries.Length + 3));
            tmpOAMEntries.Add((byte)(0));
            byte[] baseArray = new byte[8];
            for (int i = 0; i < OAMEntries.Length; i++)
            {
                tmpOAMEntries.Add(0);
                tmpOAMEntries.Add(0);
                tmpOAMEntries.Add((byte)(OAMEntries[i].Count/6));
                tmpOAMEntries.Add(0);
                tmpOAMEntries.AddRange(OAMEntries[i]);
            }
            if (insertMiddle)
            {
                tmpOAMEntries.Add(0);
                tmpOAMEntries.Add(0);
                tmpOAMEntries.AddRange(OAMRemains[0]);
            }
            buildArray(baseArray, tmpOAMEntries);
            if(insertEnd && OAMRemains.Count > 1)
                tmpOAMEntries.AddRange(OAMRemains[1]);
            else if(insertEnd && OAMRemains.Count == 1)
                tmpOAMEntries.AddRange(OAMRemains[0]);
            List<byte> tmpFinal = new List<byte>();
            tmpFinal.AddRange(baseArray);
            tmpFinal.AddRange(tmpOAMEntries);
            return tmpFinal.ToArray();
        }
        public static int widthCompare(Bitmap a, Bitmap b)
        {
            return b.Width.CompareTo(a.Width);
        }
        public static void setPalettes(byte[] Palette, bool setFirst)
        {
            for (int i = 0; i < Palette.Length / 2; i++)
            {
                if (i % 0x10 == 0 && setFirst)
                {
                    Palette[i * 2] = 0xFF;
                    Palette[(i * 2) + 1] = 0xFE;
                }
                else
                {
                    Palette[i * 2] = 0xFF;
                    Palette[(i * 2) + 1] = 0xFF;
                }
            }
        }
        public static List<Byte[]> MenuImport(List<Bitmap> Menu_Options, List<XY> Positions, Bitmap Menu_Options_Palette, Bitmap Numbers, List<byte[]> OAMRemains)
        {
            List<byte[]> finalProducts = new List<byte[]>();
            byte[] MenuPalettes = PALGet(Menu_Options_Palette);
            List<byte>[] OAMEntries = new List<byte>[Menu_Options.Count];
            Byte[] FinalGraphicsProduct = GBA.LZ77.Compress(Generate_OAM_Graphics_Palette(Menu_Options, Positions, OAMEntries, MenuPalettes, true), true);
            byte[] FinalOAMProduct = buildOAM(OAMEntries, OAMRemains, true, true);
            finalProducts.Add(FinalGraphicsProduct);
            finalProducts.Add(MenuPalettes);
            finalProducts.Add(FinalOAMProduct);
            byte[] NumbersPalettes = new byte[0x20];
            setPalettes(NumbersPalettes, false);
            finalProducts.Add(NumbersExec(Numbers, NumbersPalettes));
            finalProducts.Add(NumbersPalettes);
            return finalProducts;
        }
        public static List<Byte[]> LogoImport(List<Bitmap> Logo)
        {
            byte[] LogoGraphics = new Byte[0x4400];
            byte[] LogoPalettes = new byte[0xC0];
            setPalettes(LogoPalettes, true);
            List<byte[]> LogoArrangements = new List<byte[]>();
            List<byte[]> Product = new List<byte[]>();
            int lastaddress = 0;
            for (int i = 0; i < 4; i++)
                LogoArrangements.Add(LogoExec(Logo[i], ref lastaddress, LogoGraphics, LogoPalettes));
            if (lastaddress < 0x4400)
            {
                byte[] newLogoGraphics = new byte[lastaddress];
                for (int i = 0; i < lastaddress; i++)
                    newLogoGraphics[i] = LogoGraphics[i];
                Product.Add(newLogoGraphics);
            }
            else
                Product.Add(LogoGraphics);
            Product.Add(LogoPalettes);
            for (int i = 0; i < 4; i++)
                Product.Add(LogoArrangements[i]);
            LogoPalettes = new byte[0x200];
            for (int i = 0; i < 0x100; i++)
            {
                if (i == 0)
                {
                    LogoPalettes[i * 2] = 0xFF;
                    LogoPalettes[(i * 2) + 1] = 0xFE;
                }
                else
                {
                    LogoPalettes[i * 2] = 0xFF;
                    LogoPalettes[(i * 2) + 1] = 0xFF;
                }
            }
            List<byte> TitleStatic = new List<byte>();
            byte[] ArrangementStatic = TitleExec(Logo[4], TitleStatic, LogoPalettes, 0x800);
            LogoPalettes[0] = 0;
            LogoPalettes[1] = 0;
            Product.Add(GBA.LZ77.Compress(TitleStatic.ToArray(), true));
            Product.Add(LogoPalettes);
            Product.Add(ArrangementStatic);
            LogoPalettes = new byte[0x200];
            for (int i = 0; i < 0x100; i++)
            {
                if (i == 0)
                {
                    LogoPalettes[i * 2] = 0xFF;
                    LogoPalettes[(i * 2) + 1] = 0xFE;
                }
                else
                {
                    LogoPalettes[i * 2] = 0xFF;
                    LogoPalettes[(i * 2) + 1] = 0xFF;
                }
            }
            List<byte[]> TitleDynamic = new List<byte[]>();
            List<byte[]> ArrangementDynamic =new List<byte[]>();
            for(int i=0; i<21; i++)
            {
                byte[] Temp=new byte[0x6000];
                int total = 0;
                ArrangementDynamic.Add(TitleDynExec(Logo[5+i], Temp, LogoPalettes, ref total, 0x800));
                if (total < 0x6000)
                {
                    byte[] newTemp = new byte[total];
                    for (int j = 0; j < total; j++)
                        newTemp[j] = Temp[j];
                    TitleDynamic.Add(newTemp);
                }
                else
                    TitleDynamic.Add(Temp);
            }
            for (int i = 0; i < 21; i++)
                Product.Add(TitleDynamic[i]);
            for (int i = 0; i < 21; i++)
                Product.Add(ArrangementDynamic[i]);
            Product.Add(LogoPalettes);
            return Product;
        }
        public static List<Byte[]> HealthImport(List<Bitmap> Health, XY Position, byte[] OAMRemains)
        {
            byte[] MainPalette = new byte[0x20];

            //Press a button graphic
            List<byte> OAMEntry = new List<byte>();
            setPalettes(MainPalette, false);
            byte[] Press_Graphics = GBA.LZ77.Compress(Generate_OAM_Graphics_Palette(Health[1], Position, ref OAMEntry, MainPalette, false), true);
            byte[] Press_OAM = buildOAM(new List<byte>[] { OAMEntry }, new List<byte[]> { OAMRemains }, false, true);

            //Health graphic
            byte[] MainArrangements;
            byte[] MainGraphicsBuffer = new byte[0x6000];
            int length = 0;
            MainArrangements = LogoExec(Health[0], ref length, MainGraphicsBuffer, MainPalette);
            byte[] MainGraphics = new byte[length];
            for (int i = 0; i < length; i++)
                MainGraphics[i] = MainGraphicsBuffer[i];
            MainGraphics = GBA.LZ77.Compress(MainGraphics, true);
            MainArrangements = GBA.LZ77.Compress(MainArrangements, true);

            //Ending
            List<byte[]> Products = new List<byte[]>();
            Products.Add(MainGraphics);
            Products.Add(MainPalette);
            Products.Add(MainArrangements);
            Products.Add(Press_Graphics);
            Products.Add(Press_OAM);
            return Products;
        }
        public static List<Byte[]> GBAPlayerLogoImport(Bitmap GBAPlayerLogo)
        {
            //Setup
            byte[] GBAPalette = new byte[0x200];
            List<byte> GBAGraphics = new List<byte>();
            setPalettes(GBAPalette, true);

            //Actual processing
            byte[] GBAArrangement = TitleExec(GBAPlayerLogo, GBAGraphics, GBAPalette, 0x500);

            //Ending
            List<byte[]> Products = new List<byte[]>();
            Products.Add(GBAPalette);
            Products.Add(GBAGraphics.ToArray());
            Products.Add(GBAArrangement);
            return Products;
        }
        public static List<Byte[]> DisclaimerImport(Bitmap Disclaimer)
        {
            //Setup
            byte[] DisclaimerPalette = new byte[0x200];
            setPalettes(DisclaimerPalette, true);
            
            //Ending
            List<byte[]> Products = new List<byte[]>();
            Products.Add(DisclaimerPalette);
            Products.Add(GBA.LZ77.Compress(DisclaimerExec(Disclaimer, DisclaimerPalette), true));
            return Products;
        }
    }
}
