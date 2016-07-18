﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RopeSnake.Core;

namespace RopeSnake.Graphics
{
    public abstract class Tile
    {
        private byte[,] _pixels;

        public virtual byte this[int x, int y]
        {
            get { return _pixels[x, y]; }
            set { _pixels[x, y] = value; }
        }

        public virtual int Width => _pixels.GetLength(0);
        public virtual int Height => _pixels.GetLength(1);

        protected Tile(int width, int height)
        {
            _pixels = new byte[width, height];
        }

        protected void ResetTile(int newWidth, int newHeight)
        {
            _pixels = new byte[newWidth, newHeight];
        }

        #region IBinarySerializable implementation

        public virtual void Serialize(Stream stream)
        {
            var writer = new BinaryStream(stream);

            writer.WriteInt(Width);
            writer.WriteInt(Height);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    writer.WriteByte(_pixels[x, y]);
                }
            }
        }

        public virtual void Deserialize(Stream stream, int fileSize)
        {
            var reader = new BinaryStream(stream);

            int width = reader.ReadInt();
            int height = reader.ReadInt();

            ResetTile(width, height);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _pixels[x, y] = reader.ReadByte();
                }
            }
        }

        #endregion
    }
}