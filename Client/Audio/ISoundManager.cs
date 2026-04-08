using Library;

namespace Client.Audio
{
    public interface ISoundManager
    {
        void Initialize(nint windowHandle);
        void Play(SoundIndex index);
        void Stop(SoundIndex index);
        void StopAllSounds();
        void AdjustVolume(SoundIndex index);
        int GetVolume(SoundType type);
        void Unload();
    }
}
