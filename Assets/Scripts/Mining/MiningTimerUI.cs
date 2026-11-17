using TMPro;
using UnityEngine;

namespace Rebuild.Mining
{

    public sealed class MiningTimerUI : MonoBehaviour
    {
        [SerializeField] TMP_Text timerText;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color warningColor = new Color(1f, 0.34f, 0.34f, 1f);
        [SerializeField] float warningThresholdSec = 3f;

        public void SetTime(float seconds)
        {
            if (timerText == null) return;
            float clamped = Mathf.Max(0f, seconds);
            int totalSec = Mathf.CeilToInt(clamped);
            int mm = totalSec / 60;
            int ss = totalSec % 60;
            timerText.text = $"{mm:00}:{ss:00}";
            timerText.color = (clamped <= warningThresholdSec) ? warningColor : normalColor;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
        }
    }
}
