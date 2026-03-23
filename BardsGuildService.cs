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
    }

    public class BardsGuildService
    {
        private readonly HttpClient _client;
        private const string API_KEY = "0Tk-seyqLFwn5qCH2YzrYA";

        public BardsGuildService()
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

        public async Task<string> DownloadMidiAsync(MidiTrackInfo track)
        {
            string downloadUrl = track.SourceUrl;
            
            // If no source, we fallback to Bard's Guild Google Cloud CDN (like LuteBot)
            if (string.IsNullOrEmpty(downloadUrl))
            {
                downloadUrl = $"https://storage.googleapis.com/bgml/mid/{track.Checksum}.mid";
            }

            var response = await _client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();
            
            string tempDir = Path.Combine(Path.GetTempPath(), "Gw2MusicBot");
            Directory.CreateDirectory(tempDir);
            
            string safeName = string.Join("_", track.Name.Split(Path.GetInvalidFileNameChars()));
            if (!safeName.EndsWith(".mid", StringComparison.OrdinalIgnoreCase)) safeName += ".mid";
            
            string localPath = Path.Combine(tempDir, safeName);
            
            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(localPath, bytes);
            
            return localPath;
        }
    }
}
