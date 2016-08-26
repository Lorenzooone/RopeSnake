﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpFileSystem;

namespace RopeSnake.Core
{
    public class BinaryFileManager
    {
        private IFileSystem _fileSystem;

        protected IFileSystem Manager { get { return _fileSystem; } }

        public BinaryFileManager(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T ReadFile<T>(FileSystemPath path)
            where T : IBinarySerializable, new()
        {
            var value = new T();
            ReadFile(path, value);
            return value;
        }

        public void ReadFile(FileSystemPath path, IBinarySerializable value)
        {
            if (!_fileSystem.Exists(path))
                throw new FileNotFoundException("File not found", path.Path);

            using (var stream = _fileSystem.OpenFile(path, FileAccess.Read))
            {
                value.Deserialize(stream, (int)stream.Length);
            }
        }

        public void WriteFile(FileSystemPath path, IBinarySerializable value)
        {
            using (var stream = _fileSystem.CreateFile(path))
            {
                value.Serialize(stream);
            }
        }
    }
}
