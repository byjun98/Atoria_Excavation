using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>Talents tab grid cell.
    /// Subscribes to LevelMechanics.OnTalentChosen to light up the moment the player picks the matching card.</summary>
    public sealed class TalentNodeUI : MonoBehaviour
    {
        [SerializeField] TalentId id;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] GameObject unchosenOverlay;

        static readonly Color UnchosenIconColor = new Color(0.18f, 0.18f, 0.2f, 1f);

        // (2026-05-15) — track whether ApplyStaticInfo wired a user-authored PNG so
        // Refresh keeps the art's full saturation when chosen (color = white) instead of overwriting
        // with the talent tint. PNG 없을 땐 기존 fallback (rock silhouette + tint) 그대로.
        bool _hasUserIcon;

        public TalentId Id => id;

        public void SetId(TalentId newId)
        {
            id = newId;
            ApplyStaticInfo();
            Refresh();
        }

        void Awake() { ApplyStaticInfo(); }

        void OnEnable()
        {
            if (LevelMechanics.Instance != null)
                LevelMechanics.Instance.OnTalentChosen += HandleChosen;
            Refresh();
        }

        void OnDisable()
        {
            if (LevelMechanics.Instance != null)
                LevelMechanics.Instance.OnTalentChosen -= HandleChosen;
        }

        void ApplyStaticInfo()
        {
            var info = TalentDef.Get(id);
            if (iconImage != null)
            {
                // prefer the user-authored Talents/{TalentId}.png. Falls back to
                // the rock silhouette (which still gets tinted by Refresh() with talent.Tint).
                var userIcon = SpriteLibrary.GetTalentIcon(id);
                if (userIcon != null)
                {
                    iconImage.sprite = userIcon;
                    iconImage.preserveAspect = true;
                    _hasUserIcon = true;
                }
                else
                {
                    iconImage.sprite = RockSpriteFactory.GetRockSprite();
                    iconImage.preserveAspect = false;
                    _hasUserIcon = false;
                }
                iconImage.raycastTarget = false;
            }
            if (nameLabel != null) nameLabel.text = info.Name;
            if (descLabel != null) descLabel.text = info.Description;
        }

        void HandleChosen(TalentId chosen) { if (chosen == id) Refresh(); }

        public void Refresh()
        {
            bool chosen = LevelMechanics.Instance != null && LevelMechanics.Instance.HasTalent(id);
            var info = TalentDef.Get(id);
            if (iconImage != null)
            {
                // user PNG 는 자체 색감이 있으므로 chosen 시 Color.white (원본 그대로),
                // unchosen 시에만 silhouette 처리 (어두운 회청색). PNG 없을 땐 기존 tint/UnchosenIconColor.
                if (_hasUserIcon)
                    iconImage.color = chosen ? Color.white : UnchosenIconColor;
                else
                    iconImage.color = chosen ? info.Tint : UnchosenIconColor;
            }
            if (unchosenOverlay != null) unchosenOverlay.SetActive(!chosen);
            if (nameLabel != null) nameLabel.text = chosen ? info.Name : "???";
            if (descLabel != null) descLabel.text = chosen ? info.Description : "레벨업 시\n선택하세요.";
        }
    }
}
