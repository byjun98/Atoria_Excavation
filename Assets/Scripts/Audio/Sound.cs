using UnityEngine;
using UnityEngine.Audio;

namespace Rebuild.Audio
{
    /// <summary>Single named clip + AudioSource bundle.
    /// Wired in the Inspector; Awake on <see cref="AudioManager"/>
    /// dynamically creates the AudioSource and copies the per-clip volume/pitch/loop into it.</summary>
    [System.Serializable]
    public sealed class Sound
    {
        public string name;
        public AudioClip clip;
        public AudioType audioType = AudioType.SFX;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;

        [HideInInspector] public AudioSource source;
    }

    /// <summary>broad routing buckets so a Settings slider can mute/dim each
    /// independently (Music vs gameplay SFX vs UI clicks).</summary>
    public enum AudioType
    {
        Music = 0,
        SFX   = 1,
        UI    = 2,
    }
}
