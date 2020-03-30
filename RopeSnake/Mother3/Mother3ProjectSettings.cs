using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFileSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RopeSnake.Core;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RopeSnake.Mother3
{
    public sealed class Mother3ProjectSettings : INotifyPropertyChanged
    {
        public static readonly string[] DefaultModules =
        {
            //"Data",
            //"Text",
            //"Maps",
            "Title",
            "Battle Sprites"
        };

        public string[] LoadedModules
        {
            get { return _loadedModules; }
            set { SetField(ref _loadedModules, value); }
        }

        private string[] _loadedModules;

        public string BaseRomFile
        {
            get { return _baseRomFile; }
            set { SetField(ref _baseRomFile, value); }
        }
        private string _baseRomFile;

        public string OutputRomFile
        {
            get { return _outputRomFile; }
            set { SetField(ref _outputRomFile, value); }
        }
        private string _outputRomFile;

        public string RomConfigFile
        {
            get { return _romConfigFile; }
            set { SetField(ref _romConfigFile, value); }
        }
        private string _romConfigFile;

        [JsonProperty, JsonConverter(typeof(StringEnumConverter))]
        public OffsetTableMode OffsetTableMode
        {
            get { return _offsetTableMode; }
            set { SetField(ref _offsetTableMode, value); }
        }
        private OffsetTableMode _offsetTableMode;

        public static Mother3ProjectSettings CreateDefault()
        {
            return new Mother3ProjectSettings
            {
                BaseRomFile = "/base.gba",
                OutputRomFile = "/test.gba",
                RomConfigFile = "/rom.config.json",
                OffsetTableMode = OffsetTableMode.Fragmented,
                LoadedModules = DefaultModules.ToArray()
            };
        }

        public static Mother3ProjectSettings Create(IFileSystem fileSystem, FileSystemPath path)
        {
            var jsonManager = new JsonFileManager(fileSystem);

            jsonManager.SerializerSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

            return jsonManager.ReadJson<Mother3ProjectSettings>(path);
        }

        public void Save(IFileSystem fileSystem, FileSystemPath path)
        {
            var jsonManager = new JsonFileManager(fileSystem);
            jsonManager.WriteJson(path, this);
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    public enum OffsetTableMode
    {
        Fragmented,
        Contiguous
    }
}
