using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{








    [DisallowMultipleComponent]
    public sealed class PointerHoldForwarder : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action OnDown;
        public Action OnUp;
        public Action OnExit;

        public void OnPointerDown(PointerEventData eventData) => OnDown?.Invoke();
        public void OnPointerUp(PointerEventData eventData) => OnUp?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke();

        void OnDisable()
        {


            OnUp?.Invoke();
        }
    }
}
