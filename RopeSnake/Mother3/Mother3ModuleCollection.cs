using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RopeSnake.Core;
using RopeSnake.Mother3.Data;
using RopeSnake.Mother3.Text;
using RopeSnake.Mother3.Maps;
using RopeSnake.Mother3.Enemy_Graphics;
using RopeSnake.Mother3.Title;

namespace RopeSnake.Mother3
{
    public sealed class Mother3ModuleCollection : IEnumerable<Mother3Module>
    {
        public DataModule Data { get; private set; }
        public TextModule Text { get; private set; }
        public MapModule Maps { get; private set; }
        public TitleModule Title { get; private set; }

        #region Reflection wasteland

        private static readonly Dictionary<string, Type> _keyToModule = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> _moduleToKey = new Dictionary<Type, string>();

        public Mother3Module this[string moduleName] => _loadedModules[moduleName];

        private Dictionary<string, Mother3Module> _loadedModules = new Dictionary<string, Mother3Module>();

        static Mother3ModuleCollection()
        {
            foreach (var property in typeof(Mother3ModuleCollection).GetProperties()
                .Where(p => typeof(Mother3Module).IsAssignableFrom(p.PropertyType))
                .Where(p => p.GetIndexParameters().Length == 0))
            {
                _keyToModule.Add(property.Name, property.PropertyType);
                _moduleToKey.Add(property.PropertyType, property.Name);
            }
        }

        public Mother3ModuleCollection(Mother3RomConfig romConfig, Mother3ProjectSettings projectSettings)
        {
            if (projectSettings.LoadedModules == null)
                projectSettings.LoadedModules = Mother3ProjectSettings.DefaultModules;
            foreach (string key in projectSettings.LoadedModules)
            {
                if (_keyToModule.ContainsKey(key))
                {
                    _loadedModules.Add(key, CreateModule(_keyToModule[key], romConfig, projectSettings));

                    var moduleProperty = GetType().GetProperty(key);
                    moduleProperty.SetValue(this, this[key]);
                }
            }
        }

        public IEnumerator<Mother3Module> GetEnumerator() => _loadedModules.Values.GetEnumerator();

        private Mother3Module CreateModule(Type type, Mother3RomConfig romConfig, Mother3ProjectSettings projectSettings)
        {
            return (Mother3Module)Activator.CreateInstance(type, romConfig, projectSettings);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
