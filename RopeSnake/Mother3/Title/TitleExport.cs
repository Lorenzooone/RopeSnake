using RopeSnake.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RopeSnake.Mother3.Title
{
    //Maximum Load of frames set at 0x8057232
    // Maximum Load of Logo is set at 0x805709C
    class CreateLogos
    {
        unsafe static Bitmap MergeTwo8bpp(Bitmap firstImage, Bitmap secondImage, int xsecond, int ysecond)
        {
            if (firstImage == null)
            {
                throw new ArgumentNullException("firstImage");
            }
            if (secondImage == null)
            {
                throw new ArgumentNullException("secondImage");
            }
            Bitmap outputImage = firstImage;
            BitmapData N = LockBits(outputImage, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            BitmapData N2 = LockBits(secondImage, ImageLockMode.ReadOnly);
            byte* ptr2 = (byte*)N2.Scan0;
            for (int i = 0; i < N2.Height; i++)
            {
                for (int y = 0; y < N2.Width; y++)
                {
                    if (ptr2[y + (i * N2.Stride)] != 0)
                        ptr[y + (i * N.Stride) + xsecond + (ysecond * N.Stride)] = ptr2[y + (i * N2.Stride)];
                }
            }
            outputImage.UnlockBits(N);
            secondImage.UnlockBits(N2);
            return outputImage;
        }
        public static BitmapData LockBits(Bitmap bmp, ImageLockMode imageLockMode)
        {
            return bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), imageLockMode, bmp.PixelFormat);
        }
        public static unsafe Bitmap Create4bpp(ColorPalette PAL, int[] Arrangement, byte[] Pixels)
        {
            Bitmap a = new Bitmap(256, Arrangement.Length / (4), PixelFormat.Format8bppIndexed);
            a.Palette = PAL;
            BitmapData N = LockBits(a, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            for (int i = 0; i < a.Height / 8; i++)
            {
                for (int o = 0; o < 32; o++)
                {
                    for (int u = 0; u < 8; u++)
                        for (int j = 0; j < 4; j++)
                        {
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2)] = (byte)((Pixels[((Arrangement[(i * 32) + (o)] & 0xFFF) * 32) + j + (u * 4)] & 0xF) + (((Arrangement[(i * 32) + (o)] >> 12) & 0xF) * 0x10));
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2) + 1] = (byte)(((Pixels[((Arrangement[(i * 32) + (o)] & 0xFFF) * 32) + j + (u * 4)] >> 4) & 0xF) + (((Arrangement[(i * 32) + (o)] >> 12) & 0xF) * 0x10));
                        }
                }
            }
            a.UnlockBits(N);
            return a;
        }//Logo & Health one
        public static unsafe Bitmap Create4bpp(ColorPalette PAL, byte[] Pixels)
        {
            Bitmap a = new Bitmap(256, 8, PixelFormat.Format8bppIndexed);
            a.Palette = PAL;
            BitmapData N = LockBits(a, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            for (int i = 0; i < a.Height / 8; i++)
            {
                for (int o = 0; o < 32; o++)
                {
                    for (int u = 0; u < 8; u++)
                        for (int j = 0; j < 4; j++)
                        {
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2)] = (byte)((Pixels[j + (u * 4)+(o*32)] & 0xF));
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2) + 1] = (byte)(((Pixels[j + (u * 4)+(o*32)] >> 4) & 0xF));
                        }
                }
            }
            a.UnlockBits(N);
            return a;
        }//Numbers one
        public static unsafe Bitmap Create4bpp(ColorPalette PAL, OAM OAM, byte[] Pixels)
        {
            Bitmap a = new Bitmap(OAM.SizeX, OAM.SizeY, PixelFormat.Format8bppIndexed);
            a.Palette = PAL;
            BitmapData N = LockBits(a, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            for (int i = 0; i < a.Height / 8; i++)
            {
                for (int o = 0; o < a.Width / 8; o++)
                {
                    for (int u = 0; u < 8; u++)
                        for (int j = 0; j < 4; j++)
                        {
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2)] = (byte)((Pixels[((OAM.Tile+o+(i*(a.Width/8))) * 32) + j + (u * 4)] & 0xF) + (((OAM.Palette) * 0x10)));
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j * 2) + 1] = (byte)(((Pixels[((OAM.Tile+o + (i * (a.Width / 8))) * 32) + j + (u * 4)] >> 4) & 0xF) + (((OAM.Palette) * 0x10)));
                        }
                }
            }
            a.UnlockBits(N);
            if (OAM.Flip == 1)
            {
                a.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
            else if (OAM.Flip == 2)
            {
                a.RotateFlip(RotateFlipType.RotateNoneFlipY);
            }
            else if (OAM.Flip == 3)
            {
                a.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            }
            return a;
        }//Menu options & Press Continue one
        public static unsafe Bitmap Create8bpp(ColorPalette PAL, int[] Arrangement, byte[] Pixels, int Height)
        {
            Bitmap a = new Bitmap(256, Height, PixelFormat.Format8bppIndexed);
            a.Palette = PAL;
            BitmapData N = LockBits(a, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            for (int i = 0; i < a.Height / 8; i++)
            {
                for (int o = 0; o < 32; o++)
                {
                    for (int u = 0; u < 8; u++)
                        for (int j = 0; j < 8; j++)
                        {
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j)] = (byte)((Pixels[((Arrangement[(i * 32) + (o)] & 0xFFF) * 64) + j + (u * 8)] & 0xFF) + (((Arrangement[(i * 32) + (o)] >> 12) & 0xF) * 0x10));
                        }
                }
            }
            a.UnlockBits(N);
            return a;
        }//Title Screen and GBAPlayer one
        public static unsafe Bitmap Create8bpp(ColorPalette PAL, byte[] Pixels)
        {
            Bitmap a = new Bitmap(240, Pixels.Length/240, PixelFormat.Format8bppIndexed);
        a.Palette = PAL;
            BitmapData N = LockBits(a, ImageLockMode.WriteOnly);
        byte* ptr = (byte*)N.Scan0;
            for (int i = 0; i<a.Height / 8; i++)
            {
                for (int o = 0; o< 30; o++)
                {
                    for (int u = 0; u< 8; u++)
                        for (int j = 0; j< 8; j++)
                        {
                            ptr[((i * N.Stride * 8)) + (o * 8) + (u * N.Stride) + (j)] = (byte)((Pixels[((o+(i*30)) * 64) + j + (u * 8)] & 0xFF));
                        }
                }
            }
            a.UnlockBits(N);
            return a;
        }//Disclaimer one
        public static unsafe Bitmap PalRender(ColorPalette PAL)
        {
            Bitmap Temporale = new Bitmap(8, 64, PixelFormat.Format8bppIndexed);
            Temporale.Palette = PAL;
            BitmapData N = LockBits(Temporale, ImageLockMode.WriteOnly);
            byte* ptr = (byte*)N.Scan0;
            for(int i=0; i<16; i++)
            {
                for(int o=0; o<2; o++)
                {
                    ptr[(o * 4) + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 1 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 2 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 3 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 8 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 9 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 10 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 11 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 16 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 17 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 18 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 19 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 24 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 25 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 26 + (i * 32)] = (byte)((16 * o) + i);
                    ptr[(o * 4) + 27 + (i * 32)] = (byte)((16 * o) + i);
                }
            }
            Temporale.UnlockBits(N);
            return Temporale;
        }//Palette number 2 for options
        public static Bitmap CreateRender(OAMList OAMEntry, ColorPalette Palette, byte[] Image)
        {
            List<Bitmap> Tempo = new List<Bitmap>();
            List<int> X = new List<int>();
            List<int> Y = new List<int>();
            for (int i = 0; i < OAMEntry.OAMs.Count; i++)
            {
                    Tempo.Add(Create4bpp(Palette, OAMEntry.OAMs[i], Image));
                    X.Add(OAMEntry.OAMs[i].X);
                    Y.Add(OAMEntry.OAMs[i].Y);
            }
            int minY, minX;
            if (Y.Count > 0)
                minY = Y.Min();
            else
                minY = 0;
            if (X.Count > 0)
                minX = X.Min();
            else
                minX = 0;
            for (int i = 0; i < X.Count; i++)
            {
                X[i] -= minX;
                Y[i] -= minY;
            }
            int MaxX = 0;
            int MaxY = 0;
            for (int i = 0; i < X.Count; i++)
            {
                if (MaxX < X[i] + Tempo[i].Width)
                    MaxX = Tempo[i].Width + X[i];
                if (MaxY < Y[i] + Tempo[i].Height)
                    MaxY = Tempo[i].Height + Y[i];
            }
            if ((MaxX == 0) || (MaxY == 0))
            {
                MaxX = 8; MaxY = 8;
            }
            Bitmap Temporale = new Bitmap(MaxX, MaxY, PixelFormat.Format8bppIndexed);
            Temporale.Palette = Palette;
            for (int i = 0; i < X.Count; i++)
                Temporale = MergeTwo8bpp(Temporale, Tempo[i], X[i], Y[i]);
            return Temporale;
        }
    }
    class XY
    {
        public int Option_Number;
        public string Name;
        public int X_Coord;
        public int Y_Coord;
    }
    class OAMTotal
    {
        public static List<XY> d = new List<XY>();
        public static List<OAMList> OAMLists;
        public static List<List<byte>> OAMRemains_Title;
        public static List<List<byte>> OAMRemains_Health;
        public List<List<byte>> OAMGet (Block RomData, int offset, string Name, bool doEnd)
        {
            List<List<byte>> totalRemainingData = new List<List<byte>>();
            OAMLists = new List<OAMList>();
            int num = RomData[offset + 2] + offset + (RomData[offset + 3] << 8);
            int cont = 4;
            int i;
            for (i = 0; i < (RomData[num] + (RomData[num + 1] << 8)); i++)
            {
                int Y = 0x100;
                int X = 0x200;
                int TotSingle = RomData[num + cont] + (RomData[num + cont + 1] << 8);
                if (TotSingle == 0)
                    break;
                OAMList a = new OAMList();
                for (int u = 0; u < TotSingle; u++)
                {
                    OAM b = new OAM();
                    b.Y = RomData[num + (u * 6) + cont + 2];
                    b.X = (RomData[num + (u * 6) + cont + 4] + ((RomData[num + (u * 6) + cont + 5] & 0x1) << 8));
                    if (b.Y >= 0x80)
                        b.Y -= 0x80;
                    else
                        b.Y += 0x80;
                    if (b.X >= 0x100)
                        b.X -= 0x100;
                    else
                        b.X += 0x100;
                    b.Flip = (byte)((RomData[num + (u * 6) + cont + 5] >> 4) & 0x3);
                    b.SizeX = OAM.SetSizeX((byte)(RomData[num + cont + (u * 6) + 5] >> 6), (byte)(RomData[num + (u * 6) + cont + 3] >> 6));
                    b.SizeY = OAM.SetSizeY((byte)(RomData[num + (u * 6) + cont + 5] >> 6), (byte)(RomData[num + (u * 6) + cont + 3] >> 6));
                    b.Tile = (RomData[num + (u * 6) + cont + 6] + ((RomData[num + cont + (u * 6) + 7] & 0x3) << 8));
                    b.Palette = (byte)((RomData[num + (u * 6) + cont + 7] >> 4));
                    a.OAMs.Add(b);

                    X = Math.Min(X, b.X);
                    Y = Math.Min(Y, b.Y);
                }
                OAMLists.Add(a);
                cont += (TotSingle * 6) + 4;
                XY c = new XY
                {
                    Option_Number = d.Count,
                    X_Coord = X,
                    Y_Coord = Y,
                    Name = Name + i
                };
                d.Add(c);
            }
            if (doEnd)
            {
                List<byte> remainingData = new List<byte>();
                if (num + cont < RomData[offset + 4] + offset + (RomData[offset + 5] << 8))
                {
                    for (i = 0; i + num + cont < RomData[offset + 4] + offset + (RomData[offset + 5] << 8); i++)
                        remainingData.Add(RomData[i + num + cont]);
                    totalRemainingData.Add(remainingData);
                }
                remainingData = new List<byte>();
                num = RomData[offset + 4] + offset + (RomData[offset + 5] << 8);
                int count = RomData[num] + (RomData[num + 1] << 8);
                remainingData.Add((byte)(count & 0xFF));
                remainingData.Add((byte)((count >> 8) & 0xFF));
                cont = 2;
                for (i = 0; i < count; i++)
                {
                    for (int j = 0; j < 4; j++)
                        remainingData.Add(RomData[num + (cont++)]);
                    int inner_Count = RomData[num + cont] + (RomData[num + cont + 1] << 8);
                    for (int j = 0; j < (2 * (inner_Count + 1)); j++)
                        remainingData.Add(RomData[num + (cont++)]);
                }
                totalRemainingData.Add(remainingData);
            }
            return totalRemainingData;
        }
        public  List<XY> Extract()
        {
            return d;
        }
        public OAMList Extract(int num)
        {
            return OAMLists[num];
        }
        public int Total()
        {
            return OAMLists.Count;
        }
    }
    class OAMList
    {
        public List<OAM> OAMs=new List<OAM>();
    }
    class OAM
    {
        public int X { get; set; }
        public byte Y { get; set; }
        public byte Flip { get; set; }
        public byte SizeX { get; set; }
        public byte SizeY { get; set; }
        public int Tile { get; set; }
        public byte Palette { get; set; }
        public static byte SetSizeY(byte Size, byte Shape)
        {
            if (Shape == 0)
            {
                if (Size == 0)
                    return 8;
                else if (Size == 1)
                    return 16;
                else if (Size == 2)
                    return 32;
                else if (Size == 3)
                    return 64;
                else
                    return 0;
            }
            else if (Shape == 2)
            {
                if (Size == 0)
                    return 16;
                else if (Size == 1)
                    return 32;
                else if (Size == 2)
                    return 32;
                else if (Size == 3)
                    return 64;
                else
                    return 0;
            }
            else if (Shape == 1)
            {
                if (Size == 0)
                    return 8;
                else if (Size == 1)
                    return 8;
                else if (Size == 2)
                    return 16;
                else if (Size == 3)
                    return 32;
                else
                    return 0;
            }
            else
                return 0;
        }
        public static byte SetSizeX(byte Size, byte Shape)
        {
            if (Shape == 0)
            {
                if (Size == 0)
                    return 8;
                else if (Size == 1)
                    return 16;
                else if (Size == 2)
                    return 32;
                else if (Size == 3)
                    return 64;
                else
                    return 0;
            }
            else if (Shape == 1)
            {
                if (Size == 0)
                    return 16;
                else if (Size == 1)
                    return 32;
                else if (Size == 2)
                    return 32;
                else if (Size == 3)
                    return 64;
                else
                    return 0;
            }
            else if (Shape == 2)
            {
                if (Size == 0)
                    return 8;
                else if (Size == 1)
                    return 8;
                else if (Size == 2)
                    return 16;
                else if (Size == 3)
                    return 32;
                else
                    return 0;
            }
            else
                return 0;
        }
    }
    class Palette{
        public static ColorPalette PAL;
        public ColorPalette GetPalette(Block Romdata, int Pointer, int MaxPAL)
        {
            PAL= new Bitmap(1, 1, PixelFormat.Format8bppIndexed).Palette;
            for (int i=0; i<MaxPAL; i++)
            {
                for (int o = 0; o < 0x10; o++) {
                    int a = Romdata.Data[Pointer + (o * 2) + (i * 0x20)] + (Romdata.Data[Pointer + (o * 2) + 1 + (i * 0x20)] << 8);
                    byte R = (byte)((a & 31) << 3);
                    byte G = (byte)(((a>>5) & 31) << 3);
                    byte B = (byte)(((a>>10) & 31) << 3);
                    //if((i!=0)&&(o!=0))
                    PAL.Entries[(i * 0x10) + o] = Color.FromArgb(255, R, G, B);
                    //else
                    //PAL.Entries[(i * 0x10) + o] = Color.FromArgb(0, R, G, B);
                }
            }
            return PAL;
        }
        public ColorPalette ExtractPalette()
        {
            return PAL;
        }
    }
    class ArrangementList
    {
        public static List<int[]> Arrangement;
        public int[] ExtractArrangement(int Num)
        {
            return Arrangement[Num];
        }
        public List<int[]> GetArrangements(Block RomData, List<int> Pointers)
        {
            Arrangement = new List<int[]>();
            for (int i = 0; i < 4; i++)
            {
                int[] a = new int[0x400];
                for (int o = 0; o < 0x800; o += 2)
                {
                    a[o / 2] = (RomData.Data[Pointers[2 + i] + o] + (RomData.Data[Pointers[2 + i] + 1 + o] << 8));
                }
                Arrangement.Add(a);
            }
            return Arrangement;
        }
        public int Maximum()
        {
            int a = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int o = 0; o < 0x400; o++)
                    if (a < (Arrangement[i][o] & 0xFFF))
                        a = (Arrangement[i][o] & 0xFFF);
            }
            return a;
        }
        public int MaximumPAL()
        {
            int a = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int o = 0; o < 0x400; o++)
                    if (a < ((Arrangement[i][o] >> 12) & 0xF))
                        a = ((Arrangement[i][o] >> 12) & 0xF);
            }
            return a;
        }
    }
    class ArrangementOnly
    {
        public static int[] Arrangement;
        public int[] Extract()
        {
            return Arrangement;
        }
        public int[] GetArrangements(Block RomData, int Pointer)
        {
            int[] a = new int[0x400];
            for (int o = 0; o < 0x800; o += 2)
            {
                a[o / 2] = (RomData.Data[Pointer + o] + (RomData.Data[Pointer + 1 + o] << 8));
            }
            Arrangement = a;
            return Arrangement;
        }
        public int[] GetArrangementsGBAPlayer(Block RomData, int Pointer)
        {
            int[] a = new int[0x500];
            for (int o = 0; o < 0x500; o++)
            {
                a[o] = (RomData.Data[Pointer + o]);
            }
            Arrangement = a;
            return Arrangement;
        }
        public int[] GetArrangementsCompressed(Block RomData, int Pointer)
        {
            byte[] b;
            GBA.LZ77.Decompress(RomData.Data, Pointer, out b);
            int[] a = new int[b.Length/2];
            for (int o = 0; o < b.Length; o += 2)
            {
                a[o / 2] = (b[o] + (b[1 + o] << 8));
            }
            Arrangement = a;
            return Arrangement;
        }
        public int Maximum()
        {
            int a = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int o = 0; o < 0x400; o++)
                    if (a < (Arrangement[o] & 0xFFF))
                        a = (Arrangement[o] & 0xFFF);
            }
            return a;
        }
        public int MaximumPAL()
        {
            int a = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int o = 0; o < 0x400; o++)
                    if (a < ((Arrangement[o] >> 12) & 0xF))
                        a = ((Arrangement[o] >> 12) & 0xF);
            }
            return a;
        }
    }
    class TitleExport
    {
        static int GetFrom4(Block RomData, int offset)
        {
           return  (RomData.Data[offset] + (RomData.Data[offset + 1] << 8) + (RomData.Data[offset + 2] << 16) + (RomData.Data[offset + 3] << 24));
        }
        public static List<Bitmap> Begin(Block RomData, int offset)
        {
            int TotalEntries = GetFrom4(RomData, offset);
            List<Bitmap> Returnable=Logos(RomData, offset + 4);
            Returnable.Add(TitleStatic(RomData, offset + 4));
            for (int i = 0; i < 21; i++)
                Returnable.Add(TitleFrames(RomData, offset + 4, i));
            Returnable.AddRange(Menu(RomData, offset+4));
            Returnable.Add(Numbers(RomData, offset + 4));
            return Returnable;
        }
        public static Bitmap Disclaimer(Block RomData, int offset, int offsetPAL)
        {
            byte[] Graphic;
            GBA.LZ77.Decompress(RomData.Data, offset, out Graphic);
            Palette PAL = new Palette();
            PAL.GetPalette(RomData, offsetPAL, 0x10);
            return CreateLogos.Create8bpp(PAL.ExtractPalette(), Graphic);
        }
        public static List<Bitmap> Health(Block RomData, int offset)
        {
            List<Bitmap> Graphics = new List<Bitmap>();
            List<int> Pointers = new List<int>();
            for (int i = 0; i < 5; i++)
                Pointers.Add(GetFrom4(RomData, offset + (i + 1) * 4) + offset);
            byte[] Graphic;
            GBA.LZ77.Decompress(RomData.Data, Pointers[0], out Graphic);
            Palette PAL = new Palette();
            PAL.GetPalette(RomData, Pointers[1], 1);
            ArrangementOnly Arrangement = new ArrangementOnly();
            Arrangement.GetArrangementsCompressed(RomData, Pointers[2]);
            Graphics.Add(CreateLogos.Create4bpp(PAL.ExtractPalette(), Arrangement.Extract(), Graphic));
            OAMTotal OAM = new OAMTotal();
            GBA.LZ77.Decompress(RomData.Data, Pointers[3], out Graphic);
            OAMTotal.OAMRemains_Health = OAM.OAMGet(RomData, Pointers[4], "Health_", true);
            for (int i = 0; i < OAM.Total(); i++)
                Graphics.Add(CreateLogos.CreateRender(OAM.Extract(i), PAL.ExtractPalette(), Graphic));
            return Graphics;
        }
        public static Bitmap GBAPlayerLogo(Block RomData, int offsetPAL, int offsetGRA, int offsetARR)
        {
            byte[] Graphic = new Byte[0x4000];
            for (int i = 0; i < 0x4000; i++)
                Graphic[i] = RomData.Data[offsetGRA + i];
            Palette PAL = new Palette();
            PAL.GetPalette(RomData, offsetPAL, 16);
            ArrangementOnly Arrangement = new ArrangementOnly();
            Arrangement.GetArrangements(RomData, offsetARR);
            return CreateLogos.Create8bpp(PAL.ExtractPalette(), Arrangement.Extract(), Graphic, 160);
        }
        public static List<Bitmap> Menu(Block RomData, int offset)
        {
            List<Bitmap> MenuGraph = new List<Bitmap>();
            List<int> Pointers = new List<int>();
            Pointers.Add(GetFrom4(RomData, offset + (0x34 * 4)) + (offset - 4)); //Graphics
            Pointers.Add(GetFrom4(RomData, offset + (0x35 * 4)) + (offset - 4)); //Palettes
            Pointers.Add(GetFrom4(RomData, offset + (0x36 * 4)) + (offset - 4)); //OAM
            byte[] Graphics;
            GBA.LZ77.Decompress(RomData.Data, Pointers[0], out Graphics);
            Palette PAL=new Palette();
            PAL.GetPalette(RomData, Pointers[1], 3);
            OAMTotal OAM = new OAMTotal();
            OAMTotal.OAMRemains_Title = OAM.OAMGet(RomData, Pointers[2], "Menu_", true);
            for(int i=0; i<OAM.Total(); i++)
                MenuGraph.Add(CreateLogos.CreateRender(OAM.Extract(i), PAL.ExtractPalette(), Graphics));
            MenuGraph.Add(CreateLogos.PalRender(PAL.ExtractPalette()));
            return MenuGraph;
        }
        public static Bitmap TitleFrames(Block RomData, int offset, int num)
        {
            List<int> Pointers = new List<int>();
            Pointers.Add(GetFrom4(RomData, offset + ((num + 9) * 4)) + (offset - 4)); //Graphics
            Pointers.Add(GetFrom4(RomData, offset + ((num + 0x1E) * 4)) + (offset - 4)); //Arrangement
            Pointers.Add(GetFrom4(RomData, offset + (0x33 * 4)) + (offset - 4)); //Palette
            byte[] Graphics= new Byte[0x6000];
            for (int i = 0; i < 0x6000; i++)
                Graphics[i] = RomData.Data[Pointers[0] + i];
            Palette PAL = new Palette();
            PAL.GetPalette(RomData, Pointers[2], 0x10);
            ArrangementOnly Arrangement = new ArrangementOnly();
            Arrangement.GetArrangements(RomData, Pointers[1]);
            return CreateLogos.Create8bpp(PAL.ExtractPalette(), Arrangement.Extract(), Graphics, Arrangement.Extract().Length/4);
        }
        public static Bitmap Numbers(Block RomData, int offset)
        {
            List<int> Pointers = new List<int>();
            Pointers.Add(GetFrom4(RomData, offset + (0x37 * 4)) + (offset - 4));
            Pointers.Add(GetFrom4(RomData, offset + (0x38 * 4)) + (offset - 4));
            byte[] Graphics=new byte[0x400];
            for (int i = 0; i < 0x400; i++)
                Graphics[i] = RomData.Data[Pointers[0] + i];
            Palette PAL = new Palette();
            PAL.GetPalette(RomData, Pointers[1], 0x1);
            return CreateLogos.Create4bpp(PAL.ExtractPalette(), Graphics);
        }
        public static Bitmap TitleStatic(Block RomData, int offset)
        {
            List<int> Pointers = new List<int>();
            for (int i = 6; i < 9; i++)
                Pointers.Add(GetFrom4(RomData, offset + (i * 4))+(offset-4));
            byte[] Graphics;
            GBA.LZ77.Decompress(RomData.Data, Pointers[0], out Graphics);
            Palette PAL=new Palette();
            PAL.GetPalette(RomData, Pointers[1], 0x10);
            ArrangementOnly Arrangement= new ArrangementOnly();
            Arrangement.GetArrangements(RomData, Pointers[2]);
            return CreateLogos.Create8bpp(PAL.ExtractPalette(), Arrangement.Extract(), Graphics, Arrangement.Extract().Length / 4);
        }
        public static int Frames(Block RomData, int offset)
        {
            if ((RomData.Data[offset + 3] == 0xD1))
                return 8;
            else
                return RomData.Data[offset];
        }
        public static List<Bitmap> Logos(Block RomData, int offset)
        {
            List<int> Pointers = new List<int>();
            for (int i = 0; i < 6; i++)
                Pointers.Add(GetFrom4(RomData, offset + (i * 4))+(offset-4));
            ArrangementList Arrangements = new ArrangementList();
            Arrangements.GetArrangements(RomData, Pointers);
            int Max = Arrangements.Maximum();
            Max = (Max + 1) * 32;
            int MaxPAL = Arrangements.MaximumPAL();
            Palette PAL= new Palette();
            PAL.GetPalette(RomData, Pointers[1], Math.Max(6,MaxPAL));
            byte[] GraphicsLogo= new byte[Max];
            for (int i=0; i<Max; i++)
            {
                GraphicsLogo[i] = RomData.Data[Pointers[0] + i];
            }
            List<Bitmap> Logo = new List<Bitmap>();
            for (int i = 0; i < 4; i++)
                Logo.Add(CreateLogos.Create4bpp(PAL.ExtractPalette(), Arrangements.ExtractArrangement(i), GraphicsLogo));
            return Logo;
        }
    }
}
