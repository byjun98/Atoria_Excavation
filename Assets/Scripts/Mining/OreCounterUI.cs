using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{





    public sealed class OreCounterUI : MonoBehaviour
    {

        public static OreCounterUI Instance { get; private set; }

        [SerializeField] RectTransform[] entries = new RectTransform[8];
        [SerializeField] TextMeshProUGUI[] countLabels = new TextMeshProUGUI[8];
        [SerializeField] float punchScale = 1.08f;
        [SerializeField] float punchDuration = 0.28f;

        RectTransform[] _iconRects;
        Vector3[] _baseScales;
        Coroutine[] _punchRoutines;
        bool _subscribed;

        void Awake()
        {
            Instance = this;
            _iconRects = new RectTransform[entries.Length];
            _baseScales = new Vector3[entries.Length];
            _punchRoutines = new Coroutine[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null)
                {
                    foreach (var image in entries[i].GetComponentsInChildren<Image>(true))
                    {
                        if (image.transform == entries[i]) continue;
                        _iconRects[i] = image.rectTransform;
                        break;
                    }
                }
                _baseScales[i] = _iconRects[i] != null ? _iconRects[i].localScale : Vector3.one;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }



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

                for (int i = 0; i < 8; i++)
                    SetLabel((OreType)i, GoldAndOreMechanics.Instance.GetSessionGained((OreType)i));
            }

            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;




            RefreshRowVisibility();
        }



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

            if (ore == OreType.Gold) return true;

            if (ores != null && ores.GetSessionGained(ore) > 0) return true;

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
            for (int i = 0; i < _punchRoutines.Length; i++)
            {
                if (_punchRoutines[i] != null) StopCoroutine(_punchRoutines[i]);
                _punchRoutines[i] = null;
                if (_iconRects[i] != null) _iconRects[i].localScale = _baseScales[i];
            }
        }

        void HandleUpgradeChanged(UpgradeId id, int _)
        {

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

            tmp.text = Rebuild.Util.FormatNumbers.Format(count);
        }

        void PlayPunch(int idx)
        {
            if (idx < 0 || idx >= entries.Length) return;
            if (_iconRects[idx] == null) return;
            if (_punchRoutines[idx] != null) StopCoroutine(_punchRoutines[idx]);
            _punchRoutines[idx] = StartCoroutine(PunchRoutine(idx));
        }

        IEnumerator PunchRoutine(int idx)
        {
            var rt = _iconRects[idx];
            var basis = _baseScales[idx];
            var punched = basis * punchScale;
            float t = 0f;

            while (t < punchDuration * 0.35f)
            {
                t += Time.unscaledDeltaTime;
                rt.localScale = Vector3.Lerp(basis, punched, Mathf.Clamp01(t / (punchDuration * 0.35f)));
                yield return null;
            }

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
