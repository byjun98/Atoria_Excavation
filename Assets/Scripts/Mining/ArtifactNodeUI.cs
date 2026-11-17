using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{


    public sealed class ArtifactNodeUI : MonoBehaviour
    {
        [SerializeField] ArtifactId id;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] GameObject unfoundOverlay;
        [SerializeField] Rebuild.UI.ArtifactMuseum3DPreview museumPreview;

        static readonly Color UnfoundIconColor = new Color(0.18f, 0.18f, 0.2f, 1f);

        public ArtifactId Id => id;

        public void ConfigureMuseumPreview(Rebuild.UI.ArtifactMuseum3DPreview preview)
        {
            museumPreview = preview;
            if (iconImage != null) iconImage.enabled = preview == null;
            Refresh();
        }

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
            if (iconImage != null && museumPreview == null)
            {


                var custom = SpriteLibrary.GetArtifactSprite(id);
                iconImage.sprite = custom;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            if (nameLabel != null) nameLabel.text = info.Name;
            if (descLabel != null) descLabel.text = info.Description;
        }

        void HandleFound(ArtifactId found) => Refresh();

        public void Refresh()
        {
            var artifacts = ArtifactsManager.Instance;
            bool hasIt = artifacts != null && artifacts.HasArtifact(id);
            if (museumPreview != null) museumPreview.SetFound(hasIt);
            var info = ArtifactDef.Get(id);
            if (iconImage != null && museumPreview == null)
            {


                var custom = SpriteLibrary.GetArtifactSprite(id);
                bool hasCustom = custom != null && custom != RockSpriteFactory.GetRockSprite();
                if (hasCustom) iconImage.color = hasIt ? Color.white : new Color(0.4f, 0.4f, 0.45f, 1f);
                else iconImage.color = hasIt ? info.Tint : UnfoundIconColor;
            }
            if (unfoundOverlay != null) unfoundOverlay.SetActive(!hasIt);
            if (nameLabel != null) nameLabel.text = hasIt ? DisplayName(info.Name) : "???";
            if (descLabel != null)
            {
                if (hasIt) descLabel.text = info.Description;
                else if (artifacts != null && artifacts.GetGuaranteedArtifact() == id)
                    descLabel.text = $"유물 획득이 보장되기까지\n{artifacts.MissesUntilGuarantee}회 남았습니다.";
                else descLabel.text = "발굴 중에\n찾을 수 있습니다.";
            }
        }

        string DisplayName(string name)
        {
            if (museumPreview == null) return name;
            if (id == ArtifactId.GoldRing) return name.Replace("(", "\n(");
            if (id == ArtifactId.PurpleRing) return name.Replace(" 삼층석탑", "\n삼층석탑");
            return name;
        }
    }
}
