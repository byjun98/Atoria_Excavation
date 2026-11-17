using UnityEngine;

namespace Rebuild.Audio
{



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

            float sfxScale = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
            src.PlayOneShot(clip, volume * sfxScale);
        }
    }
}
