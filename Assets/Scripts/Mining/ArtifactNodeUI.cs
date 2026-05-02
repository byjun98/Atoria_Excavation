using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>one slot in the Artifacts tab grid (MainMenu/TabPanels/ArtifactsPanel).
    /// Subscribes to ArtifactsManager.OnArtifactFound so newly-discovered artifacts light up live.</summary>
    public sealed class ArtifactNodeUI : MonoBehaviour
    {
        [SerializeField] ArtifactId id;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] GameObject unfoundOverlay; // dim/silhouette layer

        static readonly Color UnfoundIconColor = new Color(0.18f, 0.18f, 0.2f, 1f);

        public ArtifactId Id => id;

        public void SetId(ArtifactId newId)
        {
            id = newId;
            ApplyStaticInfo();
            Refresh();
        }

        void Awake()
        {
            ApplyStaticInfo();
        }

        void OnEnable()
        {
            if (ArtifactsManager.Instance != null)
                ArtifactsManager.Instance.OnArtifactFound += HandleFound;
            Refresh();
        }

        void OnDisable()
        {
            if (ArtifactsManager.Instance != null)
                ArtifactsManager.Instance.OnArtifactFound -= HandleFound;
        }

        void ApplyStaticInfo()
        {
            var info = ArtifactDef.Get(id);
            if (iconImage != null)
            {
                // user-supplied artifact PNG via Resources (Artifacts/<EnumName>).
                // Falls back to the rock sprite + tint pattern.
                var custom = SpriteLibrary.GetArtifactSprite(id);
                iconImage.sprite = custom;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            if (nameLabel != null) nameLabel.text = info.Name;
            if (descLabel != null) descLabel.text = info.Description;
        }

        void HandleFound(ArtifactId found) { if (found == id) Refresh(); }

        public void Refresh()
        {
            bool hasIt = ArtifactsManager.Instance != null && ArtifactsManager.Instance.HasArtifact(id);
            var info = ArtifactDef.Get(id);
            if (iconImage != null)
            {
                // If a custom PNG is in place, render it at full saturation when found and gray-tinted
                // when locked. The placeholder rock sprite keeps the original tint behavior.
                var custom = SpriteLibrary.GetArtifactSprite(id);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                if (hasCustom) iconImage.color = hasIt ? Color.white : new Color(0.4f, 0.4f, 0.45f, 1f);
                else iconImage.color = hasIt ? info.Tint : UnfoundIconColor;
            }
            if (unfoundOverlay != null) unfoundOverlay.SetActive(!hasIt);
            if (nameLabel != null) nameLabel.text = hasIt ? info.Name : "???";
            if (descLabel != null) descLabel.text = hasIt ? info.Description : "발굴 중\n찾아보세요.";
        }
    }
}
