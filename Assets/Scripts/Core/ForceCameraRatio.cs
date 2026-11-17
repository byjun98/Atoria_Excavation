using UnityEngine;

namespace Rebuild.Core
{
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public sealed class ForceCameraRatio : MonoBehaviour
    {
        [SerializeField] float targetAspect = 16f / 9f;

        Camera cam;
        int lastW, lastH;

        void OnEnable()
        {
            cam = GetComponent<Camera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width == lastW && Screen.height == lastH) return;
            Apply();
        }

        void Apply()
        {
            if (cam == null) cam = GetComponent<Camera>();
            lastW = Screen.width;
            lastH = Screen.height;

            float windowAspect = (float)Screen.width / Screen.height;
            float scaleHeight = windowAspect / targetAspect;

            if (scaleHeight < 1f)
            {

                Rect r = cam.rect;
                r.width = 1f;
                r.height = scaleHeight;
                r.x = 0f;
                r.y = (1f - scaleHeight) / 2f;
                cam.rect = r;
            }
            else
            {

                float scaleWidth = 1f / scaleHeight;
                Rect r = cam.rect;
                r.width = scaleWidth;
                r.height = 1f;
                r.x = (1f - scaleWidth) / 2f;
                r.y = 0f;
                cam.rect = r;
            }
        }
    }
}
