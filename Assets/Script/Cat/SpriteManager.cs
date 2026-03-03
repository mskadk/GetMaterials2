using UnityEngine;

namespace Assets.Script.My
{
    public static class SpriteManager
    {
        // 缓存材质，避免重复创建
        private static Material _spriteMaterial;
        private static Material SpriteMaterial
        {
            get
            {
                if (_spriteMaterial == null)
                    _spriteMaterial = new Material(Shader.Find("Sprites/Default"));
                return _spriteMaterial;
            }
        }

        public static GameObject Paint(GameObject obj, string spriteName, int actionId, int frameId, float pixelsPerUnit = 1f)
        {
            CatActionGroup data = CatActionGroup.Load(spriteName);
            if (data == null || data.actions == null) return null;

            if (actionId >= data.actions.Length)
            {
                Debug.LogError($"[SpriteManager] actionId {actionId} 越界, 最大 {data.actions.Length - 1}");
                return null;
            }

            CatAction action = data.actions[actionId];
            if (frameId >= action.frames.Length)
            {
                Debug.LogError($"[SpriteManager] frameId {frameId} 越界, 最大 {action.frames.Length - 1}");
                return null;
            }

            CatFrame frame = action.frames[frameId];

            GameObject container = new GameObject("fw_icon");
            container.transform.SetParent(obj.transform, false);
            container.transform.localPosition = Vector3.zero;
            container.layer = obj.layer;

            for (int i = 0; i < frame.modules.Length; i++)
            {
                CatModule mod = frame.modules[i];
                if (mod == null) continue;
                if (frame.moduleTransparency != null && frame.moduleTransparency[i] <= 0f) continue;

                Texture2D tex = data.imgSource[mod.imgIndex];
                if (tex == null) continue;

                //tex = RemoveBlackBackground(tex, 0.1f);

                int srcX = mod.x;
                int srcY = tex.height - mod.y - mod.height;
                int srcW = mod.width;
                int srcH = mod.height;

                srcX = Mathf.Clamp(srcX, 0, tex.width);
                srcY = Mathf.Clamp(srcY, 0, tex.height);
                srcW = Mathf.Min(srcW, tex.width - srcX);
                srcH = Mathf.Min(srcH, tex.height - srcY);

                if (srcW <= 0 || srcH <= 0) continue;

                Rect rect = new Rect(srcX, srcY, srcW, srcH);
                Vector2 pivot = new Vector2(0.5f, 0.5f);

                Sprite sprite = Sprite.Create(tex, rect, pivot, pixelsPerUnit);

                GameObject moduleObj = new GameObject($"mod_{i}");
                moduleObj.transform.SetParent(container.transform, false);
                moduleObj.layer = obj.layer;

                SpriteRenderer sr = moduleObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = i;
                sr.sharedMaterial = SpriteMaterial; // 强制使用支持透明的材质

                if (frame.moduleTransparency != null && frame.moduleTransparency[i] < 1f)
                {
                    Color c = sr.color;
                    c.a = frame.moduleTransparency[i];
                    sr.color = c;
                }

                float posX = frame.locx[i] + mod.halfW;
                float posY = -(frame.locy[i] + mod.halfH);

                moduleObj.transform.localPosition = new Vector3(posX / pixelsPerUnit, posY / pixelsPerUnit, 0);

                if (frame.moduleScaleX != null && frame.moduleScaleY != null)
                {
                    float scaleX = frame.moduleScaleX[i] * frame.frameScaleX;
                    float scaleY = frame.moduleScaleY[i] * frame.frameScaleY;
                    moduleObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                }
            }

            return container;
        }

        /// <summary>
        /// 将贴图中接近黑色的像素转为透明
        /// </summary>
        /// <param name="source">原始贴图</param>
        /// <param name="threshold">黑色阈值，越大则越多深色像素被判定为"黑色"（建议 0.05~0.15）</param>
        /// <returns>新的贴图（透明背景）</returns>
        private static Texture2D RemoveBlackBackground(Texture2D source, float threshold = 0.1f)
        {
            Texture2D newTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                // 如果 RGB 都接近 0（黑色），就设为完全透明
                if (p.r <= threshold && p.g <= threshold && p.b <= threshold)
                {
                    pixels[i] = Color.clear; // (0,0,0,0)
                }
            }

            newTex.SetPixels(pixels);
            newTex.Apply();
            newTex.wrapMode = source.wrapMode;
            newTex.filterMode = source.filterMode;
            return newTex;
        }

    }
}
