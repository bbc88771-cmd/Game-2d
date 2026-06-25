using UnityEngine;

namespace Sunset.Game
{
    /// <summary>Генератор простых спрайтов-заглушек из кода (1×1 мир-единица).</summary>
    public static class PlaceholderSprites
    {
        public static Sprite Solid(Color color, int size = 16, bool circle = false)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            Color32 c = color, clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inside = !circle ||
                        (new Vector2(x + 0.5f - r, y + 0.5f - r).sqrMagnitude <= (r - 0.5f) * (r - 0.5f));
                    px[y * size + x] = inside ? c : clear;
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Цвет ресурса для иконок/ячеек.</summary>
        public static Color ResourceColor(string id)
        {
            switch (id)
            {
                case Sunset.Core.ResourceId.Wood: return new Color(0.55f, 0.36f, 0.18f);
                case Sunset.Core.ResourceId.Stone: return new Color(0.60f, 0.61f, 0.64f);
                case Sunset.Core.ResourceId.Branch: return new Color(0.72f, 0.56f, 0.30f);
                case Sunset.Core.ResourceId.Clay: return new Color(0.80f, 0.46f, 0.26f);
                default: return Color.white;
            }
        }
    }
}
