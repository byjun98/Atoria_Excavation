using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Assembly-CSharp defines a global 'DataPersistenceManager' — always reference
// Rebuild.Saving.DataPersistenceManager fully qualified here to skip the global collision.

namespace Rebuild.UI
{
    /// <summary>tiny "Saved!" toast in the corner.
    /// Subscribes to <see cref="DataPersistenceManager.OnSaved"/>; fades the label in/out
    /// over a short window so it doesn't crowd the active mining HUD.</summary>
    public sealed class SaveToastUI : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text label;
        [SerializeField] float showDuration = 1.4f;
        [SerializeField] float fadeIn = 0.15f;
        [SerializeField] float fadeOut = 0.35f;

        Coroutine _co;

        void Reset()
        {
            group = GetComponent<CanvasGroup>();
            label = GetComponentInChildren<TMP_Text>();
        }

        void Awake()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;
        }

        void OnEnable()
        {
            if (Rebuild.Saving.DataPersistenceManager.Instance != null)
                Rebuild.Saving.DataPersistenceManager.Instance.OnSaved += HandleSaved;
        }

        void OnDisable()
        {
            if (Rebuild.Saving.DataPersistenceManager.Instance != null)
                Rebuild.Saving.DataPersistenceManager.Instance.OnSaved -= HandleSaved;
        }

        void HandleSaved()
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Flash("저장됨!"));
        }

        public void ShowMessage(string msg)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Flash(msg));
        }

        IEnumerator Flash(string msg)
        {
            if (label != null) label.text = msg;
            if (group == null) yield break;
            // Fade in.
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            group.alpha = 1f;
            // Hold.
            yield return new WaitForSecondsRealtime(showDuration);
            // Fade out.
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / fadeOut);
                yield return null;
            }
            group.alpha = 0f;
            _co = null;
        }
    }
}
