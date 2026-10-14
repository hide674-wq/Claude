using System.Collections.Generic;
using UnityEngine;

namespace SlingshotHeroes
{
    public enum HeroClass { Knight, Mage, Healer }
    public enum SkillKind { PowerShot, Blast, Heal }
    public enum EnemyKind { Blob, Spiker, Boss }
    public enum AttackKind { Dash, Shoot, Slam }

    public class HeroDef
    {
        public string name;
        public HeroClass cls;
        public Color color;
        public int hp, atk;
        public float radius, mass;
        public SkillKind skill;
        public string skillName;
        public int skillCd;
    }

    public class EnemyDef
    {
        public string name;
        public EnemyKind kind;
        public Color color;
        public int hp, atk, coins, wait;
        public float radius, mass;
        public AttackKind attack;

        public string Key { get { return kind + "_" + ColorUtility.ToHtmlStringRGB(color); } }
    }

    public class WaveDef
    {
        public List<EnemyDef> enemies = new List<EnemyDef>();
    }

    public class StageDef
    {
        public string name, desc;
        public float hpScale, atkScale;
        public List<WaveDef> waves = new List<WaveDef>();
    }

    public static class GameData
    {
        public static readonly HeroDef[] Heroes;
        public static readonly StageDef[] Stages;

        static WaveDef Wave(params EnemyDef[] list)
        {
            var w = new WaveDef();
            w.enemies.AddRange(list);
            return w;
        }

        static StageDef Stage(string name, string desc, float hp, float atk, params WaveDef[] waves)
        {
            var s = new StageDef { name = name, desc = desc, hpScale = hp, atkScale = atk };
            s.waves.AddRange(waves);
            return s;
        }

        static GameData()
        {
            Heroes = new[]
            {
                new HeroDef
                {
                    name = "アルス", cls = HeroClass.Knight, color = new Color(0.25f, 0.55f, 1f),
                    hp = 1800, atk = 230, radius = 0.5f, mass = 1.4f,
                    skill = SkillKind.PowerShot, skillName = "渾身撃", skillCd = 3
                },
                new HeroDef
                {
                    name = "ミラ", cls = HeroClass.Mage, color = new Color(1f, 0.45f, 0.7f),
                    hp = 1200, atk = 270, radius = 0.45f, mass = 1.0f,
                    skill = SkillKind.Blast, skillName = "爆裂", skillCd = 4
                },
                new HeroDef
                {
                    name = "ティナ", cls = HeroClass.Healer, color = new Color(0.35f, 0.85f, 0.5f),
                    hp = 1500, atk = 170, radius = 0.45f, mass = 1.1f,
                    skill = SkillKind.Heal, skillName = "ヒール", skillCd = 4
                }
            };

            var red = new EnemyDef
            {
                name = "ルビーン", kind = EnemyKind.Blob, color = new Color(0.93f, 0.3f, 0.35f),
                hp = 520, atk = 170, coins = 12, wait = 2, radius = 0.42f, mass = 0.9f, attack = AttackKind.Dash
            };
            var orange = new EnemyDef
            {
                name = "オレンジ", kind = EnemyKind.Blob, color = new Color(1f, 0.55f, 0.15f),
                hp = 640, atk = 190, coins = 15, wait = 2, radius = 0.45f, mass = 1.0f, attack = AttackKind.Dash
            };
            var yellow = new EnemyDef
            {
                name = "イエロン", kind = EnemyKind.Blob, color = new Color(0.85f, 0.9f, 0.2f),
                hp = 480, atk = 140, coins = 14, wait = 2, radius = 0.42f, mass = 0.9f, attack = AttackKind.Shoot
            };
            var spiker = new EnemyDef
            {
                name = "ダークスパイク", kind = EnemyKind.Spiker, color = new Color(0.55f, 0.3f, 0.85f),
                hp = 900, atk = 210, coins = 30, wait = 3, radius = 0.5f, mass = 1.2f, attack = AttackKind.Shoot
            };
            var boss = new EnemyDef
            {
                name = "魔獣ガルド", kind = EnemyKind.Boss, color = new Color(0.6f, 0.15f, 0.35f),
                hp = 7000, atk = 420, coins = 200, wait = 2, radius = 1.1f, mass = 6f, attack = AttackKind.Slam
            };

            Stages = new[]
            {
                Stage("荒れ地の入口", "はじめてのクエスト。まずは操作に慣れよう。", 1f, 1f,
                    Wave(red, red, red, orange),
                    Wave(red, red, yellow, yellow, spiker),
                    Wave(boss, red, red)),
                Stage("ひび割れた大地", "敵の数が増える。連携攻撃を狙おう。", 1.6f, 1.3f,
                    Wave(red, orange, orange, yellow, yellow),
                    Wave(spiker, spiker, red, orange, yellow),
                    Wave(boss, spiker, orange, orange)),
                Stage("魔王の間", "最終決戦。スキルを温存しよう。", 2.4f, 1.7f,
                    Wave(orange, orange, yellow, yellow, spiker, red),
                    Wave(spiker, spiker, spiker, orange, orange, yellow),
                    Wave(boss, spiker, spiker, orange, yellow, red))
            };
        }
    }
}
