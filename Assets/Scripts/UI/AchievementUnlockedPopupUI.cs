using System.Collections;
using System.Collections.Generic;
using Rebuild.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{




    public sealed class AchievementUnlockedPopupUI : MonoBehaviour
    {
        public static AchievementUnlockedPopupUI Instance { get; private set; }

        [SerializeField] CanvasGroup group;
        [SerializeField] RectTransform card;
        [SerializeField] Image swatch;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descText;
        [SerializeField] float slideDistance = 220f;
        [SerializeField] float slideDuration = 0.4f;
        [SerializeField] float holdDuration = 1.9f;

        readonly Queue<AchievementId> _queue = new Queue<AchievementId>();
        Coroutine _playing;

        void Awake()
        {
            Instance = this;
            if (group != null) group.alpha = 0f;
        }

        void OnEnable()
        {
            if (AchievementsManager.Instance != null)
                AchievementsManager.Instance.OnAchievementUnlocked += Enqueue;
        }

        void OnDisable()
        {
            if (AchievementsManager.Instance != null)
                AchievementsManager.Instance.OnAchievementUnlocked -= Enqueue;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Enqueue(AchievementId id)
        {
            _queue.Enqueue(id);
            if (_playing == null) _playing = StartCoroutine(Pump());
        }

        IEnumerator Pump()
        {
            while (_queue.Count > 0)
            {
                var id = _queue.Dequeue();
                yield return PlayOne(id);
            }
            _playing = null;
        }

        IEnumerator PlayOne(AchievementId id)
        {
            var info = AchievementDef.Get(id);
            if (titleText != null) titleText.text = info.Name;
            if (descText != null) descText.text = info.Description;
            if (swatch != null)
            {
                swatch.sprite = Rebuild.Mining.SpriteLibrary.GetAchievementSprite(id);
                swatch.preserveAspect = true;
                swatch.color = info.Tint;
            }


            Rebuild.Audio.AudioManager.Instance?.Play("Achievement");


            if (card != null) card.anchoredPosition = new Vector2(card.anchoredPosition.x, slideDistance);
            if (group != null) group.alpha = 0f;
            float t = 0f;
            var startPos = card != null ? card.anchoredPosition : Vector2.zero;
            var endPos = new Vector2(startPos.x, 0f);
            while (t < slideDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / slideDuration);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                if (card != null) card.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
                if (group != null) group.alpha = e;
                yield return null;
            }
            if (card != null) card.anchoredPosition = endPos;
            if (group != null) group.alpha = 1f;


            yield return new WaitForSecondsRealtime(holdDuration);


            t = 0f;
            while (t < slideDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / slideDuration);
                if (card != null) card.anchoredPosition = Vector2.Lerp(endPos, startPos, p);
                if (group != null) group.alpha = 1f - p;
                yield return null;
            }
            if (group != null) group.alpha = 0f;
        }
    }
}
