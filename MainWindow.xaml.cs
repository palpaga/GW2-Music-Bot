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
        _favorites = FavoritesManager.LoadFavorites();
        
        _player.PlaybackFinished += (s, e) => Dispatcher.Invoke(() => ResetUI());
        _player.PlaybackStopped += (s, e) => Dispatcher.Invoke(() => ResetUI());
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
        _favorites = FavoritesManager.LoadFavorites();
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
            FavoritesManager.SaveFavorites(_favorites);
            
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
                BtnPause.IsEnabled = true;
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
                BtnPause.IsEnabled = true;
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
        // Add a small delay so the user has time to click back to the game window
        BtnPlay.Content = "Playing in 2s...";
        await System.Threading.Tasks.Task.Delay(2000);
        BtnPlay.Content = "Playing";
        _player.Play();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        _player.Pause();
        BtnPlay.Content = "Play (Wait 2s)";
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _player.Stop();
        ResetUI();
    }

    private void ResetUI()
    {
        BtnPlay.Content = "Play (Wait 2s)";
        // Restore the checkbox setting
        if (_player != null) _player.EnableGameInput = ChkGame.IsChecked == true;
    }

    private void ChkGame_Changed(object sender, RoutedEventArgs e)
    {
        if (_player != null) _player.EnableGameInput = ChkGame.IsChecked == true;
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