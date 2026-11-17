using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    public sealed class PickaxeSwingFx : MonoBehaviour
    {
        const float SwingDuration = .18f;
        const float FadeDuration = .10f;
        const float SwingPeakAngle = -55f;
        const float DefaultSize = 80f;
        const int MaxIdle = 96;
        static readonly Stack<PickaxeSwingFx> s_idle = new Stack<PickaxeSwingFx>();

        Image _img;
        RectTransform _rt;
        Color _tint;
        float _started;

        public static PickaxeSwingFx Spawn(Transform parent, Vector2 anchoredPos,
                                            Sprite sprite, Color tint, float size = DefaultSize)
        {
            if (parent == null) return null;
            PickaxeSwingFx fx = null;
            while (fx == null && s_idle.Count > 0) fx = s_idle.Pop();
            if (fx == null)
            {
                var go = new GameObject("PickaxeSwingFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.SetActive(false);
                fx = go.AddComponent<PickaxeSwingFx>();
                fx._rt = (RectTransform)go.transform;
                fx._img = go.GetComponent<Image>();
                fx._img.preserveAspect = true;
                fx._img.raycastTarget = false;
            }
            fx._rt.SetParent(parent, false);
            fx._rt.SetAsLastSibling();
            fx._rt.anchorMin = fx._rt.anchorMax = new Vector2(.5f, .5f);
            fx._rt.pivot = new Vector2(.5f, .5f);
            fx._rt.anchoredPosition = anchoredPos;
            fx._rt.sizeDelta = Vector2.one * size;
            fx._img.sprite = sprite;
            fx._img.color = tint;
            fx._tint = tint;
            fx._started = Time.unscaledTime - Time.unscaledDeltaTime;
            fx.ShowSwing(Time.unscaledDeltaTime);
            fx.gameObject.SetActive(true);
            return fx;
        }

        void Update()
        {
            float age = Time.unscaledTime - _started;
            if (age < SwingDuration + FadeDuration)
            {
                ShowSwing(age);
                return;
            }

            gameObject.SetActive(false);
            if (s_idle.Count >= MaxIdle) { Destroy(gameObject); return; }
            transform.SetParent(ProjectileLauncher.PoolParent, false);
            s_idle.Push(this);
        }

        void ShowSwing(float age)
        {
            float k = Mathf.Clamp01(age / SwingDuration);
            float angle = k < 1f ? SwingPeakAngle * Mathf.Sin(k * Mathf.PI) : 0f;
            _rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            var tint = _tint;
            tint.a *= 1f - Mathf.Clamp01((age - SwingDuration) / FadeDuration);
            _img.color = tint;
        }
    }
}
