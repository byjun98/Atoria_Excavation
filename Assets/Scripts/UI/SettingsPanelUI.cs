// Reference Rebuild.Audio.AudioManager fully qualified throughout the file to avoid
// collisions with legacy global type names.
using Rebuild.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>modal settings overlay launched from MainMenu's settingsBtn.
    /// Lives on Canvas_SettingsPanel (sortingOrder 307 — above MainMenu/Stats/AchievementUnlocked,
    /// below Tutorial/CircleTransition/SaveToast). Three sliders drive
    /// <see cref="AudioManager"/> directly (Master applies to AudioListener, Music writes to the
    /// wired-in BGM AudioSource, SFX is multiplied into every PlayOneShot). A confirm-style
    /// Reset Game button wipes the save file and re-initialises every manager.</summary>
    public sealed class SettingsPanelUI : MonoBehaviour
    {
        public static SettingsPanelUI Instance { get; private set; }

        [SerializeField] GameObject panelRoot;
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] TMP_Text masterValueLabel;
        [SerializeField] TMP_Text musicValueLabel;
        [SerializeField] TMP_Text sfxValueLabel;
        [SerializeField] Button closeButton;
        [SerializeField] Button resetGameButton;
        [SerializeField] TMP_Text resetGameLabel;
        [SerializeField] AudioSource bgmSource;

        bool _resetArmed;
        bool _binding;
        float _bgmBaseVolume;

        void Awake()
        {
            Instance = this;
            if (bgmSource != null) _bgmBaseVolume = bgmSource.volume;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            if (resetGameButton != null)
            {
                resetGameButton.onClick.RemoveAllListeners();
                resetGameButton.onClick.AddListener(OnResetClicked);
            }
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (Rebuild.Audio.AudioManager.Instance != null)
                Rebuild.Audio.AudioManager.Instance.OnVolumesChanged += SyncSlidersFromManager;
            SyncSlidersFromManager();
        }

        void OnDisable()
        {
            if (Rebuild.Audio.AudioManager.Instance != null)
                Rebuild.Audio.AudioManager.Instance.OnVolumesChanged -= SyncSlidersFromManager;
        }

        public void Show()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (panelRoot != null) panelRoot.SetActive(true);
            SyncSlidersFromManager();
            // Re-arm the reset button each time the panel opens so leaving + returning resets state.
            _resetArmed = false;
            UpdateResetLabel();
        }

        public void Hide()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (panelRoot != null) panelRoot.SetActive(false);
            _resetArmed = false;
            UpdateResetLabel();
        }

        void SyncSlidersFromManager()
        {
            var am = Rebuild.Audio.AudioManager.Instance;
            if (am == null) return;
            // Suppress the slider OnValueChanged callbacks during programmatic syncs so we don't
            // ping-pong through SetXVolume().
            _binding = true;
            if (masterSlider != null) masterSlider.value = am.MasterVolume;
            if (musicSlider != null) musicSlider.value = am.MusicVolume;
            if (sfxSlider != null) sfxSlider.value = am.SfxVolume;
            _binding = false;
            ApplyMusicVolumeToBgm();
            UpdateValueLabels();
        }

        void OnMasterChanged(float v)
        {
            if (_binding) return;
            Rebuild.Audio.AudioManager.Instance?.SetMasterVolume(v);
            UpdateValueLabels();
        }

        void OnMusicChanged(float v)
        {
            if (_binding) return;
            Rebuild.Audio.AudioManager.Instance?.SetMusicVolume(v);
            ApplyMusicVolumeToBgm();
            UpdateValueLabels();
        }

        void OnSfxChanged(float v)
        {
            if (_binding) return;
            Rebuild.Audio.AudioManager.Instance?.SetSfxVolume(v);
            UpdateValueLabels();
        }

        void ApplyMusicVolumeToBgm()
        {
            if (bgmSource == null || Rebuild.Audio.AudioManager.Instance == null) return;
            bgmSource.volume = _bgmBaseVolume * Rebuild.Audio.AudioManager.Instance.MusicVolume;
        }

        void UpdateValueLabels()
        {
            var am = Rebuild.Audio.AudioManager.Instance;
            if (am == null) return;
            if (masterValueLabel != null) masterValueLabel.text = Mathf.RoundToInt(am.MasterVolume * 100f) + "%";
            if (musicValueLabel != null) musicValueLabel.text = Mathf.RoundToInt(am.MusicVolume * 100f) + "%";
            if (sfxValueLabel != null) sfxValueLabel.text = Mathf.RoundToInt(am.SfxVolume * 100f) + "%";
        }

        void OnResetClicked()
        {
            if (!_resetArmed)
            {
                // First click arms the button — second click within this panel session confirms.
                _resetArmed = true;
                UpdateResetLabel();
                Rebuild.Audio.AudioManager.Instance?.Play("UI_Click1");
                return;
            }
            PerformReset();
        }

        void UpdateResetLabel()
        {
            if (resetGameLabel == null) return;
            resetGameLabel.text = _resetArmed ? "정말 초기화?" : "기록 초기화";
        }

        void PerformReset()
        {
            // Wipe the save file then ask every IDataPersistence to LoadData(new DTO) — same path
            // as a brand-new player on day 1. Volumes get reset too (LoadData on AudioManager
            // pushes the DTO defaults back into the sliders via OnVolumesChanged).
            var dpm = Rebuild.Saving.DataPersistenceManager.Instance;
            if (dpm != null)
            {
                dpm.WipeSaveFile();
                dpm.NewGame();
                dpm.SaveGame();
            }
            // Clear cached eligibility for one-shot managers that don't fully wipe in LoadData.
            Rebuild.Mining.TheEndingManager.Instance?.ResetAll();
            _resetArmed = false;
            UpdateResetLabel();
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            Debug.Log("[Rebuild][] Game reset — all save data wiped and managers re-initialised.");
        }
    }
}
