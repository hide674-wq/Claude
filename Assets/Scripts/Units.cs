using UnityEngine;

namespace SlingshotHeroes
{
    /// <summary>A circular body simulated by <see cref="Battle"/>'s own tiny physics engine.</summary>
    public abstract class Unit : MonoBehaviour
    {
        public Vector2 pos, vel;
        public float radius = 0.45f, mass = 1f, damp = 5f;
        public int hp, maxHp;
        public bool alive = true;

        public SpriteRenderer body, ring, shadow;
        protected Transform vis;
        SpriteRenderer barBg, barFill;
        float barW, bodySize, flash, deathT = 0.3f, appear;
        public float squash;
        Color tint = Color.white;

        protected void Build(Sprite bodySprite, float size, Color ringColor, Color barColor)
        {
            bodySize = size;
            appear = 0f;

            shadow = MakeSr("Shadow", Art.Shadow, new Vector3(0f, -radius * 0.3f, 0f), radius * 2.7f);
            ring = MakeSr("Ring", Art.Ring, Vector3.zero, radius * 2.7f);
            ring.color = ringColor;

            vis = new GameObject("Visual").transform;
            vis.SetParent(transform, false);
            vis.localPosition = new Vector3(0f, radius * 0.25f, 0f);
            body = vis.gameObject.AddComponent<SpriteRenderer>();
            body.sprite = bodySprite;

            barW = Mathf.Max(0.9f, radius * 2f);
            var barRoot = new GameObject("Bar").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0f, radius + 0.55f, 0f);
            barBg = MakeSrOn(barRoot, "Bg", Art.Square, Vector3.zero);
            barBg.color = new Color(0f, 0f, 0f, 0.75f);
            barBg.transform.localScale = new Vector3(barW + 0.08f, 0.16f, 1f);
            barFill = MakeSrOn(barRoot, "Fill", Art.SquareLeft, new Vector3(-barW * 0.5f, 0f, 0f));
            barFill.color = barColor;
            barFill.transform.localScale = new Vector3(barW, 0.1f, 1f);
        }

        SpriteRenderer MakeSr(string n, Sprite s, Vector3 localPos, float scale)
        {
            var sr = MakeSrOn(transform, n, s, localPos);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        static SpriteRenderer MakeSrOn(Transform parent, string n, Sprite s, Vector3 localPos)
        {
            var go = new GameObject(n);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            return sr;
        }

        public Transform BarRoot { get { return barBg.transform.parent; } }

        public void SyncTransform()
        {
            transform.position = new Vector3(pos.x, pos.y, 0f);
            int o = 2000 - Mathf.RoundToInt(pos.y * 40f);
            shadow.sortingOrder = o - 2;
            ring.sortingOrder = o - 1;
            body.sortingOrder = o;
            barBg.sortingOrder = 4500;
            barFill.sortingOrder = 4501;
            OnSync();
        }

        protected virtual void OnSync() { }

        public void UpdateBar()
        {
            float f = maxHp > 0 ? Mathf.Clamp01(hp / (float)maxHp) : 0f;
            barFill.transform.localScale = new Vector3(barW * f, 0.1f, 1f);
        }

        public void TakeDamage(int dmg)
        {
            hp -= dmg;
            flash = 0.18f;
            squash = 1f;
            if (hp <= 0)
            {
                hp = 0;
                alive = false;
                vel = Vector2.zero;
            }
            UpdateBar();
        }

        public void Heal(int amount)
        {
            if (!alive) return;
            hp = Mathf.Min(maxHp, hp + amount);
            UpdateBar();
        }

        public void SetTint(Color c) { tint = c; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (flash > 0f) flash -= dt;
            squash = Mathf.MoveTowards(squash, 0f, dt * 4f);
            if (appear < 1f) appear = Mathf.Min(1f, appear + dt / 0.35f);

            float pop = appear < 1f ? 1f + 0.25f * Mathf.Sin(appear * Mathf.PI) : 1f;
            float k = appear * pop;
            float sx = bodySize * (1f + squash * 0.18f) * k;
            float sy = bodySize * (1f - squash * 0.18f) * k;

            if (!alive)
            {
                deathT -= dt;
                float d = Mathf.Clamp01(deathT / 0.3f);
                sx *= d; sy *= d;
                ring.enabled = false;
                shadow.enabled = false;
                BarRoot.gameObject.SetActive(false);
                if (deathT <= 0f) gameObject.SetActive(false);
            }

            vis.localScale = new Vector3(sx, sy, 1f);
            body.color = Color.Lerp(tint, new Color(1f, 0.35f, 0.35f), Mathf.Clamp01(flash / 0.18f));
            OnTick(dt);
        }

        protected virtual void OnTick(float dt) { }
    }

    public class Hero : Unit
    {
        public HeroDef def;
        public int atk;
        public bool acted, powered;
        public int cd;
        SpriteRenderer aura;
        float auraT;

        public void Setup(HeroDef d, int index)
        {
            def = d;
            radius = d.radius;
            mass = d.mass;
            damp = Battle.IdleDamp;
            hp = maxHp = d.hp;
            atk = d.atk;
            Build(Art.Heroes[index], radius * 2.6f, new Color(0.3f, 0.7f, 1f, 0.95f), new Color(0.35f, 0.9f, 0.45f));
            aura = new GameObject("Aura").AddComponent<SpriteRenderer>();
            aura.transform.SetParent(transform, false);
            aura.sprite = Art.Glow;
            aura.color = new Color(1f, 0.85f, 0.3f, 0f);
            aura.transform.localScale = Vector3.one * radius * 5f;
            UpdateBar();
        }

        protected override void OnSync()
        {
            aura.sortingOrder = body.sortingOrder - 1;
        }

        protected override void OnTick(float dt)
        {
            auraT += dt;
            float a = powered && alive ? 0.55f + 0.25f * Mathf.Sin(auraT * 8f) : 0f;
            aura.color = new Color(1f, 0.85f, 0.3f, a);
        }
    }

    public class Enemy : Unit
    {
        public EnemyDef def;
        public int atk, countdown, atkCount;
        public bool dashing, dashHit;
        public float chainCd;
        SpriteRenderer[] pips;
        float pulse;

        public void Setup(EnemyDef d, float hpScale, float atkScale)
        {
            def = d;
            radius = d.radius;
            mass = d.mass;
            damp = Battle.EnemyDamp;
            hp = maxHp = Mathf.RoundToInt(d.hp * hpScale);
            atk = Mathf.RoundToInt(d.atk * atkScale);
            countdown = Random.Range(1, d.wait + 1);
            Build(Art.Enemy(d), radius * 2.5f, new Color(1f, 0.25f, 0.25f, 0.95f), new Color(1f, 0.35f, 0.3f));

            pips = new SpriteRenderer[3];
            for (int i = 0; i < pips.Length; i++)
            {
                var go = new GameObject("Pip" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.Disc;
                go.transform.localScale = Vector3.one * 0.17f;
                go.transform.localPosition = new Vector3(0f, radius + 0.85f, 0f);
                pips[i] = sr;
            }
            UpdateBar();
            UpdatePips();
        }

        public void UpdatePips()
        {
            for (int i = 0; i < pips.Length; i++)
            {
                bool on = alive && i < countdown;
                pips[i].gameObject.SetActive(on);
                if (!on) continue;
                float x = (i - (countdown - 1) * 0.5f) * 0.24f;
                pips[i].transform.localPosition = new Vector3(x, radius + 0.85f, 0f);
            }
        }

        protected override void OnSync()
        {
            foreach (var p in pips) p.sortingOrder = 4502;
        }

        protected override void OnTick(float dt)
        {
            if (chainCd > 0f) chainCd -= dt;
            pulse += dt;
            bool imminent = countdown <= 1;
            float k = imminent ? 0.65f + 0.35f * Mathf.Sin(pulse * 10f) : 1f;
            Color c = imminent ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 1f, 0.8f);
            foreach (var p in pips) p.color = new Color(c.r * k, c.g * k, c.b * k, 1f);
            if (!alive)
                foreach (var p in pips) p.gameObject.SetActive(false);
        }
    }

    public class Bullet : MonoBehaviour
    {
        public Vector2 pos, vel;
        public int dmg;
        public float radius = 0.2f;
    }
}
