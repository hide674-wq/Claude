using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlingshotHeroes
{
    public class Hud : MonoBehaviour
    {
        public Action onRetire;
        public Action<int> onSelect, onSkill;

        class Card
        {
            public Image portrait, frame, hpFill;
            public Button skillBtn;
            public Image skillImg;
            public Text skillText;
        }

        class PopupItem
        {
            public Text text;
            public Vector3 world;
            public float life, maxLife, rise;
        }

        Camera cam;
        RectTransform canvasRect, popupLayer, comboBox;
        Text waveText, turnText, chestText, bagText, coinText, comboNum, bannerText;
        Card[] cards = new Card[3];
        readonly List<PopupItem> popups = new List<PopupItem>();
        readonly Stack<Text> popupPool = new Stack<Text>();
        float bannerT, bannerMax, comboPop;

        public static Hud Create(Camera cam)
        {
            var go = new GameObject("HUD");
            var h = go.AddComponent<Hud>();
            h.Build(cam);
            return h;
        }

        void Build(Camera c)
        {
            cam = c;
            var canvas = UIKit.CreateCanvas("HudCanvas", 10, transform);
            canvasRect = (RectTransform)canvas.transform;

            var safe = UIKit.NewRect("Safe", canvas.transform);
            Rect sa = Screen.safeArea;
            safe.anchorMin = new Vector2(sa.x / Screen.width, sa.y / Screen.height);
            safe.anchorMax = new Vector2((sa.x + sa.width) / Screen.width, (sa.y + sa.height) / Screen.height);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;

            // ---- top bar
            var bar = UIKit.MakePanel(safe, "TopBar", new Color(0f, 0f, 0f, 0.6f));
            UIKit.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 100f));
            bar.rectTransform.anchorMin = new Vector2(0f, 1f);
            bar.rectTransform.anchorMax = new Vector2(1f, 1f);
            bar.rectTransform.sizeDelta = new Vector2(0f, 100f);

            Text l;
            var retire = UIKit.MakeButton(bar.transform, "Retire", "リタイア", 34, new Color(0.45f, 0.45f, 0.5f), () => { if (onRetire != null) onRetire(); }, out l);
            UIKit.Place((RectTransform)retire.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(200f, 66f));

            chestText = Counter(bar.transform, Art.Chest, 245f);
            bagText = Counter(bar.transform, Art.Bag, 435f);
            coinText = Counter(bar.transform, Art.Coin, 625f);

            waveText = UIKit.MakeLabel(bar.transform, "Wave", "WAVE 1/3", 38, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleRight);
            UIKit.Place(waveText.rectTransform, new Vector2(1f, 0.65f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(240f, 40f));
            turnText = UIKit.MakeLabel(bar.transform, "Turn", "TURN 1", 30, Color.white, TextAnchor.MiddleRight);
            UIKit.Place(turnText.rectTransform, new Vector2(1f, 0.25f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(240f, 36f));

            // ---- hero cards
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var card = new Card();
                var root = UIKit.NewRect("Card" + i, safe);
                UIKit.Place(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(15f + i * 210f, -112f), new Vector2(200f, 270f));

                card.frame = UIKit.MakePanel(root, "Frame", Color.white, Art.Ring);
                UIKit.Place(card.frame.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(160f, 160f));
                card.portrait = UIKit.MakePanel(root, "Portrait", Color.white, Art.Heroes[i], true);
                UIKit.Place(card.portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(140f, 140f));
                var pb = card.portrait.gameObject.AddComponent<Button>();
                pb.targetGraphic = card.portrait;
                pb.onClick.AddListener(() => { Sfx.Play("click"); if (onSelect != null) onSelect(idx); });

                var hpBg = UIKit.MakePanel(root, "HpBg", new Color(0f, 0f, 0f, 0.7f));
                UIKit.Place(hpBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(170f, 16f));
                card.hpFill = UIKit.MakePanel(hpBg.transform, "HpFill", new Color(0.35f, 0.9f, 0.45f));
                card.hpFill.rectTransform.anchorMin = new Vector2(0f, 0f);
                card.hpFill.rectTransform.anchorMax = new Vector2(0f, 1f);
                card.hpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
                card.hpFill.rectTransform.offsetMin = new Vector2(2f, 2f);
                card.hpFill.rectTransform.offsetMax = new Vector2(168f, -2f);

                Text st;
                card.skillBtn = UIKit.MakeButton(root, "Skill", "スキル", 30, new Color(0.2f, 0.5f, 0.9f), () => { if (onSkill != null) onSkill(idx); }, out st);
                UIKit.Place((RectTransform)card.skillBtn.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(190f, 62f));
                card.skillImg = card.skillBtn.GetComponent<Image>();
                card.skillText = st;
                cards[i] = card;
            }

            // ---- combo
            comboBox = UIKit.NewRect("Combo", safe);
            UIKit.Place(comboBox, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 300f), new Vector2(480f, 260f));
            comboNum = UIKit.MakeLabel(comboBox, "Num", "0", 170, new Color(1f, 0.6f, 0.15f), TextAnchor.MiddleRight);
            UIKit.Place(comboNum.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(480f, 180f));
            var cl = UIKit.MakeLabel(comboBox, "Label", "COMBO", 64, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleRight);
            UIKit.Place(cl.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(480f, 80f));
            comboBox.gameObject.SetActive(false);

            // ---- banner & popups
            bannerText = UIKit.MakeLabel(canvas.transform, "Banner", "", 120, Color.white);
            UIKit.Place(bannerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1080f, 200f));
            bannerText.gameObject.SetActive(false);

            popupLayer = UIKit.NewRect("Popups", canvas.transform);
            UIKit.Stretch(popupLayer);
        }

        Text Counter(Transform parent, Sprite icon, float x)
        {
            var img = UIKit.MakePanel(parent, "Icon", Color.white, icon);
            UIKit.Place(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(58f, 58f));
            var t = UIKit.MakeLabel(parent, "Count", "0", 40, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x + 66f, 0f), new Vector2(120f, 60f));
            return t;
        }

        // ------------------------------------------------------------------ API

        public void SetWave(int w, int n) { waveText.text = "WAVE " + w + "/" + n; }
        public void SetTurn(int t) { turnText.text = "TURN " + t; }

        public void SetCounts(int chest, int bag, int coin)
        {
            chestText.text = chest.ToString();
            bagText.text = bag.ToString();
            coinText.text = coin.ToString();
        }

        public void SetCombo(int n)
        {
            if (n <= 0) { comboBox.gameObject.SetActive(false); return; }
            comboBox.gameObject.SetActive(true);
            comboNum.text = n.ToString();
            comboPop = 1f;
        }

        public void Banner(string text, Color color, float duration = 1.2f)
        {
            bannerText.text = text;
            bannerText.color = color;
            bannerT = bannerMax = duration;
            bannerText.gameObject.SetActive(true);
        }

        public void Popup(Vector3 world, string text, Color color, int size = 48)
        {
            Text t = popupPool.Count > 0 ? popupPool.Pop() : null;
            if (t == null)
            {
                t = UIKit.MakeLabel(popupLayer, "Pop", "", size, color);
                t.rectTransform.sizeDelta = new Vector2(300f, 80f);
            }
            t.gameObject.SetActive(true);
            t.text = text;
            t.fontSize = size;
            t.color = color;
            popups.Add(new PopupItem { text = t, world = world, life = 0.9f, maxLife = 0.9f });
        }

        public void RefreshCards(IList<Hero> heroes, Hero selected, bool aimPhase)
        {
            for (int i = 0; i < cards.Length && i < heroes.Count; i++)
            {
                var h = heroes[i];
                var c = cards[i];
                float f = h.maxHp > 0 ? Mathf.Clamp01(h.hp / (float)h.maxHp) : 0f;
                var rt = c.hpFill.rectTransform;
                rt.offsetMax = new Vector2(2f + 166f * f, -2f);

                bool sel = h == selected && aimPhase && h.alive;
                if (!h.alive) c.portrait.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);
                else if (h.acted) c.portrait.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                else c.portrait.color = Color.white;
                c.frame.color = sel ? new Color(1f, 0.85f, 0.2f) : (h.alive ? new Color(0.3f, 0.7f, 1f) : new Color(0.3f, 0.3f, 0.3f));

                bool ready = h.alive && h.cd == 0;
                c.skillText.text = !h.alive ? "---" : (h.cd > 0 ? "CD " + h.cd : h.def.skillName);
                c.skillBtn.interactable = ready && aimPhase;
                c.skillImg.color = ready ? new Color(0.2f, 0.55f, 0.95f) : new Color(0.35f, 0.35f, 0.4f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (bannerT > 0f)
            {
                bannerT -= dt;
                float k = 1f - bannerT / bannerMax;
                float a = k < 0.15f ? k / 0.15f : (k > 0.8f ? (1f - k) / 0.2f : 1f);
                var c = bannerText.color;
                c.a = Mathf.Clamp01(a);
                bannerText.color = c;
                float s = 1f + 0.25f * Mathf.Clamp01(1f - k * 4f);
                bannerText.rectTransform.localScale = new Vector3(s, s, 1f);
                if (bannerT <= 0f) bannerText.gameObject.SetActive(false);
            }

            if (comboPop > 0f)
            {
                comboPop = Mathf.Max(0f, comboPop - dt * 5f);
                float s = 1f + comboPop * 0.35f;
                comboBox.localScale = new Vector3(s, s, 1f);
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var p = popups[i];
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.text.gameObject.SetActive(false);
                    popupPool.Push(p.text);
                    popups.RemoveAt(i);
                    continue;
                }
                p.rise += dt * 1.2f;
                Vector3 sp = cam.WorldToScreenPoint(p.world + new Vector3(0f, p.rise, 0f));
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out local);
                p.text.rectTransform.anchoredPosition = local;
                var c = p.text.color;
                c.a = Mathf.Clamp01(p.life / 0.3f);
                p.text.color = c;
            }
        }
    }
}
