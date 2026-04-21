using HarmonyLib;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Assets;
using System;
using System.IO;
using System.Reflection;
using Environment = Rocket.Core.Environment;

namespace RocketModPluginReloader.Patches
{
    [HarmonyPatch]
    public static class RocketPluginPatches
    {
        [HarmonyPatch(typeof(RocketPlugin), MethodType.Constructor)]
        [HarmonyPrefix]
        public static bool RocketPluginPrefix(object __instance)
        {
            Logger.Log("Patching RocketPlugin constructor...");

            var type = __instance.GetType();
            var assembly = type.Assembly;

            PrivateSetBackingField(__instance, "Assembly", assembly);

            var name = assembly.GetName().Name;
            int lastUnderscore = name.LastIndexOf('_');
            string unifiedName = lastUnderscore > -1 ? name.Substring(0, lastUnderscore) : name;

            PrivateSetBackingField(__instance, "Name", unifiedName);

            var directory = Path.Combine(Rocket.Core.Environment.PluginsDirectory, unifiedName);
            PrivateSetBackingField(__instance, "Directory", directory);

            //Translations
            var rocketPluginInstance = (RocketPlugin)__instance;
            var DefaultTranslations = rocketPluginInstance.DefaultTranslations;

            if (DefaultTranslations != null /*&& DefaultTranslations.Count() != 0*/ && IsOverridden(__instance.GetType()))
            {
                Logger.Log("Default translations found for plugin: " + unifiedName);

                //if (!Directory.Exists(directory)) Directory.CreateDirectory(directory); //No Need Trust
                var translationPath = Path.Combine(directory, string.Format(Environment.PluginTranslationFileTemplate, unifiedName, R.Settings.Instance.LanguageCode));

                var xmlAsset = new XMLFileAsset<TranslationList>(
                    translationPath,
                    [
                        typeof(TranslationList),
                        typeof(TranslationListEntry)
                    ],
                    DefaultTranslations
                );

                PrivateSet(__instance, "translations", xmlAsset);

                var translationsField = AccessTools.Field(type, "translations");
                Logger.Log("Translations loaded: " + ((translationsField?.GetValue(__instance) != null) ? "Correctly" : "Not Correctly"));

                DefaultTranslations.AddUnknownEntries(xmlAsset);
            }
            return false;
        }
        [HarmonyPatch(typeof(RocketPlugin), nameof(RocketPlugin.LoadPlugin))]
        [HarmonyPrefix]
        public static bool LoadPluginPrefix(RocketPlugin __instance)
        {
            Logger.Log("\n[loading] " + __instance.Name, ConsoleColor.Cyan);

            if (__instance.Translations != null)
                __instance.Translations.Load();
            else
                Logger.Log($"[warn] {__instance.Name} has no translations — skipping load.");

            R.Commands.RegisterFromAssembly(__instance.Assembly);

            try
            {
                __instance.GetType().GetMethod("Load",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?
                    .Invoke(__instance, null);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load " + __instance.Name + ", unloading now... :" + ex);
                __instance.UnloadPlugin(PluginState.Failure);
                return false;
            }

            __instance.GetType().GetField("state",
                BindingFlags.Instance | BindingFlags.NonPublic)?
                .SetValue(__instance, PluginState.Loaded);

            return false;
        }
        private static bool IsOverridden(Type pluginType)
        {
            var prop = pluginType.GetProperty(nameof(RocketPlugin.DefaultTranslations));
            return prop?.DeclaringType != typeof(RocketPlugin);
        }
        static void PrivateSetBackingField<T>(object instance, string propName, T value) => AccessTools.Field(instance.GetType(), $"<{propName}>k__BackingField")?.SetValue(instance, value);
        static void PrivateSet<T>(object instance, string propName, T value) => AccessTools.Field(instance.GetType(), propName)?.SetValue(instance, value);
    }
}
