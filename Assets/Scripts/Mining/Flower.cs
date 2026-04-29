using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>a hoverable / clickable flower drop.
    /// Lives under Canvas_Game (alongside TileRows / Rocks). Spawned by <see cref="FlowerMechanics"/>
    /// on rock destroy when the SpringSeason talent is chosen. Auto-fades after <c>lifetime</c>
    /// or pays out +2 random ore on the first pointer contact (hover or tap) and disappears.
    /// (HOVER_MINING_REFERENCE.md §E) — Flower uses Option A (single-shot enter)
    /// rather than the channel pattern Rock uses, because the payout is a one-time bonus.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class Flower : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] float lifetime = 5f;
        [SerializeField] int oreBonus = 2;
        [SerializeField] float spawnPunchTime = 0.18f;

        Image _img;
        RectTransform _rt;
        Coroutine _lifeRoutine;
        bool _consumed;

        void Awake()
        {
            _img = GetComponent<Image>();
            _rt = (RectTransform)transform;
        }

        public void Initialize()
        {
            _consumed = false;
            _img.raycastTarget = true;
            // prefer the user-authored Flower PNG; fall back to runtime rosette.
            var userFlower = SpriteLibrary.GetFlowerSprite();
            if (userFlower != null)
            {
                _img.sprite = userFlower;
                _img.preserveAspect = true;
                _img.color = Color.white; // user art carries its own color
            }
            else
            {
                _img.sprite = FireSpriteFactory.GetFlower();
                _img.preserveAspect = false;
                // Random pastel tint so multiple flowers feel distinct.
                float hue = UnityEngine.Random.value;
                _img.color = Color.HSVToRGB(hue, 0.55f, 1.0f);
            }
            // Start small + scale up (spawn punch).
            _rt.localScale = Vector3.one * 0.2f;
            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            _lifeRoutine = StartCoroutine(Lifecycle());
        }

        public void OnPointerEnter(PointerEventData eventData) => Consume();
        public void OnPointerClick(PointerEventData eventData) => Consume();

        void Consume()
        {
            if (_consumed) return;
            _consumed = true;
            _img.raycastTarget = false;

            if (GoldAndOreMechanics.Instance != null)
            {
                // Pick a random ore weighted same as RockSpawner — Shell keeps it simple: uniform random across 8.
                var ore = (OreType)UnityEngine.Random.Range(0, 8);
                GoldAndOreMechanics.Instance.AddOre(ore, oreBonus);
                Rebuild.Audio.OverlappingSounds.Instance?.PlayPop();
            }
            // FlowerPop SFX in addition to OverlappingSounds.PlayPop. Independent
            // channel so multiple flower pops within one frame layer naturally.
            Rebuild.Audio.AudioManager.Instance?.Play("FlowerPop");
            // independent click counter (separate from rock clicks).
            Rebuild.Stats.StatsManager.Instance?.RecordFlowerClicked();
            if (_lifeRoutine != null) StopCoroutine(_lifeRoutine);
            StartCoroutine(FadeAndDestroy(0.18f, 1.4f));
        }

        IEnumerator Lifecycle()
        {
            // Pop in.
            float t = 0f;
            var start = _rt.localScale;
            var target = Vector3.one;
            while (t < spawnPunchTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / spawnPunchTime);
                _rt.localScale = Vector3.Lerp(start, target * 1.1f, k);
                yield return null;
            }
            // Settle.
            t = 0f;
            while (t < 0.10f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.10f);
                _rt.localScale = Vector3.Lerp(target * 1.1f, target, k);
                yield return null;
            }
            _rt.localScale = target;

            // Wait for lifetime, then time out + fade.
            float wait = Mathf.Max(0.1f, lifetime - spawnPunchTime - 0.10f);
            float waited = 0f;
            while (waited < wait && !_consumed)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_consumed) yield break;
            StartCoroutine(FadeAndDestroy(0.35f, 0.85f));
        }

        IEnumerator FadeAndDestroy(float dur, float endScaleMul)
        {
            _img.raycastTarget = false;
            var startScale = _rt.localScale;
            var endScale = startScale * endScaleMul;
            var c0 = _img.color;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _rt.localScale = Vector3.Lerp(startScale, endScale, k);
                var c = c0; c.a = c0.a * (1f - k);
                _img.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
