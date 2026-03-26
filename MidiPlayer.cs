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

        public bool IsPlaying => _cts != null && !_cts.IsCancellationRequested;
        public bool EnableGameInput { get; set; } = true;

        public double PlaybackSpeed { get; set; } = 0.80;
        public bool RestrictToTwoOctaves { get; set; } = false;
        public int OctaveChangeDelayMs { get; set; } = 15;
        public int SelectedTrackIndex { get; set; } = -1;


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

        public List<string> GetTrackNames()
        {
            var names = new List<string>();
            if (_midiFile == null) return names;

            int i = 1;
            foreach (var chunk in _midiFile.GetTrackChunks())
            {
                string trackName = $"Track {i}";
                var sequenceNameEvent = chunk.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault();
                if (sequenceNameEvent != null && !string.IsNullOrWhiteSpace(sequenceNameEvent.Text))
                {
                    trackName += $" ({sequenceNameEvent.Text})";
                }
                
                var noteCount = chunk.GetNotes().Count;
                var uniquePrograms = chunk.Events.OfType<ProgramChangeEvent>().Select(p => p.ProgramNumber).Distinct().Count();
                
                trackName += $" - {noteCount} notes";
                if (uniquePrograms > 0) trackName += $", {uniquePrograms} inst";
                
                names.Add(trackName);
                i++;
            }
            return names;
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

            if (_outputDevice != null)
            {
                Melanchall.DryWetMidi.Multimedia.Playback audioPlayback;
                if (SelectedTrackIndex >= 0 && SelectedTrackIndex < _midiFile.GetTrackChunks().Count())
                {
                    var trackChunk = _midiFile.GetTrackChunks().ElementAt(SelectedTrackIndex);
                    
                    var tempMidi = new MidiFile();
                    tempMidi.TimeDivision = _midiFile.TimeDivision; // Essential for timing
                    
                    if (_midiFile.GetTrackChunks().Count() > 1 && SelectedTrackIndex > 0) 
                    {
                        tempMidi.Chunks.Add(_midiFile.GetTrackChunks().First().Clone()); // Copy tempo/meta chunk safely
                    }
                    tempMidi.Chunks.Add(trackChunk.Clone());
                    
                    audioPlayback = tempMidi.GetPlayback(_outputDevice);
                }
                else
                {
                    audioPlayback = _midiFile.GetPlayback(_outputDevice);
                }
                
                audioPlayback.Speed = PlaybackSpeed;
                audioPlayback.Start();

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
                IEnumerable<Melanchall.DryWetMidi.Interaction.Note> sourceNotes = _midiFile.GetNotes();
                if (SelectedTrackIndex >= 0 && SelectedTrackIndex < _midiFile.GetTrackChunks().Count())
                {
                    sourceNotes = _midiFile.GetTrackChunks().ElementAt(SelectedTrackIndex).GetNotes();
                }

                var rawNotes = sourceNotes
                    .Select(n => new
                    {
                        TimeMs = (long)n.TimeAs<MetricTimeSpan>(tempoMap).TotalMilliseconds,
                        NoteNumber = n.NoteNumber
                    })
                    .OrderBy(n => n.TimeMs)
                    .ToList();

                if (rawNotes.Count == 0) return;

                // Auto-transpose the entire track to fit within the playable range and minimize accidentals if needed
                int transposeOffset = 0;
                int bestOffset = 0;
                int minPenalty = int.MaxValue;

                int maxAllowedOctave = RestrictToTwoOctaves ? 2 : 3;

                // Test transpositions from -48 to +48 semitones (4 octaves up/down)
                for (int i = -48; i <= 48; i++)
                {
                    int penalty = 0;
                    foreach (var n in rawNotes)
                    {
                        int transposedNote = n.NoteNumber + i;
                        int noteInOctave = ((transposedNote % 12) + 12) % 12;
                        
                        // Penalty for accidentals if function keys are disabled
                        if (ConfigManager.Config.KeyBinds.DisableFunctionKeys)
                        {
                            if (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || noteInOctave == 8 || noteInOctave == 10)
                            {
                                penalty += 1000; // Heavy penalty: note will be completely dropped (hole)
                            }
                        }

                        // Penalty for out of range notes
                        int gw2Octave = (transposedNote / 12) - 1 - 2;
                        
                        if (gw2Octave < 1 || gw2Octave > maxAllowedOctave)
                        {
                            penalty += 1; // Minor penalty: note will be clamped to nearest octave, breaking melody shape
                        }
                    }
                    
                    // Prefer transpositions closer to original pitch if penalties are equal
                    if (penalty < minPenalty || (penalty == minPenalty && Math.Abs(i) < Math.Abs(bestOffset)))
                    {
                        minPenalty = penalty;
                        bestOffset = i;
                    }
                }
                
                transposeOffset = bestOffset;
                System.Diagnostics.Debug.WriteLine($"Auto-transposing by {transposeOffset} semitones. Penalty score: {minPenalty}");

                var noteEvents = new List<Gw2NoteEvent>();

                // 2. Extract all notes allowing polyphony on the same octave
                foreach (var rn in rawNotes)
                {
                    var gw2n = NoteMapper.GetGw2NoteFromMidi(rn.NoteNumber + transposeOffset, RestrictToTwoOctaves);
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
                    // "Blind Mode" initial reset:
                    // Spam 6 times to ensure we drop down from page 5 (Major chords) to 1
                    for (int i = 0; i < 6; i++)
                    {
                        InputSimulator.PressKey(ConfigManager.Config.KeyBinds.OctaveDown);
                        Thread.Sleep(10);
                    }
                    _currentOctave = 1; 
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

                        // Blind navigation to the target octave
                        // We trust the game state: press keys and update _currentOctave accordingly.
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

                            if (OctaveChangeDelayMs > 0)
                            {
                                Thread.Sleep(OctaveChangeDelayMs);
                            }
                        }

                        // 3. Play all notes of the chord on this octave at the same time
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









