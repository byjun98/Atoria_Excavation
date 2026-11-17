using System;
using System.Collections.Generic;
using Rebuild.Saving;
using UnityEngine;
using UnityEngine.Audio;

namespace Rebuild.Audio
{












    public sealed class AudioManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] Sound[] sounds = System.Array.Empty<Sound>();
        [SerializeField] AudioMixerGroup musicGroup;
        [SerializeField] AudioMixerGroup sfxGroup;
        [SerializeField] AudioMixerGroup uiGroup;

        [SerializeField, Range(0f, 0.2f)] float uiPitchJitter = 0.10f;


        [SerializeField, Range(0f, 1f)] float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 1f;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public event Action OnVolumesChanged;

        Dictionary<string, Sound> _byName;
        static readonly HashSet<string> s_uiPitchRandomNames = new HashSet<string>
        {
            "Click_1", "UI_Click1", "HoverUI", "FadeIn", "CardHover"
        };

        void Awake()
        {
            Instance = this;
            AudioListener.volume = masterVolume;
            _byName = new Dictionary<string, Sound>(sounds?.Length ?? 0, StringComparer.Ordinal);
            if (sounds == null) return;
            for (int i = 0; i < sounds.Length; i++)
            {
                var s = sounds[i];
                if (s == null || string.IsNullOrEmpty(s.name)) continue;
                var src = gameObject.AddComponent<AudioSource>();
                src.clip = s.clip;
                src.volume = s.volume;
                src.pitch = s.pitch;
                src.loop = s.loop;
                src.playOnAwake = false;
                src.outputAudioMixerGroup = ResolveGroup(s.audioType);
                s.source = src;
                _byName[s.name] = s;



            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioMixerGroup ResolveGroup(AudioType t)
        {
            switch (t)
            {
                case AudioType.Music: return musicGroup;
                case AudioType.UI:    return uiGroup != null ? uiGroup : sfxGroup;
                case AudioType.SFX:
                default:              return sfxGroup;
            }
        }



        public void Play(string name)
        {
            if (_byName == null || string.IsNullOrEmpty(name)) return;
            if (!_byName.TryGetValue(name, out var s)) return;
            if (s.source == null || s.clip == null) return;

            s.source.pitch = s_uiPitchRandomNames.Contains(name)
                ? s.pitch * (1f + UnityEngine.Random.Range(-uiPitchJitter, uiPitchJitter))
                : s.pitch;
            if (s.loop) { if (!s.source.isPlaying) s.source.Play(); }
            else        { s.source.PlayOneShot(s.clip, s.volume * sfxVolume); }
        }


        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            AudioListener.volume = masterVolume;
            OnVolumesChanged?.Invoke();
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            OnVolumesChanged?.Invoke();
        }

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            OnVolumesChanged?.Invoke();
        }


        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            masterVolume = Mathf.Clamp01(data.masterVolume);
            musicVolume = Mathf.Clamp01(data.musicVolume);
            sfxVolume = Mathf.Clamp01(data.sfxVolume);
            AudioListener.volume = masterVolume;
            OnVolumesChanged?.Invoke();
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            data.masterVolume = masterVolume;
            data.musicVolume = musicVolume;
            data.sfxVolume = sfxVolume;
        }

        public void Stop(string name)
        {
            if (_byName == null) return;
            if (_byName.TryGetValue(name, out var s) && s.source != null)
                s.source.Stop();
        }

        public bool IsPlaying(string name)
        {
            if (_byName == null) return false;
            return _byName.TryGetValue(name, out var s) && s.source != null && s.source.isPlaying;
        }

        public bool Has(string name) => _byName != null && _byName.ContainsKey(name);
    }
}
