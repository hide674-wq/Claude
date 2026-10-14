using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SlingshotHeroes
{
    /// <summary>Helpers to build uGUI hierarchies from code.</summary>
    public static class UIKit
    {
        public static Font Font;

        public static void Init()
        {
            if (Font != null) return;
            Font = LoadFont();
        }

        static Font LoadFont()
        {
            // Prefer an installed CJK-capable OS font so Japanese renders everywhere.
            try
            {
                string[] prefer = { "NotoSansCJK", "Noto Sans CJK", "NotoSansJP", "Noto Sans JP", "SourceHan", "DroidSansFallback", "DroidSansJapanese", "Hiragino", "Yu Gothic", "Meiryo" };
                string[] names = Font.GetOSInstalledFontNames();
                foreach (var pref in prefer)
                    foreach (var n in names)
                        if (n.IndexOf(pref, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return Font.CreateDynamicFontFromOSFont(n, 32);
            }
            catch (System.Exception) { }

            Font f = null;
#if UNITY_2022_2_OR_NEWER
            f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            f = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 32);
            return f;
        }

        public static Canvas CreateCanvas(string name, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image MakePanel(Transform parent, string name, Color color, Sprite sprite = null, bool raycast = false)
        {
            var rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null)
            {
                img.sprite = sprite;
                if (sprite == Art.RoundRect) img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text MakeLabel(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, bool outline = true)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.85f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return t;
        }

        public static Button MakeButton(Transform parent, string name, string text, int fontSize, Color bg, UnityAction onClick,
            out Text label)
        {
            var img = MakePanel(parent, name, bg, Art.RoundRect, true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Sfx.Play("click"));
            if (onClick != null) btn.onClick.AddListener(onClick);
            label = MakeLabel(img.transform, "Label", text, fontSize, Color.white);
            Stretch(label.rectTransform);
            return btn;
        }

        public static Button MakeButton(Transform parent, string name, string text, int fontSize, Color bg, UnityAction onClick)
        {
            Text l;
            return MakeButton(parent, name, text, fontSize, bg, onClick, out l);
        }
    }
}
