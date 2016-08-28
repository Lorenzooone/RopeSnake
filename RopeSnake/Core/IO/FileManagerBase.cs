﻿using SharpFileSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RopeSnake.Core
{
    public abstract class FileManagerBase
    {
        private IFileSystem _fileSystem;
        private object _lockObj = new object();
        protected IFileSystem FileSystem { get { return _fileSystem; } }
        protected IndexTotal CurrentIndex { get; set; }
        protected string CountFile { get; } = "count.txt";

        public ISet<object> StaleObjects { get; set; }
        public event FileEventDelegate FileRead;
        public event FileEventDelegate FileWrite;
        public bool SuppressEvents { get; set; } = false;

        protected FileManagerBase(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        protected virtual void OnFileRead(FileSystemPath path, IndexTotal index)
        {
            if (FileRead != null && !SuppressEvents)
            {
                FileRead(this, new FileEventArgs(path, index));
            }
        }

        protected virtual void OnFileWrite(FileSystemPath path, IndexTotal index)
        {
            if (FileWrite != null && !SuppressEvents)
            {
                FileWrite(this, new FileEventArgs(path, index));
            }
        }

        protected void ReadFileAction(FileSystemPath path, Action<Stream> action)
        {
            if (!path.IsFile)
                throw new ArgumentException(nameof(path));

            if (!FileSystem.Exists(path))
                throw new FileNotFoundException("File not found", path.Path);

            OnFileRead(path, CurrentIndex);

            using (var stream = FileSystem.OpenFile(path, FileAccess.Read))
            {
                action(stream);
            }
        }

        protected void CreateFileAction(FileSystemPath path, object value, Action<Stream> action)
        {
            if (!path.IsFile)
                throw new ArgumentException(nameof(path));

            if (!IsStale(value))
                return;

            OnFileWrite(path, CurrentIndex);

            if (!FileSystem.Exists(path.ParentPath))
            {
                FileSystem.CreateDirectoryRecursive(path.ParentPath);
            }

            using (var stream = FileSystem.CreateFile(path))
            {
                action(stream);
            }
        }

        protected List<T> ReadFileListAction<T>(FileSystemPath directory, string extension, Func<FileSystemPath, T> action)
        {
            if (!directory.IsDirectory)
                throw new ArgumentException(nameof(directory));

            var list = new List<T>();

            lock (_lockObj)
            {
                CurrentIndex = IndexTotal.Single;
                var countPath = directory.AppendFile(CountFile);

                if (!FileSystem.Exists(countPath))
                    throw new Exception($"The count file {CountFile} is missing from {directory.Path}.");

                int count = 0;
                ReadFileAction(countPath, stream =>
                {
                    using (var reader = new StreamReader(stream))
                    {
                        if (!int.TryParse(reader.ReadToEnd(), out count))
                        {
                            throw new FormatException("The count file must contain a single integer string.");
                        }
                    }
                });

                for (int i = 0; i < count; i++)
                {
                    CurrentIndex = new IndexTotal(i + 1, count);
                    var path = directory.AppendFile($"{i}.{extension}");

                    if (FileSystem.Exists(path))
                    {
                        list.Add(action(path));
                    }
                    else
                    {
                        list.Add(default(T));
                    }
                }
            }

            return list;
        }

        protected void CreateFileListAction<T>(FileSystemPath directory, string extension, IList<T> list, Action<FileSystemPath, T> action)
        {
            if (!directory.IsDirectory)
                throw new ArgumentException(nameof(directory));

            lock (_lockObj)
            {
                CurrentIndex = IndexTotal.Single;
                CreateFileAction(directory.AppendFile(CountFile), null, stream =>
                {
                    using (var writer = new StreamWriter(stream))
                        writer.Write(list.Count.ToString());
                });

                for (int i = 0; i < list.Count; i++)
                {
                    CurrentIndex = new IndexTotal(i + 1, list.Count);
                    var path = directory.AppendFile($"{i}.{extension}");

                    T value = list[i];
                    if (value != null)
                    {
                        action(path, value);
                    }
                    else
                    {
                        FileSystem.Delete(path);
                    }
                }
            }
        }

        protected virtual bool IsStale(object value)
        {
            if (StaleObjects == null || value == null)
                return true;

            if (StaleObjects.Contains(value))
                return true;

            var enumerable = value as IEnumerable;
            foreach (var child in enumerable)
            {
                if (StaleObjects.Contains(child))
                    return true;
            }

            return false;
        }
    }
}