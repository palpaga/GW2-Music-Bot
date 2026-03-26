using System.Collections.Generic;
using System.IO;
using System;
using System.Text.Json;

namespace Gw2MusicBot
{
    public class KeyBindsConfig
    {
        public ushort[] Notes { get; set; } = new ushort[12] { 0x31, 0x70, 0x32, 0x71, 0x33, 0x34, 0x72, 0x35, 0x73, 0x36, 0x74, 0x37 };
        public ushort HighC { get; set; } = 0x38;      // 8
        public ushort OctaveDown { get; set; } = 0x39; // 9
        public ushort OctaveUp { get; set; } = 0x30;   // 0
        public ushort StopPlayback { get; set; } = 0x1B; // ESC
        public bool DisableFunctionKeys { get; set; } = true;
    }

    public class AppConfig
    {
        public string Language { get; set; } = "en"; // Default language
        public List<MidiTrackInfo> Favorites { get; set; } = new List<MidiTrackInfo>();
        public KeyBindsConfig KeyBinds { get; set; } = new KeyBindsConfig();
    }

    public static class ConfigManager
    {
        private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gw2MusicBot", "config.json");

        public static AppConfig Config { get; private set; } = new AppConfig();

        public static void Load()
        {
            // Migrate from old local path if it exists
            if (!File.Exists(FilePath) && File.Exists("config.json"))
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.Move("config.json", FilePath);
                }
                catch { }
            }

            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                    if (loaded != null) Config = loaded;
                }
                catch { }
            }
        }

        public static void Save()
        {
            try 
            {
                string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}





