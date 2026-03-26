using System.Collections.Generic;

namespace Gw2MusicBot
{
    public static class I18n
    {
        public static string CurrentLanguage => ConfigManager.Config.Language;

        private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            {
                "en", new Dictionary<string, string>
                {
                    {"AppTitle", "GW2 Music Bot - API Edition"},
                    {"SearchPlaceholder", "Search..."},
                    {"BtnSearch", "Search"},
                    {"BtnSearching", "Searching..."},
                    {"BtnFavorites", "★ Favorites"},
                    {"BtnLocal", "Local..."},
                    {"Preview", "▶ Preview"},
                    {"Score", "Score"},
                    {"PlayerSettings", "Player Settings"},
                    {"MidiTrack", "MIDI Track:"},
                    {"PreviewTrack", "▶ Preview Track"},
                    {"RestrictOctaves", "Restrict to 2 Octaves (Faster playback)"},
                    {"RestrictOctavesDesc", "Reduces octave switching. Helps prevent the game from dropping notes and desyncing into chord modes on very fast songs."},
                    {"OctaveDelay", "Delay before an octave change (ms) :"},
                    {"PlaybackSpeed", "Playback Speed:"},
                    {"KeyMapping", "Key Mapping"},
                    {"DisableFKeys", "Disable Sharp/Flat notes (Ignore F1-F5)"},
                    {"DisableFKeysDesc", "Checked by default. Function keys are only available on the Piano. Other instruments cannot play these notes."},
                    {"NoMusicSelected", "No music selected"},
                    {"Play", "PLAY"},
                    {"PlayingIn", "Playing in {0}..."},
                    {"Playing", "PLAYING (Press Stop Shortcut to cancel)"},
                    {"AllTracks", "All Tracks (Merged)"},
                    {"Downloading", "Downloading {0}..."},
                    {"DownloadingPreview", "Downloading for preview: {0}..."},
                    {"ListeningTo", "Listening to: {0}"},
                    {"Ready", "Ready: {0}"},
                    {"LoadError", "Load error"},
                    {"ErrorLoadingMidi", "Error loading MIDI file:\n{0}"},
                    {"UpdateAvailable", "Update available: {0} ! Click here to download."},
                    {"PreviewStop", "⏹ Stop Preview"},
                    {"BtnStop", "⏹ STOP ({0})"},
                    {"DisclaimerTitle", "⚠️ DISCLAIMER & ARENANET POLICY"},
                    {"DisclaimerText", "While using macros specifically for playing musical instruments has historically been tolerated by ArenaNet, "},
                    {"DisclaimerBold", "you use this tool entirely at your own risk."},
                    {"DisclaimerEnd", " It is a third-party macro program. We hold no responsibility for any actions taken against your account."},
                    {"DisclaimerLink", "Read official ArenaNet Macro Policy"},
                    {"DisclaimerLimitations", "Limitations: The bot presses keys blindly with no game feedback. Input lag can cause missed keys/octaves. Adjust speed, track, or octave delay if the sound degrades."}
                }
            },
            {
                "fr", new Dictionary<string, string>
                {
                    {"AppTitle", "GW2 Music Bot - Édition API"},
                    {"SearchPlaceholder", "Rechercher..."},
                    {"BtnSearch", "Chercher"},
                    {"BtnSearching", "Recherche..."},
                    {"BtnFavorites", "★ Favoris"},
                    {"BtnLocal", "Local..."},
                    {"Preview", "▶ Aperçu"},
                    {"Score", "Note"},
                    {"PlayerSettings", "Paramètres du Lecteur"},
                    {"MidiTrack", "Piste MIDI :"},
                    {"PreviewTrack", "▶ Aperçu Piste"},
                    {"RestrictOctaves", "Restreindre à 2 Octaves (Lecture plus rapide)"},
                    {"RestrictOctavesDesc", "Réduit les changements d'octaves. Évite que le jeu rate des notes et se désynchronise sur les morceaux très rapides."},
                    {"OctaveDelay", "Délai avant un changement d'octave (ms) :"},
                    {"PlaybackSpeed", "Vitesse de Lecture :"},
                    {"KeyMapping", "Raccourcis Clavier"},
                    {"DisableFKeys", "Désactiver les dièses/bémols (Ignorer F1-F5)"},
                    {"DisableFKeysDesc", "Coché par défaut. Les touches F sont uniquement sur le Piano. Les autres instruments ne peuvent pas jouer ces notes."},
                    {"NoMusicSelected", "Aucune musique sélectionnée"},
                    {"Play", "JOUER"},
                    {"PlayingIn", "Lecture dans {0}..."},
                    {"Playing", "EN LECTURE (Appuyez sur Stop pour annuler)"},
                    {"AllTracks", "Toutes les Pistes (Fusionnées)"},
                    {"Downloading", "Téléchargement de {0}..."},
                    {"DownloadingPreview", "Téléchargement pour l'aperçu : {0}..."},
                    {"ListeningTo", "Écoute de : {0}"},
                    {"Ready", "Prêt : {0}"},
                    {"LoadError", "Erreur de chargement"},
                    {"ErrorLoadingMidi", "Erreur lors du chargement du fichier MIDI :\n{0}"},
                    {"UpdateAvailable", "Mise à jour disponible : {0} ! Cliquez ici pour télécharger."},
                    {"PreviewStop", "⏹ Arrêter l'aperçu"},
                    {"BtnStop", "⏹ STOP ({0})"},
                    {"DisclaimerTitle", "⚠️ AVERTISSEMENTS & POLITIQUE ARENANET"},
                    {"DisclaimerText", "Bien que l'utilisation de macros spécifiques pour jouer des instruments ait été historiquement tolérée par ArenaNet, "},
                    {"DisclaimerBold", "vous utilisez cet outil à vos risques et périls."},
                    {"DisclaimerEnd", " Il s'agit d'un programme tiers. Nous ne sommes pas responsables des actions prises contre votre compte."},
                    {"DisclaimerLink", "Lire la politique officielle d'ArenaNet sur les Macros"},
                    {"DisclaimerLimitations", "Limitations : Le bot appuie à l'aveugle sans retour du jeu. Le lag (latence) peut rater des notes/octaves. Ajustez la vitesse, la piste ou le délai d'octave si le son se dégrade."}
                }
            }
        };

        public static string T(string key, params object[] args)
        {
            string lang = _translations.ContainsKey(CurrentLanguage) ? CurrentLanguage : "en";
            
            if (_translations[lang].TryGetValue(key, out string? value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
            
            return key; // Fallback to key if translation missing
        }
    }
}