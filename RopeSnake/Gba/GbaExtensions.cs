using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RopeSnake.Core;
using RopeSnake.Graphics;

namespace RopeSnake.Gba
{
    public static class GbaExtensions
    {
        private static readonly int CompressionBufferSize = 128 * 1024;
        private static readonly ThreadLocal<Block> CompressionBuffers;

        static GbaExtensions()
        {
            CompressionBuffers = new ThreadLocal<Block>(() => new Block(CompressionBufferSize));
        }

        public static int ReadGbaPointer(this BinaryStream stream)
        {
            return stream.ReadInt() & 0x1FFFFFF;
        }

        public static void WriteGbaPointer(this BinaryStream stream, int pointer)
        {
            stream.WriteInt(pointer | 0x8000000);
        }

        public static Block ReadCompressed(this BinaryStream stream)
        {
            int start = stream.Position;

            // Check for LZ77 signature
            if (stream.ReadByte() != 0x10)
                throw new Exception($"Expected LZ77 header at position 0x{start:X}");

            // Read the block length
            int length = stream.ReadByte();
            length += (stream.ReadByte() << 8);
            length += (stream.ReadByte() << 16);
            Block decompressed = new Block(length);

            int bPos = 0;
            while (bPos < length)
            {
                byte ch = stream.ReadByte();
                for (int i = 0; i < 8; i++)
                {
                    switch ((ch >> (7 - i)) & 1)
                    {
                        case 0:

                            // Direct copy
                            if (bPos >= length) break;
                            decompressed[bPos++] = stream.ReadByte();
                            break;

                        case 1:

                            // Compression magic
                            int t = (stream.ReadByte() << 8);
                            t += stream.ReadByte();
                            int n = ((t >> 12) & 0xF) + 3;    // Number of bytes to copy

                            int o = (t & 0xFFF);

                            // Copy n bytes from bPos-o to the output
                            for (int j = 0; j < n; j++)
                            {
                                if (bPos >= length) break;
                                decompressed[bPos] = decompressed[bPos - o - 1];
                                bPos++;
                            }

                            break;

                        default:
                            break;
                    }
                }
            }

            return decompressed;
        }

        public static int WriteCompressed(this BinaryStream stream, Block uncompressed, int count, bool vram)
        {
            LinkedList<int>[] lookup = new LinkedList<int>[256];
            for (int i = 0; i < 256; i++)
                lookup[i] = new LinkedList<int>();

            int start = stream.Position;
            int current = 0;

            List<byte> temp = new List<byte>();
            int control = 0;

            // Encode the signature and the length
            stream.WriteByte(0x10);
            stream.WriteByte((byte)(count & 0xFF));
            stream.WriteByte((byte)((count >> 8) & 0xFF));
            stream.WriteByte((byte)((count >> 16) & 0xFF));

            // VRAM bug: you can't reference the previous byte
            int distanceStart = vram ? 2 : 1;

            while (current < count)
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
                    else if (current >= count)
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

                            int farthest = Math.Min(18, count - current + start);
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
                stream.WriteByte((byte)(control & 0xFF));

                for (int i = 0; i < temp.Count; i++)
                    stream.WriteByte(temp[i]);
            }

            return stream.Position - start;
        }

        public static T ReadCompressed<T>(this BinaryStream stream, Func<BinaryStream, int, T> readAction)
        {
            var decompressed = stream.ReadCompressed();
            return readAction(decompressed.ToBinaryStream(), decompressed.Size);
        }

        public static int WriteCompressed(this BinaryStream stream, bool vram, Action<BinaryStream> writeAction)
        {
            var buffer = CompressionBuffers.Value;
            var bufferStream = buffer.ToBinaryStream();
            writeAction(bufferStream);
            return stream.WriteCompressed(buffer, bufferStream.Position, vram);
        }

        public static GbaTile ReadTile(this BinaryStream stream, int bitDepth)
        {
            var tile = new GbaTile();
            tile.Read(stream, bitDepth);
            return tile;
        }

        public static void WriteTile(this BinaryStream stream, GbaTile tile, int bitDepth)
        {
            tile.Write(stream, bitDepth);
        }

        public static GbaTileset ReadTileset(this BinaryStream stream, int count, int bitDepth)
        {
            var tileset = new GbaTileset(count, bitDepth);
            for (int i = 0; i < count; i++)
            {
                tileset[i] = stream.ReadTile(bitDepth);
            }
            return tileset;
        }

        public static GbaTileset ReadCompressedTileset(this BinaryStream stream, int bitDepth)
            => stream.ReadCompressed((s, l) => s.ReadTileset(l / (bitDepth * 8), bitDepth));

        public static void WriteTileset(this BinaryStream stream, GbaTileset tileset, int bitDepth)
        {
            for (int i = 0; i < tileset.Count; i++)
            {
                stream.WriteTile(tileset[i], bitDepth);
            }
        }

        public static int WriteCompressedTileset(this BinaryStream stream, GbaTileset tileset, bool vram)
            => stream.WriteCompressed(vram, s => s.WriteTileset(tileset, tileset.BitDepth));

        public static Color ReadColor(this BinaryStream stream)
        {
            ushort value = stream.ReadUShort();
            byte r = (byte)((value & 0x1F) * 8);
            byte g = (byte)(((value >> 5) & 0x1F) * 8);
            byte b = (byte)(((value >> 10) & 0x1F) * 8);
            return new Color(r, g, b);
        }

        public static void WriteColor(this BinaryStream stream, Color color)
        {
            ushort value = (ushort)(
                ((color.R / 8) & 0x1F) |
                (((color.G / 8) & 0x1F) << 5) |
                (((color.B / 8) & 0x1F) << 10));
            stream.WriteUShort(value);
        }

        public static Palette ReadPalette(this BinaryStream stream, int subPaletteCount, int colorsPerSubPalette)
        {
            var palette = new Palette(subPaletteCount, colorsPerSubPalette);
            for (int subPalette = 0; subPalette < subPaletteCount; subPalette++)
            {
                for (int color = 0; color < colorsPerSubPalette; color++)
                {
                    palette[subPalette, color] = stream.ReadColor();
                }
            }
            return palette;
        }

        public static void WritePalette(this BinaryStream stream, Palette palette)
        {
            for (int subPalette = 0; subPalette < palette.SubPaletteCount; subPalette++)
            {
                for (int color = 0; color < palette.ColorsPerSubPalette; color++)
                {
                    stream.WriteColor(palette[subPalette, color]);
                }
            }
        }

        public static TileInfo ReadTileInfo(this BinaryStream stream)
        {
            var tileInfo = new TileInfo();
            ushort value = stream.ReadUShort();
            tileInfo.TileIndex = value & 0x3FF;
            tileInfo.FlipX = (value & 0x400) != 0;
            tileInfo.FlipY = (value & 0x800) != 0;
            tileInfo.PaletteIndex = (value >> 12) & 0xF;
            return tileInfo;
        }

        public static void WriteTileInfo(this BinaryStream stream, TileInfo tileInfo)
        {
            ushort value = (ushort)(
                (tileInfo.TileIndex & 0x3FF) |
                (tileInfo.FlipX ? 0x400 : 0) |
                (tileInfo.FlipY ? 0x800 : 0) |
                ((tileInfo.PaletteIndex & 0xF) << 12));
            stream.WriteUShort(value);
        }

        public static GbaTilemap ReadTilemap(this BinaryStream stream, int width, int height)
        {
            var tilemap = new GbaTilemap(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tilemap[x, y] = stream.ReadTileInfo();
                }
            }
            return tilemap;
        }

        public static void WriteTilemap(this BinaryStream stream, GbaTilemap tilemap)
        {
            for (int y = 0; y < tilemap.Height; y++)
            {
                for (int x = 0; x < tilemap.Width; x++)
                {
                    stream.WriteTileInfo(tilemap[x, y]);
                }
            }
        }
    }
}
