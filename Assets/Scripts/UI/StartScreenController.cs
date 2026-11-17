using TMPro;
using Rebuild.Mining;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{







    [RequireComponent(typeof(Canvas))]
    public sealed class StartScreenController : MonoBehaviour
    {
        [Header("Auto-resolved if left empty")]
        [SerializeField] TextMeshProUGUI logo;
        [SerializeField] Button playGameBtn;
        [SerializeField] Button settingsBtn;
        [SerializeField] Button creditsBtn;
        [SerializeField] Button exitGameBtn;
        [SerializeField] ParticleSystem dustEffect;
        [SerializeField] AudioSource bgmSource;

        [Header("")]
        [SerializeField] GameObject mainMenuCanvasPlaceholder;

        void Awake()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null) canvas.worldCamera = Camera.main;

            AutoResolve();
            ConfigureLogo();

            var museumBackdrop = transform.Find("MuseumBackdrop");
            if (museumBackdrop == null)
            {
                ConfigureButtonLayout();
                ConfigureButtonLabels();
            }
            if (!TryAddMuseumRelicDrift(museumBackdrop))
                ConfigureDust();
            HookButtons();
        }

        void ConfigureButtonLabels()
        {

            SetButtonLabel(playGameBtn, "시작");
            SetButtonLabel(settingsBtn, "설정");
            SetButtonLabel(creditsBtn,  "크레딧");
            SetButtonLabel(exitGameBtn, "나가기");
        }

        void SetButtonLabel(Button btn, string text)
        {
            if (btn == null) return;
            if (HasLabeledStartButtonSprite(btn))
            {
                DisableTypographyFallbackText(btn.transform);
                DisableTypographyOverlay(btn.transform);
                return;
            }

            if (HasTypographyOverlay(btn.transform))
            {
                DisableTypographyFallbackText(btn.transform);
                return;
            }

            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (tmp == null) return;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            SetNoWrap(tmp);
            tmp.overflowMode = TextOverflowModes.Overflow;
            var mat = tmp.fontMaterial;
            mat.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 1f));
            mat.SetFloat("_OutlineWidth", 0.2f);
            tmp.ForceMeshUpdate(true, true);
        }

        void Start()
        {
            if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
                bgmSource.Play();
        }

        void AutoResolve()
        {
            if (logo == null)
            {
                var t = transform.Find("Logo");
                if (t != null) logo = t.GetComponent<TextMeshProUGUI>();
            }
            playGameBtn = playGameBtn  != null ? playGameBtn  : FindButton("startScreenBtns/PlayGame_btn");
            settingsBtn = settingsBtn  != null ? settingsBtn  : FindButton("startScreenBtns/Settings_btn");
            creditsBtn  = creditsBtn   != null ? creditsBtn   : FindButton("startScreenBtns/Credits_btn");
            exitGameBtn = exitGameBtn  != null ? exitGameBtn  : FindButton("startScreenBtns/ExitGame_btn");

            if (dustEffect == null)
            {
                var t = transform.Find("DustEffects_parent/DustEffect");
                if (t != null) dustEffect = t.GetComponent<ParticleSystem>();
            }

            if (bgmSource == null)
            {
                var bgmGo = GameObject.Find("BGM");
                if (bgmGo != null) bgmSource = bgmGo.GetComponent<AudioSource>();
            }
        }

        Button FindButton(string relativePath)
        {
            var t = transform.Find(relativePath);
            return t != null ? t.GetComponent<Button>() : null;
        }

        void ConfigureButtonLayout()
        {
            var btnsT = transform.Find("startScreenBtns") as RectTransform;
            if (btnsT != null)
            {
                btnsT.anchorMin = new Vector2(0.5f, 0.5f);
                btnsT.anchorMax = new Vector2(0.5f, 0.5f);
                btnsT.pivot = new Vector2(0.5f, 0.5f);
                btnsT.anchoredPosition = new Vector2(0f, -160f);
                btnsT.sizeDelta = new Vector2(360f, 580f);
                btnsT.localScale = new Vector3(0.8967f, 0.8967f, 0.8967f);
            }

            ConfigureButtonRect(playGameBtn, new Vector2(0f, 240f));
            ConfigureButtonRect(settingsBtn, new Vector2(0f, 80f));
            ConfigureButtonRect(creditsBtn, new Vector2(0f, -80f));
            ConfigureButtonRect(exitGameBtn, new Vector2(0f, -240f));
        }

        static void ConfigureButtonRect(Button btn, Vector2 anchoredPosition)
        {
            if (btn == null) return;
            var rt = btn.transform as RectTransform;
            if (rt == null) return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(320f, 160f);
            rt.localScale = Vector3.one;

            var hoverFx = btn.GetComponent<HoverFx>();
            if (hoverFx != null) hoverFx.RecacheBasePose();
        }

        void ConfigureLogo()
        {
            if (logo == null) return;
            if (HasTypographyOverlay(logo.transform))
            {
                logo.enabled = false;
                DisableTypographyFallbackText(logo.transform);
                return;
            }


            logo.text = "아토리아 발굴단";
            logo.alignment = TextAlignmentOptions.Center;
            SetNoWrap(logo);
            logo.overflowMode = TextOverflowModes.Overflow;


            var mat = logo.fontMaterial;
            mat.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 1f));
            mat.SetFloat("_OutlineWidth", 0.35f);
            logo.ForceMeshUpdate(true, true);
        }

        static void SetNoWrap(TMP_Text tmp)
        {



#if UNITY_2023_1_OR_NEWER
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
#else
            tmp.enableWordWrapping = false;
#endif
        }

        static bool HasTypographyOverlay(Transform root)
        {
            if (root == null) return false;
            var overlay = root.Find("TypographyImage");
            if (overlay == null || !overlay.gameObject.activeSelf) return false;
            var img = overlay.GetComponent<Image>();
            return img != null && img.enabled && img.sprite != null;
        }

        static bool HasLabeledStartButtonSprite(Button btn)
        {
            if (btn == null) return false;
            var image = btn.GetComponent<Image>();
            if (image == null || image.sprite == null) return false;

            var name = image.sprite.name;
            return name == "Btn_Start"
                || name == "Btn_Settings"
                || name == "Btn_Credits"
                || name == "Btn_Exit";
        }

        static void DisableTypographyFallbackText(Transform root)
        {
            if (root == null) return;
            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: true);
            for (int i = 0; i < tmps.Length; i++)
            {
                if (tmps[i].transform.parent != null &&
                    tmps[i].transform.parent.name == "TypographyImage")
                    continue;

                tmps[i].enabled = false;
                if (tmps[i].transform != root)
                    tmps[i].gameObject.SetActive(false);
            }
        }

        static void DisableTypographyOverlay(Transform root)
        {
            if (root == null) return;
            var overlay = root.Find("TypographyImage");
            if (overlay == null) return;
            var image = overlay.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = false;
                image.sprite = null;
            }
            overlay.gameObject.SetActive(false);
        }

        bool TryAddMuseumRelicDrift(Transform backdrop)
        {
            if (backdrop == null) return false;
            var relic = SpriteLibrary.GetArtifactSprite(ArtifactId.AncientDevice);
            if (relic == null) return false;

            if (dustEffect != null)
            {
                dustEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                dustEffect.gameObject.SetActive(false);
            }

            var effect = new GameObject("MuseumRelicDrift", typeof(RectTransform), typeof(Canvas), typeof(StartScreenRelicDrift));
            var rect = (RectTransform)effect.transform;
            var tint = transform.Find("MuseumBackdropTint");
            rect.SetParent(tint != null ? tint : backdrop, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            effect.GetComponent<StartScreenRelicDrift>().Initialize(relic);
            return true;
        }

        void ConfigureDust()
        {
            if (dustEffect == null) return;

            ApplyDustArtifactSprite();


            bool wasPlaying = dustEffect.isPlaying;
            if (wasPlaying) dustEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = dustEffect.main;
            main.loop = true;
            main.duration = 5f;
            main.startLifetime = 3f;
            main.startSpeed = 1.2f;
            main.startSize = 1.5f;
            main.startColor = new Color(0.85f, 0.78f, 0.6f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = dustEffect.emission;
            emission.rateOverTime = 6f;

            var shape = dustEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 0.5f, 0f);



            var velocityOverLifetime = dustEffect.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var sizeOverLifetime = dustEffect.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var curve = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.2f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLifetime = dustEffect.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            dustEffect.Play();
        }

        void ApplyDustArtifactSprite()
        {
            var renderer = dustEffect.GetComponent<ParticleSystemRenderer>();
            if (renderer == null) return;

            var sprite = SpriteLibrary.GetArtifactSprite(ArtifactId.AncientDevice);
            if (sprite == null || sprite.texture == null) return;

            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var material = renderer.material;
            var shader = Shader.Find("Sprites/Default");
            if (material == null && shader != null)
                material = new Material(shader);
            if (material == null) return;

            if (shader != null) material.shader = shader;
            material.mainTexture = sprite.texture;
            renderer.material = material;
        }

        void HookButtons()
        {
            if (playGameBtn != null)  playGameBtn.onClick.AddListener(OnPlayGame);
            if (settingsBtn != null)  settingsBtn.onClick.AddListener(OnSettings);
            if (creditsBtn  != null)  creditsBtn.onClick.AddListener(OnCredits);
            if (exitGameBtn != null)  exitGameBtn.onClick.AddListener(OnExitGame);
        }

        void OnPlayGame()
        {

            if (MainMenuController.Instance != null)
            {
                MainMenuController.Instance.PlayGame();
                return;
            }
            Debug.LogWarning("[Rebuild][] PlayGame clicked but MainMenuController.Instance is null. Run Tools > Rebuild > Setup Scene.");
            if (mainMenuCanvasPlaceholder != null) mainMenuCanvasPlaceholder.SetActive(true);
        }

        void OnSettings()
        {
            if (SettingsPanelUI.Instance != null) SettingsPanelUI.Instance.Show();
            else Debug.Log("[Rebuild][] title Settings clicked but SettingsPanelUI.Instance is null. Run Tools > Rebuild > Setup Scene.");
        }
        void OnCredits()  => Debug.Log("[Rebuild][] Credits clicked.");

        void OnExitGame()
        {
            Debug.Log("[Rebuild][] ExitGame clicked.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
