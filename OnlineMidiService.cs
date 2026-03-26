using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gw2MusicBot
{
    public class MidiTrackInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;
        public int Score { get; set; }
        public double PlaybackSpeed { get; set; } = 1.0;
        public bool RestrictToTwoOctaves { get; set; } = false;
        public int OctaveChangeDelayMs { get; set; } = 15;
        public int SelectedTrackIndex { get; set; } = -1;
    }

    public class OnlineMidiService
    {
        private readonly HttpClient _client;
        private const string API_KEY = "0Tk-seyqLFwn5qCH2YzrYA";

        public OnlineMidiService()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Gw2MusicBot");
        }

        public async Task<List<MidiTrackInfo>> SearchAsync(string query)
        {
            string url = $"http://api.bardsguild.life/?key={API_KEY}&find={Uri.EscapeDataString(query)}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var tracks = new List<MidiTrackInfo>();
            
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        // LuteBot uses "filename" or "title"
                        string name = item.GetProperty("filename").GetString() ?? 
                                      item.GetProperty("title").GetString() ?? "Unknown";
                        
                        string checksum = item.GetProperty("checksum").GetString() ?? "";
                        
                        // LuteBot checks if a source URL exists
                        string sourceUrl = "";
                        if (item.TryGetProperty("source_url", out JsonElement sourceElement) && sourceElement.ValueKind != JsonValueKind.Null)
                        {
                            sourceUrl = sourceElement.GetString() ?? "";
                        }
                        
                        int score = item.GetProperty("m_score").GetInt32();
                        
                        tracks.Add(new MidiTrackInfo
                        {
                            Name = name,
                            Checksum = checksum,
                            SourceUrl = sourceUrl,
                            Score = score
                        });
                    }
                }
                
                // Sort by descending score (best quality for LuteBot/GW2)
                tracks.Sort((a, b) => b.Score.CompareTo(a.Score));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse Bard's Guild JSON: {ex.Message}");
            }
            
            return tracks;
        }

        public async Task<string> DownloadMidiAsync(MidiTrackInfo track, bool isFavorite = false)
        {
            string downloadUrl = track.SourceUrl;

            // If no source, we fallback to Bard's Guild Google Cloud CDN (like LuteBot)
            if (string.IsNullOrEmpty(downloadUrl))
            {
                downloadUrl = $"https://storage.googleapis.com/bgml/mid/{track.Checksum}.mid";
            }

            string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gw2MusicBot", "Cache");
            string tempDir = Path.Combine(Path.GetTempPath(), "Gw2MusicBot");

            string safeName = string.Join("_", track.Name.Split(Path.GetInvalidFileNameChars()));
            if (!safeName.EndsWith(".mid", StringComparison.OrdinalIgnoreCase)) safeName += ".mid";

            string localPath = isFavorite 
                ? Path.Combine(cacheDir, $"{track.Checksum}_{safeName}")
                : Path.Combine(tempDir, safeName);

            if (isFavorite)
            {
                Directory.CreateDirectory(cacheDir);
                if (File.Exists(localPath)) return localPath;
            }
            else
            {
                Directory.CreateDirectory(tempDir);
            }

            var response = await _client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(localPath, bytes);

            return localPath;
        }
    }
}


