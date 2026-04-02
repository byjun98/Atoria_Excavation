using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{
    /// <summary>generic pointer hold/release relay. Attached to a UGUI element that
    /// owns a <c>Button</c> (or any Selectable / raycast target); whoever wants to know when the
    /// user holds vs releases assigns the <see cref="OnDown"/>, <see cref="OnUp"/>, and
    /// <see cref="OnExit"/> delegates at runtime.
    /// <para>Unity's <c>Button.onClick</c> fires on release; for hold-to-craft we need separate
    /// down + up events. We can't subclass <c>Button</c> without losing the visual transition
    /// presets, so this forwarder sits next to the Button on the same GameObject.</para>
    /// </summary>
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
            // When the panel hides while the user has a finger down, OnPointerUp never fires.
            // Treat the disable as a release so the caller can clean up the hold state.
            OnUp?.Invoke();
        }
    }
}
