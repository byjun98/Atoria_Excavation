using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>
    /// HUD widget: 8 ore counters (Gold + 7 ores) shown top-left of Canvas_UI_MiningScene.
    /// Subscribes to GoldAndOreMechanics.OnSessionGainedChanged and animates a small punch on increment.
    /// Real MaterialFrame_topLeft (popup tween, ArtifactDisplayPosition, frame sprites) lands in /polish.
    /// </summary>
    public sealed class OreCounterUI : MonoBehaviour
    {
        /// <summary>OreDropPopupFx queries this for the fly-to RectTransform.</summary>
        public static OreCounterUI Instance { get; private set; }

        [SerializeField] RectTransform[] entries = new RectTransform[8];
        [SerializeField] TextMeshProUGUI[] countLabels = new TextMeshProUGUI[8];
        [SerializeField] float punchScale = 1.45f;
        [SerializeField] float punchDuration = 0.28f;

        Vector3[] _baseScales;
        Coroutine[] _punchRoutines;
        bool _subscribed;

        void Awake()
        {
            Instance = this;
            _baseScales = new Vector3[entries.Length];
            _punchRoutines = new Coroutine[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                _baseScales[i] = entries[i] != null ? entries[i].localScale : Vector3.one;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>fly-to target for ore drop popups. Returns the RectTransform of
        /// the counter slot for <paramref name="ore"/>, or null when the slot wasn't wired.</summary>
        public RectTransform GetEntryRT(OreType ore)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= entries.Length) return null;
            return entries[idx];
        }

        void OnEnable()
        {
            if (GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.OnSessionGainedChanged += HandleSessionGainedChanged;
                _subscribed = true;
                // Sync current session values on activation.
                for (int i = 0; i < 8; i++)
                    SetLabel((OreType)i, GoldAndOreMechanics.Instance.GetSessionGained((OreType)i));
            }
            // Spawn SkillTree 노드 구매 시 HUD row 즉시 활성.
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;
            // progressive unlock. Row 가 visible 한 조건:
            //   (1) 누적 카운트 > 0 (한 번이라도 채굴함), 또는
            //   (2) 해당 ore 의 Spawn SkillTree 노드 구매됨 (Cobalt~Painite),
            //   (3) Gold/Copper/Iron 은 base 광물 — 항상 visible (시작 직후엔 Gold 만).
            RefreshRowVisibility();
        }

        /// <summary>recalculate visible/hidden state for each row. Called from OnEnable
        /// and after the player's first drop of a new ore type (via HandleSessionGainedChanged).</summary>
        public void RefreshRowVisibility()
        {
            var ore = GoldAndOreMechanics.Instance;
            var sk = SkillTreeManager.Instance;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null) continue;
                bool visible = ShouldShowRow((OreType)i, ore, sk);
                entries[i].gameObject.SetActive(visible);
            }
        }

        static bool ShouldShowRow(OreType ore, GoldAndOreMechanics ores, SkillTreeManager sk)
        {
            // Gold 는 첫 광물 — 항상 표시 (mining session 진입 직후 빈 상태 X).
            if (ore == OreType.Gold) return true;
            // 이미 1개 이상 보유하면 표시 (한 번 발굴 후엔 계속 보임).
            if (ores != null && ores.GetSessionGained(ore) > 0) return true;
            // 해당 ore 의 Spawn SkillTree 노드 구매됐으면 표시.
            if (sk != null)
            {
                UpgradeId spawnId = ore switch
                {
                    OreType.Copper  => UpgradeId.CopperSpawn,
                    OreType.Iron    => UpgradeId.IronSpawn,
                    OreType.Cobalt  => UpgradeId.CobaltSpawn,
                    OreType.Uranium => UpgradeId.UraniumSpawn,
                    OreType.Ismium  => UpgradeId.IsmiumSpawn,
                    OreType.Iridium => UpgradeId.IridiumSpawn,
                    OreType.Painite => UpgradeId.PainiteSpawn,
                    _ => (UpgradeId)(-1),
                };
                if ((int)spawnId >= 0 && sk.GetLevel(spawnId) > 0) return true;
            }
            return false;
        }

        void OnDisable()
        {
            if (_subscribed && GoldAndOreMechanics.Instance != null)
            {
                GoldAndOreMechanics.Instance.OnSessionGainedChanged -= HandleSessionGainedChanged;
                _subscribed = false;
            }
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
        }

        void HandleUpgradeChanged(UpgradeId id, int _)
        {
            // Spawn 노드 구매 시점 row 즉시 활성. 다른 노드는 무관 cheap no-op.
            switch (id)
            {
                case UpgradeId.CopperSpawn:
                case UpgradeId.IronSpawn:
                case UpgradeId.CobaltSpawn:
                case UpgradeId.UraniumSpawn:
                case UpgradeId.IsmiumSpawn:
                case UpgradeId.IridiumSpawn:
                case UpgradeId.PainiteSpawn:
                    RefreshRowVisibility();
                    break;
            }
        }

        void HandleSessionGainedChanged(OreType ore, long newCount)
        {
            SetLabel(ore, newCount);
            if (newCount > 0) PlayPunch((int)ore);
            // 첫 ore 획득 시 row 활성. 이미 visible 이면 cheap no-op.
            int idx = (int)ore;
            if (newCount > 0 && idx >= 0 && idx < entries.Length
                && entries[idx] != null && !entries[idx].gameObject.activeSelf)
            {
                entries[idx].gameObject.SetActive(true);
            }
            else if (newCount <= 0)
            {
                RefreshRowVisibility();
            }
        }

        void SetLabel(OreType ore, long count)
        {
            int idx = (int)ore;
            if (idx < 0 || idx >= countLabels.Length) return;
            var tmp = countLabels[idx];
            if (tmp == null) return;
            // K/M/B/T 축약 (100k 이상). 100k 이하는 그대로 (1,234 형식).
            tmp.text = Rebuild.Util.FormatNumbers.Format(count);
        }

        void PlayPunch(int idx)
        {
            if (idx < 0 || idx >= entries.Length) return;
            if (entries[idx] == null) return;
            if (_punchRoutines[idx] != null) StopCoroutine(_punchRoutines[idx]);
            _punchRoutines[idx] = StartCoroutine(PunchRoutine(idx));
        }

        IEnumerator PunchRoutine(int idx)
        {
            var rt = entries[idx];
            var basis = _baseScales[idx];
            var punched = basis * punchScale;
            float t = 0f;
            // up (fast)
            while (t < punchDuration * 0.35f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(basis, punched, Mathf.Clamp01(t / (punchDuration * 0.35f)));
                yield return null;
            }
            // settle
            t = 0f;
            float settle = punchDuration * 0.65f;
            while (t < settle)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(punched, basis, Mathf.Clamp01(t / settle));
                yield return null;
            }
            rt.localScale = basis;
            _punchRoutines[idx] = null;
        }
    }
}
