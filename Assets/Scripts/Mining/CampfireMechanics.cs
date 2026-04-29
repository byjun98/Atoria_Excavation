using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>Camper talent system.
    /// Singleton on MiningSessionRoot. On <see cref="ActivateForSession"/> it builds (once)
    /// a Fireplace visual under Canvas_Game and starts a periodic auto-damage tick that
    /// chips away at the closest non-dying rock every <c>tickInterval</c> seconds.
    /// Visual + ticks only run when the player has the Camper talent chosen.</summary>
    public sealed class CampfireMechanics : MonoBehaviour
    {
        public static CampfireMechanics Instance { get; private set; }

        [Header("Behaviour — area-kill cycle")]
        // Original burn-collider behavior toggles fireCollider every
        // burnOffTime (collider OFF, 0.28s) then burnOnTime (collider ON, 0.06s). Any rock
        // overlapping during the ON window dies. Replaces the old "every 5s closest rock -1 dmg"
        // tick which was too weak + had no area meaning.
        [SerializeField] float burnOffTime = 0.28f;
        [SerializeField] float burnOnTime = 0.06f;
        [Tooltip("Full extents (canvas px) of the burn area centered on the fireplace. " +
                 ".E — 원본 SampleScene Fireplace BoxCollider2D (85.4, 80.2) × scale 1.25 ≈ (107, 100) 매칭. " +
                 "이전 (220, 260) 은 임의 값으로 원본 대비 ~2-2.6× 컸음.")]
        [SerializeField] Vector2 burnArea = new Vector2(107f, 100f);
        [SerializeField] Vector2 fireplaceAnchored = new Vector2(-1100f, -310f);
        [SerializeField] Vector2 fireplaceSize = new Vector2(120f, 180f);
        [SerializeField] Vector2 glowSize = new Vector2(240f, 240f);

        Transform _canvasGameTransform;
        GameObject _fireplaceRoot;
        Image _flameImage;
        Image _glowImage;
        Coroutine _tickRoutine;
        Coroutine _pulseRoutine;

        public bool IsActive { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ActivateForSession(Transform canvasGameRoot)
        {
            _canvasGameTransform = canvasGameRoot;
            // Always build the visual so the gameplay state is consistent — toggle visibility by talent.
            EnsureFireplaceBuilt();
            bool talentOn = LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(TalentId.Camper);
            if (_fireplaceRoot != null) _fireplaceRoot.SetActive(talentOn);
            if (!talentOn) { IsActive = false; return; }
            IsActive = true;
            if (_tickRoutine != null) StopCoroutine(_tickRoutine);
            _tickRoutine = StartCoroutine(BurnCycle());
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _pulseRoutine = StartCoroutine(PulseFlame());
            // campfire ambient loop. Plays only while the talent is active.
            Rebuild.Audio.AudioManager.Instance?.Play("BonfireLoop");
            // count sessions where the campfire actually spawned (talent on).
            Rebuild.Stats.StatsManager.Instance?.RecordCampfireSpawned();
        }

        public void DeactivateForSession()
        {
            IsActive = false;
            if (_tickRoutine != null) { StopCoroutine(_tickRoutine); _tickRoutine = null; }
            if (_pulseRoutine != null) { StopCoroutine(_pulseRoutine); _pulseRoutine = null; }
            if (_fireplaceRoot != null) _fireplaceRoot.SetActive(false);
            // stop the ambient bonfire loop when leaving the session or losing
            // the talent on a respec. AudioManager.Stop is safe even if the clip never started.
            Rebuild.Audio.AudioManager.Instance?.Stop("BonfireLoop");
        }

        void EnsureFireplaceBuilt()
        {
            if (_fireplaceRoot != null && _flameImage != null) return;
            if (_canvasGameTransform == null) return;

            _fireplaceRoot = new GameObject("Fireplace",
                typeof(RectTransform), typeof(CanvasRenderer));
            _fireplaceRoot.transform.SetParent(_canvasGameTransform, false);
            var root = (RectTransform)_fireplaceRoot.transform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = fireplaceAnchored;
            root.sizeDelta = new Vector2(Mathf.Max(fireplaceSize.x, glowSize.x),
                                         Mathf.Max(fireplaceSize.y, glowSize.y));

            // Glow (behind).
            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowGo.transform.SetParent(_fireplaceRoot.transform, false);
            var grt = (RectTransform)glowGo.transform;
            grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = Vector2.zero;
            grt.sizeDelta = glowSize;
            _glowImage = glowGo.GetComponent<Image>();
            _glowImage.sprite = FireSpriteFactory.GetGlow();
            _glowImage.color = new Color(1f, 0.55f, 0.20f, 0.55f);
            _glowImage.raycastTarget = false;
            _glowImage.preserveAspect = false;

            // Flame (front).
            var flameGo = new GameObject("Flame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            flameGo.transform.SetParent(_fireplaceRoot.transform, false);
            var frt = (RectTransform)flameGo.transform;
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = Vector2.zero;
            frt.sizeDelta = fireplaceSize;
            _flameImage = flameGo.GetComponent<Image>();
            // prefer the user-authored Campfire PNG when present (replaces glow+flame
            // with a single sprite). Falls back to the runtime two-layer rosette otherwise.
            var userCampfire = SpriteLibrary.GetCampfireSprite();
            if (userCampfire != null)
            {
                _flameImage.sprite = userCampfire;
                _flameImage.color = Color.white; // user art carries its own color
                _flameImage.preserveAspect = true;
                // Hide the runtime glow layer so the user art reads cleanly.
                if (_glowImage != null) _glowImage.enabled = false;
            }
            else
            {
                _flameImage.sprite = FireSpriteFactory.GetFlame();
                _flameImage.color = new Color(1f, 0.78f, 0.22f, 0.95f);
                _flameImage.preserveAspect = false;
            }
            _flameImage.raycastTarget = false;
        }

        /// <summary>original BurnCollider cycle. burnOffTime (no kill) → burnOnTime
        /// (kill all rocks inside burnArea). Mirrors original Fire-tag collider toggle so the
        /// camper acts as an area hazard the player must respect, not a slow chip damage.</summary>
        IEnumerator BurnCycle()
        {
            while (IsActive)
            {
                // OFF window — no kill.
                yield return new WaitForSeconds(burnOffTime);
                if (!IsActive) yield break;
                // ON window — area kill at start, then linger so the user can see the burn beat.
                BurnRocksInArea();
                yield return new WaitForSeconds(burnOnTime);
            }
        }

        void BurnRocksInArea()
        {
            if (_fireplaceRoot == null) return;
            var rocks = Object.FindObjectsByType<Rock>(FindObjectsSortMode.None);
            if (rocks == null || rocks.Length == 0) return;
            Vector3 originWorld = _fireplaceRoot.transform.position;
            // Canvas-relative scale so burnArea (anchored px) matches the rock's screen size.
            float canvasScale = _fireplaceRoot.transform.lossyScale.x;
            float halfX = burnArea.x * 0.5f * canvasScale;
            float halfY = burnArea.y * 0.5f * canvasScale;
            for (int i = 0; i < rocks.Length; i++)
            {
                var r = rocks[i];
                if (r == null || r.IsDying) continue;
                Vector3 d = r.transform.position - originWorld;
                if (Mathf.Abs(d.x) <= halfX && Mathf.Abs(d.y) <= halfY)
                    r.InstaKill();
            }
        }

        IEnumerator PulseFlame()
        {
            float t = 0f;
            var baseScale = Vector3.one;
            while (IsActive)
            {
                t += Time.unscaledDeltaTime;
                // Slight flickering scale + alpha so the visual reads as "alive".
                float s = 1f + 0.10f * Mathf.Sin(t * 6.0f) + 0.04f * Mathf.Sin(t * 13.7f);
                if (_flameImage != null) _flameImage.transform.localScale = baseScale * s;
                if (_glowImage != null)
                {
                    var c = _glowImage.color;
                    c.a = 0.45f + 0.10f * Mathf.Sin(t * 4.3f);
                    _glowImage.color = c;
                }
                yield return null;
            }
            if (_flameImage != null) _flameImage.transform.localScale = baseScale;
        }
    }
}
