using System.Collections.Generic;
using UnityEngine;

namespace SlingshotHeroes
{
    /// <summary>Tiny software rasteriser used to draw every sprite at startup (no external art needed).</summary>
    public class Pix
    {
        public readonly int w, h;
        readonly Color[] c;

        public Pix(int w, int h)
        {
            this.w = w;
            this.h = h;
            c = new Color[w * h];
        }

        void Blend(int x, int y, Color col, float cov)
        {
            if (cov <= 0f || x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            Color d = c[i];
            float a = col.a * Mathf.Clamp01(cov);
            float oa = a + d.a * (1f - a);
            if (oa <= 0f) return;
            float k = d.a * (1f - a);
            c[i] = new Color(
                (col.r * a + d.r * k) / oa,
                (col.g * a + d.g * k) / oa,
                (col.b * a + d.b * k) / oa,
                oa);
        }

        public void Circle(float cx, float cy, float r, Color col)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
            int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    Blend(x, y, col, r - dist + 0.5f);
                }
        }

        public void Glow(float cx, float cy, float r, Color col)
        {
            int x0 = Mathf.FloorToInt(cx - r - 1), x1 = Mathf.CeilToInt(cx + r + 1);
            int y0 = Mathf.FloorToInt(cy - r - 1), y1 = Mathf.CeilToInt(cy + r + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float t = 1f - Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (t > 0f) Blend(x, y, col, t * t);
                }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.FloorToInt(cx - rx - 1), x1 = Mathf.CeilToInt(cx + rx + 1);
            int y0 = Mathf.FloorToInt(cy - ry - 1), y1 = Mathf.CeilToInt(cy + ry + 1);
            float m = Mathf.Min(rx, ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    Blend(x, y, col, (1f - dist) * m + 0.5f);
                }
        }

        public void Ring(float cx, float cy, float r, float thick, Color col)
        {
            int x0 = Mathf.FloorToInt(cx - r - thick - 1), x1 = Mathf.CeilToInt(cx + r + thick + 1);
            int y0 = Mathf.FloorToInt(cy - r - thick - 1), y1 = Mathf.CeilToInt(cy + r + thick + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    Blend(x, y, col, thick * 0.5f - Mathf.Abs(dist - r) + 0.5f);
                }
        }

        public void Line(Vector2 a, Vector2 b, float thick, Color col)
        {
            float minX = Mathf.Min(a.x, b.x) - thick - 1, maxX = Mathf.Max(a.x, b.x) + thick + 1;
            float minY = Mathf.Min(a.y, b.y) - thick - 1, maxY = Mathf.Max(a.y, b.y) + thick + 1;
            Vector2 ab = b - a;
            float len2 = Mathf.Max(ab.sqrMagnitude, 0.0001f);
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
                    float dist = (p - (a + ab * t)).magnitude;
                    Blend(x, y, col, thick * 0.5f - dist + 0.5f);
                }
        }

        static bool Inside(Vector2[] poly, float px, float py)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > py) != (poly[j].y > py) &&
                    px < (poly[j].x - poly[i].x) * (py - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        public void Poly(Vector2[] poly, Color col)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in poly)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int y = Mathf.FloorToInt(minY) - 1; y <= Mathf.CeilToInt(maxY) + 1; y++)
                for (int x = Mathf.FloorToInt(minX) - 1; x <= Mathf.CeilToInt(maxX) + 1; x++)
                {
                    int n = 0;
                    if (Inside(poly, x + 0.25f, y + 0.25f)) n++;
                    if (Inside(poly, x + 0.75f, y + 0.25f)) n++;
                    if (Inside(poly, x + 0.25f, y + 0.75f)) n++;
                    if (Inside(poly, x + 0.75f, y + 0.75f)) n++;
                    if (n > 0) Blend(x, y, col, n / 4f);
                }
        }

        public void RoundRect(float radius, Color col)
        {
            float hx = w * 0.5f, hy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - hx) - (hx - radius);
                    float qy = Mathf.Abs(y + 0.5f - hy) - (hy - radius);
                    float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                    float d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                    Blend(x, y, col, 0.5f - d);
                }
        }

        public void Erase(float cx, float cy, float r)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float cov = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    if (cov > 0f)
                    {
                        Color d = c[y * w + x];
                        d.a *= 1f - cov;
                        c[y * w + x] = d;
                    }
                }
        }

        public void SetRaw(int x, int y, Color col) { c[y * w + x] = col; }

        public Sprite ToSprite(float ppu, Vector2 pivot, bool repeat = false, Vector4? border = null)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(c);
            tex.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect,
                border ?? Vector4.zero);
        }
    }

    /// <summary>All procedurally generated sprites. Every "unit" sprite is 1 world unit wide.</summary>
    public static class Art
    {
        public static Sprite Square, SquareLeft, Disc, Glow, Ring, Arrow, Shadow, Ground, Sky, Moon;
        public static Sprite RoundRect, Coin, Chest, Bag;
        public static Sprite[] Heroes = new Sprite[3];
        static readonly Dictionary<string, Sprite> enemies = new Dictionary<string, Sprite>();
        static bool ready;

        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        public static void Init()
        {
            if (ready) return;
            ready = true;

            var p = new Pix(4, 4);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) p.SetRaw(x, y, Color.white);
            Square = p.ToSprite(4, Mid);
            SquareLeft = p.ToSprite(4, new Vector2(0f, 0.5f));

            p = new Pix(64, 64); p.Circle(32, 32, 31, Color.white); Disc = p.ToSprite(64, Mid);
            p = new Pix(64, 64); p.Glow(32, 32, 32, Color.white); Glow = p.ToSprite(64, Mid);
            p = new Pix(128, 128); p.Ring(64, 64, 59, 7, Color.white); Ring = p.ToSprite(128, Mid);
            p = new Pix(64, 64);
            p.Poly(new[] { new Vector2(32, 62), new Vector2(4, 8), new Vector2(32, 20), new Vector2(60, 8) }, Color.white);
            Arrow = p.ToSprite(64, Mid);
            p = new Pix(64, 64); p.Ellipse(32, 32, 30, 22, new Color(0, 0, 0, 0.4f)); Shadow = p.ToSprite(64, Mid);

            p = new Pix(64, 64); p.RoundRect(18, Color.white);
            RoundRect = p.ToSprite(100, Mid, false, new Vector4(24, 24, 24, 24));

            p = new Pix(64, 64);
            p.Circle(32, 32, 30, new Color(0.7f, 0.45f, 0.05f));
            p.Circle(32, 32, 26, new Color(1f, 0.82f, 0.2f));
            p.Ring(32, 32, 17, 4, new Color(0.85f, 0.6f, 0.1f));
            p.Ellipse(24, 42, 8, 5, new Color(1, 1, 1, 0.5f));
            Coin = p.ToSprite(64, Mid);

            p = new Pix(64, 64);
            p.Poly(new[] { new Vector2(6, 8), new Vector2(58, 8), new Vector2(58, 34), new Vector2(6, 34) }, new Color(0.55f, 0.3f, 0.12f));
            p.Poly(new[] { new Vector2(6, 34), new Vector2(58, 34), new Vector2(52, 52), new Vector2(12, 52) }, new Color(0.75f, 0.45f, 0.18f));
            p.Poly(new[] { new Vector2(6, 30), new Vector2(58, 30), new Vector2(58, 36), new Vector2(6, 36) }, new Color(0.95f, 0.8f, 0.3f));
            p.Poly(new[] { new Vector2(27, 24), new Vector2(37, 24), new Vector2(37, 40), new Vector2(27, 40) }, new Color(1f, 0.9f, 0.4f));
            Chest = p.ToSprite(64, Mid);

            p = new Pix(64, 64);
            p.Ellipse(32, 26, 22, 20, new Color(0.75f, 0.75f, 0.8f));
            p.Poly(new[] { new Vector2(22, 40), new Vector2(42, 40), new Vector2(46, 56), new Vector2(18, 56) }, new Color(0.65f, 0.65f, 0.72f));
            p.Line(new Vector2(20, 54), new Vector2(44, 54), 5, new Color(0.9f, 0.55f, 0.3f));
            p.Ellipse(24, 32, 7, 10, new Color(1, 1, 1, 0.3f));
            Bag = p.ToSprite(64, Mid);

            BuildGround();
            BuildSky();

            Heroes[0] = DrawHero(HeroClass.Knight, GameData.Heroes[0].color);
            Heroes[1] = DrawHero(HeroClass.Mage, GameData.Heroes[1].color);
            Heroes[2] = DrawHero(HeroClass.Healer, GameData.Heroes[2].color);
        }

        public static Sprite Enemy(EnemyDef d)
        {
            Sprite s;
            if (enemies.TryGetValue(d.Key, out s)) return s;
            switch (d.kind)
            {
                case EnemyKind.Spiker: s = DrawSpiker(d.color); break;
                case EnemyKind.Boss: s = DrawBoss(d.color); break;
                default: s = DrawBlob(d.color); break;
            }
            enemies[d.Key] = s;
            return s;
        }

        // ------------------------------------------------------------------ ground / sky

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 144665;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        static void BuildGround()
        {
            const int size = 256, cells = 5;
            float cs = size / (float)cells;
            var jx = new float[cells, cells];
            var jy = new float[cells, cells];
            var shade = new float[cells, cells];
            for (int j = 0; j < cells; j++)
                for (int i = 0; i < cells; i++)
                {
                    jx[i, j] = 0.15f + 0.7f * Hash(i, j, 1);
                    jy[i, j] = 0.15f + 0.7f * Hash(i, j, 2);
                    shade[i, j] = 0.27f + 0.09f * Hash(i, j, 3);
                }

            var p = new Pix(size, size);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = Mathf.FloorToInt(x / cs), cy = Mathf.FloorToInt(y / cs);
                    float d1 = 1e9f, d2 = 1e9f;
                    float v = 0.3f;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int gx = cx + dx, gy = cy + dy;
                            int wx = ((gx % cells) + cells) % cells, wy = ((gy % cells) + cells) % cells;
                            float px = (gx + jx[wx, wy]) * cs, py = (gy + jy[wx, wy]) * cs;
                            float ddx = x + 0.5f - px, ddy = y + 0.5f - py;
                            float d = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                            if (d < d1) { d2 = d1; d1 = d; v = shade[wx, wy]; }
                            else if (d < d2) d2 = d;
                        }
                    float edge = d2 - d1;
                    float crack = 1f - Mathf.SmoothStep(0.8f, 4.5f, edge);
                    float n = (Hash(x, y, 9) - 0.5f) * 0.05f;
                    float lit = v * (1f - 0.22f * (d1 / cs)) + n;
                    Color baseCol = new Color(lit, lit * 0.92f, lit * 0.97f, 1f);
                    Color crackCol = new Color(0.06f, 0.05f, 0.08f, 1f);
                    p.SetRaw(x, y, Color.Lerp(baseCol, crackCol, crack));
                }
            Ground = p.ToSprite(64, Mid, true);
        }

        static void BuildSky()
        {
            var p = new Pix(4, 128);
            Color top = new Color(0.09f, 0.07f, 0.22f), mid = new Color(0.35f, 0.22f, 0.5f), low = new Color(0.1f, 0.07f, 0.14f);
            for (int y = 0; y < 128; y++)
            {
                float t = y / 127f;
                Color col = t > 0.5f ? Color.Lerp(mid, top, (t - 0.5f) * 2f) : Color.Lerp(low, mid, t * 2f);
                for (int x = 0; x < 4; x++) p.SetRaw(x, y, col);
            }
            Sky = p.ToSprite(4, Mid);

            p = new Pix(128, 128);
            p.Circle(64, 64, 50, new Color(0.95f, 0.93f, 0.8f));
            p.Erase(84, 70, 44);
            Moon = p.ToSprite(128, Mid);
        }

        // ------------------------------------------------------------------ characters

        static Sprite DrawHero(HeroClass cls, Color col)
        {
            var p = new Pix(128, 128);
            Color dark = new Color(col.r * 0.42f, col.g * 0.42f, col.b * 0.42f, 1f);
            Color white = Color.white, ink = new Color(0.1f, 0.08f, 0.14f);

            p.Circle(64, 60, 56, dark);
            p.Circle(64, 60, 51, col);
            p.Ellipse(48, 84, 22, 11, new Color(1, 1, 1, 0.28f));

            // class decoration behind the face
            if (cls == HeroClass.Knight)
            {
                var helm = new List<Vector2>();
                for (int a = 25; a <= 155; a += 5)
                {
                    float r = a * Mathf.Deg2Rad;
                    helm.Add(new Vector2(64 + Mathf.Cos(r) * 52, 60 + Mathf.Sin(r) * 52));
                }
                p.Poly(helm.ToArray(), new Color(0.74f, 0.78f, 0.86f));
                p.Poly(new[] { new Vector2(58, 108), new Vector2(70, 108), new Vector2(78, 127), new Vector2(64, 120), new Vector2(50, 127) },
                    new Color(0.9f, 0.2f, 0.25f));
                p.Line(new Vector2(30, 96), new Vector2(98, 96), 4, new Color(0.45f, 0.5f, 0.6f));
            }
            else if (cls == HeroClass.Mage)
            {
                Color hat = new Color(0.4f, 0.22f, 0.7f);
                p.Ellipse(64, 96, 50, 11, new Color(0.3f, 0.16f, 0.55f));
                p.Poly(new[] { new Vector2(34, 98), new Vector2(94, 98), new Vector2(74, 127), new Vector2(66, 127) }, hat);
                p.Line(new Vector2(38, 100), new Vector2(90, 100), 6, new Color(1f, 0.85f, 0.3f));
            }

            // face
            p.Ellipse(46, 56, 9, 12, white);
            p.Ellipse(82, 56, 9, 12, white);
            p.Circle(48, 54, 5.5f, ink);
            p.Circle(84, 54, 5.5f, ink);
            p.Circle(50, 57, 2f, white);
            p.Circle(86, 57, 2f, white);
            p.Ellipse(34, 40, 7, 4, new Color(1f, 0.5f, 0.5f, 0.5f));
            p.Ellipse(94, 40, 7, 4, new Color(1f, 0.5f, 0.5f, 0.5f));
            p.Line(new Vector2(54, 38), new Vector2(64, 33), 3, ink);
            p.Line(new Vector2(64, 33), new Vector2(74, 38), 3, ink);

            if (cls == HeroClass.Healer)
            {
                p.Line(new Vector2(64, 76), new Vector2(64, 94), 7, white);
                p.Line(new Vector2(55, 85), new Vector2(73, 85), 7, white);
                p.Ring(64, 113, 11, 4, new Color(1f, 0.85f, 0.3f));
            }
            return p.ToSprite(128, Mid);
        }

        static Sprite DrawBlob(Color col)
        {
            var p = new Pix(128, 128);
            Color dark = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 1f);
            Color white = Color.white, ink = new Color(0.08f, 0.05f, 0.1f);

            for (int i = 0; i < 4; i++) p.Circle(28 + i * 24, 24, 15, dark);
            p.Ellipse(64, 66, 54, 50, dark);
            for (int i = 0; i < 4; i++) p.Circle(28 + i * 24, 24, 12.5f, col);
            p.Ellipse(64, 67, 50, 46, col);
            p.Ellipse(46, 88, 20, 10, new Color(1, 1, 1, 0.3f));

            p.Ellipse(44, 70, 12, 15, white);
            p.Ellipse(84, 70, 12, 15, white);
            p.Circle(46, 68, 6.5f, ink);
            p.Circle(86, 68, 6.5f, ink);
            p.Circle(48, 71, 2.2f, white);
            p.Circle(88, 71, 2.2f, white);
            p.Line(new Vector2(30, 92), new Vector2(54, 84), 5, dark);
            p.Line(new Vector2(98, 92), new Vector2(74, 84), 5, dark);
            p.Ellipse(64, 44, 10, 5, dark);
            return p.ToSprite(128, Mid);
        }

        static Sprite DrawSpiker(Color col)
        {
            var p = new Pix(128, 128);
            Color dark = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 1f);
            var outer = new List<Vector2>();
            var inner = new List<Vector2>();
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f + 0.1f;
                float ro = (i % 2 == 0) ? 62f : 42f;
                outer.Add(new Vector2(64 + Mathf.Cos(a) * ro, 64 + Mathf.Sin(a) * ro));
                float ri = (i % 2 == 0) ? 54f : 38f;
                inner.Add(new Vector2(64 + Mathf.Cos(a) * ri, 64 + Mathf.Sin(a) * ri));
            }
            p.Poly(outer.ToArray(), dark);
            p.Poly(inner.ToArray(), col);
            p.Circle(64, 64, 34, new Color(col.r * 0.75f, col.g * 0.75f, col.b * 0.75f, 1f));
            Color eye = new Color(1f, 0.9f, 0.25f);
            p.Poly(new[] { new Vector2(34, 76), new Vector2(58, 66), new Vector2(54, 80) }, eye);
            p.Poly(new[] { new Vector2(94, 76), new Vector2(70, 66), new Vector2(74, 80) }, eye);
            p.Line(new Vector2(44, 46), new Vector2(54, 40), 3, dark);
            p.Line(new Vector2(54, 40), new Vector2(64, 46), 3, dark);
            p.Line(new Vector2(64, 46), new Vector2(74, 40), 3, dark);
            p.Line(new Vector2(74, 40), new Vector2(84, 46), 3, dark);
            return p.ToSprite(128, Mid);
        }

        static Sprite DrawBoss(Color col)
        {
            var p = new Pix(128, 128);
            Color dark = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 1f);
            Color bone = new Color(0.93f, 0.9f, 0.78f);
            p.Poly(new[] { new Vector2(26, 86), new Vector2(8, 124), new Vector2(54, 104) }, bone);
            p.Poly(new[] { new Vector2(102, 86), new Vector2(120, 124), new Vector2(74, 104) }, bone);
            p.Circle(64, 62, 60, dark);
            p.Circle(64, 62, 55, col);
            p.Ellipse(46, 88, 22, 11, new Color(1, 1, 1, 0.18f));
            Color eye = new Color(1f, 0.85f, 0.2f);
            p.Poly(new[] { new Vector2(26, 82), new Vector2(58, 66), new Vector2(56, 84), new Vector2(34, 90) }, eye);
            p.Poly(new[] { new Vector2(102, 82), new Vector2(70, 66), new Vector2(72, 84), new Vector2(94, 90) }, eye);
            p.Circle(50, 75, 5, new Color(0.7f, 0.05f, 0.05f));
            p.Circle(78, 75, 5, new Color(0.7f, 0.05f, 0.05f));
            p.Ellipse(64, 38, 30, 14, new Color(0.15f, 0.02f, 0.06f));
            for (int i = 0; i < 5; i++)
            {
                float x = 44 + i * 10;
                p.Poly(new[] { new Vector2(x - 4, 46), new Vector2(x + 4, 46), new Vector2(x, 36) }, Color.white);
            }
            return p.ToSprite(128, Mid);
        }
    }
}
