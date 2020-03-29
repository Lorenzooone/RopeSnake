using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using RopeSnake.Core;
using SharpFileSystem;

namespace FreeSpace
{
    class Pointers
    {
        public static int Base = 0x1C90960;
        public static int BaseSOB = 0x1C91E88;//Start of enemy SOB blocks pointers
        public static int BaseCCG = 0x1C909A8;//Start of enemy CCG blocks pointers
        public static int BasePAL = 0x1C91530;//Start of enemy palettes pointers
        public static void Removal(byte[] memblock)
        {
            int t = 0, g = 0, character = 0;
            int[] PointerSOB = new int[257], SOBLength = new int[257], PointerCCG = new int[257], CCGLength = new int[257];
            for (int i = 0; i <= 256; i++)
            {
                int Enemynum = i;
                int PoiSOB = BaseSOB + (Enemynum * 8);
                int PoiCCG = BaseCCG + (Enemynum * 8);
                int PoiPAL = BasePAL + (Enemynum * 8);
                PointerSOB[i] = memblock[PoiSOB] + (memblock[PoiSOB + 1] << 8) + (memblock[PoiSOB + 2] << 16) + (memblock[PoiSOB + 3] << 24) + Base;//SOB
                PointerCCG[i] = memblock[PoiCCG] + (memblock[PoiCCG + 1] << 8) + (memblock[PoiCCG + 2] << 16) + (memblock[PoiCCG + 3] << 24) + Base;//CCG
                SOBLength[i] = memblock[PoiSOB + 4] + (memblock[PoiSOB + 5] << 8) + (memblock[PoiSOB + 6] << 16) + (memblock[PoiSOB + 7] << 24);//SOBLength
                CCGLength[i] = memblock[PoiCCG + 4] + (memblock[PoiCCG + 5] << 8) + (memblock[PoiCCG + 6] << 16) + (memblock[PoiCCG + 7] << 24);//CCGLength
            }
            g = 0;
            while (t <= 256)
            {
                g = PointerSOB[t];
                character = g;
                for (g = 0; g <= SOBLength[t] / 4; g++)
                {
                    memblock[(int)character] = 255;
                    memblock[(int)character + 1] = 255;
                    memblock[(int)character + 2] = 255;
                    memblock[(int)character + 3] = 255;
                    character = character + 4;
                }
                t = t + 1;
            }
            character = 0;
            while (t <= 256)
            {
                g = PointerCCG[t];
                character = g;
                for (g = 0; g <= CCGLength[t] / 4; g++)
                {
                    memblock[(int)character] = 255;
                    memblock[(int)character + 1] = 255;
                    memblock[(int)character + 2] = 255;
                    memblock[(int)character + 3] = 255;
                    character = character + 4;
                }
                t = t + 1;
            }
        }
    }
}//Import of C++ code. It's commented, but somewhat messy.
namespace GBA
{
    class LZ77
    {
        public static int Decompress(byte[] data, int address, out byte[] output)
        {
            output = null;
            int start = address;

            if (data[address++] != 0x10) return -1; // Check for LZ77 signature

            // Read the block length
            int length = data[address++];
            length += (data[address++] << 8);
            length += (data[address++] << 16);
            output = new byte[length];

            int bPos = 0;
            while (bPos < length)
            {
                byte ch = data[address++];
                for (int i = 0; i < 8; i++)
                {
                    switch ((ch >> (7 - i)) & 1)
                    {
                        case 0:

                            // Direct copy
                            if (bPos >= length) break;
                            output[bPos++] = data[address++];
                            break;

                        case 1:

                            // Compression magic
                            int t = (data[address++] << 8);
                            t += data[address++];
                            int n = ((t >> 12) & 0xF) + 3;    // Number of bytes to copy
                            int o = (t & 0xFFF);

                            // Copy n bytes from bPos-o to the output
                            for (int j = 0; j < n; j++)
                            {
                                if (bPos >= length) break;
                                output[bPos] = output[bPos - o - 1];
                                bPos++;
                            }

                            break;

                        default:
                            break;
                    }
                }
            }

            return address - start;
        }
        public static byte[] Compress(byte[] uncompressed, bool vram)
        {
            LinkedList<int>[] lookup = new LinkedList<int>[256];
            List<byte> Compressed = new List<byte>();
            for (int i = 0; i < 256; i++)
                lookup[i] = new LinkedList<int>();

            int start = 0;
            int current = 0;

            List<byte> temp = new List<byte>();
            int control = 0;

            // Encode the signature and the length
            Compressed.Add(0x10);
            Compressed.Add((byte)(uncompressed.Length & 0xFF));
            Compressed.Add((byte)((uncompressed.Length >> 8) & 0xFF));
            Compressed.Add((byte)((uncompressed.Length >> 16) & 0xFF));

            // VRAM bug: you can't reference the previous byte
            int distanceStart = vram ? 2 : 1;

            while (current < uncompressed.Length)
            {
                temp.Clear();
                control = 0;

                for (int i = 0; i < 8; i++)
                {
                    bool found = false;

                    // First byte should be raw
                    if (current == 0)
                    {
                        byte value = uncompressed[current];
                        lookup[value].AddFirst(current++);
                        temp.Add(value);
                        found = true;
                    }
                    else if (current >= uncompressed.Length)
                    {
                        break;
                    }
                    else
                    {
                        // We're looking for the longest possible string
                        // The farthest possible distance from the current address is 0x1000
                        int max_length = -1;
                        int max_distance = -1;

                        LinkedList<int> possibleAddresses = lookup[uncompressed[current]];

                        foreach (int possible in possibleAddresses)
                        {
                            if (current - possible > 0x1000)
                                break;

                            if (current - possible < distanceStart)
                                continue;

                            int farthest = Math.Min(18, uncompressed.Length - current + start);
                            int l = 0;
                            for (; l < farthest; l++)
                            {
                                if (uncompressed[possible + l] != uncompressed[current + l])
                                {
                                    if (l > max_length)
                                    {
                                        max_length = l;
                                        max_distance = current - possible;
                                    }
                                    break;
                                }
                            }

                            if (l == farthest)
                            {
                                max_length = farthest;
                                max_distance = current - possible;
                                break;
                            }
                        }

                        if (max_length >= 3)
                        {
                            for (int j = 0; j < max_length; j++)
                            {
                                byte value = uncompressed[current + j];
                                lookup[value].AddFirst(current + j);
                            }

                            current += max_length;

                            // We hit a match, so add it to the output
                            int t = (max_distance - 1) & 0xFFF;
                            t |= (((max_length - 3) & 0xF) << 12);
                            temp.Add((byte)((t >> 8) & 0xFF));
                            temp.Add((byte)(t & 0xFF));

                            // Set the control bit
                            control |= (1 << (7 - i));

                            found = true;
                        }
                    }

                    if (!found)
                    {
                        // If we didn't find any strings, copy the byte to the output
                        byte value = uncompressed[current];
                        lookup[value].AddFirst(current++);
                        temp.Add(value);
                    }
                }

                // Flush the temp buffer
                Compressed.Add((byte)(control & 0xFF));

                for (int i = 0; i < temp.Count; i++)
                    Compressed.Add(temp[i]);
            }
            while (Compressed.Count % 4 != 0)
                Compressed.Add(0);
            return Compressed.ToArray();
        }
    }
}
namespace RopeSnake.Mother3.Enemy_Graphics
{
    class CheckNulls
    {
        public byte Size, Position;
    }
    class Moved
    {
        public int start, end;
    }
    class FinalProducts
    {
        public byte[] CCG, SOB, Palette;
        public int PointerCCG, PointerSOB, PointerPAL, ToCCG, ToSOB, AddressCCG, AddressSOB;
    }
    class Importing
    {
        static int HasColourInside(Color[] a, Color b)
        {
            for (int o = 0; o < a.Length; o++)
            {
                if (AreColoursSame(a[o],b))
                    return o;
            }
            return -1;
        }
        static bool AreColoursSame(Color a, Color b)
        {
            if (a.R != b.R)
                return false;
            if (a.B != b.B)
                return false;
            if (a.G != b.G)
                return false;
            if (a.A != b.A)
                return false;
            return true;
        }
        static List<byte> ConvertImgTo4bpp(Bitmap img, int height, ref int tilewidth, ref int tileheight, out byte[] PALette)
        {
            List<int> Times = new List<int>();
            List<int> Connections = new List<int>();
            List<Color> Maxcolors = new List<Color>();
            int width = img.Width;
            int k = 0, u = 0, lenght=0;
            height = img.Height;
            for (int i = 0; i < img.Height; i++)
            {
                for (int j = 0; j < img.Width; j++)
                {
                    Color pixel = img.GetPixel(j, i);
                    for (k = 0; k < lenght; k++)
                    {
                        u = 0;
                        if ((((pixel.R / 8) & 31) == ((Maxcolors[k].R / 8) & 31)) && (((pixel.G / 8) & 31) == ((Maxcolors[k].G / 8) & 31)) && (((pixel.B / 8) & 31) == ((Maxcolors[k].B / 8) & 31)) && (pixel.A == Maxcolors[k].A))//Avoid colors that would be identical.
                        {
                            u = 1;
                            Times[k] += 1;
                            k = lenght;
                        }
                    }
                    if (u == 0)
                    {
                        lenght += 1;
                        Times.Add(0);
                        Maxcolors.Add(pixel);
                    }
                }
            }
            List<int> SortedTimes = Times.OrderByDescending(o => o).ToList();
            for (k = 0; k < lenght; k++)
            {
                for (u = 0; u < lenght; u++)
                {
                    if ((SortedTimes[k] == Times[u]) && (!Connections.Exists(x => x == u)))
                    {
                        Connections.Add(u);
                        u = lenght;
                    }
                }
            }
            if (Connections[0] != 0)
            {
                if (Connections.IndexOf(0) <= 15)
                    Connections[Connections.IndexOf(0)] = Connections[0];
                else
                    Connections[15] = Connections[0];
                Connections[0] = 0;
            }
            u = 0;
            List<Color> FinalColors = new List<Color>();
            for (k = 0; k < lenght; k++)
            {
                FinalColors.Add(Maxcolors[Connections[k]]);
            }
            u = 0;
            if (FinalColors.Exists(x => x.A <= 128))
            {
                while (u == 0)
                {
                    if ((FinalColors.FindIndex(x => x.A <= 128)) != (FinalColors.FindLastIndex(x => x.A <= 128)))
                    {
                        FinalColors.RemoveAt(FinalColors.FindLastIndex(x => x.A <= 128));
                    }
                    else
                        u = 1;
                }
            }
            while ((width % 8) != 0)
                width += 1;
            while ((height % 8) != 0)
                height += 1;
            tileheight = height / 8;
            tilewidth = width / 8;
            if ((tileheight * tilewidth) > 1024)
            {
                Console.WriteLine("Error! Image is too big!");
                Environment.Exit(1);
            }
            if (FinalColors.Count > 16)
                FinalColors.RemoveRange(16, FinalColors.Count - 16);
            else
                while (FinalColors.Count < 16)
                    FinalColors.Add(Color.Black);
            List<Byte> Realhex = new List<Byte>();
            for (k = 0; k < FinalColors.Count; k++)
            {
                int B = ((FinalColors[k].B >> 3) & 31) << 10;
                int G = ((FinalColors[k].G >> 3) & 31) << 5;
                int R = ((FinalColors[k].R >> 3) & 31);
                Realhex.Add((byte)((B + G + R) & 0xFF));
                Realhex.Add((byte)(((B + G + R) >> 8) & 0xFF));
            }
            while (Realhex.Count < 32)
                Realhex.Add(0);
            PALette = Realhex.ToArray();
            Realhex = new List<Byte>();
            byte rest;
            for (u = 0; u < tileheight; u++)
            {
                for (k = 0; k < tilewidth; k++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            rest = 0;
                            for(int r=0; r<2; r++)
                            if (((u * 8) + j) < img.Height)
                            {
                                if ((8 * k) + (i * 2) +r < img.Width)
                                {
                                    Color pixel = img.GetPixel((8 * k) + (i * 2) +r, (u * 8) + j);
                                    if (HasColourInside(FinalColors.ToArray(), pixel) != -1)
                                    {
                                        rest += (Byte)((HasColourInside(FinalColors.ToArray(), pixel))<<(r*4));
                                    }
                                    else
                                    {
                                        int[] Similar = new int[FinalColors.Count];
                                        for (int d = 0; d < FinalColors.Count; d++)
                                        {
                                            Similar[d] = 0;
                                                Similar[d] += 1000 * Math.Abs(FinalColors[d].A - pixel.A);
                                                Similar[d] += Math.Abs(FinalColors[d].B - pixel.B);
                                                Similar[d] += Math.Abs(FinalColors[d].G - pixel.G);
                                                Similar[d] += Math.Abs(FinalColors[d].R - pixel.R);
                                        }
                                        int e = 1000000000;
                                        int MostSimilar = 0;
                                        for (int d = 0; d < FinalColors.Count; d++)
                                        {
                                            if (Similar[d] <= e)
                                            {
                                                e = Similar[d];
                                                MostSimilar = d;
                                            }
                                        }
                                        rest += (Byte)((MostSimilar)<<(r*4));
                                    }
                                }
                            }
                            Realhex.Add(rest);
                        }
                    }

                }
            }
            for (k = 0; k <= 31; k++)
            {
                Realhex.Add(255);//Prepare the image for CCG compression.
            }
            return Realhex;
        }
        static Bitmap MergeTwo32ArgbFrontBack(Bitmap firstImage, Bitmap secondImage)
        {
            if (firstImage == null)
            {
                throw new ArgumentNullException("firstImage");
            }
            if (secondImage == null)
            {
                throw new ArgumentNullException("secondImage");
            }
            int outputImageWidth = Math.Max(firstImage.Width, secondImage.Width);
            int outputImageHeight = firstImage.Height + secondImage.Height;
            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(firstImage, new Rectangle(new Point(), firstImage.Size),
                    new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(new Point(0, firstImage.Height), secondImage.Size),
                    new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
            }
            return outputImage;
        }
        static bool CheckIfHFlipStart(Bitmap a)
        {
            if (a.Width < 8)
                return false;
            for (int i = 0; i < a.Height; i++)
                for (int j = 0; j < a.Width / 2; j++)
                    if (!AreColoursSame(a.GetPixel(j, i), a.GetPixel(a.Width - j -1, i)))
                        return false;
            return true;
        }
        static Bitmap ChangeIfHFlipStart(Bitmap a)
        {
            Color begin = a.GetPixel(0, 0);
            for (int i = 0; i < a.Height; i++)
                for (int j = 0; j < a.Width / 2; j++)
                    a.SetPixel(j + (a.Width / 2) + (a.Width % 2), i, begin);
            return a;
        }
        static List<Byte> UniteHex(List<Byte> a, List<Byte> b, int Width1, int Width2, int Height1, int Height2)
        {
            List<Byte> c = new List<byte>();
            for (int j = 0; j < Height1; j++)
                for (int i = 0; i < Math.Max(Width1, Width2); i++)
                {
                    for (int u = 0; u < 32; u++)
                    {
                        if (i >= Width1)
                            c.Add(0);
                        else
                            c.Add(a[((j * Width1) * 32) + (i * 32) + u]);
                    }
                }
            for (int j = 0; j < Height2; j++)
                for (int i = 0; i < Math.Max(Width1, Width2); i++)
                {
                    for (int u = 0; u < 32; u++)
                    {
                        if (i >= Width2)
                            c.Add(0);
                        else
                            c.Add(b[((j * Width2) * 32) + (i * 32) + u]);
                    }
                }
            return c;
        }
        static int InsertPointer(byte[] memblock, byte[] a, int Pointer, int LastUsed)
        {
            memblock[Pointer] = (byte)(((LastUsed - FreeSpace.Pointers.Base)) & 0xFF);
            memblock[Pointer + 1] = (byte)(((LastUsed - FreeSpace.Pointers.Base) >> 8) & 0xFF);
            memblock[Pointer + 2] = (byte)(((LastUsed - FreeSpace.Pointers.Base) >> 16) & 0xFF);
            memblock[Pointer + 3] = (byte)(((LastUsed - FreeSpace.Pointers.Base) >> 24) & 0xFF);
            memblock[Pointer + 4] = (byte)(((a.Length)) & 0xFF);
            memblock[Pointer + 5] = (byte)(((a.Length) >> 8) & 0xFF);
            memblock[Pointer + 6] = (byte)(((a.Length) >> 16) & 0xFF);
            memblock[Pointer + 7] = (byte)(((a.Length) >> 24) & 0xFF);
            for (int i = 0; i < a.Length; i++)
                memblock[LastUsed + i] = a[i];
            return a.Length + LastUsed;
        }
        static void InsertOldPointer(byte[] memblock, int Pointer, int OldPointsTo, byte[] a)
        {
            memblock[Pointer] = (byte)(((OldPointsTo - FreeSpace.Pointers.Base)) & 0xFF);
            memblock[Pointer + 1] = (byte)(((OldPointsTo - FreeSpace.Pointers.Base) >> 8) & 0xFF);
            memblock[Pointer + 2] = (byte)(((OldPointsTo - FreeSpace.Pointers.Base) >> 16) & 0xFF);
            memblock[Pointer + 3] = (byte)(((OldPointsTo - FreeSpace.Pointers.Base) >> 24) & 0xFF);
            memblock[Pointer + 4] = (byte)(((a.Length)) & 0xFF);
            memblock[Pointer + 5] = (byte)(((a.Length) >> 8) & 0xFF);
            memblock[Pointer + 6] = (byte)(((a.Length) >> 16) & 0xFF);
            memblock[Pointer + 7] = (byte)(((a.Length) >> 24) & 0xFF);
        }
        static bool AreArraySame(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }
        static bool AreArraySame(byte[,] a, int index, byte[] b)
        {
            for (int i = 0; i < b.Length; i++)
                if (a[index,i] != b[i])
                    return false;
            return true;
        }
        static bool BackFront(byte[,] Tile, byte[,]BackTile, int Frontheight, int Backheight, int Frontwidth, int Backwidth)
        {
            if ((Backheight!=Frontheight)||(Frontwidth!=Backheight))
                return false;
            for (int h = 0; h < Frontheight; h++)
                for (int g = 0; g < Frontwidth; g++)
                    for(int f=0; f<32; f++)
                    if (!(Tile[(h*Frontwidth)+g, f]==BackTile[(h*Frontwidth)+g, f]))
                        return false;
            return true;
        }
        static char ConvertHexToChar(int a)
        {
            switch (a)
            {
                case 1:
                    return '1';
                case 2:
                    return '2';
                case 3:
                    return '3';
                case 4:
                    return '4';
                case 5:
                    return '5';
                case 6:
                    return '6';
                case 7:
                    return '7';
                case 8:
                    return '8';
                case 9:
                    return '9';
                case 10:
                    return 'A';
                case 11:
                    return 'B';
                case 12:
                    return 'C';
                case 13:
                    return 'D';
                case 14:
                    return 'E';
                case 15:
                    return 'F';
                default:
                    return '0';
            }
        }
        static List<List<CheckNulls>> CheckForEmpty(List<List<byte>> Tile, int Tilestart, int YSize, int XSize, byte l)
        {
            List<List<Byte>> TempTile = new List<List<byte>>();
            List<Byte> Compare0 = new List<byte>();
            for (int i = 0; i < 32; i++)
                Compare0.Add(0);
            List<List<CheckNulls>> Resolution = new List<List<CheckNulls>>();
            for (int i = 0; i < YSize; i++)
                for (int j = 0; j < l; j++)
                    TempTile.Add(Tile[Tilestart + (i * l) + j]);
            for (byte i = 0; i < YSize; i++)
            {
                CheckNulls a = new CheckNulls();
                Resolution.Add(new List<CheckNulls>());
                for (int j = 0; j < XSize; j++)
                {
                    if (AreArraySame(TempTile[(i * l) + j].ToArray(), Compare0.ToArray()))
                    {
                        if (j > 0)
                            a.Size += 1;
                        else
                            a.Size = 1;
                    }
                    else
                        break;
                }
                if (a.Size == XSize)
                {
                    a.Position = 0;
                    Resolution[i].Add(a);
                }
            }
            int rightSize = XSize;
            for (byte i = 0; i < YSize; i++)
            {
                int foundSize = 0;
                for (int j = 0; j < XSize; j++)
                {
                    if (AreArraySame(TempTile[(i * l) + j].ToArray(), Compare0.ToArray()))
                    {
                        if (j > 0)
                            foundSize += 1;
                        else
                            foundSize = 1;
                    }
                    else
                        break;
                }
                rightSize = Math.Min(rightSize, foundSize);
            }
            int leftSize = XSize;
            for (byte i = 0; i < YSize; i++)
            {
                int foundSize = 0;
                for (int j = XSize - 1; j >= 0; j--)
                {
                    if (AreArraySame(TempTile[(i * l) + j].ToArray(), Compare0.ToArray()))
                    {
                        if (j < XSize - 1)
                            foundSize += 1;
                        else
                            foundSize = 1;
                    }
                    else
                        break;
                }
                leftSize = Math.Min(leftSize, foundSize);
            }
            if (rightSize != 0 && leftSize != 0)
            {
                for (byte i = 0; i < YSize; i++)
                    if (Resolution[i].Count == 0)
                    {
                        CheckNulls a = new CheckNulls();
                        a.Position = 0;
                        a.Size = (byte)rightSize;
                        Resolution[i].Add(a);
                        a = new CheckNulls();
                        a.Position = (byte)(XSize - leftSize);
                        a.Size = (byte)leftSize;
                        Resolution[i].Add(a);
                    }

            }
            else if (rightSize != 0)
            {
                for (byte i = 0; i < YSize; i++)
                    if (Resolution[i].Count == 0)
                    {
                        CheckNulls a = new CheckNulls();
                        a.Position = 0;
                        a.Size = (byte)rightSize;
                    }

            }
            else if (leftSize != 0)
            {
                for (byte i = 0; i < YSize; i++)
                    if (Resolution[i].Count == 0)
                    {
                        CheckNulls a = new CheckNulls();
                        a.Position = (byte)(XSize - leftSize);
                        a.Size = (byte)leftSize;
                        Resolution[i].Add(a);
                    }
            }
            return Resolution;
        }
        static void Fixing(ref List<List<CheckNulls>> Removal, int Pos, int XSize)
        {
            CheckNulls a = new CheckNulls();
            a.Position = 0;
            a.Size = 0;
            if (Removal[Pos].Count > 0)
            {
                if (Removal[Pos][0].Position != 0)
                    Removal[Pos].Insert(0, a);
            }
            else
                Removal[Pos].Add(a);
            a = new CheckNulls();
            a.Size = 0;
            a.Position = (byte)XSize;
            if (Removal[Pos][Removal[Pos].Count - 1].Position + Removal[Pos][Removal[Pos].Count - 1].Size < XSize)
                Removal[Pos].Add(a);
        }
        static Byte[,] Finalize(Byte[,] Tile, Byte[] SOB, int Tilewidth)
        {
            int a;
            List<List<Byte>> Finalized = new List<List<byte>>();
            List<Moved> Changes = new List<Moved>();
            int Tilecount = 0;
            for (int g = 0; g < 2; g++)
            {
                if ((g==1)&&((SOB[8] + (SOB[9] << 8)) == (SOB[10] + (SOB[11] << 8))))
                {
                    break;
                }
                int OAMNum = SOB[(SOB[8 + (g * 2)] + (SOB[9 + (g * 2)] << 8)) + 2] + (SOB[(SOB[8 + (g * 2)] + (SOB[9 + (g * 2)] << 8)) + 3] << 8);
                a = (SOB[8+(g*2)] + (SOB[9 + (g * 2)] << 8)) + 4;
                Tilecount = FinalizeInnerCycle(OAMNum, a, 8, SOB, Finalized, Changes, Tilewidth, Tile, Tilecount);
            }
            Byte[,] Final = new Byte[Finalized.Count, 32];
            for (int i = 0; i < Finalized.Count; i++)
                for (int u = 0; u < Finalized[i].Count; u++)
                    Final[i, u] = Finalized[i][u];
            return Final;
        }
        public static Byte[] Finalize(List<List<byte>> Tile, List<Byte>[] OAM, int Tilewidth, int OAMSize)
        {
            Byte[,] convertedTiles = new Byte[Tile.Count, 0x20];
            for (int i = 0; i < Tile.Count; i++)
                for (int k = 0; k < 0x20; k++)
                    convertedTiles[i, k] = Tile[i][k];
            List<List<Byte>> Finalized = new List<List<byte>>();
            List<Moved> Changes = new List<Moved>();
            int Tilecount = 0;
            for (int i = 0; i < OAM.Length; i++)
            {
                byte[] tmpOAM = OAM[i].ToArray();
                Tilecount = FinalizeInnerCycle(OAM[i].Count / OAMSize, 0, OAMSize, tmpOAM, Finalized, Changes, Tilewidth, convertedTiles, Tilecount);
                OAM[i] = tmpOAM.ToList();
            }
            Byte[] Final = new Byte[Finalized.Count * 0x20];
            for (int i = 0; i < Finalized.Count; i++)
                for (int u = 0; u < 0x20; u++)
                    Final[(i * 0x20) + u] = Finalized[i][u];
            return Final;
        }
        public static Byte[] Finalize(List<List<byte>> Tile, ref List<Byte> OAM, int Tilewidth, int OAMSize)
        {
            Byte[,] convertedTiles = new Byte[Tile.Count, 0x20];
            for (int i = 0; i < Tile.Count; i++)
                for (int k = 0; k < 0x20; k++)
                    convertedTiles[i, k] = Tile[i][k];
            List<List<Byte>> Finalized = new List<List<byte>>();
            List<Moved> Changes = new List<Moved>();
            byte[] tmpOAM = OAM.ToArray();
            FinalizeInnerCycle(OAM.Count / OAMSize, 0, OAMSize, tmpOAM, Finalized, Changes, Tilewidth, convertedTiles, 0);
            OAM = tmpOAM.ToList();
            Byte[] Final = new Byte[Finalized.Count * 0x20];
            for (int i = 0; i < Finalized.Count; i++)
                for (int u = 0; u < 0x20; u++)
                    Final[(i * 0x20) + u] = Finalized[i][u];
            return Final;
        }
        static int FinalizeInnerCycle(int OAMNum, int a, int OAMSize, byte[] OAMArray, List<List<byte>> Finalized, List<Moved> Changes, int Tilewidth, byte[,] Tile, int Tilecount)
        {
            int Shape, Size, Tilestart, XSize, YSize;
            while (OAMNum > 0)
            {
                a++;
                Shape = OAMArray[a++] & 0xC0;
                a++;
                int Flips = (OAMArray[a] >> 4) & 0x3;
                Size = OAMArray[a++] & 0xC0;
                if (Size % 2 != 0)
                    Size -= 1;
                Moved temporary = new Moved();
                Tilestart = OAMArray[a] + (OAMArray[a + 1] << 8);
                OAMArray[a] = (Byte)(Tilecount & 0xFF);
                OAMArray[a + 1] = (Byte)((Tilecount >> 8) & 0x3);
                temporary.start = Tilestart;
                temporary.end = OAMArray[a] + ((OAMArray[a + 1] & 0x3) << 8);
                Changes.Add(temporary);
                GBA.OAM.getSizesOAM(Shape, Size, out XSize, out YSize);
                if (Flips == 0)
                    for (int i = 0; i < YSize; i++)
                    {
                        for (int j = 0; j < XSize; j++)
                        {
                            Finalized.Add(new List<byte>());
                            for (int k = 0; k < 0x20; k++)
                            {
                                if (Tilestart + (i * Tilewidth) + j < Tile.Length / 32)
                                    Finalized[Tilecount].Add(Tile[Tilestart + (i * Tilewidth) + j, k]);
                                else
                                    Finalized[Tilecount].Add(0);
                            }
                            Tilecount++;
                        }
                    }
                else
                {
                    int tilend = 0, i = 0;
                    for (; (i < Changes.Count) && (Tilestart != Changes[i].start); i++) ;
                    tilend = Changes[i].end;
                    OAMArray[a] = (Byte)(tilend & 0xFF);
                    OAMArray[a + 1] = (Byte)((tilend >> 8) & 0x3);
                }
                a += OAMSize - 4;
                OAMNum -= 1;
            }
            return Tilecount;
        }
        static int CheckSizeX(List<byte> OAMList, int index)
        {
                switch (OAMList[1 + index] >> 6)
                {
                    case 0:
                        switch (OAMList[3 + index] >> 6)
                        {
                            case 0:
                            return 8;
                            case 1:
                            return 16;
                            case 2:
                            return 32;
                            default:
                            return 64;
                        }
                    case 1:
                        switch (OAMList[3 + index] >> 6)
                        {
                            case 0:
                            return 16;
                            case 1:
                            return 32;
                            case 2:
                            return 32;
                            default:
                            return 64;
                        }
                    default:
                        switch (OAMList[3 + index] >> 6)
                        {
                            case 0:
                            return 8;
                            case 1:
                            return 8;
                            case 2:
                                return 16;
                            default:
                                return 32;
                        }
                }
        }
        static byte[,] checkForSame(byte[,] Tile, byte[] SOB)
        {
            List<GBA.OAM> Entries = GBA.OAM.OAMGet(SOB, 0);
            Entries.Sort(GBA.OAM.compareTiles);
            bool[] foundRepeated = new bool[Entries.Count];
            int[] repeatOf = new int[Entries.Count];
            bool foundOne = false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (!foundRepeated[i])
                {
                    int width = Entries[i].Width;
                    int height = Entries[i].Height;
                    for (int j = i + 1; j < Entries.Count; j++)
                    {
                        if (Entries[j].Width == width && Entries[j].Height == height)
                        {
                            bool different = false;
                            if (Entries[j].Tile == Entries[i].Tile)
                                different = true;
                            for (int y = 0; !different && y < height; y++)
                                for (int x = 0; !different && x < width; x++)
                                    for (int k = 0; k < 32; k++)
                                        if (Tile[Entries[j].Tile + x + (y * width), k] != Tile[Entries[i].Tile + x + (y * width), k])
                                        {
                                            different = true;
                                            break;
                                        }
                            if (!different)
                            {
                                foundOne = true;
                                foundRepeated[j] = true;
                                repeatOf[j] = i;
                            }
                            while (j + 1 < Entries.Count && Entries[j].Tile == Entries[j + 1].Tile)
                                j++;
                        }
                    }
                }
                while (i + 1 < Entries.Count && Entries[i].Tile == Entries[i + 1].Tile)
                    i++;
            }
            if (!foundOne)
                return Tile;
            int[] initialTilestarts = new int[Entries.Count];
            for (int i = 0; i < Entries.Count; i++)
                initialTilestarts[i] = Entries[i].Tile;
            int totalSubtract = 0;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (foundRepeated[i])
                {
                    int singleSubtract = Entries[i].Width * Entries[i].Height;
                    totalSubtract += singleSubtract;
                    int startOfIt = i + 1;
                    Entries[i].Tile = Entries[repeatOf[i]].Tile;
                    Entries[i].setSOBEntryTile(SOB);
                    while (startOfIt < Entries.Count && initialTilestarts[i] == initialTilestarts[startOfIt])
                    {
                        Entries[startOfIt].Tile = Entries[repeatOf[i]].Tile;
                        Entries[startOfIt].setSOBEntryTile(SOB);
                        foundRepeated[startOfIt] = true;
                        startOfIt++;
                    }
                    for (int j = startOfIt; j < Entries.Count; j++)
                    {
                        Entries[j].Tile -= singleSubtract;
                        Entries[j].setSOBEntryTile(SOB);
                    }
                }
                while (i + 1 < Entries.Count && Entries[i].Tile == Entries[i + 1].Tile)
                    i++;
            }
            byte[,] NewTile = new byte[(Tile.Length / 32) - totalSubtract, 32];
            for (int i = 0; i < Entries.Count; i++)
            {
                if (!foundRepeated[i])
                {
                    for (int y = 0; y < Entries[i].Height; y++)
                        for (int x = 0; x < Entries[i].Width; x++)
                        {
                            for (int k = 0; k < 32; k++)
                                NewTile[Entries[i].Tile + x + (y * Entries[i].Width), k] = Tile[initialTilestarts[i] + x + (y * Entries[i].Width), k];
                        }
                }
                while (i + 1 < Entries.Count && Entries[i].Tile == Entries[i + 1].Tile)
                    i++;
            }
            return NewTile;
        }
        static byte[] OAMFixHFlip(byte[] OAMListInsert, bool IsHFlipped, int rest)
        {
            if (IsHFlipped)
            {
                List<byte> OAMList = new List<byte>();
                OAMList.AddRange(OAMListInsert);
                int n = OAMList.Count / 8;
                int max = 0;
                int size = 0;
                for(int i=0; i<n; i++)
                {
                    size = CheckSizeX(OAMList, i * 8);
                    max = Math.Max(OAMList[2 + (i * 8)] + ((OAMList[3 + (i * 8)] & 0x1) << 8) + size, max);
                    for (int j = 0; j < 8; j++)
                        if (j != 3)
                            OAMList.Add(OAMList[j + (i * 8)]);
                        else
                            OAMList.Add((byte)(OAMList[j + (i * 8)] + 0x10));
                }
                for(int i=n; i<2*n; i++)
                {
                    size = CheckSizeX(OAMList, i * 8);
                    size = -size -OAMList[2 + (i * 8)] - ((OAMList[3 + (i * 8)] & 0x1) << 8) + (2*max)-rest;
                    OAMList[2 + (i * 8)] = (byte)(size & 0xFF);
                    OAMList[3 + (i * 8)] = (byte)((size>>8 & 0x1)+((OAMList[3 + (i * 8)]>>1)<<1));
                }
                return OAMList.ToArray();
            }
            return OAMListInsert;
        }
        static void OAMAdd(ref List<Byte> OAMList, int Tilestart, List<List<Byte>> Tile, int y, int l, int x, byte size, byte Shape, byte Height, int Width, int YSizeTemp, int XSizeTemp, bool expanded)
        {
            OAMList.Add(Height);
            OAMList.Add(Shape);
            OAMList.Add((Byte)((Width) & 0xFF));
            OAMList.Add((Byte)((size) + (((Width) >> 8) & 0x1)));
            OAMList.Add((Byte)((Tilestart + (y * l) + x) & 0xFF));
            OAMList.Add((Byte)(((Tilestart + (y * l) + x) >> 8) & 0x3));
            if (expanded)
            {
                OAMList.Add(0);
                OAMList.Add(0);
            }
        }
        static int MakeWidth(int X, int temp)
        {
            int Width = X + ((temp) << 3);
            while (Width < 0)
                Width += 0x200;
            while (Width > 0x1FF)
                Width -= 0x200;
            return Width;
        }
        static Byte[] SOBGen(Byte[] OAMFront, Byte[] OAMBack, int TileheightFront, int TilewidthFront, int TilewidthBack, int Back)
        {
            List<Byte> SOB = new List<byte>();
            SOB.Add(0x73);
            SOB.Add(0x6F);
            SOB.Add(0x62);
            SOB.Add(0x20);
            if (Back >= 1)
            {
                SOB.Add(2);
                SOB.Add(0);
                SOB.Add(2);
                SOB.Add(0);
                SOB.Add(0x10);
                SOB.Add(0);
                SOB.Add((Byte)(((0x14 + OAMFront.Count())) & 0xFF));
                SOB.Add((Byte)(((0x14 + OAMFront.Count()) >> 8) & 0xFF));
                SOB.Add((Byte)(((0x18 + OAMFront.Count() + OAMBack.Count()))&0xFF));
                SOB.Add((Byte)(((0x18 + OAMFront.Count() + OAMBack.Count()) >> 8) & 0xFF));
                SOB.Add((Byte)(0x18 + OAMFront.Count() + OAMBack.Count()));
                SOB.Add((Byte)(((0x18 + OAMFront.Count() + OAMBack.Count()) >> 8) & 0xFF));
            }
            else
            {
                SOB.Add(2);
                SOB.Add(0);
                SOB.Add(2);
                SOB.Add(0);
                SOB.Add(0x10);
                SOB.Add(0);
                SOB.Add(0x10);
                SOB.Add(0);
                SOB.Add((Byte)(((0x14 + OAMFront.Count())) & 0xFF));
                SOB.Add((Byte)(((0x14 + OAMFront.Count()) >> 8) & 0xFF));
                SOB.Add((Byte)(((0x14 + OAMFront.Count())) & 0xFF));
                SOB.Add((Byte)(((0x14 + OAMFront.Count()) >> 8) & 0xFF));
            }
            SOB.Add(0);
            SOB.Add(0);
            SOB.Add((Byte)((OAMFront.Count() / 8) & 0xFF));
            SOB.Add((Byte)(((OAMFront.Count() / 8) >> 8) & 0xFF));
            SOB.AddRange(OAMFront);
            if (Back >= 1)
            {
                SOB.Add(0);
                SOB.Add(0);
                SOB.Add((Byte)((OAMBack.Count() / 8) & 0xFF));
                SOB.Add((Byte)(((OAMBack.Count() / 8) >> 8) & 0xFF));
                for (int k = 0; k < OAMBack.Count(); k++)
                {
                    if (k % 8 != 4)
                        SOB.Add(OAMBack[k]);
                    else
                    {
                        SOB.Add((Byte)((OAMBack[k] + (Math.Max(TilewidthFront, TilewidthBack) * TileheightFront)) & 0xFF));
                        SOB.Add(((Byte)(OAMBack[k + 1] + (((OAMBack[k] + (Math.Max(TilewidthFront, TilewidthBack) * TileheightFront)) >> 8) & 0x3))));
                        k++;
                    }
                }
            }
            SOB.Add(4);
            SOB.Add(0);
            SOB.Add(1);
            SOB.Add(0);
            SOB.Add(0);
            SOB.Add(0);
            SOB.Add(0);
            SOB.Add(0);
            SOB.Add(0x7E);
            SOB.Add(0x73);
            SOB.Add(0x6F);
            SOB.Add(0x62);
            return SOB.ToArray();
        }
        static Byte[,] TileOAM(Byte[,] Tile, int Tileheight, int Tilewidth, out int Modiheight, out int Modiwidth, out Byte[] OAM, bool IsHFlipped)
        {
            Modiwidth = Tilewidth;
            Modiheight = 0;
            int Limit = Tileheight, times = 0, more = 0, mors = 0;
            List<List<Byte>> TempTiles = new List<List<Byte>>();
            List<List<Byte>> TumpTiles = new List<List<Byte>>();
            int o = Limit;
            if (Tilewidth > 4)
                Modiwidth = 8;
            else if (Tilewidth > 2)
                Modiwidth = 4;
            for (int i = 0; i < Limit; i++)
            {
                Modiheight += 1;
                for (int k = 0; k < Modiwidth; k++)
                {
                    TumpTiles.Add(new List<Byte>());
                    if (k >= Tilewidth)
                    {
                        for (int j = 0; j < 32; j++)
                            TumpTiles[(i * Modiwidth) + k].Add(0);
                    }
                    else
                    {
                        for (int j = 0; j < 32; j++)
                            TumpTiles[(i * Modiwidth) + k].Add(Tile[(i * Tilewidth) + k, j]);
                    }
                }
            }
            int Singularwidth, Singularheight, XAdd = 0;
            List<int> X = new List<int>(), Y = new List<int>(), XSize = new List<int>(), YSize = new List<int>(), Tilestart = new List<int>();
            X.Add(0);
            Y.Add(0);
            XSize.Add(Modiwidth);
            YSize.Add(Tileheight);
            Tilestart.Add(0);
            if (Tileheight > 1)
                TumpTiles = RemoveExceedTiles(TumpTiles, Modiwidth, Tileheight, out Singularwidth, out Singularheight, ref Modiheight, ref XSize, ref YSize, ref X, ref Y, ref XAdd);
            else
            {
                Singularheight = 1; Singularwidth = 1;
            }
            if (TumpTiles.Count > 0)
                TumpTiles = FixTile(TumpTiles, Singularwidth, Tilewidth, ref Singularheight, ref Modiheight, ref XSize, ref YSize);
            else
            {
                List<Byte> s = new List<Byte>();
                for (int c = 0; c < 32; c++)
                    s.Add(0);
                TumpTiles.Add(s);
                Singularheight = 1;
                Tilewidth = 1;
                Tileheight = 1;
            }
            int XTemp = X[X.Count() - 1] + XSize[XSize.Count() - 1];
            MoveTile(ref TumpTiles, Singularwidth, ref Singularheight, ref Modiheight, Tilewidth, XAdd, ref XSize, ref YSize, ref X, ref Y, ref Tilestart);
            TempTiles.AddRange(TumpTiles);
            TumpTiles = new List<List<Byte>>();
            times += 1;
            if (Tilewidth > 8)
            {
                for (int g = 0; g < ((Tilewidth - 1) / 8); g++)
                {
                    Tilestart.Add(Modiheight * 8);
                    mors = more;
                    for (int i = 0; i < Limit; i++)
                    {
                        Modiheight += 1;
                        for (int k = 0; k < 8; k++)
                        {
                            TumpTiles.Add(new List<Byte>());
                            if ((8 * times + k) >= Tilewidth)
                            {
                                for (int j = 0; j < 32; j++)
                                    TumpTiles[(i * 8) + k].Add(0);
                            }
                            else
                            {
                                for (int j = 0; j < 32; j++)
                                    TumpTiles[(i * 8) + k].Add(Tile[(i * Tilewidth) + 8 * times + k, j]);
                            }
                        }
                    }
                    X.Add(XTemp + XAdd);
                    Y.Add(0);
                    XSize.Add(8);
                    YSize.Add(Tileheight);
                    TumpTiles = RemoveExceedTiles(TumpTiles, 8, Tileheight, out Singularwidth, out Singularheight, ref Modiheight, ref XSize, ref YSize, ref X, ref Y, ref XAdd);
                    TumpTiles = FixTile(TumpTiles, Singularwidth, Tilewidth, ref Singularheight, ref Modiheight, ref XSize, ref YSize);
                    XTemp = X[X.Count() - 1] + XSize[XSize.Count() - 1];
                    MoveTile(ref TumpTiles, Singularwidth, ref Singularheight, ref Modiheight, Tilewidth, XAdd, ref XSize, ref YSize, ref X, ref Y, ref Tilestart);
                    TempTiles.AddRange(TumpTiles);
                    TumpTiles = new List<List<Byte>>();
                    times += 1;
                }
            }
            OAM = OAMGen(TempTiles, XSize, YSize, X, Y, Tilestart, Tilewidth, Tileheight, IsHFlipped).ToArray();
            Limit = TempTiles.Count();
            Byte[,] Newtiles = new Byte[Limit, 32];
            for (int i = 0; i < Limit; i++)
            {
                for (int k = 0; k < 32; k++)
                    Newtiles[i, k] = TempTiles[i][k];
            }
            return Newtiles;
        }
        static void MoveTile(ref List<List<Byte>> TempTiles, int Tilewidth, ref int TileheightMod, ref int Modiheight, int RealTileWidth, int XAdd, ref List<int> XSize, ref List<int> YSize, ref List<int> X, ref List<int> Y, ref List<int> Tilestart)
        {
            List<byte> Compare0 = new List<byte>();
            for (int j = 0; j < 32; j++)
                Compare0.Add(0);
            int l = RealTileWidth, YOriginal;
            if (RealTileWidth >= 5)
                l = 8;
            else if (RealTileWidth >= 3)
                l = 4;
            int mors, OriginalStart = Tilestart[Tilestart.Count() - 1];
            for (mors = TileheightMod; (mors % 2) != 0; mors++) ;
            int morn = (mors / l) * 2;
            int Tileheight = TileheightMod;
            int XTemp = X[X.Count() - 1];
            switch((Tilewidth % l))
            {
                case 5:
                case 6:
                        if (morn > 0)
                            XSize[XSize.Count() - 1] -= 2;
                        YSize[YSize.Count() - 1] -= morn;
                        YOriginal = Y[Y.Count() - 1] + YSize[YSize.Count() - 1];
                        for (int g = 0; g < 3; g++)
                        {
                            YSize.Add(morn);
                            Y.Add(YOriginal);
                            Tilestart.Add(6 + ((g * morn) * l) + OriginalStart);
                            XSize.Add(2);
                            X.Add(XTemp + (g * 2));
                            for (int i = 0; i < morn; i++)
                            {
                                for (int k = 0; k < 2; k++)
                                    TempTiles[((i + (g * morn)) * l) + k + 6] = TempTiles[((i + (Tileheight - morn)) * l) + (g * 2) + k];
                            }
                            int temp = 0;
                            for (int i = 0; i < morn; i++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (TempTiles[((i + (g * morn)) * l) + k + 6] == Compare0)
                                        temp += 1;
                                }
                            }
                            if (temp == (2 * morn))
                            {
                                Y.RemoveAt(Y.Count() - 1);
                                X.RemoveAt(X.Count() - 1);
                                YSize.RemoveAt(YSize.Count() - 1);
                                XSize.RemoveAt(XSize.Count() - 1);
                                Tilestart.RemoveAt(Tilestart.Count() - 1);
                            }
                        }
                        Modiheight -= morn;
                        TileheightMod -= morn;
                    break;
                case 3:
                case 4:
                    if (RealTileWidth > 4)
                        if (mors / 2 > 0)
                        {
                            XSize[XSize.Count() - 1] = 4;
                            YSize[YSize.Count() - 1] -= mors / 2;
                            YOriginal = Y[Y.Count() - 1] + YSize[YSize.Count() - 1];
                            X.Add(X[X.Count() - 1]);
                            Y.Add(YOriginal);
                            XSize.Add(4);
                            YSize.Add(mors / 2);
                            Tilestart.Add(4 + OriginalStart);
                            for (int i = 0; i < mors / 2; i++)
                            {
                                for (int k = 0; k < 4; k++)
                                {
                                    TempTiles[(i * l) + k + 4] = TempTiles[((i + (mors / 2)) * l) + k];
                                }
                            }
                            Modiheight -= mors / 2;
                            TileheightMod -= mors / 2;
                        }
                    break;
                case 2:
                case 1:
                    if (RealTileWidth > 2)
                    {
                        morn = mors / 4;
                        int Countemp = X.Count() - 1;
                        if ((mors % 4) != 0)
                            morn++;
                        XSize[Countemp] = 2;
                        YOriginal = Y[Countemp] + morn;
                        for (int g = 0; g < 3; g++)
                        {
                            Tilestart.Add(OriginalStart + (2 * (g + 1)));
                            XSize.Add(2);
                            YSize.Add(0);
                            X.Add(XTemp);
                            Y.Add(YOriginal + (morn * g));
                            int temp = 0;
                            for (int i = 0; i < morn; i++)
                            {
                                if ((i + (morn * (g + 1))) < Tileheight)
                                {
                                    Modiheight -= 1;
                                    TileheightMod -= 1;
                                    YSize[Countemp] -= 1;
                                    YSize[YSize.Count() - 1] += 1;
                                }
                                for (int k = 0; k < 2; k++)
                                {
                                    if ((i + (morn * (g + 1))) < Tileheight)
                                        TempTiles[(i * l) + (g * 2) + k + 2] = TempTiles[((i + (morn * (g + 1))) * l) + k];
                                    else
                                    {
                                        for (int j = 0; j < 32; j++)
                                            TempTiles[(i * l) + (g * 2) + k + 2][j] = 0;
                                    }
                                }
                            }
                            for (int i = 0; i < morn; i++)
                            {
                                for (int k = 0; k < 2; k++)
                                {
                                    if (TempTiles[(i * l) + (g * 2) + k + 2] == Compare0)
                                        temp += 1;
                                }
                            }
                            if (temp == (2 * morn))
                            {
                                Y.RemoveAt(Y.Count() - 1);
                                X.RemoveAt(X.Count() - 1);
                                YSize.RemoveAt(YSize.Count() - 1);
                                XSize.RemoveAt(XSize.Count() - 1);
                                Tilestart.RemoveAt(Tilestart.Count() - 1);
                            }
                        }
                    }
                    break;
            }
        }
        static List<List<Byte>> FixTile(List<List<Byte>> TempTiles, int Tilewidth, int RealTileWidth, ref int Tileheight, ref int Modiheight, ref List<int> XSize, ref List<int> YSize)
        {
            int l = Tilewidth;
            List<List<Byte>> NewTile = new List<List<Byte>>();
            if (RealTileWidth > 4)
            {
                l = 8;
                XSize[XSize.Count() - 1] = 8;
            }
            else if (RealTileWidth > 2)
            {
                l = 4;
                XSize[XSize.Count() - 1] = 4;
            }
            for (int i = 0; i < Tileheight; i++)
            {
                for (int k = 0; k < l; k++)
                {
                    NewTile.Add(new List<Byte>());
                    if (k >= Tilewidth)
                    {
                        for (int j = 0; j < 32; j++)
                            NewTile[(i * l) + k].Add(0);
                    }
                    else
                        NewTile[(i * l) + k] = TempTiles[(i * Tilewidth) + k];
                }
            }
            while (Tileheight % 2 != 0)
            {
                for (int k = 0; k < l; k++)
                {
                    NewTile.Add(new List<Byte>());
                    for (int j = 0; j < 32; j++)
                        NewTile[(Tileheight * l) + k].Add(0);
                }
                Tileheight += 1;
                Modiheight += 1;
                YSize[YSize.Count() - 1] += 1;
            }
            return NewTile;
        }
        static List<Byte> FindSame(List<List<CheckNulls>> Remove)
        {
            List<byte> Correlated = new List<byte>();
            Correlated.Add(0);
            for(int i=1; i<Remove.Count; i++)
            {
                int z = 0;
                if (Remove[i].Count == Remove[i - 1].Count)
                {
                    for (; (z < Remove[i].Count)&&((Remove[i][z].Size==Remove[i-1][z].Size)&& (Remove[i][z].Position == Remove[i - 1][z].Position)); z++) ;
                }
                if (z != Remove[i].Count)
                    Correlated.Add((byte)i);
            }
            return Correlated;
        }
        public static void OAMFor(ref List<Byte> OAMList, List<List<Byte>>Tile , int Tilestart, int YSize, int XSize, int X, int Y, int l, bool extended)
        {
            byte Shape = 0, Size = 0, ttemp = 0;
            int Height, Width = 0;
            int XSizeTemp = 0;
            int YSizeTemp = 0;
            if (YSize > 8)
            {
                OAMFor(ref OAMList, Tile, Tilestart + (l * 8), YSize - 8, XSize, X, Y + (8 << 3), l, extended);
                YSize = 8;
            }
            List<List<CheckNulls>> Removal = CheckForEmpty(Tile, Tilestart, YSize, XSize, (byte)l);
            for (int i = 0; i < Removal.Count; i++)
                Fixing(ref Removal, i, XSize);
            if (Removal.Count > 0)
            {
                List<byte> SameBehaviour = FindSame(Removal);
                List<int> YSizeBehav = new List<int>();
                for (int i = 0; i < SameBehaviour.Count - 1; i++)
                    YSizeBehav.Add(SameBehaviour[i + 1] - SameBehaviour[i]);
                YSizeBehav.Add(YSize - SameBehaviour[SameBehaviour.Count - 1]);
                int tempY = 0;
                for (int i = 0; i < SameBehaviour.Count; i++)
                    if (XSize - Removal[SameBehaviour[i]][0].Size > 0)
                    {
                        for (int t = YSizeBehav[i]; t > 0;)
                        {
                            Height = Y + ((tempY) << 3);
                            switch (t)
                            {
                                case 1:
                                case 3:
                                case 5:
                                case 7:
                                    ttemp = 0;
                                    for (int z = 0; z < Removal[SameBehaviour[i]].Count - 1; z++)
                                        for (int temp = Removal[SameBehaviour[i]][z].Position + Removal[SameBehaviour[i]][z].Size, d = Removal[SameBehaviour[i]][z + 1].Position - temp; d > 0; temp += ttemp)
                                        {
                                            Width = MakeWidth(X, temp);
                                            switch (d)
                                            {
                                                case 1:
                                                case 3:
                                                    Size = 0;
                                                    Shape = 0;
                                                    d -= 1;
                                                    ttemp = 1;
                                                    XSizeTemp = 1;
                                                    YSizeTemp = 1;
                                                    break;
                                                case 2:
                                                    Size = 0x0;
                                                    Shape = 0x40;
                                                    d = 0;
                                                    XSizeTemp = 2;
                                                    YSizeTemp = 1;
                                                    break;
                                                default: //Max size is 4x1
                                                    Size = 0x40;
                                                    Shape = 0x40;
                                                    d -= 4;
                                                    ttemp = 4;
                                                    XSizeTemp = 4;
                                                    YSizeTemp = 1;
                                                    break;
                                            }
                                            OAMAdd(ref OAMList, Tilestart, Tile, tempY, l, temp, Size, Shape, (byte)Height, Width, YSizeTemp, XSizeTemp, extended);
                                        }
                                    t -= 1;
                                    tempY += 1;
                                    break;
                                case 2:
                                case 6:
                                    ttemp = 0;
                                    for (int z = 0; z < Removal[SameBehaviour[i]].Count - 1; z++)
                                        for (int temp = Removal[SameBehaviour[i]][z].Position + Removal[SameBehaviour[i]][z].Size, d = Removal[SameBehaviour[i]][z + 1].Position - temp; d > 0; temp += ttemp)
                                        {
                                            Width = MakeWidth(X, temp);
                                            switch (d)
                                            {
                                                case 1:
                                                case 3:
                                                    Size = 0;
                                                    Shape = 0x80;
                                                    d -= 1;
                                                    ttemp = 1;
                                                    XSizeTemp = 1;
                                                    YSizeTemp = 2;
                                                    break;
                                                case 2:
                                                    Size = 0x40;
                                                    Shape = 0;
                                                    d = 0;
                                                    XSizeTemp = 2;
                                                    YSizeTemp = 2;
                                                    break;
                                                default: //Max size is 4x2
                                                    Size = 0x80;
                                                    Shape = 0x40;
                                                    d -= 4;
                                                    XSizeTemp = 4;
                                                    YSizeTemp = 2;
                                                    ttemp = 4;
                                                    break;
                                            }
                                            OAMAdd(ref OAMList, Tilestart, Tile, tempY, l, temp, Size, Shape, (byte)Height, Width, YSizeTemp, XSizeTemp, extended);
                                        }
                                    t -= 2;
                                    tempY += 2;
                                    break;
                                case 4:
                                    ttemp = 0;
                                    for (int z = 0; z < Removal[SameBehaviour[i]].Count - 1; z++)
                                        for (int temp = Removal[SameBehaviour[i]][z].Position + Removal[SameBehaviour[i]][z].Size, d = Removal[SameBehaviour[i]][z + 1].Position - temp; d > 0; temp += ttemp)
                                        {
                                            Width = MakeWidth(X, temp);
                                            switch (d)
                                            {
                                                case 1:
                                                case 3:
                                                case 5:
                                                case 7:
                                                    Size = 0x40;
                                                    Shape = 0x80;
                                                    ttemp = 1;
                                                    d -= 1;
                                                    XSizeTemp = 1;
                                                    YSizeTemp = 4;
                                                    break;
                                                case 2:
                                                case 6:
                                                    Size = 0x80;
                                                    Shape = 0x80;
                                                    d -= 2;
                                                    ttemp = 2;
                                                    XSizeTemp = 2;
                                                    YSizeTemp = 4;
                                                    break;
                                                case 4:
                                                    Size = 0x80;
                                                    Shape = 0;
                                                    d -= 4;
                                                    XSizeTemp = 4;
                                                    YSizeTemp = 4;
                                                    break;
                                                default: //Case 8
                                                    Size = 0xC0;
                                                    Shape = 0x40;
                                                    d -= 8;
                                                    XSizeTemp = 8;
                                                    YSizeTemp = 4;
                                                    break;
                                            }
                                            OAMAdd(ref OAMList, Tilestart, Tile, tempY, l, temp, Size, Shape, (byte)Height, Width, YSizeTemp, XSizeTemp, extended);
                                        }
                                    t -= 4;
                                    tempY += 4;
                                    break;
                                default:
                                    ttemp = 0;
                                    for (int z = 0; z < Removal[SameBehaviour[i]].Count - 1; z++)
                                        for (int temp = Removal[SameBehaviour[i]][z].Position + Removal[SameBehaviour[i]][z].Size, d = Removal[SameBehaviour[i]][z + 1].Position - temp; d > 0; temp += ttemp)
                                        {
                                            Width = MakeWidth(X, temp);
                                            switch (d)
                                            {
                                                case 1:
                                                case 3:
                                                case 5:
                                                case 7:
                                                    Size = 0x40;
                                                    Shape = 0x80;
                                                    d -= 1;
                                                    ttemp = 1;
                                                    XSizeTemp = 1;
                                                    YSizeTemp = 4;
                                                    OAMAdd(ref OAMList, Tilestart, Tile, tempY + 4, l, temp, Size, Shape, (byte)(Height + 32), Width, YSizeTemp, XSizeTemp, extended);
                                                    break;
                                                case 2:
                                                case 6:
                                                    Size = 0x80;
                                                    Shape = 0x80;
                                                    d -= 2;
                                                    ttemp = 2;
                                                    XSizeTemp = 2;
                                                    YSizeTemp = 4;
                                                    OAMAdd(ref OAMList, Tilestart, Tile, tempY + 4, l, temp, Size, Shape, (byte)(Height + 32), Width, YSizeTemp, XSizeTemp, extended);
                                                    break;
                                                case 4:
                                                    Size = 0xC0;
                                                    Shape = 0x80;
                                                    XSizeTemp = 4;
                                                    YSizeTemp = 8;
                                                    d -= 4;
                                                    break;
                                                default: //Case 8
                                                    Size = 0xC0;
                                                    Shape = 0;
                                                    d -= 8;
                                                    XSizeTemp = 8;
                                                    YSizeTemp = 8;
                                                    break;
                                            }
                                            OAMAdd(ref OAMList, Tilestart, Tile, tempY, l, temp, Size, Shape, (byte)Height, Width, YSizeTemp, XSizeTemp, extended);
                                        }
                                    t -= 8;
                                    tempY += 8;
                                    break;
                            }
                        }
                    }
                    else
                        tempY += YSizeBehav[i];
            }
        }
        static void OAMForBase(ref List<Byte> OAMList, List<List<Byte>> Tile, int Tilestart, int YSize, int XSize, int X, int Y, int l, int OriginalHeight, int OriginalWidth, bool IsHFlipped)
        {
            if (IsHFlipped)
                OriginalWidth *= 2;
            OAMFor(ref OAMList, Tile, Tilestart, YSize, XSize, -((OriginalWidth << 3) / 2) + (X << 3), -28 - (((OriginalHeight << 3) / 2) - ((Y) << 3)), l, true);
        }
        static List<Byte> OAMGen(List<List<Byte>> Tile, List<int> XSize, List<int> YSize, List<int> X, List<int> Y, List<int> Tilestart, int OriginalWidth, int OriginalHeight, bool IsHFlipped)
        {
            List<Byte> OAMList = new List<byte>();
            int l = OriginalWidth;
            if (OriginalWidth > 4)
                l = 8;
            else if (OriginalWidth > 2)
                l = 4;
            for (int k = 0; k < (Tilestart.Count()); k++)
            {
                if (Tilestart[k] >= 1024)
                {
                    Console.WriteLine("Too many tiles in the end!");
                    Environment.Exit(01);
                }
                OAMForBase(ref OAMList, Tile, Tilestart[k], YSize[k], XSize[k], X[k], Y[k], l, OriginalHeight, OriginalWidth, IsHFlipped);
            }
            return OAMList;
        }
        static List<List<Byte>> RemoveExceedTiles(List<List<Byte>> Tile, int Tilewidth, int Tileheight, out int Tilewdth, out int Tilehight, ref int Modiheight, ref List<int> XSize, ref List<int> YSize, ref List<int> X, ref List<int> Y, ref int XAdd)
        {
            int temp = 0, hu = 0, hd = 0, hl = 0, hr = 0, g = 0;
            while (g == 0)
            {
                for (int u = (hu * Tilewidth); u < ((hu + 1) * Tilewidth); u++)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u][k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tilewidth))
                {
                    hu += 1;
                    Modiheight -= 1;
                    Y[Y.Count() - 1] += 1;
                    YSize[YSize.Count() - 1] -= 1;
                }
                else
                    g = 1;
                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = ((Tileheight - hd - 1) * Tilewidth); u < ((Tileheight - hd) * Tilewidth); u++)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u][k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tilewidth))
                {
                    hd += 1;
                    Modiheight -= 1;
                    YSize[YSize.Count() - 1] -= 1;
                }
                else
                    g = 1;

                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = hl; u <= (((Tileheight - 1) * (Tilewidth)) + hl); u = u + Tilewidth)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u][k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tileheight))
                {
                    hl += 1;
                    XAdd -= 1;
                    XSize[XSize.Count() - 1] -= 1;
                    X[X.Count() - 1] += 1;
                }
                else
                    g = 1;
                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = (Tilewidth - hr - 1); u < (((Tileheight) * (Tilewidth)) + Tilewidth - hr - 1); u = u + Tilewidth)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u][k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * (Tileheight)))
                {
                    hr += 1;
                    XAdd += 1;
                    XSize[XSize.Count() - 1] -= 1;
                }
                else
                    g = 1;
                temp = 0;
            }
            Tilehight = Tileheight - hu - hd;
            Tilewdth = Tilewidth - hl - hr;
            List<List<Byte>> NewTile = new List<List<Byte>>();
            for (int f = 0; f < (Tilehight); f++)
            {
                for (int d = 0; d < Tilewdth; d++)
                {
                    NewTile.Add(new List<Byte>());
                    NewTile[(d + (Tilewdth * f))] = Tile[hl + d + (Tilewidth * (f + hu))];
                }
            }
            return NewTile;
        }
        static Byte[,] RemoveExceedingTiles(byte[,] Tile, int Tilewidth, int Tileheight, ref int Backheight, out int Tilewdth, out int Tilehight)
        {
            int temp = 0, hu = 0, hd = 0, hl = 0, hr = 0, g = 0;
            while (g == 0)
            {
                for (int u = (hu * Tilewidth); u < ((hu + 1) * Tilewidth); u++)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u, k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tilewidth))
                {
                    hu += 1;
                    Backheight -= 8;
                }
                else
                    g = 1;
                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = ((Tileheight - hd - 1) * Tilewidth); u < ((Tileheight - hd) * Tilewidth); u++)
                {
                    if (u == -1) { break; }
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u, k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tilewidth))
                {
                    hd += 1;
                }
                else
                    g = 1;

                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = hl; u <= (((Tileheight - 1) * (Tilewidth)) + hl); u = u + Tilewidth)
                {
                    if (u == -1) { break; }
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u, k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * Tileheight))
                {
                    hl += 1;
                }
                else
                    g = 1;
                temp = 0;
            }
            g = 0;
            while (g == 0)
            {
                for (int u = (Tilewidth - hr - 1); u < (((Tileheight) * (Tilewidth)) + Tilewidth - hr - 1); u = u + Tilewidth)
                {
                    if (u == -1) { break; }
                    for (int k = 0; k < 32; k++)
                    {
                        if (Tile[u, k] == 0)
                            temp += 1;
                    }
                }
                if (temp == (32 * (Tileheight)))
                {
                    hr += 1;
                }
                else
                    g = 1;
                temp = 0;
            }
            Tilehight = Math.Max(Tileheight - hu - hd, 0);
            Tilewdth = Math.Max(Tilewidth - hl - hr, 0);
            Byte[,] NewTile = new Byte[(Tilehight * Tilewdth), 32];
            for (int f = 0; f < (Tilehight); f++)
            {
                for (int d = 0; d < Tilewdth; d++)
                {
                    for (int k = 0; k < 32; k++)
                    {
                        NewTile[(d + (Tilewdth * f)), k] = Tile[hl + d + (Tilewidth * (f + hu)), k];
                    }
                }
            }
            return NewTile;
        }
        static Byte[,] SeparateBack(byte[,] Tile, int Tilewidth, int Tileheight, int Back, int Backheight, out int Tilehight, out int Tilehight2, out byte[,] TileBack)
        {
            if (Back >= 1)
            {
                Tilehight = (Backheight / 8);
                Tilehight2 = Tileheight - (Backheight / 8);
            }
            else
            {
                Tilehight = Tileheight;
                Tilehight2 = 0;
            }
            Byte[,] TileFront = new Byte[Tilewidth * Tilehight, 32];
            TileBack = new Byte[Tilewidth * Tilehight2, 32];
            for (int i = 0; i < (Tilewidth * Tilehight); i++)
            {
                for (int j = 0; j < 32; j++)
                    TileFront[i, j] = Tile[i, j];
            }
            for (int i = 0; i < (Tilehight2 * Tilewidth); i++)
            {
                for (int j = 0; j < 32; j++)
                    TileBack[i, j] = Tile[i + (Tilewidth * Tilehight), j];
            }
            return TileFront;
        }
        static Byte[,] UniteTile(Byte[,] TileFront, Byte[,] TileBack, int Back, int Tilewidth, int Tileheight, int Tilewidth2, int Tileheight2, ref byte[] OAMFront, ref byte[] OAMBack, out int TilewidthUNITE, out int TileheightUNITE)
        {
            if (Tilewidth > Tilewidth2)
                TilewidthUNITE = Tilewidth;
            else
                TilewidthUNITE = Tilewidth2;
            TileheightUNITE = Tileheight + Tileheight2;
            if (TilewidthUNITE != Tilewidth)
            {
                int e = OAMFront.Count() / 8;
                for (int b = 0; b < e; b++)
                {
                    int N = OAMFront[(b * 8) + 4] + ((OAMFront[(b * 8) + 5] & 0x3) << 8);
                    int q = N % Tilewidth;
                    N = N / Tilewidth;
                    N = N * TilewidthUNITE;
                    N += q;
                    OAMFront[(b * 8) + 4] = (byte)(N & 0xFF);
                    OAMFront[(b * 8) + 5] = (byte)((N >> 8) & 0x3);
                }
            }
            if (TilewidthUNITE != Tilewidth2)
            {
                int e = OAMBack.Count() / 8;
                for (int b = 0; b < e; b++)
                {
                    int N = OAMBack[(b * 8) + 4] + ((OAMBack[(b * 8) + 5] & 0x3) << 8);
                    int q = N % Tilewidth2;
                    N = N / Tilewidth2;
                    N = N * TilewidthUNITE;
                    N += q;
                    OAMBack[(b * 8) + 4] = (byte)(N & 0xFF);
                    OAMBack[(b * 8) + 5] = (byte)((N >> 8) & 0x3);
                }
            }
            Byte[,] Tile = new Byte[TilewidthUNITE * TileheightUNITE, 32];
            for (int i = 0; i < Tileheight; i++)
            {
                for (int k = 0; k < TilewidthUNITE; k++)
                {
                    if (k >= Tilewidth)
                    {
                        for (int j = 0; j < 32; j++)
                            Tile[(i * TilewidthUNITE) + k, j] = 0;
                    }
                    else
                        for (int j = 0; j < 32; j++)
                            Tile[(i * TilewidthUNITE) + k, j] = TileFront[(i * Tilewidth) + k, j];
                }
            }
            if (Back >= 1)
            {
                for (int i = 0; i < Tileheight2; i++)
                {
                    for (int k = 0; k < TilewidthUNITE; k++)
                    {
                        if (k >= Tilewidth2)
                        {
                            for (int j = 0; j < 32; j++)
                                Tile[((i + Tileheight) * TilewidthUNITE) + k, j] = 0;
                        }
                        else
                            for (int j = 0; j < 32; j++)
                                Tile[((i + Tileheight) * TilewidthUNITE) + k, j] = TileBack[(i * Tilewidth2) + k, j];
                    }
                }
            }
            return Tile;
        }
        public static void Import(ref Block alpha, IProgress<ProgressPercent> progress, string OutputO)
        {
            byte[] memblock = alpha.Data;
            int LastCCG = (memblock[FreeSpace.Pointers.BaseCCG]) + (memblock[FreeSpace.Pointers.BaseCCG + 1] << 8) + (memblock[FreeSpace.Pointers.BaseCCG + 2] << 16) + (memblock[FreeSpace.Pointers.BaseCCG + 3] << 24) + FreeSpace.Pointers.Base;
            int LastSOB = (memblock[FreeSpace.Pointers.BaseSOB]) + (memblock[FreeSpace.Pointers.BaseSOB + 1] << 8) + (memblock[FreeSpace.Pointers.BaseSOB + 2] << 16) + (memblock[FreeSpace.Pointers.BaseSOB + 3] << 24) + FreeSpace.Pointers.Base;
            const int EndingSOB= 0x1CFFD98;
            FreeSpace.Pointers.Removal(memblock);
            List<FinalProducts> End = new List<FinalProducts>();
            int Num = -1;
            while (Num <= 256)
            {
                Num += 1;
                for (; Num <= 256; Num++)
                {
                    string Put="";
                    string Path = OutputO+"\\BattleSprites\\";
                    if (Num < 10)
                        Put += "00";
                    else if (Num < 100)
                    {
                        Put += "0";
                        Put += ConvertHexToChar(Num / 10);
                    }
                    else
                    {
                        Put += ConvertHexToChar(Num / 100);
                        Put += ConvertHexToChar((Num / 10) % 10);
                    }
                    Put += Num % 10;
                    Path += Put;
                    progress?.Report(new ProgressPercent("Reading Enemy " + Num + "'s Graphics",
                        ((Num * 100f) / 257)));
                    if (System.IO.File.Exists(Path + ".png"))
                    {
                        int Back = 0, Enemynum = Num;
                        if (System.IO.File.Exists(Path + "Back.png"))
                            Back = 1;
                        int k = 0, u = 0;
                        bool IsHFlip=false, IsBackHFlip=false;
                        int restFront = 0, restBack = 0;
                        Bitmap img = new Bitmap(Path + ".png");
                        int Backheight = img.Height;
                        int tileheight = 0, tilewidth = 0;
                        byte[] PALette;
                        IsHFlip = CheckIfHFlipStart(img);
                        restFront = img.Width % 2;
                        if (IsHFlip)
                            img = ChangeIfHFlipStart(img);
                        if (Back == 1)
                        {
                            Bitmap img2 = new Bitmap(Path + "Back.png");
                            IsBackHFlip = CheckIfHFlipStart(img2);
                            restBack = img2.Width % 2;
                            if (IsBackHFlip)
                                img2 = ChangeIfHFlipStart(img2);
                            img = MergeTwo32ArgbFrontBack(img, img2);
                        }
                        Byte[] Image = ConvertImgTo4bpp(img, Backheight, ref tilewidth, ref tileheight, out PALette).ToArray();
                        Byte[,] Tile = new Byte[((Image.Count()) / 32), 32];
                        for (u = 0; u < ((Image.Count()) / 32); u++)
                        {
                            for (int j = 0; j < 32; j++)
                            {
                                Tile[u, j] = Image[(u * 32) + j];
                                //if(Tile[u,j]<=15)
                                //Console.Write("0"); Testing purpose.
                                //Console.Write("{0:X}|",Tile[u, j]);
                            }
                            //Console.WriteLine("");
                        }
                        byte[,] NewTile = Tile;
                        if (tileheight > 1)
                            NewTile = RemoveExceedingTiles(Tile, tilewidth, tileheight, ref Backheight, out tilewidth, out tileheight);
                        Byte[,] Tileback, OAMTile = new Byte[1, 1];
                        Byte[] OAMFront, OAMBack, SOB = new Byte[1];
                        int tilewidth1 = tilewidth;
                        int tileheight1 = 0;
                        int tilewidth2 = tilewidth;
                        int tileheight2 = 0;
                        int tileheight1temp = tileheight;
                        int tileheight2temp = tileheight;
                        int Tiletemph = tileheight;
                        int Tiletempw = tilewidth;
                        if ((Back == 1))
                        {
                            NewTile = SeparateBack(NewTile, tilewidth, tileheight, Back, Backheight, out tileheight1, out tileheight2, out Tileback);
                            NewTile = RemoveExceedingTiles(NewTile, tilewidth, tileheight1, ref Backheight, out tilewidth1, out tileheight1);
                            if (tileheight2 > 0)
                            {
                                Tileback = RemoveExceedingTiles(Tileback, tilewidth2, tileheight2, ref Backheight, out tilewidth2, out tileheight2);
                                if (!BackFront(NewTile, Tileback, tileheight1, tileheight2, tilewidth1, tilewidth2))
                                {
                                    tileheight1temp = tileheight1;
                                    tileheight2temp = tileheight2;
                                    OAMTile = TileOAM(NewTile, tileheight1, tilewidth1, out tileheight1, out tilewidth1, out OAMFront, IsHFlip);
                                    Tileback = TileOAM(Tileback, tileheight2, tilewidth2, out tileheight2, out tilewidth2, out OAMBack, IsBackHFlip);
                                    OAMTile = UniteTile(OAMTile, Tileback, Back, tilewidth1, tileheight1, tilewidth2, tileheight2, ref OAMFront, ref OAMBack, out tilewidth, out tileheight);
                                    OAMFront=OAMFixHFlip(OAMFront, IsHFlip, restFront);
                                    OAMBack = OAMFixHFlip(OAMBack, IsBackHFlip, restBack);
                                    SOB = SOBGen(OAMFront, OAMBack, tileheight1, tilewidth1, tilewidth2, Back);
                                }
                                else
                                    Back = 0;
                            }
                            else
                                Back = 0;
                        }
                        if (Back == 0)
                        {
                            OAMTile = TileOAM(NewTile, tileheight, tilewidth, out tileheight, out tilewidth, out OAMFront, IsHFlip);
                            OAMFront = OAMFixHFlip(OAMFront, IsHFlip, restFront);
                            OAMBack = new Byte[0];
                            SOB = SOBGen(OAMFront, OAMBack, tileheight, tilewidth, tilewidth2, Back);
                        }
                        OAMTile = Finalize(OAMTile, SOB, tilewidth);
                        OAMTile = checkForSame(OAMTile, SOB);
                        Image = new Byte[OAMTile.Length + 32];
                        for (u = 0; u < OAMTile.Length / 32; u++)
                        {
                            for (k = 0; k < 32; k++)
                                Image[(u * 32) + k] = OAMTile[u, k];
                        }
                        for (k = 0; k <= 31; k++)
                        {
                            Image[OAMTile.Length + k] = 255;//Prepare the image for CCG compression.
                        }
                        int Tiletemp = Image.Count() / 32;
                        Image = GBA.LZ77.Compress(Image, true);
                        byte[] CCG = new byte[Image.Count() + 16];
                        CCG[0] = 0x63;
                        CCG[1] = 0x63;
                        CCG[2] = 0x67;
                        CCG[3] = 0x20;
                        CCG[4] = 0x2;
                        CCG[5] = 0;
                        CCG[6] = 0;
                        CCG[7] = 0;
                        CCG[8] = ((byte)(Tiletemp & 0xFF));
                        CCG[9] = ((byte)((Tiletemp >> 8) & 0xFF));
                        CCG[10] = ((byte)((Tiletemp >> 16) & 0xFF));
                        CCG[11] = ((byte)((Tiletemp >> 24) & 0xFF));
                        for (int o = 0; o < Image.Count(); o++)
                            CCG[o + 12] = Image[o];
                        CCG[Image.Count() + 12] = 0x7E;
                        CCG[Image.Count() + 13] = 0x63;
                        CCG[Image.Count() + 14] = 0x63;
                        CCG[Image.Count() + 15] = 0x67;
                        int Enemytable = 0xD0D28; //If it has a back sprite, make it turnable in battle and in memo. I'll leave this commented since this should be in the EnemyData.
                                                  /*if (Back >= 1)
                                                  {
                                                      memblock[Enemytable + (Enemynum * 0x90) + 0x74] = 1;
                                                      memblock[Enemytable + (Enemynum * 0x90) + 0x75] = 1;
                                                  }
                                                  else
                                                  {
                                                      memblock[Enemytable + (Enemynum * 0x90) + 0x74] = 0;
                                                      memblock[Enemytable + (Enemynum * 0x90) + 0x75] = 0;
                                                  }*/
                        memblock[Enemytable + (Enemynum * 0x90) + 0x6C] = 1;//Fix the heights, so we avoid problems.
                                                                            //I suggest making an algorythm that edits the value at "0xC6D62 + (Enemynum * 2)" based on the number of initial breaks in Memo entries. This value will be then used by my algorythm to make everything look pretty.
                        if (memblock[0xC6D62 + (Enemynum * 2)] >= 128)
                        {
                            memblock[Enemytable + (Enemynum * 0x90) + 0x70] = (byte)(36 - ((tileheight1temp << 3) / 2) + (256 - ((256 - memblock[0xC6D62 + (Enemynum * 2)]) / 2)));
                            memblock[Enemytable + (Enemynum * 0x90) + 0x71] = (byte)(36 - ((tileheight2temp << 3) / 2) + (256 - ((256 - memblock[0xC6D62 + (Enemynum * 2)]) / 2)));
                        }
                        else
                        {
                            memblock[Enemytable + (Enemynum * 0x90) + 0x70] = (byte)(36 - ((tileheight1temp << 3) / 2) + (memblock[0xC6D62 + (Enemynum * 2)] / 2));
                            memblock[Enemytable + (Enemynum * 0x90) + 0x71] = (byte)(36 - ((tileheight2temp << 3) / 2) + (memblock[0xC6D62 + (Enemynum * 2)] / 2));
                        }
                        if (tileheight1temp >= 12)
                            memblock[Enemytable + (Enemynum * 0x90) + 0x72] = (byte)(256 - (((tileheight1temp - 12) << 3) / 2));
                        else
                            memblock[Enemytable + (Enemynum * 0x90) + 0x72] = 0;
                        if (tileheight2temp >= 12)
                            memblock[Enemytable + (Enemynum * 0x90) + 0x73] = (byte)(256 - (((tileheight2temp - 12) << 3) / 2));
                        else
                            memblock[Enemytable + (Enemynum * 0x90) + 0x73] = 0;
                        FinalProducts a = new FinalProducts();
                        a.CCG = CCG;
                        a.SOB = SOB;
                        a.Palette = PALette;
                        End.Add(a);
                    }
                    else
                    {
                        if (Num != 0)
                        {
                            FinalProducts a = new FinalProducts();
                            a.CCG = End[0].CCG;
                            a.SOB = End[0].SOB;
                            a.Palette = End[0].Palette;
                            End.Add(a);
                        }
                        else
                        {
                            FinalProducts a = new FinalProducts();
                            byte[] u = new byte[32];
                            for (int h = 0; h < 32; h++)
                                u[h] = (byte)0;
                            byte[] i = GBA.LZ77.Compress(u, true);
                            byte[] CCG = new byte[i.Length + 16];
                            CCG[0] = 0x63;
                            CCG[1] = 0x63;
                            CCG[2] = 0x67;
                            CCG[3] = 0x20;
                            CCG[4] = 0x2;
                            CCG[5] = 0;
                            CCG[6] = 0;
                            CCG[7] = 0;
                            CCG[8] = 1;
                            CCG[9] = 0;
                            CCG[10] = 0;
                            CCG[11] = 0;
                            for (int o = 0; o < i.Length; o++)
                                CCG[o + 12] = i[o];
                            CCG[i.Length + 12] = 0x7E;
                            CCG[i.Length + 13] = 0x63;
                            CCG[i.Length + 14] = 0x63;
                            CCG[i.Length + 15] = 0x67;
                            a.CCG = CCG;
                            List<byte> SOB = new List<byte>();
                            SOB.Add(0x73);
                            SOB.Add(0x6F);
                            SOB.Add(0x62);
                            SOB.Add(0x20);
                            SOB.Add(2);
                            SOB.Add(0);
                            SOB.Add(2);
                            SOB.Add(0);
                            SOB.Add(0x10);
                            SOB.Add(0);
                            SOB.Add(0x10);
                            SOB.Add(0);
                            SOB.Add((Byte)(0x14 + 8));
                            SOB.Add(0);
                            SOB.Add((Byte)(0x14 + 8));
                            SOB.Add(0);
                            SOB.Add(1);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(4);
                            SOB.Add(0);
                            SOB.Add(1);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0);
                            SOB.Add(0x7E);
                            SOB.Add(0x73);
                            SOB.Add(0x6F);
                            SOB.Add(0x62);
                            a.SOB = SOB.ToArray();
                            a.Palette = u;
                            End.Add(a);
                        }
                    }
                }
            }
            for (int Enemynum = 0; Enemynum <= 256; Enemynum++)
            {
                progress?.Report(new ProgressPercent("Inserting Enemy " + Enemynum + "'s Graphics",
                    ((Enemynum * 100f) / 257)));
                int BaseSOB = FreeSpace.Pointers.BaseSOB + (Enemynum * 8);
                int BaseCCG = FreeSpace.Pointers.BaseCCG + (Enemynum * 8);
                int BasePAL = FreeSpace.Pointers.BasePAL + (Enemynum * 8);
                End[Enemynum].PointerCCG = BaseCCG;
                End[Enemynum].PointerSOB = BaseSOB;
                End[Enemynum].PointerPAL = BasePAL;
                End[Enemynum].ToCCG = Enemynum;
                End[Enemynum].ToSOB = Enemynum;
                int flag1 = 0, flag2 = 0;
                for (int i = 0; i < Enemynum; i++)
                {
                    if ((AreArraySame(End[Enemynum].CCG, End[i].CCG)) && (flag1 == 0))
                    {
                        End[Enemynum].ToCCG = i;
                        flag1 = 1;
                    }
                    if ((AreArraySame(End[Enemynum].SOB, End[i].SOB)) && (flag2 == 0))
                    {
                        flag2 = 1;
                        End[Enemynum].ToSOB = i;
                    }
                }
            }
            for (int Enemynum = 0; Enemynum <= 256; Enemynum++)
            {
                if (End[Enemynum].ToCCG == Enemynum)
                {
                    End[Enemynum].AddressCCG = LastCCG;
                    LastCCG = InsertPointer(memblock, End[Enemynum].CCG, End[Enemynum].PointerCCG, LastCCG);
                }
                else
                {
                    End[Enemynum].AddressCCG = End[End[Enemynum].ToCCG].AddressCCG;
                    InsertOldPointer(memblock, End[Enemynum].PointerCCG, End[End[Enemynum].ToCCG].AddressCCG, End[Enemynum].CCG);
                }
            }
            for(int Enemynum=0; Enemynum<=256; Enemynum++)
            { 
                if (End[Enemynum].ToSOB == Enemynum)
                {
                    if (EndingSOB - LastSOB - End[Enemynum].SOB.Count() > 0)
                        End[Enemynum].AddressSOB = LastSOB;
                    else
                    {
                        End[Enemynum].AddressSOB = LastCCG;
                        LastSOB = LastCCG;
                    }
                    LastSOB = InsertPointer(memblock, End[Enemynum].SOB, End[Enemynum].PointerSOB, LastSOB);
                }
                else
                {
                    End[Enemynum].AddressSOB = End[End[Enemynum].ToSOB].AddressSOB;
                    InsertOldPointer(memblock, End[Enemynum].PointerSOB, End[End[Enemynum].ToSOB].AddressSOB, End[Enemynum].SOB);
                }
                int OffsetPAL = memblock[FreeSpace.Pointers.BasePAL + (Enemynum * 8)] + (memblock[FreeSpace.Pointers.BasePAL + (Enemynum * 8) + 1] << 8) + (memblock[FreeSpace.Pointers.BasePAL + (Enemynum * 8) + 2] << 16) + (memblock[FreeSpace.Pointers.BasePAL + (Enemynum * 8) + 3] << 24) + FreeSpace.Pointers.Base;//Palette
                for (int i = 0; i < 32; i++)
                    memblock[OffsetPAL + i] = (byte)(End[Enemynum].Palette[i] & 0xFF);
            }
            alpha.CopyFrom(memblock);
        }
    }
}