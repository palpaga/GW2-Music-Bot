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
        public bool IsPaused { get; private set; }
        public bool EnableGameInput { get; set; } = true;

        public double PlaybackSpeed { get; set; } = 0.80;
        public bool RestrictToTwoOctaves { get; set; } = false;
        public int OctaveChangeDelayMs { get; set; } = 0;
        public int SelectedTrackIndex { get; set; } = -1;

        public event EventHandler? PlaybackFinished;
        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackStopped;
        public event EventHandler? PausedStateChanged;

        public Gw2MidiPlayer()
        {
            try
            {
                _outputDevice = OutputDevice.GetByIndex(0);
            }
            catch { }
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

                    var tempMidi = new MidiFile
                    {
                        TimeDivision = _midiFile.TimeDivision
                    };

                    if (_midiFile.GetTrackChunks().Count() > 1 && SelectedTrackIndex > 0)
                    {
                        tempMidi.Chunks.Add(_midiFile.GetTrackChunks().First().Clone());
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

                token.Register(() =>
                {
                    audioPlayback.Stop();
                    audioPlayback.Dispose();
                });
            }
        }

        private void PlayMacroLoop(CancellationToken token)
        {
            try
            {
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
                        NoteNumber = (int)n.NoteNumber
                    })
                    .OrderBy(n => n.TimeMs)
                    .ToList();

                if (rawNotes.Count == 0)
                {
                    FinishPlayback();
                    return;
                }

                int transposeOffset = FindBestTransposeOffset(rawNotes.Select(n => n.NoteNumber));
                var noteEvents = new List<Gw2NoteEvent>();

                foreach (var rawNote in rawNotes)
                {
                    var gw2Note = NoteMapper.GetGw2NoteFromMidi(rawNote.NoteNumber + transposeOffset, RestrictToTwoOctaves);
                    if (gw2Note == null) continue;

                    noteEvents.Add(new Gw2NoteEvent
                    {
                        TimeMs = rawNote.TimeMs,
                        NoteNumber = rawNote.NoteNumber,
                        KeyToPress = gw2Note.KeyToPress,
                        Octave = gw2Note.Octave
                    });
                }

                var groupedEvents = GroupChordEvents(noteEvents);
                var sw = new Stopwatch();
                bool isFirstFocus = true;
                bool isCurrentlyFocused = !EnableGameInput || InputSimulator.IsGw2Focused();
                if (isCurrentlyFocused)
                {
                    if (EnableGameInput)
                    {
                        ResetInstrumentOctave();
                        isFirstFocus = false;
                    }

                    sw.Start();
                }
                else
                {
                    SetPaused(true);
                }

                _ = Task.Run(async () =>
                {
                    while (_cts != null && !_cts.IsCancellationRequested)
                    {
                        bool hasFocus = !EnableGameInput || InputSimulator.IsGw2Focused();
                        if (hasFocus != isCurrentlyFocused)
                        {
                            isCurrentlyFocused = hasFocus;
                            SetPaused(!hasFocus);

                            if (hasFocus)
                            {
                                if (isFirstFocus && EnableGameInput)
                                {
                                    ResetInstrumentOctave();
                                    isFirstFocus = false;
                                }

                                sw.Start();
                            }
                            else
                            {
                                sw.Stop();
                            }
                        }

                        await Task.Delay(100);
                    }
                }, token);

                foreach (var group in groupedEvents)
                {
                    if (token.IsCancellationRequested) break;

                    long targetTime = (long)(group[0].TimeMs / PlaybackSpeed);

                    while (true)
                    {
                        if (token.IsCancellationRequested) break;

                        if ((InputSimulator.GetAsyncKeyState(ConfigManager.Config.KeyBinds.StopPlayback) & 0x8000) != 0)
                        {
                            Stop();
                            break;
                        }

                        if (isCurrentlyFocused && sw.ElapsedMilliseconds >= targetTime)
                        {
                            break;
                        }

                        Thread.Sleep(1);
                    }

                    if (token.IsCancellationRequested) break;
                    if (!EnableGameInput) continue;

                    var octaveGroups = group.GroupBy(n => n.Octave).OrderBy(g => g.Key);

                    foreach (var octGroup in octaveGroups)
                    {
                        int targetOctave = octGroup.Key;

                        while (_currentOctave != targetOctave)
                        {
                            if (token.IsCancellationRequested) break;

                            if ((InputSimulator.GetAsyncKeyState(ConfigManager.Config.KeyBinds.StopPlayback) & 0x8000) != 0 ||
                                (EnableGameInput && !InputSimulator.IsGw2Focused()))
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

                        if (token.IsCancellationRequested || !IsPlaying) break;

                        foreach (var note in octGroup)
                        {
                            InputSimulator.PressKey(note.KeyToPress);
                        }
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    FinishPlayback();
                }
            }
            catch (Exception)
            {
                // Ignore cancellation errors.
            }
        }

        private int FindBestTransposeOffset(IEnumerable<int> noteNumbers)
        {
            var notes = noteNumbers.ToList();
            int bestOffset = 0;
            int minPenalty = int.MaxValue;
            int maxAllowedOctave = RestrictToTwoOctaves ? 2 : 3;

            for (int offset = -48; offset <= 48; offset++)
            {
                int penalty = 0;

                foreach (int noteNumber in notes)
                {
                    int transposedNote = noteNumber + offset;
                    int noteInOctave = ((transposedNote % 12) + 12) % 12;

                    if (ConfigManager.Config.KeyBinds.DisableFunctionKeys &&
                        (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || noteInOctave == 8 || noteInOctave == 10))
                    {
                        penalty += 1000;
                    }

                    int gw2Octave = (transposedNote / 12) - 1 - 2;
                    if (gw2Octave < 1 || gw2Octave > maxAllowedOctave)
                    {
                        penalty += 1;
                    }
                }

                if (penalty < minPenalty || (penalty == minPenalty && Math.Abs(offset) < Math.Abs(bestOffset)))
                {
                    minPenalty = penalty;
                    bestOffset = offset;
                }
            }

            Debug.WriteLine($"Auto-transposing by {bestOffset} semitones. Penalty score: {minPenalty}");
            return bestOffset;
        }

        private static List<List<Gw2NoteEvent>> GroupChordEvents(List<Gw2NoteEvent> noteEvents)
        {
            var groupedEvents = new List<List<Gw2NoteEvent>>();
            List<Gw2NoteEvent>? currentGroup = null;

            foreach (var ev in noteEvents)
            {
                if (currentGroup == null || ev.TimeMs - currentGroup[0].TimeMs > 15)
                {
                    currentGroup = new List<Gw2NoteEvent> { ev };
                    groupedEvents.Add(currentGroup);
                }
                else
                {
                    currentGroup.Add(ev);
                }
            }

            return groupedEvents;
        }

        private void ResetInstrumentOctave()
        {
            for (int i = 0; i < 6; i++)
            {
                InputSimulator.PressKey(ConfigManager.Config.KeyBinds.OctaveDown);
                Thread.Sleep(30);
            }

            _currentOctave = 1;
        }

        private void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;

            IsPaused = paused;
            PausedStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void FinishPlayback()
        {
            _cts?.Dispose();
            _cts = null;
            SetPaused(false);
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
                SetPaused(false);
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
