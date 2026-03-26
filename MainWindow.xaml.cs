using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2MusicBot;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Gw2MidiPlayer _player;
    private OnlineMidiService _midiService;
    private List<MidiTrackInfo> _favorites;
    private MidiTrackInfo? _currentTrack = null;
    private bool _isUpdatingUI = false;
    private bool _isPreviewPlaying = false;

    public MainWindow()
    {
        InitializeComponent();
        _player = new Gw2MidiPlayer();
        _midiService = new OnlineMidiService();
        
        ConfigManager.Load();
        _favorites = ConfigManager.Config.Favorites;

        // Initialize language dropdown
        if (ConfigManager.Config.Language == "fr")
            CmbLanguage.SelectedIndex = 1;
        else
            CmbLanguage.SelectedIndex = 0;
            
        ApplyTranslations();
        
        UpdateKeyBindUI();
        
        _player.PlaybackFinished += (s, e) => Dispatcher.Invoke(() => ResetUI());
        _player.PlaybackStopped += (s, e) => Dispatcher.Invoke(() => ResetUI());
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbLanguage.SelectedItem is ComboBoxItem item)
        {
            string newLang = item.Tag?.ToString() ?? "en";
            if (ConfigManager.Config.Language != newLang)
            {
                ConfigManager.Config.Language = newLang;
                ConfigManager.Save();
                ApplyTranslations();
            }
        }
    }

    private void ApplyTranslations()
    {
        Title = I18n.T("AppTitle");
        TxtMainTitle.Text = I18n.T("AppTitle");
        TxtSearch.Text = I18n.T("SearchPlaceholder");
        BtnSearch.Content = I18n.T("BtnSearch");
        BtnShowFavorites.Content = I18n.T("BtnFavorites");
        BtnLoadLocal.Content = I18n.T("BtnLocal");
        
        GrpPlayerSettings.Header = I18n.T("PlayerSettings");
        TxtMidiTrackLabel.Text = I18n.T("MidiTrack");
        
        if (!_isPreviewPlaying)
        {
            BtnPreviewTrack.Content = "▶ " + I18n.T("PreviewTrack");
            BtnPreviewTrack.Style = (Style)FindResource("AccentButton");
        }
        else
        {
            BtnPreviewTrack.Content = I18n.T("PreviewStop");
            BtnPreviewTrack.Style = (Style)FindResource("DangerButton");
        }
        
        ChkTwoOctaves.Content = I18n.T("RestrictOctaves");
        TxtTwoOctavesDesc.Text = I18n.T("RestrictOctavesDesc");
        TxtOctaveDelayLabel.Text = " | " + I18n.T("OctaveDelay");
        TxtPlaybackSpeedLabel.Text = I18n.T("PlaybackSpeed");
        ExpKeyMapping.Header = I18n.T("KeyMapping");
        ChkDisableFKeys.Content = I18n.T("DisableFKeys");
        TxtDisableFKeysDesc.Text = I18n.T("DisableFKeysDesc");
        
        if (TxtFileName.Text == "No music selected" || TxtFileName.Text == "Aucune musique sélectionnée")
        {
            TxtFileName.Text = I18n.T("NoMusicSelected");
        }
        
        if (!_player.IsPlaying)
        {
            BtnPlay.Content = "▶ " + I18n.T("Play");
            BtnPlay.Style = (Style)FindResource("PrimaryButton");
        }
        else
        {
            string stopKeyName = KeyInterop.KeyFromVirtualKey(ConfigManager.Config.KeyBinds.StopPlayback).ToString();
            BtnPlay.Content = I18n.T("BtnStop", stopKeyName);
            BtnPlay.Style = (Style)FindResource("DangerButton");
        }

        // Traduction Footer
        TxtDisclaimerTitle.Text = I18n.T("DisclaimerTitle");
        RunDisclaimerText1.Text = I18n.T("DisclaimerText");
        RunDisclaimerBold.Text = I18n.T("DisclaimerBold");
        RunDisclaimerEnd.Text = I18n.T("DisclaimerEnd");
        RunDisclaimerLink.Text = I18n.T("DisclaimerLink");
        RunDisclaimerLimitations.Text = I18n.T("DisclaimerLimitations");
        
        // Refresh track combobox if populated
        if (CmbTracks.Items.Count > 0 && CmbTracks.Items[0] is string firstItem && (firstItem == "All Tracks (Merged)" || firstItem == "Toutes les Pistes (Fusionnées)"))
        {
            ApplyTrackSettingsToUI(); 
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "v1.0.0";
        string tag = currentVersion.Split('+')[0];
        Title = $"{I18n.T("AppTitle")} ({tag})";
        await CheckForUpdatesAsync(tag);
    }
    
    private async Task CheckForUpdatesAsync(string currentTag)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Gw2MusicBot");
            var response = await client.GetStringAsync("https://api.github.com/repos/palpaga/GW2-Music-Bot/releases/latest");
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            string latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            
            if (!string.IsNullOrEmpty(latestTag) && latestTag != currentTag && !currentTag.StartsWith(latestTag))
            {
                if (string.Compare(latestTag, currentTag) > 0)
                {
                    RunUpdateText.Text = I18n.T("UpdateAvailable", latestTag);
                    LinkUpdate.NavigateUri = new Uri(doc.RootElement.GetProperty("html_url").GetString() ?? "");
                    TxtUpdate.Visibility = Visibility.Visible;
                }
            }
        }
        catch { }
    }

    private void UpdateKeyBindUI()
    {
        var binds = ConfigManager.Config.KeyBinds;
        
        TxtNote0.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[0]).ToString();
        TxtNote1.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[1]).ToString();
        TxtNote2.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[2]).ToString();
        TxtNote3.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[3]).ToString();
        TxtNote4.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[4]).ToString();
        TxtNote5.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[5]).ToString();
        TxtNote6.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[6]).ToString();
        TxtNote7.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[7]).ToString();
        TxtNote8.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[8]).ToString();
        TxtNote9.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[9]).ToString();
        TxtNote10.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[10]).ToString();
        TxtNote11.Text = KeyInterop.KeyFromVirtualKey(binds.Notes[11]).ToString();
        
        TxtHighC.Text = KeyInterop.KeyFromVirtualKey(binds.HighC).ToString();
        TxtOctDown.Text = KeyInterop.KeyFromVirtualKey(binds.OctaveDown).ToString();
        TxtOctUp.Text = KeyInterop.KeyFromVirtualKey(binds.OctaveUp).ToString();
        TxtStop.Text = KeyInterop.KeyFromVirtualKey(binds.StopPlayback).ToString();
        

        ChkDisableFKeys.Checked -= ChkDisableFKeys_Changed;
        ChkDisableFKeys.Unchecked -= ChkDisableFKeys_Changed;
        ChkDisableFKeys.IsChecked = binds.DisableFunctionKeys;
        ChkDisableFKeys.Checked += ChkDisableFKeys_Changed;
        ChkDisableFKeys.Unchecked += ChkDisableFKeys_Changed;

        EnableTrackSettings(false);
    }

    private void EnableTrackSettings(bool enable)
    {
        SliderSpeed.IsEnabled = enable;
        ChkTwoOctaves.IsEnabled = enable;
        TxtOctaveDelay.IsEnabled = enable;
        CmbTracks.IsEnabled = enable;
        TxtSpeedVal.Opacity = enable ? 1.0 : 0.5;
        if (BtnPreviewTrack != null) BtnPreviewTrack.IsEnabled = enable;
    }

    private void ApplyTrackSettingsToUI()
    {
        if (_currentTrack == null) return;
        _isUpdatingUI = true;

        SliderSpeed.Value = _currentTrack.PlaybackSpeed;
        TxtSpeedVal.Text = $"{_currentTrack.PlaybackSpeed:0.00}x";

        ChkTwoOctaves.IsChecked = _currentTrack.RestrictToTwoOctaves;
        TxtOctaveDelay.Text = _currentTrack.OctaveChangeDelayMs.ToString();

        CmbTracks.SelectionChanged -= CmbTracks_SelectionChanged;
        CmbTracks.Items.Clear();
        CmbTracks.Items.Add(I18n.T("AllTracks"));
        if (_player != null)
        {
            var trackNames = _player.GetTrackNames();
            foreach (var name in trackNames)
            {
                CmbTracks.Items.Add(name);
            }
        }
        
        int selIndex = _currentTrack.SelectedTrackIndex + 1; // -1 becomes 0 (All Tracks)
        if (selIndex >= 0 && selIndex < CmbTracks.Items.Count)
        {
            CmbTracks.SelectedIndex = selIndex;
        }
        else
        {
            CmbTracks.SelectedIndex = 0;
        }
        CmbTracks.SelectionChanged += CmbTracks_SelectionChanged;

        _isUpdatingUI = false;
    }

    private void SaveTrackSetting()
    {
        if (_isUpdatingUI || _currentTrack == null) return;

        _currentTrack.PlaybackSpeed = SliderSpeed.Value;
        _currentTrack.RestrictToTwoOctaves = ChkTwoOctaves.IsChecked == true;
        
        if (int.TryParse(TxtOctaveDelay.Text, out int octDelay) && octDelay >= 0)
        {
            _currentTrack.OctaveChangeDelayMs = octDelay;
        }

        if (CmbTracks.SelectedIndex >= 0)
        {
            _currentTrack.SelectedTrackIndex = CmbTracks.SelectedIndex - 1; // 0 (All Tracks) becomes -1
        }

        if (_player != null)
        {
            _player.PlaybackSpeed = _currentTrack.PlaybackSpeed;
            _player.RestrictToTwoOctaves = _currentTrack.RestrictToTwoOctaves;
            _player.OctaveChangeDelayMs = _currentTrack.OctaveChangeDelayMs;
            _player.SelectedTrackIndex = _currentTrack.SelectedTrackIndex;
        }

        ConfigManager.Save();
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        string query = TxtSearch.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        BtnSearch.IsEnabled = false;
        BtnSearch.Content = I18n.T("BtnSearching");
        LstResults.ItemsSource = null;

        try
        {
            var results = await _midiService.SearchAsync(query);
            LstResults.ItemsSource = results;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during search: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSearch.IsEnabled = true;
            BtnSearch.Content = I18n.T("BtnSearch");
        }
    }

    private void BtnShowFavorites_Click(object sender, RoutedEventArgs e)
    {
        ConfigManager.Load();
        _favorites = ConfigManager.Config.Favorites;
        LstResults.ItemsSource = null;
        LstResults.ItemsSource = _favorites;
    }

    private void BtnToggleFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MidiTrackInfo track)
        {
            if (_favorites.Any(f => f.Checksum == track.Checksum))
            {
                _favorites.RemoveAll(f => f.Checksum == track.Checksum);
                btn.Foreground = Brushes.Black;
            }
            else
            {
                if (_currentTrack != null && _currentTrack.Checksum == track.Checksum)
                {
                    _favorites.Add(_currentTrack);
                }
                else
                {
                    _favorites.Add(track);
                }
                btn.Foreground = Brushes.Orange;
            }
            ConfigManager.Save();
            
            // If we are on the favorites tab, refresh
            if (LstResults.ItemsSource == _favorites)
            {
                LstResults.ItemsSource = null;
                LstResults.ItemsSource = _favorites;
            }
        }
    }

    private void ResetPreviewButtonUI()
    {
        _isPreviewPlaying = false;
        BtnPreviewTrack.Content = "▶ " + I18n.T("PreviewTrack");
        BtnPreviewTrack.Style = (Style)FindResource("AccentButton");
    }

    private void BtnPreviewTrack_Click(object sender, RoutedEventArgs e)
    {
        if (_currentTrack != null)
        {
            if (_isPreviewPlaying)
            {
                _player.Stop();
                ResetPreviewButtonUI();
                return;
            }

            _player.Stop();
            
            _isPreviewPlaying = true;
            BtnPreviewTrack.Content = I18n.T("PreviewStop");
            BtnPreviewTrack.Style = (Style)FindResource("DangerButton");
            
            _player.EnableGameInput = false;
            _player.PlayAudioPreviewOnly();
        }
    }

    private async void LstResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstResults.SelectedItem is MidiTrackInfo track)
        {
            LstResults.IsEnabled = false;
            TxtFileName.Text = I18n.T("Downloading", track.Name);
            BtnPlay.IsEnabled = false;

            try
            {
                bool isFav = _favorites.Any(f => f.Checksum == track.Checksum);
                string localPath = await _midiService.DownloadMidiAsync(track, isFav);
                _player.LoadFile(localPath);

                _currentTrack = track;
                EnableTrackSettings(true);
                ApplyTrackSettingsToUI();

                TxtFileName.Text = I18n.T("Ready", track.Name);
                BtnPlay.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtFileName.Text = I18n.T("LoadError");
            }
            finally
            {
                LstResults.IsEnabled = true;
            }
        }
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "MIDI Files (*.mid)|*.mid|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                _player.LoadFile(openFileDialog.FileName);
                
                _currentTrack = new MidiTrackInfo { Name = System.IO.Path.GetFileName(openFileDialog.FileName) };
                EnableTrackSettings(true);
                ApplyTrackSettingsToUI();

                TxtFileName.Text = I18n.T("Ready", _currentTrack.Name);

                LstResults.SelectedItem = null; // Deselect online list
                BtnPlay.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.T("ErrorLoadingMidi", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_player.IsPlaying)
        {
            _player.Stop();
            ResetUI();
            return;
        }

        BtnPlay.IsEnabled = false;
        
        // Countdown from 3
        for (int i = 3; i > 0; i--)
        {
            BtnPlay.Content = I18n.T("PlayingIn", i);
            await Task.Delay(1000);
        }
        
        string stopKeyName = KeyInterop.KeyFromVirtualKey(ConfigManager.Config.KeyBinds.StopPlayback).ToString();
        BtnPlay.Content = I18n.T("BtnStop", stopKeyName);
        BtnPlay.Style = (Style)FindResource("DangerButton");
        BtnPlay.IsEnabled = true;
        
        _player.EnableGameInput = true;
        _player.Play();
    }

    private void ResetUI()
    {
        BtnPlay.Content = "▶ " + I18n.T("Play");
        BtnPlay.Style = (Style)FindResource("PrimaryButton");
        BtnPlay.IsEnabled = true;
        
        ResetPreviewButtonUI();
        
        if (_player != null) _player.EnableGameInput = true;
    }

    private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_player != null)
        {
            _player.PlaybackSpeed = e.NewValue;
        }
        if (TxtSpeedVal != null)
        {
            TxtSpeedVal.Text = $"{e.NewValue:F2}x";
        }
        SaveTrackSetting();
    }

    private void KeyBind_KeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (sender is TextBox tb && tb.Tag is string tag)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            ushort vk = (ushort)KeyInterop.VirtualKeyFromKey(key);
            
            tb.Text = key.ToString();

            if (tag.StartsWith("Note_"))
            {
                if (int.TryParse(tag.Substring(5), out int index))
                {
                    ConfigManager.Config.KeyBinds.Notes[index] = vk;
                }
            }
            else if (tag == "HighC") ConfigManager.Config.KeyBinds.HighC = vk;
            else if (tag == "OctaveDown") ConfigManager.Config.KeyBinds.OctaveDown = vk;
            else if (tag == "OctaveUp") ConfigManager.Config.KeyBinds.OctaveUp = vk;
            else if (tag == "Stop") ConfigManager.Config.KeyBinds.StopPlayback = vk;

            ConfigManager.Save();
        }
    }

    private void ChkDisableFKeys_Changed(object sender, RoutedEventArgs e)
    {
        ConfigManager.Config.KeyBinds.DisableFunctionKeys = ChkDisableFKeys.IsChecked == true;
        ConfigManager.Save();
    }

    private void ChkTwoOctaves_Changed(object sender, RoutedEventArgs e)
    {
        SaveTrackSetting();
    }

    private void TxtOctaveDelay_TextChanged(object sender, TextChangedEventArgs e)
    {
        SaveTrackSetting();
    }

    private void CmbTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveTrackSetting();
    }
}

public partial class MainWindow {
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { }
        e.Handled = true;
    }
}
