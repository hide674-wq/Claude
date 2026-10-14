using System.Collections.Generic;
using UnityEngine;

namespace SlingshotHeroes
{
    public class FxParticle : MonoBehaviour
    {
        public SpriteRenderer sr;
        public Vector2 vel;
        public float drag, life, maxLife, s0, s1, spin;
        public Color c0, c1;

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Fx.Release(this); return; }
            float k = 1f - life / maxLife;
            transform.position += (Vector3)(vel * dt);
            vel *= Mathf.Max(0f, 1f - drag * dt);
            float s = Mathf.Lerp(s0, s1, k);
            transform.localScale = new Vector3(s, s, 1f);
            sr.color = Color.Lerp(c0, c1, k);
            if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);
        }
    }

    /// <summary>Small pooled particle helper (bursts, rings, glows, trails).</summary>
    public static class Fx
    {
        static Transform root;
        static readonly Stack<FxParticle> pool = new Stack<FxParticle>();

        public static void Init(Transform parent) { root = parent; pool.Clear(); }
        public static void Clear() { root = null; pool.Clear(); }

        public static void Release(FxParticle p)
        {
            p.gameObject.SetActive(false);
            pool.Push(p);
        }

        static FxParticle Get(Sprite sprite, Vector2 pos, int order)
        {
            FxParticle p = null;
            while (pool.Count > 0 && p == null) p = pool.Pop();
            if (p == null)
            {
                var go = new GameObject("Fx");
                go.transform.SetParent(root, false);
                p = go.AddComponent<FxParticle>();
                p.sr = go.AddComponent<SpriteRenderer>();
            }
            p.gameObject.SetActive(true);
            p.sr.sprite = sprite;
            p.sr.sortingOrder = order;
            p.transform.position = new Vector3(pos.x, pos.y, 0f);
            p.transform.rotation = Quaternion.identity;
            p.vel = Vector2.zero;
            p.drag = 0f;
            p.spin = 0f;
            return p;
        }

        public static void Burst(Vector2 pos, Color col, int n, float speed, float size, float life = 0.45f)
        {
            for (int i = 0; i < n; i++)
            {
                var p = Get(Art.Disc, pos, 4000);
                float a = Random.value * Mathf.PI * 2f;
                p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * Random.Range(0.4f, 1f);
                p.drag = 3f;
                p.maxLife = p.life = life * Random.Range(0.7f, 1.2f);
                p.s0 = size * Random.Range(0.6f, 1.2f);
                p.s1 = 0f;
                p.c0 = col;
                p.c1 = new Color(col.r, col.g, col.b, 0f);
            }
        }

        public static void Ring(Vector2 pos, Color col, float endScale, float life = 0.4f)
        {
            var p = Get(Art.Ring, pos, 3900);
            p.maxLife = p.life = life;
            p.s0 = 0.2f;
            p.s1 = endScale;
            p.c0 = col;
            p.c1 = new Color(col.r, col.g, col.b, 0f);
        }

        public static void Glow(Vector2 pos, Color col, float scale, float life)
        {
            var p = Get(Art.Glow, pos, 3950);
            p.maxLife = p.life = life;
            p.s0 = scale * 0.5f;
            p.s1 = scale;
            p.c0 = col;
            p.c1 = new Color(col.r, col.g, col.b, 0f);
        }

        public static void Trail(Vector2 pos, Color col, float size)
        {
            var p = Get(Art.Disc, pos, 1500);
            p.maxLife = p.life = 0.3f;
            p.s0 = size;
            p.s1 = 0f;
            p.c0 = new Color(col.r, col.g, col.b, 0.5f);
            p.c1 = new Color(col.r, col.g, col.b, 0f);
        }
    }
}
