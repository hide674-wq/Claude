using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlingshotHeroes
{
    /// <summary>
    /// One quest run: arena, slingshot aiming, custom circle physics, hero/enemy turns, waves, skills.
    /// </summary>
    public class Battle : MonoBehaviour
    {
        public class Result
        {
            public bool win;
            public int turns, coins, chests, bags;
        }

        public const float IdleDamp = 6f;
        public const float EnemyDamp = 5f;
        const float ShooterDamp = 1.45f;
        const float MaxPull = 2.2f;
        const float MinSpeed = 8f;
        const float MaxSpeed = 23f;

        enum Phase { Intro, Aim, Moving, Enemy, Transition, Ended }

        Game game;
        Hud hud;
        Camera cam;
        StageDef stage;
        Phase phase;

        Rect arena;
        Vector3 camBase;
        readonly List<Hero> heroes = new List<Hero>();
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Bullet> bullets = new List<Bullet>();
        readonly List<Unit> units = new List<Unit>();
        readonly HashSet<Hero> linked = new HashSet<Hero>();

        Hero selected, shooter;
        int wave, round = 1, combo;
        int coins, chests, bags;
        bool shotDone;
        float hitStop, shake, stillTime, shotTime, wallSfxT, trailT;

        // aiming
        bool aiming;
        Vector2 aimStart, aimDir = Vector2.up;
        float aimPower;
        SpriteRenderer[] dots;
        SpriteRenderer bandL, bandR, arrow;

        public void Begin(Game g, Camera c, Hud h, int stageIndex)
        {
            game = g;
            cam = c;
            hud = h;
            stage = GameData.Stages[stageIndex];
            Fx.Init(transform);
            BuildArena();
            BuildAimVisuals();
            SpawnHeroes();
            hud.onSelect = SelectHero;
            hud.onSkill = UseSkill;
            hud.SetCounts(0, 0, 0);
            hud.SetTurn(1);
            StartCoroutine(Run());
        }

        void OnDestroy() { Fx.Clear(); }

        // ================================================================== setup

        void BuildArena()
        {
            float aspect = (float)Screen.width / Screen.height;
            float ortho = Mathf.Max(6.2f, 4.1f / aspect);
            cam.orthographicSize = ortho;
            camBase = new Vector3(0f, 0f, -10f);
            cam.transform.position = camBase;

            float viewH = ortho * 2f;
            float top = ortho - viewH * 0.255f;
            float bottom = -ortho + viewH * 0.04f;
            float h = Mathf.Min(top - bottom, 12.5f);
            float cy = (top + bottom) * 0.5f;
            arena = new Rect(-3.5f, cy - h * 0.5f, 7f, h);

            var sky = NewSr("Sky", Art.Sky, new Vector3(0f, 0f, 0f), -6000);
            sky.transform.localScale = new Vector3(60f, (viewH + 4f) / 32f, 1f);
            var moon = NewSr("Moon", Art.Moon, new Vector3(2.4f, ortho - 1.6f, 0f), -5900);
            moon.transform.localScale = Vector3.one * 1.8f;
            moon.color = new Color(1f, 1f, 1f, 0.55f);

            var ground = NewSr("Ground", Art.Ground, new Vector3(arena.center.x, arena.center.y, 0f), -5000);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(arena.width, arena.height);

            Color wall = new Color(1f, 0.62f, 0.25f, 0.95f);
            Color glow = new Color(1f, 0.5f, 0.2f, 0.25f);
            Wall("WallT", new Vector2(arena.center.x, arena.yMax), arena.width + 0.2f, 0.14f, wall, glow);
            Wall("WallB", new Vector2(arena.center.x, arena.yMin), arena.width + 0.2f, 0.14f, wall, glow);
            Wall("WallL", new Vector2(arena.xMin, arena.center.y), 0.14f, arena.height, wall, glow);
            Wall("WallR", new Vector2(arena.xMax, arena.center.y), 0.14f, arena.height, wall, glow);
        }

        void Wall(string n, Vector2 p, float w, float h, Color c, Color glow)
        {
            var g = NewSr(n + "Glow", Art.Square, p, -4900);
            g.transform.localScale = new Vector3(w + 0.5f, h + 0.5f, 1f);
            g.color = glow;
            var s = NewSr(n, Art.Square, p, -4800);
            s.transform.localScale = new Vector3(w, h, 1f);
            s.color = c;
        }

        SpriteRenderer NewSr(string n, Sprite s, Vector3 pos, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            return sr;
        }

        void BuildAimVisuals()
        {
            dots = new SpriteRenderer[22];
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i] = NewSr("Dot" + i, Art.Disc, Vector3.zero, 3000);
                dots[i].transform.localScale = Vector3.one * 0.14f;
                dots[i].gameObject.SetActive(false);
            }
            arrow = NewSr("Arrow", Art.Arrow, Vector3.zero, 3100);
            arrow.color = new Color(0.55f, 0.9f, 1f, 0.95f);
            arrow.gameObject.SetActive(false);
            bandL = NewSr("BandL", Art.Square, Vector3.zero, 3050);
            bandR = NewSr("BandR", Art.Square, Vector3.zero, 3050);
            bandL.color = bandR.color = new Color(0.4f, 0.85f, 1f, 0.9f);
            bandL.gameObject.SetActive(false);
            bandR.gameObject.SetActive(false);
        }

        void SpawnHeroes()
        {
            float y = arena.yMin + 1.7f;
            float[] xs = { -2.0f, 0f, 2.0f };
            float[] ys = { y + 0.2f, y - 0.6f, y + 0.2f };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject(GameData.Heroes[i].name);
                go.transform.SetParent(transform, false);
                var h = go.AddComponent<Hero>();
                h.Setup(GameData.Heroes[i], i);
                h.pos = new Vector2(xs[i], ys[i]);
                h.SyncTransform();
                heroes.Add(h);
            }
        }

        void SpawnWave(WaveDef def)
        {
            foreach (var e in enemies) if (e != null) Destroy(e.gameObject);
            enemies.Clear();

            var placed = new List<Unit>(heroes.ToArray());
            foreach (var d in def.enemies)
            {
                var go = new GameObject(d.name);
                go.transform.SetParent(transform, false);
                var e = go.AddComponent<Enemy>();
                e.Setup(d, stage.hpScale, stage.atkScale);

                Vector2 p = Vector2.zero;
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    float minY = Mathf.Max(arena.center.y, arena.yMin + 4.5f);
                    float maxY = arena.yMax - e.radius - 0.5f;
                    p = d.kind == EnemyKind.Boss
                        ? new Vector2(0f, arena.yMax - e.radius - 1.4f)
                        : new Vector2(Random.Range(arena.xMin + e.radius + 0.4f, arena.xMax - e.radius - 0.4f), Random.Range(minY, maxY));
                    bool ok = true;
                    foreach (var o in placed)
                        if ((o.pos - p).magnitude < o.radius + e.radius + 0.35f) { ok = false; break; }
                    if (ok) break;
                }
                e.pos = p;
                e.SyncTransform();
                enemies.Add(e);
                placed.Add(e);
                Fx.Ring(p, new Color(1f, 0.4f, 0.4f, 0.8f), e.radius * 3f, 0.4f);
            }
        }

        // ================================================================== flow

        int AliveEnemies() { int n = 0; foreach (var e in enemies) if (e.alive) n++; return n; }
        int AliveHeroes() { int n = 0; foreach (var h in heroes) if (h.alive) n++; return n; }

        IEnumerator Run()
        {
            phase = Phase.Intro;
            yield return new WaitForSeconds(0.5f);
            for (wave = 0; wave < stage.waves.Count; wave++)
            {
                hud.SetWave(wave + 1, stage.waves.Count);
                bool boss = wave == stage.waves.Count - 1;
                hud.Banner(boss ? "BOSS BATTLE" : "WAVE " + (wave + 1), boss ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.9f, 0.5f), 1.4f);
                yield return new WaitForSeconds(1.0f);
                SpawnWave(stage.waves[wave]);
                yield return new WaitForSeconds(0.6f);

                while (AliveEnemies() > 0)
                {
                    yield return PlayerPhase();
                    aiming = false;
                    HideAim();
                    if (AliveHeroes() == 0) { yield return Finish(false); yield break; }
                    if (AliveEnemies() == 0) break;

                    yield return EnemyPhase();
                    if (AliveHeroes() == 0) { yield return Finish(false); yield break; }

                    round++;
                    hud.SetTurn(round);
                    foreach (var h in heroes) if (h.cd > 0) h.cd--;
                }

                phase = Phase.Transition;
                if (wave < stage.waves.Count - 1)
                {
                    hud.Banner("WAVE CLEAR!", new Color(0.5f, 1f, 0.6f), 1.4f);
                    Sfx.Play("skill", 0.6f);
                    yield return new WaitForSeconds(1.6f);
                }
            }
            yield return Finish(true);
        }

        Hero PickNext()
        {
            if (selected != null && selected.alive && !selected.acted) return selected;
            foreach (var h in heroes) if (h.alive && !h.acted) return h;
            return null;
        }

        IEnumerator PlayerPhase()
        {
            foreach (var h in heroes) if (h.alive) h.acted = false;
            hud.Banner("YOUR TURN", new Color(0.45f, 0.85f, 1f), 1.0f);
            while (true)
            {
                if (AliveEnemies() == 0 || AliveHeroes() == 0) yield break;
                Hero next = PickNext();
                if (next == null) yield break;
                selected = next;
                phase = Phase.Aim;
                shotDone = false;
                while (!shotDone && AliveEnemies() > 0 && AliveHeroes() > 0) yield return null;
            }
        }

        IEnumerator WaitSettled(float timeout)
        {
            float t = 0f, still = 0f;
            while (t < timeout)
            {
                t += Time.deltaTime;
                bool moving = false;
                foreach (var u in units) if (u.alive && u.vel.sqrMagnitude > 0.6f) { moving = true; break; }
                still = moving ? 0f : still + Time.deltaTime;
                if (still > 0.25f && bullets.Count == 0) yield break;
                yield return null;
            }
        }

        IEnumerator Finish(bool win)
        {
            phase = Phase.Ended;
            hud.SetCombo(0);
            if (win)
            {
                hud.Banner("QUEST CLEAR!", new Color(1f, 0.9f, 0.4f), 2f);
                Sfx.Play("win");
            }
            else
            {
                hud.Banner("GAME OVER", new Color(1f, 0.4f, 0.4f), 2f);
                Sfx.Play("lose");
            }
            yield return new WaitForSeconds(2.2f);
            game.OnBattleEnd(new Result { win = win, turns = round, coins = coins, chests = chests, bags = bags });
        }

        // ================================================================== enemy phase

        Hero PickTarget()
        {
            var list = new List<Hero>();
            foreach (var h in heroes) if (h.alive) list.Add(h);
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        IEnumerator EnemyPhase()
        {
            phase = Phase.Enemy;
            selected = null;
            hud.Banner("ENEMY TURN", new Color(1f, 0.4f, 0.35f), 1.0f);
            yield return new WaitForSeconds(0.9f);

            foreach (var e in new List<Enemy>(enemies))
            {
                if (!e.alive) continue;
                e.countdown--;
                if (e.countdown > 0) { e.UpdatePips(); continue; }
                e.countdown = e.def.wait;
                e.UpdatePips();
                if (AliveHeroes() == 0) break;
                yield return EnemyAct(e);
                yield return WaitSettled(1.8f);
                if (AliveHeroes() == 0) break;
            }
            yield return WaitSettled(2f);
        }

        IEnumerator EnemyAct(Enemy e)
        {
            Hero target = PickTarget();
            if (target == null) yield break;

            AttackKind kind = e.def.attack;
            if (e.def.kind == EnemyKind.Boss) kind = (e.atkCount++ % 2 == 0) ? AttackKind.Slam : AttackKind.Shoot;

            switch (kind)
            {
                case AttackKind.Dash: yield return DoDash(e, target); break;
                case AttackKind.Slam: yield return DoSlam(e); break;
                default: yield return DoShoot(e, target, e.def.kind == EnemyKind.Boss ? 5 : 1); break;
            }
        }

        IEnumerator Telegraph(Enemy e, Vector2 to, float dur, bool line)
        {
            var go = NewSr("Tele", Art.Square, e.pos, 3000);
            go.color = new Color(1f, 0.2f, 0.2f, 0.45f);
            go.gameObject.SetActive(line);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (!e.alive) break;
                if (line) PlaceLine(go.transform, e.pos, to, 0.18f);
                e.SetTint(Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), Mathf.PingPong(t * 8f, 1f)));
                e.squash = Mathf.Sin(t * 30f) * 0.3f;
                yield return null;
            }
            e.SetTint(Color.white);
            Destroy(go.gameObject);
        }

        IEnumerator DoDash(Enemy e, Hero target)
        {
            yield return Telegraph(e, target.pos, 0.6f, true);
            if (!e.alive || !target.alive) yield break;
            Vector2 dir = (target.pos - e.pos).normalized;
            e.vel = dir * 14f;
            e.damp = 2.2f;
            e.dashing = true;
            e.dashHit = false;
            Sfx.Play("dash");
            float t = 0f;
            while (t < 1.5f && e.vel.magnitude > 1.2f) { t += Time.deltaTime; yield return null; }
            e.dashing = false;
            e.damp = EnemyDamp;
        }

        IEnumerator DoShoot(Enemy e, Hero target, int count)
        {
            yield return Telegraph(e, target.pos, 0.6f, count == 1);
            if (!e.alive || !target.alive) yield break;
            Vector2 baseDir = (target.pos - e.pos).normalized;
            for (int i = 0; i < count; i++)
            {
                float ang = count == 1 ? 0f : Mathf.Lerp(-40f, 40f, i / (float)(count - 1));
                Vector2 dir = Quaternion.Euler(0f, 0f, ang) * baseDir;
                var go = NewSr("Bullet", Art.Disc, e.pos, 3200);
                go.transform.localScale = Vector3.one * 0.42f;
                go.color = e.def.kind == EnemyKind.Boss ? new Color(1f, 0.3f, 0.55f) : new Color(1f, 0.85f, 0.3f);
                var b = go.gameObject.AddComponent<Bullet>();
                b.pos = e.pos;
                b.vel = dir * 7.5f;
                b.dmg = Mathf.RoundToInt(e.atk * (count > 1 ? 0.6f : 1f));
                bullets.Add(b);
            }
            Sfx.Play("dash", 0.7f, 1.6f);
            float t = 0f;
            while (bullets.Count > 0 && t < 3f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator DoSlam(Enemy e)
        {
            const float slamR = 3.0f;
            var area = NewSr("SlamArea", Art.Disc, e.pos, 2900);
            area.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            var edge = NewSr("SlamEdge", Art.Ring, e.pos, 2901);
            edge.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            float t = 0f, dur = 0.9f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = slamR * 2f * k;
                area.transform.localScale = Vector3.one * s;
                edge.transform.localScale = Vector3.one * slamR * 2f / 0.94f;
                area.transform.position = edge.transform.position = new Vector3(e.pos.x, e.pos.y, 0f);
                e.squash = Mathf.Sin(t * 25f) * 0.4f;
                yield return null;
            }
            Destroy(area.gameObject);
            Destroy(edge.gameObject);

            Sfx.Play("boom");
            shake = 0.6f;
            Fx.Ring(e.pos, new Color(1f, 0.5f, 0.3f, 0.9f), slamR * 2.1f, 0.5f);
            Fx.Burst(e.pos, new Color(1f, 0.6f, 0.3f), 26, 7f, 0.3f, 0.6f);
            foreach (var h in heroes)
            {
                if (!h.alive) continue;
                Vector2 d = h.pos - e.pos;
                if (d.magnitude <= slamR + h.radius)
                {
                    h.vel += d.normalized * 12f;
                    DamageHero(h, e.atk);
                }
            }
            yield return new WaitForSeconds(0.4f);
        }

        void DamageHero(Hero h, int dmg)
        {
            if (!h.alive) return;
            h.TakeDamage(dmg);
            hud.Popup(h.pos + Vector2.up * 0.6f, dmg.ToString(), new Color(1f, 0.35f, 0.35f), 56);
            Fx.Burst(h.pos, new Color(1f, 0.4f, 0.4f), 10, 4f, 0.2f);
            Sfx.Play("hurt");
            shake = Mathf.Max(shake, 0.25f);
            if (!h.alive)
            {
                Fx.Burst(h.pos, h.def.color, 24, 6f, 0.3f, 0.7f);
                Sfx.Play("kill");
                if (selected == h) selected = null;
            }
        }

        // ================================================================== hero actions

        void SelectHero(int i)
        {
            if (phase != Phase.Aim || aiming || i < 0 || i >= heroes.Count) return;
            var h = heroes[i];
            if (h.alive && !h.acted) selected = h;
        }

        void UseSkill(int i)
        {
            if (phase != Phase.Aim || aiming || i < 0 || i >= heroes.Count) return;
            var h = heroes[i];
            if (!h.alive || h.cd > 0) return;

            hud.Banner(h.def.skillName + "!", h.def.color, 0.9f);
            switch (h.def.skill)
            {
                case SkillKind.PowerShot:
                    h.powered = true;
                    if (!h.acted) selected = h;
                    Fx.Glow(h.pos, new Color(1f, 0.85f, 0.3f, 0.9f), 3.5f, 0.6f);
                    Sfx.Play("skill");
                    break;

                case SkillKind.Blast:
                    Sfx.Play("boom", 0.9f);
                    shake = 0.45f;
                    foreach (var e in new List<Enemy>(enemies))
                    {
                        if (!e.alive) continue;
                        int dmg = Mathf.RoundToInt(h.atk * 1.4f);
                        e.TakeDamage(dmg);
                        hud.Popup(e.pos + Vector2.up * 0.6f, dmg.ToString(), new Color(1f, 0.6f, 0.9f), 60);
                        Fx.Ring(e.pos, new Color(1f, 0.5f, 0.8f, 0.9f), 2.4f, 0.5f);
                        Fx.Burst(e.pos, new Color(1f, 0.5f, 0.8f), 12, 5f, 0.25f);
                        if (!e.alive) OnEnemyKilled(e);
                    }
                    break;

                case SkillKind.Heal:
                    Sfx.Play("heal");
                    foreach (var o in heroes)
                    {
                        if (!o.alive) continue;
                        int amt = Mathf.RoundToInt(o.maxHp * 0.4f);
                        int before = o.hp;
                        o.Heal(amt);
                        hud.Popup(o.pos + Vector2.up * 0.6f, "+" + (o.hp - before), new Color(0.5f, 1f, 0.6f), 56);
                        Fx.Ring(o.pos, new Color(0.5f, 1f, 0.6f, 0.9f), 2.2f, 0.6f);
                        Fx.Burst(o.pos, new Color(0.6f, 1f, 0.7f), 10, 3f, 0.2f, 0.7f);
                    }
                    break;
            }
            h.cd = h.def.skillCd;
        }

        void HandleAim()
        {
            if (selected == null || !selected.alive) { aiming = false; HideAim(); return; }

            if (!aiming && Input.GetMouseButtonDown(0) && !PointerOverUi())
            {
                aiming = true;
                aimStart = ScreenToWorld(Input.mousePosition);
            }

            if (!aiming) return;

            Vector2 cur = ScreenToWorld(Input.mousePosition);
            Vector2 pull = aimStart - cur;
            float len = Mathf.Min(pull.magnitude, MaxPull);
            if (pull.sqrMagnitude > 0.0001f) aimDir = pull.normalized;
            aimPower = Mathf.Clamp01(len / MaxPull);

            if (Input.GetMouseButtonUp(0))
            {
                aiming = false;
                HideAim();
                if (aimPower > 0.12f) Launch();
                return;
            }

            if (aimPower > 0.08f) ShowAim(); else HideAim();
        }

        bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0) return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        Vector2 ScreenToWorld(Vector2 sp)
        {
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -cam.transform.position.z));
            return new Vector2(w.x, w.y);
        }

        void Launch()
        {
            shooter = selected;
            shooter.acted = true;
            shooter.damp = ShooterDamp;
            shooter.vel = aimDir * Mathf.Lerp(MinSpeed, MaxSpeed, aimPower);
            combo = 0;
            hud.SetCombo(0);
            linked.Clear();
            stillTime = 0f;
            shotTime = 0f;
            phase = Phase.Moving;
            Sfx.Play("launch", 0.8f, Mathf.Lerp(0.9f, 1.2f, aimPower));
            Fx.Ring(shooter.pos, new Color(0.5f, 0.9f, 1f, 0.9f), shooter.radius * 4f, 0.3f);
        }

        void UpdateMoving(float dt)
        {
            shotTime += dt;
            trailT -= dt;
            if (trailT <= 0f && shooter.vel.sqrMagnitude > 9f)
            {
                trailT = 0.03f;
                Fx.Trail(shooter.pos, shooter.def.color, shooter.radius * 1.6f);
            }

            bool moving = false;
            foreach (var u in units) if (u.alive && u.vel.sqrMagnitude > 0.5f) { moving = true; break; }
            stillTime = moving ? 0f : stillTime + dt;
            if (stillTime > 0.3f || shotTime > 8f) EndShot();
        }

        void EndShot()
        {
            if (shooter != null)
            {
                shooter.damp = IdleDamp;
                shooter.powered = false;
            }
            foreach (var u in units) if (u.alive && u != shooter) u.vel = Vector2.zero;
            if (shooter != null) shooter.vel = Vector2.zero;
            shooter = null;
            combo = 0;
            StartCoroutine(HideComboLater());
            phase = Phase.Aim;
            shotDone = true;
        }

        IEnumerator HideComboLater()
        {
            yield return new WaitForSeconds(0.9f);
            if (combo == 0) hud.SetCombo(0);
        }

        // ================================================================== aim visuals

        void HideAim()
        {
            if (dots == null) return;
            foreach (var d in dots) d.gameObject.SetActive(false);
            arrow.gameObject.SetActive(false);
            bandL.gameObject.SetActive(false);
            bandR.gameObject.SetActive(false);
        }

        void ShowAim()
        {
            Vector2 p = selected.pos;
            Vector2 d = aimDir;
            float r = selected.radius;

            // dotted trajectory with wall bounces
            float total = Mathf.Min(Mathf.Lerp(MinSpeed, MaxSpeed, aimPower) / ShooterDamp * 0.85f, 7.5f);
            float step = 0.34f, travelled = 0f;
            int count = 0;
            Vector2 q = p;
            while (count < dots.Length && travelled < total)
            {
                q += d * step;
                if (q.x < arena.xMin + r) { q.x = 2f * (arena.xMin + r) - q.x; d.x = -d.x; }
                else if (q.x > arena.xMax - r) { q.x = 2f * (arena.xMax - r) - q.x; d.x = -d.x; }
                if (q.y < arena.yMin + r) { q.y = 2f * (arena.yMin + r) - q.y; d.y = -d.y; }
                else if (q.y > arena.yMax - r) { q.y = 2f * (arena.yMax - r) - q.y; d.y = -d.y; }
                travelled += step;

                bool hit = false;
                foreach (var e in enemies)
                    if (e.alive && (e.pos - q).sqrMagnitude < (e.radius + r) * (e.radius + r)) { hit = true; break; }
                if (travelled > 0.9f)
                {
                    var dot = dots[count++];
                    dot.gameObject.SetActive(true);
                    dot.transform.position = new Vector3(q.x, q.y, 0f);
                    float f = 1f - travelled / (total + 0.01f);
                    dot.color = new Color(1f, 1f, 1f, Mathf.Clamp(f + 0.25f, 0.25f, 1f));
                }
                if (hit) break;
            }
            for (int i = count; i < dots.Length; i++) dots[i].gameObject.SetActive(false);

            // arrow head
            Vector2 ap = p + aimDir * (0.85f + aimPower * 0.5f);
            arrow.gameObject.SetActive(true);
            arrow.transform.position = new Vector3(ap.x, ap.y, 0f);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f);
            arrow.transform.localScale = Vector3.one * (0.55f + aimPower * 0.25f);

            // sling band (V shape behind the hero)
            Vector2 pullPt = p - aimDir * (aimPower * MaxPull);
            Vector2 perp = new Vector2(-aimDir.y, aimDir.x) * (r * 0.9f);
            bandL.gameObject.SetActive(true);
            bandR.gameObject.SetActive(true);
            PlaceLine(bandL.transform, pullPt, p + perp, 0.09f);
            PlaceLine(bandR.transform, pullPt, p - perp, 0.09f);
        }

        static void PlaceLine(Transform t, Vector2 a, Vector2 b, float thick)
        {
            Vector2 d = b - a;
            t.position = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude, thick, 1f);
        }

        // ================================================================== update / physics

        void Update()
        {
            float dt = Time.deltaTime;
            float sdt = dt;
            if (hitStop > 0f) { hitStop -= dt; sdt = dt * 0.05f; }
            wallSfxT -= dt;

            Simulate(sdt);
            UpdateBullets(sdt);

            if (phase == Phase.Aim) HandleAim();
            else if (phase == Phase.Moving) UpdateMoving(dt);

            hud.RefreshCards(heroes, selected, phase == Phase.Aim);
        }

        void LateUpdate()
        {
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.6f);
            Vector2 o = shake > 0f ? Random.insideUnitCircle * shake : Vector2.zero;
            cam.transform.position = camBase + new Vector3(o.x, o.y, 0f);
        }

        void Simulate(float dt)
        {
            units.Clear();
            foreach (var h in heroes) if (h.alive) units.Add(h);
            foreach (var e in enemies) if (e.alive) units.Add(e);

            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.008f));
            float h2 = dt / steps;
            for (int s = 0; s < steps; s++) Step(h2);
            foreach (var u in units) u.SyncTransform();
            foreach (var h in heroes) if (!h.alive) h.SyncTransform();
            foreach (var e in enemies) if (!e.alive) e.SyncTransform();
        }

        void Step(float h)
        {
            foreach (var u in units)
            {
                if (!u.alive) continue;
                u.pos += u.vel * h;
                u.vel *= Mathf.Exp(-u.damp * h);
                WallBounce(u);
            }

            for (int i = 0; i < units.Count; i++)
            {
                var a = units[i];
                if (!a.alive) continue;
                for (int j = i + 1; j < units.Count; j++)
                {
                    var b = units[j];
                    if (!b.alive || !a.alive) continue;
                    Vector2 d = b.pos - a.pos;
                    float rr = a.radius + b.radius;
                    float d2 = d.sqrMagnitude;
                    if (d2 >= rr * rr) continue;

                    float dist = Mathf.Sqrt(d2);
                    Vector2 n = dist > 1e-4f ? d / dist : Vector2.right;
                    float overlap = rr - dist;
                    float tot = a.mass + b.mass;
                    a.pos -= n * (overlap * (b.mass / tot));
                    b.pos += n * (overlap * (a.mass / tot));

                    float vn = Vector2.Dot(b.vel - a.vel, n);
                    if (vn < 0f)
                    {
                        const float e = 0.85f;
                        float imp = -(1f + e) * vn / (1f / a.mass + 1f / b.mass);
                        a.vel -= n * (imp / a.mass);
                        b.vel += n * (imp / b.mass);
                        OnContact(a, b, -vn);
                    }
                }
            }
        }

        void WallBounce(Unit u)
        {
            float r = u.radius;
            float rest = (u is Hero) ? 0.92f : 0.7f;
            bool hit = false;
            if (u.pos.x < arena.xMin + r) { u.pos.x = arena.xMin + r; if (u.vel.x < 0f) { u.vel.x *= -rest; hit = true; } }
            else if (u.pos.x > arena.xMax - r) { u.pos.x = arena.xMax - r; if (u.vel.x > 0f) { u.vel.x *= -rest; hit = true; } }
            if (u.pos.y < arena.yMin + r) { u.pos.y = arena.yMin + r; if (u.vel.y < 0f) { u.vel.y *= -rest; hit = true; } }
            else if (u.pos.y > arena.yMax - r) { u.pos.y = arena.yMax - r; if (u.vel.y > 0f) { u.vel.y *= -rest; hit = true; } }

            if (hit && u.vel.sqrMagnitude > 9f)
            {
                Fx.Burst(u.pos, new Color(1f, 0.8f, 0.5f), 4, 3f, 0.12f, 0.25f);
                if (wallSfxT <= 0f) { Sfx.Play("wall", 0.5f); wallSfxT = 0.08f; }
            }
        }

        void UpdateBullets(float dt)
        {
            Rect big = new Rect(arena.xMin - 2f, arena.yMin - 2f, arena.width + 4f, arena.height + 4f);
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var b = bullets[i];
                b.pos += b.vel * dt;
                b.transform.position = new Vector3(b.pos.x, b.pos.y, 0f);
                bool remove = false;
                foreach (var h in heroes)
                {
                    if (!h.alive) continue;
                    float rr = h.radius + b.radius;
                    if ((h.pos - b.pos).sqrMagnitude < rr * rr)
                    {
                        DamageHero(h, b.dmg);
                        Fx.Burst(b.pos, new Color(1f, 0.8f, 0.3f), 8, 4f, 0.18f);
                        remove = true;
                        break;
                    }
                }
                if (!remove && !big.Contains(b.pos)) remove = true;
                if (remove)
                {
                    bullets.RemoveAt(i);
                    Destroy(b.gameObject);
                }
            }
        }

        // ================================================================== combat

        void OnContact(Unit a, Unit b, float speed)
        {
            Enemy ea = a as Enemy, eb = b as Enemy;
            Hero ha = a as Hero, hb = b as Hero;

            if (phase == Phase.Moving && shooter != null)
            {
                if (a == shooter && eb != null) HitEnemy(eb, speed);
                else if (b == shooter && ea != null) HitEnemy(ea, speed);
                else if (a == shooter && hb != null) Link(hb);
                else if (b == shooter && ha != null) Link(ha);
                else if (ea != null && eb != null && speed > 6f) ChainHit(ea, eb);
            }

            if (ea != null && ea.dashing && !ea.dashHit && hb != null) EnemyHitsHero(ea, hb);
            else if (eb != null && eb.dashing && !eb.dashHit && ha != null) EnemyHitsHero(eb, ha);
        }

        void HitEnemy(Enemy e, float speed)
        {
            float sf = Mathf.Clamp(speed / 12f, 0.55f, 1.5f);
            float mult = 1f + combo * 0.08f;
            if (shooter.powered) mult *= 2.5f;
            int dmg = Mathf.RoundToInt(shooter.atk * sf * mult * Random.Range(0.95f, 1.05f));
            combo++;
            e.TakeDamage(dmg);
            hud.SetCombo(combo);

            Color pc = shooter.powered ? new Color(1f, 0.9f, 0.3f) : (combo >= 5 ? new Color(1f, 0.6f, 0.2f) : Color.white);
            hud.Popup(e.pos + Vector2.up * 0.7f, dmg.ToString(), pc, shooter.powered ? 72 : 54);
            Fx.Burst(e.pos, shooter.def.color, 8, 5f, 0.2f);
            Fx.Glow(e.pos, new Color(1f, 1f, 0.8f, 0.9f), 1.6f, 0.2f);
            Sfx.Play("hit", 0.9f, Mathf.Min(1.8f, 1f + combo * 0.06f));
            hitStop = 0.05f;
            shake = Mathf.Max(shake, shooter.powered ? 0.3f : 0.12f);
            if (!e.alive) OnEnemyKilled(e);
        }

        void ChainHit(Enemy a, Enemy b)
        {
            if (a.chainCd > 0f || b.chainCd > 0f) return;
            a.chainCd = b.chainCd = 0.25f;
            int dmg = Mathf.RoundToInt(shooter.atk * 0.3f);
            foreach (var e in new[] { a, b })
            {
                e.TakeDamage(dmg);
                hud.Popup(e.pos + Vector2.up * 0.7f, dmg.ToString(), new Color(0.8f, 0.9f, 1f), 44);
                if (!e.alive) OnEnemyKilled(e);
            }
            combo++;
            hud.SetCombo(combo);
            Fx.Burst((a.pos + b.pos) * 0.5f, new Color(0.8f, 0.9f, 1f), 8, 4f, 0.18f);
            Sfx.Play("hit", 0.6f, 1.3f);
        }

        void Link(Hero ally)
        {
            if (!ally.alive || linked.Contains(ally)) return;
            linked.Add(ally);
            combo++;
            hud.SetCombo(combo);
            Fx.Ring(ally.pos, ally.def.color, 4.4f, 0.5f);
            Fx.Glow(ally.pos, ally.def.color, 3f, 0.4f);
            Sfx.Play("skill", 0.5f, 1.4f);
            hud.Popup(ally.pos + Vector2.up * 0.9f, "LINK!", ally.def.color, 50);

            if (ally.def.cls == HeroClass.Healer)
            {
                int amt = Mathf.RoundToInt(shooter.maxHp * 0.15f);
                shooter.Heal(amt);
                hud.Popup(shooter.pos + Vector2.up * 0.6f, "+" + amt, new Color(0.5f, 1f, 0.6f), 48);
            }

            foreach (var e in new List<Enemy>(enemies))
            {
                if (!e.alive || (e.pos - ally.pos).magnitude > 2.2f + e.radius) continue;
                int dmg = Mathf.RoundToInt(ally.atk * 0.9f * (1f + combo * 0.05f));
                e.TakeDamage(dmg);
                hud.Popup(e.pos + Vector2.up * 0.7f, dmg.ToString(), ally.def.color, 48);
                Fx.Burst(e.pos, ally.def.color, 8, 4f, 0.2f);
                if (!e.alive) OnEnemyKilled(e);
            }
            hitStop = 0.06f;
        }

        void EnemyHitsHero(Enemy e, Hero h)
        {
            e.dashHit = true;
            DamageHero(h, e.atk);
        }

        void OnEnemyKilled(Enemy e)
        {
            Fx.Burst(e.pos, e.def.color, 18, 6f, 0.28f, 0.6f);
            Fx.Ring(e.pos, e.def.color, e.radius * 5f, 0.4f);
            Sfx.Play("kill", 0.8f);
            shake = Mathf.Max(shake, e.def.kind == EnemyKind.Boss ? 0.6f : 0.2f);

            int c = Mathf.RoundToInt(e.def.coins * stage.hpScale * Random.Range(0.9f, 1.15f));
            coins += c;
            hud.Popup(e.pos + Vector2.up * 1.1f, "+" + c, new Color(1f, 0.85f, 0.2f), 44);
            Fx.Burst(e.pos, new Color(1f, 0.85f, 0.2f), 6, 4f, 0.2f, 0.7f);
            Sfx.Play("coin", 0.4f);
            if (e.def.kind == EnemyKind.Boss || Random.value < 0.12f) chests++;
            if (Random.value < 0.25f) bags++;
            hud.SetCounts(chests, bags, coins);
            e.UpdatePips();
        }
    }
}
