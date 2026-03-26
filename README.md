# GW2 Music Bot 🎵

A standalone, modern desktop application that allows you to automatically play MIDI songs using the musical instruments in Guild Wars 2.

## ✨ Features

- **Online Search:** Find your favorite songs directly within the app using the [Bard's Guild API](https://bardsguild.life/).
- **Local Support:** Load and play your own .mid files easily.
- **Multiple Instruments:** Support for Piano, Flute, Bass, Lute, Harp, and Minstrel. It automatically handles the available octaves for each instrument.
- **Preview Mode:** Listen to tracks locally (audio only) before playing them in-game.
- **Favorites System:** Save your preferred online tracks (indicated by a golden star ★) for quick access.
- **Advanced Player Settings:** 
  - Restrict playback to 2 octaves (prevents dropped notes in fast songs).
  - Adjust the delay before an octave switch.
  - Modify playback speed.
- **Bilingual Interface:** Switch seamlessly between English and French.
- **Modern Dark Theme:** A clean, easy-on-the-eyes interface with smart status indicators.

## 🚀 How to Use

1. Launch Guild Wars 2 and equip your musical instrument.
2. Open **GW2 Music Bot**.
3. Select your equipped instrument in the **Player Settings**.
4. Search for a song or load a local MIDI file.
5. Click **▶ PLAY**.
6. **Immediately click back into the Guild Wars 2 window!** The bot will count down from 3 seconds before it starts sending keystrokes to the active window.
7. To stop playback at any time, press your configured Stop bind (e.g., Space or NumPad0).

## ⚠️ Disclaimer & ArenaNet Policy

While using macros specifically for playing musical instruments has historically been tolerated by ArenaNet (as long as it doesn't give a gameplay advantage), **you use this tool entirely at your own risk.** 

It is a third-party macro program. The creators hold no responsibility for any actions taken against your account. Always be respectful to other players when playing music in crowded areas!

Please read ArenaNet's official policy regarding macros and third-party tools here:
[**Policy: Macros and Macro Use**](https://help.guildwars2.com/hc/en-us/articles/360013762153-Policy-Macros-and-Macro-Use)

### 💡 Limitations
The bot presses keys blindly with no game feedback. Input lag or low framerates can cause missed keys or missed octave shifts. If a song sounds degraded or if the bot starts casting your skills (because your instrument unequipped), press your Stop bind immediately. You can adjust the "Playback Speed" or "Octave Delay" settings to improve reliability on complex songs.

## 🛠️ Building the Project

This application is built with C# and WPF (.NET 10.0).
A GitHub Action is configured to automatically build and publish a single-file .exe for Windows upon every release tag.

To build it manually into a standalone executable:
`shell
dotnet publish Gw2MusicBot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish_final
`

## 💖 Credits

Made with ❤️ by Lama and Google Gemini  
Source code: [https://github.com/palpaga/GW2-Music-Bot](https://github.com/palpaga/GW2-Music-Bot)
