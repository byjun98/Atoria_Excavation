using System.Collections;
using Rebuild.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
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
        [SerializeField] Image titleImage;
        [SerializeField] Button closeButton;
        [SerializeField] Frame[] frames = CreateMuseumFrames();

        int _activeFrameIdx;
        bool _showing;
        Coroutine _bodyReveal;

        static Frame[] CreateMuseumFrames() => new[]
        {
            new Frame { title = "발굴 방법", body = "커서를 <color=#78B99B>발굴지 위</color>에 올리면 곡괭이가 자동으로 발굴합니다.\n발굴 시간이 끝나면 모은 원석을 정산합니다." },
            new Frame { title = "원석에서 주괴로", body = "발굴한 원석은 정산할 때 <color=#78B99B>주괴</color>로 바뀝니다.\n주괴로 스킬을 구매하거나 곡괭이를 복원할 수 있습니다." },
            new Frame { title = "나만의 특성", body = "레벨이 오르면 <color=#78B99B>특성 카드</color>를 선택할 수 있습니다.\n선택한 특성은 이후 발굴에도 적용됩니다." },
            new Frame { title = "복원공방", body = "주괴를 모아 곡괭이를 복원하세요.\n<color=#78B99B>복원 버튼을 길게 누르면</color> 더 강한 곡괭이가 완성됩니다." },
            new Frame { title = "자동 발굴터", body = "자동 발굴터는 일정한 간격으로 주괴를 생산합니다.\n주괴를 써서 <color=#78B99B>발굴 주기를 줄이고 산출량을 늘릴 수 있습니다.</color>" },
            new Frame { title = "유물 전시", body = "발굴 중에 발견한 <color=#78B99B>유물</color>의 효과는 이후에도 계속 적용됩니다.\n모은 유물과 그 효과는 유물 전시에서 확인할 수 있습니다." }
        };

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            BindButtons();
        }

        void BindButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnNext);
                nextButton.onClick.AddListener(OnNext);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(Finish);
                skipButton.onClick.AddListener(Finish);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Finish);
                closeButton.onClick.AddListener(Finish);
            }
        }

        public void ConfigureMuseumTutorial()
        {
            frames = CreateMuseumFrames();
            BindButtons();
            if (titleImage != null) titleImage.gameObject.SetActive(false);
            if (titleText != null) { titleText.gameObject.SetActive(true); titleText.color = MuseumUiTheme.Text; }
            if (bodyText != null)
            {
                bodyText.color = MuseumUiTheme.Text;
                bodyText.richText = true;
                bodyText.textWrappingMode = TextWrappingModes.Normal;
                bodyText.lineSpacing = 6f;
            }
            if (pageIndicator != null) pageIndicator.color = MuseumUiTheme.Muted;
            if (nextButtonLabel != null) nextButtonLabel.color = MuseumUiTheme.Panel;
        }

        public void ConfigureCloseButton(Button button)
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Finish);
            closeButton = button;
            BindButtons();
        }

        void OnEnable()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnRequestShowFrame += ShowFrame;
        }

        void OnDisable()
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnRequestShowFrame -= ShowFrame;
            StopBodyReveal();
            _showing = false;
        }

        public void ShowFrame(int frameIdx)
        {
            if (frames == null || frameIdx < 0 || frameIdx >= frames.Length) return;
            StopBodyReveal();
            _activeFrameIdx = frameIdx;
            _showing = true;
            if (panelRoot != null) panelRoot.SetActive(true);
            var frame = frames[frameIdx];
            if (titleImage != null) titleImage.gameObject.SetActive(false);
            if (titleText != null) { titleText.gameObject.SetActive(true); titleText.text = frame.title; }
            if (pageIndicator != null) pageIndicator.text = "발굴 안내";
            if (nextButtonLabel != null) nextButtonLabel.text = "바로 보기";
            if (bodyText != null)
            {
                bodyText.text = frame.body;
                bodyText.maxVisibleCharacters = 0;
                bodyText.ForceMeshUpdate();
                if (isActiveAndEnabled && bodyText.textInfo.characterCount > 1)
                {
                    _bodyReveal = StartCoroutine(RevealBody());
                    return;
                }
            }
            ShowFullBody();
        }

        public void Show() => ShowFrame(0);

        IEnumerator RevealBody()
        {
            var characters = bodyText.textInfo.characterInfo;
            int characterCount = bodyText.textInfo.characterCount;
            int visible = 0;
            float delay = 0f;

            while (visible < characterCount)
            {
                while (delay <= 0f && visible < characterCount)
                {
                    delay += characters[visible++].character switch
                    {
                        '.' or '!' or '?' => .12f,
                        ',' or ':' => .06f,
                        '\n' => 0f,
                        _ => .025f
                    };
                }
                bodyText.maxVisibleCharacters = visible;
                if (visible == characterCount) break;

                yield return null;
                delay -= Time.unscaledDeltaTime;
            }

            _bodyReveal = null;
            ShowFullBody();
        }

        void StopBodyReveal()
        {
            if (_bodyReveal == null) return;
            StopCoroutine(_bodyReveal);
            _bodyReveal = null;
        }

        void ShowFullBody()
        {
            if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
            if (nextButtonLabel != null) nextButtonLabel.text = "확인";
        }

        void OnNext()
        {
            if (!_showing) return;
            Rebuild.Audio.AudioManager.Instance?.Play("Click_1");
            if (_bodyReveal != null)
            {
                StopBodyReveal();
                ShowFullBody();
                return;
            }
            Finish();
        }

        void Finish()
        {
            if (!_showing) return;
            _showing = false;
            StopBodyReveal();
            if (panelRoot != null) panelRoot.SetActive(false);
            Rebuild.Audio.AudioManager.Instance?.Play("UI_Click1");
            TutorialManager.Instance?.MarkSeen(_activeFrameIdx);
        }
    }
}
