# RocketMod Plugin Reloading Module

## Overview

In RocketMod, when a plugin developer updates a DLL, the server must be stopped because the DLL gets locked after loading. This prevents replacing or deleting the DLL, making testing and updating plugins a hassle. Every change requires a full server restart.

This module removes that limitation by patching RocketMod, allowing plugins to be dynamically reloaded, similar to OpenMod. After each reload, only the updated DLL inside the plugin folder is loaded. This means developers can modify and reload plugins on the fly without interrupting the server, significantly improving the development experience (please dispose correctly your plugins).

It also no longer creates plugin folders, config files, or translation files when the plugin doesn't actually need them.

## Command

`/rm reload` or `/rm rel`

Can be used from the server console or in-game chat (admin only).

## Installation

1. Place the `RocketModPluginReloader` folder inside the `Modules` folder of your server.
2. Done!

## Blacklist

To prevent the reloader from renaming a plugin (and to skip the name-sensitive reload logic), add its DLL name to the blacklist file.

- **Path:** `<ServerRoot>/Rocket/blacklist.rm_reloader`
- **Format:** one per line, or separate with commas/semicolons (`PluginName.dll` or `PluginName` -- case-insensitive, `.dll` optional)
- **Auto-create:** the file is generated on first run if missing
- **Effect:** the plugin still loads normally, but its assembly name is left unchanged, so you can forget about having the new version load if blacklisted -- only the first version loaded will persist even if you put an updated DLL
- **Caching:** the blacklist is parsed once per server lifetime. If you edit the file, restart the server for changes to take effect

## Credits

Some code from OpenMod has been used in this repository.

Special thanks to:
- Trojaner
- Rube200
- iamsilk
- Diffoz

## License

This project includes code from OpenMod, which is licensed under the MIT License.

Modifications have been made to enable dynamic reloading for RocketMod.

## Known Issues

- None
