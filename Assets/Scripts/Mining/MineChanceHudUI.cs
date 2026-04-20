using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{
    /// <summary>8-ore drop chance HUD shown on the right side of the Mine panel.
    /// Mirrors original <c>TheMine.UpdateChances()</c>: a small column of (ore icon, "33.5%")
    /// rows that reflects the current drop distribution. When introduces
    /// SkillTree-gated weights, the same UI will surface them automatically by listening to
    /// <see cref="SkillTreeManager.OnUpgradeChanged"/>.
    /// <para>Read-only — clicks pass through. Activates with the parent MinePanel; subscribes to
    /// SkillTreeManager only while active so re-purchase flickering doesn't fire updates while
    /// the player is on another tab.</para>
    /// </summary>
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
            // Icon sprite + ore tint follow the same SpriteLibrary fallback as ResourcePanelUI.
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (row == null || row.icon == null) continue;
                if (row.icon.sprite != null) continue; // user wired explicitly
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
