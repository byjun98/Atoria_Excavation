using Rebuild.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    /// <summary>first-boot tutorial overlay. Lives on Canvas_Tutorial (sortingOrder 310,
    /// just below the circle transition / save toast). Walks the player through 3 inline frames
    /// (Welcome, How to Mine, How to Use Talents). Subscribes to <see cref="TutorialManager.OnRequestShow"/>
    /// so DataPersistenceManager.OnLoaded can trigger the panel once on first launch.</summary>
    public sealed class TutorialFrameUI : MonoBehaviour
    {
        [System.Serializable]
        public struct Frame
        {
            public string title;
            [TextArea(2, 5)] public string body;
        }

        [SerializeField] GameObject panelRoot;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text pageIndicator;
        [SerializeField] TMP_Text nextButtonLabel;
        [SerializeField] Button nextButton;
        [SerializeField] Button skipButton;
        // frame 별 title overlay PNG. Render 시 매칭되는 sprite 있으면 titleImage 활성
        // + titleText hide. 없으면 TMP fallback. frames index 0,1 이 Welcome / OreAndBars 매핑.
        [SerializeField] UnityEngine.UI.Image titleImage;

        // Default copy — 6 frame 한글 본문 (mining/oreAndCrafting/
        // talent/anvil/mine/artifact).
        [SerializeField] Frame[] frames = new Frame[]
        {
            // frame 0 — 첫 mining session 진입 시 환영 + 게임 룰
            new Frame { title = "아토리아 발굴단에\n오신 것을 환영합니다!",
                body = "게임은 간단합니다. 채굴 세션이 시작되면 커서를 바위 위에 올려 채굴하세요.\n\n커서를 따라 원이 움직입니다. 이 원이 채굴 구역이며, 구역 안의 바위는 곡괭이가 자동으로 채굴합니다.\n\n우측 상단의 타이머가 0이 되면 채굴 세션이 종료됩니다." },
            // frame 1 — 첫 세션 종료 후 광석/바 변환 설명
            new Frame { title = "광석과 주괴!",
                body = "바위를 채굴하면 광석을 획득합니다.\n\n바위는 일반 바위, 덩어리 바위, 완전 자원 바위의 3종류가 있습니다.\n\n일반 바위는 광석 1개, 덩어리 바위는 3개, 완전 자원 바위는 7개를 줍니다.\n\n광석은 자동으로 주괴가 됩니다. 3개의 광석이 1개의 주괴로 제작됩니다. 잉여 광석이 있으면 추가 주괴가 만들어집니다. 예: 광석 10개 → 주괴 4개." },
            // frame 2 — talent 탭 첫 진입 (LV 1 도달 후)
            new Frame { title = "특성",
                body = "레벨이 오르면 특성 포인트를 획득합니다.\n\n세션 종료 후 무작위 3종 특성 카드 중 1종을 선택하면 영구적으로 적용됩니다.\n\n특성은 곡괭이 강화, 광석 추가 드롭, 자동 채굴 등 다양한 능력을 부여합니다." },
            // frame 3 — anvil 탭 첫 진입 (곡괭이 강화 1+ 구매 후)
            new Frame { title = "복원공방",
                body = "주괴를 모아 곡괭이를 복원할 수 있습니다.\n\n곡괭이는 1~14단계가 있으며, 단계가 오를수록 채굴 피해와 영역이 커집니다.\n\n버튼을 길게 눌러 곡괭이 복원을 시작하세요. 게이지가 가득 차면 완성." },
            // frame 4 — mine 탭 첫 진입 (항상)
            new Frame { title = "자동 발굴터",
                body = "채굴 세션 밖에서도 광석을 모을 수 있는 자동 발굴터입니다.\n\n시간이 지나면 자동으로 광석을 발굴합니다. 주괴를 투자하여 발굴 속도와 산출량을 개선할 수 있습니다." },
            // frame 5 — artifact 탭 첫 진입 (첫 유물 발견 후)
            new Frame { title = "유물",
                body = "발굴 중 매우 낮은 확률로 고대 유물이 발견됩니다.\n\n유물은 영구적 보너스를 부여합니다. 14종의 유물을 모두 모으는 것이 발굴단의 궁극적 목표입니다.\n\n발견된 유물은 박물관 진열대에 보존됩니다." },
        };

        int _index;

        void Awake()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNext);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(Finish);
            }
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        // currently displaying frame index (0..5). frames[0..N-1] map to the 6
        // context-triggered frames; if frames array is shorter the renderer falls back to a
        // generic "tutorial closed" path on Finish.
        int _activeFrameIdx;

        void OnEnable()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnRequestShowFrame += ShowFrame;
        }

        void OnDisable()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnRequestShowFrame -= ShowFrame;
        }

        public void ShowFrame(int frameIdx)
        {
            _activeFrameIdx = frameIdx;
            _index = Mathf.Clamp(frameIdx, 0, Mathf.Max(0, frames != null ? frames.Length - 1 : 0));
            if (panelRoot != null) panelRoot.SetActive(true);
            Render();
        }

        public void Show() => ShowFrame(0);

        void OnNext()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            _index++;
            if (_index >= frames.Length) { Finish(); return; }
            Render();
        }

        void Render()
        {
            if (frames == null || frames.Length == 0) { Finish(); return; }
            int i = Mathf.Clamp(_index, 0, frames.Length - 1);
            var f = frames[i];
            if (titleText != null) titleText.text = f.title;
            if (bodyText != null) bodyText.text = f.body;
            if (pageIndicator != null) pageIndicator.text = $"{i + 1} / {frames.Length}";
            if (nextButtonLabel != null) nextButtonLabel.text = (i == frames.Length - 1) ? "확인!" : "다음";

            // frame title overlay PNG. 매칭되는 sprite 있으면 titleImage 활성 + TMP hide.
            var titleSprite = ResolveTitleSprite(i);
            if (titleImage != null)
            {
                if (titleSprite != null)
                {
                    titleImage.sprite = titleSprite;
                    titleImage.preserveAspect = true;
                    titleImage.gameObject.SetActive(true);
                    if (titleText != null) titleText.gameObject.SetActive(false);
                }
                else
                {
                    titleImage.gameObject.SetActive(false);
                    if (titleText != null) titleText.gameObject.SetActive(true);
                }
            }
        }

        static Sprite ResolveTitleSprite(int frameIdx)
        {
            string name = frameIdx switch
            {
                0 => "Header_Welcome",
                1 => "Header_OreAndBars",
                _ => null, // 다른 frame 은 TMP_Text 유지 (PNG 미작성)
            };
            return name != null ? Rebuild.Mining.SpriteLibrary.GetTypography(name) : null;
        }

        void Finish()
        {
            Rebuild.Audio.AudioManager.Instance?.Play("UI_Click1");
            if (panelRoot != null) panelRoot.SetActive(false);
            // mark only the frame that was just shown as seen. Other frames keep
            // their pressedOk flag intact so the context triggers still fire.
            if (TutorialManager.Instance != null) TutorialManager.Instance.MarkSeen(_activeFrameIdx);
        }
    }
}
