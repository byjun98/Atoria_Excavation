using UnityEngine;

namespace Rebuild.UI
{
    /// <summary>신라 단청 (丹靑) 톤 팔레트.
    /// 원본 게임의 violet/pink high-saturation 톤 대신 한국 전통 단청 (적·청·녹·금)
    /// 톤을 사용. 모든 Setup script 가 hardcoded color literal 대신 이 클래스 상수를
    /// 참조하면 일괄 톤 변경이 가능.
    /// <para>
    /// 단청은 신라/고려/조선의 전통 건축 채색법. 청(파랑) + 적(빨강) + 녹(초록) + 황(노랑) + 흑(검정) 5색이
    /// 기본. 황룡사·불국사·국립경주박물관 의 전시 분위기와 통일감.
    /// </para>
    /// </summary>
    public static class DancheongPalette
    {
        // ───── Background / panel ────────────────────────────────────────────
        /// <summary>진남색 — 배경 dim. 카메라 bg 와 가까움.</summary>
        public static readonly Color BgDeepNavy   = new Color32(0x1B, 0x2E, 0x5C, 0xFF); // #1B2E5C
        /// <summary>단청 갈색 — panel/frame 배경.</summary>
        public static readonly Color PanelBrown   = new Color32(0x2E, 0x1F, 0x1B, 0xFF); // #2E1F1B

        // ───── Card / node ────────────────────────────────────────────────────
        /// <summary>단청 녹 (forest) — idle 카드.</summary>
        public static readonly Color CardIdle     = new Color32(0x36, 0x5E, 0x3B, 0xFF); // #365E3B
        /// <summary>단청 녹 brighter — hover.</summary>
        public static readonly Color CardHover    = new Color32(0x4A, 0x7A, 0x4E, 0xFF); // #4A7A4E
        /// <summary>단청 적 (crimson) — selected / active card.</summary>
        public static readonly Color CardSelected = new Color32(0xC8, 0x44, 0x3A, 0xFF); // #C8443A
        /// <summary>회갈 — disabled.</summary>
        public static readonly Color Disabled     = new Color32(0x5A, 0x55, 0x50, 0xFF); // #5A5550

        // ───── Text ──────────────────────────────────────────────────────────
        /// <summary>신라 금색 — title / 강조 텍스트.</summary>
        public static readonly Color TitleGold    = new Color32(0xFF, 0xD6, 0x00, 0xFF); // #FFD600
        /// <summary>한지 흰 — 본문 텍스트.</summary>
        public static readonly Color BodyHanji    = new Color32(0xF5, 0xE6, 0xD3, 0xFF); // #F5E6D3
        /// <summary>짙은 갈색 outline (TMP Outline).</summary>
        public static readonly Color OutlineDark  = new Color32(0x3E, 0x2A, 0x1B, 0xFF); // #3E2A1B

        // ───── CTA / interactive ─────────────────────────────────────────────
        /// <summary>단청 적 — primary CTA (발굴 시작 등).</summary>
        public static readonly Color CtaPrimary   = new Color32(0xC8, 0x44, 0x3A, 0xFF); // #C8443A
        /// <summary>단청 적 brighter — CTA hover.</summary>
        public static readonly Color CtaHover     = new Color32(0xE0, 0x5A, 0x4D, 0xFF); // #E05A4D
        /// <summary>단청 청 — secondary 버튼 (취소/되돌리기).</summary>
        public static readonly Color CtaSecondary = new Color32(0x1B, 0x2E, 0x5C, 0xFF); // #1B2E5C

        // ───── Accent ────────────────────────────────────────────────────────
        /// <summary>옥색 — 신라 곡옥, 옥피리.</summary>
        public static readonly Color AccentJade       = new Color32(0x66, 0xCC, 0xAA, 0xFF); // #66CCAA
        /// <summary>주홍 — 단청 적 보다 밝은 highlight.</summary>
        public static readonly Color AccentVermillion = new Color32(0xE8, 0x4A, 0x39, 0xFF); // #E84A39
        /// <summary>금 — 신라 금관, 강조.</summary>
        public static readonly Color AccentGold       = new Color32(0xFF, 0xD6, 0x00, 0xFF); // #FFD600
    }
}
