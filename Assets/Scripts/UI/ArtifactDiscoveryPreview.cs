using Rebuild.Mining;
using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.UI
{
    [RequireComponent(typeof(RawImage))]
    public sealed class ArtifactDiscoveryPreview : MonoBehaviour
    {
        public const int DiscoveryLayer = 30;
        const float DegreesPerSecond = 30f;
        const float FrameInterval = 1f / 30f;
        const int TextureSize = 512;

        [SerializeField] ArtifactMuseum3DGallery gallery;

        RawImage _image;
        GameObject _stage;
        Camera _camera;
        RenderTexture _texture;
        Transform _display;
        Transform _turntable;
        ArtifactId _id;
        float _startedAt;
        float _nextRenderAt;

        public bool Show(ArtifactId id)
        {
            var source = gallery != null ? gallery.GetDisplay((int)id) : null;
            if (source == null || gallery.PreviewCamera == null) return false;

            if (_stage == null) CreateStage(gallery.PreviewCamera);
            if (_display == null || id != _id)
            {
                if (_display != null)
                {
                    _display.gameObject.SetActive(false);
                    Destroy(_display.gameObject);
                }
                _id = id;
                _display = Instantiate(source, _stage.transform, false);
                foreach (var child in _display.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = DiscoveryLayer;
                _turntable = _display.Find("Turntable");
            }

            _stage.SetActive(true);
            if (!_texture.IsCreated()) _texture.Create();
            _image.texture = _texture;
            _image.enabled = true;
            _startedAt = Time.unscaledTime;
            Render(_startedAt);
            return true;
        }

        void CreateStage(Camera source)
        {
            _image = GetComponent<RawImage>();
            _stage = new GameObject("ArtifactDiscoveryStage") { hideFlags = HideFlags.DontSave };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_stage, gameObject.scene);
            _stage.SetActive(false);
            _stage.transform.position = new Vector3(0f, -1000f, 0f);

            var cameraObject = new GameObject("DiscoveryCamera", typeof(Camera));
            cameraObject.transform.SetParent(_stage.transform, false);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.CopyFrom(source);
            cameraObject.transform.SetLocalPositionAndRotation(new Vector3(0f, -.05f, -30f), Quaternion.identity);
            _camera.enabled = false;
            _camera.orthographicSize = 1.78f;
            _camera.cullingMask = 1 << DiscoveryLayer;

            foreach (var original in source.transform.parent.GetComponentsInChildren<Light>(true))
            {
                var light = Instantiate(original, _stage.transform, false);
                light.cullingMask = _camera.cullingMask;
            }

            _texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = "Artifact Discovery Preview",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 2,
                hideFlags = HideFlags.DontSave
            };
            _camera.targetTexture = _texture;
        }

        void LateUpdate()
        {
            if (_stage == null || !_stage.activeSelf) return;
            float now = Time.unscaledTime;
            if (now >= _nextRenderAt) Render(now);
        }

        void Render(float now)
        {
            if (_turntable != null)
                _turntable.localRotation = Quaternion.AngleAxis((now - _startedAt) * DegreesPerSecond, Vector3.up);
            _camera.Render();
            _nextRenderAt = now + FrameInterval;
        }

        void OnDisable()
        {
            if (_stage != null) _stage.SetActive(false);
            if (_image != null) _image.enabled = false;
            if (_texture != null) _texture.Release();
        }

        void OnDestroy()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_stage != null) Destroy(_stage);
            if (_texture != null) Destroy(_texture);
        }
    }
}
