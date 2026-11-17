using Rebuild.Mining;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    public sealed class SkillPurchaseFeedbackPlayer : MonoBehaviour
    {
        const int MaxParticles = 20;

        [SerializeField] Image shine;
        [SerializeField] Image wave;
        readonly Image[] _particles = new Image[MaxParticles];
        SkillPurchaseFeedback _owner;
        Image _source;
        Sprite _burstSprite;
        int _particleCount;
        float _elapsed;
        float _duration;
        float _nextPurchaseTime;
        bool _completed;

        public void Configure(Sprite burstSprite, Material _)
        {
            StopVisuals();
            _burstSprite = burstSprite;
            EnsureImages();
        }

        public void ConfigureRing(Sprite ringSprite)
        {
            if (wave != null) wave.sprite = ringSprite;
        }

        void EnsureImages()
        {
            if (shine == null) shine = GetOrCreateImage("Shine");
            if (wave == null) wave = GetOrCreateImage("Wave");

            shine.raycastTarget = false;
            shine.preserveAspect = true;
            shine.material = null;
            shine.enabled = false;
            wave.raycastTarget = false;
            wave.enabled = false;
        }

        Image GetOrCreateImage(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                var created = new GameObject(childName, typeof(RectTransform));
                created.transform.SetParent(transform, false);
                child = created.transform;
            }

            var image = child.GetComponent<Image>();
            if (image == null) image = child.gameObject.AddComponent<Image>();
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            return image;
        }

        void OnEnable()
        {
            if (Application.isPlaying) EnsureImages();
        }

        public void Play(SkillPurchaseFeedback owner, Image source, bool bought, bool completed)
        {
            if (!isActiveAndEnabled || owner == null || source == null || shine == null) return;
            if (bought && Time.unscaledTime < _nextPurchaseTime) return;
            if (!bought && (_owner != null || Time.unscaledTime < _nextPurchaseTime)) return;

            StopVisuals();
            _owner = owner;
            _source = source;
            _completed = completed;
            _elapsed = 0f;
            _duration = bought ? .52f : .30f;
            if (bought) _nextPurchaseTime = Time.unscaledTime + .18f;
            transform.SetAsLastSibling();
            AlignToSource();

            shine.sprite = source.sprite;
            shine.enabled = shine.sprite != null;
            shine.rectTransform.sizeDelta = source.rectTransform.rect.size;
            if (completed && wave != null)
            {
                if (wave.sprite == null) wave.sprite = RingSpriteFactory.GetRing();
                wave.rectTransform.sizeDelta = source.rectTransform.rect.size + new Vector2(22f, 22f);
                wave.enabled = true;
            }

            if (bought)
            {
                _particleCount = completed ? MaxParticles : 14;
                for (int i = 0; i < _particleCount; i++)
                {
                    var particle = _particles[i];
                    if (particle == null)
                    {
                        particle = GetOrCreateImage($"Spark{i + 1}");
                        particle.raycastTarget = false;
                        particle.rectTransform.sizeDelta = new Vector2(7f, 7f);
                        _particles[i] = particle;
                    }
                    particle.sprite = _burstSprite;
                    particle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    particle.enabled = true;
                }
            }

            UpdateVisuals();
        }

        void AlignToSource()
        {
            transform.position = _source.rectTransform.TransformPoint(_source.rectTransform.rect.center);
            transform.rotation = _source.transform.rotation;
            var parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            var scale = _source.transform.lossyScale;
            transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1f);
        }

        void Update()
        {
            if (_duration <= 0f) return;
            if (_source == null || _owner == null || !_owner.isActiveAndEnabled || !_source.gameObject.activeInHierarchy)
            {
                StopVisuals();
                return;
            }
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed >= _duration)
            {
                StopVisuals();
                return;
            }
            AlignToSource();
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            float flash = _elapsed < .30f ? Mathf.Sin(Mathf.PI * _elapsed / .30f) : 0f;
            var highlight = _particleCount > 0 && !_completed ? MuseumUiTheme.Jade : MuseumUiTheme.Gold;
            highlight.a = flash * .62f;
            shine.color = highlight;
            shine.transform.localScale = Vector3.one * (1f + flash * .07f);

            if (_completed && wave != null)
            {
                float t = Mathf.Clamp01(_elapsed / .45f);
                wave.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.7f, t);
                var color = MuseumUiTheme.Gold;
                color.a = (1f - t) * .65f;
                wave.color = color;
            }

            for (int i = 0; i < _particleCount; i++)
            {
                float angle = (i + .5f) * Mathf.PI * 2f / _particleCount;
                float life = Mathf.Clamp01(_elapsed / (.28f + .04f * (i % 5)));
                float distance = Mathf.Lerp(12f, 50f + 7f * (i % 4), life);
                var particle = _particles[i];
                var rect = particle.rectTransform;
                rect.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                rect.localScale = Vector3.one * (1f - life);
                highlight.a = 1f - life;
                particle.color = highlight;
                particle.enabled = life < 1f;
            }
        }

        public void StopFor(SkillPurchaseFeedback owner)
        {
            if (_owner == owner) StopVisuals();
        }

        void StopVisuals()
        {
            _owner = null;
            _source = null;
            _duration = 0f;
            for (int i = 0; i < _particleCount; i++)
                _particles[i].enabled = false;
            _particleCount = 0;
            if (shine != null) shine.enabled = false;
            if (wave != null) wave.enabled = false;
        }

        void OnDisable() => StopVisuals();
    }
}
