using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Gw2MusicBot
{
    public static class FavoritesManager
    {
        private static readonly string FilePath = "favorites.json";

        public static List<MidiTrackInfo> LoadFavorites()
        {
            if (!File.Exists(FilePath))
                return new List<MidiTrackInfo>();

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<List<MidiTrackInfo>>(json) ?? new List<MidiTrackInfo>();
            }
            catch
            {
                return new List<MidiTrackInfo>();
            }
        }

        public static void SaveFavorites(List<MidiTrackInfo> favorites)
        {
            string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
    }
}
