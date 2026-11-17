using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{

    public sealed class ArtifactMuseum3DGallery : MonoBehaviour
    {
        public const int ExhibitLayer = 29;
        public const int AtlasSize = 1024;
        const float FrameInterval = 1f / 20f;
        [SerializeField] Camera previewCamera;
        [SerializeField] Transform stageRoot;
        [SerializeField] RawImage[] previews;
        [SerializeField] Transform[] turntables;
        RenderTexture atlas;
        bool dirty;
        int focusedIndex = -1;
        float nextRenderAt, lastRotationAt;

        public RenderTexture Atlas => atlas;
        public Camera PreviewCamera => previewCamera;
        public int FocusedIndex => focusedIndex;
        public int RenderCount { get; private set; }

        public void Configure(Camera camera, Transform stage, RawImage[] images, Transform[] models)
        {
            previewCamera = camera;
            stageRoot = stage;
            previews = images;
            turntables = models;
            previewCamera.enabled = false;
            for (int i = 0; i < previews.Length; i++)
                if (previews[i] != null) previews[i].uvRect = CellRect(i);
            if (Application.isPlaying && isActiveAndEnabled) Activate();
        }

        public Transform GetDisplay(int index) =>
            turntables != null && index >= 0 && index < turntables.Length && turntables[index] != null
                ? turntables[index].parent : null;

        public static Rect CellRect(int index)
        {

            const float inset = 1f / AtlasSize;
            return new Rect(index % 4 * .25f + inset, (3 - index / 4) * .25f + inset,
                .25f - 2f * inset, .25f - 2f * inset);
        }

        void OnEnable()
        {
            if (Application.isPlaying && previewCamera != null) Activate();
        }

        void Activate()
        {
            if (atlas == null)
            {
                atlas = new RenderTexture(AtlasSize, AtlasSize, 16, RenderTextureFormat.ARGB32)
                {
                    name = "Museum Exhibit Atlas",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    antiAliasing = 2,
                    useMipMap = false,
                    hideFlags = HideFlags.DontSave
                };
            }
            if (!atlas.IsCreated()) atlas.Create();
            previewCamera.enabled = false;
            previewCamera.targetTexture = atlas;
            foreach (var preview in previews)
                if (preview != null) preview.texture = atlas;
            stageRoot.gameObject.SetActive(true);
            dirty = true;
            nextRenderAt = Time.unscaledTime;
        }

        public void RequestRender()
        {
            if (isActiveAndEnabled) dirty = true;
        }

        public void SetFocus(int index, bool focused)
        {
            if (!isActiveAndEnabled || turntables == null || index < 0 || index >= turntables.Length) return;
            if (focused && focusedIndex != index)
            {
                focusedIndex = index;
                lastRotationAt = Time.unscaledTime;
            }
            else if (!focused && focusedIndex == index) focusedIndex = -1;
        }

        void LateUpdate()
        {
            if (atlas == null || previewCamera == null) return;
            float now = Time.unscaledTime;
            if (now < nextRenderAt) return;
            if (!atlas.IsCreated())
            {
                atlas.Create();
                dirty = true;
            }
            if (focusedIndex >= 0 && turntables[focusedIndex] != null)
            {
                turntables[focusedIndex].Rotate(0, 32f * (now - lastRotationAt), 0, Space.Self);
                lastRotationAt = now;
                dirty = true;
            }
            if (!dirty) return;
            previewCamera.Render();
            RenderCount++;
            dirty = false;
            nextRenderAt = now + FrameInterval;
        }

        void OnDisable()
        {
            focusedIndex = -1;
            dirty = false;
            if (previewCamera != null) previewCamera.enabled = false;
            if (stageRoot != null) stageRoot.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (previewCamera != null) previewCamera.targetTexture = null;
            if (previews != null)
                foreach (var preview in previews)
                    if (preview != null) preview.texture = null;
            if (atlas == null) return;
            atlas.Release();
            if (Application.isPlaying) Destroy(atlas);
            else DestroyImmediate(atlas);
            atlas = null;
        }
    }
}
