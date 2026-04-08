using System;
using Client.Envir;
using Client.Platform.SDL3;
using Library;

namespace Client.Audio
{
    /// <summary>
    /// SDL3-based sound manager. Currently a stub implementation that initialises SDL3 audio
    /// but does not play sounds. Full playback will require SDL3_mixer or a similar library
    /// for format decoding and mixing.
    /// </summary>
    public class SDL3SoundManager : ISoundManager
    {
        private bool _initialised;

        public void Initialize(nint windowHandle)
        {
            if (_initialised) return;

            int result = SDL3Native.SDL_InitSubSystem(SDL3Native.SDL_INIT_AUDIO);
            if (result < 0)
            {
                System.Diagnostics.Debug.WriteLine($"[SDL3SoundManager] SDL audio init failed: {SDL3Native.GetError()}");
                return;
            }

            _initialised = true;
            System.Diagnostics.Debug.WriteLine("[SDL3SoundManager] SDL3 audio subsystem initialised (playback not yet implemented).");
        }

        public void Play(SoundIndex index)
        {
            // TODO: Implement with SDL3_mixer or raw SDL3 audio queues.
        }

        public void Stop(SoundIndex index)
        {
            // TODO: Implement with SDL3_mixer.
        }

        public void StopAllSounds()
        {
            // TODO: Implement with SDL3_mixer.
        }

        public void AdjustVolume(SoundIndex index)
        {
            // TODO: Implement with SDL3_mixer.
        }

        public int GetVolume(SoundType type)
        {
            return 0;
        }

        public void Unload()
        {
            if (!_initialised) return;

            // SDL_QuitSubSystem is not exposed via our minimal bindings.
            // The audio subsystem will be cleaned up when SDL_Quit is called.
            _initialised = false;
            System.Diagnostics.Debug.WriteLine("[SDL3SoundManager] SDL3 audio unloaded.");
        }
    }
}
