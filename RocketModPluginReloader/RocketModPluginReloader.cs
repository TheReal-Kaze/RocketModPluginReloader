global using Logger = Rocket.Core.Logging.Logger;
global using Rocket.Core;
global using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using Rocket.Unturned.Chat;
using HarmonyLib;
using SDG.Framework.Modules;
using SDG.Unturned;
using System.Reflection;
using UnityEngine;

namespace RocketModPluginReloader
{
    public class RocketModPluginReloader : IModuleNexus
    {
        internal const string HarmonyId = "com.Kaze.rmfix";
        internal Harmony? harmony;

        internal void RegisterInput() 
        {
            CommandWindow.onCommandWindowInputted += HandleInput;
            UnturnedPlayerEvents.OnPlayerChatted += OnPlayerChatted;
        }
        internal void UnregisterInput()
        {
            CommandWindow.onCommandWindowInputted -= HandleInput;
            UnturnedPlayerEvents.OnPlayerChatted -= OnPlayerChatted;
        }
        public void initialize()
        {
            harmony = new Harmony(HarmonyId);
            harmony?.PatchAll();

            //var list = harmony.GetPatchedMethods().ToList();
            //Logger.Log($"Count of patched method {list.Count}");
            RegisterInput();

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            Logger.Log($"{assemblyName.Name} {assemblyName.Version} has been loaded!");
        }
        public void shutdown()
        {
            UnregisterInput();
            harmony?.UnpatchAll(HarmonyId);
        }
        private void HandleInput(string Text, ref bool ShouldExecuteCommand)
        {
            if (!Text.StartsWith("/") || !Text.ToLower().Contains("rm rel")) return;

            var reloadMethod = AccessTools.Method(typeof(RocketPluginManager), "Reload");
            reloadMethod.Invoke(R.Plugins, null);

            UnturnedChat.Say("Plugins reloaded!", Color.green);
        }
        private void OnPlayerChatted(UnturnedPlayer player, ref Color color, string message, EChatMode chatMode, ref bool cancel)
        {
            if(player.IsAdmin) HandleInput(message, ref cancel);
        }
    }
}
