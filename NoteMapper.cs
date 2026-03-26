using System;

namespace Gw2MusicBot
{
    public class Gw2Note
    {
        public ushort KeyToPress { get; set; }
        public int Octave { get; set; } // 3 = Low, 4 = Mid, 5 = High
    }

    public static class NoteMapper
    {
        public static Gw2Note? GetGw2NoteFromMidi(int midiNoteNumber, bool restrictToTwoOctaves = false)
        {
            // MIDI Note 60 is C4 (Middle C)
            int noteInOctave = ((midiNoteNumber % 12) + 12) % 12; // ensure positive modulo
            int octave = (midiNoteNumber / 12) - 1;

            // Mapping for the Piano (and other chromatic instruments)
            if (ConfigManager.Config.KeyBinds.DisableFunctionKeys)
            {
                // We've already auto-transposed to minimize accidentals,
                // but if there are still passing accidentals, we just skip them
                // because playing a flattened note often sounds worse than a rest.
                if (noteInOctave == 1 || noteInOctave == 3 || noteInOctave == 6 || noteInOctave == 8 || noteInOctave == 10)
                {
                    return null; // Skip unplayable accidental note
                }
            }
            
            ushort key = ConfigManager.Config.KeyBinds.Notes[noteInOctave];

            // Adjust octave to correspond to the 5 modes of the GW2 Piano
            // Mode 2: Minor Chords (represented as octave 2)
            // Mode 3: Low Octave (represented as octave 3)
            // Mode 4: Middle Octave (represented as octave 4)
            // Mode 5: High Octave (represented as octave 5)
            // Mode 6: Major Chords (represented as octave 6)

            // For now, we translate normal MIDI notes to the 3 main octaves (1, 2, 3)
            // Octave 1 = Low, Octave 2 = Middle, Octave 3 = High.
            // The -2 generally brings MIDI octave 4 to the middle.
            octave -= 2;

            var gw2Note = new Gw2Note { KeyToPress = key, Octave = octave };

            if (gw2Note.Octave < 1) gw2Note.Octave = 1;
            
            if (restrictToTwoOctaves)
            {
                if (gw2Note.Octave > 2) gw2Note.Octave = 2;
            }
            else
            {
                if (gw2Note.Octave > 3) gw2Note.Octave = 3;
            }

            return gw2Note;
        }
    }
}

