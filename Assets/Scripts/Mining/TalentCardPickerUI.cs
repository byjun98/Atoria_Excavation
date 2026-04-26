using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>Talent card picker.
    /// Shows up to 3 random unchosen talents. Clicking one applies it (LevelMechanics.ChooseTalent)
    /// and raises <see cref="OnCardChosen"/>. The session controller re-invokes <see cref="Show"/>
    /// if there are still talent points to spend, otherwise hides the panel and resumes the results flow.
    /// </summary>
    public sealed class TalentCardPickerUI : MonoBehaviour
    {
        [SerializeField] GameObject panelRoot;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Button[] cardButtons = new Button[3];
        /// <summary>top accent stripe inside each card. Tinted with TalentDef.Tint
        /// so the card still carries the talent's identity color, while the rest of the card
        /// stays on the 단청 갈 panel tone.</summary>
        [SerializeField] Image[] cardImages = new Image[3];
        /// <summary>talent icon (resolved via SpriteLibrary, falls back to rock + tint).</summary>
        [SerializeField] Image[] cardIcons = new Image[3];
        [SerializeField] TMP_Text[] cardNames = new TMP_Text[3];
        [SerializeField] TMP_Text[] cardDescs = new TMP_Text[3];

        readonly TalentId[] _currentDraw = new TalentId[3];
        readonly bool[] _slotFilled = new bool[3];
        // cache the initial transform pose so the bezier+CardDown animations can
        // snap each card back to its slot on the next Show() call.
        Vector2[] _initialPositions;
        Vector3[] _initialScales;

        public event Action OnCardChosen;

        void Awake()
        {
            _initialPositions = new Vector2[cardButtons.Length];
            _initialScales = new Vector3[cardButtons.Length];
            // Hook button click handlers once. Awake is safe because panelRoot is a sibling, not this GO.
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int idx = i; // capture
                if (cardButtons[i] == null) continue;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
                var rt = (RectTransform)cardButtons[i].transform;
                _initialPositions[i] = rt.anchoredPosition;
                _initialScales[i] = rt.localScale;
            }
        }

        public void Show(IReadOnlyList<TalentId> candidates)
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            // Draw up to 3 unique random from candidates.
            var pool = new List<TalentId>(candidates);
            for (int i = 0; i < cardButtons.Length; i++)
            {
                _slotFilled[i] = false;
                if (pool.Count == 0)
                {
                    HideSlot(i);
                    continue;
                }
                int pick = UnityEngine.Random.Range(0, pool.Count);
                var id = pool[pick];
                pool.RemoveAt(pick);
                _currentDraw[i] = id;
                _slotFilled[i] = true;
                FillSlot(i, id);
            }
            if (titleText != null)
            {
                var lm = LevelMechanics.Instance;
                titleText.text = lm != null
                    ? $"특성을 선택하세요 ({lm.NextTalentPrice} 포인트)"
                    : "특성을 선택하세요!";
            }
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        void FillSlot(int idx, TalentId id)
        {
            var info = TalentDef.Get(id);

            // Tint stripe — talent identity color stays as a top accent.
            if (idx < cardImages.Length && cardImages[idx] != null)
            {
                var tint = info.Tint;
                tint.a = 0.92f;
                cardImages[idx].color = tint;
            }

            // Icon — user-supplied via SpriteLibrary, fallback rock + tint.
            if (idx < cardIcons.Length && cardIcons[idx] != null)
            {
                var iconSprite = SpriteLibrary.GetTalentIcon(id);
                if (iconSprite != null)
                {
                    cardIcons[idx].sprite = iconSprite;
                    cardIcons[idx].color = Color.white;
                    cardIcons[idx].preserveAspect = true;
                }
                else
                {
                    cardIcons[idx].sprite = RockSpriteFactory.GetRockSprite();
                    cardIcons[idx].color = info.Tint;
                    cardIcons[idx].preserveAspect = false;
                }
            }

            if (idx < cardNames.Length && cardNames[idx] != null) cardNames[idx].text = info.Name;
            if (idx < cardDescs.Length && cardDescs[idx] != null) cardDescs[idx].text = info.Description;
            if (idx < cardButtons.Length && cardButtons[idx] != null)
            {
                // reset transform pose so the previous Show()'s bezier exit /
                // CardDown slide don't carry over into the next picker invocation.
                var rt = (RectTransform)cardButtons[idx].transform;
                if (_initialPositions != null) rt.anchoredPosition = _initialPositions[idx];
                if (_initialScales != null) rt.localScale = _initialScales[idx];
                rt.localRotation = Quaternion.identity;
                cardButtons[idx].gameObject.SetActive(true);
                cardButtons[idx].interactable = true;
            }
        }

        void HideSlot(int idx)
        {
            if (idx < cardButtons.Length && cardButtons[idx] != null)
                cardButtons[idx].gameObject.SetActive(false);
        }

        Coroutine _animCo;

        void OnSlotClicked(int idx)
        {
            if (!_slotFilled[idx]) return;
            if (LevelMechanics.Instance == null) return;
            // Lock all buttons so a fast double-click doesn't double-spend.
            for (int i = 0; i < cardButtons.Length; i++)
                if (cardButtons[i] != null) cardButtons[i].interactable = false;
            // Apply the talent immediately so downstream systems (Camper, etc.) react ASAP.
            LevelMechanics.Instance.ChooseTalent(_currentDraw[idx]);
            Rebuild.Audio.AudioManager.Instance?.Play("CardClick");

            // Bezier card move + dismiss animation. Mirrors original
            // LevelMechanics.MoveCardToUnlockedTalents (prep punch 0.45s → CardWoosh →
            // bezier move 0.5s + scale shrink + 360° spin → CardPop). Unselected cards
            // slide down (CardDown) over the same window. Coroutine fires OnCardChosen
            // at the end so the session controller knows when to refresh.
            if (gameObject.activeInHierarchy)
            {
                if (_animCo != null) StopCoroutine(_animCo);
                _animCo = StartCoroutine(DismissAnimation(idx));
            }
            else
            {
                // Panel inactive (rare — e.g. tab switched mid-click): skip animation.
                OnCardChosen?.Invoke();
            }
        }

        IEnumerator DismissAnimation(int chosenIdx)
        {
            // step 1 — unselected cards slide down + shrink (parallel coroutines).
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (i == chosenIdx) continue;
                if (cardButtons[i] == null) continue;
                StartCoroutine(CardDown(cardButtons[i].transform));
            }

            // step 2 — chosen card prep: scale 1 → 1.15 over 0.45s.
            var chosenBtn = cardButtons[chosenIdx];
            if (chosenBtn == null)
            {
                yield return new WaitForSecondsRealtime(0.45f);
                OnCardChosen?.Invoke();
                yield break;
            }
            var chosenRT = (RectTransform)chosenBtn.transform;
            Vector2 startPos = chosenRT.anchoredPosition;
            Vector3 baseScale = chosenRT.localScale;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                chosenRT.localScale = baseScale * Mathf.Lerp(1f, 1.15f, k);
                yield return null;
            }
            // 0.2s hold then CardWoosh.
            yield return new WaitForSecondsRealtime(0.2f);
            Rebuild.Audio.AudioManager.Instance?.Play("CardWoosh");

            // step 3 — Bezier move (start → mid hump up → end at top-center) +
            // scale 1.15 → 0.2 + Z-rotation 360°. SmoothStep on the parameter t.
            Vector2 endPos = new Vector2(0f, 480f); // top-center target (panel-local).
            Vector2 midPos = new Vector2((startPos.x + endPos.x) * 0.5f,
                                          Mathf.Max(startPos.y, endPos.y) + 220f);
            const float moveDur = 0.5f;
            t = 0f;
            Quaternion startRot = chosenRT.localRotation;
            while (t < moveDur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / moveDur);
                float s = Mathf.SmoothStep(0f, 1f, k);
                chosenRT.anchoredPosition = BezierAt(startPos, midPos, endPos, s);
                chosenRT.localScale = baseScale * Mathf.Lerp(1.15f, 0.2f, s);
                chosenRT.localRotation = startRot * Quaternion.Euler(0f, 0f, 360f * s);
                yield return null;
            }

            // CardPop on arrival.
            Rebuild.Audio.AudioManager.Instance?.Play("CardPop");
            chosenBtn.gameObject.SetActive(false);

            OnCardChosen?.Invoke();
            _animCo = null;
        }

        static IEnumerator CardDown(Transform t)
        {
            var rt = (RectTransform)t;
            Vector2 startPos = rt.anchoredPosition;
            Vector3 startScale = rt.localScale;
            const float dur = 0.4f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / dur);
                rt.anchoredPosition = new Vector2(startPos.x, Mathf.Lerp(startPos.y, startPos.y - 300f, k));
                rt.localScale = Vector3.Lerp(startScale, Vector3.zero, k);
                yield return null;
            }
            t.gameObject.SetActive(false);
        }

        static Vector2 BezierAt(Vector2 a, Vector2 b, Vector2 c, float t)
            => (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
    }
}
