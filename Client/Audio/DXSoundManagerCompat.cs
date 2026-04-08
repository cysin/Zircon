// Cross-platform stub for DXSoundManager.
// On Windows, the real DXSoundManager.cs (using DirectSound) is used.
// On other platforms, this stub provides the same static API surface.
#if !WINDOWS
using Library;

namespace Client.Envir
{
    public static class DXSoundManager
    {
        public static void Create() { }
        public static void Unload() { }
        public static void Play(SoundIndex index) { }
        public static void Stop(SoundIndex index) { }
        public static void StopAllSounds() { }
        public static void AdjustVolume(SoundIndex index) { }
        public static void AdjustVolume() { }
        public static void UpdateFlags()
        {
            var sounds = new System.Collections.Generic.List<Client.Rendering.ISoundCacheItem>(
                Client.Rendering.RenderingPipelineManager.GetRegisteredSoundCaches());
            for (int i = sounds.Count - 1; i >= 0; i--)
                sounds[i].UpdateFlags();
        }
        public static int GetVolume(SoundType type) => 0;

        public static int SystemVolume { get; set; } = 50;
        public static int MusicVolume { get; set; } = 50;
        public static int PlayerVolume { get; set; } = 50;
        public static int MonsterVolume { get; set; } = 50;
        public static int MagicVolume { get; set; } = 50;
        public static int SpellEffectVolume { get; set; } = 50;
    }
}
#endif
