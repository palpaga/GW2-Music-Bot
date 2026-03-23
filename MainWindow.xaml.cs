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

namespace Gw2MusicBot;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Gw2MidiPlayer _player;
    private BardsGuildService _midiService;
    private List<MidiTrackInfo> _favorites;

    public MainWindow()
    {
        InitializeComponent();
        _player = new Gw2MidiPlayer();
        _midiService = new BardsGuildService();
        
        ConfigManager.Load();
        _favorites = ConfigManager.Config.Favorites;
        
        UpdateKeyBindUI();
        
        _player.PlaybackFinished += (s, e) => Dispatcher.Invoke(() => ResetUI());
        _player.PlaybackStopped += (s, e) => Dispatcher.Invoke(() => ResetUI());
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
        
        TxtOctaveDelay.Text = binds.OctaveChangeDelayMs.ToString();
        TxtNoteDelay.Text = binds.NoteDelayMs.ToString();

        ChkDisableFKeys.Checked -= ChkDisableFKeys_Changed;
        ChkDisableFKeys.Unchecked -= ChkDisableFKeys_Changed;
        ChkDisableFKeys.IsChecked = binds.DisableFunctionKeys;
        ChkDisableFKeys.Checked += ChkDisableFKeys_Changed;
        ChkDisableFKeys.Unchecked += ChkDisableFKeys_Changed;

        ChkTwoOctaves.Checked -= ChkTwoOctaves_Changed;
        ChkTwoOctaves.Unchecked -= ChkTwoOctaves_Changed;
        ChkTwoOctaves.IsChecked = binds.RestrictToTwoOctaves;
        ChkTwoOctaves.Checked += ChkTwoOctaves_Changed;
        ChkTwoOctaves.Unchecked += ChkTwoOctaves_Changed;
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        string query = TxtSearch.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        BtnSearch.IsEnabled = false;
        BtnSearch.Content = "Searching...";
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
            BtnSearch.Content = "Search";
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
                _favorites.Add(track);
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

    private async void BtnPreviewListen_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MidiTrackInfo track)
        {
            _player.Stop();
            TxtFileName.Text = $"Downloading for preview: {track.Name}...";
            
            try
            {
                string localPath = await _midiService.DownloadMidiAsync(track);
                _player.LoadFile(localPath);
                TxtFileName.Text = $"Listening to: {track.Name}";
                
                // Force audio playback only (no game input)
                _player.EnableGameInput = false;
                _player.PlayAudioPreviewOnly();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void LstResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstResults.SelectedItem is MidiTrackInfo track)
        {
            LstResults.IsEnabled = false;
            TxtFileName.Text = $"Downloading {track.Name}...";
            BtnPlay.IsEnabled = false;

            try
            {
                string localPath = await _midiService.DownloadMidiAsync(track);
                _player.LoadFile(localPath);
                
                TxtFileName.Text = $"Ready: {track.Name}";
                BtnPlay.IsEnabled = true;
                BtnStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Download error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtFileName.Text = "Load error";
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
                TxtFileName.Text = $"Ready: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                
                LstResults.SelectedItem = null; // Deselect online list
                BtnPlay.IsEnabled = true;
                BtnStop.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading MIDI file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        BtnPlay.IsEnabled = false;
        
        // Countdown from 3
        for (int i = 3; i > 0; i--)
        {
            BtnPlay.Content = $"Playing in {i}...";
            await System.Threading.Tasks.Task.Delay(1000);
        }
        
        BtnPlay.Content = "PLAYING (Press Stop Shortcut to cancel)";
        _player.Play();
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _player.Stop();
        ResetUI();
    }

    private void ResetUI()
    {
        BtnPlay.Content = "PLAY";
        BtnPlay.IsEnabled = true;
        
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
        ConfigManager.Config.KeyBinds.RestrictToTwoOctaves = ChkTwoOctaves.IsChecked == true;
        ConfigManager.Save();
    }

    private void BtnSaveDelay_Click(object sender, RoutedEventArgs e)
    {
        bool octValid = int.TryParse(TxtOctaveDelay.Text, out int octDelay) && octDelay >= 0;
        bool noteValid = int.TryParse(TxtNoteDelay.Text, out int noteDelay) && noteDelay >= 0;

        if (octValid && noteValid)
        {
            ConfigManager.Config.KeyBinds.OctaveChangeDelayMs = octDelay;
            ConfigManager.Config.KeyBinds.NoteDelayMs = noteDelay;
            ConfigManager.Save();
            MessageBox.Show($"Delays saved:\nOctave: {octDelay}ms\nNotes: {noteDelay}ms", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Please enter valid positive numbers for the delays.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtOctaveDelay.Text = ConfigManager.Config.KeyBinds.OctaveChangeDelayMs.ToString();
            TxtNoteDelay.Text = ConfigManager.Config.KeyBinds.NoteDelayMs.ToString();
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch { }
    }

    protected override void OnClosed(EventArgs e)
    {
        _player.Dispose();
        base.OnClosed(e);
    }
}