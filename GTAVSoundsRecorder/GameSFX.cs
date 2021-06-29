using Rage;
using System;
using N = Rage.Native.NativeFunction;

namespace GTAVSoundsRecorder
{
    public class GameSFX : IDisposable
    {
        public string Dictionary { get; }
        public string Name { get; }
        public int SoundID { get; }

        public bool IsPlaying => !N.Natives.HAS_SOUND_FINISHED<bool>(SoundID);

        public GameSFX(string dictionary, string name)
        {
            Dictionary = dictionary;
            Name = name;
            SoundID = N.Natives.GET_SOUND_ID<int>();
        }

        public void Play()
        {
            if (IsPlaying) Stop();
            N.Natives.PLAY_SOUND_FRONTEND(SoundID, Name, Dictionary, false);
        }
        public void Play(Vector3 position)
        {
            if (IsPlaying) Stop();
            N.Natives.PLAY_SOUND_FROM_COORD(SoundID, Name, position.X, position.Y, position.Z, Dictionary, 0, 0, 0);
        }
        public void Play(Entity entity)
        {
            if (IsPlaying) Stop();
            N.Natives.PLAY_SOUND_FROM_ENTITY(SoundID, Name, entity, Dictionary, 0, 0);
        }

        public void Stop() => N.Natives.STOP_SOUND(SoundID);
        public void Dispose()
        {
            Stop();
            N.Natives.RELEASE_SOUND_ID(SoundID);
        }
    }
}
