using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.UI
{






    [DisallowMultipleComponent]
    public sealed class HoverCursorReset : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {



            SystemCursorController.Instance?.ResetToSystemCursor();
        }

        public void OnPointerExit(PointerEventData eventData)
        {

            SystemCursorController.Instance?.ApplyCursor();
        }

        void OnDisable()
        {



            SystemCursorController.Instance?.ApplyCursor();
        }
    }
}
