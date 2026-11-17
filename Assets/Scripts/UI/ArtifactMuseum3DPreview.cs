using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.UI
{
    public sealed class ArtifactMuseum3DPreview : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        static readonly Color UnfoundTint = new Color(.52f, .54f, .55f, 1f);

        [SerializeField] ArtifactMuseum3DGallery gallery;
        [SerializeField] RawImage image;
        [SerializeField] int exhibitIndex;
        bool found;

        public void Configure(ArtifactMuseum3DGallery owner, RawImage preview, int index)
        {
            gallery = owner;
            image = preview;
            exhibitIndex = index;
        }

        public void SetFound(bool hasArtifact)
        {
            if (image != null) image.color = hasArtifact ? Color.white : UnfoundTint;
            if (found != hasArtifact && gallery != null) gallery.RequestRender();
            found = hasArtifact;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (gallery != null) gallery.SetFocus(exhibitIndex, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (gallery != null) gallery.SetFocus(exhibitIndex, false);
        }

        void OnDisable()
        {
            if (gallery != null) gallery.SetFocus(exhibitIndex, false);
        }
    }
}
