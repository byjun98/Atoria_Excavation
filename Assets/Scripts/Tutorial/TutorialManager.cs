using System;
using Rebuild.Saving;
using UnityEngine;

namespace Rebuild.Tutorial
{
    /// <summary>+ 6-frame context-triggered tutorial. Mirrors
    /// original `Tutorial.cs` six pressedOk flags: miningSession (0) / oreAndCrafting (1) /
    /// talent (2) / anvil (3) / theMine (4) / artifact (5). Each frame fires once when the
    /// player first encounters its context (boot, first session end, first tab visit, etc.).
    /// <para>Legacy `tutorialSeen` single-bool save (schema v2) migrates into pressedOk[0] only,
    /// so the existing player sees the post-boot frames again the first time they meet each
    /// context — by design.</para>
    /// </summary>
    public sealed class TutorialManager : MonoBehaviour, Rebuild.Saving.IDataPersistence
    {
        public static TutorialManager Instance { get; private set; }

        [SerializeField] bool[] _pressedOk = new bool[6];

        public bool HasSeen(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return true;
            return _pressedOk[frameIdx];
        }

        // Legacy event kept for back-compat. Listeners receive frameIdx; old single
        // OnRequestShow callers can interpret 0 as "boot tutorial".
        public event Action<int> OnRequestShowFrame;
        public event Action OnTutorialFinished;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Called by DataPersistenceManager once LoadGame populates _pressedOk.
        /// Fires frame 0 (boot intro) when un-seen.</summary>
        public void MaybeShowOnBoot()
        {
            MaybeShow(0);
        }

        /// <summary>context-triggered frame request. Tab-switch hooks call this
        /// with frame indices 2~5; MiningSessionController calls 0 and 1.</summary>
        public void MaybeShow(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return;
            if (_pressedOk[frameIdx]) return;
            OnRequestShowFrame?.Invoke(frameIdx);
        }

        public void MarkSeen(int frameIdx)
        {
            if (frameIdx < 0 || frameIdx >= _pressedOk.Length) return;
            if (_pressedOk[frameIdx]) return;
            _pressedOk[frameIdx] = true;
            // OnTutorialFinished fires once all 6 are seen (mirrors original "tutorial completed"
            // milestone — currently no listener but reserved for future achievements).
            for (int i = 0; i < _pressedOk.Length; i++) if (!_pressedOk[i]) return;
            OnTutorialFinished?.Invoke();
        }

        /// <summary>Debug helper — used by the Editor menu to re-trigger the tutorial.</summary>
        public void ResetSeen()
        {
            for (int i = 0; i < _pressedOk.Length; i++) _pressedOk[i] = false;
        }

        // ---- + 36.6 IDataPersistence ----
        public void LoadData(GameDataDTO data)
        {
            if (data == null) return;
            // schema v3 — read tutorialPressedOk[6] directly.
            if (data.tutorialPressedOk != null && data.tutorialPressedOk.Length >= _pressedOk.Length)
            {
                for (int i = 0; i < _pressedOk.Length; i++) _pressedOk[i] = data.tutorialPressedOk[i];
            }
            else if (data.tutorialSeen)
            {
                // schema v2 migration — single bool maps to "boot intro seen".
                _pressedOk[0] = true;
            }
            // frame 0 (mining session 환영) 자동 fire 제거.
            // MiningSessionController.ApplyEnterSession 가 명시 MaybeShow(0) 호출 — 원본은 첫
            // mining session 진입 시 popup (mining grid 배경). 이전엔 LoadData 즉시 fire 라
            // 사용자가 menu 화면에서 popup 봤음 (원본과 timing 차이).
        }

        public void SaveData(GameDataDTO data)
        {
            if (data == null) return;
            if (data.tutorialPressedOk == null || data.tutorialPressedOk.Length != _pressedOk.Length)
                data.tutorialPressedOk = new bool[_pressedOk.Length];
            for (int i = 0; i < _pressedOk.Length; i++) data.tutorialPressedOk[i] = _pressedOk[i];
            // Also keep the legacy bool current so an old version loading the new save still works.
            data.tutorialSeen = _pressedOk[0];
        }
    }
}
