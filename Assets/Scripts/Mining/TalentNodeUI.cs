using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{


    public sealed class TalentNodeUI : MonoBehaviour
    {
        [SerializeField] TalentId id;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text descLabel;
        [SerializeField] GameObject unchosenOverlay;

        static readonly Color UnchosenIconColor = new Color(0.18f, 0.18f, 0.2f, 1f);




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


                if (_hasUserIcon)
                    iconImage.color = chosen ? Color.white : UnchosenIconColor;
                else
                    iconImage.color = chosen ? info.Tint : UnchosenIconColor;
            }
            if (unchosenOverlay != null) unchosenOverlay.SetActive(!chosen);
            if (nameLabel != null) nameLabel.text = chosen ? info.Name : "???";
            if (descLabel != null) descLabel.text = chosen ? info.Description : "레벨이 오르면\n선택할 수 있습니다.";
        }
    }
}
