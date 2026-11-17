using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



namespace Rebuild.UI
{



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
            _co = StartCoroutine(Flash("저장했습니다."));
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

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            group.alpha = 1f;

            yield return new WaitForSecondsRealtime(showDuration);

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
