using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlingshotHeroes
{
    /// <summary>Procedurally synthesised sound effects (no audio files required).</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static AudioSource[] sources;
        static int next;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        public static bool Mute;

        public static void Init(GameObject host)
        {
            if (sources != null) return;
            sources = new AudioSource[8];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = host.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }
            Build();
        }

        static float Noise() { return UnityEngine.Random.value * 2f - 1f; }
        static float Sin(float f, float t) { return Mathf.Sin(2f * Mathf.PI * f * t); }

        static void Add(string name, float dur, Func<float, float> fn)
        {
            int n = (int)(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float fade = Mathf.Clamp01((dur - t) * 30f);
                data[i] = Mathf.Clamp(fn(t) * fade, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            clips[name] = clip;
        }

        static float Note(float t, float start, float len, float freq)
        {
            if (t < start || t > start + len) return 0f;
            float lt = t - start;
            return Sin(freq, lt) * Mathf.Exp(-lt * 7f);
        }

        static void Build()
        {
            Add("hit", 0.18f, t =>
                Mathf.Sin(2f * Mathf.PI * (80f * t + 27.5f * (1f - Mathf.Exp(-8f * t)))) * Mathf.Exp(-18f * t) * 0.8f
                + Noise() * Mathf.Exp(-40f * t) * 0.5f);
            Add("launch", 0.25f, t => Mathf.Sin(2f * Mathf.PI * (200f * t + 750f * t * t)) * Mathf.Exp(-6f * t) * 0.6f);
            Add("wall", 0.08f, t => Sin(400f, t) * Mathf.Exp(-50f * t) * 0.4f);
            Add("click", 0.06f, t => Sin(820f, t) * Mathf.Exp(-45f * t) * 0.4f);
            Add("coin", 0.25f, t => (t < 0.07f ? Sin(1200f, t) : Sin(1600f, t)) * Mathf.Exp(-9f * t) * 0.4f);
            Add("skill", 0.5f, t => (Note(t, 0f, 0.15f, 523f) + Note(t, 0.08f, 0.15f, 659f) + Note(t, 0.16f, 0.15f, 784f) + Note(t, 0.24f, 0.25f, 1047f)) * 0.35f);
            Add("heal", 0.5f, t => (Note(t, 0f, 0.25f, 659f) + Note(t, 0.15f, 0.35f, 880f)) * 0.4f);
            Add("hurt", 0.3f, t => (Mathf.Sign(Sin(110f, t)) * 0.4f + Noise() * 0.3f) * Mathf.Exp(-10f * t));
            Add("dash", 0.3f, t => Noise() * Mathf.Exp(-5f * t) * 0.3f * Mathf.Clamp01(t * 20f));
            Add("kill", 0.3f, t => (Noise() * 0.5f + Sin(120f - 150f * t, t) * 0.6f) * Mathf.Exp(-12f * t));
            Add("boom", 0.5f, t => (Noise() * 0.6f + Sin(60f, t) * 0.7f) * Mathf.Exp(-6f * t));
            Add("win", 1.2f, t => (Note(t, 0f, 0.3f, 523f) + Note(t, 0.15f, 0.3f, 659f) + Note(t, 0.3f, 0.3f, 784f) + Note(t, 0.5f, 0.7f, 1047f)) * 0.4f);
            Add("lose", 0.9f, t => Sin(330f - 190f * t, t) * Mathf.Exp(-3f * t) * 0.5f);
        }

        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (Mute || sources == null) return;
            AudioClip clip;
            if (!clips.TryGetValue(name, out clip)) return;
            var s = sources[next];
            next = (next + 1) % sources.Length;
            s.pitch = pitch;
            s.PlayOneShot(clip, vol);
        }
    }
}
