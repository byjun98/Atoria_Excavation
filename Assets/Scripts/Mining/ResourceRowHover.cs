using UnityEngine;
using UnityEngine.EventSystems;

namespace Rebuild.Mining
{
    public sealed class ResourceRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] ResourcePanelUI panel;
        [SerializeField] int index;

        public void Configure(ResourcePanelUI owner, int rowIndex)
        {
            panel = owner;
            index = rowIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (panel != null) panel.ShowResourceTooltip(index, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (panel != null) panel.HideResourceTooltip(index);
        }

        void OnDisable()
        {
            if (panel != null) panel.HideResourceTooltip(index);
        }
    }
}
