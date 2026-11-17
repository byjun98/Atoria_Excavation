using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{









    public sealed class MineChanceHudUI : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public Image icon;
            public TMP_Text label;
        }

        [SerializeField] Row[] rows = new Row[8];

        bool _subscribed;

        void OnEnable()
        {
            if (SkillTreeManager.Instance != null)
            {
                SkillTreeManager.Instance.OnUpgradeChanged += HandleUpgradeChanged;
                _subscribed = true;
            }
            EnsureIcons();
            RefreshAll();
        }

        void OnDisable()
        {
            if (_subscribed && SkillTreeManager.Instance != null)
            {
                SkillTreeManager.Instance.OnUpgradeChanged -= HandleUpgradeChanged;
                _subscribed = false;
            }
        }

        void HandleUpgradeChanged(UpgradeId _, int __) => RefreshAll();

        void EnsureIcons()
        {

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || row.icon == null) continue;
                if (row.icon.sprite != null) continue;
                var ore = (OreType)i;
                var hud = SpriteLibrary.GetOreHudIcon(ore);
                if (hud != null)
                {
                    row.icon.sprite = hud;
                    row.icon.color = Color.white;
                    row.icon.preserveAspect = true;
                }
                else
                {
                    row.icon.sprite = RockSpriteFactory.GetRockSprite();
                    row.icon.color = RockSpriteFactory.GetOreColor(ore);
                    row.icon.preserveAspect = false;
                }
                row.icon.raycastTarget = false;
            }
        }

        void RefreshAll()
        {
            var weights = MineManager.GetOreWeights();
            if (weights == null) return;
            int total = 0;
            for (int i = 0; i < weights.Length; i++) total += weights[i].weight;
            if (total <= 0) total = 1;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null || rows[i].label == null) continue;
                int w = i < weights.Length ? weights[i].weight : 0;
                float pct = (w * 100f) / total;
                rows[i].label.text = $"{pct:0.0}%";
            }
        }
    }
}
