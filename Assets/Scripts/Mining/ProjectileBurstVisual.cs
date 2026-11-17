using UnityEngine;
using UnityEngine.UI;

namespace Rebuild.Mining
{

    public sealed class ProjectileBurstVisual : MonoBehaviour
    {
        const int FrameSize = 96;
        const int FramesPerSecond = 24;
        const string SheetFolder = "AtoriaFX/";

        static readonly Sprite[][] s_sheets = new Sprite[4][];
        Image _image;
        Sprite[] _frames;

        public static bool TryBegin(RectTransform board, Vector3 worldOrigin, ProjectileType type,
            bool beamS, float sizeScale, float angle, ref ProjectileBurstVisual visual)
        {
            if (board == null) return false;
            int slot;
            float size;
            switch (type)
            {
                case ProjectileType.ElectricitySplash: slot = 0; size = 237f; break;
                case ProjectileType.Dynamite: slot = 1; size = 364f; break;
                case ProjectileType.BeamOfLight: slot = beamS ? 3 : 2; size = 246f; break;
                default: return false;
            }
            var frames = Load(slot);
            if (frames == null) return false;

            if (visual == null)
            {
                var root = new GameObject("PixelFX Projectile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                root.SetActive(false);
                visual = root.AddComponent<ProjectileBurstVisual>();
                visual._image = root.GetComponent<Image>();
                visual._image.raycastTarget = false;
            }

            var rect = visual._image.rectTransform;
            rect.SetParent(board, false);
            rect.SetAsLastSibling();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = frames[0].pivot / FrameSize;
            rect.sizeDelta = Vector2.one * (size * sizeScale);
            rect.position = worldOrigin;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            visual._frames = frames;
            visual._image.sprite = frames[0];
            visual.gameObject.SetActive(true);
            return true;
        }

        public void Advance(float age)
        {
            int index = Mathf.Min((int)(age * FramesPerSecond), _frames.Length - 1);
            var frame = _frames[index];
            if (_image.sprite != frame) _image.sprite = frame;
        }

        static Sprite[] Load(int slot)
        {
            if (s_sheets[slot] != null) return s_sheets[slot];
            string name;
            int count, columns, originY;
            switch (slot)
            {
                case 0: name = "Lightning"; count = 10; columns = 4; originY = 48; break;
                case 1: name = "Dynamite"; count = 20; columns = 5; originY = 57; break;
                case 2: name = "BeamCoral"; count = 16; columns = 4; originY = 70; break;
                default: name = "BeamCyan"; count = 16; columns = 4; originY = 70; break;
            }
            var texture = Resources.Load<Texture2D>(SheetFolder + name);
            int rows = (count + columns - 1) / columns;
            if (texture == null || texture.width != columns * FrameSize || texture.height != rows * FrameSize)
                return null;

            var frames = new Sprite[count];
            var pivot = new Vector2(.5f, 1f - (float)originY / FrameSize);
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect((i % columns) * FrameSize, (rows - 1 - i / columns) * FrameSize, FrameSize, FrameSize);
                frames[i] = Sprite.Create(texture, rect, pivot, 100, 0, SpriteMeshType.FullRect);
            }
            return s_sheets[slot] = frames;
        }
    }
}
