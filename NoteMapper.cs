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
        public static Gw2Note GetGw2NoteFromMidi(int midiNoteNumber)
        {
            // MIDI Note 60 is C4 (Middle C)
            int noteInOctave = midiNoteNumber % 12;
            int octave = (midiNoteNumber / 12) - 1;

            ushort key = 0;

            // Mapping for the Piano (and other chromatic instruments)
            // C = 1, C# = F1, D = 2, D# = F2, E = 3, F = 4, F# = F3, G = 5, G# = F4, A = 6, A# = F5, B = 7
            switch (noteInOctave)
            {
                case 0:  key = InputSimulator.VK_1; break; // C
                case 1:  key = InputSimulator.VK_F1; break; // C#
                case 2:  key = InputSimulator.VK_2; break; // D
                case 3:  key = InputSimulator.VK_F2; break; // D#
                case 4:  key = InputSimulator.VK_3; break; // E
                case 5:  key = InputSimulator.VK_4; break; // F
                case 6:  key = InputSimulator.VK_F3; break; // F#
                case 7:  key = InputSimulator.VK_5; break; // G
                case 8:  key = InputSimulator.VK_F4; break; // G#
                case 9:  key = InputSimulator.VK_6; break; // A
                case 10: key = InputSimulator.VK_F5; break; // A#
                case 11: key = InputSimulator.VK_7; break; // B
            }

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

            if (octave < 1) octave = 1;
            if (octave > 3) octave = 3;

            return new Gw2Note { KeyToPress = key, Octave = octave };
        }
    }
}
