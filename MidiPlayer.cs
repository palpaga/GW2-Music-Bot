using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2MusicBot
{
    public class Gw2NoteEvent
    {
        public long TimeMs { get; set; }
        public int NoteNumber { get; set; }
        public ushort KeyToPress { get; set; }
        public int Octave { get; set; }
    }

    public class Gw2MidiPlayer : IDisposable
    {
        private MidiFile? _midiFile;
        private CancellationTokenSource? _cts;
        private OutputDevice? _outputDevice;
        private int _currentOctave = 2; // GW2 Piano has 3 octaves. We start in the middle (2).


        public bool EnableGameInput { get; set; } = true;

        public double PlaybackSpeed { get; set; } = 1.0;


        public event EventHandler? PlaybackFinished;
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackStopped;

        public Gw2MidiPlayer()
        {
            try {
                // Initialize default audio device
                _outputDevice = OutputDevice.GetByIndex(0);
            } catch { }
        }

        public void LoadFile(string filePath)
        {
            Stop();
            _midiFile = MidiFile.Read(filePath);
        }

        public void Play()
        {
            if (_midiFile == null) return;
            Stop();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            PlaybackStarted?.Invoke(this, EventArgs.Empty);

            Task.Run(() => PlayMacroLoop(token), token);
        }

        public void PlayAudioPreviewOnly()
        {
            if (_midiFile == null) return;
            Stop();

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            
            // For preview listening only, we use the standard audio player
            if (_outputDevice != null)
            {
                var audioPlayback = _midiFile.GetPlayback(_outputDevice);
                audioPlayback.Speed = PlaybackSpeed;
                audioPlayback.Start();
                
                // Cleanup handled by Cancel
                token.Register(() => {
                    audioPlayback.Stop();
                    audioPlayback.Dispose();
                });
            }
        }

        private async Task PlayMacroLoop(CancellationToken token)
        {
            try
            {
                // 1. Parse the entire music to create a "Macro" (list of planned actions)
                var tempoMap = _midiFile!.GetTempoMap();
                var rawNotes = _midiFile.GetNotes()
                    .Select(n => new
                    {
                        TimeMs = (long)n.TimeAs<MetricTimeSpan>(tempoMap).TotalMilliseconds,
                        NoteNumber = n.NoteNumber
                    })
                    .OrderBy(n => n.TimeMs)
                    .ToList();

                if (rawNotes.Count == 0) return;

                int transposeOffset = 0;
                if (ConfigManager.Config.KeyBinds.DisableFunctionKeys)
                {
                    // Auto-transpose the entire track to minimize sharps/flats (diatonic scale adaptation)
                    int bestOffset = 0;
                    int minAccidentals = int.MaxValue;

                    for (int i = -5; i <= 6; i++)
                    {
                        int accidentals = 0;
                        foreach (var n in rawNotes)
                        {
                            int noteInOctave = ((n.NoteNumber + i) % 12 + 12) % 12; // ensure positive modulo
                            if (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || noteInOctave == 8 || noteInOctave == 10)
                            {
                                accidentals++;
                            }
                        }
                        
                        // We also want to prefer transpositions that stay closer to the original pitch
                        // So if we have equal accidentals, we pick the one closer to 0
                        if (accidentals < minAccidentals || (accidentals == minAccidentals && Math.Abs(i) < Math.Abs(bestOffset)))
                        {
                            minAccidentals = accidentals;
                            bestOffset = i;
                        }
                    }
                    transposeOffset = bestOffset;
                    System.Diagnostics.Debug.WriteLine($"Auto-transposing by {transposeOffset} semitones to minimize accidentals.");
                }

                var noteEvents = new List<Gw2NoteEvent>();

                // 2. Extract all notes allowing polyphony on the same octave
                foreach (var rn in rawNotes)
                {
                    var gw2n = NoteMapper.GetGw2NoteFromMidi(rn.NoteNumber + transposeOffset);
                    if (gw2n == null) continue; // Skip if it still falls on an unplayable accidental

                    noteEvents.Add(new Gw2NoteEvent
                    {
                        TimeMs = rn.TimeMs,
                        NoteNumber = rn.NoteNumber,
                        KeyToPress = gw2n.KeyToPress,
                        Octave = gw2n.Octave
                    });
                }

                var sw = new Stopwatch();

                if (EnableGameInput)
                {
                    // The user must ensure the instrument is on the middle octave (default on opening)
                    _currentOctave = 2; 
                }

                // Group notes that are very close to form real chords
                // 15ms tolerance: If notes follow each other within 15ms, they are part of the same chord.
                var groupedEvents = new List<List<Gw2NoteEvent>>();
                List<Gw2NoteEvent>? currentGroup = null;

                foreach (var ev in noteEvents)
                {
                    if (currentGroup == null)
                    {
                        currentGroup = new List<Gw2NoteEvent> { ev };
                        groupedEvents.Add(currentGroup);
                    }
                    else
                    {
                        if (ev.TimeMs - currentGroup[0].TimeMs <= 15)
                        {
                            currentGroup.Add(ev);
                        }
                        else
                        {
                            currentGroup = new List<Gw2NoteEvent> { ev };
                            groupedEvents.Add(currentGroup);
                        }
                    }
                }

                sw.Start();

                foreach (var group in groupedEvents)
                {
                    if (token.IsCancellationRequested) break;

                    long targetTime = (long)(group[0].TimeMs / PlaybackSpeed);

                    // Active wait until it's time to play the chord
                    while (sw.ElapsedMilliseconds < targetTime)
                    {
                        if (token.IsCancellationRequested) break;
                        
                        // Check for the user's global stop shortcut (e.g. Escape)
                        if ((InputSimulator.GetAsyncKeyState(ConfigManager.Config.KeyBinds.StopPlayback) & 0x8000) != 0)
                        {
                            Stop();
                            break;
                        }
                        
                        Thread.Sleep(1);
                    }

                    if (token.IsCancellationRequested) break;
                    if (!EnableGameInput) continue;

                    // Separate the notes of this chord by octave
                    var octaveGroups = group.GroupBy(n => n.Octave).OrderBy(g => g.Key);

                    foreach (var octGroup in octaveGroups)
                    {
                        int targetOctave = octGroup.Key;

                        // 1. Sequential and safe octave change
                        while (_currentOctave != targetOctave)
                        {
                            if (token.IsCancellationRequested) break;
                            
                            if ((InputSimulator.GetAsyncKeyState(ConfigManager.Config.KeyBinds.StopPlayback) & 0x8000) != 0)
                            {
                                Stop();
                                break;
                            }

                            if (targetOctave > _currentOctave)
                            {
                                InputSimulator.PressKey(ConfigManager.Config.KeyBinds.OctaveUp);
                                _currentOctave++;
                            }
                            else
                            {
                                InputSimulator.PressKey(ConfigManager.Config.KeyBinds.OctaveDown);
                                _currentOctave--;
                            }
                            
                            // We leave strict time for the GW2 bar to change (50ms)
                            Thread.Sleep(10);
                        }

                        // 2. Play all notes of the chord on this octave at the same time
                        foreach (var note in octGroup)
                        {
                            InputSimulator.PressKey(note.KeyToPress);
                        }
                    }
                }

                PlaybackFinished?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
                // Ignore cancellation errors
            }
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Stop();
            _outputDevice?.Dispose();
        }
    }
}