# GW2 Music Bot 🎵

A standalone desktop application that allows you to automatically play MIDI songs using the musical instruments in Guild Wars 2. 

## Features

- **Online Search:** Find your favorite songs directly within the app using the [Bard's Guild API](https://bardsguild.life/).
- **Local Support:** Play your own `.mid` files.
- **Multiple Instruments:** Support for Piano, Flute, Bass, Lute, Harp, and Minstrel. It automatically handles the available octaves for each instrument.
- **Preview Mode:** Listen to the track locally (audio only) before playing it in-game.
- **Favorites:** Save your preferred online tracks for quick access.
- **Adjustable Speed:** Play tracks faster or slower.

## How to Use

1. Launch Guild Wars 2 and equip your musical instrument.
2. Open **GW2 Music Bot**.
3. Select your equipped instrument in the **Player Settings**.
4. Search for a song or load a local MIDI file.
5. Click **Play (Wait 2s)**.
6. **Immediately click back into the Guild Wars 2 window!** The bot will wait 2 seconds before it starts sending keystrokes to the active window.

## ⚠️ Disclaimer & ArenaNet Policy

Please read ArenaNet's official policy regarding macros and third-party tools here:
[**Policy: Macros and Macro Use**](https://help.guildwars2.com/hc/en-us/articles/360013762153-Policy-Macros-and-Macro-Use#:~:text=In%20general%2C%20our%20policy%20is,is%20prohibited%20under%20any%20circumstances.)

While using macros specifically for playing musical instruments has historically been tolerated by ArenaNet (as long as it doesn't give a gameplay advantage), **you use this tool entirely at your own risk.** It is still a third-party macro program. The creators hold no responsibility for any actions taken against your account. Always be respectful to other players when playing music in crowded areas!

## Building the Project

This application is built with C# and WPF (.NET 10.0).
A GitHub Action is configured to automatically build and publish a single-file `.exe` for Windows upon every release tag.

To build it manually into a standalone executable:
```shell
dotnet publish Gw2MusicBot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish_final
```

## Credits

Made with ❤️ by Lama and Gemini 
Source code: [https://github.com/palpaga/GW2-Music-Bot](https://github.com/palpaga/GW2-Music-Bot)
