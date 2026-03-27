using UnityEngine;

namespace Rebuild.Audio
{
    /// <summary>dedicated <see cref="AudioSource"/> per high-frequency event so a
    /// pickaxe swing / rock break / ore pop / beam fire doesn't drop the previous one on retrigger.
    /// Each bucket picks a random clip from its pool + a tiny pitch jitter so repetition stays musical.</summary>
    public sealed class OverlappingSounds : MonoBehaviour
    {
        public static OverlappingSounds Instance { get; private set; }

        [Header("Dedicated AudioSources (one per bucket)")]
        [SerializeField] AudioSource pickaxeSource;
        [SerializeField] AudioSource rockBreakSource;
        [SerializeField] AudioSource popSource;
        [SerializeField] AudioSource beamSource;

        [Header("Clip pools — random pick on Play")]
        [SerializeField] AudioClip[] pickaxeClips;
        [SerializeField] AudioClip[] rockBreakClips;
        [SerializeField] AudioClip[] popClips;
        [SerializeField] AudioClip[] beamClips;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] float pickaxeVolume = 0.70f;
        [SerializeField, Range(0f, 1f)] float rockBreakVolume = 0.80f;
        [SerializeField, Range(0f, 1f)] float popVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] float beamVolume = 0.65f;

        [Header("Pitch jitter")]
        [SerializeField, Range(0f, 0.25f)] float pitchJitter = 0.08f;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void PlayPickaxe()   => Play(pickaxeSource, pickaxeClips, pickaxeVolume);
        public void PlayRockBreak() => Play(rockBreakSource, rockBreakClips, rockBreakVolume);
        public void PlayPop()       => Play(popSource, popClips, popVolume);
        public void PlayBeam()      => Play(beamSource, beamClips, beamVolume);

        void Play(AudioSource src, AudioClip[] clips, float volume)
        {
            if (src == null || clips == null || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            // scale by the user's SFX volume so the Settings slider affects these too.
            float sfxScale = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
            src.PlayOneShot(clip, volume * sfxScale);
        }
    }
}
