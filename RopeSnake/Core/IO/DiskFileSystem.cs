﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RopeSnake.Core
{
    public class DiskFileSystem : IFileSystem
    {
        public string BasePath { get; set; } = "";

        protected virtual string GetFullPath(string path)
        {
            return Path.Combine(BasePath, path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(GetFullPath(path));
        }

        public Stream CreateFile(string path)
        {
            var fileInfo = new FileInfo(GetFullPath(path));
            fileInfo.Directory.Create();
            return File.Create(fileInfo.FullName);
        }

        public Stream OpenFile(string path)
        {
            return File.Open(GetFullPath(path), FileMode.Open);
        }

        public int GetFileSize(string path)
        {
            return (int)(new FileInfo(GetFullPath(path))).Length;
        }
    }
}
