using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{
    /// <summary>restores the OS cursor while the pointer hovers a button (e.g.
    /// EndSession) and re-applies the mining hand cursor on exit. Mirrors original
    /// <c>HoverClickable</c>'s <c>Cursor.SetCursor(null)</c> on Enter / SetCursor on Exit pattern.
    /// <para>Attach to a UGUI element that needs the standard arrow back during hover. Works
    /// alongside Button without needing a Selectable subclass.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverCursorReset : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            // While the pointer is over us, drop the hand cursor so the player knows this is
            // a click target (not the mining surface). SystemCursorController preserves its
            // _inMining flag so OnPointerExit restores the hand.
            SystemCursorController.Instance?.ResetToSystemCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Reapply whichever cursor the session state dictates (normal/gold hand or default).
            SystemCursorController.Instance?.ApplyCursor();
        }

        void OnDisable()
        {
            // If we hide while the pointer is still over us, the Exit event may never fire.
            // Force a reapply so the player isn't stuck staring at the system cursor when the
            // session is still active elsewhere.
            SystemCursorController.Instance?.ApplyCursor();
        }
    }
}
