using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SlingshotHeroes
{
    /// <summary>Creates itself at startup (no scene setup needed) and drives title / stage select / battle / result.</summary>
    public class Game : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindObjectOfType<Game>() != null) return;
            new GameObject("Game").AddComponent<Game>();
        }

        Camera cam;
        Canvas menuCanvas;
        GameObject titlePanel, stagePanel, resultPanel, confirmPanel;
        Text coinText, resultTitle, resultBody;
        Button[] stageButtons = new Button[3];
        Text[] stageLabels = new Text[3];
        RectTransform[] titleHeroes = new RectTransform[3];

        GameObject battleRoot;
        Battle battle;
        Hud hud;
        int stageIndex;
        float titleT;

        static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
        static readonly Color Blue = new Color(0.2f, 0.5f, 0.95f);

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            Art.Init();
            UIKit.Init();
            Sfx.Init(gameObject);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.04f, 0.09f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            menuCanvas = UIKit.CreateCanvas("MenuCanvas", 20, transform);
            BuildTitle();
            BuildStageSelect();
            BuildResult();
            BuildConfirm();
            ShowBackdrop();
            ShowTitle();
        }

        // ================================================================== backdrop (sky behind the menus)

        GameObject backdrop;

        void ShowBackdrop()
        {
            if (backdrop != null) return;
            backdrop = new GameObject("Backdrop");
            var sky = new GameObject("Sky").AddComponent<SpriteRenderer>();
            sky.transform.SetParent(backdrop.transform, false);
            sky.sprite = Art.Sky;
            sky.sortingOrder = -6000;
            sky.transform.localScale = new Vector3(60f, 1.2f, 1f);
            var ground = new GameObject("Ground").AddComponent<SpriteRenderer>();
            ground.transform.SetParent(backdrop.transform, false);
            ground.sprite = Art.Ground;
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = new Vector2(40f, 9f);
            ground.transform.position = new Vector3(0f, -9f, 0f);
            ground.sortingOrder = -5000;
            var moon = new GameObject("Moon").AddComponent<SpriteRenderer>();
            moon.transform.SetParent(backdrop.transform, false);
            moon.sprite = Art.Moon;
            moon.transform.position = new Vector3(2.6f, 5.5f, 0f);
            moon.transform.localScale = Vector3.one * 2.2f;
            moon.sortingOrder = -5900;
        }

        // ================================================================== menus

        Image FullPanel(string name, Color c)
        {
            var img = UIKit.MakePanel(menuCanvas.transform, name, c, null, true);
            UIKit.Stretch(img.rectTransform);
            return img;
        }

        void BuildTitle()
        {
            var panel = FullPanel("Title", new Color(0f, 0f, 0f, 0f));
            titlePanel = panel.gameObject;

            var t1 = UIKit.MakeLabel(panel.transform, "T1", "スリングショット", 96, Gold);
            UIKit.Place(t1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 620f), new Vector2(1000f, 130f));
            var t2 = UIKit.MakeLabel(panel.transform, "T2", "ヒーローズ", 130, Color.white);
            UIKit.Place(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 480f), new Vector2(1000f, 170f));
            var t3 = UIKit.MakeLabel(panel.transform, "T3", "引っ張って、はじけ飛べ！", 44, new Color(0.7f, 0.9f, 1f));
            UIKit.Place(t3.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 360f), new Vector2(1000f, 70f));

            for (int i = 0; i < 3; i++)
            {
                var img = UIKit.MakePanel(panel.transform, "Hero" + i, Color.white, Art.Heroes[i]);
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 300f, 30f), new Vector2(260f, 260f));
                titleHeroes[i] = img.rectTransform;
            }

            var start = UIKit.MakeButton(panel.transform, "Start", "ゲームスタート", 64, Blue, ShowStageSelect);
            UIKit.Place((RectTransform)start.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(760f, 150f));

            coinText = UIKit.MakeLabel(panel.transform, "Coins", "", 44, Gold);
            UIKit.Place(coinText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(900f, 70f));

            Text l;
            var reset = UIKit.MakeButton(panel.transform, "Sound", "サウンド ON/OFF", 32, new Color(0.35f, 0.35f, 0.4f),
                () => { Sfx.Mute = !Sfx.Mute; }, out l);
            UIKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(420f, 70f));
        }

        void BuildStageSelect()
        {
            var panel = FullPanel("Stage", new Color(0.03f, 0.02f, 0.08f, 0.88f));
            stagePanel = panel.gameObject;

            var t = UIKit.MakeLabel(panel.transform, "Head", "ステージ選択", 80, Gold);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 120f));

            for (int i = 0; i < GameData.Stages.Length; i++)
            {
                int idx = i;
                Text label;
                var b = UIKit.MakeButton(panel.transform, "Stage" + i, "", 44, Blue, () => StartBattle(idx), out label);
                UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f - i * 290f), new Vector2(900f, 250f));
                label.alignment = TextAnchor.MiddleCenter;
                stageButtons[i] = b;
                stageLabels[i] = label;
            }

            var back = UIKit.MakeButton(panel.transform, "Back", "もどる", 48, new Color(0.4f, 0.4f, 0.45f), ShowTitle);
            UIKit.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(420f, 110f));
        }

        void BuildResult()
        {
            var panel = FullPanel("Result", new Color(0f, 0f, 0f, 0.8f));
            resultPanel = panel.gameObject;

            resultTitle = UIKit.MakeLabel(panel.transform, "Head", "", 110, Gold);
            UIKit.Place(resultTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 480f), new Vector2(1000f, 160f));
            resultBody = UIKit.MakeLabel(panel.transform, "Body", "", 54, Color.white);
            resultBody.lineSpacing = 1.4f;
            UIKit.Place(resultBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(900f, 500f));

            var again = UIKit.MakeButton(panel.transform, "Again", "もう一度", 56, Blue, () => StartBattle(stageIndex));
            UIKit.Place((RectTransform)again.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(640f, 130f));
            var menu = UIKit.MakeButton(panel.transform, "Menu", "ステージ選択", 56, new Color(0.4f, 0.4f, 0.45f), ShowStageSelect);
            UIKit.Place((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -500f), new Vector2(640f, 130f));
        }

        void BuildConfirm()
        {
            var panel = FullPanel("Confirm", new Color(0f, 0f, 0f, 0.7f));
            confirmPanel = panel.gameObject;
            var box = UIKit.MakePanel(panel.transform, "Box", new Color(0.12f, 0.1f, 0.2f, 0.98f), Art.RoundRect);
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 500f));
            var q = UIKit.MakeLabel(box.transform, "Q", "リタイアしますか？", 60, Color.white);
            UIKit.Place(q.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 100f));
            var yes = UIKit.MakeButton(box.transform, "Yes", "はい", 52, new Color(0.8f, 0.3f, 0.3f), ConfirmRetire);
            UIKit.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-190f, 70f), new Vector2(320f, 120f));
            var no = UIKit.MakeButton(box.transform, "No", "いいえ", 52, Blue, CloseConfirm);
            UIKit.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(190f, 70f), new Vector2(320f, 120f));
        }

        void HideMenus()
        {
            titlePanel.SetActive(false);
            stagePanel.SetActive(false);
            resultPanel.SetActive(false);
            confirmPanel.SetActive(false);
        }

        void ShowTitle()
        {
            DestroyBattle();
            ShowBackdrop();
            HideMenus();
            coinText.text = "所持コイン  " + PlayerPrefs.GetInt("coins", 0);
            titlePanel.SetActive(true);
        }

        void ShowStageSelect()
        {
            DestroyBattle();
            ShowBackdrop();
            HideMenus();
            for (int i = 0; i < GameData.Stages.Length; i++)
            {
                bool unlocked = i == 0 || PlayerPrefs.GetInt("clear_" + (i - 1), 0) == 1;
                bool cleared = PlayerPrefs.GetInt("clear_" + i, 0) == 1;
                var s = GameData.Stages[i];
                stageLabels[i].text = unlocked
                    ? "STAGE " + (i + 1) + (cleared ? "  ★CLEAR" : "") + "\n" + s.name + "\n<size=32>" + s.desc + "</size>"
                    : "STAGE " + (i + 1) + "\nロック中：前のステージをクリアしよう";
                stageLabels[i].supportRichText = true;
                stageButtons[i].interactable = unlocked;
            }
            stagePanel.SetActive(true);
        }

        // ================================================================== battle

        void StartBattle(int idx)
        {
            stageIndex = idx;
            DestroyBattle();
            HideMenus();
            if (backdrop != null) { Destroy(backdrop); backdrop = null; }

            battleRoot = new GameObject("Battle");
            hud = Hud.Create(cam);
            hud.transform.SetParent(battleRoot.transform, false);
            hud.onRetire = ShowConfirm;
            battle = battleRoot.AddComponent<Battle>();
            battle.Begin(this, cam, hud, idx);
        }

        void DestroyBattle()
        {
            Time.timeScale = 1f;
            if (battleRoot != null) Destroy(battleRoot);
            battleRoot = null;
            battle = null;
            hud = null;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.orthographicSize = 8f;
        }

        void ShowConfirm()
        {
            if (battle == null) return;
            confirmPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        void CloseConfirm()
        {
            confirmPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        void ConfirmRetire()
        {
            confirmPanel.SetActive(false);
            Time.timeScale = 1f;
            ShowStageSelect();
        }

        public void OnBattleEnd(Battle.Result r)
        {
            int total = PlayerPrefs.GetInt("coins", 0);
            int gained = r.win ? r.coins : r.coins / 2;
            PlayerPrefs.SetInt("coins", total + gained);
            if (r.win) PlayerPrefs.SetInt("clear_" + stageIndex, 1);
            PlayerPrefs.Save();

            DestroyBattle();
            ShowBackdrop();
            HideMenus();
            resultTitle.text = r.win ? "QUEST CLEAR!" : "GAME OVER";
            resultTitle.color = r.win ? Gold : new Color(1f, 0.45f, 0.45f);
            resultBody.text = "ターン数   " + r.turns + "\n獲得コイン   " + gained + "\n宝箱   " + r.chests + "\n素材   " + r.bags;
            resultPanel.SetActive(true);
        }

        void Update()
        {
            if (titlePanel != null && titlePanel.activeSelf)
            {
                titleT += Time.deltaTime;
                for (int i = 0; i < 3; i++)
                {
                    var p = titleHeroes[i].anchoredPosition;
                    p.y = 30f + Mathf.Sin(titleT * 2.2f + i * 1.3f) * 22f;
                    titleHeroes[i].anchoredPosition = p;
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) && battle != null && !confirmPanel.activeSelf) ShowConfirm();
        }
    }
}
