using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpFileSystem;
using RopeSnake.Core;
using RopeSnake.Mother3.Data;
using System.Diagnostics;
using NLog;
using File = System.IO.File;
using Directory = System.IO.Directory;
using Newtonsoft.Json;
using RopeSnake.Mother3.Enemy_Graphics;
using RopeSnake.Mother3.Title;
using System.Drawing;

namespace RopeSnake.Mother3
{
    public sealed class Mother3Project
    {
        private static readonly Logger _log = LogManager.GetLogger(nameof(Mother3Project));
        public  static string outputO;
        private static readonly FileSystemPath CachePath = "/.cache/".ToPath();
        private static readonly FileSystemPath CacheKeysFile = CachePath.AppendFile("keys.txt");
        private static readonly FileSystemPath FileSystemStatePath = CachePath.AppendFile("state.json");
        private static readonly FileSystemPath CompilationReportLog = "/compile.log".ToPath();
        public static readonly FileSystemPath DefaultProjectFile = "/project.json".ToPath();

        public Mother3RomConfig RomConfig { get; private set; }
        public Mother3ProjectSettings ProjectSettings { get; private set; }
        public Mother3ModuleCollection Modules { get; private set; }
        public HashSet<object> StaleObjects { get; private set; }

        private Mother3Project(Mother3RomConfig romConfig,
            Mother3ProjectSettings projectSettings)
        {
            RomConfig = romConfig;
            ProjectSettings = projectSettings;
            Modules = new Mother3ModuleCollection(romConfig, projectSettings);
        }

        private void UpdateRomConfig(Block romData)
        {
            if (romData != null)
            {
                if (RomConfig.ScriptEncodingParameters != null)
                    RomConfig.ReadEncodingPadData(romData);
            }

            RomConfig.UpdateLookups();
        }

        public static Mother3Project CreateNew(string romDataPath, string romConfigPath, string outputDirectory,
            IProgress<ProgressPercent> progress = null)
        {
            foreach (var filePath in new[] { romDataPath, romConfigPath })
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            if (File.Exists(outputDirectory))
                throw new Exception("Output path must be a directory");

            _log.Info($"Creating new project with ROM file \"{romDataPath}\" and config file \"{romConfigPath}\"");

            var romData = new Block(File.ReadAllBytes(romDataPath));
            var romConfig = JsonConvert.DeserializeObject<Mother3RomConfig>(File.ReadAllText(romConfigPath));
            var projectSettings = Mother3ProjectSettings.CreateDefault();

            var project = new Mother3Project(romConfig, projectSettings);
            project.UpdateRomConfig(romData);

            if (romConfig.IsJapanese)
                romConfig.AddJapaneseCharsToLookup(romData);

            var fileSystem = new PhysicalFileSystemWrapper(outputDirectory);
            fileSystem.CreateDirectory(FileSystemPath.Root);
            project.SaveProject(fileSystem, DefaultProjectFile);

            _log.Info("Copying base ROM to output directory");
            File.Copy(romDataPath, Path.Combine(outputDirectory, projectSettings.BaseRomFile.ToPath().EntityName), true);

            _log.Info("Finished creating project");
            return project;
        }

        public static Mother3Project Load(IFileSystem fileSystem, FileSystemPath projectSettingsPath,
            IProgress<ProgressPercent> progress, string Out)
        {
            _log.Info($"Loading project \"{projectSettingsPath.Path}\"");
            var projectSettings = Mother3ProjectSettings.Create(fileSystem, projectSettingsPath);
            var romConfig = Mother3RomConfig.Create(fileSystem, projectSettings.RomConfigFile.ToPath());

            var binaryManager = new BinaryFileManager(fileSystem);
            var baseRom = binaryManager.ReadFile<Block>(projectSettings.BaseRomFile.ToPath());

            var project = new Mother3Project(romConfig, projectSettings);
            project.UpdateRomConfig(baseRom);

            foreach (var module in project.Modules)
            {
                module.Progress = progress;
                _log.Info($"Loading module: {module.Name}");
                module.ReadFromFiles(fileSystem);
            }
            outputO = Out;
            _log.Info("Finished loading project");

            return project;
        }

        public void SaveProject(IFileSystem fileSystem, FileSystemPath projectSettingsPath)
        {
            _log.Info($"Saving project \"{projectSettingsPath.Path}\"");

            var jsonManager = new JsonFileManager(fileSystem);
            jsonManager.WriteJson(projectSettingsPath, ProjectSettings);
            jsonManager.WriteJson(ProjectSettings.RomConfigFile.ToPath(), RomConfig);
        }

        public void WriteToFiles(IFileSystem fileSystem, FileSystemPath projectSettingsPath)
        {
            _log.Info("Writing modules to disk");
            foreach (var module in Modules)
            {
                _log.Info($"Writing module {module.Name}");
                module.WriteToFiles(fileSystem, StaleObjects);
            }
            _log.Info("Finished writing modules");
        }

        public void Decompile(string output, IFileSystem fileSystem, IProgress<ProgressPercent> progress = null)
        {
            _log.Info("Decompiling project");

            _log.Info($"Reading base ROM from {ProjectSettings.BaseRomFile}");
            var binaryManager = new BinaryFileManager(fileSystem);
            var baseRom = binaryManager.ReadFile<Block>(ProjectSettings.BaseRomFile.ToPath());
            UpdateRomConfig(baseRom);

            if (RomConfig.Patches != null)
            {
                foreach (var patchCollection in RomConfig.Patches)
                {
                    _log.Info($"Applying patch: {patchCollection.Description}");
                    foreach (var patch in patchCollection)
                    {
                        patch.Apply(baseRom);
                    }
                }
            }

            foreach (var module in Modules)
            {
                _log.Info($"Reading module from ROM: {module.Name}");
                module.Progress = progress;
                module.ReadFromRom(baseRom);
            }

            //Modules.Data.UpdateNameHints(Modules.Text);
            //Modules.Maps.UpdateNameHints(Modules.Text);
            _log.Info("Decompiling enemies graphics");
            outputO = output;
            if(ProjectSettings.LoadedModules.Contains("Battle Sprites"))
                Extraction.Extract(baseRom.Data, output + "\\BattleSprites\\", progress);
            _log.Info("Finished decompiling project");
        }
        public void Compile(IFileSystemWrapper fileSystem, bool useCache, int maxThreads = 1,
            IProgress<ProgressPercent> progress = null)
        {
            _log.Info("Compiling project");

            var freeRanges = Modules.SelectMany(m => RomConfig.FreeRanges[m.Name])
                .Concat(RomConfig.FreeRanges["Nullspace"]);
            var allocator = new RangeAllocator(freeRanges);
            var binaryManager = new BinaryFileManager(fileSystem);
            var baseRom = binaryManager.ReadFile<Block>(ProjectSettings.BaseRomFile.ToPath());
            var outputRomData = new Block(baseRom);
            BlockCollection cache;
            if (useCache)
            {
                _log.Info("Reading cache");
                var staleBlockKeys = GetStaleBlockKeys(fileSystem, GetChangedPaths(fileSystem));
                cache = ReadCache(fileSystem, staleBlockKeys, progress);
            }
            else
            {
                cache = new BlockCollection();
            }

            _log.Info("Executing compiler");
            var compiler = Compiler.Create(outputRomData, allocator, Modules, cache, maxThreads);
            compiler.AllocationAlignment = 4;
            var compilationResult = compiler.Compile(progress);
            if(ProjectSettings.LoadedModules.Contains("Battle Sprites"))
                Importing.Import(ref outputRomData, progress, outputO);
            //_log.Debug("Filling free ranges with 0xFF");
            //FillFreeRanges(outputRomData, allocator.Ranges, 0xFF);
            _log.Info($"Writing output ROM file to {ProjectSettings.OutputRomFile}");
            binaryManager.WriteFile(ProjectSettings.OutputRomFile.ToPath(), outputRomData);

            if (useCache)
            {
                _log.Info("Writing cache");
                WriteCache(fileSystem, compilationResult.WrittenBlocks, compilationResult.UpdatedKeys, progress);

                var jsonManager = new JsonFileManager(fileSystem);
                jsonManager.WriteJson(FileSystemStatePath, fileSystem.GetState(FileSystemPath.Root, CachePath));
            }
            LogCompilationResult(fileSystem, compilationResult, allocator.Ranges);
            _log.Info("Finished compiling");
        }

        public BlockCollection ReadCache(IFileSystem fileSystem, IEnumerable<string> staleBlockKeys,
            IProgress<ProgressPercent> progress = null)
        {
            if (fileSystem.Exists(CachePath))
            {
                var binaryManager = new BinaryFileManager(fileSystem);

                if (progress != null)
                    binaryManager.FileRead += (s, e) => FileManagerBase.FileReadEventProgressHandler(s, e, progress);

                return new BlockCollection(binaryManager.ReadFileDictionary<Block>(CachePath, staleBlockKeys));
            }
            else
            {
                return new BlockCollection();
            }
        }

        public void WriteCache(IFileSystem fileSystem, BlockCollection cache, IEnumerable<string> updatedKeys,
            IProgress<ProgressPercent> progress = null)
        {
            var binaryManager = new BinaryFileManager(fileSystem);

            if (progress != null)
                binaryManager.FileWrite += (s, e) => FileManagerBase.FileWriteEventProgressHandler(s, e, progress);

            binaryManager.StaleObjects = new HashSet<object>(updatedKeys.Select(k => cache[k]));
            binaryManager.WriteFileDictionary(CachePath, cache);
        }

        public bool Validate()
        {
            bool success = true;
            foreach (var module in Modules)
            {
                _log.Info($"Validating module {module.Name}...");
                success &= module.Validate(new LazyString(module.Name));
            }
            _log.Info("Finished validating modules");
            return success;
        }

        private IEnumerable<string> GetStaleBlockKeys(IFileSystem fileSystem, IEnumerable<FileSystemPath> paths)
        {
            return Modules.SelectMany(m => paths.SelectMany(p => m.GetStaleBlockKeys(fileSystem, p)));
        }

        private FileSystemState GetPreviousFileSystemState(IFileSystem fileSystem)
        {
            if (!fileSystem.Exists(FileSystemStatePath))
                return new FileSystemState(Enumerable.Empty<FileMetaData>());

            var jsonManager = new JsonFileManager(fileSystem);
            return jsonManager.ReadJson<FileSystemState>(FileSystemStatePath);
        }

        private IEnumerable<FileSystemPath> GetChangedPaths(IFileSystemWrapper fileSystem)
        {
            var previousState = GetPreviousFileSystemState(fileSystem);
            var currentState = fileSystem.GetState(FileSystemPath.Root, CachePath);
            var differences = currentState.Compare(previousState);
            return differences.Keys;
        }

        private static void LogCompilationResult(IFileSystem fileSystem, Compiler.CompilationResult result,
            IEnumerable<Range> remainingRanges)
        {
            using (var logFile = fileSystem.CreateFile(CompilationReportLog))
            {
                using (var writer = new StreamWriter(logFile))
                {
                    writer.WriteLine($"Compilation report, {DateTime.Now}");
                    writer.WriteLine("====");
                    writer.WriteLine();

                    if (remainingRanges.Count() > 0)
                    {
                        int totalFree = remainingRanges.Sum(r => r.Size);
                        writer.WriteLine($"Remaining free ranges: {totalFree} (0x{totalFree:X}) bytes total");

                        var logger = new LogTableWriter(writer);
                        logger.AddHeader("Start", 12);
                        logger.AddHeader("End", 12);
                        logger.AddHeader("Size", 12);
                        logger.AddHeader("Size (hex)", 12);
                        logger.WriteHeader(2);
                        foreach (var range in remainingRanges)
                        {
                            logger.WriteLine(2, $"0x{range.Start:X}", $"0x{range.End:X}", range.Size, $"0x{range.Size:X}");
                        }
                        writer.WriteLine();
                    }

                    if (result.WrittenBlocks.Count > 0)
                    {
                        writer.WriteLine("Blocks written to ROM:");

                        var logger = new LogTableWriter(writer);
                        logger.AddHeader("", 4);
                        logger.AddHeader("Key", 40);
                        logger.AddHeader("Size", 12);
                        logger.AddHeader("Size (hex)", 12);
                        logger.AddHeader("Location", 12);
                        logger.WriteHeader(2);
                        foreach (var kv in result.WrittenBlocks.Where(kv => kv.Value != null)
                            .OrderBy(kv => result.AllocationResult[kv.Key]))
                        {
                            var key = kv.Key;
                            var block = kv.Value;
                            logger.WriteLine(2, result.UpdatedKeys.Contains(key) ? "[*]" : "", key, block.Size, $"0x{block.Size:X}", $"0x{result.AllocationResult[key]:X}");
                        }
                    }
                }
            }
        }

        private void FillFreeRanges(Block romData, IEnumerable<Range> freeRanges, byte fillValue)
        {
            foreach (var range in freeRanges)
            {
                for (int position = range.Start; position <= range.End; position++)
                {
                    romData[position] = fillValue;
                }
            }
        }
    }
}
