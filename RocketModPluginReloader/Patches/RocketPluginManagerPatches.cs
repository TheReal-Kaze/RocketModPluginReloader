using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Builder;
using AsmResolver.PE.DotNet.Metadata.Strings;
using AsmResolver.PE.DotNet.Metadata.Tables;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using HarmonyLib;
using Rocket.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RocketModPluginReloader.Patches
{
    [HarmonyPatch]
    public class RocketPluginManagerPatches
    {
        public static int ID = 0;
        public static HashSet<string>? Entries;
        public const string blacklistFilePath = "blacklist.rm_reloader";
        [HarmonyPrefix]
        [HarmonyPatch(typeof(RocketPluginManager), nameof(RocketPluginManager.LoadAssembliesFromDirectory))]
        public static bool LoadAssembliesFromDirectoryFix(ref List<Assembly> __result, string directory, string extension = "*.dll")
        {
            __result = new List<Assembly>();
            foreach (FileInfo item in new DirectoryInfo(directory).GetFiles(extension, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    byte[] rawAssembly = File.ReadAllBytes(item.FullName);

                    Assembly assembly = Assembly.Load(ModifyAssembly(rawAssembly));

                    //Assembly assembly = Assembly.Load(rawAssembly);
                    if (RocketHelper.GetTypesFromInterface(assembly, "IRocketPlugin").FindAll((Type x) => !x.IsAbstract).Count == 1)
                    {
                        Logger.Log("Loading " + assembly.GetName().Name + " from the memory");
                        __result.Add(assembly);
                    }
                    else
                        Logger.LogError("Invalid or outdated plugin assembly: " + assembly.GetName().Name);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Could not load plugin assembly: " + item.Name);
                }
            }
            return false;
        }
        public static byte[] ModifyAssembly(byte[] rawAssembly)
        {
            var image = PEImage.FromBytes(rawAssembly);
            var metadata = image.DotNetDirectory!.Metadata!;
            var tablesStream = metadata.GetStream<TablesStream>();
            var oldStringsStream = metadata.GetStream<StringsStream>();

            ref var assemblyRow = ref tablesStream
                .GetTable<AssemblyDefinitionRow>(TableIndex.Assembly)
                .GetRowRef(1);

            string originalName = oldStringsStream.GetStringByIndex(assemblyRow.Name)!;

            // Check Blacklist
            if (IsBlacklisted(originalName))
                return rawAssembly;

            string newName = $"{originalName}_{++ID}";

            assemblyRow.Name = oldStringsStream.GetPhysicalSize();

            using var output = new MemoryStream();
            var writer = new BinaryStreamWriter(output);

            writer.WriteBytes(oldStringsStream.CreateReader().ReadToEnd());
            writer.WriteBytes(Encoding.UTF8.GetBytes(newName));
            writer.WriteByte(0);
            writer.Align(4);

            var newStringsStream = new SerializedStringsStream(output.ToArray());
            tablesStream.StringIndexSize = newStringsStream.IndexSize;
            metadata.Streams[metadata.Streams.IndexOf(oldStringsStream)] = newStringsStream;

            var builder = new ManagedPEFileBuilder();
            output.SetLength(0);
            builder.CreateFile(image).Write(output);

            return output.ToArray();
        }
        static bool IsBlacklisted(string assemblyName)
        {
            LoadBlacklist();
            return Entries?.Contains(assemblyName) ?? false;
        }
        static void LoadBlacklist()
        {
            if (!File.Exists(blacklistFilePath))
            {
                Logger.LogWarning($"[Blacklist] blacklist.dat not found, creating a default one at {Path.GetFullPath(blacklistFilePath)}.");
                File.WriteAllText(blacklistFilePath, "ImperialPluginsLoader.dll, Uconomy.dll");
            }
            if (Entries == null)
            {
                Entries = File.ReadAllText(blacklistFilePath)
               .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(e => e.Trim())
               .Where(e => !string.IsNullOrWhiteSpace(e))
               .Select(e => e.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? e[..^4] : e)
               .ToHashSet(StringComparer.OrdinalIgnoreCase);

                Logger.Log($"[Blacklist] Loading from: {Path.GetFullPath(blacklistFilePath)}");
                Logger.Log($"[Blacklist] Entries: {string.Join(", ", Entries)}");
            }
        }
    }
}
