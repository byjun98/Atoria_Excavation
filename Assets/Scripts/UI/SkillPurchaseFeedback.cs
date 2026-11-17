using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rebuild.UI
{

    public sealed class SkillPurchaseFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] SkillPurchaseFeedbackPlayer sharedPlayer;
        [SerializeField] Image sourceIcon;

        public void Configure(SkillPurchaseFeedbackPlayer player, Image icon)
        {
            sharedPlayer = player;
            sourceIcon = icon;
        }

        public void PlayBought(bool completed)
        {
            if (isActiveAndEnabled && sharedPlayer != null)
                sharedPlayer.Play(this, sourceIcon, true, completed);
        }

        public void OnPointerEnter(PointerEventData eventData) => PlaySelection();
        public void OnSelect(BaseEventData eventData) => PlaySelection();

        void PlaySelection()
        {
            if (isActiveAndEnabled && sharedPlayer != null)
                sharedPlayer.Play(this, sourceIcon, false, false);
        }

        void OnDisable()
        {
            if (sharedPlayer != null) sharedPlayer.StopFor(this);
        }
    }
}
